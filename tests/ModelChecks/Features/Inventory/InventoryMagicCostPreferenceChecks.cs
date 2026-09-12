using SephiriaEnhancements.Inventory;
using SephiriaEnhancements.Runtime.Inventory;
using SephiriaEnhancements.ModelChecks.Runtime.Inventory;

namespace SephiriaEnhancements.ModelChecks.Features.Inventory;

internal static class InventoryMagicCostPreferenceChecks
{
    internal static void Run()
    {
        var defaults = InventoryOptimizationPreferences.Default;
        Check(!defaults.AllowAdditionalMagicCost, "default avoids additional magic cost");
        foreach (bool cost in new[] { false, true })
        {
            var preference = defaults.WithAdditionalMagicCost(cost);
            Check(InventoryOptimizationPreferencesCodec.TryDecode(InventoryOptimizationPreferencesCodec.Encode(preference),
                InventorySearchEffort.Balanced, true, out var decoded), "decode");
            var marked = InventoryArtifactIntentEditor.PlacePriority(preference, 100, 1000, 0);
            foreach (var retained in new[] { decoded, marked, InventoryArtifactIntentEditor.Remove(marked, new InventoryItemKey(1000, 100)),
                preference.WithExecutionSettings(InventorySearchEffort.Fast, false),
                InventoryOptimizationPreferenceComposer.Compose(preference, defaults, InventorySearchEffort.Fast, false) })
                Check(retained.AllowAdditionalMagicCost == cost, "cost choice survives editors, execution and composition");
        }
        foreach (bool allowed in new[] { false, true })
        {
            var snapshot = InventorySnapshotFixture.ArtifactsAtLevels(new[] { 1, 3 }, new[] { 0 }, 3,
                new[] { 1 }, new[] { 2 });
            var item = snapshot.Items[0];
            var preference = InventoryArtifactIntentEditor.PlacePriority(defaults.WithAdditionalMagicCost(allowed),
                item.InstanceId, item.EntityId, 0);
            var policy = InventoryOptimizationPolicyResolver.Resolve(snapshot, preference);
            Check(policy.ArtifactInstanceRules[item.ItemKey].MinimumEffectiveLevel == (allowed ? 2 : 1), "cost opt-in still respects stat penalty cap");
            var moved = new InventoryLayoutProjection(new[] { 1 }, new int[1]);
            Check(new InventoryOptimizationScorer(snapshot, policy).Score(moved, InventorySettlementProjector.Evaluate(snapshot, moved))
                .AutomaticLevelRegressions == 1, "cost opt-in must not bypass stat penalties");
            var texts = new Dictionary<string, string>();
            InventoryOptimizationLocalization.Register((language, key, text) => { if (language == "en-US") texts[key] = text; });
            var target = preference.ArtifactPreferences.Single();
            Check(InventoryOptimizationLocalization.FormatArtifactTarget(target, item.Artifact, key => texts[key],
                allowAdditionalMagicCost: allowed).Contains((allowed ? 2 : 1).ToString()), "display and policy target agree");
            var affordable = InventorySnapshotFixture.ArtifactsAtLevels(new[] { 1, 2 }, new[] { 0 }, 3, new[] { 1 }, new[] { 2 });
            var solved = InventoryOptimizerSelector.Solve(affordable, InventoryOptimizationPolicyResolver.Resolve(affordable, preference),
                new InventorySearchBudget(8, 100, 10000));
            Check(solved.Succeeded && solved.Layout.GetCell(0) == (allowed ? 1 : 0), "cost choice changes the actual chosen layout");
        }
        Console.WriteLine("Magic cost preferences: persistence, cost ceilings, displayed targets and selected layouts passed");
    }
    private static void Check(bool valid, string message) { if (!valid) throw new InvalidOperationException(message); }
}
