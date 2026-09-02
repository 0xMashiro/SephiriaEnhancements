using SephiriaEnhancements.Runtime.Inventory;
using SephiriaEnhancements.Inventory;

internal static class InventoryCapacitySemanticsChecks
{
    private const int Width = 6;

    internal static string Run()
    {
        foreach ((int storage, int expectedHeight) in new[]
        {
            (24, 4), (30, 5), (32, 6), (36, 6), (42, 7)
        })
        {
            InventorySnapshot snapshot = CreateEmptySnapshot(storage);
            if (snapshot.Width != Width ||
                snapshot.Storage != storage ||
                snapshot.Height != expectedHeight)
            {
                throw new InvalidOperationException(
                    "inventory capacity geometry failed for storage=" + storage);
            }
        }

        InventorySnapshot partial = CreateEmptySnapshot(storage: 32);
        if (!partial.TryGetCell(1, 5, out InventoryCellSnapshot lastCell) ||
            lastCell.Index != 31 || partial.TryGetCell(2, 5, out _))
        {
            throw new InvalidOperationException(
                "partial final row validity failed for storage=32");
        }

        AssertCondition(ArtifactActivationConditionKind.BottomRow,
            currentCell: 31, expectedSatisfiedCells: new[] { 26, 31 },
            expectedUnsatisfiedCells: new[] { 25 });
        AssertCondition(ArtifactActivationConditionKind.Border,
            currentCell: 31, expectedSatisfiedCells: new[] { 26, 31 },
            expectedUnsatisfiedCells: new[] { 25 });
        AssertCondition(ArtifactActivationConditionKind.Interior,
            currentCell: 19, expectedSatisfiedCells: new[] { 19, 22 },
            expectedUnsatisfiedCells: new[] { 25 });
        AssertCondition(ArtifactActivationConditionKind.SideEdge,
            currentCell: 30, expectedSatisfiedCells: new[] { 30 },
            expectedUnsatisfiedCells: new[] { 31 });
        AssertCondition(ArtifactActivationConditionKind.BothSidesEmpty,
            currentCell: 25, expectedSatisfiedCells: new[] { 25 },
            expectedUnsatisfiedCells: new[] { 31 });

        return "capacities=24,30,32,36,42;partialRow=5x6+2;" +
            "positionalConditions=5";
    }

    private static void AssertCondition(
        ArtifactActivationConditionKind condition, int currentCell,
        int[] expectedSatisfiedCells, int[] expectedUnsatisfiedCells)
    {
        InventorySnapshot snapshot = CreateConditionSnapshot(condition,
            currentCell);
        foreach (int cell in expectedSatisfiedCells)
        {
            AssertEnabled(snapshot, cell, expected: true, condition);
        }
        foreach (int cell in expectedUnsatisfiedCells)
        {
            AssertEnabled(snapshot, cell, expected: false, condition);
        }
    }

    private static void AssertEnabled(InventorySnapshot snapshot, int cell,
        bool expected, ArtifactActivationConditionKind condition)
    {
        ProjectedInventorySettlement settlement =
            InventorySettlementProjector.Evaluate(snapshot,
                new InventoryLayoutProjection(new[] { cell }, new[] { 0 }));
        if (!settlement.Succeeded || settlement.Artifacts.Count != 1 ||
            settlement.Artifacts[0].Enabled != expected)
        {
            throw new InvalidOperationException(
                "partial-row condition failed: " + condition +
                ", cell=" + cell + ", expected=" + expected +
                ", succeeded=" + settlement.Succeeded +
                ", enabled=" + (settlement.Artifacts.Count == 1
                    ? settlement.Artifacts[0].Enabled
                    : false) + ", issues=" +
                string.Join(",", settlement.Issues));
        }
    }

    private static InventorySnapshot CreateEmptySnapshot(int storage)
    {
        return new InventorySnapshot(Width, storage, CreateCells(storage,
            occupiedCell: -1), Array.Empty<InventoryItemSnapshot>());
    }

    private static InventorySnapshot CreateConditionSnapshot(
        ArtifactActivationConditionKind condition, int currentCell)
    {
        InventoryCellSnapshot[] cells = CreateCells(storage: 32, currentCell);
        var criteria = new CriteriaSnapshot(condition,
            CriteriaEvaluationState.Satisfied,
            CriteriaEvaluationState.NotApplicable);
        var artifact = new ArtifactSnapshot(displayedLevel: 1, maxLevel: 1,
            enchant: 0, effectEnabledLevel: 1,
            limitedEffectEnabledLevel: 1, effectEnabled: true,
            penaltyEnabled: false, weaponRestricted: false,
            requiredWeapon: string.Empty, weaponCompatible: true,
            uniqueEffect: false, uniqueEffectRegistered: false,
            calculationOrder: "Pre", criteria,
            Array.Empty<string>(), Array.Empty<string>(),
            attackable: false, magic: null);
        var item = new InventoryItemSnapshot(instanceId: 1, entityId: 101,
            quantity: 1, cellIndex: currentCell,
            x: currentCell % Width, y: currentCell / Width,
            name: "Condition Artifact", nameKey: "",
            nativeItemTypeName: "Charm", rarity: "Normal",
            baseCategories: Array.Empty<string>(),
            kind: InventoryItemKind.RestrictedArtifact, artifact,
            stoneTablet: null);
        return new InventorySnapshot(Width, storage: 32, cells,
            new[] { item });
    }

    private static InventoryCellSnapshot[] CreateCells(int storage,
        int occupiedCell)
    {
        var cells = new InventoryCellSnapshot[storage];
        for (int cell = 0; cell < storage; cell++)
        {
            var settlement = new InventoryCellSettlementSnapshot(
                baselineKnown: true, baselineLevel: 1,
                baselineMaximumLevel: -1, baselineTemporaryLevel: 0,
                baselineLevelMultiplier: 0, baselineDisableCount: 0,
                baselineCriteriaBypassCount: 0, enchantLevel: 0,
                fixedLevel: 0, fixedDisableCount: 0,
                fixedCriteriaBypassCount: 0, fixedLevelMultiplier: 0,
                tabletLevel: 0, tabletDisableCount: 0,
                tabletCriteriaBypassCount: 0, tabletLevelMultiplier: 0);
            cells[cell] = new InventoryCellSnapshot(cell, cell % Width,
                cell / Width, level: 1,
                maxLevel: cell == occupiedCell ? 1 : -1,
                temporaryLevel: 0, levelMultiplier: 0, disableCount: 0,
                ignoreCriteriaCount: 0, mystic: false, settlement);
        }
        return cells;
    }
}
