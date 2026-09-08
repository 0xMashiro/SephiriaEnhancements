using SephiriaEnhancements.Inventory;
using SephiriaEnhancements.Runtime.Inventory;
using SephiriaEnhancements.ModelChecks.Runtime.Inventory;

namespace SephiriaEnhancements.ModelChecks.Features.Inventory;

internal static class InventorySpecialEffectPreferenceChecks
{
    internal static void Run()
    {
        var defaults = InventoryOptimizationPreferences.Default;
        Check(defaults.PositionEffectPreference == InventoryPositionEffectPreference.Improve && !defaults.AllowAdditionalMagicCost, "safe defaults");
        foreach (var mode in Enum.GetValues<InventoryPositionEffectPreference>())
            foreach (bool cost in new[] { false, true })
            {
                var preference = defaults.WithSpecialEffects(mode, cost);
                Check(InventoryOptimizationPreferencesCodec.TryDecode(InventoryOptimizationPreferencesCodec.Encode(preference),
                    InventorySearchEffort.Balanced, true, out var decoded), "decode");
                var marked = InventoryArtifactIntentEditor.PlacePriority(preference, 100, 1000, 0);
                foreach (var retained in new[] { decoded, marked, InventoryArtifactIntentEditor.Remove(marked, new InventoryItemKey(1000, 100)),
                preference.WithExecutionSettings(InventorySearchEffort.Fast, false),
                InventoryOptimizationPreferenceComposer.Compose(preference, defaults, InventorySearchEffort.Fast, false) })
                    Check(retained.PositionEffectPreference == mode && retained.AllowAdditionalMagicCost == cost, "settings survive editors, execution and composition");
            }
        var source = new InventoryItemKey(9000, 0);
        var kind = InventoryPositionEffectKind.MagicCooldownRecovery;
        var rule = new InventoryPositionEffectRule(source, kind, new[] { 1.0, 2.0, 3.0, 4.0 },
            offsets: new[] { new InventoryOffsetSnapshot(1, 0) });
        var board = InventoryPositionEffectChecks.Board(3, new[] { 0, 0, 3, 0, 0, 0 }, new[] { 0, 1, 5 },
            new[] { rule }, Array.Empty<InventoryPositionEffectValue>(), observationsAvailable: false);
        var current = InventoryLayoutProjection.Current(board);
        // Use a source at bottom-left to transfer its right-neighbor bonus.
        var redistributed = new InventoryLayoutProjection(new[] { 3, 1, 4 }, new int[3]);
        InventoryOptimizationScore Score(InventoryPositionEffectPreference mode, InventoryLayoutProjection layout) =>
            new InventoryOptimizationScorer(board, InventoryOptimizationPolicyResolver.Resolve(board, defaults.WithSpecialEffects(mode, false)))
                .Score(layout, InventorySettlementProjector.Evaluate(board, layout));
        Check(Score(InventoryPositionEffectPreference.Improve, redistributed).PositionEffectRegressions > 0 &&
            Score(InventoryPositionEffectPreference.Redistribute, redistributed).PositionEffectRegressions == 0, "redistribution permits changing positive recipients");
        Check(Score(InventoryPositionEffectPreference.Preserve, current).PositionEffectUtilizationPoints == 0 &&
            Score(InventoryPositionEffectPreference.Improve, current).PositionEffectUtilizationPoints > 0, "preserve does not reward additional position use");
        foreach (var effect in new[] {
            new InventoryPositionEffectRule(source, kind, new[] { -1.0 }, offsets: new[] { new InventoryOffsetSnapshot(1, 0) }),
            new InventoryPositionEffectRule(source, InventoryPositionEffectKind.HalfBoardWeaponMode, new[] { 1.0 }, boundary: 1, channels: new[] { "A", "B" }) })
        {
            var protectedBoard = InventoryPositionEffectChecks.Board(3, new int[6], new[] { 0, 1, 5 },
                new[] { effect }, Array.Empty<InventoryPositionEffectValue>(), observationsAvailable: false);
            var changed = new InventoryLayoutProjection(new[] { 2, 1, 0 }, new int[3]);
            if (effect.Kind == kind) changed = new InventoryLayoutProjection(new[] { 3, 1, 4 }, new int[3]);
            var score = new InventoryOptimizationScorer(protectedBoard, InventoryOptimizationPolicyResolver.Resolve(protectedBoard,
                defaults.WithSpecialEffects(InventoryPositionEffectPreference.Redistribute, false)))
                .Score(changed, InventorySettlementProjector.Evaluate(protectedBoard, changed));
            Check(score.PositionEffectRegressions > 0, "redistribution retains negative-effect and mode protections");
        }
        foreach (bool allowed in new[] { false, true })
        {
            var snapshot = InventorySnapshotFixture.ArtifactsAtLevels(new[] { 1, 3 }, new[] { 0 }, 3,
                new[] { 1 }, new[] { 2 });
            var item = snapshot.Items[0];
            var preference = InventoryArtifactIntentEditor.PlacePriority(defaults.WithSpecialEffects(InventoryPositionEffectPreference.Improve, allowed),
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
        Console.WriteLine("Special effect preferences: persistence, default protection, redistribution, cost ceilings and displayed targets passed");
    }
    private static void Check(bool valid, string message) { if (!valid) throw new InvalidOperationException(message); }
}
