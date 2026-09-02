#nullable disable
using System;
using System.Collections.Generic;
using System.Linq;

namespace SephiriaEnhancements.Inventory
{
    internal static class InventoryArtifactIntentEditor
    {
        internal static bool IsMarked(
            InventoryOptimizationPreferences preferences, int instanceId) =>
            preferences?.ArtifactPreferences.Any(rule =>
                rule.TargetsInstance && rule.InstanceId == instanceId &&
                rule.Level == InventoryPreferenceLevel.Priority) == true;

        internal static int Count(
            InventoryOptimizationPreferences preferences) =>
            preferences?.ArtifactPreferences.Count(rule =>
                rule.TargetsInstance &&
                rule.Level == InventoryPreferenceLevel.Priority) ?? 0;

        internal static ArtifactOptimizationPreference[] OrderedPriorities(
            InventoryOptimizationPreferences preferences) =>
            preferences?.ArtifactPreferences.Where(rule =>
                    rule.TargetsInstance &&
                    rule.Level == InventoryPreferenceLevel.Priority)
                .OrderBy(rule => rule.PriorityOrder < 0
                    ? int.MaxValue
                    : rule.PriorityOrder)
                .ThenBy(rule => rule.InstanceId).ToArray() ??
            Array.Empty<ArtifactOptimizationPreference>();

        internal static ArtifactOptimizationPreference[] AvoidedInstances(
            InventoryOptimizationPreferences preferences) =>
            preferences?.ArtifactPreferences.Where(rule =>
                rule.TargetsInstance &&
                rule.Level == InventoryPreferenceLevel.Avoid).ToArray() ??
            Array.Empty<ArtifactOptimizationPreference>();

        internal static InventoryOptimizationPreferences Toggle(
            InventoryOptimizationPreferences preferences, int instanceId,
            int entityId)
        {
            preferences ??= InventoryOptimizationPreferences.Default;
            if (instanceId < 0 || entityId < 0)
            {
                return preferences;
            }

            bool remove = IsMarked(preferences, instanceId);
            ArtifactOptimizationPreference[] rules = preferences.
                ArtifactPreferences.Where(rule =>
                    !rule.TargetsInstance || rule.InstanceId != instanceId).
                ToArray();
            if (!remove)
            {
                int order = OrderedPriorities(preferences).Length;
                rules = rules.Append(new ArtifactOptimizationPreference(
                    instanceId, entityId, InventoryPreferenceLevel.Priority,
                    minimumEffectiveLevel: 1, priorityOrder: order)).ToArray();
            }
            return ReplaceArtifacts(preferences, NormalizePriorityOrder(rules));
        }

        internal static InventoryOptimizationPreferences PlacePriority(
            InventoryOptimizationPreferences preferences, int instanceId,
            int entityId, int index)
        {
            preferences ??= InventoryOptimizationPreferences.Default;
            if (instanceId < 0 || entityId < 0)
            {
                return preferences;
            }
            var ordered = OrderedPriorities(preferences).Where(rule =>
                rule.InstanceId != instanceId).ToList();
            ordered.Insert(Math.Clamp(index, 0, ordered.Count),
                new ArtifactOptimizationPreference(instanceId, entityId,
                    InventoryPreferenceLevel.Priority, 1));
            ArtifactOptimizationPreference[] other = preferences.
                ArtifactPreferences.Where(rule => !rule.TargetsInstance ||
                    rule.InstanceId != instanceId &&
                    rule.Level != InventoryPreferenceLevel.Priority).ToArray();
            ArtifactOptimizationPreference[] normalized = ordered.Select(
                (rule, priorityOrder) =>
                    new ArtifactOptimizationPreference(rule.InstanceId,
                        rule.EntityId, InventoryPreferenceLevel.Priority,
                        rule.MinimumEffectiveLevel, priorityOrder)).ToArray();
            return ReplaceArtifacts(preferences,
                other.Concat(normalized).ToArray());
        }

        internal static InventoryOptimizationPreferences PlaceAvoid(
            InventoryOptimizationPreferences preferences, int instanceId,
            int entityId)
        {
            preferences ??= InventoryOptimizationPreferences.Default;
            if (instanceId < 0 || entityId < 0)
            {
                return preferences;
            }
            ArtifactOptimizationPreference[] rules = preferences.
                ArtifactPreferences.Where(rule => !rule.TargetsInstance ||
                    rule.InstanceId != instanceId).Append(
                    new ArtifactOptimizationPreference(instanceId, entityId,
                        InventoryPreferenceLevel.Avoid)).ToArray();
            return ReplaceArtifacts(preferences,
                NormalizePriorityOrder(rules));
        }

        internal static InventoryOptimizationPreferences Remove(
            InventoryOptimizationPreferences preferences, int instanceId)
        {
            preferences ??= InventoryOptimizationPreferences.Default;
            ArtifactOptimizationPreference[] rules = preferences.
                ArtifactPreferences.Where(rule => !rule.TargetsInstance ||
                    rule.InstanceId != instanceId).ToArray();
            return rules.Length == preferences.ArtifactPreferences.Count
                ? preferences
                : ReplaceArtifacts(preferences,
                    NormalizePriorityOrder(rules));
        }

        internal static InventoryOptimizationPreferences Prune(
            InventoryOptimizationPreferences preferences,
            IEnumerable<int> validInstanceIds)
        {
            preferences ??= InventoryOptimizationPreferences.Default;
            var valid = new HashSet<int>(validInstanceIds ?? Array.Empty<int>());
            ArtifactOptimizationPreference[] rules = preferences.
                ArtifactPreferences.Where(rule => !rule.TargetsInstance ||
                    valid.Contains(rule.InstanceId)).ToArray();
            return rules.Length == preferences.ArtifactPreferences.Count
                ? preferences
                : ReplaceArtifacts(preferences,
                    NormalizePriorityOrder(rules));
        }

        private static ArtifactOptimizationPreference[] NormalizePriorityOrder(
            IEnumerable<ArtifactOptimizationPreference> rules)
        {
            ArtifactOptimizationPreference[] source = rules.ToArray();
            var ordered = source.Where(rule => rule.TargetsInstance &&
                    rule.Level == InventoryPreferenceLevel.Priority)
                .OrderBy(rule => rule.PriorityOrder < 0
                    ? int.MaxValue
                    : rule.PriorityOrder)
                .ThenBy(rule => rule.InstanceId).Select((rule, index) =>
                    new ArtifactOptimizationPreference(rule.InstanceId,
                        rule.EntityId, rule.Level,
                        rule.MinimumEffectiveLevel, index)).ToArray();
            return source.Where(rule => !rule.TargetsInstance ||
                    rule.Level != InventoryPreferenceLevel.Priority)
                .Concat(ordered).ToArray();
        }

        private static InventoryOptimizationPreferences ReplaceArtifacts(
            InventoryOptimizationPreferences preferences,
            ArtifactOptimizationPreference[] rules) => new(
                preferences.SearchEffort,
                preferences.AllowStoneTabletRotation, rules,
                preferences.ComboPreferences.ToArray());
    }
}
