using SephiriaEnhancements.Runtime.Inventory;
using SephiriaEnhancements.Inventory;

namespace SephiriaEnhancements.ModelChecks.Runtime.Inventory;

internal static class InventorySnapshotFixture
{
    internal static InventorySnapshot ArtifactsAtLevels(int[] levels,
        int[] itemCells, int maxLevel = 10)
    {
        var cells = new InventoryCellSnapshot[levels.Length];
        for (int cell = 0; cell < levels.Length; cell++)
        {
            bool occupied = Array.IndexOf(itemCells, cell) >= 0;
            InventoryCellSettlementSnapshot settlement = Settlement(
                levels[cell]);
            cells[cell] = new InventoryCellSnapshot(cell, cell, 0,
                levels[cell], occupied ? maxLevel : -1, temporaryLevel: 0,
                levelMultiplier: 0, disableCount: 0,
                ignoreCriteriaCount: 0, mystic: false, settlement);
        }

        var items = new InventoryItemSnapshot[itemCells.Length];
        for (int index = 0; index < itemCells.Length; index++)
        {
            int cell = itemCells[index];
            bool enabled = levels[cell] >= 0;
            items[index] = ArtifactItem(100 + index, 1000 + index, cell,
                levels.Length, levels[cell], maxLevel, enchantLevel: 0,
                enabled);
        }
        return RequireValid(new InventorySnapshot(levels.Length,
            levels.Length, cells, items), "artifact-level");
    }

    internal static InventorySnapshot SingleArtifactWithContributions(
        string scenario, int baselineLevel, int enchantLevel, int multiplier,
        int disableCount, int globalActiveValue, int maxLevel,
        bool enabled, int cappedLevel)
    {
        int additiveLevel = baselineLevel + enchantLevel;
        int displayedLevel = multiplier == 0
            ? additiveLevel
            : additiveLevel * multiplier;
        var settlement = new InventoryCellSettlementSnapshot(
            baselineKnown: true, baselineLevel,
            baselineMaximumLevel: -1, baselineTemporaryLevel: 0,
            baselineLevelMultiplier: 0, baselineDisableCount: 0,
            baselineCriteriaBypassCount: 0, enchantLevel,
            fixedLevel: 0, fixedDisableCount: disableCount,
            fixedCriteriaBypassCount: 0,
            fixedLevelMultiplier: multiplier,
            tabletLevel: 0, tabletDisableCount: 0,
            tabletCriteriaBypassCount: 0, tabletLevelMultiplier: 0);
        var cell = new InventoryCellSnapshot(0, 0, 0, displayedLevel,
            maxLevel, temporaryLevel: 0, levelMultiplier: multiplier,
            disableCount, ignoreCriteriaCount: 0, mystic: false,
            settlement);
        InventoryItemSnapshot item = ArtifactItem(100, 1000, 0, 1,
            displayedLevel, maxLevel, enchantLevel, enabled, cappedLevel,
            scenario);
        return RequireValid(new InventorySnapshot(1, 1, new[] { cell },
            new[] { item }, globalActiveValue: globalActiveValue), scenario);
    }

    internal static InventorySnapshot UnregisteredUniqueArtifactAtLevels(
        int[] levels, int itemCell, int maxLevel = 10)
    {
        var cells = new InventoryCellSnapshot[levels.Length];
        for (int cell = 0; cell < levels.Length; cell++)
        {
            bool occupied = cell == itemCell;
            cells[cell] = new InventoryCellSnapshot(cell, cell, 0,
                levels[cell], occupied ? maxLevel : -1, temporaryLevel: 0,
                levelMultiplier: 0, disableCount: 0,
                ignoreCriteriaCount: 0, mystic: false,
                Settlement(levels[cell]));
        }

        InventoryItemSnapshot item = ArtifactItem(100, 1000, itemCell,
            levels.Length, levels[itemCell], maxLevel, enchantLevel: 0,
            enabled: false, name: "Unregistered Unique Artifact",
            uniqueEffect: true, uniqueEffectRegistered: false);
        return RequireValid(new InventorySnapshot(levels.Length,
            levels.Length, cells, new[] { item }),
            "unregistered-unique-artifact");
    }

    internal static InventorySnapshot DuplicateArtifactsAtLevels(
        int[] levels, int[] itemCells, int maxLevel = 10)
    {
        var cells = new InventoryCellSnapshot[levels.Length];
        for (int cell = 0; cell < levels.Length; cell++)
        {
            bool occupied = Array.IndexOf(itemCells, cell) >= 0;
            cells[cell] = new InventoryCellSnapshot(cell, cell, 0,
                levels[cell], occupied ? maxLevel : -1, temporaryLevel: 0,
                levelMultiplier: 0, disableCount: 0,
                ignoreCriteriaCount: 0, mystic: false, Settlement(levels[cell]));
        }

        var items = new InventoryItemSnapshot[itemCells.Length];
        for (int index = 0; index < itemCells.Length; index++)
        {
            int cell = itemCells[index];
            items[index] = ArtifactItem(100 + index, entityId: 1000, cell,
                levels.Length, levels[cell], maxLevel, enchantLevel: 0,
                enabled: levels[cell] >= 0, name: "Duplicate Artifact");
        }
        return RequireValid(new InventorySnapshot(levels.Length,
            levels.Length, cells, items), "duplicate-artifact-level");
    }

    internal static InventorySnapshot FullWithArtifactAndBlockers(int width,
        int storage, int artifactCell, int targetLevelCell)
    {
        var cells = new InventoryCellSnapshot[storage];
        var items = new InventoryItemSnapshot[storage];
        for (int cell = 0; cell < storage; cell++)
        {
            int level = cell == targetLevelCell ? 0 : -1;
            InventoryCellSettlementSnapshot settlement = Settlement(level);
            cells[cell] = new InventoryCellSnapshot(cell, cell % width,
                cell / width, level, cell == artifactCell ? 5 : -1,
                temporaryLevel: 0, levelMultiplier: 0, disableCount: 0,
                ignoreCriteriaCount: 0, mystic: false, settlement);
            if (cell == artifactCell)
            {
                items[cell] = ArtifactItem(100, 1000, cell, width, level,
                    maxLevel: 5, enchantLevel: 0, enabled: level >= 0,
                    name: "Full Bag Artifact");
            }
            else
            {
                items[cell] = new InventoryItemSnapshot(100 + cell,
                    1000 + cell, quantity: 1, cellIndex: cell,
                    x: cell % width, y: cell / width, name: "Blocker",
                    nameKey: string.Empty, nativeItemTypeName: "Misc",
                    rarity: "Normal", baseCategories: Array.Empty<string>(),
                    kind: InventoryItemKind.Other, artifact: null,
                    stoneTablet: null);
            }
        }
        return RequireValid(new InventorySnapshot(width, storage, cells,
            items), "full inventory");
    }

    private static InventoryItemSnapshot ArtifactItem(int instanceId,
        int entityId, int cell, int width, int displayedLevel, int maxLevel,
        int enchantLevel, bool enabled, int? cappedLevel = null,
        string name = "Boundary Artifact", bool uniqueEffect = false,
        bool uniqueEffectRegistered = false)
    {
        int effectiveLevel = cappedLevel ??
            (enabled ? Math.Min(displayedLevel, maxLevel) : 0);
        var artifact = new ArtifactSnapshot(displayedLevel, maxLevel,
            enchantLevel, effectEnabledLevel: displayedLevel,
            limitedEffectEnabledLevel: effectiveLevel,
            effectEnabled: enabled, penaltyEnabled: !enabled,
            weaponRestricted: false, requiredWeapon: string.Empty,
            weaponCompatible: true, uniqueEffect,
            uniqueEffectRegistered, calculationOrder: "Default",
            new CriteriaSnapshot(ArtifactActivationConditionKind.None,
                CriteriaEvaluationState.NotApplicable,
                CriteriaEvaluationState.NotApplicable),
            Array.Empty<string>(), Array.Empty<string>(), attackable: false,
            magic: null);
        return new InventoryItemSnapshot(instanceId, entityId, quantity: 1,
            cellIndex: cell, x: cell % width, y: cell / width, name,
            nameKey: string.Empty, nativeItemTypeName: "Charm",
            rarity: "Normal", baseCategories: Array.Empty<string>(),
            kind: InventoryItemKind.Artifact, artifact, stoneTablet: null);
    }

    private static InventoryCellSettlementSnapshot Settlement(int level) =>
        new(baselineKnown: true, baselineLevel: level,
            baselineMaximumLevel: -1, baselineTemporaryLevel: 0,
            baselineLevelMultiplier: 0, baselineDisableCount: 0,
            baselineCriteriaBypassCount: 0, enchantLevel: 0,
            fixedLevel: 0, fixedDisableCount: 0,
            fixedCriteriaBypassCount: 0, fixedLevelMultiplier: 0,
            tabletLevel: 0, tabletDisableCount: 0,
            tabletCriteriaBypassCount: 0, tabletLevelMultiplier: 0);

    private static InventorySnapshot RequireValid(InventorySnapshot snapshot,
        string scenario)
    {
        if (!snapshot.SettlementValidation.LayoutProjectionReady)
        {
            throw new InvalidOperationException(scenario +
                " fixture invalid: " + string.Join(",",
                    snapshot.SettlementValidation.Issues));
        }
        return snapshot;
    }

    internal static InventorySnapshot WithRestrictedArtifact(
        out InventoryCellSnapshot[] inventoryCells,
        out InventoryItemSnapshot[] inventoryItems)
    {
        inventoryCells = new[]
        {
            new InventoryCellSnapshot(0, 0, 0, 2, 5, 0, 0, 0, 0, false),
            new InventoryCellSnapshot(1, 1, 0, -1, 5, 0, 2, 2, 1, true),
            new InventoryCellSnapshot(2, 0, 1, 1, 5, 0, 0, 0, 0, false)
        };
        var restrictedCriteria = new CriteriaSnapshot(
            ArtifactActivationConditionKind.Unknown,
            CriteriaEvaluationState.Unsatisfied, CriteriaEvaluationState.Satisfied);
        var restrictedArtifact = new ArtifactSnapshot(-1, 5, 2, -1, 0,
            false, true, false, "", true, false, false, "Default",
            restrictedCriteria, new[] { "EMBER" }, new[] { "EMBER", "GLACIER" },
            false, null);
        inventoryItems = new[]
        {
            new InventoryItemSnapshot(10, 100, 1, 0, 0, 0, "Ordinary", "Item_Ordinary_Name",
                "Charm", "Rare", new[] { "STURDY" }, InventoryItemKind.Artifact,
                new ArtifactSnapshot(2, 5, 0, 2, 2, true, false, false, "",
                    true, false, false, "Default",
                    new CriteriaSnapshot(ArtifactActivationConditionKind.None,
                        CriteriaEvaluationState.NotApplicable,
                        CriteriaEvaluationState.NotApplicable),
                    new[] { "STURDY" }, new[] { "STURDY" }, false, null), null),
            new InventoryItemSnapshot(11, 101, 1, 1, 1, 0, "Restricted",
                "Item_Restricted_Name", "Charm", "Legend", new[] { "EMBER" },
                InventoryItemKind.RestrictedArtifact, restrictedArtifact, null)
        };
        var presetSnapshot = new NativePresetSnapshot(2, true, "Fire", 7, "Scholar",
            new[] { 101 }, new[] { "EMBER" });
        var comboSnapshots = new[]
        {
            new ComboCategorySnapshot("EMBER", 3, 3, 1, 1, 1,
                new[] { 2, 4, 6 }, new[] { 2, 4 }, true, 4, 2, 0)
        };
        return new InventorySnapshot(2, 3, inventoryCells, inventoryItems,
            true, 1, presetSnapshot, comboSnapshots, true, 2,
            unlimitedComboStatValue: 1);
    }

    internal static InventorySnapshot RowDependentArtifact()
    {
        var zeroSettlement = new InventoryCellSettlementSnapshot(true, 0, -1, 0,
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);
        var levelTwoSettlement = new InventoryCellSettlementSnapshot(true, 2, 2, 0,
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);
        var rowArtifact = new ArtifactSnapshot(0, 2, 0,
            0, 0, true, false, false, "", true, false, false, "Pre",
            new CriteriaSnapshot(ArtifactActivationConditionKind.None,
                CriteriaEvaluationState.NotApplicable,
                CriteriaEvaluationState.NotApplicable), new[] { "FIRE" },
            new[] { "FIRE", "ICE" }, true, null,
            new ArtifactCategoryRuleSnapshot(ArtifactCategoryRuleKind.RowModulo,
                new[] { "FIRE", "ICE" }));
        return new InventorySnapshot(2, 4,
            new[]
            {
                new InventoryCellSnapshot(0, 0, 0, 0, 2, 0, 0, 0, 0, false,
                    zeroSettlement),
                new InventoryCellSnapshot(1, 1, 0, 0, -1, 0, 0, 0, 0, false,
                    zeroSettlement),
                new InventoryCellSnapshot(2, 0, 1, 2, 2, 0, 0, 0, 0, false,
                    levelTwoSettlement),
                new InventoryCellSnapshot(3, 1, 1, 0, -1, 0, 0, 0, 0, false,
                    zeroSettlement)
            },
            new[] { new InventoryItemSnapshot(31, 301, 1, 0, 0, 0, "Row",
                "Item_Row", "Charm", "Rare", Array.Empty<string>(),
                InventoryItemKind.Artifact, rowArtifact, null) },
            comboCategories: new[]
            {
                new ComboCategorySnapshot("FIRE", 1, 1, 1, 0, 0,
                    Array.Empty<int>(), Array.Empty<int>(), false),
                new ComboCategorySnapshot("ICE", 0, 0, 0, 0, 0,
                    new[] { 1 }, Array.Empty<int>(), false)
            });
    }
}
