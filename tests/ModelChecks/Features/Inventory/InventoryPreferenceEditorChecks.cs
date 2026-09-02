using SephiriaEnhancements.ModelChecks.Runtime.Inventory;
using SephiriaEnhancements.Runtime.Inventory;
using SephiriaEnhancements.Inventory;

namespace SephiriaEnhancements.ModelChecks.Features.Inventory;

internal static class InventoryPreferenceEditorChecks
{
    internal static string Run()
    {
        VerifyChoiceCycle();
        VerifyArtifactEditing();
        VerifyComboEditing();
        return "automatic override;priority cycle;enabled-only and bounded values passed";
    }

    private static void VerifyChoiceCycle()
    {
        InventoryPreferenceChoice choice =
            InventoryPreferenceChoice.Automatic;
        var visited = new List<InventoryPreferenceChoice>();
        for (int index = 0; index < 6; index++)
        {
            visited.Add(choice);
            choice = InventoryPreferenceEditor.NextChoice(choice);
        }
        if (choice != InventoryPreferenceChoice.Automatic ||
            visited.Distinct().Count() != 6)
        {
            throw new InvalidOperationException(
                "HUD preference choices must form one complete cycle");
        }
    }

    private static void VerifyArtifactEditing()
    {
        InventorySnapshot snapshot = InventorySnapshotFixture.ArtifactsAtLevels(
            new[] { 0, 5 }, new[] { 0 }, maxLevel: 5);
        InventoryOptimizationPreferences preferences =
            InventoryOptimizationPreferences.Default;
        InventoryPreferenceEditorTarget target = InventoryPreferenceEditor.
            BuildTargets(snapshot, preferences,
                InventoryOptimizationTargetKind.Artifact).Single();
        if (target.Choice != InventoryPreferenceChoice.Automatic ||
            target.EntityId != 1000 || target.MaximumValue != 5)
        {
            throw new InvalidOperationException(
                "artifact HUD target projection is incomplete");
        }

        preferences = InventoryPreferenceEditor.SetChoice(preferences, target,
            InventoryPreferenceChoice.Priority);
        target = InventoryPreferenceEditor.BuildTargets(snapshot, preferences,
            InventoryOptimizationTargetKind.Artifact).Single();
        preferences = InventoryPreferenceEditor.SetRequiredValue(preferences,
            target, 0);
        target = InventoryPreferenceEditor.BuildTargets(snapshot, preferences,
            InventoryOptimizationTargetKind.Artifact).Single();
        if (target.RequiredValue != 0 ||
            preferences.ArtifactPreferences.Single().MinimumEffectiveLevel != 0)
        {
            throw new InvalidOperationException(
                "artifact HUD must preserve enabled-only level zero intent");
        }
        preferences = InventoryPreferenceEditor.SetRequiredValue(preferences,
            target, 99);
        target = InventoryPreferenceEditor.BuildTargets(snapshot, preferences,
            InventoryOptimizationTargetKind.Artifact).Single();
        if (target.Choice != InventoryPreferenceChoice.Priority ||
            target.RequiredValue != 5 ||
            preferences.ArtifactPreferences.Single().TargetsInstance)
        {
            throw new InvalidOperationException(
                "artifact HUD edits must create a bounded entity rule");
        }

        preferences = InventoryPreferenceEditor.SetChoice(preferences, target,
            InventoryPreferenceChoice.Ignored);
        if (preferences.ArtifactPreferences.Single().Level !=
            InventoryPreferenceLevel.Neutral)
        {
            throw new InvalidOperationException(
                "Ignored must explicitly override native preset projection");
        }
        target = InventoryPreferenceEditor.BuildTargets(snapshot, preferences,
            InventoryOptimizationTargetKind.Artifact).Single();
        preferences = InventoryPreferenceEditor.SetChoice(preferences, target,
            InventoryPreferenceChoice.Automatic);
        if (preferences.ArtifactPreferences.Count != 0)
        {
            throw new InvalidOperationException(
                "Automatic must remove the explicit entity override");
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
        InventoryPreferenceEditorTarget target = InventoryPreferenceEditor.
            BuildTargets(snapshot, preferences,
                InventoryOptimizationTargetKind.ComboCategory).Single();
        preferences = InventoryPreferenceEditor.SetChoice(preferences, target,
            InventoryPreferenceChoice.Avoid);
        target = InventoryPreferenceEditor.BuildTargets(snapshot, preferences,
            InventoryOptimizationTargetKind.ComboCategory).Single();
        preferences = InventoryPreferenceEditor.SetRequiredValue(preferences,
            target, 3);
        ComboOptimizationPreference rule =
            preferences.ComboPreferences.Single();
        if (rule.Level != InventoryPreferenceLevel.Avoid ||
            rule.MinimumCount != 3 || target.MaximumValue != 4)
        {
            throw new InvalidOperationException(
                "combo HUD edits must retain a meaningful Avoid threshold");
        }
    }
}
