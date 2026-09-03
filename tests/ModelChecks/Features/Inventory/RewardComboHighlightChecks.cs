using SephiriaEnhancements.Inventory;
using SephiriaEnhancements.Runtime.Inventory;

namespace SephiriaEnhancements.ModelChecks.Features.Inventory;

internal static class RewardComboHighlightChecks
{
    internal static void Run()
    {
        Check(Snapshot(4), new[] { "GUARD" }, true, "preferred category below maximum");
        Check(Snapshot(0), new[] { "GUARD" }, true, "preferred category not yet owned");
        Check(Snapshot(6), new[] { "GUARD" }, false, "maximum reached");
        Check(Snapshot(8), new[] { "GUARD" }, false, "count exceeds maximum");
        Check(Snapshot(4, maximum: 0), new[] { "GUARD" }, false, "unknown maximum");
        Check(Snapshot(4, enabled: false), new[] { "GUARD" }, false, "disabled preset");
        Check(Snapshot(4, preferred: false), new[] { "GUARD" }, false, "category absent from preset");
        Check(Snapshot(4), new[] { "SHADOW" }, false, "unrelated category");
        Check(Snapshot(4), Array.Empty<string>(), false, "no reward categories");
        Check(Snapshot(4), new[] { "SHADOW", "GUARD" }, true, "multiple possible categories");
        Check(Snapshot(4), new[] { "guard" }, false, "canonical category identity");
        Check(null!, new[] { "GUARD" }, false, "missing or invalidated observation");
        Check(new InventorySnapshot(1, 0, Array.Empty<InventoryCellSnapshot>(),
            Array.Empty<InventoryItemSnapshot>()), new[] { "GUARD" }, false, "no preset");

        // The native current count includes bonuses and pair contributions.
        // Artifact count and applied/reached thresholds are not the cap check.
        Check(Snapshot(6, artifactCount: 2), new[] { "GUARD" }, false, "bonus completes combo");
        Check(Snapshot(5, artifactCount: 8), new[] { "GUARD" }, true, "raw item count is not effective count");

        foreach (var state in new[]
        {
            (Snapshot(4), true), (Snapshot(6), false), (Snapshot(5), true),
            (Snapshot(5, preferred: false), false), (Snapshot(5), true)
        })
            Check(state.Item1, new[] { "GUARD" }, state.Item2, "inventory and preset changes");

        Console.WriteLine("RewardComboHighlight: preset categories, native count cap and changing observations passed");
    }

    private static InventorySnapshot Snapshot(int count, int maximum = 6,
        bool enabled = true, bool preferred = true, int artifactCount = 2)
    {
        var preset = new NativePresetSnapshot(0, enabled, "", 0, "",
            new[] { 1 }, preferred ? new[] { "GUARD" } : Array.Empty<string>());
        var category = new ComboCategorySnapshot("GUARD", count,
            appliedCount: 2, artifactCategoryCount: artifactCount,
            bonusCount: 2, inferredUniquePairCount: 0,
            setThresholds: new[] { 2 }, comboThresholds: new[] { 2, 4, 6 },
            nativePresetFavorite: preferred, highestComboCount: maximum);
        return new InventorySnapshot(1, 0, Array.Empty<InventoryCellSnapshot>(),
            Array.Empty<InventoryItemSnapshot>(), nativePreset: preset,
            comboCategories: new[] { category });
    }

    private static void Check(InventorySnapshot snapshot, string[] categories,
        bool expected, string scenario)
    {
        if (RewardComboHighlightPolicy.ShouldHighlight(snapshot, categories) != expected)
            throw new InvalidOperationException("Reward combo highlight: " + scenario);
    }
}
