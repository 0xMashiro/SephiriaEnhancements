using SephiriaEnhancements.ModelChecks.Runtime.Inventory;
using SephiriaEnhancements.Runtime.Inventory;
using SephiriaEnhancements.Inventory;

namespace SephiriaEnhancements.ModelChecks.Features.Inventory;

internal static class InventoryComboTargetEditorChecks
{
    internal static string Run()
    {
        VerifyChoiceCycle();
        VerifyComboEditing();
        VerifyPresetDisplayOrder();
        return "automatic override;priority cycle;bounded values;stable active-preset display order passed";
    }

    private static void VerifyPresetDisplayOrder()
    {
        var source = InventorySnapshotFixture.ArtifactsAtLevels(new[] { 0 }, new[] { 0 });
        var categories = new[] { "EARTH", "FIRE", "ICE", "WIND" }.Select(id =>
            new ComboCategorySnapshot(id, 1, 1, 1, 0, 0, new[] { 2 }, new[] { 3 }, false, 3)).ToArray();
        var preferences = new InventoryOptimizationPreferences(InventorySearchEffort.Balanced, true,
            Array.Empty<ArtifactOptimizationPreference>(), new[]
            {
                new ComboOptimizationPreference("FIRE", InventoryPreferenceLevel.Avoid, 1, InventoryConstraintStrength.Hard),
                new ComboOptimizationPreference("MISSING", InventoryPreferenceLevel.Priority, 2, InventoryConstraintStrength.Hard)
            });
        InventorySnapshot Board(bool enabled, params string[] favorites) => new(source.Width, source.Storage,
            source.Cells.ToArray(), source.Items.ToArray(), comboCategories: categories,
            nativePreset: new NativePresetSnapshot(2, enabled, "test", 0, string.Empty, Array.Empty<int>(), favorites));
        var related = InventoryComboTargetEditor.BuildTargets(Board(true, "WIND", "FIRE", "FIRE"), preferences);
        if (!related.Select(target => target.CategoryId).SequenceEqual(new[] { "FIRE", "WIND", "EARTH", "ICE", "MISSING" }))
            throw new InvalidOperationException("preset-related categories must lead without claiming preset array order is a ranking");
        var fire = related.First();
        if (fire.Choice != InventoryPreferenceChoice.Avoid || fire.RequiredValue != 1 || fire.Strength != InventoryConstraintStrength.Hard ||
            related.Last().CategoryId != "MISSING")
            throw new InvalidOperationException("display sorting must preserve overrides and unavailable saved targets");
        foreach (var board in new[] { Board(false, "WIND", "FIRE"), Board(true) })
            if (!InventoryComboTargetEditor.BuildTargets(board, preferences).Select(target => target.CategoryId)
                .SequenceEqual(new[] { "EARTH", "FIRE", "ICE", "WIND", "MISSING" }))
                throw new InvalidOperationException("disabled or empty presets must preserve base display order");
        if (InventoryComboTargetEditor.BuildTargets(Board(true, "ICE"), preferences).First().CategoryId != "ICE")
            throw new InvalidOperationException("updated preset observation must change the display group");
    }

    private static void VerifyChoiceCycle()
    {
        InventoryPreferenceChoice choice =
            InventoryPreferenceChoice.Automatic;
        var visited = new List<InventoryPreferenceChoice>();
        for (int index = 0; index < 3; index++)
        {
            visited.Add(choice);
            choice = InventoryComboTargetEditor.NextChoice(choice);
        }
        if (choice != InventoryPreferenceChoice.Automatic ||
            visited.Distinct().Count() != 3)
        {
            throw new InvalidOperationException(
                "HUD preference choices must form one complete cycle");
        }
    }

    private static void VerifyComboEditing()
    {
        InventorySnapshot artifactSnapshot =
            InventorySnapshotFixture.ArtifactsAtLevels(new[] { 0 },
                new[] { 0 });
        var category = new ComboCategorySnapshot("EMBER", currentCount: 2,
            appliedCount: 2, artifactCategoryCount: 2, bonusCount: 0,
            inferredUniquePairCount: 0, setThresholds: new[] { 2, 4 },
            comboThresholds: new[] { 3 }, nativePresetFavorite: true,
            highestComboCount: 4);
        var snapshot = new InventorySnapshot(artifactSnapshot.Width,
            artifactSnapshot.Storage, artifactSnapshot.Cells.ToArray(),
            artifactSnapshot.Items.ToArray(), comboCategories: new[]
            {
                category
            });
        InventoryOptimizationPreferences preferences =
            InventoryOptimizationPreferences.Default;
        InventoryComboTarget target = InventoryComboTargetEditor.
            BuildTargets(snapshot, preferences).Single();
        if (target.RequiredValue != 0 || target.CanAdjustRequiredValue)
            throw new InvalidOperationException("automatic combo targets must default to zero without showing controls");
        preferences = InventoryComboTargetEditor.SetChoice(preferences, target,
            InventoryPreferenceChoice.Avoid);
        target = InventoryComboTargetEditor.BuildTargets(snapshot, preferences).Single();
        preferences = InventoryComboTargetEditor.SetRequiredValue(preferences,
            target, 3);
        ComboOptimizationPreference rule =
            preferences.ComboPreferences.Single();
        if (rule.Level != InventoryPreferenceLevel.Avoid ||
            rule.TargetCount != 3 || target.MaximumValue != 4)
        {
            throw new InvalidOperationException(
                "combo HUD edits must retain a meaningful Avoid threshold");
        }
        preferences = InventoryComboTargetEditor.SetRequiredValue(preferences, target, 0);
        target = InventoryComboTargetEditor.BuildTargets(snapshot, preferences).Single();
        if (target.RequiredValue != 0 || preferences.ComboPreferences.Single().TargetCount != 0)
            throw new InvalidOperationException("combo Avoid must allow an inclusive maximum of zero");
        preferences = InventoryComboTargetEditor.SetChoice(preferences, target, InventoryPreferenceChoice.Priority);
        target = InventoryComboTargetEditor.BuildTargets(snapshot, preferences).Single();
        if (target.RequiredValue != 0 || !target.CanAdjustRequiredValue)
            throw new InvalidOperationException("combo Priority must preserve an editable zero minimum");
    }
}
