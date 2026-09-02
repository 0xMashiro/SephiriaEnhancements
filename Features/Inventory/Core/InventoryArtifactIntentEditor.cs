#nullable disable
using System;
using SephiriaEnhancements.Runtime.Inventory;
using System.Collections.Generic;
using System.Linq;

namespace SephiriaEnhancements.Inventory
{
    internal static class InventoryArtifactIntentEditor
    {
        internal static bool IsMarked(
            InventoryOptimizationPreferences preferences, InventoryItemKey itemKey) =>
            preferences?.ArtifactPreferences.Any(rule =>
                rule.TargetsInstance && rule.ItemKey == itemKey &&
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
                .ThenBy(rule => rule.EntityId).ThenBy(rule => rule.InstanceId).ToArray() ??
            Array.Empty<ArtifactOptimizationPreference>();

        internal static ArtifactOptimizationPreference[] AvoidedInstances(
            InventoryOptimizationPreferences preferences) =>
            preferences?.ArtifactPreferences.Where(rule =>
                rule.TargetsInstance &&
                rule.Level == InventoryPreferenceLevel.Avoid)
                .OrderBy(rule => rule.IntentSlotIndex).ToArray() ??
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
            var itemKey = new InventoryItemKey(entityId, instanceId);

            return IsMarked(preferences, itemKey)
                ? Remove(preferences, itemKey)
                : PlacePriority(preferences, instanceId, entityId,
                    FirstEmptySlot(preferences, InventoryPreferenceLevel.Priority));
        }

        internal static InventoryOptimizationPreferences PlacePriority(
            InventoryOptimizationPreferences preferences, int instanceId,
            int entityId, int index) => Place(preferences, instanceId, entityId,
                InventoryPreferenceLevel.Priority, index);

        internal static InventoryOptimizationPreferences PlaceAvoid(
            InventoryOptimizationPreferences preferences, int instanceId,
            int entityId, int index) => Place(preferences, instanceId, entityId,
                InventoryPreferenceLevel.Avoid, index);

        internal static int SlotCount(IEnumerable<ArtifactOptimizationPreference> rules) =>
            rules.Select(rule => rule.IntentSlotIndex).DefaultIfEmpty(-1).Max() + 1;

        internal static InventoryOptimizationPreferences SetMinimumEffectiveLevel(
            InventoryOptimizationPreferences preferences, InventorySnapshot snapshot,
            InventoryItemKey itemKey, int minimumEffectiveLevel)
        {
            preferences ??= InventoryOptimizationPreferences.Default;
            ArtifactOptimizationPreference source = preferences.ArtifactPreferences
                .FirstOrDefault(rule => rule.TargetsInstance && rule.ItemKey == itemKey &&
                    rule.Level == InventoryPreferenceLevel.Priority);
            InventoryItemSnapshot item = snapshot?.Items.FirstOrDefault(candidate => candidate.ItemKey == itemKey);
            if (source == null || item?.Artifact == null)
            {
                return preferences;
            }
            int level = Math.Max(0, Math.Min(item.Artifact.MaxLevel, minimumEffectiveLevel));
            if (level == source.MinimumEffectiveLevel)
            {
                return preferences;
            }
            return ReplaceArtifacts(preferences, preferences.ArtifactPreferences.Select(rule =>
                ReferenceEquals(rule, source)
                    ? new ArtifactOptimizationPreference(source.InstanceId, source.EntityId,
                        source.Level, level, source.IntentSlotIndex)
                    : rule).ToArray());
        }

        private static int FirstEmptySlot(InventoryOptimizationPreferences preferences,
            InventoryPreferenceLevel level)
        {
            var occupied = new HashSet<int>(preferences.ArtifactPreferences
                .Where(rule => rule.TargetsInstance && rule.Level == level)
                .Select(rule => rule.IntentSlotIndex));
            int index = 0;
            while (occupied.Contains(index))
            {
                index++;
            }
            return index;
        }

        private static InventoryOptimizationPreferences Place(
            InventoryOptimizationPreferences preferences, int instanceId,
            int entityId, InventoryPreferenceLevel level, int index)
        {
            preferences ??= InventoryOptimizationPreferences.Default;
            if (instanceId < 0 || entityId < 0 || index < 0)
            {
                return preferences;
            }
            var itemKey = new InventoryItemKey(entityId, instanceId);
            ArtifactOptimizationPreference source = preferences.ArtifactPreferences
                .FirstOrDefault(rule => rule.TargetsInstance && rule.ItemKey == itemKey);
            ArtifactOptimizationPreference destination = preferences.ArtifactPreferences
                .FirstOrDefault(rule => rule.TargetsInstance && rule.Level == level &&
                    rule.IntentSlotIndex == index);
            if (source != null && ReferenceEquals(source, destination))
            {
                return preferences;
            }
            var rules = preferences.ArtifactPreferences
                .Where(rule => !ReferenceEquals(rule, source) &&
                    !ReferenceEquals(rule, destination)).ToList();
            // A board move swaps occupied slots. A new inventory reference
            // replaces the destination mark without changing either artifact.
            if (source != null && destination != null)
            {
                rules.Add(AtSlot(destination, source.Level, source.IntentSlotIndex));
            }
            rules.Add(new ArtifactOptimizationPreference(instanceId, entityId, level,
                source?.Level == InventoryPreferenceLevel.Priority
                    ? source.MinimumEffectiveLevel : 0, index));
            return ReplaceArtifacts(preferences, rules.ToArray());
        }

        private static ArtifactOptimizationPreference AtSlot(
            ArtifactOptimizationPreference rule, InventoryPreferenceLevel level,
            int index) => new(rule.InstanceId, rule.EntityId, level,
                rule.Level == InventoryPreferenceLevel.Priority
                    ? rule.MinimumEffectiveLevel : 0, index);

        internal static InventoryOptimizationPreferences Remove(
            InventoryOptimizationPreferences preferences, InventoryItemKey itemKey)
        {
            preferences ??= InventoryOptimizationPreferences.Default;
            ArtifactOptimizationPreference[] rules = preferences.
                ArtifactPreferences.Where(rule => !rule.TargetsInstance ||
                    rule.ItemKey != itemKey).ToArray();
            return rules.Length == preferences.ArtifactPreferences.Count
                ? preferences
                : ReplaceArtifacts(preferences, rules);
        }

        internal static InventoryOptimizationPreferences Prune(
            InventoryOptimizationPreferences preferences,
            IEnumerable<InventoryItemKey> validItemKeys)
        {
            preferences ??= InventoryOptimizationPreferences.Default;
            var valid = new HashSet<InventoryItemKey>(validItemKeys ?? Array.Empty<InventoryItemKey>());
            ArtifactOptimizationPreference[] rules = preferences.
                ArtifactPreferences.Where(rule => !rule.TargetsInstance ||
                    valid.Contains(rule.ItemKey)).ToArray();
            return rules.Length == preferences.ArtifactPreferences.Count
                ? preferences
                : ReplaceArtifacts(preferences, rules);
        }

        private static InventoryOptimizationPreferences ReplaceArtifacts(
            InventoryOptimizationPreferences preferences,
            ArtifactOptimizationPreference[] rules) => new(
                preferences.SearchEffort,
                preferences.AllowStoneTabletRotation, rules,
                preferences.ComboPreferences.ToArray());
    }
}
