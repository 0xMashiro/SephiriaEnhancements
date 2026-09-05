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
        VerifyNeighborMatchThreshold();
        VerifyStaticCategoryWorkspace();
        Console.WriteLine("InventorySettlementProjector: dynamic categories, dependency cycles and retained results passed");
    }

    private static void VerifyNeighborMatchThreshold()
    {
        foreach (int minimumCount in new[] { 1, 2, 3 })
        {
            string[] matched = minimumCount == 1 ? new[] { "FIRE", "ICE" } :
                minimumCount == 2 ? new[] { "FIRE" } : Array.Empty<string>();
            var snapshot = CategoryBoard(new[] {
                new ArtifactCategoryRuleSnapshot(ArtifactCategoryRuleKind.NeighborMatch,
                    neighborOffsets: new[] { new InventoryOffsetSnapshot(1, 0), new InventoryOffsetSnapshot(0, 1) },
                    match: minimumCount), ArtifactCategoryRuleSnapshot.Static,
                ArtifactCategoryRuleSnapshot.Static, ArtifactCategoryRuleSnapshot.Static },
                new[] { matched, new[] { "FIRE", "ICE" }, new[] { "FIRE" }, new[] { "ICE" } });
            var layout = InventoryLayoutProjection.Current(snapshot);
            var workspace = new InventorySettlementProjectionWorkspace(snapshot);
            foreach (var result in new[] { InventorySettlementProjector.Evaluate(snapshot, layout),
                         InventorySettlementProjector.EvaluateForScoring(snapshot, layout, workspace) })
                if (!result.Succeeded || result.ComboCounts["FIRE"] != (minimumCount <= 2 ? 3 : 2) ||
                    result.ComboCounts["ICE"] != (minimumCount == 1 ? 3 : 2))
                    throw new InvalidOperationException("neighbor categories must independently meet the inclusive match threshold");
        }
    }

    private static void VerifyStaticCategoryWorkspace()
    {
        var snapshot = CategoryBoard(Enumerable.Repeat(ArtifactCategoryRuleSnapshot.Static, 4).ToArray(),
            new[] { new[] { "FIRE" }, new[] { "FIRE" }, new[] { "ICE" }, Array.Empty<string>() });
        var workspace = new InventorySettlementProjectionWorkspace(snapshot);
        if (workspace.StaticComboCounts == null)
            throw new InvalidOperationException("static inventory categories must be prepared once");
        for (int first = 0; first < 4; first++)
            for (int second = 0; second < 4; second++)
            {
                var layout = InventoryLayoutProjection.Current(snapshot).WithCellsSwapped(first, second);
                var projected = InventorySettlementProjector.EvaluateForScoring(snapshot, layout, workspace);
                var full = InventorySettlementProjector.Evaluate(snapshot, layout);
                if (!projected.Succeeded || !projected.ComboCounts.OrderBy(pair => pair.Key)
                        .SequenceEqual(full.ComboCounts.OrderBy(pair => pair.Key)) ||
                    projected.ComboCounts["FIRE"] != 2 || projected.ComboCounts["ICE"] != 1)
                    throw new InvalidOperationException("static categories must match full settlement for every swap");
            }
        var dynamic = InventorySnapshotFixture.RowDependentArtifact();
        if (new InventorySettlementProjectionWorkspace(dynamic).StaticComboCounts != null)
            throw new InvalidOperationException("dynamic categories must not use the static count cache");
        var other = CategoryBoard(Enumerable.Repeat(ArtifactCategoryRuleSnapshot.Static, 4).ToArray(),
            new[] { new[] { "ICE" }, new[] { "ICE" }, new[] { "ICE" }, new[] { "FIRE" } });
        var otherWorkspace = new InventorySettlementProjectionWorkspace(other);
        if (otherWorkspace.StaticComboCounts["FIRE"] != 1 || workspace.StaticComboCounts["FIRE"] != 2)
            throw new InvalidOperationException("category counts belong to one input snapshot");
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
