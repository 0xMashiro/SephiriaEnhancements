using SephiriaEnhancements.Inventory;
using SephiriaEnhancements.Runtime.Inventory;
using SephiriaEnhancements.ModelChecks.Runtime.Inventory;

namespace SephiriaEnhancements.ModelChecks.Features.Inventory;

internal static class RewardComboHighlightChecks
{
    internal static void Run()
    {
        Check(Board(4), true, "fruit category progresses without immediate tier gain");
        Check(Board(5), true, "fruit preference reaches next tier");
        Check(Board(0), true, "preferred missing category can be built up");
        Check(Board(6), false, "maximum reached");
        Check(Board(8), false, "over maximum");
        Check(Board(5, fruit: 0), false, "favorites alone never imply category demand");
        Check(Board(5, fruit: -1), false, "negative fruit");
        Check(Board(5, enabled: false), false, "inactive fruit intent");
        Check(Board(5), false, "unknown reward category", categories: new[] { "OTHER" });
        Check(Board(5), false, "conditional category is not predicted", categories: Array.Empty<string>());
        Check(null!, false, "invalidated observation");
        Check(Board(5, full: true), false, "no space for an addition");
        Check(Board(5, duplicate: true), false, "duplicate reward does not promise added count");
        var disabled = Board(5);
        Check(new InventorySnapshot(disabled.Width, disabled.Storage, disabled.Cells.ToArray(), disabled.Items.ToArray(),
            artifactEffectsEnabled: false, nativePreset: disabled.NativePreset,
            comboCategories: disabled.ComboCategories.ToArray()), false, "artifact effects disabled");
        var dynamicBoard = InventorySnapshotFixture.RowDependentArtifact();
        Check(new InventorySnapshot(dynamicBoard.Width, dynamicBoard.Storage, dynamicBoard.Cells.ToArray(), dynamicBoard.Items.ToArray(),
            nativePreset: Board(5).NativePreset, comboCategories: Board(5).ComboCategories.ToArray()),
            false, "existing dynamic categories require placement simulation");
        var low = Find(Board(4, fruit: 1));
        var high = Find(Board(4, fruit: 2));
        if (high!.Priority <= low!.Priority || high.CategoryId != "GUARD" ||
            high.CurrentCount != 4 || high.TargetCount != 6)
            throw new InvalidOperationException("fruit priority and next-tier reason must agree");
        Check(Board(2), true, "several additions still contribute toward next tier");
        Check(Board(3), true, "one addition to next tier");
        Console.WriteLine("RewardComboHighlight: progress toward tiers, ordinal fruit preferences, duplicates, capacity and uncertainty passed");
    }

    private static ComboCategorySnapshot Category(string id, int count) =>
        new(id, count, 2, 2, 0, 0, new[] { 2 }, new[] { 2, 4, 6 }, true, 6);

    private static InventorySnapshot Board(int count, int fruit = 2, bool enabled = true, bool full = false, bool duplicate = false)
    {
        var template = InventorySnapshotFixture.ArtifactsAtLevels(new[] { 0, 0 },
            full ? new[] { 0, 1 } : duplicate ? new[] { 0 } : Array.Empty<int>());
        return new InventorySnapshot(2, 2, template.Cells.ToArray(), template.Items.ToArray(),
            nativePreset: new NativePresetSnapshot(0, enabled, "", 0, "", new[] { 1 }, new[] { "GUARD" },
                fruits: new[] { new NativePresetFruitSnapshot("GUARD", fruit) }),
            comboCategories: new[] { Category("GUARD", count) });
    }

    private static RewardComboHighlightPolicy.Opportunity? Find(InventorySnapshot board,
        string[]? categories = null) =>
        RewardComboHighlightPolicy.FindOpportunity(board,
            1000, categories ?? new[] { "GUARD" });

    private static void Check(InventorySnapshot board, bool expected, string scenario,
        string[]? categories = null)
    {
        if ((Find(board, categories) != null) != expected)
            throw new InvalidOperationException("Reward highlight: " + scenario);
    }
}
