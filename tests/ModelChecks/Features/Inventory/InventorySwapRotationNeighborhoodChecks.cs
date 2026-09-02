using SephiriaEnhancements.Runtime.Inventory;
using SephiriaEnhancements.Inventory;

namespace SephiriaEnhancements.ModelChecks.Features.Inventory;

internal static class InventorySwapRotationNeighborhoodChecks
{
    internal static string Run()
    {
        InventorySnapshot snapshot = InventoryNeighborhoodFixture.StoneTabletMoveAndRotation();
        ResolvedInventoryOptimizationPolicy policy =
            InventoryOptimizationPolicyResolver.Resolve(snapshot,
                InventoryOptimizationPreferences.Default);
        var scorer = new InventoryOptimizationScorer(snapshot, policy);
        InventoryLayoutProjection current = InventoryLayoutProjection.Current(
            snapshot);
        InventoryOptimizationScore currentScore = Score(snapshot, scorer,
            current);

        InventoryOptimizationScore bestSingleStepScore = currentScore;
        for (int firstCell = 0; firstCell < snapshot.Storage; firstCell++)
        {
            for (int secondCell = firstCell + 1;
                secondCell < snapshot.Storage; secondCell++)
            {
                InventoryLayoutProjection candidate = current.WithCellsSwapped(
                    firstCell, secondCell);
                if (!candidate.ContentEquals(current))
                {
                    Promote(snapshot, scorer, candidate,
                        ref bestSingleStepScore);
                }
            }
        }
        for (int rotation = 1; rotation < 4; rotation++)
        {
            Promote(snapshot, scorer, current.WithRotation(1, rotation),
                ref bestSingleStepScore);
        }

        InventoryOptimizationProposal optimized = InventoryOptimizer.Solve(
            snapshot, policy,
            new InventorySearchBudget(maximumImprovementRounds: 8,
                maximumCandidateEvaluations: 1000,
                maximumElapsedMilliseconds: 1000));
        InventoryExhaustiveSearchResult exact =
            InventoryExhaustiveSearchOracle.Solve(snapshot, policy,
                new InventoryExhaustiveSearchLimits(
                    maximumCandidateLayouts: 200,
                    maximumElapsedMilliseconds: 1000));
        InventoryOptimizationProposal evaluationLimited =
            InventoryOptimizer.Solve(snapshot, policy,
                new InventorySearchBudget(maximumImprovementRounds: 8,
                    maximumCandidateEvaluations: 14,
                    maximumElapsedMilliseconds: 1000));

        if (bestSingleStepScore.CompareTo(currentScore) > 0)
        {
            throw new InvalidOperationException(
                "the stone-tablet scenario must remain a single-step local optimum");
        }
        if (!optimized.Succeeded || !optimized.Improved ||
            !exact.ProvenOptimal ||
            exact.EstimatedCandidateLayouts != 120 ||
            optimized.BestScore.CompareTo(exact.BestScore) != 0 ||
            !optimized.Layout.ContentEquals(exact.BestLayout) ||
            optimized.Layout.GetCell(0) != 2 ||
            optimized.Layout.GetCell(1) != 4 ||
            optimized.Layout.GetRotation(1) != 1)
        {
            throw new InvalidOperationException(
                "joint stone-tablet move and rotation must escape the local optimum and match the exhaustive oracle");
        }
        if (!evaluationLimited.Succeeded ||
            evaluationLimited.CandidateEvaluations != 14 ||
            evaluationLimited.TerminationReason !=
                InventorySearchTerminationReason.CandidateEvaluationLimit)
        {
            throw new InvalidOperationException(
                "joint stone-tablet search must obey the shared candidate-evaluation budget");
        }

        return "start=" + Describe(current) +
            ";singleStep=local-optimum" +
            ";joint=" + Describe(optimized.Layout) +
            ";exact=" + Describe(exact.BestLayout) +
            ";budget=" + evaluationLimited.CandidateEvaluations;
    }

    private static InventoryOptimizationScore Score(InventorySnapshot snapshot,
        InventoryOptimizationScorer scorer, InventoryLayoutProjection layout)
    {
        ProjectedInventorySettlement settlement =
            InventorySettlementProjector.Evaluate(snapshot, layout);
        if (!settlement.Succeeded)
        {
            throw new InvalidOperationException(
                "synthetic stone-tablet layout must be evaluable: " +
                string.Join(',', settlement.Issues));
        }
        return scorer.Score(layout, settlement);
    }

    private static void Promote(InventorySnapshot snapshot,
        InventoryOptimizationScorer scorer, InventoryLayoutProjection candidate,
        ref InventoryOptimizationScore bestScore)
    {
        InventoryOptimizationScore score = Score(snapshot, scorer, candidate);
        if (score.CompareTo(bestScore) > 0)
        {
            bestScore = score;
        }
    }

    private static string Describe(InventoryLayoutProjection layout)
    {
        return layout.GetCell(0) + "," + layout.GetCell(1) + "@" +
            layout.GetRotation(1);
    }
}
