#nullable disable
using SephiriaEnhancements.Runtime.Inventory;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace SephiriaEnhancements.Inventory
{
    internal static class InventoryOptimizationPreferencesCodec
    {
        private const string Version = "v4";

        internal static string Encode(
            InventoryOptimizationPreferences preferences)
        {
            preferences ??= InventoryOptimizationPreferences.Default;
            var lines = new List<string> { Version };
            foreach (ComboOptimizationPreference rule in preferences.
                ComboPreferences.OrderBy(rule => rule.CategoryId,
                    StringComparer.Ordinal))
            {
                lines.Add(string.Join("|", "C",
                    Uri.EscapeDataString(rule.CategoryId),
                    ((int)rule.Level).ToString(CultureInfo.InvariantCulture),
                    rule.TargetCount.ToString(CultureInfo.InvariantCulture),
                    ((int)rule.Strength).ToString(CultureInfo.InvariantCulture)));
            }
            return string.Join("\n", lines);
        }

        internal static bool TryDecode(string payload,
            InventorySearchEffort searchEffort,
            bool allowStoneTabletRotation,
            out InventoryOptimizationPreferences preferences)
        {
            preferences = InventoryOptimizationPreferences.Default.
                WithExecutionSettings(searchEffort,
                    allowStoneTabletRotation);
            if (string.IsNullOrWhiteSpace(payload))
            {
                return false;
            }

            string[] lines = payload.Replace("\r\n", "\n").
                Replace('\r', '\n').Split('\n');
            if (lines.Length == 0 || !string.Equals(lines[0], Version,
                    StringComparison.Ordinal))
            {
                return false;
            }

            var combos = new Dictionary<string,
                ComboOptimizationPreference>(StringComparer.Ordinal);
            for (int index = 1; index < lines.Length; index++)
            {
                if (string.IsNullOrWhiteSpace(lines[index]))
                {
                    continue;
                }
                string[] fields = lines[index].Split('|');
                if (fields.Length != 5 ||
                    !int.TryParse(fields[2], NumberStyles.Integer,
                        CultureInfo.InvariantCulture, out int levelValue) ||
                    !Enum.IsDefined(typeof(InventoryPreferenceLevel),
                        levelValue) ||
                    !int.TryParse(fields[3], NumberStyles.Integer,
                        CultureInfo.InvariantCulture, out int requiredValue))
                {
                    return false;
                }

                var level = (InventoryPreferenceLevel)levelValue;
                if (!int.TryParse(fields[4], NumberStyles.Integer, CultureInfo.InvariantCulture, out int strength) ||
                    !Enum.IsDefined(typeof(InventoryConstraintStrength), strength)) return false;
                if (fields[0] != "C" || requiredValue < 0)
                {
                    return false;
                }

                string categoryId;
                try
                {
                    categoryId = Uri.UnescapeDataString(fields[1]);
                }
                catch (UriFormatException)
                {
                    return false;
                }
                if (string.IsNullOrEmpty(categoryId))
                {
                    return false;
                }
                combos[categoryId] = new ComboOptimizationPreference(
                    categoryId, level, requiredValue, (InventoryConstraintStrength)strength);
            }

            preferences = new InventoryOptimizationPreferences(searchEffort,
                allowStoneTabletRotation, Array.Empty<ArtifactOptimizationPreference>(), combos.Values.OrderBy(rule =>
                        rule.CategoryId, StringComparer.Ordinal).ToArray());
            return true;
        }
    }
}
