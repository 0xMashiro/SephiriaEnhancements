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
        private const string Version = "v1";

        internal static string Encode(
            InventoryOptimizationPreferences preferences)
        {
            preferences ??= InventoryOptimizationPreferences.Default;
            var lines = new List<string> { Version };
            foreach (ArtifactOptimizationPreference rule in preferences.
                ArtifactPreferences.Where(rule => !rule.TargetsInstance)
                    .OrderBy(rule => rule.EntityId))
            {
                lines.Add(string.Join("|", "A",
                    rule.EntityId.ToString(CultureInfo.InvariantCulture),
                    ((int)rule.Level).ToString(CultureInfo.InvariantCulture),
                    rule.MinimumEffectiveLevel.ToString(
                        CultureInfo.InvariantCulture)));
            }
            foreach (ComboOptimizationPreference rule in preferences.
                ComboPreferences.OrderBy(rule => rule.CategoryId,
                    StringComparer.Ordinal))
            {
                lines.Add(string.Join("|", "C",
                    Uri.EscapeDataString(rule.CategoryId),
                    ((int)rule.Level).ToString(CultureInfo.InvariantCulture),
                    rule.MinimumCount.ToString(CultureInfo.InvariantCulture)));
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

            var artifacts = new Dictionary<int,
                ArtifactOptimizationPreference>();
            var combos = new Dictionary<string,
                ComboOptimizationPreference>(StringComparer.Ordinal);
            for (int index = 1; index < lines.Length; index++)
            {
                if (string.IsNullOrWhiteSpace(lines[index]))
                {
                    continue;
                }
                string[] fields = lines[index].Split('|');
                if (fields.Length != 4 ||
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
                if (fields[0] == "A")
                {
                    if (!int.TryParse(fields[1], NumberStyles.Integer,
                            CultureInfo.InvariantCulture, out int entityId) ||
                        entityId < 0 || requiredValue < 0)
                    {
                        return false;
                    }
                    artifacts[entityId] = new ArtifactOptimizationPreference(
                        -1, entityId, level, requiredValue);
                    continue;
                }
                if (fields[0] != "C" || requiredValue < 1)
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
                    categoryId, level, requiredValue);
            }

            preferences = new InventoryOptimizationPreferences(searchEffort,
                allowStoneTabletRotation, artifacts.Values.OrderBy(rule =>
                    rule.EntityId).ToArray(), combos.Values.OrderBy(rule =>
                        rule.CategoryId, StringComparer.Ordinal).ToArray());
            return true;
        }
    }
}
