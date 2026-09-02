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
            int requiredValue, int maximumValue,
            InventoryConstraintStrength strength = InventoryConstraintStrength.Soft)
        {
            Strength = strength;
            CategoryId = categoryId;
            Choice = choice;
            RequiredValue = Math.Max(0, requiredValue);
            MaximumValue = Math.Max(RequiredValue, maximumValue);
        }

        internal string CategoryId { get; }
        internal InventoryConstraintStrength Strength { get; }
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
            var categories = snapshot.ComboCategories.ToDictionary(category => category.CategoryId, StringComparer.Ordinal);
            // A saved Hard rule must remain editable even when no item currently
            // supplies its category; otherwise the player cannot clear a conflict.
            return categories.Keys.Concat(rules.Keys).Distinct(StringComparer.Ordinal).Select(categoryId =>
            {
                rules.TryGetValue(categoryId, out var rule);
                categories.TryGetValue(categoryId, out var category);
                int maximum = Math.Max(1, Math.Max(category?.CurrentCount ?? 0, category?.HighestComboCount ?? 0));
                maximum = Math.Max(maximum, category?.SetThresholds.DefaultIfEmpty(0).Max() ?? 0);
                maximum = Math.Max(maximum, category?.ComboThresholds.DefaultIfEmpty(0).Max() ?? 0);
                return new InventoryComboTarget(categoryId,
                    rule == null ? InventoryPreferenceChoice.Automatic
                        : rule.Level == InventoryPreferenceLevel.Priority
                            ? InventoryPreferenceChoice.Priority : InventoryPreferenceChoice.Avoid,
                    rule?.TargetCount ?? 0, maximum, rule?.Strength ?? InventoryConstraintStrength.Soft);
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
                    value, target.Strength));
            }
            return new InventoryOptimizationPreferences(preferences.SearchEffort, preferences.AllowStoneTabletRotation,
                preferences.ArtifactPreferences.ToArray(), rules.ToArray());
        }

        internal static InventoryOptimizationPreferences SetStrength(InventoryOptimizationPreferences preferences,
            InventoryComboTarget target, InventoryConstraintStrength strength)
        {
            preferences ??= InventoryOptimizationPreferences.Default;
            return target?.CanAdjustRequiredValue != true ? preferences : Replace(preferences,
                new InventoryComboTarget(target.CategoryId, target.Choice, target.RequiredValue, target.MaximumValue, strength),
                target.Choice, target.RequiredValue);
        }
    }
}
