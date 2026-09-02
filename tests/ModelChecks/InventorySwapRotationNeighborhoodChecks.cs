using SephiriaEnhancements.Runtime.Inventory;
using SephiriaEnhancements.Inventory;

internal static class InventorySwapRotationNeighborhoodChecks
{
    internal static string Run()
    {
        InventorySnapshot snapshot = CreateConformanceScenario();
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

    internal static InventorySnapshot CreateConformanceScenario()
    {
        const int storage = 6;
        var cells = new InventoryCellSnapshot[storage];
        for (int cell = 0; cell < storage; cell++)
        {
            var settlement = new InventoryCellSettlementSnapshot(true,
                baselineLevel: 0, baselineMaximumLevel: -1,
                baselineTemporaryLevel: 0, baselineLevelMultiplier: 0,
                baselineDisableCount: 0, baselineCriteriaBypassCount: 0,
                enchantLevel: 0, fixedLevel: 0, fixedDisableCount: 0,
                fixedCriteriaBypassCount: 0, fixedLevelMultiplier: 0,
                tabletLevel: 0, tabletDisableCount: 0,
                tabletCriteriaBypassCount: 0, tabletLevelMultiplier: 0);
            cells[cell] = new InventoryCellSnapshot(cell, cell, 0,
                level: 0, maxLevel: cell == 2 ? 1 : -1,
                temporaryLevel: 0, levelMultiplier: 0, disableCount: 0,
                ignoreCriteriaCount: 0, mystic: false, settlement);
        }

        var artifact = new ArtifactSnapshot(displayedLevel: 0, maxLevel: 1,
            enchant: 0, effectEnabledLevel: 0,
            limitedEffectEnabledLevel: 0, effectEnabled: true,
            penaltyEnabled: false, weaponRestricted: false,
            requiredWeapon: string.Empty, weaponCompatible: true,
            uniqueEffect: false, uniqueEffectRegistered: false,
            calculationOrder: "Pre",
            new CriteriaSnapshot(ArtifactActivationConditionKind.None,
                CriteriaEvaluationState.NotApplicable,
                CriteriaEvaluationState.NotApplicable),
            Array.Empty<string>(), Array.Empty<string>(), attackable: false,
            magic: null);
        var artifactItem = new InventoryItemSnapshot(instanceId: 101,
            entityId: 201, quantity: 1, cellIndex: 2, x: 2, y: 0,
            name: "Artifact", nameKey: "Artifact", nativeItemTypeName: "Charm",
            rarity: "Normal", baseCategories: Array.Empty<string>(),
            kind: InventoryItemKind.Artifact, artifact,
            stoneTablet: null);

        var placements = new TabletPlacementProjectionSnapshot[storage];
        for (int cell = 0; cell < storage; cell++)
        {
            var rotations = new TabletRotationProjectionSnapshot[4];
            for (int rotation = 0; rotation < rotations.Length; rotation++)
            {
                TabletAdditionSnapshot[] effects = cell == 4 && rotation == 1
                    ? new[]
                    {
                        new TabletAdditionSnapshot(2, 0, "+1",
                            validCell: true, xWorldPosition: false,
                            yWorldPosition: false, borderTop: false,
                            borderRight: false, borderBottom: false,
                            borderLeft: false,
                            effectKind: TabletEffectKind.IncreaseLevel,
                            levelParameter: 1)
                    }
                    : Array.Empty<TabletAdditionSnapshot>();
                rotations[rotation] = new TabletRotationProjectionSnapshot(
                    rotation, Array.Empty<TabletAdditionSnapshot>(), effects,
                    parseSucceeded: true);
            }
            placements[cell] = new TabletPlacementProjectionSnapshot(cell,
                x: cell, y: 0, rotations);
        }
        var stoneTablet = new StoneTabletSnapshot(rotation: 0,
            rotatable: true, custom: false, applied: true,
            includesCriteriaInMinMaxGrid: false,
            conditionQuery: string.Empty, effectQuery: string.Empty,
            placementProjections: placements);
        var stoneTabletItem = new InventoryItemSnapshot(instanceId: 102,
            entityId: 202, quantity: 1, cellIndex: 0, x: 0, y: 0,
            name: "Stone Tablet", nameKey: "StoneTablet",
            nativeItemTypeName: "StoneTablet", rarity: "Normal",
            baseCategories: Array.Empty<string>(),
            kind: InventoryItemKind.StoneTablet, artifact: null, stoneTablet);

        return new InventorySnapshot(width: 6, storage, cells,
            new[] { artifactItem, stoneTabletItem });
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
