using SephiriaEnhancements.ModelChecks.Runtime.Inventory;
using SephiriaEnhancements.Runtime.Inventory;
using SephiriaEnhancements.Inventory;

namespace SephiriaEnhancements.ModelChecks.Features.Inventory;

internal static class InventoryTargetReachabilityChecks
{
    internal static string Run()
    {
        VerifyProvenUnreachableArtifactTarget();
        VerifySelectedLayoutReachesTarget();
        VerifyObservedReachableAvoidCondition();
        VerifyNeighborhoodEvidenceRemainsUnresolved();
        return "selected;observed;proven-unreachable;unresolved passed";
    }

    private static void VerifyProvenUnreachableArtifactTarget()
    {
        InventorySnapshot snapshot = InventorySnapshotFixture.ArtifactsAtLevels(
            new[] { 3, 0 }, new[] { 0 });
        ResolvedInventoryOptimizationPolicy policy = ResolveArtifactPolicy(
            snapshot, InventoryPreferenceLevel.Priority, minimumLevel: 4);
        InventoryOptimizationProposal proposal = InventoryOptimizerSelector.
            Solve(snapshot, policy, new InventorySearchBudget(2, 100, 1000));
        InventoryOptimizationTargetEvaluation evaluation = proposal.
            TargetEvaluations.Single();

        if (!proposal.OptimalityProven ||
            evaluation.AfterConditionReached ||
            evaluation.MaximumObservedValue != 3 ||
            evaluation.MaximumObservedCompletionPoints != 7_500 ||
            evaluation.Reachability !=
                InventoryTargetReachability.ProvenUnreachable)
        {
            throw new InvalidOperationException(
                "complete enumeration must prove level four unreachable " +
                "when the independent maximum is level three");
        }
    }

    private static void VerifySelectedLayoutReachesTarget()
    {
        InventorySnapshot snapshot = InventorySnapshotFixture.ArtifactsAtLevels(
            new[] { 3, 0 }, new[] { 0 });
        ResolvedInventoryOptimizationPolicy policy = ResolveArtifactPolicy(
            snapshot, InventoryPreferenceLevel.Priority, minimumLevel: 3);
        InventoryOptimizationProposal proposal = InventoryOptimizerSelector.
            Solve(snapshot, policy, new InventorySearchBudget(2, 100, 1000));
        InventoryOptimizationTargetEvaluation evaluation = proposal.
            TargetEvaluations.Single();

        if (!evaluation.AfterConditionReached ||
            evaluation.Reachability != InventoryTargetReachability.
                SelectedLayoutReachesCondition ||
            evaluation.MaximumObservedValue != 3 ||
            evaluation.MaximumObservedCompletionPoints !=
                InventoryOptimizationScorer.TargetCompletionScale)
        {
            throw new InvalidOperationException(
                "selected layout must report its reached target directly");
        }
    }

    private static void VerifyObservedReachableAvoidCondition()
    {
        InventorySnapshot snapshot = InventorySnapshotFixture.ArtifactsAtLevels(
            new[] { 0, -1 }, new[] { 0 });
        ResolvedInventoryOptimizationPolicy policy = ResolveArtifactPolicy(
            snapshot, InventoryPreferenceLevel.Avoid, minimumLevel: 5);
        InventoryOptimizationProposal proposal = InventoryOptimizerSelector.
            Solve(snapshot, policy, new InventorySearchBudget(2, 100, 1000));
        InventoryOptimizationTargetEvaluation evaluation = proposal.
            TargetEvaluations.Single();

        if (!proposal.OptimalityProven ||
            policy.ArtifactInstanceRules[100].MinimumEffectiveLevel != 0 ||
            evaluation.RequiredValue != 0 ||
            !evaluation.AfterConditionReached ||
            evaluation.BeforeConditionReached ||
            evaluation.MaximumObservedValue != 1 ||
            evaluation.MaximumObservedCompletionPoints !=
                InventoryOptimizationScorer.TargetCompletionScale ||
            evaluation.Reachability !=
                InventoryTargetReachability.SelectedLayoutReachesCondition)
        {
            throw new InvalidOperationException(
                "artifact Avoid must select a disabled artifact state and " +
                "report the condition as reached");
        }
    }

    private static void VerifyNeighborhoodEvidenceRemainsUnresolved()
    {
        InventorySnapshot snapshot = InventorySnapshotFixture.ArtifactsAtLevels(
            new[] { 3, 0 }, new[] { 0 });
        ResolvedInventoryOptimizationPolicy policy = ResolveArtifactPolicy(
            snapshot, InventoryPreferenceLevel.Priority, minimumLevel: 4);
        ProjectedInventorySettlement current =
            InventorySettlementProjector.Evaluate(snapshot,
                InventoryLayoutProjection.Current(snapshot));
        InventoryOptimizationTargetEvaluation evaluation =
            new InventoryOptimizationScorer(snapshot, policy).
                EvaluateTargets(current, current).Single();

        if (evaluation.Reachability !=
                InventoryTargetReachability.Unresolved ||
            evaluation.MaximumObservedValue != 3)
        {
            throw new InvalidOperationException(
                "partial observations must not be promoted to an " +
                "unreachability proof");
        }
    }

    private static ResolvedInventoryOptimizationPolicy ResolveArtifactPolicy(
        InventorySnapshot snapshot, InventoryPreferenceLevel level,
        int minimumLevel)
    {
        var preferences = new InventoryOptimizationPreferences(
            InventorySearchEffort.Balanced,
            allowStoneTabletRotation: true,
            new[]
            {
                new ArtifactOptimizationPreference(100, 1000, level,
                    minimumLevel)
            },
            Array.Empty<ComboOptimizationPreference>());
        return InventoryOptimizationPolicyResolver.Resolve(snapshot,
            preferences);
    }
}
