#nullable disable
using SephiriaEnhancements.Runtime.Inventory;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SephiriaEnhancements.Inventory
{
    internal enum InventoryPreferenceChoice
    {
        Automatic,
        Priority,
        Avoid
    }

    internal sealed class InventoryComboTarget
    {
        internal InventoryComboTarget(string categoryId, InventoryPreferenceChoice choice,
            int requiredValue, int maximumValue)
        {
            CategoryId = categoryId;
            Choice = choice;
            RequiredValue = Math.Max(0, requiredValue);
            MaximumValue = Math.Max(RequiredValue, maximumValue);
        }

        internal string CategoryId { get; }
        internal InventoryPreferenceChoice Choice { get; }
        internal int RequiredValue { get; }
        internal int MaximumValue { get; }
        internal bool CanAdjustRequiredValue => Choice != InventoryPreferenceChoice.Automatic;
    }

    internal static class InventoryComboTargetEditor
    {
        internal static InventoryPreferenceChoice NextChoice(InventoryPreferenceChoice choice) => choice switch
        {
            InventoryPreferenceChoice.Automatic => InventoryPreferenceChoice.Priority,
            InventoryPreferenceChoice.Priority => InventoryPreferenceChoice.Avoid,
            _ => InventoryPreferenceChoice.Automatic
        };

        internal static IReadOnlyList<InventoryComboTarget> BuildTargets(InventorySnapshot snapshot,
            InventoryOptimizationPreferences preferences)
        {
            if (snapshot == null) return Array.Empty<InventoryComboTarget>();
            preferences ??= InventoryOptimizationPreferences.Default;
            var rules = preferences.ComboPreferences.GroupBy(rule => rule.CategoryId, StringComparer.Ordinal)
                .ToDictionary(group => group.Key, group => group.Last(), StringComparer.Ordinal);
            return snapshot.ComboCategories.Select(category =>
            {
                rules.TryGetValue(category.CategoryId, out var rule);
                int maximum = Math.Max(1, Math.Max(category.CurrentCount, category.HighestComboCount));
                maximum = Math.Max(maximum, category.SetThresholds.DefaultIfEmpty(0).Max());
                maximum = Math.Max(maximum, category.ComboThresholds.DefaultIfEmpty(0).Max());
                return new InventoryComboTarget(category.CategoryId,
                    rule == null ? InventoryPreferenceChoice.Automatic
                        : rule.Level == InventoryPreferenceLevel.Priority
                            ? InventoryPreferenceChoice.Priority : InventoryPreferenceChoice.Avoid,
                    rule?.TargetCount ?? 0, maximum);
            }).ToArray();
        }

        internal static InventoryOptimizationPreferences SetChoice(InventoryOptimizationPreferences preferences,
            InventoryComboTarget target, InventoryPreferenceChoice choice)
        {
            preferences ??= InventoryOptimizationPreferences.Default;
            return target == null ? preferences : Replace(preferences, target, choice, target.RequiredValue);
        }

        internal static InventoryOptimizationPreferences SetRequiredValue(InventoryOptimizationPreferences preferences,
            InventoryComboTarget target, int requiredValue)
        {
            preferences ??= InventoryOptimizationPreferences.Default;
            return target?.CanAdjustRequiredValue != true ? preferences
                : Replace(preferences, target, target.Choice, Math.Max(0, Math.Min(target.MaximumValue, requiredValue)));
        }

        private static InventoryOptimizationPreferences Replace(InventoryOptimizationPreferences preferences,
            InventoryComboTarget target, InventoryPreferenceChoice choice, int value)
        {
            var rules = preferences.ComboPreferences.Where(rule =>
                !string.Equals(rule.CategoryId, target.CategoryId, StringComparison.Ordinal));
            if (choice != InventoryPreferenceChoice.Automatic)
            {
                rules = rules.Append(new ComboOptimizationPreference(target.CategoryId,
                    choice == InventoryPreferenceChoice.Priority ? InventoryPreferenceLevel.Priority : InventoryPreferenceLevel.Avoid,
                    value));
            }
            return new InventoryOptimizationPreferences(preferences.SearchEffort, preferences.AllowStoneTabletRotation,
                preferences.ArtifactPreferences.ToArray(), rules.ToArray());
        }
    }
}
