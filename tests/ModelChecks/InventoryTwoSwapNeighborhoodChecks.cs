using SephiriaEnhancements.Runtime.Inventory;
using SephiriaEnhancements.Inventory;

internal static class InventoryTwoSwapNeighborhoodChecks
{
    internal static InventorySnapshot CreateConformanceScenario()
    {
        return CreateSnapshot(new[] { 1, 1, 1, 1, 1, 1 },
            new[]
            {
                ArtifactActivationConditionKind.BothSidesArtifacts,
                ArtifactActivationConditionKind.None,
                ArtifactActivationConditionKind.None
            }, new[] { 2, 0, 4 });
    }

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
        InventorySnapshot snapshot = CreateConformanceScenario();
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
        InventorySnapshot flatSnapshot = CreateSnapshot(levels,
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

    private static InventorySnapshot CreateSnapshot(int[] levels,
        ArtifactActivationConditionKind[] conditions, int[] itemCells)
    {
        const int storage = 6;
        int[] maximumLevels = { 1, 2, 3 };
        var occupied = new bool[storage];
        foreach (int cell in itemCells)
        {
            occupied[cell] = true;
        }

        var items = new InventoryItemSnapshot[itemCells.Length];
        for (int itemIndex = 0; itemIndex < items.Length; itemIndex++)
        {
            int cell = itemCells[itemIndex];
            bool conditionApplies = conditions[itemIndex] !=
                ArtifactActivationConditionKind.None;
            bool criteriaSatisfied = EvaluateOneRowCondition(
                conditions[itemIndex], cell, occupied);
            var artifact = new ArtifactSnapshot(levels[cell],
                maximumLevels[itemIndex], enchant: 0,
                effectEnabledLevel: 0,
                limitedEffectEnabledLevel: criteriaSatisfied
                    ? Math.Min(levels[cell], maximumLevels[itemIndex])
                    : 0,
                effectEnabled: criteriaSatisfied,
                penaltyEnabled: !criteriaSatisfied,
                weaponRestricted: false, requiredWeapon: string.Empty,
                weaponCompatible: true, uniqueEffect: false,
                uniqueEffectRegistered: false, calculationOrder: "Pre",
                new CriteriaSnapshot(conditions[itemIndex],
                    conditionApplies
                        ? criteriaSatisfied
                            ? CriteriaEvaluationState.Satisfied
                            : CriteriaEvaluationState.Unsatisfied
                        : CriteriaEvaluationState.NotApplicable,
                    CriteriaEvaluationState.NotApplicable),
                Array.Empty<string>(), Array.Empty<string>(),
                attackable: false, magic: null);
            items[itemIndex] = new InventoryItemSnapshot(100 + itemIndex,
                200 + itemIndex, quantity: 1, cell,
                x: cell, y: 0, "Artifact " + itemIndex,
                "Artifact_" + itemIndex, "Charm", "Normal",
                Array.Empty<string>(), conditionApplies
                    ? InventoryItemKind.RestrictedArtifact
                    : InventoryItemKind.Artifact,
                artifact, stoneTablet: null);
        }

        var cells = new InventoryCellSnapshot[storage];
        for (int cell = 0; cell < storage; cell++)
        {
            int itemIndex = Array.IndexOf(itemCells, cell);
            int maximumLevel = itemIndex >= 0 ? maximumLevels[itemIndex] : -1;
            var settlement = new InventoryCellSettlementSnapshot(true,
                baselineLevel: levels[cell], baselineMaximumLevel: -1,
                baselineTemporaryLevel: 0, baselineLevelMultiplier: 0,
                baselineDisableCount: 0, baselineCriteriaBypassCount: 0,
                enchantLevel: 0, fixedLevel: 0, fixedDisableCount: 0,
                fixedCriteriaBypassCount: 0, fixedLevelMultiplier: 0,
                tabletLevel: 0, tabletDisableCount: 0,
                tabletCriteriaBypassCount: 0, tabletLevelMultiplier: 0);
            cells[cell] = new InventoryCellSnapshot(cell, cell, 0,
                levels[cell], maximumLevel, temporaryLevel: 0,
                levelMultiplier: 0, disableCount: 0,
                ignoreCriteriaCount: 0, mystic: false, settlement);
        }
        return new InventorySnapshot(width: 6, storage, cells, items);
    }

    private static bool EvaluateOneRowCondition(
        ArtifactActivationConditionKind condition, int cell,
        bool[] occupied)
    {
        return condition switch
        {
            ArtifactActivationConditionKind.None => true,
            ArtifactActivationConditionKind.BothSidesArtifacts =>
                cell > 0 && cell < 5 && occupied[cell - 1] &&
                occupied[cell + 1],
            _ => false
        };
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
