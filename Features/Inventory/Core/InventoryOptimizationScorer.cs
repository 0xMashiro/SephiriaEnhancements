#nullable disable
using SephiriaEnhancements.Runtime.Inventory;

using System;
using System.Collections.Generic;
using System.Linq;

namespace SephiriaEnhancements.Inventory
{
    internal sealed class InventoryOptimizationScorer
    {
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
        private readonly Dictionary<InventoryItemKey, int> damageTargetOrders;
        private readonly HashSet<InventoryItemKey> activeDamageTargets = new();
        private readonly HashSet<InventoryItemKey> redirectedDamageSources = new();

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
            damageTargetOrders = policy.ArtifactInstanceRules.Values.Where(rule =>
                rule.Level == InventoryPreferenceLevel.Priority && rule.PriorityOrder >= 0).
                ToDictionary(rule => rule.ItemKey, rule => rule.PriorityOrder);
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
            activeDamageTargets.Clear();
            redirectedDamageSources.Clear();

            foreach (ProjectedInventoryArtifactSettlement artifact in settlement.Artifacts)
            {
                InventoryItemSnapshot item = itemsByKey[artifact.ItemKey];
                if (item.Artifact.EffectEnabled && !artifact.Enabled)
                {
                    sourceEnabledArtifactsDeactivated++;
                }
                if (artifact.Enabled)
                {
                    if (damageTargetOrders.ContainsKey(artifact.ItemKey)) activeDamageTargets.Add(artifact.ItemKey);
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
                    InventoryTargetState state = EvaluateArtifactInstance(rule, artifact);
                    bool reached = state.Reached;
                    int completionPoints = state.CompletionPoints;
                    if (rule.Strength == InventoryConstraintStrength.Hard)
                    {
                        if (!reached) hardViolations++;
                        hardCompletion += completionPoints;
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
                InventoryTargetState state = EvaluateArtifactEntity(rule, settlement.Artifacts);
                if (rule.Strength == InventoryConstraintStrength.Hard)
                {
                    if (!state.Reached) hardViolations++;
                    hardCompletion += state.CompletionPoints;
                }
                else if (rule.Level == InventoryPreferenceLevel.Avoid)
                    avoidedTargetsActive += state.Value;
                else
                {
                    if (state.Reached) presetTargetsSatisfied++;
                    presetTargetCompletionPoints += state.CompletionPoints;
                }
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
                var (targetReached, completionPoints) = InventoryTargetState.Combo(rule, count);
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

            double[] orderedDamage = settlement.PositionEffects.Count == 0 || damageTargetOrders.Count == 0
                ? Array.Empty<double>() : new double[orderedPriorityCount];
            foreach (var effect in settlement.PositionEffects)
            {
                if (effect.Key.Kind != InventoryPositionEffectKind.DependencyDamage ||
                    !effect.Key.Target.HasValue || !activeDamageTargets.Contains(effect.Key.Target.Value)) continue;
                orderedDamage[damageTargetOrders[effect.Key.Target.Value]] += effect.Value;
                if (effect.Value > 0) redirectedDamageSources.Add(effect.Key.Source);
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
                hardConstraintViolations: hardViolations, hardConstraintCompletionPoints: hardCompletion,
                orderedPriorityDamageBonuses: orderedDamage);
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
                // The explicit recipient order owns this transfer. Broken chains
                // and unrelated position effects retain their normal protection.
                if (key.Kind == InventoryPositionEffectKind.DependencyDamage &&
                    redirectedDamageSources.Contains(key.Source)) return false;
                double current = after?.Value ?? 0;
                return (before?.Mode ?? after?.Mode) == true
                    ? before != null && before.Value >= 0 && (after == null || current != before.Value)
                    : current < (before?.Value ?? 0);
            }
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
                InventoryTargetState beforeState = EvaluateArtifactInstance(
                    rule, beforeArtifact);
                InventoryTargetState afterState = EvaluateArtifactInstance(
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
                InventoryTargetState beforeState = EvaluateArtifactEntity(
                    rule, before.Artifacts);
                InventoryTargetState afterState = EvaluateArtifactEntity(
                    rule, after.Artifacts);
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
                var (beforeReached, beforeCompletion) = InventoryTargetState.Combo(rule, beforeCount);
                var (afterReached, afterCompletion) = InventoryTargetState.Combo(rule, afterCount);
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
                InventoryTargetState state = EvaluateArtifactInstance(rule,
                    artifact);
                Observe(evidence, target, state.Value, state.CompletionPoints,
                    state.Reached);
            }

            foreach (var (rule, target) in entityTargets)
            {
                InventoryTargetState state = EvaluateArtifactEntity(rule,
                    settlement.Artifacts);
                Observe(evidence, target,
                    state.Value, state.CompletionPoints, state.Reached);
            }

            foreach (var (rule, target) in comboTargets)
            {
                settlement.ComboCounts.TryGetValue(rule.CategoryId,
                    out int count);
                var (reached, completion) = InventoryTargetState.Combo(rule, count);
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

        private static InventoryTargetState EvaluateArtifactInstance(
            ResolvedArtifactOptimizationRule rule,
            ProjectedInventoryArtifactSettlement artifact) => artifact == null ? default :
            InventoryTargetState.Artifact(rule.Level, rule.MinimumEffectiveLevel,
                artifact.Enabled, artifact.CappedEffectiveLevel);

        private InventoryTargetState EvaluateArtifactEntity(
            ResolvedArtifactOptimizationRule rule,
            IReadOnlyList<ProjectedInventoryArtifactSettlement> artifacts)
        {
            int value = 0;
            int completion = 0;
            bool reached = rule.Level == InventoryPreferenceLevel.Avoid;
            foreach (var artifact in artifacts)
            {
                if (policy.ArtifactInstanceRules.ContainsKey(artifact.ItemKey) ||
                    itemsByKey[artifact.ItemKey].EntityId != rule.EntityId) continue;
                var state = EvaluateArtifactInstance(rule, artifact);
                if (rule.Level == InventoryPreferenceLevel.Avoid)
                {
                    value += state.Value;
                    reached &= state.Reached;
                }
                else
                {
                    value = Math.Max(value, state.Value);
                    completion = Math.Max(completion, state.CompletionPoints);
                    reached |= state.Reached;
                }
            }
            if (rule.Level == InventoryPreferenceLevel.Avoid)
                completion = reached ? InventoryTargetState.TargetCompletionScale : 0;
            return new InventoryTargetState(value, reached, completion);
        }

        private static string ArtifactTarget(int entityId, int instanceId) =>
            "Artifact:" + entityId + ":" +
            (instanceId < 0 ? "*" : instanceId.ToString());

        private static string ComboTarget(string categoryId) =>
            "Combo:" + categoryId;

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
