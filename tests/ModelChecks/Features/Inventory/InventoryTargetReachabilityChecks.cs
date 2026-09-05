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
        VerifyUnselectedCandidateEvidence();
        return "selected;observed;proven-unreachable;unresolved;conflicting minimum targets passed";
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
                InventoryTargetState.TargetCompletionScale)
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
            policy.ArtifactInstanceRules[snapshot.Items[0].ItemKey].MinimumEffectiveLevel != 0 ||
            evaluation.RequiredValue != 0 ||
            !evaluation.AfterConditionReached ||
            evaluation.BeforeConditionReached ||
            evaluation.MaximumObservedValue != 1 ||
            evaluation.MaximumObservedCompletionPoints !=
                InventoryTargetState.TargetCompletionScale ||
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
        InventoryOptimizationTargetEvaluation evaluation =
            InventoryOptimizer.Solve(snapshot, policy,
                new InventorySearchBudget(8, 100, 5000)).TargetEvaluations.Single();

        if (evaluation.Reachability !=
                InventoryTargetReachability.Unresolved ||
            evaluation.MaximumObservedValue != 3)
        {
            throw new InvalidOperationException(
                "partial observations must not be promoted to an " +
                "unreachability proof");
        }
    }

    private static void VerifyUnselectedCandidateEvidence()
    {
        // Only one level-five cell exists. Both targets are individually
        // reachable, but the first queue entry wins their conflict.
        InventorySnapshot snapshot = InventorySnapshotFixture.ArtifactsAtLevels(
            new[] { 5, 0, 0, 0, 0, 0 }, new[] { 0, 1 });
        var preferences = new InventoryOptimizationPreferences(InventorySearchEffort.Balanced, true,
            snapshot.Items.Select((item, index) => new ArtifactOptimizationPreference(
                item.InstanceId, item.EntityId, InventoryPreferenceLevel.Priority, 5, index)).ToArray(),
            Array.Empty<ComboOptimizationPreference>());
        ResolvedInventoryOptimizationPolicy policy = InventoryOptimizationPolicyResolver.Resolve(snapshot, preferences);
        foreach (int budget in new[] { 1, 2, 100 })
        {
            InventoryOptimizationProposal result = InventoryOptimizer.Solve(snapshot, policy,
                new InventorySearchBudget(8, budget, 5000));
            InventoryOptimizationTargetEvaluation second = result.TargetEvaluations.Single(target =>
                target.Target == "Artifact:1001:101");
            if (!result.Succeeded || result.Improved || second.RequiredValue != 5 ||
                second.AfterConditionReached || second.AfterValue != 0 ||
                second.Reachability != (budget == 1 ? InventoryTargetReachability.Unresolved
                    : InventoryTargetReachability.ObservedReachable) ||
                second.MaximumObservedValue != (budget == 1 ? 0 : 5))
                throw new InvalidOperationException(
                    "an unselected candidate proves an individual target reachable without relaxing it or sacrificing queue order");
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
