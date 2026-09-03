#nullable disable
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;

namespace SephiriaEnhancements.Runtime.GameBridge.Inventory
{
    // Reflection keeps optional artifact implementations out of the adapter's
    // signatures. Missing/changed members are reported by the capture boundary.
    internal static class NativeInventoryEffectAccess
    {
        private const BindingFlags Members = BindingFlags.Public | BindingFlags.NonPublic |
            BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;
        private static readonly Dictionary<MethodInfo, Instruction[]> Bodies = new();

        internal static IEnumerable<Type> Hierarchy(Type type)
        {
            for (; type != null; type = type.BaseType) yield return type;
        }

        internal static object Read(object instance, string name)
        {
            Type type = instance as Type ?? instance.GetType();
            FieldInfo field = Hierarchy(type).Select(owner => owner.GetField(name, Members)).FirstOrDefault(value => value != null);
            if (field == null) throw new MissingFieldException(type.FullName, name);
            return field.GetValue(field.IsStatic ? null : instance);
        }

        internal static T Read<T>(object instance, string name)
        {
            object value = Read(instance, name);
            if (value is T typed) return typed;
            throw new InvalidOperationException("Unexpected field type: " + instance.GetType().Name + "." + name);
        }

        internal static int Integer(object instance, string name) => Convert.ToInt32(Read(instance, name));
        internal static double[] Curve(object instance, string name)
        {
            object field = Read(instance, name);
            if (!(field is int[]) && !(field is float[]))
                throw new InvalidOperationException("Unexpected curve type: " + name);
            var values = ((IEnumerable)field).Cast<object>().Select(Convert.ToDouble).ToArray();
            if (values.Length == 0 || values.Any(value => double.IsNaN(value) || double.IsInfinity(value)))
                throw new InvalidOperationException("Invalid curve: " + name);
            return values;
        }

        internal static MethodInfo Method(Type type, string name) => Hierarchy(type)
            .SelectMany(owner => owner.GetMethods(Members)).FirstOrDefault(method => method.Name == name) ??
            throw new MissingMethodException(type.FullName, name);

        internal static object Invoke(object instance, string name, params object[] arguments) =>
            Method(instance.GetType(), name).Invoke(instance, arguments);

        internal sealed class Instruction
        {
            internal Instruction(OpCode code, object operand) { Code = code; Operand = operand; }
            internal OpCode Code { get; }
            internal object Operand { get; }
            internal int? Integer => Code == OpCodes.Ldc_I4_M1 ? -1 :
                Code.Value >= OpCodes.Ldc_I4_0.Value && Code.Value <= OpCodes.Ldc_I4_8.Value
                    ? Code.Value - OpCodes.Ldc_I4_0.Value :
                Code == OpCodes.Ldc_I4 || Code == OpCodes.Ldc_I4_S ? (int?)Convert.ToInt32(Operand) : null;
        }

        internal static Instruction[] Instructions(MethodInfo method)
        {
            if (Bodies.TryGetValue(method, out var cached)) return cached;
            if (method.GetMethodBody() == null)
                throw new InvalidOperationException("Method body unavailable: " + method.Name);
            return Bodies[method] = PatchProcessor.ReadMethodBody(method)
                .Select(instruction => new Instruction(instruction.Key, instruction.Value)).ToArray();
        }

        internal static int CoordinateOffset(Type type, string method, string coordinate)
        {
            var body = Instructions(Method(type, method));
            int[] reads = Enumerable.Range(0, body.Length).Where(index =>
                body[index].Code == OpCodes.Ldfld && body[index].Operand is FieldInfo field && field.Name == coordinate).ToArray();
            if (reads.Length != 1) throw new InvalidOperationException("Ambiguous target coordinate: " + method);
            int at = reads[0];
            int? value = body.ElementAtOrDefault(at + 1)?.Integer;
            if (value == null)
            {
                if (body.ElementAtOrDefault(at + 1)?.Code == OpCodes.Newobj &&
                    body[at + 1].Operand is ConstructorInfo constructor && constructor.DeclaringType?.Name == "ItemPosition")
                    return 0;
                throw new InvalidOperationException("Changed target coordinate: " + method);
            }
            OpCode operation = body[at + 2].Code;
            if (operation == OpCodes.Add) return value.Value;
            if (operation == OpCodes.Sub) return -value.Value;
            throw new InvalidOperationException("Changed target coordinate: " + method);
        }

        internal static int HalfBoardBoundary(Type type, string method)
        {
            var body = Instructions(Method(type, method));
            var matches = Enumerable.Range(0, Math.Max(0, body.Length - 6)).Where(index =>
                body[index].Operand is FieldInfo field && field.Name == "XIdx" &&
                body[index + 1].Integer.HasValue &&
                (body[index + 2].Code == OpCodes.Bgt || body[index + 2].Code == OpCodes.Bgt_S) &&
                body[index + 3].Integer == 1 && body[index + 6].Integer == 0).ToArray();
            if (matches.Length != 1) throw new InvalidOperationException("Changed half-board comparison: " + method);
            return body[matches[0] + 1].Integer.Value;
        }

        internal static int SlotCount(Type type, string method)
        {
            var body = Instructions(Method(type, method));
            var bounds = Enumerable.Range(1, body.Length - 1).Where(index =>
                (body[index].Code == OpCodes.Blt || body[index].Code == OpCodes.Blt_S) &&
                body[index - 1].Integer.HasValue).Select(index => body[index - 1].Integer.Value).ToArray();
            if (bounds.Length != 1 || bounds[0] <= 0) throw new InvalidOperationException("Changed slot range: " + method);
            return bounds[0];
        }

        internal static string[] StringConstants(Type type, string method) => Instructions(Method(type, method))
            .Where(instruction => instruction.Code == OpCodes.Ldstr).Select(instruction => (string)instruction.Operand).Distinct().ToArray();

        internal static string[] StatChannels(Type type, string method)
        {
            var body = Instructions(Method(type, method));
            Type statType = body.Select(instruction => instruction.Operand).OfType<MethodInfo>()
                .Where(called => called.Name == "AddCustomStat").Select(called => called.GetParameters()[0].ParameterType)
                .Distinct().Single();
            if (!statType.IsEnum) throw new InvalidOperationException("Changed stat identifier: " + method);
            return Enumerable.Range(1, body.Length - 1).Where(index => body[index].Integer.HasValue &&
                body[index - 1].Operand is MethodInfo getter && getter.Name == "get_NetworkAvatar")
                .Select(index => Enum.GetName(statType, body[index].Integer.Value) ??
                    throw new InvalidOperationException("Unknown stat identifier")).Distinct().ToArray();
        }

        internal static string[] StatStringChannels(Type type, string method)
        {
            var body = Instructions(Method(type, method));
            return Enumerable.Range(1, body.Length - 1).Where(index => body[index].Code == OpCodes.Ldstr &&
                body[index - 1].Operand is MethodInfo getter && getter.Name == "get_NetworkAvatar")
                .Select(index => (string)body[index].Operand).Distinct().ToArray();
        }

        internal static string[] RowStatChannels(Type type, IReadOnlyList<string> categories)
        {
            if (categories == null || categories.Count == 0)
                throw new InvalidOperationException("Row category cycle is empty");
            var body = Instructions(Method(type, "SearchCategory"));
            bool rowModulo = Enumerable.Range(0, Math.Max(0, body.Length - 5)).Any(index =>
                body[index].Operand is FieldInfo coordinate && coordinate.Name == "YIdx" &&
                body[index + 1].Code == OpCodes.Ldarg_0 &&
                body[index + 2].Operand is FieldInfo cycle && cycle.Name == "lineCategory" &&
                body[index + 3].Code == OpCodes.Ldlen && body[index + 4].Code == OpCodes.Conv_I4 &&
                body[index + 5].Code == OpCodes.Rem);
            if (!rowModulo) throw new InvalidOperationException("Changed row category selection");
            var channels = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (int index in Enumerable.Range(0, body.Length).Where(index =>
                         body[index].Code == OpCodes.Stfld && body[index].Operand is FieldInfo field && field.Name == "addedStat"))
            {
                // assignedCategory == literal; false branch skips the enum-field assignment.
                if (index < 7 || body[index - 7].Code != OpCodes.Ldarg_0 ||
                    !(body[index - 6].Operand is FieldInfo category) || category.Name != "assignedCategory" ||
                    body[index - 5].Code != OpCodes.Ldstr ||
                    !(body[index - 4].Operand is MethodInfo comparison) || comparison.DeclaringType != typeof(string) ||
                    comparison.Name != "op_Equality" ||
                    (body[index - 3].Code != OpCodes.Brfalse && body[index - 3].Code != OpCodes.Brfalse_S) ||
                    body[index - 2].Code != OpCodes.Ldarg_0 || !body[index - 1].Integer.HasValue)
                    throw new InvalidOperationException("Changed row category stat mapping");
                var stat = (FieldInfo)body[index].Operand;
                string channel = stat.FieldType.IsEnum ? Enum.GetName(stat.FieldType, body[index - 1].Integer.Value) : null;
                if (channel == null || !channels.TryAdd((string)body[index - 5].Operand, channel))
                    throw new InvalidOperationException("Ambiguous row category stat mapping");
            }
            return categories.Select(category => category != null && channels.TryGetValue(category, out string channel)
                ? channel : throw new InvalidOperationException("Unknown row category stat: " + category)).ToArray();
        }
    }
}
