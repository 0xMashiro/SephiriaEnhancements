using SephiriaEnhancements.ModelChecks.Runtime.Inventory;
using SephiriaEnhancements.Runtime.Inventory;
using SephiriaEnhancements.Inventory;

namespace SephiriaEnhancements.ModelChecks.Features.Inventory;

internal static class InventorySearchBudgetChecks
{
    internal static void Run()
    {
        InventorySnapshot rowSnapshot = InventorySnapshotFixture.RowDependentArtifact();
        ResolvedInventoryOptimizationPolicy defaultPolicy =
            InventoryOptimizationPolicyResolver.Resolve(rowSnapshot,
                InventoryOptimizationPreferences.Default);
        InventoryOptimizationProposal evaluationLimited = InventoryOptimizer.Solve(
            rowSnapshot, defaultPolicy,
            new InventorySearchBudget(maximumImprovementRounds: 4,
                maximumCandidateEvaluations: 1,
                maximumElapsedMilliseconds: 1000));
        if (!evaluationLimited.Succeeded ||
            evaluationLimited.CandidateEvaluations != 1 ||
            evaluationLimited.TerminationReason !=
                InventorySearchTerminationReason.CandidateEvaluationLimit)
            throw new InvalidOperationException(
                "candidate evaluation budget must stop search after the initial layout");

        InventoryOptimizationProposal timeLimited = InventoryOptimizer.Solve(
            rowSnapshot, defaultPolicy,
            new InventorySearchBudget(maximumImprovementRounds: 4,
                maximumCandidateEvaluations: 100,
                maximumElapsedMilliseconds: 0));
        if (!timeLimited.Succeeded || timeLimited.CandidateEvaluations != 1 ||
            timeLimited.TerminationReason !=
                InventorySearchTerminationReason.ElapsedTimeLimit)
            throw new InvalidOperationException(
                "elapsed time budget must stop search after the initial layout");
        VerifyBestCandidateSurvivesBudget();
        VerifyAdditionalBudgetPreservesQuality();
        VerifyFinalRoundComparesCompoundCandidates();
        InventoryBatchEvaluationChecks.Run();
        Console.WriteLine("InventorySearchBudget: limits, best-candidate retention and increasing-budget quality passed");
    }

    private static void VerifyFinalRoundComparesCompoundCandidates()
    {
        var snapshot = InventoryNeighborhoodFixture.OneRowArtifacts(
            new[] { 1, 3, 0, 0, 2, 0 }, new[]
            {
                ArtifactActivationConditionKind.None,
                ArtifactActivationConditionKind.None,
                ArtifactActivationConditionKind.BothSidesArtifacts
            }, new[] { 0, 4, 2 });
        var policy = InventoryOptimizationPolicyResolver.Resolve(snapshot,
            InventoryOptimizationPreferences.Default);
        var scorer = new InventoryOptimizationScorer(snapshot, policy);
        // Both arrangements enable the surrounded artifact. The early three-cell
        // cycle has only two effective levels; the later one has four.
        var witness = new InventoryLayoutProjection(new[] { 0, 2, 1 }, new int[3]);
        var witnessScore = scorer.Score(witness, InventorySettlementProjector.Evaluate(snapshot, witness));
        var result = InventoryOptimizer.Solve(snapshot, policy, new InventorySearchBudget(1, 500, 5000));
        if (!result.Succeeded || witnessScore.EnabledArtifactCount != 3 ||
            witnessScore.CappedEffectiveArtifactLevelTotal != 4 || result.BestScore.CompareTo(witnessScore) < 0)
            throw new InvalidOperationException("the final round must compare compound improvements when no local-search round remains");
    }

    private static void VerifyBestCandidateSurvivesBudget()
    {
        InventorySnapshot snapshot = InventorySnapshotFixture.ArtifactsAtLevels(
            new[] { 0, 5, 1, 0, 0, 0 }, new[] { 0 });
        ResolvedInventoryOptimizationPolicy policy = InventoryOptimizationPolicyResolver.Resolve(
            snapshot, InventoryOptimizationPreferences.Default);
        var budget = new InventorySearchBudget(8, 2, 5000);
        foreach (InventoryOptimizationProposal result in new[]
        {
            InventoryOptimizer.Solve(snapshot, policy, budget),
            InventoryOptimizerSelector.Solve(snapshot, policy, budget)
        })
        {
            if (!result.Succeeded || !result.Improved || result.CandidateEvaluations != 2 ||
                result.TerminationReason != InventorySearchTerminationReason.CandidateEvaluationLimit ||
                result.Layout.GetCell(0) != 1 || result.BestScore.CappedEffectiveArtifactLevelTotal != 5 ||
                result.Outcome?.AfterEffectiveLevels != 5)
                throw new InvalidOperationException(
                    "a budget cutoff must retain the better layout already evaluated, including its outcome");
            if (result.SearchStages.Sum(stage => stage.CandidateEvaluations) != result.CandidateEvaluations - 1 ||
                result.SearchStages.Sum(stage => stage.Improvements) != 1 ||
                result.SearchStages.Max(stage => stage.LastImprovementCandidate) != 2 ||
                result.SearchStages.Any(stage => stage.Round != 1 || stage.ElapsedMilliseconds < 0))
                throw new InvalidOperationException("partial-round diagnostics must account for the evaluated improvement before cutoff");
        }
    }

    private static void VerifyAdditionalBudgetPreservesQuality()
    {
        foreach (InventorySnapshot snapshot in new[]
        {
            InventoryNeighborhoodFixture.BothSidesArtifacts(),
            InventoryNeighborhoodFixture.StoneTabletMoveAndRotation()
        })
        {
            ResolvedInventoryOptimizationPolicy policy = InventoryOptimizationPolicyResolver.Resolve(
                snapshot, InventoryOptimizationPreferences.Default);
            InventoryOptimizationScore? previous = null;
            bool improved = false;
            for (int evaluations = 1; evaluations <= 160; evaluations++)
            {
                InventoryOptimizationProposal result = InventoryOptimizer.Solve(snapshot, policy,
                    new InventorySearchBudget(8, evaluations, 5000));
                if (!result.Succeeded || result.CandidateEvaluations > evaluations ||
                    result.TerminationReason == InventorySearchTerminationReason.ElapsedTimeLimit ||
                    previous != null && result.BestScore.CompareTo(previous) < 0)
                    throw new InvalidOperationException(
                        "extending the same deterministic search must not discard an earlier improvement");
                previous = result.BestScore;
                improved |= result.Improved;
            }
            if (!improved)
                throw new InvalidOperationException("the budget sweep must reach a joint-move improvement");
        }
    }
}
