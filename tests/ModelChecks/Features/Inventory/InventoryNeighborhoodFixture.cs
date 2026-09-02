using SephiriaEnhancements.Runtime.Inventory;

namespace SephiriaEnhancements.ModelChecks.Features.Inventory;

internal static class InventoryNeighborhoodFixture
{
    internal static InventorySnapshot BothSidesArtifacts()
    {
        return OneRowArtifacts(new[] { 1, 1, 1, 1, 1, 1 },
            new[]
            {
                ArtifactActivationConditionKind.BothSidesArtifacts,
                ArtifactActivationConditionKind.None,
                ArtifactActivationConditionKind.None
            }, new[] { 2, 0, 4 });
    }

    internal static InventorySnapshot StoneTabletMoveAndRotation()
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

    internal static InventorySnapshot OneRowArtifacts(int[] levels,
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
}
