using SephiriaEnhancements.Runtime.Inventory;
using SephiriaEnhancements.Inventory;

namespace SephiriaEnhancements.ModelChecks.Features.Inventory;

internal static class InventoryTwoSwapNeighborhoodChecks
{
    internal static string Run()
    {
        int[] levels = { 1, 1, 1, 1, 1, 1 };
        ArtifactActivationConditionKind[] conditions =
        {
            ArtifactActivationConditionKind.BothSidesArtifacts,
            ArtifactActivationConditionKind.None,
            ArtifactActivationConditionKind.None
        };
        int[] startingCells = { 2, 0, 4 };
        InventorySnapshot snapshot = InventoryNeighborhoodFixture.BothSidesArtifacts();
        ResolvedInventoryOptimizationPolicy policy =
            InventoryOptimizationPolicyResolver.Resolve(snapshot,
                InventoryOptimizationPreferences.Default);
        var scorer = new InventoryOptimizationScorer(snapshot, policy);
        InventoryLayoutProjection current = InventoryLayoutProjection.Current(
            snapshot);
        ProjectedInventorySettlement currentSettlement =
            InventorySettlementProjector.Evaluate(snapshot, current);
        InventoryOptimizationScore currentScore = scorer.Score(current,
            currentSettlement);

        InventoryOptimizationScore bestSingleSwapScore = currentScore;
        for (int firstCell = 0; firstCell < snapshot.Storage; firstCell++)
            for (int secondCell = firstCell + 1;
                secondCell < snapshot.Storage; secondCell++)
            {
                InventoryLayoutProjection candidate = current.WithCellsSwapped(
                    firstCell, secondCell);
                if (candidate.ContentEquals(current))
                {
                    continue;
                }
                ProjectedInventorySettlement settlement =
                    InventorySettlementProjector.Evaluate(snapshot,
                        candidate);
                if (!settlement.Succeeded)
                {
                    continue;
                }
                InventoryOptimizationScore score = scorer.Score(candidate,
                    settlement);
                if (score.CompareTo(bestSingleSwapScore) > 0)
                {
                    bestSingleSwapScore = score;
                }
            }

        InventoryOptimizationProposal optimized = InventoryOptimizer.Solve(
            snapshot, policy,
            new InventorySearchBudget(maximumImprovementRounds: 8,
                maximumCandidateEvaluations: 500,
                maximumElapsedMilliseconds: 1000));
        InventoryExhaustiveSearchResult exact =
            InventoryExhaustiveSearchOracle.Solve(snapshot, policy,
                new InventoryExhaustiveSearchLimits(
                    maximumCandidateLayouts: 200,
                    maximumElapsedMilliseconds: 1000));
        InventorySnapshot flatSnapshot = InventoryNeighborhoodFixture.OneRowArtifacts(levels,
            new[]
            {
                ArtifactActivationConditionKind.None,
                ArtifactActivationConditionKind.None,
                ArtifactActivationConditionKind.None
            }, new[] { 0, 2, 4 });
        ResolvedInventoryOptimizationPolicy flatPolicy =
            InventoryOptimizationPolicyResolver.Resolve(flatSnapshot,
                InventoryOptimizationPreferences.Default);
        InventoryOptimizationProposal flat = InventoryOptimizer.Solve(
            flatSnapshot, flatPolicy,
            new InventorySearchBudget(maximumImprovementRounds: 8,
                maximumCandidateEvaluations: 500,
                maximumElapsedMilliseconds: 1000));
        if (bestSingleSwapScore.CompareTo(currentScore) > 0)
        {
            throw new InvalidOperationException(
                "the confirmed adjacency scenario must remain a single-swap local optimum");
        }
        if (!optimized.Succeeded || !optimized.Improved ||
            !exact.ProvenOptimal ||
            exact.BestScore.CompareTo(currentScore) <= 0 ||
            optimized.BestScore.CompareTo(exact.BestScore) != 0 ||
            !optimized.Layout.ContentEquals(exact.BestLayout) ||
            optimized.DuplicateLayoutsSkipped <= 0 ||
            optimized.CandidateEvaluations > exact.CandidateLayoutsEvaluated)
        {
            throw new InvalidOperationException(
                "two-swap neighborhood must escape the adjacency local " +
                "optimum and match the exhaustive oracle; optimized=" +
                Describe(optimized.Layout) + ";exact=" +
                Describe(exact.BestLayout) + ";evaluations=" +
                optimized.CandidateEvaluations + ";exactEvaluations=" +
                exact.CandidateLayoutsEvaluated + ";duplicates=" +
                optimized.DuplicateLayoutsSkipped + ";termination=" +
                optimized.TerminationReason);
        }
        if (!flat.Succeeded || flat.Improved ||
            flat.TerminationReason !=
                InventorySearchTerminationReason.NeighborhoodLocalOptimum ||
            flat.CandidateEvaluations != 120 ||
            flat.DuplicateLayoutsSkipped <= 0)
        {
            throw new InvalidOperationException(
                "bounded neighborhoods must enumerate each unique three-item " +
                "one-row layout once;evaluations=" +
                flat.CandidateEvaluations + ";duplicates=" +
                flat.DuplicateLayoutsSkipped + ";termination=" +
                flat.TerminationReason + ";improved=" + flat.Improved);
        }

        return "start=" + Describe(current) +
            ";singleSwap=local-optimum" +
            ";twoSwap=" + Describe(optimized.Layout) +
            ";exact=" + Describe(exact.BestLayout) +
            ";evaluations=" + optimized.CandidateEvaluations +
            ";duplicatesSkipped=" + optimized.DuplicateLayoutsSkipped +
            ";flatThreeItemEvaluations=" + flat.CandidateEvaluations;
    }

    private static string Describe(InventoryLayoutProjection layout)
    {
        var cells = new int[layout.ItemCount];
        for (int index = 0; index < cells.Length; index++)
        {
            cells[index] = layout.GetCell(index);
        }
        return string.Join(',', cells);
    }
}
