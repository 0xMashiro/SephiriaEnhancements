using SephiriaEnhancements.Runtime.Inventory;

namespace SephiriaEnhancements.ModelChecks.Runtime.Inventory;

internal static class InventorySettlementProjectorChecks
{
    internal static void Run()
    {
        InventorySnapshot rowSnapshot = InventorySnapshotFixture.RowDependentArtifact();
        ProjectedInventorySettlement movedRow =
            InventorySettlementProjector.Evaluate(rowSnapshot,
                new InventoryLayoutProjection(new[] { 2 }, new[] { 0 }));
        if (!rowSnapshot.SettlementValidation.LayoutProjectionReady || !movedRow.Succeeded ||
            movedRow.ComboCounts["FIRE"] != 0 || movedRow.ComboCounts["ICE"] != 1)
            throw new InvalidOperationException(
                "row-dependent categories must follow the candidate row");
        VerifyCategoryWorkspaceReuse();
        Console.WriteLine("InventorySettlementProjector: dynamic categories, dependency cycles and retained results passed");
    }

    private static void VerifyCategoryWorkspaceReuse()
    {
        var mixed = CategoryBoard(new[] {
            new ArtifactCategoryRuleSnapshot(ArtifactCategoryRuleKind.RowModulo, new[] { "FIRE", "ICE" }),
            new ArtifactCategoryRuleSnapshot(ArtifactCategoryRuleKind.DependencyTarget, targetX: -1),
            new ArtifactCategoryRuleSnapshot(ArtifactCategoryRuleKind.NeighborMatch,
                neighborOffsets: new[] { new InventoryOffsetSnapshot(0, -1) }, match: 1),
            ArtifactCategoryRuleSnapshot.Static },
            new[] { new[] { "FIRE" }, new[] { "FIRE" }, new[] { "FIRE" }, new[] { "ICE" } });
        Verify(mixed, new[] { 2, 1, 0, 3 }, 3, 1, 0, 2);

        var cycle = CategoryBoard(new[] {
            new ArtifactCategoryRuleSnapshot(ArtifactCategoryRuleKind.DependencyTarget, targetX: 1),
            new ArtifactCategoryRuleSnapshot(ArtifactCategoryRuleKind.DependencyTarget, targetX: -1),
            ArtifactCategoryRuleSnapshot.Static, ArtifactCategoryRuleSnapshot.Static },
            new[] { Array.Empty<string>(), Array.Empty<string>(), new[] { "FIRE" }, new[] { "ICE" } });
        Verify(cycle, new[] { 0, 2, 1, 3 }, 1, 1, 2, 1);

        static void Verify(InventorySnapshot snapshot, int[] movedCells,
            int beforeFire, int beforeIce, int afterFire, int afterIce)
        {
            if (!snapshot.SettlementValidation.LayoutProjectionReady)
                throw new InvalidOperationException(string.Join(";", snapshot.SettlementValidation.Issues));
            var workspace = new InventorySettlementProjectionWorkspace(snapshot);
            var current = InventoryLayoutProjection.Current(snapshot);
            var moved = new InventoryLayoutProjection(movedCells, new int[movedCells.Length]);
            var before = InventorySettlementProjector.EvaluateForScoring(snapshot, current, workspace);
            var after = InventorySettlementProjector.EvaluateForScoring(snapshot, moved, workspace);
            var full = InventorySettlementProjector.Evaluate(snapshot, moved);
            var restored = InventorySettlementProjector.EvaluateForScoring(snapshot, current, workspace);
            Check(before, beforeFire, beforeIce);
            Check(after, afterFire, afterIce);
            Check(full, afterFire, afterIce);
            Check(restored, beforeFire, beforeIce);
        }
        static void Check(ProjectedInventorySettlement settlement, int fire, int ice)
        {
            if (!settlement.Succeeded || settlement.ComboCounts["FIRE"] != fire || settlement.ComboCounts["ICE"] != ice)
                throw new InvalidOperationException("category workspace must clear prior candidates and preserve returned results");
        }
    }

    private static InventorySnapshot CategoryBoard(ArtifactCategoryRuleSnapshot[] rules, string[][] categories)
    {
        var items = Enumerable.Range(0, 4).Select(index =>
        {
            var artifact = new ArtifactSnapshot(0, 3, 0, 0, 0, true, false, false, "", true, false, false, "Pre",
                new CriteriaSnapshot(ArtifactActivationConditionKind.None, CriteriaEvaluationState.NotApplicable,
                    CriteriaEvaluationState.NotApplicable), categories[index], new[] { "FIRE", "ICE" }, true, null, rules[index]);
            return new InventoryItemSnapshot(index, 7000 + index, 1, index, index % 2, index / 2, "Test", "",
                "Charm", "Normal", categories[index], InventoryItemKind.Artifact, artifact, null);
        }).ToArray();
        var cells = Enumerable.Range(0, 4).Select(index => new InventoryCellSnapshot(index, index % 2, index / 2,
            0, 3, 0, 0, 0, 0, false, new InventoryCellSettlementSnapshot(true, 0, -1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0))).ToArray();
        var combos = new[] { "FIRE", "ICE" }.Select(category =>
        {
            int count = categories.Count(values => values.Contains(category));
            return new ComboCategorySnapshot(category, count, count, count, 0, 0, Array.Empty<int>(), Array.Empty<int>(), false);
        }).ToArray();
        return new InventorySnapshot(2, 4, cells, items, comboCategories: combos);
    }
}
