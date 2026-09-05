#nullable disable
using System;
using System.Collections.Generic;
using System.Linq;
using SephiriaEnhancements.Runtime;
using SephiriaEnhancements.Runtime.Inventory;

namespace SephiriaEnhancements.Inventory
{
    internal enum InventoryIntentSatisfaction
    {
        NotEvaluated,
        Satisfied,
        Partial,
        Unmet
    }

    internal sealed class InventoryIntentResultFeedback
    {
        private readonly InventoryOptimizationPreferences preferences;
        private readonly long epoch;
        private readonly long revision;
        private readonly uint player;
        private readonly Dictionary<InventoryItemKey, InventoryArtifactGoalFeedback> goals = new();
        private readonly Dictionary<string, InventoryIntentSatisfaction> comboGoals = new();

        // Publish only after native settlement verification, or a verified unchanged search.
        internal InventoryIntentResultFeedback(InventorySnapshot actual,
            ResolvedInventoryOptimizationPolicy policy, InventoryOptimizationPreferences preferences,
            RuntimeStateSnapshot runtime)
        {
            this.preferences = preferences;
            epoch = runtime.GameplayContextEpoch;
            revision = runtime.InventoryRevision;
            player = runtime.PlayerNetId;
            foreach (var rule in policy.ComboRules.Values)
            {
                if (rule.Source == InventoryPreferenceSource.NativePreset) continue;
                int count = actual.ComboCategories.FirstOrDefault(category => category.CategoryId == rule.CategoryId)?.CurrentCount ?? 0;
                bool reached = InventoryTargetState.Combo(rule, count).Reached;
                comboGoals[rule.CategoryId] = reached ? InventoryIntentSatisfaction.Satisfied
                    : rule.Strength == InventoryConstraintStrength.Hard || rule.Level == InventoryPreferenceLevel.Avoid || count == 0
                        ? InventoryIntentSatisfaction.Unmet : InventoryIntentSatisfaction.Partial;
            }
            foreach (var item in actual.Items.Where(item => item.Artifact != null))
            {
                if (!policy.ArtifactInstanceRules.TryGetValue(item.ItemKey, out var rule)) continue;
                goals[item.ItemKey] = new InventoryArtifactGoalFeedback(item.Artifact,
                    rule.Level, rule.MinimumEffectiveLevel, rule.Strength);
            }
        }

        internal bool IsCurrent(RuntimeStateSnapshot runtime, InventoryOptimizationPreferences current) =>
            ReferenceEquals(preferences, current) && runtime?.HasSettledInventoryObservation == true &&
            runtime.GameplayContextEpoch == epoch && runtime.InventoryRevision == revision && runtime.PlayerNetId == player;

        internal InventoryArtifactGoalFeedback Find(InventoryItemKey key) =>
            goals.TryGetValue(key, out var goal) ? goal : null;
        internal InventoryIntentSatisfaction FindCombo(string categoryId) =>
            comboGoals.TryGetValue(categoryId, out var state) ? state : InventoryIntentSatisfaction.NotEvaluated;
    }

    internal sealed class InventoryArtifactGoalFeedback
    {
        internal InventoryArtifactGoalFeedback(ArtifactSnapshot artifact,
            InventoryPreferenceLevel preference, int targetLevel,
            InventoryConstraintStrength strength = InventoryConstraintStrength.Soft)
        {
            TargetLevel = targetLevel;
            CurrentLevel = Math.Max(0, artifact.LimitedEffectEnabledLevel);
            Active = artifact.EffectEnabled;
            var target = InventoryTargetState.Artifact(preference, targetLevel, Active, CurrentLevel);
            State = target.Reached ? InventoryIntentSatisfaction.Satisfied
                : strength == InventoryConstraintStrength.Hard || !Active || preference == InventoryPreferenceLevel.Avoid
                    ? InventoryIntentSatisfaction.Unmet : InventoryIntentSatisfaction.Partial;
        }

        internal int TargetLevel { get; }
        internal int CurrentLevel { get; }
        internal bool Active { get; }
        internal InventoryIntentSatisfaction State { get; }
    }
}
