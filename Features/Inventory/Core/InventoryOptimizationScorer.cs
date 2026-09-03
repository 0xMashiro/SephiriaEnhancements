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
        private readonly int orderedPriorityCount;
        private readonly (string Category, int[] Thresholds)[] comboThresholds;
        private readonly (ResolvedArtifactOptimizationRule Rule, string Target)[] instanceTargets;
        private readonly (ResolvedArtifactOptimizationRule Rule, string Target)[] entityTargets;
        private readonly (ResolvedComboOptimizationRule Rule, string Target)[] comboTargets;
        private readonly Dictionary<InventoryPositionEffectKey, InventoryPositionEffectValue> baselineEffects;
        // A scorer belongs to one search; scratch indexes never escape into its results.
        private readonly Dictionary<InventoryItemKey, ProjectedInventoryArtifactSettlement> observedArtifacts = new();
        private readonly Dictionary<InventoryPositionEffectKey, InventoryPositionEffectValue> candidateEffects = new();

        internal InventoryOptimizationScorer(InventorySnapshot snapshot,
            ResolvedInventoryOptimizationPolicy policy)
        {
            this.snapshot = snapshot;
            this.policy = policy;
            itemsByKey = snapshot.Items.ToDictionary(item => item.ItemKey);
            orderedPriorityCount = policy.ArtifactInstanceRules.Values.
                Where(rule => rule.Level == InventoryPreferenceLevel.Priority &&
                    rule.PriorityOrder >= 0).Select(rule =>
                    rule.PriorityOrder).DefaultIfEmpty(-1).Max() + 1;
            comboThresholds = snapshot.ComboCategories.Select(category =>
                (category.CategoryId, category.SetThresholds.Union(category.ComboThresholds).
                    Where(threshold => threshold > 0).ToArray())).ToArray();
            instanceTargets = policy.ArtifactInstanceRules.Values.Select(rule =>
                (rule, ArtifactTarget(rule.EntityId, rule.ItemKey.NativeInstanceId))).ToArray();
            entityTargets = policy.ArtifactEntityRules.Values.Select(rule =>
                (rule, ArtifactTarget(rule.EntityId, -1))).ToArray();
            comboTargets = policy.ComboRules.Values.Select(rule =>
                (rule, ComboTarget(rule.CategoryId))).ToArray();
            baselineEffects = InventoryPositionEffectProjector.EvaluateCurrent(snapshot).ToDictionary(value => value.Key);
        }

        internal InventoryOptimizationScore Score(InventoryLayoutProjection layout,
            ProjectedInventorySettlement settlement)
        {
            int priorityTargetsSatisfied = 0;
            int priorityTargetCompletionPoints = 0;
            int avoidedTargetsActive = 0;
            int presetTargetsSatisfied = 0;
            int presetTargetCompletionPoints = 0;
            int sourceEnabledArtifactsDeactivated = 0;
            int enabledArtifactCount = 0;
            int cappedEffectiveArtifactLevelTotal = 0;
            int excessArtifactLevelTotal = 0;
            int hardViolations = 0;
            int hardCompletion = 0;
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
                    int completionPoints = CalculateArtifactCompletionPoints(
                        artifact.Enabled, effectiveLevel,
                        rule.MinimumEffectiveLevel);
                    if (rule.Strength == InventoryConstraintStrength.Hard)
                    {
                        bool satisfied = rule.Level == InventoryPreferenceLevel.Avoid ? !artifact.Enabled : reached;
                        if (!satisfied) hardViolations++;
                        hardCompletion += rule.Level == InventoryPreferenceLevel.Avoid
                            ? satisfied ? TargetCompletionScale : 0 : completionPoints;
                        continue;
                    }
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
                    if (rule.Strength == InventoryConstraintStrength.Hard)
                    {
                        bool inactive = candidates.All(artifact => !artifact.Enabled);
                        if (!inactive) hardViolations++;
                        else hardCompletion += TargetCompletionScale;
                        continue;
                    }
                    avoidedTargetsActive += candidates.Count(artifact =>
                        artifact.Enabled);
                    continue;
                }

                bool reached = candidates.Any(artifact => artifact.Enabled &&
                    artifact.CappedEffectiveLevel >=
                        rule.MinimumEffectiveLevel);
                int completionPoints = candidates.Select(artifact =>
                    CalculateArtifactCompletionPoints(artifact.Enabled,
                        artifact.CappedEffectiveLevel,
                        rule.MinimumEffectiveLevel)).DefaultIfEmpty(0).Max();
                if (rule.Strength == InventoryConstraintStrength.Hard)
                {
                    if (!reached) hardViolations++;
                    hardCompletion += completionPoints;
                    continue;
                }
                if (reached) presetTargetsSatisfied++;
                presetTargetCompletionPoints += completionPoints;
            }

            int comboBreakpointValue = 0;
            foreach (var (category, thresholds) in comboThresholds)
            {
                settlement.ComboCounts.TryGetValue(category,
                    out int count);
                foreach (int threshold in thresholds)
                    if (count >= threshold) comboBreakpointValue += threshold;
            }
            foreach (ResolvedComboOptimizationRule rule in policy.ComboRules.Values)
            {
                settlement.ComboCounts.TryGetValue(rule.CategoryId, out int count);
                var (targetReached, completionPoints) = EvaluateComboTarget(rule, count);
                if (rule.Strength == InventoryConstraintStrength.Hard)
                {
                    if (!targetReached) hardViolations++;
                    hardCompletion += completionPoints;
                    continue;
                }
                switch (rule.Level)
                {
                    case InventoryPreferenceLevel.Priority when rule.Source != InventoryPreferenceSource.NativePreset:
                        if (targetReached) priorityTargetsSatisfied++;
                        priorityTargetCompletionPoints += completionPoints;
                        break;
                    case InventoryPreferenceLevel.Avoid:
                        if (!targetReached) avoidedTargetsActive++;
                        break;
                    case InventoryPreferenceLevel.Priority:
                        if (targetReached) presetTargetsSatisfied++;
                        presetTargetCompletionPoints += completionPoints;
                        break;
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
                presetTargetsSatisfied: presetTargetsSatisfied,
                presetTargetCompletionPoints:
                    presetTargetCompletionPoints,
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
                positionEffectRegressions: CountPositionEffectRegressions(settlement),
                automaticLevelRegressions: CountAutomaticLevelRegressions(settlement),
                hardConstraintViolations: hardViolations, hardConstraintCompletionPoints: hardCompletion);
        }

        private int CountAutomaticLevelRegressions(ProjectedInventorySettlement settlement)
        {
            int regressions = 0;
            foreach (var artifact in settlement.Artifacts)
            {
                var observed = itemsByKey[artifact.ItemKey].Artifact;
                int limit = observed.SafeAutomaticLevel;
                if (policy.ArtifactInstanceRules.TryGetValue(artifact.ItemKey, out var rule))
                {
                    if (rule.Level == InventoryPreferenceLevel.Avoid) continue;
                    // Explicit level requests may opt into a stronger penalty.
                    limit = Math.Max(limit, rule.MinimumEffectiveLevel);
                }
                if (artifact.Enabled && artifact.CappedEffectiveLevel > limit) regressions++;
            }
            return regressions;
        }

        private int CountPositionEffectRegressions(ProjectedInventorySettlement settlement)
        {
            if (baselineEffects.Count == 0 && settlement.PositionEffects.Count == 0) return 0;
            candidateEffects.Clear();
            foreach (var effect in settlement.PositionEffects) candidateEffects.Add(effect.Key, effect);
            int regressions = 0;
            foreach (var before in baselineEffects.Values)
            {
                candidateEffects.TryGetValue(before.Key, out var after);
                if (Regressed(before.Key, before, after)) regressions++;
            }
            foreach (var after in settlement.PositionEffects)
                if (!baselineEffects.ContainsKey(after.Key) && Regressed(after.Key, null, after)) regressions++;
            return regressions;

            bool Regressed(InventoryPositionEffectKey key, InventoryPositionEffectValue before,
                InventoryPositionEffectValue after)
            {
                if (policy.ArtifactInstanceRules.TryGetValue(key.Source, out var instanceRule) &&
                    instanceRule.Level == InventoryPreferenceLevel.Avoid) return false;
                if (!policy.ArtifactInstanceRules.ContainsKey(key.Source) &&
                    policy.ArtifactEntityRules.TryGetValue(key.Source.EntityId, out var entityRule) &&
                    entityRule.Level == InventoryPreferenceLevel.Avoid) return false;
                double current = after?.Value ?? 0;
                return (before?.Mode ?? after?.Mode) == true
                    ? before != null && before.Value >= 0 && (after == null || current != before.Value)
                    : current < (before?.Value ?? 0);
            }
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

        // Level zero is a working artifact, and must outrank an inactive one
        // within its queue slot even when its upgrade target is out of reach.
        private static int CalculateArtifactCompletionPoints(bool active, int currentValue, int minimumValue) =>
            Math.Max(active ? 1 : 0, CalculateTargetCompletionPoints(active, currentValue, minimumValue));

        private static (bool Reached, int CompletionPoints) EvaluateComboTarget(
            ResolvedComboOptimizationRule rule, int count)
        {
            if (rule.Level == InventoryPreferenceLevel.Avoid)
            {
                bool reached = count <= rule.TargetCount;
                return (reached, reached ? TargetCompletionScale : 0);
            }
            return (count >= rule.TargetCount,
                CalculateTargetCompletionPoints(true, count, rule.TargetCount));
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
                var (beforeReached, beforeCompletion) = EvaluateComboTarget(rule, beforeCount);
                var (afterReached, afterCompletion) = EvaluateComboTarget(rule, afterCount);
                string target = ComboTarget(rule.CategoryId);
                InventoryTargetSearchEvidence evidence = CombineEvidence(
                    searchEvidence, target, beforeCount, beforeCompletion,
                    beforeReached, afterCount, afterCompletion, afterReached);
                result.Add(new InventoryOptimizationTargetEvaluation(
                    target,
                    InventoryOptimizationTargetKind.ComboCategory,
                    rule.Level, rule.Source, rule.TargetCount,
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
            if (settlement?.Succeeded != true || evidence == null ||
                policy.ArtifactInstanceRules.Count == 0 && policy.ArtifactEntityRules.Count == 0 &&
                policy.ComboRules.Count == 0)
            {
                return;
            }

            observedArtifacts.Clear();
            foreach (var artifact in settlement.Artifacts) observedArtifacts.Add(artifact.ItemKey, artifact);
            foreach (var (rule, target) in instanceTargets)
            {
                observedArtifacts.TryGetValue(rule.ItemKey,
                    out ProjectedInventoryArtifactSettlement artifact);
                ArtifactTargetState state = EvaluateArtifactInstance(rule,
                    artifact);
                Observe(evidence, target, state.Value, state.CompletionPoints,
                    state.Reached);
            }

            foreach (var (rule, target) in entityTargets)
            {
                ArtifactTargetState state = EvaluateArtifactEntity(rule,
                    observedArtifacts);
                Observe(evidence, target,
                    state.Value, state.CompletionPoints, state.Reached);
            }

            foreach (var (rule, target) in comboTargets)
            {
                settlement.ComboCounts.TryGetValue(rule.CategoryId,
                    out int count);
                var (reached, completion) = EvaluateComboTarget(rule, count);
                Observe(evidence, target, count,
                    completion, reached);
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
                CalculateArtifactCompletionPoints(artifact.Enabled, value,
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
                CalculateArtifactCompletionPoints(artifact.Enabled,
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
