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
        VerifyBestCandidatesSurviveNeighborhoodLimits();
        Console.WriteLine("InventorySearchBudget: evaluation and elapsed-time limits; retained neighborhood improvements passed");
    }

    private static void VerifyBestCandidatesSurviveNeighborhoodLimits()
    {
        foreach (var snapshot in new[]
        {
            InventoryNeighborhoodFixture.BothSidesArtifacts(),
            InventoryNeighborhoodFixture.StoneTabletMoveAndRotation()
        })
        {
            var policy = InventoryOptimizationPolicyResolver.Resolve(snapshot, InventoryOptimizationPreferences.Default);
            InventoryOptimizationScore? previous = null;
            bool improved = false;
            for (int limit = 1; limit <= 120; limit++)
            {
                var proposal = InventoryOptimizer.Solve(snapshot, policy, new InventorySearchBudget(8, limit, 10000));
                if (!proposal.Succeeded || proposal.CandidateEvaluations > limit ||
                    previous != null && proposal.BestScore.CompareTo(previous) < 0)
                    throw new InvalidOperationException("a larger candidate budget must not discard the best layout already found");
                previous = proposal.BestScore;
                improved |= proposal.Improved;
            }
            if (!improved)
                throw new InvalidOperationException("budget checks must reach an improvement in the joint neighborhood");
        }
    }
}
