using SephiriaEnhancements.Runtime.Inventory;
using SephiriaEnhancements.Inventory;

namespace SephiriaEnhancements.ModelChecks.Features.Inventory;

internal static class InventorySolverConformanceChecks
{
    internal static string Run()
    {
        var scenarios = new[]
        {
            new Scenario("EnchantedArtifact", CreateEnchantedArtifact()),
            new Scenario("RestrictedTopRow", CreateRestrictedTopRow()),
            new Scenario("RowDependentCombo", CreateRowDependentCombo()),
            new Scenario("BothSidesArtifacts",
                InventoryNeighborhoodFixture.BothSidesArtifacts()),
            new Scenario("StoneTabletMoveAndRotation",
                InventoryNeighborhoodFixture.
                    StoneTabletMoveAndRotation()),
            new Scenario("ComboAndStoneTabletInteraction",
                CreateComboAndStoneTabletInteraction())
        };

        int candidateLayouts = 0;
        foreach (Scenario scenario in scenarios)
        {
            ResolvedInventoryOptimizationPolicy policy =
                InventoryOptimizationPolicyResolver.Resolve(scenario.Snapshot,
                    InventoryOptimizationPreferences.Default);
            long estimated = InventoryExhaustiveSearchOracle.
                EstimateCandidateLayouts(scenario.Snapshot);
            InventoryExhaustiveSearchResult exact =
                InventoryExhaustiveSearchOracle.Solve(scenario.Snapshot, policy,
                    new InventoryExhaustiveSearchLimits(
                        maximumCandidateLayouts: 10000,
                        maximumElapsedMilliseconds: 5000));
            InventoryOptimizationProposal heuristic = InventoryOptimizer.Solve(
                scenario.Snapshot, policy,
                new InventorySearchBudget(maximumImprovementRounds: 16,
                    maximumCandidateEvaluations: 10000,
                    maximumElapsedMilliseconds: 5000));

            if (!exact.ProvenOptimal || !heuristic.Succeeded ||
                !heuristic.Improved ||
                heuristic.BestScore.CompareTo(exact.BestScore) != 0 ||
                estimated != exact.CandidateLayoutsEvaluated ||
                !InventoryLayoutPlanner.TryCreate(scenario.Snapshot,
                    heuristic.Layout, out InventoryApplicationPlan _,
                    out string issue) || issue != string.Empty)
            {
                throw new InvalidOperationException(
                    "inventory solver conformance failed: " + scenario.Name +
                    ";" + Describe(scenario.Snapshot, exact, heuristic));
            }
            candidateLayouts += exact.CandidateLayoutsEvaluated;
        }

        const int randomizedScenarioCount = 192;
        var random = new Random(0x5E71);
        for (int scenarioIndex = 0; scenarioIndex < randomizedScenarioCount;
            scenarioIndex++)
        {
            InventorySnapshot snapshot = CreateRandomizedSmallScenario(random,
                scenarioIndex);
            ResolvedInventoryOptimizationPolicy policy =
                InventoryOptimizationPolicyResolver.Resolve(snapshot,
                    InventoryOptimizationPreferences.Default);
            InventoryExhaustiveSearchResult exact =
                InventoryExhaustiveSearchOracle.Solve(snapshot, policy,
                    new InventoryExhaustiveSearchLimits(
                        maximumCandidateLayouts: 10000,
                        maximumElapsedMilliseconds: 5000));
            InventoryOptimizationProposal heuristic = InventoryOptimizer.Solve(
                snapshot, policy,
                new InventorySearchBudget(maximumImprovementRounds: 16,
                    maximumCandidateEvaluations: 10000,
                    maximumElapsedMilliseconds: 5000));

            if (!exact.ProvenOptimal || !heuristic.Succeeded ||
                heuristic.BestScore.CompareTo(exact.BestScore) != 0 ||
                exact.EstimatedCandidateLayouts !=
                    exact.CandidateLayoutsEvaluated ||
                !InventoryLayoutPlanner.TryCreate(snapshot, heuristic.Layout,
                    out InventoryApplicationPlan _, out string issue) ||
                issue != string.Empty)
            {
                throw new InvalidOperationException(
                    "randomized inventory solver conformance failed: seed=" +
                    scenarioIndex + ";" + Describe(snapshot, exact, heuristic));
            }
            candidateLayouts += exact.CandidateLayoutsEvaluated;
        }

        return "scenarios=" + scenarios.Length +
            ";randomized=" + randomizedScenarioCount +
            ";exhaustiveLayouts=" + candidateLayouts;
    }

    private static InventorySnapshot CreateRandomizedSmallScenario(
        Random random, int scenarioIndex)
    {
        const int width = 6;
        const int storage = 12;
        int itemCount = scenarioIndex % 3 == 0 ? 3 : 2;
        var itemCells = new int[itemCount];
        for (int itemIndex = 0; itemIndex < itemCells.Length; itemIndex++)
        {
            int cell;
            do
            {
                cell = random.Next(storage);
            }
            while (Array.IndexOf(itemCells, cell, 0, itemIndex) >= 0);
            itemCells[itemIndex] = cell;
        }
        int[] baselineLevels = new int[storage];
        for (int cell = 0; cell < storage; cell++)
        {
            baselineLevels[cell] = random.Next(4);
        }

        var items = new InventoryItemSnapshot[itemCells.Length];
        var artifacts = new ArtifactSnapshot[itemCells.Length];
        var enchants = new int[itemCells.Length];
        var maximumLevels = new int[itemCells.Length];
        var conditions = new ArtifactActivationConditionKind[itemCells.Length];
        ArtifactActivationConditionKind[] supportedConditions =
        {
            ArtifactActivationConditionKind.None,
            ArtifactActivationConditionKind.TopRow,
            ArtifactActivationConditionKind.BottomRow,
            ArtifactActivationConditionKind.SideEdge,
            ArtifactActivationConditionKind.Interior,
            ArtifactActivationConditionKind.Border,
            ArtifactActivationConditionKind.BothSidesEmpty,
            ArtifactActivationConditionKind.BothSidesArtifacts
        };

        for (int itemIndex = 0; itemIndex < itemCells.Length; itemIndex++)
        {
            int cell = itemCells[itemIndex];
            int displayedLevel = baselineLevels[cell] + random.Next(3);
            enchants[itemIndex] = displayedLevel - baselineLevels[cell];
            maximumLevels[itemIndex] = random.Next(1, 5);
            conditions[itemIndex] = supportedConditions[random.Next(
                supportedConditions.Length)];
            bool criteriaSatisfied = IsCriteriaSatisfied(conditions[itemIndex],
                cell, itemCells);
            CriteriaEvaluationState runtimeState = conditions[itemIndex] ==
                    ArtifactActivationConditionKind.None
                ? CriteriaEvaluationState.NotApplicable
                : criteriaSatisfied
                    ? CriteriaEvaluationState.Satisfied
                    : CriteriaEvaluationState.Unsatisfied;
            bool enabled = criteriaSatisfied;
            artifacts[itemIndex] = CreateArtifact(displayedLevel,
                maximumLevels[itemIndex], enchants[itemIndex],
                conditions[itemIndex], runtimeState,
                effectiveCategories: Array.Empty<string>(),
                categoryRule: ArtifactCategoryRuleSnapshot.Static,
                effectEnabled: enabled);
            InventoryItemKind kind = conditions[itemIndex] ==
                    ArtifactActivationConditionKind.None
                ? InventoryItemKind.Artifact
                : InventoryItemKind.RestrictedArtifact;
            int instanceId = scenarioIndex * 10 + itemIndex + 1000;
            items[itemIndex] = new InventoryItemSnapshot(instanceId,
                entityId: 2000 + itemIndex, quantity: 1, cell,
                x: cell % width, y: cell / width,
                "Artifact " + instanceId, "Artifact_" + instanceId,
                "Charm", "Normal", Array.Empty<string>(), kind,
                artifacts[itemIndex], stoneTablet: null);
        }

        var cells = new InventoryCellSnapshot[storage];
        for (int cell = 0; cell < storage; cell++)
        {
            int itemIndex = Array.IndexOf(itemCells, cell);
            int enchant = itemIndex >= 0 ? enchants[itemIndex] : 0;
            int maximumLevel = itemIndex >= 0
                ? maximumLevels[itemIndex]
                : -1;
            var settlement = new InventoryCellSettlementSnapshot(true,
                baselineLevel: baselineLevels[cell], baselineMaximumLevel: -1,
                baselineTemporaryLevel: 0, baselineLevelMultiplier: 0,
                baselineDisableCount: 0, baselineCriteriaBypassCount: 0,
                enchantLevel: enchant, fixedLevel: 0, fixedDisableCount: 0,
                fixedCriteriaBypassCount: 0, fixedLevelMultiplier: 0,
                tabletLevel: 0, tabletDisableCount: 0,
                tabletCriteriaBypassCount: 0, tabletLevelMultiplier: 0);
            cells[cell] = new InventoryCellSnapshot(cell, cell % width,
                cell / width, baselineLevels[cell] + enchant, maximumLevel,
                temporaryLevel: 0, levelMultiplier: 0, disableCount: 0,
                ignoreCriteriaCount: 0, mystic: false, settlement);
        }

        return new InventorySnapshot(width, storage, cells, items);
    }

    private static bool IsCriteriaSatisfied(
        ArtifactActivationConditionKind condition, int cell,
        int[] occupiedCells)
    {
        int x = cell % 6;
        int y = cell / 6;
        return condition switch
        {
            ArtifactActivationConditionKind.None => true,
            ArtifactActivationConditionKind.TopRow => y == 0,
            ArtifactActivationConditionKind.BottomRow => cell >= 6,
            ArtifactActivationConditionKind.SideEdge => x == 0 || x == 5,
            ArtifactActivationConditionKind.Interior =>
                x > 0 && x < 5 && y > 0 && cell + 7 <= 11,
            ArtifactActivationConditionKind.Border =>
                x == 0 || x == 5 || y == 0 || cell >= 6,
            ArtifactActivationConditionKind.BothSidesEmpty =>
                x > 0 && x < 5 &&
                Array.IndexOf(occupiedCells, cell - 1) < 0 &&
                Array.IndexOf(occupiedCells, cell + 1) < 0,
            ArtifactActivationConditionKind.BothSidesArtifacts =>
                x > 0 && x < 5 &&
                Array.IndexOf(occupiedCells, cell - 1) >= 0 &&
                Array.IndexOf(occupiedCells, cell + 1) >= 0,
            _ => false
        };
    }

    private static string Describe(InventorySnapshot snapshot,
        InventoryExhaustiveSearchResult exact,
        InventoryOptimizationProposal heuristic)
    {
        string items = string.Join(",", snapshot.Items.Select(item =>
            item.Artifact == null
                ? item.CellIndex + ":Tablet@" + item.StoneTablet.Rotation
                : item.CellIndex + ":" + item.Artifact.Criteria.Kind + ":" +
                    item.Artifact.Enchant + ":" + item.Artifact.MaxLevel));
        return "items=" + items +
            ";exact=" + DescribeLayout(exact.BestLayout) +
            ";heuristic=" + DescribeLayout(heuristic.Layout) +
            ";exactScore=" + DescribeScore(exact.BestScore) +
            ";heuristicScore=" + DescribeScore(heuristic.BestScore) +
            ";termination=" + heuristic.TerminationReason +
            ";evaluations=" + heuristic.CandidateEvaluations;
    }

    private static string DescribeLayout(InventoryLayoutProjection layout)
    {
        return layout == null ? "null" :
            string.Join(",", Enumerable.Range(0, layout.ItemCount)
                .Select(layout.GetCell));
    }

    private static string DescribeScore(InventoryOptimizationScore score)
    {
        return score == null ? "null" :
            score.EnabledArtifactCount + "/" +
            score.CappedEffectiveArtifactLevelTotal + "/" +
            score.ExcessArtifactLevelTotal + "/" + score.MovedItemCount;
    }

    private static InventorySnapshot CreateEnchantedArtifact()
    {
        int[] baselineLevels = { 0, 1, 2, 3 };
        InventoryCellSnapshot[] cells = CreateCells(width: 2, baselineLevels,
            occupiedCells: new[] { 0 }, maximumLevel: 3,
            currentLevels: new[] { 1, 1, 2, 3 }, enchantAtCell: 0,
            enchantLevel: 1);
        ArtifactSnapshot artifact = CreateArtifact(displayedLevel: 1,
            maximumLevel: 3, enchant: 1,
            ArtifactActivationConditionKind.None,
            CriteriaEvaluationState.NotApplicable,
            effectiveCategories: Array.Empty<string>(),
            categoryRule: ArtifactCategoryRuleSnapshot.Static);
        return new InventorySnapshot(width: 2, storage: 4, cells,
            new[] { CreateArtifactItem(1, 101, 0, artifact) });
    }

    private static InventorySnapshot CreateRestrictedTopRow()
    {
        int[] levels = { 1, 1, 1, 1 };
        InventoryCellSnapshot[] cells = CreateCells(width: 2, levels,
            occupiedCells: new[] { 2 }, maximumLevel: 1,
            currentLevels: levels, enchantAtCell: -1, enchantLevel: 0);
        ArtifactSnapshot artifact = CreateArtifact(displayedLevel: 1,
            maximumLevel: 1, enchant: 0,
            ArtifactActivationConditionKind.TopRow,
            CriteriaEvaluationState.Unsatisfied,
            effectiveCategories: Array.Empty<string>(),
            categoryRule: ArtifactCategoryRuleSnapshot.Static,
            effectEnabled: false);
        return new InventorySnapshot(width: 2, storage: 4, cells,
            new[] { CreateArtifactItem(2, 102, 2, artifact,
                InventoryItemKind.RestrictedArtifact) });
    }

    private static InventorySnapshot CreateRowDependentCombo()
    {
        int[] levels = { 1, 1, 1, 1 };
        InventoryCellSnapshot[] cells = CreateCells(width: 2, levels,
            occupiedCells: new[] { 0 }, maximumLevel: 1,
            currentLevels: levels, enchantAtCell: -1, enchantLevel: 0);
        ArtifactSnapshot artifact = CreateArtifact(displayedLevel: 1,
            maximumLevel: 1, enchant: 0,
            ArtifactActivationConditionKind.None,
            CriteriaEvaluationState.NotApplicable,
            effectiveCategories: new[] { "FIRE" },
            categoryRule: new ArtifactCategoryRuleSnapshot(
                ArtifactCategoryRuleKind.RowModulo,
                new[] { "FIRE", "ICE" }));
        var categories = new[]
        {
            new ComboCategorySnapshot("FIRE", 1, 1, 1, 0, 0,
                Array.Empty<int>(), Array.Empty<int>(), false),
            new ComboCategorySnapshot("ICE", 0, 0, 0, 0, 0,
                new[] { 1 }, Array.Empty<int>(), false)
        };
        return new InventorySnapshot(width: 2, storage: 4, cells,
            new[] { CreateArtifactItem(3, 103, 0, artifact) },
            comboCategories: categories);
    }

    private static InventorySnapshot CreateComboAndStoneTabletInteraction()
    {
        const int width = 6;
        const int storage = 12;
        int[] levels = new int[storage];
        InventoryCellSnapshot[] cells = CreateCells(width, levels,
            occupiedCells: new[] { 2, 8 }, maximumLevel: 1,
            currentLevels: levels, enchantAtCell: -1, enchantLevel: 0);

        ArtifactSnapshot rowArtifact = CreateArtifact(displayedLevel: 0,
            maximumLevel: 1, enchant: 0,
            ArtifactActivationConditionKind.None,
            CriteriaEvaluationState.NotApplicable,
            effectiveCategories: new[] { "FIRE" },
            categoryRule: new ArtifactCategoryRuleSnapshot(
                ArtifactCategoryRuleKind.RowModulo,
                new[] { "FIRE", "ICE" }));
        ArtifactSnapshot iceArtifact = CreateArtifact(displayedLevel: 0,
            maximumLevel: 1, enchant: 0,
            ArtifactActivationConditionKind.None,
            CriteriaEvaluationState.NotApplicable,
            effectiveCategories: new[] { "ICE" },
            categoryRule: ArtifactCategoryRuleSnapshot.Static);
        var rowItem = new InventoryItemSnapshot(401, 501, quantity: 1,
            cellIndex: 2, x: 2, y: 0, "Row Artifact", "Row_Artifact",
            "Charm", "Normal", Array.Empty<string>(),
            InventoryItemKind.Artifact, rowArtifact, stoneTablet: null);
        var iceItem = new InventoryItemSnapshot(402, 502, quantity: 1,
            cellIndex: 8, x: 2, y: 1, "Ice Artifact", "Ice_Artifact",
            "Charm", "Normal", new[] { "ICE" },
            InventoryItemKind.Artifact, iceArtifact, stoneTablet: null);

        var placements = new TabletPlacementProjectionSnapshot[storage];
        for (int cell = 0; cell < storage; cell++)
        {
            var rotations = new TabletRotationProjectionSnapshot[4];
            for (int rotation = 0; rotation < rotations.Length; rotation++)
            {
                TabletAdditionSnapshot[] effects = cell == 10 && rotation == 1
                    ? new[]
                    {
                        new TabletAdditionSnapshot(1, 1, "+1",
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
                cell % width, cell / width, rotations);
        }
        var tablet = new StoneTabletSnapshot(rotation: 0, rotatable: true,
            custom: false, applied: true,
            includesCriteriaInMinMaxGrid: false,
            conditionQuery: string.Empty, effectQuery: string.Empty,
            placementProjections: placements);
        var tabletItem = new InventoryItemSnapshot(403, 503, quantity: 1,
            cellIndex: 0, x: 0, y: 0, "Stone Tablet", "Stone_Tablet",
            "StoneTablet", "Normal", Array.Empty<string>(),
            InventoryItemKind.StoneTablet, artifact: null,
            stoneTablet: tablet);

        var categories = new[]
        {
            new ComboCategorySnapshot("FIRE", 1, 1, 1, 0, 0,
                Array.Empty<int>(), Array.Empty<int>(), false),
            new ComboCategorySnapshot("ICE", 1, 1, 1, 0, 0,
                new[] { 2 }, Array.Empty<int>(), false)
        };
        return new InventorySnapshot(width, storage, cells,
            new[] { rowItem, iceItem, tabletItem },
            comboCategories: categories);
    }

    private static InventoryCellSnapshot[] CreateCells(int width,
        int[] baselineLevels, int[] occupiedCells, int maximumLevel,
        int[] currentLevels, int enchantAtCell, int enchantLevel)
    {
        var cells = new InventoryCellSnapshot[baselineLevels.Length];
        for (int cell = 0; cell < cells.Length; cell++)
        {
            bool occupied = Array.IndexOf(occupiedCells, cell) >= 0;
            var settlement = new InventoryCellSettlementSnapshot(true,
                baselineLevel: baselineLevels[cell], baselineMaximumLevel: -1,
                baselineTemporaryLevel: 0, baselineLevelMultiplier: 0,
                baselineDisableCount: 0, baselineCriteriaBypassCount: 0,
                enchantLevel: cell == enchantAtCell ? enchantLevel : 0,
                fixedLevel: 0, fixedDisableCount: 0,
                fixedCriteriaBypassCount: 0, fixedLevelMultiplier: 0,
                tabletLevel: 0, tabletDisableCount: 0,
                tabletCriteriaBypassCount: 0, tabletLevelMultiplier: 0);
            cells[cell] = new InventoryCellSnapshot(cell, cell % width,
                cell / width, currentLevels[cell],
                occupied ? maximumLevel : -1, temporaryLevel: 0,
                levelMultiplier: 0, disableCount: 0,
                ignoreCriteriaCount: 0, mystic: false, settlement);
        }
        return cells;
    }

    private static ArtifactSnapshot CreateArtifact(int displayedLevel,
        int maximumLevel, int enchant,
        ArtifactActivationConditionKind condition,
        CriteriaEvaluationState runtimeState, string[] effectiveCategories,
        ArtifactCategoryRuleSnapshot categoryRule, bool effectEnabled = true)
    {
        return new ArtifactSnapshot(displayedLevel, maximumLevel, enchant,
            effectEnabledLevel: effectEnabled ? Math.Min(displayedLevel,
                maximumLevel) : 0,
            limitedEffectEnabledLevel: effectEnabled ? Math.Min(displayedLevel,
                maximumLevel) : 0,
            effectEnabled, penaltyEnabled: !effectEnabled,
            weaponRestricted: false, requiredWeapon: string.Empty,
            weaponCompatible: true, uniqueEffect: false,
            uniqueEffectRegistered: false, calculationOrder: "Pre",
            new CriteriaSnapshot(condition, runtimeState,
                CriteriaEvaluationState.NotApplicable), effectiveCategories,
            effectiveCategories, attackable: false, magic: null, categoryRule);
    }

    private static InventoryItemSnapshot CreateArtifactItem(int instanceId,
        int entityId, int cell, ArtifactSnapshot artifact,
        InventoryItemKind kind = InventoryItemKind.Artifact)
    {
        return new InventoryItemSnapshot(instanceId, entityId, quantity: 1,
            cell, x: cell % 2, y: cell / 2, "Artifact " + instanceId,
            "Artifact_" + instanceId, "Charm", "Normal",
            Array.Empty<string>(), kind, artifact, stoneTablet: null);
    }

    private sealed record Scenario(string Name, InventorySnapshot Snapshot);
}
