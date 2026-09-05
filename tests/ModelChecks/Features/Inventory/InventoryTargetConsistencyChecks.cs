using SephiriaEnhancements.Inventory;
using SephiriaEnhancements.ModelChecks.Runtime.Inventory;
using SephiriaEnhancements.Runtime;
using SephiriaEnhancements.Runtime.Inventory;

namespace SephiriaEnhancements.ModelChecks.Features.Inventory;

internal static class InventoryTargetConsistencyChecks
{
    internal static void Run()
    {
        foreach (var (level, target, reached, completion) in new[]
        {
            (-1, 0, false, 0), (0, 0, true, 10000), (0, 3, false, 1),
            (1, 3, false, 3333), (6, 3, true, 10000), (6, 8, false, 7500)
        })
            foreach (bool avoid in new[] { false, true })
                foreach (var strength in new[] { InventoryConstraintStrength.Soft, InventoryConstraintStrength.Hard })
                {
                    var snapshot = InventorySnapshotFixture.ArtifactsAtLevels(new[] { level }, new[] { 0 }, maxLevel: 6);
                    var preference = avoid ? InventoryPreferenceLevel.Avoid : InventoryPreferenceLevel.Priority;
                    var preferences = new InventoryOptimizationPreferences(InventorySearchEffort.Balanced, true,
                        new[] { new ArtifactOptimizationPreference(100, 1000, preference, target, intentSlotIndex: 0, strength: strength) },
                        Array.Empty<ComboOptimizationPreference>());
                    var policy = InventoryOptimizationPolicyResolver.Resolve(snapshot, preferences);
                    var layout = InventoryLayoutProjection.Current(snapshot);
                    var settlement = InventorySettlementProjector.Evaluate(snapshot, layout);
                    bool expectedReached = avoid ? level < 0 : reached;
                    int expectedCompletion = avoid ? expectedReached ? 10000 : 0 : completion;
                    Verify(snapshot, layout, settlement, policy, preferences, expectedReached, expectedCompletion,
                        avoid, strength, !avoid && level >= 0);
                }
        // Explicit native counts isolate goal interpretation from combo settlement mechanics.
        foreach (var (count, target, reached, completion) in new[]
        {
            (0, 0, true, 10000), (0, 3, false, 0), (1, 3, false, 3333), (4, 3, true, 10000)
        })
            foreach (bool avoid in new[] { false, true })
                foreach (var strength in new[] { InventoryConstraintStrength.Soft, InventoryConstraintStrength.Hard })
                {
                    var category = new ComboCategorySnapshot("Scholar", count, count, 0, count, 0,
                        Array.Empty<int>(), Array.Empty<int>(), false);
                    var snapshot = new InventorySnapshot(1, 0, Array.Empty<InventoryCellSnapshot>(),
                        Array.Empty<InventoryItemSnapshot>(), comboCategories: new[] { category });
                    var preferences = new InventoryOptimizationPreferences(InventorySearchEffort.Balanced, true,
                        Array.Empty<ArtifactOptimizationPreference>(),
                        new[] { new ComboOptimizationPreference("Scholar", avoid ? InventoryPreferenceLevel.Avoid : InventoryPreferenceLevel.Priority, target, strength) });
                    var policy = InventoryOptimizationPolicyResolver.Resolve(snapshot, preferences);
                    var layout = InventoryLayoutProjection.Current(snapshot);
                    var settlement = new ProjectedInventorySettlement(true, null, null,
                        new Dictionary<string, int> { ["Scholar"] = count }, null);
                    bool expectedReached = avoid ? count <= target : reached;
                    Verify(snapshot, layout, settlement, policy, preferences, expectedReached,
                        avoid ? expectedReached ? 10000 : 0 : completion, avoid, strength, !avoid && count > 0);
                }
        Console.WriteLine("InventoryTargetConsistency: 40 artifact/combo cases agree across score, evidence and native feedback");
    }

    private static void Verify(InventorySnapshot snapshot, InventoryLayoutProjection layout,
        ProjectedInventorySettlement settlement, ResolvedInventoryOptimizationPolicy policy,
        InventoryOptimizationPreferences preferences, bool reached, int completion, bool avoid,
        InventoryConstraintStrength strength, bool canBePartial)
    {
        var scorer = new InventoryOptimizationScorer(snapshot, policy);
        var score = scorer.Score(layout, settlement);
        var evidence = new Dictionary<string, InventoryTargetSearchEvidence>();
        scorer.ObserveTargets(settlement, evidence);
        var evaluation = scorer.EvaluateTargets(settlement, settlement, evidence).Single();
        Require(evaluation.AfterConditionReached == reached && evaluation.AfterCompletionPoints == completion &&
            evidence.Single().Value.ConditionObserved == reached && evidence.Single().Value.MaximumObservedCompletionPoints == completion);
        if (strength == InventoryConstraintStrength.Hard)
            Require(score.HardConstraintViolations == (reached ? 0 : 1) && score.HardConstraintCompletionPoints == completion);
        else if (avoid) Require(score.AvoidedTargetsActive == (reached ? 0 : 1));
        else Require(score.PriorityTargetsSatisfied == (reached ? 1 : 0) && score.PriorityTargetCompletionPoints == completion);
        var runtime = new RuntimeStateSnapshot("fixture", 1, 1, 1, 1, 1,
            RuntimeCapabilities.InventorySnapshot | RuntimeCapabilities.SettledInventoryObservation,
            RuntimeConsistencyState.Consistent, 0, "");
        var feedback = new InventoryIntentResultFeedback(snapshot, policy, preferences, runtime);
        var state = snapshot.Items.Count == 0 ? feedback.FindCombo("Scholar") : feedback.Find(snapshot.Items[0].ItemKey).State;
        Require(state == (reached ? InventoryIntentSatisfaction.Satisfied
            : strength == InventoryConstraintStrength.Soft && canBePartial ? InventoryIntentSatisfaction.Partial : InventoryIntentSatisfaction.Unmet));
    }

    private static void Require(bool condition)
    {
        if (!condition) throw new InvalidOperationException("target evaluation diverged between scoring, evidence and native feedback");
    }
}
