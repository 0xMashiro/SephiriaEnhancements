#nullable disable
using SephiriaEnhancements.Runtime.Inventory;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace SephiriaEnhancements.Inventory
{
    internal enum InventoryOptimizationTendency
    {
        Automatic,
        Stable,
        Aggressive
    }

    internal static class InventoryOptimizationTendencyPolicy
    {
        internal static InventorySearchEffort GetSearchEffort(
            InventoryOptimizationTendency tendency) => tendency switch
            {
                InventoryOptimizationTendency.Stable =>
                    InventorySearchEffort.Fast,
                InventoryOptimizationTendency.Aggressive =>
                    InventorySearchEffort.Thorough,
                _ => InventorySearchEffort.Balanced
            };
    }

    internal enum InventorySearchEffort
    {
        Fast,
        Balanced,
        Thorough
    }

    internal enum InventoryPreferenceLevel
    {
        Avoid,
        Neutral,
        Prefer,
        Core,
        Priority
    }

    internal enum InventoryPreferenceSource
    {
        ManualInstance,
        UserEntityRule,
        UserCategoryRule,
        NativePreset
    }

    internal sealed class ArtifactOptimizationPreference
    {
        internal ArtifactOptimizationPreference(int instanceId, int entityId,
            InventoryPreferenceLevel level, int minimumEffectiveLevel = 1,
            int intentSlotIndex = -1)
        {
            InstanceId = instanceId;
            EntityId = entityId;
            Level = level;
            // Artifact Avoid means keeping the artifact effect disabled; the
            // level threshold has no role in that native enabled-state rule.
            MinimumEffectiveLevel = level == InventoryPreferenceLevel.Avoid
                ? 0
                : Math.Max(0, minimumEffectiveLevel);
            IntentSlotIndex = TargetsInstance
                ? Math.Max(-1, intentSlotIndex)
                : -1;
        }

        internal InventoryItemKey ItemKey => new(EntityId, InstanceId);
        internal int InstanceId { get; }
        internal int EntityId { get; }
        internal InventoryPreferenceLevel Level { get; }
        internal int MinimumEffectiveLevel { get; }
        internal int IntentSlotIndex { get; }
        internal int PriorityOrder => Level == InventoryPreferenceLevel.Priority
            ? IntentSlotIndex : -1;
        internal bool TargetsInstance => InstanceId >= 0;
    }

    internal sealed class ComboOptimizationPreference
    {
        internal ComboOptimizationPreference(string categoryId,
            InventoryPreferenceLevel level, int minimumCount = 1)
        {
            CategoryId = categoryId ?? string.Empty;
            Level = level;
            // A combo count of zero is vacuous for every preference tier,
            // including Avoid. Keep combo targets on the game's first
            // meaningful count while artifact level zero remains meaningful.
            MinimumCount = Math.Max(1, minimumCount);
        }

        internal string CategoryId { get; }
        internal InventoryPreferenceLevel Level { get; }
        internal int MinimumCount { get; }
    }

    internal sealed class InventoryOptimizationPreferences
    {
        internal static readonly InventoryOptimizationPreferences Default = new(
            InventorySearchEffort.Balanced,
            allowStoneTabletRotation: true,
            Array.Empty<ArtifactOptimizationPreference>(),
            Array.Empty<ComboOptimizationPreference>());

        internal InventoryOptimizationPreferences(
            InventorySearchEffort searchEffort, bool allowStoneTabletRotation,
            ArtifactOptimizationPreference[] artifactPreferences,
            ComboOptimizationPreference[] comboPreferences)
        {
            SearchEffort = searchEffort;
            AllowStoneTabletRotation = allowStoneTabletRotation;
            ArtifactPreferences = Array.AsReadOnly(artifactPreferences == null
                ? Array.Empty<ArtifactOptimizationPreference>()
                : (ArtifactOptimizationPreference[])artifactPreferences.Clone());
            ComboPreferences = Array.AsReadOnly(comboPreferences == null
                ? Array.Empty<ComboOptimizationPreference>()
                : (ComboOptimizationPreference[])comboPreferences.Clone());
        }

        internal InventorySearchEffort SearchEffort { get; }
        internal bool AllowStoneTabletRotation { get; }
        internal IReadOnlyList<ArtifactOptimizationPreference>
            ArtifactPreferences
        { get; }
        internal IReadOnlyList<ComboOptimizationPreference> ComboPreferences
        { get; }

        internal InventoryOptimizationPreferences WithExecutionSettings(
            InventorySearchEffort searchEffort,
            bool allowStoneTabletRotation) => new(searchEffort,
                allowStoneTabletRotation, ArtifactPreferences.ToArray(),
                ComboPreferences.ToArray());
    }

    internal static class PersistentInventoryOptimizationPolicyStore
    {
        private static InventoryOptimizationPreferences current =
            InventoryOptimizationPreferences.Default;

        internal static InventoryOptimizationPreferences Capture() => current;

        internal static InventoryOptimizationPreferences Capture(
            InventorySearchEffort searchEffort,
            bool allowStoneTabletRotation) => current.WithExecutionSettings(
                searchEffort, allowStoneTabletRotation);

        internal static void Replace(InventoryOptimizationPreferences preferences)
        {
            current = preferences ?? InventoryOptimizationPreferences.Default;
        }
    }

    internal static class ExplorationInventoryIntentStore
    {
        private static InventoryOptimizationPreferences current =
            InventoryOptimizationPreferences.Default;

        internal static InventoryOptimizationPreferences Capture() => current;

        internal static void Replace(InventoryOptimizationPreferences intent)
        {
            current = intent ?? InventoryOptimizationPreferences.Default;
        }

        internal static void Clear()
        {
            current = InventoryOptimizationPreferences.Default;
        }
    }

    internal static class InventoryOptimizationPreferenceComposer
    {
        internal static InventoryOptimizationPreferences Compose(
            InventoryOptimizationPreferences persistentPolicy,
            InventoryOptimizationPreferences explorationIntent,
            InventorySearchEffort searchEffort,
            bool allowStoneTabletRotation)
        {
            persistentPolicy ??= InventoryOptimizationPreferences.Default;
            explorationIntent ??= InventoryOptimizationPreferences.Default;

            ArtifactOptimizationPreference[] artifacts = persistentPolicy.
                ArtifactPreferences.Concat(explorationIntent.
                    ArtifactPreferences).GroupBy(ArtifactIdentity).
                Select(group => group.Last()).ToArray();
            ComboOptimizationPreference[] combos = persistentPolicy.
                ComboPreferences.Concat(explorationIntent.ComboPreferences).
                GroupBy(rule => rule.CategoryId, StringComparer.Ordinal).
                Select(group => group.Last()).ToArray();
            return new InventoryOptimizationPreferences(searchEffort,
                allowStoneTabletRotation, artifacts, combos);
        }

        private static string ArtifactIdentity(
            ArtifactOptimizationPreference preference) =>
            preference.TargetsInstance
                ? "Item:" + preference.ItemKey
                : "Entity:" + preference.EntityId;
    }

    internal sealed class ResolvedArtifactOptimizationRule
    {
        internal ResolvedArtifactOptimizationRule(int instanceId, int entityId,
            InventoryPreferenceLevel level, int minimumEffectiveLevel,
            InventoryPreferenceSource source, int priorityOrder = -1)
        {
            InstanceId = instanceId;
            EntityId = entityId;
            Level = level;
            MinimumEffectiveLevel = minimumEffectiveLevel;
            Source = source;
            PriorityOrder = priorityOrder;
        }

        internal InventoryItemKey ItemKey => new(EntityId, InstanceId);
        internal int InstanceId { get; }
        internal int EntityId { get; }
        internal InventoryPreferenceLevel Level { get; }
        internal int MinimumEffectiveLevel { get; }
        internal InventoryPreferenceSource Source { get; }
        internal int PriorityOrder { get; }
    }

    internal sealed class ResolvedComboOptimizationRule
    {
        internal ResolvedComboOptimizationRule(string categoryId,
            InventoryPreferenceLevel level, int minimumCount,
            InventoryPreferenceSource source)
        {
            CategoryId = categoryId;
            Level = level;
            MinimumCount = minimumCount;
            Source = source;
        }

        internal string CategoryId { get; }
        internal InventoryPreferenceLevel Level { get; }
        internal int MinimumCount { get; }
        internal InventoryPreferenceSource Source { get; }
    }

    internal sealed class ResolvedInventoryOptimizationPolicy
    {
        internal ResolvedInventoryOptimizationPolicy(
            InventorySearchEffort searchEffort, bool allowStoneTabletRotation,
            IDictionary<InventoryItemKey, ResolvedArtifactOptimizationRule>
                artifactInstanceRules,
            IDictionary<int, ResolvedArtifactOptimizationRule>
                artifactEntityRules,
            IDictionary<string, ResolvedComboOptimizationRule> comboRules)
        {
            SearchEffort = searchEffort;
            AllowStoneTabletRotation = allowStoneTabletRotation;
            ArtifactInstanceRules = new ReadOnlyDictionary<InventoryItemKey,
                ResolvedArtifactOptimizationRule>(new Dictionary<InventoryItemKey,
                    ResolvedArtifactOptimizationRule>(artifactInstanceRules));
            ArtifactEntityRules = new ReadOnlyDictionary<int,
                ResolvedArtifactOptimizationRule>(new Dictionary<int,
                    ResolvedArtifactOptimizationRule>(artifactEntityRules));
            ComboRules = new ReadOnlyDictionary<string,
                ResolvedComboOptimizationRule>(new Dictionary<string,
                    ResolvedComboOptimizationRule>(comboRules,
                        StringComparer.Ordinal));
        }

        internal InventorySearchEffort SearchEffort { get; }
        internal bool AllowStoneTabletRotation { get; }
        internal IReadOnlyDictionary<InventoryItemKey, ResolvedArtifactOptimizationRule>
            ArtifactInstanceRules
        { get; }
        internal IReadOnlyDictionary<int, ResolvedArtifactOptimizationRule>
            ArtifactEntityRules
        { get; }
        internal IReadOnlyDictionary<string, ResolvedComboOptimizationRule>
            ComboRules
        { get; }
    }

    internal static class InventoryOptimizationPolicyResolver
    {
        internal static ResolvedInventoryOptimizationPolicy Resolve(
            InventorySnapshot snapshot,
            InventoryOptimizationPreferences preferences)
        {
            preferences ??= InventoryOptimizationPreferences.Default;
            var instancePreferences = new Dictionary<InventoryItemKey,
                ArtifactOptimizationPreference>();
            var entityPreferences = new Dictionary<int,
                ArtifactOptimizationPreference>();
            foreach (ArtifactOptimizationPreference preference in
                preferences.ArtifactPreferences)
            {
                if (preference.TargetsInstance)
                {
                    instancePreferences[preference.ItemKey] = preference;
                }
                else
                {
                    entityPreferences[preference.EntityId] = preference;
                }
            }

            var artifactInstanceRules = new Dictionary<InventoryItemKey,
                ResolvedArtifactOptimizationRule>();
            bool presetEnabled = snapshot?.BuildIntent?.NativePresetEnabled == true;
            var nativeEntities = new HashSet<int>(presetEnabled
                ? snapshot.BuildIntent.PreferredArtifactEntityIds
                : Array.Empty<int>());
            InventoryItemSnapshot[] artifacts = snapshot?.Items.Where(item =>
                item.Artifact != null).ToArray() ??
                Array.Empty<InventoryItemSnapshot>();
            foreach (InventoryItemSnapshot item in artifacts)
            {
                if (!instancePreferences.TryGetValue(item.ItemKey,
                        out ArtifactOptimizationPreference preference))
                {
                    continue;
                }
                artifactInstanceRules[item.ItemKey] =
                    new ResolvedArtifactOptimizationRule(item.InstanceId,
                        item.EntityId, preference.Level,
                        preference.MinimumEffectiveLevel,
                        InventoryPreferenceSource.ManualInstance,
                        preference.PriorityOrder);
            }

            var artifactEntityRules = new Dictionary<int,
                ResolvedArtifactOptimizationRule>();
            foreach (IGrouping<int, InventoryItemSnapshot> group in artifacts.
                Where(item => !instancePreferences.ContainsKey(item.ItemKey)).
                GroupBy(item => item.EntityId))
            {
                if (entityPreferences.TryGetValue(group.Key,
                        out ArtifactOptimizationPreference preference))
                {
                    artifactEntityRules[group.Key] =
                        new ResolvedArtifactOptimizationRule(-1, group.Key,
                            preference.Level,
                            preference.MinimumEffectiveLevel,
                            InventoryPreferenceSource.UserEntityRule);
                }
                else if (nativeEntities.Contains(group.Key))
                {
                    artifactEntityRules[group.Key] =
                        new ResolvedArtifactOptimizationRule(-1, group.Key,
                            InventoryPreferenceLevel.Prefer, 1,
                            InventoryPreferenceSource.NativePreset);
                }
            }

            var comboRules = new Dictionary<string,
                ResolvedComboOptimizationRule>(StringComparer.Ordinal);
            if (presetEnabled)
            {
                foreach (string categoryId in
                    snapshot.BuildIntent.PreferredCategories)
                {
                    comboRules[categoryId] = new ResolvedComboOptimizationRule(
                        categoryId, InventoryPreferenceLevel.Prefer, 1,
                        InventoryPreferenceSource.NativePreset);
                }
            }
            foreach (ComboOptimizationPreference preference in
                preferences.ComboPreferences)
            {
                comboRules[preference.CategoryId] =
                    new ResolvedComboOptimizationRule(preference.CategoryId,
                        preference.Level, preference.MinimumCount,
                        InventoryPreferenceSource.UserCategoryRule);
            }

            return new ResolvedInventoryOptimizationPolicy(
                preferences.SearchEffort, preferences.AllowStoneTabletRotation,
                artifactInstanceRules, artifactEntityRules, comboRules);
        }
    }
}
