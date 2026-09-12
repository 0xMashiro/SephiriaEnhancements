using SephiriaEnhancements.Inventory;
using SephiriaEnhancements.Runtime.Inventory;
using SephiriaEnhancements.Runtime.GameBridge.Inventory;
using SephiriaEnhancements.ModelChecks.Runtime.Inventory;
using SephiriaEnhancements.Diagnostics;

namespace SephiriaEnhancements.ModelChecks.Features.Inventory;

internal static class InventoryPresetIntentChecks
{
    internal static void Run()
    {
        VerifyCurrentSetupWrites();
        VerifyPrioritiesAndCounts();
        VerifyManualOverrides();
        VerifyChangingIntentChangesSearch();
        VerifyPrecedence();
        Console.WriteLine("Inventory preset intent: live keys, aggregation, ordered counts, manual overrides, changed search and comparator passed");
    }

    private static void VerifyCurrentSetupWrites()
    {
        foreach (string key in new[] { "Item_Favorite_301", "FruitSkewer_Fruit0_Value",
                     "FruitSkewer_Fruit0_Category", "FruitSkewer_FruitCount", "FruitSkewer_AdaptiveItemDropBonus", "Preset_SelectedSlot" })
        {
            long before = NativePresetChangeSignal.Revision;
            NativePresetChangeSignal.ObserveCurrentSetupWrite(key);
            Require(NativePresetChangeSignal.Revision == before + 1, "current edits must be observed before disk save");
        }
        foreach (string key in new[] { "Preset_2_Item_Favorite_301", "Preset_2_FruitSkewer_Fruit0_Value", "Player0FruitSkewer_FruitCount", "Money" })
        {
            long before = NativePresetChangeSignal.Revision;
            NativePresetChangeSignal.ObserveCurrentSetupWrite(key);
            Require(NativePresetChangeSignal.Revision == before, "saved slots and exploration records are not current intent writes");
        }
    }

    private static InventorySnapshot Board(params NativePresetFruitSnapshot[] fruits)
    {
        return new InventorySnapshot(1, 0, Array.Empty<InventoryCellSnapshot>(), Array.Empty<InventoryItemSnapshot>(),
            nativePreset: new NativePresetSnapshot(-1, true, "", 0, "", Array.Empty<int>(), Array.Empty<string>(), fruits: fruits),
            comboCategories: new[] { Category("FIRE", 2, 4), Category("ICE", 2, 4), Category("SUN", 2, 4) });
    }

    private static ComboCategorySnapshot Category(string id, params int[] tiers) =>
        new(id, 0, 0, 0, 0, 0, tiers, tiers, false, highestComboCount: tiers.DefaultIfEmpty(0).Max());

    private static InventoryOptimizationScore Score(InventorySnapshot board, int fire, int ice = 0, int sun = 0,
        InventoryOptimizationPreferences? preferences = null)
    {
        var scorer = new InventoryOptimizationScorer(board, InventoryOptimizationPolicyResolver.Resolve(board, preferences!));
        var settlement = new ProjectedInventorySettlement(true, null, Array.Empty<ProjectedInventoryArtifactSettlement>(),
            new Dictionary<string, int> { ["FIRE"] = fire, ["ICE"] = ice, ["SUN"] = sun }, null);
        return scorer.Score(InventoryLayoutProjection.Current(board), settlement);
    }

    private static void VerifyPrioritiesAndCounts()
    {
        var board = Board(new("FIRE", 1), new("FIRE", 1), new("ICE", 2), new("SUN", 1));
        Require(board.BuildIntent.FruitSkewerCategoryPriorities["FIRE"] == 2, "duplicate fruits aggregate");
        Require(Score(board, 2).CompareTo(Score(board, 0, 2)) == 0, "equal investment has no category-order bias");
        Require(Score(board, 2).CompareTo(Score(board, 0, 0, 2)) > 0, "larger investment wins an equal tier tradeoff");
        Require(Score(board, 1).CompareTo(Score(board, 0, 0, 4)) > 0, "no lower-priority gain compensates one higher-priority count");
        Require(Score(board, 3).OrderedFruitSkewerComboCounts[0] > Score(board, 2).OrderedFruitSkewerComboCounts[0], "every useful count matters, not just tiers");
        Require(Score(board, 4).OrderedFruitSkewerComboCounts[0] == Score(board, 400).OrderedFruitSkewerComboCounts[0], "no fruit score beyond final tier");
        Require(Score(board, 2).OrderedFruitSkewerComboCounts[0] == 2, "count is not multiplied by priority or number of tiers");
        var cancelled = Board(new("FIRE", 2), new("FIRE", -2), new("ICE", -1));
        Require(Score(cancelled, 4, 4).OrderedFruitSkewerComboCounts.Count == 0 &&
            Score(cancelled, 4, 4).AvoidedTargetsActive == 0, "negative or neutral fruit does not disable owned artifacts");
        Require(Score(board, 4, 0).CompareTo(Score(board, 2, 2)) > 0, "equal-priority total ties favor completed categories");
        Require(Score(board, 4, 4, 1).CompareTo(Score(board, 4, 4, 0)) > 0, "lower priority improves after higher priorities tie");
        var strict = Board(new("FIRE", 3), new("ICE", 2), new("SUN", 1));
        Require(Score(strict, 1).CompareTo(Score(strict, 0, 4, 4)) > 0, "multiple lower groups cannot buy higher-priority loss");
        var remapped = Board(new("FIRE", 1000), new("ICE", 999), new("SUN", 1));
        Require(Score(strict, 1, 2, 3).CompareTo(Score(remapped, 1, 2, 3)) == 0,
            "changing priority gaps while retaining order never changes the score");
        var unlimited = new InventorySnapshot(board.Width, board.Storage, board.Cells.ToArray(), board.Items.ToArray(),
            nativePreset: board.NativePreset, comboCategories: board.ComboCategories.ToArray(), unlimitedComboStatValue: 1);
        Require(Score(unlimited, 5).CompareTo(Score(unlimited, 4)) > 0, "native unlimited combo remains useful beyond the normal maximum");
        string json = InventoryReproductionJson.Serialize(board);
        Require(json.Contains("FruitSkewerPreferences") && json.Contains("Priority"), "reproduction records scoring inputs");
    }

    private static void VerifyManualOverrides()
    {
        var board = Board(new NativePresetFruitSnapshot("FIRE", 3));
        foreach (var level in new[] { InventoryPreferenceLevel.Priority, InventoryPreferenceLevel.Avoid })
        {
            var preferences = new InventoryOptimizationPreferences(InventorySearchEffort.Fast, false,
                Array.Empty<ArtifactOptimizationPreference>(), new[] { new ComboOptimizationPreference("FIRE", level, 0) });
            Require(Score(board, 4, preferences: preferences).OrderedFruitSkewerComboCounts.Count == 0,
                "even a manual zero target replaces automatic fruit preference");
            Require(InventoryComboTargetEditor.BuildTargets(board, preferences).Single(target => target.CategoryId == "FIRE").FruitSkewerPriority == 0,
                "manual UI and score agree");
        }
        var targets = InventoryComboTargetEditor.BuildTargets(board, InventoryOptimizationPreferences.Default);
        Require(targets[0].CategoryId == "FIRE" && targets[0].FruitSkewerPriority == 3, "UI shows the active source and strength");
    }

    private static void VerifyChangingIntentChangesSearch()
    {
        var source = InventorySnapshotFixture.RowDependentArtifact();
        InventoryOptimizationProposal Solve(string category)
        {
            var board = new InventorySnapshot(source.Width, source.Storage, source.Cells.ToArray(), source.Items.ToArray(),
                nativePreset: new NativePresetSnapshot(-1, true, "", 0, "", Array.Empty<int>(), Array.Empty<string>(),
                    fruits: new[] { new NativePresetFruitSnapshot(category, 2) }),
                comboCategories: new[] { Category("FIRE", 1), Category("ICE", 1) });
            return InventoryOptimizerSelector.Solve(board, InventoryOptimizationPolicyResolver.Resolve(board, InventoryOptimizationPreferences.Default),
                new InventorySearchBudget(4, 1000, 5000));
        }
        var fire = Solve("FIRE");
        var ice = Solve("ICE");
        Require(fire.Succeeded && ice.Succeeded, "both configurations are searchable");
        Require(!fire.Improved && ice.Improved && ice.Layout.GetCell(0) >= 2, "live intent changes the selected real layout");
    }

    private static void VerifyPrecedence()
    {
        InventoryOptimizationScore Value(int manual = 0, int loss = 0, int favorite = 0, long fruit = 0, int inferred = 0) =>
            new(manual, 0, 0, favorite, 0, 0, 0, 0, 0, 0, 0, 0,
                automaticLevelRegressions: loss, orderedFruitSkewerComboCounts: new[] { fruit }, preferredCategoryTargetsSatisfied: inferred);
        Require(Value(manual: 1).CompareTo(Value(fruit: 1000)) > 0, "manual first");
        Require(Value().CompareTo(Value(loss: 1, fruit: 1000)) > 0, "default protections precede fruit");
        Require(Value(favorite: 1).CompareTo(Value(fruit: 1000)) > 0, "explicit favorite before fruit");
        Require(Value(fruit: 1).CompareTo(Value(inferred: 1000)) > 0, "fruit before inferred categories");
        var values = new[] { Value(), Value(fruit: 1), Value(favorite: 1), Value(manual: 1), Value(loss: 1, fruit: 1000) };
        foreach (var a in values) foreach (var b in values) foreach (var c in values)
            if (a.CompareTo(b) >= 0 && b.CompareTo(c) >= 0) Require(a.CompareTo(c) >= 0, "transitive comparator");
    }

    private static void Require(bool value, string reason)
    {
        if (!value) throw new InvalidOperationException(reason);
    }
}
