using SephiriaEnhancements.Runtime.Inventory;
using SephiriaEnhancements.Inventory;

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
        VerifyIndependentNeighborMatches();
        VerifyStaticCategoryWorkspace();
        Console.WriteLine("InventorySettlementProjector: dynamic categories, dependency cycles and retained results passed");
    }

    private static void VerifyIndependentNeighborMatches()
    {
        var paper = new ArtifactCategoryRuleSnapshot(ArtifactCategoryRuleKind.NeighborMatch,
            neighborOffsets: new[] { new InventoryOffsetSnapshot(-1, 0), new InventoryOffsetSnapshot(1, 0) }, match: 2);
        var rules = new[] { ArtifactCategoryRuleSnapshot.Static, paper, ArtifactCategoryRuleSnapshot.Static,
            ArtifactCategoryRuleSnapshot.Static, paper, ArtifactCategoryRuleSnapshot.Static };
        var categories = new[] { new[] { "FIRE" }, new[] { "FIRE" }, new[] { "FIRE" },
            new[] { "ICE" }, new[] { "ICE" }, new[] { "ICE" } };
        var snapshot = CategoryBoard(rules, categories, 3, false);
        var mysticSnapshot = CategoryBoard(rules, categories.Select(values =>
            values.Select(value => value == "FIRE" ? "MYSTIC" : value).ToArray()).ToArray(), 3, false);
        if (!snapshot.SettlementValidation.LayoutProjectionReady)
            throw new InvalidOperationException("independent category copiers must be supported");
        var workspace = new InventorySettlementProjectionWorkspace(snapshot);
        int supported = 0, rejected = 0, plans = 0, rejectedPlans = 0;
        foreach (var cells in Permutations(Enumerable.Range(0, 6).ToArray(), 0))
        {
            var layout = new InventoryLayoutProjection(cells, new int[6]);
            bool adjacent = cells[1] / 3 == cells[4] / 3 && Math.Abs(cells[1] - cells[4]) == 1;
            var full = InventorySettlementProjector.Evaluate(snapshot, layout);
            var fast = InventorySettlementProjector.EvaluateForScoring(snapshot, layout, workspace);
            if (full.Succeeded == adjacent || fast.Succeeded == adjacent)
                throw new InvalidOperationException("category interactions must be checked for each candidate");
            if (adjacent) { rejected++; continue; }
            supported++;
            if (InventoryLayoutPlanner.TryCreate(snapshot, layout, out var plan, out var planIssue))
            {
                plans++;
                if (plan.Swaps.Count > 2 * snapshot.Items.Count)
                    throw new InvalidOperationException("temporary moves must have a bounded plan length");
                var intermediate = InventoryLayoutProjection.Current(snapshot);
                foreach (var swap in plan.Swaps)
                {
                    var first = Enumerable.Range(0, snapshot.Items.Count)
                        .Where(i => intermediate.GetCell(i) == swap.FirstCell)
                        .Select(i => (InventoryItemKey?)snapshot.Items[i].ItemKey).SingleOrDefault();
                    var second = Enumerable.Range(0, snapshot.Items.Count)
                        .Where(i => intermediate.GetCell(i) == swap.SecondCell)
                        .Select(i => (InventoryItemKey?)snapshot.Items[i].ItemKey).SingleOrDefault();
                    if (first != swap.ExpectedFirstItemKey || second != swap.ExpectedSecondItemKey)
                        throw new InvalidOperationException("each move must confirm the actual item instances at that step");
                    intermediate = intermediate.WithCellsSwapped(swap.FirstCell, swap.SecondCell);
                    if (!InventorySettlementProjector.Evaluate(snapshot, intermediate).Succeeded)
                        throw new InvalidOperationException("application must not enter unprojectable intermediate layouts");
                }
                for (int item = 0; item < snapshot.Items.Count; item++)
                    if (intermediate.GetCell(item) != layout.GetCell(item))
                        throw new InvalidOperationException("safe moves must reach every requested final item position");
            }
            else
            {
                rejectedPlans++;
                if (plan != null || planIssue != "LayoutIntermediateCategoriesUnavailable")
                    throw new InvalidOperationException("unsafe intermediate plans must be rejected before applying any moves");
            }
            int fire = 2, ice = 2;
            foreach (int source in new[] { 1, 4 })
            {
                int cell = cells[source];
                if (cell % 3 != 1) continue;
                int left = Array.IndexOf(cells, cell - 1), right = Array.IndexOf(cells, cell + 1);
                if (categories[left][0] == categories[right][0])
                {
                    if (categories[left][0] == "FIRE") fire++;
                    else ice++;
                }
            }
            if (full.ComboCounts["FIRE"] != fire || full.ComboCounts["ICE"] != ice ||
                fast.ComboCounts["FIRE"] != fire || fast.ComboCounts["ICE"] != ice)
                throw new InvalidOperationException("independent neighbor categories must match both neighbors in every permutation");
            var mystic = InventorySettlementProjector.Evaluate(mysticSnapshot, layout);
            if (mystic.Succeeded != (fire == 3))
                throw new InvalidOperationException("every candidate must retain the captured generated-cell contribution");
            if (InventoryLayoutPlanner.TryCreate(mysticSnapshot, layout, out var mysticPlan, out _))
            {
                if (!mystic.Succeeded) throw new InvalidOperationException("movement cannot bypass generated-cell restrictions");
                var intermediate = InventoryLayoutProjection.Current(mysticSnapshot);
                foreach (var swap in mysticPlan.Swaps)
                {
                    intermediate = intermediate.WithCellsSwapped(swap.FirstCell, swap.SecondCell);
                    if (!InventorySettlementProjector.Evaluate(mysticSnapshot, intermediate).Succeeded)
                        throw new InvalidOperationException("generated-cell count must also be preserved between moves");
                }
                for (int item = 0; item < snapshot.Items.Count; item++)
                    if (intermediate.GetCell(item) != layout.GetCell(item))
                        throw new InvalidOperationException("generated-cell-safe plans must reach their target");
            }
        }
        if (supported != 528 || rejected != 192)
            throw new InvalidOperationException("all six-item category layouts must be checked");
        if (plans != 528 || rejectedPlans != 0)
            throw new InvalidOperationException("all supported six-item layouts must have safe reordered plans");
        var policy = InventoryOptimizationPolicyResolver.Resolve(snapshot, InventoryOptimizationPreferences.Default);
        var exact = InventoryExhaustiveSearchOracle.Solve(snapshot, policy, new InventoryExhaustiveSearchLimits(1000, 1000, false));
        if (!exact.SearchStarted || exact.ProvenOptimal || exact.SearchSpaceExhausted ||
            exact.TerminationReason != InventoryExhaustiveSearchTerminationReason.UnsupportedCandidateLayouts)
            throw new InvalidOperationException("skipped mechanisms cannot produce an optimality proof");
        var selected = InventoryOptimizerSelector.Solve(snapshot, policy, new InventorySearchBudget(4, 1000, 1000, false));
        if (!selected.Succeeded || selected.OptimalityProven ||
            selected.TerminationReason != InventorySearchTerminationReason.UnsupportedCandidateLayouts ||
            !InventoryLayoutPlanner.TryCreate(snapshot, selected.Layout, out _, out _))
            throw new InvalidOperationException("selector must return a valid supported layout without claiming global optimality");
        var interacting = CategoryBoard(new[] { paper, paper, ArtifactCategoryRuleSnapshot.Static,
            ArtifactCategoryRuleSnapshot.Static }, new[] { Array.Empty<string>(), Array.Empty<string>(), new[] { "FIRE" }, new[] { "ICE" } }, 2, false);
        if (interacting.SettlementValidation.LayoutProjectionReady)
            throw new InvalidOperationException("history-dependent starting layouts remain unsupported");
        var three = CategoryBoard(rules.Concat(rules.Take(3)).ToArray(),
            categories.Concat(categories.Take(3)).ToArray(), 3, false);
        var threeResult = InventorySettlementProjector.Evaluate(three, InventoryLayoutProjection.Current(three));
        if (!threeResult.Succeeded || threeResult.ComboCounts["FIRE"] != 6 || threeResult.ComboCounts["ICE"] != 3)
            throw new InvalidOperationException("three independent category copiers must also be supported");
        var mysticCopy = CategoryBoard(new[] { paper, ArtifactCategoryRuleSnapshot.Static, ArtifactCategoryRuleSnapshot.Static },
            new[] { Array.Empty<string>(), new[] { "MYSTIC" }, new[] { "MYSTIC" } }, 3, false);
        VerifyMysticPreservation(mysticCopy, new[] { 1, 0, 2 });
        var mysticDependency = CategoryBoard(new[] { new ArtifactCategoryRuleSnapshot(ArtifactCategoryRuleKind.DependencyTarget, targetX: -1),
            ArtifactCategoryRuleSnapshot.Static, ArtifactCategoryRuleSnapshot.Static },
            new[] { Array.Empty<string>(), new[] { "MYSTIC" }, new[] { "MYSTIC" } }, 3, false);
        VerifyMysticPreservation(mysticDependency, new[] { 1, 0, 2 });
        var staticMystic = CategoryBoard(Enumerable.Repeat(ArtifactCategoryRuleSnapshot.Static, 3).ToArray(),
            new[] { Array.Empty<string>(), new[] { "MYSTIC" }, new[] { "MYSTIC" } }, 3, false);
        if (!staticMystic.SettlementValidation.LayoutProjectionReady)
            throw new InvalidOperationException("invariant generated-cell categories remain supported");
        var attackable = CategoryBoard(rules, categories, 3, true);
        if (attackable.SettlementValidation.LayoutProjectionReady)
            throw new InvalidOperationException("attackable category copiers can feed indirect dependencies");
        Console.WriteLine($"Independent neighbor categories: {supported} projected layouts, {rejected} interactions rejected; {plans} safe plans, {rejectedPlans} intermediate plans rejected");

        static IEnumerable<int[]> Permutations(int[] values, int start)
        {
            if (start == values.Length) { yield return (int[])values.Clone(); yield break; }
            for (int next = start; next < values.Length; next++)
            {
                (values[start], values[next]) = (values[next], values[start]);
                foreach (var result in Permutations(values, start + 1)) yield return result;
                (values[start], values[next]) = (values[next], values[start]);
            }
        }
    }

    private static void VerifyMysticPreservation(InventorySnapshot snapshot, int[] changedCells)
    {
        var current = InventoryLayoutProjection.Current(snapshot);
        var unchangedCount = current.WithCellsSwapped(1, 2);
        var changedCount = new InventoryLayoutProjection(changedCells, new int[changedCells.Length]);
        var workspace = new InventorySettlementProjectionWorkspace(snapshot);
        foreach (var layout in new[] { current, unchangedCount, changedCount, unchangedCount })
        {
            bool expected = layout != changedCount;
            var full = InventorySettlementProjector.Evaluate(snapshot, layout);
            var fast = InventorySettlementProjector.EvaluateForScoring(snapshot, layout, workspace);
            if (full.Succeeded != expected || fast.Succeeded != expected ||
                InventoryLayoutPlanner.TryCreate(snapshot, layout, out _, out _) != expected)
                throw new InvalidOperationException("generated-cell effects may be retained only while their category count is unchanged");
            if (!expected && !full.Issues.Contains("LayoutProjectionMysticCountChanged"))
                throw new InvalidOperationException("unknown generated-cell changes must have a specific reason");
        }
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

    private static InventorySnapshot CategoryBoard(ArtifactCategoryRuleSnapshot[] rules, string[][] categories,
        int width = 2, bool neighborAttackable = true)
    {
        var items = Enumerable.Range(0, rules.Length).Select(index =>
        {
            var artifact = new ArtifactSnapshot(0, 3, 0, 0, 0, true, false, false, "", true, false, false, "Pre",
                new CriteriaSnapshot(ArtifactActivationConditionKind.None, CriteriaEvaluationState.NotApplicable,
                    CriteriaEvaluationState.NotApplicable), categories[index], new[] { "FIRE", "ICE" },
                rules[index].Kind != ArtifactCategoryRuleKind.NeighborMatch || neighborAttackable, null, rules[index]);
            return new InventoryItemSnapshot(index, 7000 + index, 1, index, index % width, index / width, "Test", "",
                "Charm", "Normal", categories[index], InventoryItemKind.Artifact, artifact, null);
        }).ToArray();
        var cells = Enumerable.Range(0, rules.Length).Select(index => new InventoryCellSnapshot(index, index % width, index / width,
            0, 3, 0, 0, 0, 0, false, new InventoryCellSettlementSnapshot(true, 0, -1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0))).ToArray();
        var combos = new[] { "FIRE", "ICE" }.Concat(categories.SelectMany(values => values)).Distinct().Select(category =>
        {
            int count = categories.Count(values => values.Contains(category));
            return new ComboCategorySnapshot(category, count, count, count, 0, 0, Array.Empty<int>(), Array.Empty<int>(), false);
        }).ToArray();
        return new InventorySnapshot(width, rules.Length, cells, items, comboCategories: combos);
    }
}
