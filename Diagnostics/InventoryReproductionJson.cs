#nullable disable
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using SephiriaEnhancements.Inventory;
using SephiriaEnhancements.Runtime.Inventory;

namespace SephiriaEnhancements.Diagnostics
{
    // Replay and recording share a closed set of constructor inputs. Computed
    // properties are not part of the input contract; evidence is projected explicitly.
    internal static class InventoryReproductionJson
    {
        internal const int SchemaVersion = 3;
        private static readonly Dictionary<Type, ConstructorInfo> Constructors = BuildContracts();

        internal static ConstructorInfo InputConstructor(Type type) => Constructors.TryGetValue(type, out ConstructorInfo constructor)
            ? constructor : throw new InvalidOperationException("Unsupported inventory reproduction type: " + type.Name);

        internal static PropertyInfo[] InputProperties(Type type) => InputConstructor(type).GetParameters()
            .Select(parameter => type.GetProperty(parameter.Name, BindingFlags.Instance | BindingFlags.Public |
                BindingFlags.NonPublic | BindingFlags.IgnoreCase) ??
                throw new InvalidOperationException("Missing inventory input property: " + type.Name + "." + parameter.Name)).ToArray();

        private static Dictionary<Type, ConstructorInfo> BuildContracts()
        {
            var contracts = new Dictionary<Type, ConstructorInfo>();
            foreach (Type root in new[] { typeof(InventorySnapshot), typeof(InventoryOptimizationPreferences),
                         typeof(InventorySearchBudget), typeof(InventoryLayoutProjection),
                         typeof(InventoryOptimizationScore), typeof(InventoryOptimizationTargetEvaluation),
                         typeof(ResolvedArtifactOptimizationRule), typeof(ResolvedComboOptimizationRule),
                         typeof(InventorySettlementValidationSnapshot) }) Add(root);
            return contracts;

            void Add(Type type)
            {
                if (type.IsArray) { Add(type.GetElementType()); return; }
                Type nullable = Nullable.GetUnderlyingType(type);
                if (nullable != null) { Add(nullable); return; }
                if (type.IsPrimitive || type.IsEnum || type == typeof(string) || contracts.ContainsKey(type)) return;
                // Model inputs have one internal constructor. Private implementation
                // overloads are deliberately excluded, rather than guessed from a log.
                ConstructorInfo constructor = type.GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic)
                    .Single(candidate => candidate.IsAssembly);
                contracts.Add(type, constructor);
                foreach (ParameterInfo parameter in constructor.GetParameters()) Add(parameter.ParameterType);
            }
        }

        internal static string Serialize(object value)
        {
            var output = new StringBuilder();
            Write(output, value);
            return output.ToString();
        }

        private static void Write(StringBuilder output, object value)
        {
            if (value == null) { output.Append("null"); return; }
            if (value is string text) { Quote(output, text); return; }
            if (value is bool flag) { output.Append(flag ? "true" : "false"); return; }
            Type type = value.GetType();
            if (type.IsEnum) { Quote(output, value.ToString()); return; }
            if (type.IsPrimitive || value is decimal)
            {
                string number = Convert.ToString(value, CultureInfo.InvariantCulture);
                if (number == "NaN" || number == "Infinity" || number == "-Infinity") Quote(output, number);
                else output.Append(number);
                return;
            }
            if (value is InventoryLayoutProjection layout)
            {
                Write(output, new { CellsByItem = layout.CopyCells(), RotationsByItem = layout.CopyRotations() });
                return;
            }
            if (value is IEnumerable sequence)
            {
                output.Append('[');
                bool first = true;
                foreach (object entry in sequence)
                {
                    if (!first) output.Append(',');
                    first = false;
                    Write(output, entry);
                }
                output.Append(']');
                return;
            }
            output.Append('{');
            bool firstProperty = true;
            PropertyInfo[] properties = type.Assembly == typeof(InventoryReproductionJson).Assembly &&
                type.IsDefined(typeof(CompilerGeneratedAttribute), false) && type.Name.Contains("AnonymousType")
                ? type.GetProperties(BindingFlags.Instance | BindingFlags.Public)
                : InputProperties(type);
            foreach (PropertyInfo property in properties.OrderBy(p => p.Name, StringComparer.Ordinal))
            {
                if (property.GetIndexParameters().Length != 0 || property.GetMethod == null) continue;
                if (!firstProperty) output.Append(',');
                firstProperty = false;
                Quote(output, property.Name);
                output.Append(':');
                // User-authored preset labels have no role in arrangement scoring.
                Write(output, value is NativePresetSnapshot && property.Name == "Name"
                    ? string.Empty : property.GetValue(value));
            }
            output.Append('}');
        }

        private static void Quote(StringBuilder output, string value)
        {
            output.Append('"');
            foreach (char character in value)
            {
                if (character == '"' || character == '\\') output.Append('\\').Append(character);
                else if (character < 32 || char.IsSurrogate(character))
                    output.Append("\\u").Append(((int)character).ToString("x4", CultureInfo.InvariantCulture));
                else output.Append(character);
            }
            output.Append('"');
        }
    }
}
