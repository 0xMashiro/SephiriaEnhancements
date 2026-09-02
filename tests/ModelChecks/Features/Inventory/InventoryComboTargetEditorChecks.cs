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
        return "automatic override;priority cycle;enabled-only and bounded values passed";
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
