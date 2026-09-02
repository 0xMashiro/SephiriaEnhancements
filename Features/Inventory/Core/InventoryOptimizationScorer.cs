#nullable disable
using SephiriaEnhancements.Runtime.Inventory;

using System;
using System.Collections.Generic;
using System.Linq;

namespace SephiriaEnhancements.Inventory
{
    internal sealed class InventoryOptimizationScorer
    {
        internal const int TargetCompletionScale = 10_000;

        private readonly InventorySnapshot snapshot;
        private readonly ResolvedInventoryOptimizationPolicy policy;
        private readonly Dictionary<InventoryItemKey, InventoryItemSnapshot> itemsByKey;

        internal InventoryOptimizationScorer(InventorySnapshot snapshot,
            ResolvedInventoryOptimizationPolicy policy)
        {
            this.snapshot = snapshot;
            this.policy = policy;
            itemsByKey = snapshot.Items.ToDictionary(item => item.ItemKey);
        }

        internal InventoryOptimizationScore Score(InventoryLayoutProjection layout,
            ProjectedInventorySettlement settlement)
        {
            int priorityTargetsSatisfied = 0;
            int priorityTargetCompletionPoints = 0;
            int avoidedTargetsActive = 0;
            int coreTargetsSatisfied = 0;
            int coreTargetCompletionPoints = 0;
            int preferredTargetsSatisfied = 0;
            int preferredTargetCompletionPoints = 0;
            int sourceEnabledArtifactsDeactivated = 0;
            int enabledArtifactCount = 0;
            int cappedEffectiveArtifactLevelTotal = 0;
            int excessArtifactLevelTotal = 0;
            int orderedPriorityCount = policy.ArtifactInstanceRules.Values.
                Where(rule => rule.Level == InventoryPreferenceLevel.Priority &&
                    rule.PriorityOrder >= 0).Select(rule =>
                    rule.PriorityOrder).DefaultIfEmpty(-1).Max() + 1;
            int[] orderedPriorityCompletionPoints =
                new int[orderedPriorityCount];

            foreach (ProjectedInventoryArtifactSettlement artifact in settlement.Artifacts)
            {
                InventoryItemSnapshot item = itemsByKey[artifact.ItemKey];
                if (item.Artifact.EffectEnabled && !artifact.Enabled)
                {
                    sourceEnabledArtifactsDeactivated++;
                }
                if (artifact.Enabled)
                {
                    enabledArtifactCount++;
                    cappedEffectiveArtifactLevelTotal +=
                        artifact.CappedEffectiveLevel;
                }
                excessArtifactLevelTotal += Math.Max(0,
                    artifact.DisplayedLevel - item.Artifact.MaxLevel);
                if (policy.ArtifactInstanceRules.TryGetValue(
                        artifact.ItemKey,
                        out ResolvedArtifactOptimizationRule rule))
                {
                    int effectiveLevel = artifact.Enabled
                        ? artifact.CappedEffectiveLevel
                        : 0;
                    bool reached = artifact.Enabled &&
                        effectiveLevel >= rule.MinimumEffectiveLevel;
                    int completionPoints = CalculateTargetCompletionPoints(
                        artifact.Enabled, effectiveLevel,
                        rule.MinimumEffectiveLevel);
                    if (rule.Level == InventoryPreferenceLevel.Priority &&
                        rule.PriorityOrder >= 0 && rule.PriorityOrder <
                            orderedPriorityCompletionPoints.Length)
                    {
                        orderedPriorityCompletionPoints[rule.PriorityOrder] =
                            completionPoints;
                    }
                    switch (rule.Level)
                    {
                        case InventoryPreferenceLevel.Priority:
                            if (reached) priorityTargetsSatisfied++;
                            priorityTargetCompletionPoints += completionPoints;
                            break;
                        case InventoryPreferenceLevel.Avoid:
                            if (artifact.Enabled) avoidedTargetsActive++;
                            break;
                        case InventoryPreferenceLevel.Core:
                            if (reached) coreTargetsSatisfied++;
                            coreTargetCompletionPoints += completionPoints;
                            break;
                        case InventoryPreferenceLevel.Prefer:
                            if (reached)
                            {
                                preferredTargetsSatisfied++;
                            }
                            preferredTargetCompletionPoints += completionPoints;
                            break;
                    }
                }
            }

            foreach (ResolvedArtifactOptimizationRule rule in
                policy.ArtifactEntityRules.Values)
            {
                ProjectedInventoryArtifactSettlement[] candidates = settlement.Artifacts.
                    Where(artifact => !policy.ArtifactInstanceRules.ContainsKey(
                            artifact.ItemKey) &&
                        itemsByKey[artifact.ItemKey].EntityId ==
                            rule.EntityId).ToArray();
                if (rule.Level == InventoryPreferenceLevel.Avoid)
                {
                    avoidedTargetsActive += candidates.Count(artifact =>
                        artifact.Enabled);
                    continue;
                }

                bool reached = candidates.Any(artifact => artifact.Enabled &&
                    artifact.CappedEffectiveLevel >=
                        rule.MinimumEffectiveLevel);
                int completionPoints = candidates.Select(artifact =>
                    CalculateTargetCompletionPoints(artifact.Enabled,
                        artifact.CappedEffectiveLevel,
                        rule.MinimumEffectiveLevel)).DefaultIfEmpty(0).Max();
                switch (rule.Level)
                {
                    case InventoryPreferenceLevel.Priority:
                        if (reached) priorityTargetsSatisfied++;
                        priorityTargetCompletionPoints += completionPoints;
                        break;
                    case InventoryPreferenceLevel.Core:
                        if (reached) coreTargetsSatisfied++;
                        coreTargetCompletionPoints += completionPoints;
                        break;
                    case InventoryPreferenceLevel.Prefer:
                        if (reached) preferredTargetsSatisfied++;
                        preferredTargetCompletionPoints += completionPoints;
                        break;
                }
            }

            int comboBreakpointValue = 0;
            foreach (ComboCategorySnapshot category in snapshot.ComboCategories)
            {
                settlement.ComboCounts.TryGetValue(category.CategoryId,
                    out int count);
                int reached = CalculateReachedBreakpointValue(category, count);
                comboBreakpointValue += reached;
                if (policy.ComboRules.TryGetValue(category.CategoryId,
                        out ResolvedComboOptimizationRule rule))
                {
                    bool targetReached = count >= rule.MinimumCount;
                    int completionPoints = CalculateTargetCompletionPoints(
                        active: count > 0, currentValue: count,
                        minimumValue: rule.MinimumCount);
                    switch (rule.Level)
                    {
                        case InventoryPreferenceLevel.Priority:
                            if (targetReached) priorityTargetsSatisfied++;
                            priorityTargetCompletionPoints += completionPoints;
                            break;
                        case InventoryPreferenceLevel.Avoid:
                            if (targetReached) avoidedTargetsActive++;
                            break;
                        case InventoryPreferenceLevel.Core:
                            if (targetReached) coreTargetsSatisfied++;
                            coreTargetCompletionPoints += completionPoints;
                            break;
                        case InventoryPreferenceLevel.Prefer:
                            if (targetReached) preferredTargetsSatisfied++;
                            preferredTargetCompletionPoints += completionPoints;
                            break;
                    }
                }
            }

            int movedItemCount = 0;
            int rotatedTabletCount = 0;
            for (int index = 0; index < snapshot.Items.Count; index++)
            {
                InventoryItemSnapshot item = snapshot.Items[index];
                if (layout.GetCell(index) != item.CellIndex) movedItemCount++;
                if (item.StoneTablet != null &&
                    layout.GetRotation(index) != item.StoneTablet.Rotation)
                {
                    rotatedTabletCount++;
                }
            }

            return new InventoryOptimizationScore(
                priorityTargetsSatisfied: priorityTargetsSatisfied,
                priorityTargetCompletionPoints:
                    priorityTargetCompletionPoints,
                avoidedTargetsActive: avoidedTargetsActive,
                coreTargetsSatisfied: coreTargetsSatisfied,
                coreTargetCompletionPoints: coreTargetCompletionPoints,
                preferredTargetsSatisfied: preferredTargetsSatisfied,
                preferredTargetCompletionPoints:
                    preferredTargetCompletionPoints,
                sourceEnabledArtifactsDeactivated:
                    sourceEnabledArtifactsDeactivated,
                enabledArtifactCount: enabledArtifactCount,
                comboBreakpointValue: comboBreakpointValue,
                cappedEffectiveArtifactLevelTotal:
                    cappedEffectiveArtifactLevelTotal,
                excessArtifactLevelTotal: excessArtifactLevelTotal,
                movedItemCount: movedItemCount,
                rotatedTabletCount: rotatedTabletCount,
                orderedPriorityCompletionPoints:
                    orderedPriorityCompletionPoints,
                positionEffectRegressions: CountPositionEffectRegressions(settlement));
        }

        private int CountPositionEffectRegressions(ProjectedInventorySettlement settlement)
        {
            if (snapshot.PositionEffects.Observed.Count == 0 && settlement.PositionEffects.Count == 0) return 0;
            var baseline = snapshot.PositionEffects.Observed.ToDictionary(value => value.Key);
            var candidates = settlement.PositionEffects.ToDictionary(value => value.Key);
            int regressions = 0;
            foreach (var key in baseline.Keys.Union(candidates.Keys))
            {
                if (policy.ArtifactInstanceRules.TryGetValue(key.Source, out var instanceRule) &&
                    instanceRule.Level == InventoryPreferenceLevel.Avoid) continue;
                if (!policy.ArtifactInstanceRules.ContainsKey(key.Source) &&
                    policy.ArtifactEntityRules.TryGetValue(key.Source.EntityId, out var entityRule) &&
                    entityRule.Level == InventoryPreferenceLevel.Avoid) continue;
                baseline.TryGetValue(key, out var before);
                candidates.TryGetValue(key, out var after);
                double current = after?.Value ?? 0;
                bool lost = (before?.Mode ?? after?.Mode) == true
                    ? before != null && before.Value >= 0 && (after == null || current != before.Value)
                    : current < (before?.Value ?? 0);
                if (lost) regressions++;
            }
            return regressions;
        }

        internal static int CalculateTargetCompletionPoints(bool active,
            int currentValue, int minimumValue)
        {
            if (!active)
            {
                return 0;
            }
            if (minimumValue <= 0)
            {
                return TargetCompletionScale;
            }

            long nonNegativeValue = Math.Max(0, currentValue);
            return (int)Math.Min(TargetCompletionScale,
                nonNegativeValue * TargetCompletionScale / minimumValue);
        }

        internal InventoryOptimizationTargetEvaluation[] EvaluateTargets(
            ProjectedInventorySettlement before,
            ProjectedInventorySettlement after,
            IReadOnlyDictionary<string, InventoryTargetSearchEvidence>
                searchEvidence = null,
            bool allLayoutsEvaluated = false)
        {
            var result = new List<InventoryOptimizationTargetEvaluation>();
            var beforeArtifacts = before.Artifacts.ToDictionary(
                artifact => artifact.ItemKey);
            var afterArtifacts = after.Artifacts.ToDictionary(
                artifact => artifact.ItemKey);
            foreach (ResolvedArtifactOptimizationRule rule in
                policy.ArtifactInstanceRules.Values)
            {
                beforeArtifacts.TryGetValue(rule.ItemKey,
                    out ProjectedInventoryArtifactSettlement beforeArtifact);
                afterArtifacts.TryGetValue(rule.ItemKey,
                    out ProjectedInventoryArtifactSettlement afterArtifact);
                ArtifactTargetState beforeState = EvaluateArtifactInstance(
                    rule, beforeArtifact);
                ArtifactTargetState afterState = EvaluateArtifactInstance(
                    rule, afterArtifact);
                int requiredValue = ArtifactRequiredValue(rule);
                string target = ArtifactTarget(rule.EntityId,
                    rule.ItemKey.NativeInstanceId);
                InventoryTargetSearchEvidence evidence = CombineEvidence(
                    searchEvidence, target, beforeState.Value,
                    beforeState.CompletionPoints, beforeState.Reached,
                    afterState.Value, afterState.CompletionPoints,
                    afterState.Reached);
                result.Add(new InventoryOptimizationTargetEvaluation(
                    target,
                    InventoryOptimizationTargetKind.Artifact,
                    rule.Level, rule.Source, requiredValue,
                    beforeState.Value, afterState.Value,
                    beforeState.Reached, afterState.Reached,
                    beforeState.CompletionPoints,
                    afterState.CompletionPoints,
                    evidence.MaximumObservedValue,
                    evidence.MaximumObservedCompletionPoints,
                    ResolveReachability(afterState.Reached, evidence,
                        allLayoutsEvaluated)));
            }

            foreach (ResolvedArtifactOptimizationRule rule in
                policy.ArtifactEntityRules.Values)
            {
                ArtifactTargetState beforeState = EvaluateArtifactEntity(
                    rule, beforeArtifacts);
                ArtifactTargetState afterState = EvaluateArtifactEntity(
                    rule, afterArtifacts);
                int requiredValue = ArtifactRequiredValue(rule);
                string target = ArtifactTarget(rule.EntityId, -1);
                InventoryTargetSearchEvidence evidence = CombineEvidence(
                    searchEvidence, target, beforeState.Value,
                    beforeState.CompletionPoints, beforeState.Reached,
                    afterState.Value, afterState.CompletionPoints,
                    afterState.Reached);
                result.Add(new InventoryOptimizationTargetEvaluation(target,
                    InventoryOptimizationTargetKind.Artifact, rule.Level,
                    rule.Source, requiredValue, beforeState.Value,
                    afterState.Value, beforeState.Reached, afterState.Reached,
                    beforeState.CompletionPoints,
                    afterState.CompletionPoints,
                    evidence.MaximumObservedValue,
                    evidence.MaximumObservedCompletionPoints,
                    ResolveReachability(afterState.Reached, evidence,
                        allLayoutsEvaluated)));
            }

            foreach (ResolvedComboOptimizationRule rule in
                policy.ComboRules.Values)
            {
                before.ComboCounts.TryGetValue(rule.CategoryId,
                    out int beforeCount);
                after.ComboCounts.TryGetValue(rule.CategoryId,
                    out int afterCount);
                bool beforeReached = beforeCount >= rule.MinimumCount;
                bool afterReached = afterCount >= rule.MinimumCount;
                int beforeCompletion = CalculateTargetCompletionPoints(
                    beforeCount > 0, beforeCount, rule.MinimumCount);
                int afterCompletion = CalculateTargetCompletionPoints(
                    afterCount > 0, afterCount, rule.MinimumCount);
                string target = ComboTarget(rule.CategoryId);
                InventoryTargetSearchEvidence evidence = CombineEvidence(
                    searchEvidence, target, beforeCount, beforeCompletion,
                    beforeReached, afterCount, afterCompletion, afterReached);
                result.Add(new InventoryOptimizationTargetEvaluation(
                    target,
                    InventoryOptimizationTargetKind.ComboCategory,
                    rule.Level, rule.Source, rule.MinimumCount,
                    beforeCount, afterCount, beforeReached, afterReached,
                    beforeCompletion, afterCompletion,
                    evidence.MaximumObservedValue,
                    evidence.MaximumObservedCompletionPoints,
                    ResolveReachability(afterReached, evidence,
                        allLayoutsEvaluated)));
            }
            return result.ToArray();
        }

        internal void ObserveTargets(ProjectedInventorySettlement settlement,
            IDictionary<string, InventoryTargetSearchEvidence> evidence)
        {
            if (settlement?.Succeeded != true || evidence == null)
            {
                return;
            }

            var artifacts = settlement.Artifacts.ToDictionary(
                artifact => artifact.ItemKey);
            foreach (ResolvedArtifactOptimizationRule rule in
                policy.ArtifactInstanceRules.Values)
            {
                artifacts.TryGetValue(rule.ItemKey,
                    out ProjectedInventoryArtifactSettlement artifact);
                ArtifactTargetState state = EvaluateArtifactInstance(rule,
                    artifact);
                Observe(evidence, ArtifactTarget(rule.EntityId,
                    rule.ItemKey.NativeInstanceId), state.Value, state.CompletionPoints,
                    state.Reached);
            }

            foreach (ResolvedArtifactOptimizationRule rule in
                policy.ArtifactEntityRules.Values)
            {
                ArtifactTargetState state = EvaluateArtifactEntity(rule,
                    artifacts);
                Observe(evidence, ArtifactTarget(rule.EntityId, -1),
                    state.Value, state.CompletionPoints, state.Reached);
            }

            foreach (ResolvedComboOptimizationRule rule in
                policy.ComboRules.Values)
            {
                settlement.ComboCounts.TryGetValue(rule.CategoryId,
                    out int count);
                bool reached = count >= rule.MinimumCount;
                Observe(evidence, ComboTarget(rule.CategoryId), count,
                    CalculateTargetCompletionPoints(count > 0, count,
                        rule.MinimumCount), reached);
            }
        }

        private static void Observe(
            IDictionary<string, InventoryTargetSearchEvidence> evidence,
            string target, int value, int completionPoints, bool reached)
        {
            evidence.TryGetValue(target,
                out InventoryTargetSearchEvidence previous);
            if (previous == null)
            {
                evidence[target] = new InventoryTargetSearchEvidence(value,
                    completionPoints, reached);
                return;
            }

            previous.Observe(value, completionPoints, reached);
        }

        private static InventoryTargetSearchEvidence CombineEvidence(
            IReadOnlyDictionary<string, InventoryTargetSearchEvidence>
                searchEvidence,
            string target, int beforeValue, int beforeCompletion,
            bool beforeReached, int afterValue, int afterCompletion,
            bool afterReached)
        {
            InventoryTargetSearchEvidence observed = null;
            searchEvidence?.TryGetValue(target, out observed);
            return new InventoryTargetSearchEvidence(
                Math.Max(observed?.MaximumObservedValue ?? 0,
                    Math.Max(beforeValue, afterValue)),
                Math.Max(observed?.MaximumObservedCompletionPoints ?? 0,
                    Math.Max(beforeCompletion, afterCompletion)),
                beforeReached || afterReached ||
                    observed?.ConditionObserved == true);
        }

        private static InventoryTargetReachability ResolveReachability(
            bool selectedLayoutReachesCondition,
            InventoryTargetSearchEvidence evidence,
            bool allLayoutsEvaluated)
        {
            if (selectedLayoutReachesCondition)
            {
                return InventoryTargetReachability.
                    SelectedLayoutReachesCondition;
            }
            if (evidence.ConditionObserved)
            {
                return InventoryTargetReachability.ObservedReachable;
            }
            return allLayoutsEvaluated
                ? InventoryTargetReachability.ProvenUnreachable
                : InventoryTargetReachability.Unresolved;
        }

        private static int ArtifactRequiredValue(
            ResolvedArtifactOptimizationRule rule) =>
            rule.Level == InventoryPreferenceLevel.Avoid
                ? 0
                : rule.MinimumEffectiveLevel;

        private static ArtifactTargetState EvaluateArtifactInstance(
            ResolvedArtifactOptimizationRule rule,
            ProjectedInventoryArtifactSettlement artifact)
        {
            if (artifact == null)
            {
                return default;
            }
            if (rule.Level == InventoryPreferenceLevel.Avoid)
            {
                bool disabled = !artifact.Enabled;
                return new ArtifactTargetState(artifact.Enabled ? 1 : 0,
                    disabled, disabled ? TargetCompletionScale : 0);
            }

            int value = EffectiveLevel(artifact);
            bool reached = artifact.Enabled &&
                value >= rule.MinimumEffectiveLevel;
            return new ArtifactTargetState(value, reached,
                CalculateTargetCompletionPoints(artifact.Enabled, value,
                    rule.MinimumEffectiveLevel));
        }

        private ArtifactTargetState EvaluateArtifactEntity(
            ResolvedArtifactOptimizationRule rule,
            IReadOnlyDictionary<InventoryItemKey, ProjectedInventoryArtifactSettlement> artifacts)
        {
            ProjectedInventoryArtifactSettlement[] candidates = artifacts.Values.Where(
                artifact => !policy.ArtifactInstanceRules.ContainsKey(
                        artifact.ItemKey) &&
                    itemsByKey[artifact.ItemKey].EntityId ==
                        rule.EntityId).ToArray();
            if (candidates.Length == 0)
            {
                return default;
            }
            if (rule.Level == InventoryPreferenceLevel.Avoid)
            {
                int activeCount = candidates.Count(artifact =>
                    artifact.Enabled);
                bool allDisabled = activeCount == 0;
                return new ArtifactTargetState(activeCount, allDisabled,
                    allDisabled ? TargetCompletionScale : 0);
            }

            int value = candidates.Max(EffectiveLevel);
            bool reached = candidates.Any(artifact => artifact.Enabled &&
                artifact.CappedEffectiveLevel >=
                    rule.MinimumEffectiveLevel);
            int completion = candidates.Max(artifact =>
                CalculateTargetCompletionPoints(artifact.Enabled,
                    artifact.CappedEffectiveLevel,
                    rule.MinimumEffectiveLevel));
            return new ArtifactTargetState(value, reached, completion);
        }

        private static string ArtifactTarget(int entityId, int instanceId) =>
            "Artifact:" + entityId + ":" +
            (instanceId < 0 ? "*" : instanceId.ToString());

        private static string ComboTarget(string categoryId) =>
            "Combo:" + categoryId;

        private static int EffectiveLevel(
            ProjectedInventoryArtifactSettlement artifact)
        {
            return artifact?.Enabled == true
                ? artifact.CappedEffectiveLevel
                : 0;
        }

        private readonly struct ArtifactTargetState
        {
            internal ArtifactTargetState(int value, bool reached,
                int completionPoints)
            {
                Value = value;
                Reached = reached;
                CompletionPoints = completionPoints;
            }

            internal int Value { get; }
            internal bool Reached { get; }
            internal int CompletionPoints { get; }
        }

        internal static int CalculateReachedBreakpointValue(
            ComboCategorySnapshot category, int count)
        {
            var thresholds = new HashSet<int>(category.SetThresholds);
            thresholds.UnionWith(category.ComboThresholds);
            int value = 0;
            foreach (int threshold in thresholds)
            {
                if (threshold > 0 && count >= threshold)
                {
                    value += threshold;
                }
            }
            return value;
        }
    }
}
