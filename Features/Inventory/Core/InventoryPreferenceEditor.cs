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
        Prefer,
        Core,
        Priority,
        Avoid,
        Ignored
    }

    internal sealed class InventoryPreferenceEditorTarget
    {
        internal InventoryPreferenceEditorTarget(
            InventoryOptimizationTargetKind kind, string target,
            string displayName, int entityId, string categoryId,
            InventoryPreferenceChoice choice, int requiredValue,
            int maximumValue)
        {
            Kind = kind;
            Target = target ?? string.Empty;
            DisplayName = displayName ?? string.Empty;
            EntityId = entityId;
            CategoryId = categoryId ?? string.Empty;
            Choice = choice;
            RequiredValue = Math.Max(kind ==
                InventoryOptimizationTargetKind.ComboCategory ? 1 : 0,
                requiredValue);
            MaximumValue = Math.Max(RequiredValue, maximumValue);
        }

        internal InventoryOptimizationTargetKind Kind { get; }
        internal string Target { get; }
        internal string DisplayName { get; }
        internal int EntityId { get; }
        internal string CategoryId { get; }
        internal InventoryPreferenceChoice Choice { get; }
        internal int RequiredValue { get; }
        internal int MaximumValue { get; }
        internal bool CanAdjustRequiredValue =>
            Choice == InventoryPreferenceChoice.Prefer ||
            Choice == InventoryPreferenceChoice.Core ||
            Choice == InventoryPreferenceChoice.Priority ||
            Kind == InventoryOptimizationTargetKind.ComboCategory &&
                Choice == InventoryPreferenceChoice.Avoid;
    }

    internal static class InventoryPreferenceEditor
    {
        internal static InventoryPreferenceChoice NextChoice(
            InventoryPreferenceChoice choice) => choice switch
            {
                InventoryPreferenceChoice.Automatic =>
                    InventoryPreferenceChoice.Prefer,
                InventoryPreferenceChoice.Prefer =>
                    InventoryPreferenceChoice.Core,
                InventoryPreferenceChoice.Core =>
                    InventoryPreferenceChoice.Priority,
                InventoryPreferenceChoice.Priority =>
                    InventoryPreferenceChoice.Avoid,
                InventoryPreferenceChoice.Avoid =>
                    InventoryPreferenceChoice.Ignored,
                _ => InventoryPreferenceChoice.Automatic
            };

        internal static IReadOnlyList<InventoryPreferenceEditorTarget>
            BuildTargets(InventorySnapshot snapshot,
                InventoryOptimizationPreferences preferences,
                InventoryOptimizationTargetKind kind)
        {
            if (snapshot == null)
            {
                return Array.Empty<InventoryPreferenceEditorTarget>();
            }
            preferences ??= InventoryOptimizationPreferences.Default;
            return kind == InventoryOptimizationTargetKind.Artifact
                ? BuildArtifactTargets(snapshot, preferences)
                : BuildComboTargets(snapshot, preferences);
        }

        internal static InventoryOptimizationPreferences SetChoice(
            InventoryOptimizationPreferences preferences,
            InventoryPreferenceEditorTarget target,
            InventoryPreferenceChoice choice)
        {
            preferences ??= InventoryOptimizationPreferences.Default;
            if (target == null)
            {
                return preferences;
            }

            return target.Kind == InventoryOptimizationTargetKind.Artifact
                ? SetArtifactChoice(preferences, target, choice)
                : SetComboChoice(preferences, target, choice);
        }

        internal static InventoryOptimizationPreferences SetRequiredValue(
            InventoryOptimizationPreferences preferences,
            InventoryPreferenceEditorTarget target, int requiredValue)
        {
            preferences ??= InventoryOptimizationPreferences.Default;
            if (target?.CanAdjustRequiredValue != true)
            {
                return preferences;
            }

            int value = Math.Max(target.Kind ==
                InventoryOptimizationTargetKind.ComboCategory ? 1 : 0,
                Math.Min(target.MaximumValue, requiredValue));
            return target.Kind == InventoryOptimizationTargetKind.Artifact
                ? ReplaceArtifact(preferences, target.EntityId,
                    ToLevel(target.Choice), value)
                : ReplaceCombo(preferences, target.CategoryId,
                    ToLevel(target.Choice), value);
        }

        private static IReadOnlyList<InventoryPreferenceEditorTarget>
            BuildArtifactTargets(InventorySnapshot snapshot,
                InventoryOptimizationPreferences preferences)
        {
            var explicitRules = preferences.ArtifactPreferences
                .Where(rule => !rule.TargetsInstance)
                .GroupBy(rule => rule.EntityId)
                .ToDictionary(group => group.Key, group => group.Last());
            var result = new List<InventoryPreferenceEditorTarget>();
            foreach (IGrouping<int, InventoryItemSnapshot> group in
                snapshot.Items.Where(item => item.Artifact != null)
                    .GroupBy(item => item.EntityId))
            {
                InventoryItemSnapshot first = group.First();
                explicitRules.TryGetValue(group.Key,
                    out ArtifactOptimizationPreference rule);
                int maximum = group.Max(item => item.Artifact.MaxLevel);
                result.Add(new InventoryPreferenceEditorTarget(
                    InventoryOptimizationTargetKind.Artifact,
                    "ArtifactEntity:" + group.Key,
                    string.IsNullOrEmpty(first.Name)
                        ? "#" + group.Key
                        : first.Name,
                    group.Key, string.Empty,
                    rule == null
                        ? InventoryPreferenceChoice.Automatic
                        : FromLevel(rule.Level),
                    rule?.MinimumEffectiveLevel ?? 1,
                    maximum));
            }
            return result;
        }

        private static IReadOnlyList<InventoryPreferenceEditorTarget>
            BuildComboTargets(InventorySnapshot snapshot,
                InventoryOptimizationPreferences preferences)
        {
            var explicitRules = preferences.ComboPreferences
                .GroupBy(rule => rule.CategoryId, StringComparer.Ordinal)
                .ToDictionary(group => group.Key, group => group.Last(),
                    StringComparer.Ordinal);
            var result = new List<InventoryPreferenceEditorTarget>();
            foreach (ComboCategorySnapshot category in snapshot.ComboCategories)
            {
                explicitRules.TryGetValue(category.CategoryId,
                    out ComboOptimizationPreference rule);
                int maximum = Math.Max(1, Math.Max(category.CurrentCount,
                    category.HighestComboCount));
                if (category.SetThresholds.Count != 0)
                {
                    maximum = Math.Max(maximum,
                        category.SetThresholds.Max());
                }
                if (category.ComboThresholds.Count != 0)
                {
                    maximum = Math.Max(maximum,
                        category.ComboThresholds.Max());
                }
                result.Add(new InventoryPreferenceEditorTarget(
                    InventoryOptimizationTargetKind.ComboCategory,
                    "Combo:" + category.CategoryId,
                    category.CategoryId, -1, category.CategoryId,
                    rule == null
                        ? InventoryPreferenceChoice.Automatic
                        : FromLevel(rule.Level),
                    rule?.MinimumCount ?? 1, maximum));
            }
            return result;
        }

        private static InventoryOptimizationPreferences SetArtifactChoice(
            InventoryOptimizationPreferences preferences,
            InventoryPreferenceEditorTarget target,
            InventoryPreferenceChoice choice)
        {
            if (choice == InventoryPreferenceChoice.Automatic)
            {
                return RemoveArtifact(preferences, target.EntityId);
            }
            int requiredValue = choice == InventoryPreferenceChoice.Avoid
                ? 0
                : Math.Max(0, target.RequiredValue);
            return ReplaceArtifact(preferences, target.EntityId,
                ToLevel(choice), requiredValue);
        }

        private static InventoryOptimizationPreferences SetComboChoice(
            InventoryOptimizationPreferences preferences,
            InventoryPreferenceEditorTarget target,
            InventoryPreferenceChoice choice)
        {
            if (choice == InventoryPreferenceChoice.Automatic)
            {
                return RemoveCombo(preferences, target.CategoryId);
            }
            return ReplaceCombo(preferences, target.CategoryId,
                ToLevel(choice), Math.Max(1, target.RequiredValue));
        }

        private static InventoryOptimizationPreferences ReplaceArtifact(
            InventoryOptimizationPreferences preferences, int entityId,
            InventoryPreferenceLevel level, int requiredValue)
        {
            ArtifactOptimizationPreference[] rules = preferences.
                ArtifactPreferences.Where(rule => rule.TargetsInstance ||
                    rule.EntityId != entityId)
                .Append(new ArtifactOptimizationPreference(-1, entityId,
                    level, requiredValue)).ToArray();
            return new InventoryOptimizationPreferences(
                preferences.SearchEffort,
                preferences.AllowStoneTabletRotation, rules,
                preferences.ComboPreferences.ToArray());
        }

        private static InventoryOptimizationPreferences RemoveArtifact(
            InventoryOptimizationPreferences preferences, int entityId) =>
            new(preferences.SearchEffort,
                preferences.AllowStoneTabletRotation,
                preferences.ArtifactPreferences.Where(rule =>
                    rule.TargetsInstance || rule.EntityId != entityId).
                    ToArray(),
                preferences.ComboPreferences.ToArray());

        private static InventoryOptimizationPreferences ReplaceCombo(
            InventoryOptimizationPreferences preferences, string categoryId,
            InventoryPreferenceLevel level, int requiredValue)
        {
            ComboOptimizationPreference[] rules = preferences.ComboPreferences
                .Where(rule => !string.Equals(rule.CategoryId, categoryId,
                    StringComparison.Ordinal))
                .Append(new ComboOptimizationPreference(categoryId, level,
                    requiredValue)).ToArray();
            return new InventoryOptimizationPreferences(
                preferences.SearchEffort,
                preferences.AllowStoneTabletRotation,
                preferences.ArtifactPreferences.ToArray(), rules);
        }

        private static InventoryOptimizationPreferences RemoveCombo(
            InventoryOptimizationPreferences preferences, string categoryId) =>
            new(preferences.SearchEffort,
                preferences.AllowStoneTabletRotation,
                preferences.ArtifactPreferences.ToArray(),
                preferences.ComboPreferences.Where(rule => !string.Equals(
                    rule.CategoryId, categoryId, StringComparison.Ordinal)).
                    ToArray());

        private static InventoryPreferenceChoice FromLevel(
            InventoryPreferenceLevel level) => level switch
            {
                InventoryPreferenceLevel.Prefer =>
                    InventoryPreferenceChoice.Prefer,
                InventoryPreferenceLevel.Core =>
                    InventoryPreferenceChoice.Core,
                InventoryPreferenceLevel.Priority =>
                    InventoryPreferenceChoice.Priority,
                InventoryPreferenceLevel.Avoid =>
                    InventoryPreferenceChoice.Avoid,
                _ => InventoryPreferenceChoice.Ignored
            };

        private static InventoryPreferenceLevel ToLevel(
            InventoryPreferenceChoice choice) => choice switch
            {
                InventoryPreferenceChoice.Prefer =>
                    InventoryPreferenceLevel.Prefer,
                InventoryPreferenceChoice.Core =>
                    InventoryPreferenceLevel.Core,
                InventoryPreferenceChoice.Priority =>
                    InventoryPreferenceLevel.Priority,
                InventoryPreferenceChoice.Avoid =>
                    InventoryPreferenceLevel.Avoid,
                _ => InventoryPreferenceLevel.Neutral
            };
    }
}
