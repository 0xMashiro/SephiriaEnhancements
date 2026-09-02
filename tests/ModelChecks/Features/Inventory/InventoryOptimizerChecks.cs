using SephiriaEnhancements.ModelChecks.Runtime.Inventory;
using SephiriaEnhancements.Runtime.Inventory;
using SephiriaEnhancements.Inventory;

namespace SephiriaEnhancements.ModelChecks.Features.Inventory;

internal static class InventoryOptimizerChecks
{
    internal static void Run()
    {
        InventorySnapshot rowSnapshot = InventorySnapshotFixture.RowDependentArtifact();
        ResolvedInventoryOptimizationPolicy defaultPolicy =
            InventoryOptimizationPolicyResolver.Resolve(rowSnapshot,
                InventoryOptimizationPreferences.Default);
        InventoryOptimizationProposal optimizedRow = InventoryOptimizer.Solve(rowSnapshot,
            defaultPolicy, new InventorySearchBudget(maximumImprovementRounds: 4,
                maximumCandidateEvaluations: 100,
                maximumElapsedMilliseconds: 1000));
        if (!optimizedRow.Succeeded || !optimizedRow.Improved ||
            optimizedRow.Layout.GetCell(0) < 2 ||
            optimizedRow.BestScore.ComboBreakpointValue <=
                optimizedRow.CurrentScore.ComboBreakpointValue ||
            optimizedRow.CandidateEvaluations < 2 ||
            optimizedRow.ElapsedMilliseconds < 0)
            throw new InvalidOperationException(
                "optimizer must move the row-dependent artifact across a real breakpoint");
        if (!InventoryLayoutPlanner.TryCreate(rowSnapshot, optimizedRow.Layout,
                out InventoryApplicationPlan rowPlan, out string rowPlanIssue) ||
            rowPlan.Swaps.Count != 1 || rowPlan.Rotations.Count != 0 ||
            rowPlan.Swaps[0].ExpectedFirstItemKey != null ||
            rowPlan.Swaps[0].ExpectedSecondItemKey != new InventoryItemKey(301, 31) ||
            rowPlanIssue != string.Empty)
            throw new InvalidOperationException(
                "layout planner must produce one identity-checked move into an empty cell");
        InventoryOptimizationOutcome? rowOutcome = optimizedRow.Outcome;
        if (rowOutcome == null)
        {
            throw new InvalidOperationException(
                "inventory outcome must be available for a successful proposal");
        }
        InventoryCategoryOutcome fireOutcome = rowOutcome.CategoryChanges.Single(
            change => change.CategoryId == "FIRE");
        InventoryCategoryOutcome iceOutcome = rowOutcome.CategoryChanges.Single(
            change => change.CategoryId == "ICE");
        InventoryArtifactOutcome artifactOutcome = rowOutcome.ArtifactChanges.Single(
            change => change.InstanceId == 31);
        if (artifactOutcome.EntityId != 301 ||
            artifactOutcome.NameKey != "Item_Row" ||
            !artifactOutcome.BeforeEnabled || !artifactOutcome.AfterEnabled ||
            artifactOutcome.BeforeEffectiveLevel != 0 ||
            artifactOutcome.AfterEffectiveLevel != 2 ||
            rowOutcome.MovedItems != 1 ||
            rowOutcome.RotatedTablets != 0 ||
            rowOutcome.BeforeArtifactsEnabled != 1 ||
            rowOutcome.AfterArtifactsEnabled != 1 ||
            rowOutcome.BeforeEffectiveLevels != 0 ||
            rowOutcome.AfterEffectiveLevels != 2 ||
            rowOutcome.BeforeBreakpointValue != 0 ||
            rowOutcome.AfterBreakpointValue != 1 ||
            rowOutcome.ArtifactChanges.Count != 1 ||
            rowOutcome.CategoryChanges.Count != 2 ||
            fireOutcome.BeforeCount != 1 || fireOutcome.AfterCount != 0 ||
            fireOutcome.BeforeBreakpointValue != 0 ||
            fireOutcome.AfterBreakpointValue != 0 ||
            iceOutcome.BeforeCount != 0 || iceOutcome.AfterCount != 1 ||
            iceOutcome.BeforeBreakpointValue != 0 ||
            iceOutcome.AfterBreakpointValue != 1)
        {
            throw new InvalidOperationException(
                "inventory outcome must explain artifact, category and operation changes");
        }
        Console.WriteLine("InventoryOptimizer: breakpoint search and native operation planning passed");
        Console.WriteLine("InventoryOptimizationOutcome: HUD-ready change summary passed");

        InventoryLayoutProjection currentRowLayout = InventoryLayoutProjection.Current(
            rowSnapshot);
        if (!currentRowLayout.ContentEquals(new InventoryLayoutProjection(
                currentRowLayout.CopyCells(), currentRowLayout.CopyRotations())) ||
            currentRowLayout.CompareStableTo(optimizedRow.Layout) >= 0)
            throw new InvalidOperationException(
                "candidate layout ordering must be deterministic without string keys");

        long rowCandidateLayouts =
            InventoryExhaustiveSearchOracle.EstimateCandidateLayouts(rowSnapshot);
        InventoryExhaustiveSearchResult exactRow =
            InventoryExhaustiveSearchOracle.Solve(rowSnapshot, defaultPolicy,
                new InventoryExhaustiveSearchLimits(
                    maximumCandidateLayouts: 10,
                    maximumElapsedMilliseconds: 1000));
        if (rowCandidateLayouts != 4 || !exactRow.SearchStarted ||
            !exactRow.ProvenOptimal || exactRow.EstimatedCandidateLayouts != 4 ||
            exactRow.CandidateLayoutsEvaluated != 4 ||
            exactRow.BestScore.CompareTo(optimizedRow.BestScore) != 0 ||
            !exactRow.BestLayout.ContentEquals(optimizedRow.Layout))
            throw new InvalidOperationException(
                "exhaustive oracle must prove and reproduce the small-layout optimum");

        InventoryExhaustiveSearchResult rejectedExactRow =
            InventoryExhaustiveSearchOracle.Solve(rowSnapshot, defaultPolicy,
                new InventoryExhaustiveSearchLimits(
                    maximumCandidateLayouts: 3,
                    maximumElapsedMilliseconds: 1000));
        if (rejectedExactRow.SearchStarted || rejectedExactRow.ProvenOptimal ||
            rejectedExactRow.TerminationReason !=
                InventoryExhaustiveSearchTerminationReason.CandidateLayoutLimit)
            throw new InvalidOperationException(
                "exhaustive oracle must reject search spaces above its exact limit");
        Console.WriteLine("InventoryExhaustiveSearchOracle: exact optimum and search-space gate passed");

        InventoryOptimizationProposal exactHybrid = InventoryOptimizerSelector.Solve(
            rowSnapshot, defaultPolicy,
            new InventorySearchBudget(maximumImprovementRounds: 4,
                maximumCandidateEvaluations: 10,
                maximumElapsedMilliseconds: 1000));
        InventoryOptimizationProposal neighborhoodHybrid =
            InventoryOptimizerSelector.Solve(rowSnapshot, defaultPolicy,
                new InventorySearchBudget(maximumImprovementRounds: 4,
                    maximumCandidateEvaluations: 3,
                    maximumElapsedMilliseconds: 1000));
        if (!exactHybrid.Succeeded || !exactHybrid.OptimalityProven ||
            exactHybrid.SearchMethod != InventoryOptimizationSearchMethod.Exhaustive ||
            exactHybrid.TerminationReason !=
                InventorySearchTerminationReason.SearchSpaceExhausted ||
            exactHybrid.CandidateEvaluations != 4 ||
            exactHybrid.Outcome == null ||
            !exactHybrid.Layout.ContentEquals(exactRow.BestLayout) ||
            neighborhoodHybrid.SearchMethod !=
                InventoryOptimizationSearchMethod.Neighborhood ||
            neighborhoodHybrid.OptimalityProven)
        {
            throw new InvalidOperationException(
                "hybrid solver must prove small spaces and budget larger spaces with neighborhood search");
        }
        Console.WriteLine("InventoryOptimizerSelector: exact-small and bounded-neighborhood selection passed");
    }
}
