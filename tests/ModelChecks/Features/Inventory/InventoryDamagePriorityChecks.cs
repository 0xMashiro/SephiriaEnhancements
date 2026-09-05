using SephiriaEnhancements.Inventory;
using SephiriaEnhancements.Runtime.Inventory;
using SephiriaEnhancements.Diagnostics;
using SephiriaEnhancements.ModelChecks.Runtime.Diagnostics;
using System.Text.Json;

namespace SephiriaEnhancements.ModelChecks.Features.Inventory;

internal static class InventoryDamagePriorityChecks
{
    internal static void Run()
    {
        var fixture = new InventoryDamagePriorityFixture();
        var snapshot = fixture.Snapshot(InventoryDamagePriorityFixture.Original);
        var current = InventoryLayoutProjection.Current(snapshot);
        var moved = current.WithCellsSwapped(0, 1);
        var before = InventorySettlementProjector.Evaluate(snapshot, current);
        var after = InventorySettlementProjector.Evaluate(snapshot, moved);
        foreach (int recipient in new[] { 0, 1 })
        {
            // Use the same editor and policy composition as the live priority list.
            var item = snapshot.Items[recipient];
            var preferences = InventoryArtifactIntentEditor.PlacePriority(InventoryOptimizationPreferences.Default, item.InstanceId, item.EntityId, 0);
            preferences = InventoryArtifactIntentEditor.SetMinimumEffectiveLevel(preferences, snapshot, item.ItemKey, 0);
            var policy = InventoryOptimizationPolicyResolver.Resolve(snapshot, preferences);
            var scorer = new InventoryOptimizationScorer(snapshot, policy);
            var a = scorer.Score(current, before);
            var b = scorer.Score(moved, after);
            Check(recipient == 0 ? a.CompareTo(b) > 0 : b.CompareTo(a) > 0, "recipient order chooses damage destination");
            Check((recipient == 0 ? a : b).OrderedPriorityDamageBonuses[0] == (recipient == 0 ? 84 : 60), "independent duplicate-source total");
            Check(b.PositionEffectRegressions == (recipient == 1 ? 0 : 2), "only explicit reassignment releases old protection");
            Check(!InventoryAdditiveScoreBound.IsAttained(snapshot, policy, a), "damage target cannot trigger additive certificate");
            var result = InventoryOptimizerSelector.Solve(snapshot, policy, new InventorySearchBudget(16, 15000, 10000));
            Check(result.Succeeded && result.BestScore.OrderedPriorityDamageBonuses[0] == (recipient == 0 ? 84 : 60), "live solver follows recipient");
            Check(InventoryLayoutPlanner.TryCreate(snapshot, result.Layout, out _, out _), "application plan remains valid");
            using var json = JsonDocument.Parse(InventoryReproductionJson.Serialize(b));
            Check(InventoryReproductionReplay.Read<InventoryOptimizationScore>(json.RootElement).CompareTo(b) == 0, "score replay preserves ordered damage");
            // Reuse one scorer across layouts to catch stale recipient/source indexes.
            Check(scorer.Score(current, before).CompareTo(a) == 0, "scorer scratch state resets");
        }
        var defaultScorer = new InventoryOptimizationScorer(snapshot, InventoryOptimizationPolicyResolver.Resolve(snapshot, InventoryOptimizationPreferences.Default));
        Check(defaultScorer.Score(moved, after).PositionEffectRegressions == 2, "unmarked items keep old protection");
        Check(defaultScorer.Score(moved, after).OrderedPriorityDamageBonuses.Count == 0, "no invented target");
        VerifyProtection(snapshot, moved, after);
        VerifyScarceLevels();
        Console.WriteLine("InventoryDamagePriority: recipients, duplicate sources, hard requirements, protection, solver and replay passed");
    }

    private static void VerifyProtection(InventorySnapshot snapshot, InventoryLayoutProjection moved, ProjectedInventorySettlement after)
    {
        var extra = new InventoryPositionEffectValue(new InventoryPositionEffectKey(snapshot.Items[4].ItemKey, InventoryPositionEffectKind.FirstSlotsElementDamage, channel: "FireDamage"), 6);
        var extraRule = new InventoryPositionEffectRule(snapshot.Items[4].ItemKey, extra.Key.Kind, new double[] { 0, 1, 2, 3, 4, 5, 6 }, boundary: 1, channels: new[] { "FireDamage" });
        var mixed = new InventorySnapshot(snapshot.Width, snapshot.Storage, snapshot.Cells.ToArray(), snapshot.Items.ToArray(),
            positionEffects: new InventoryPositionEffectsSnapshot(snapshot.PositionEffects.Rules.Append(extraRule).ToArray(), snapshot.PositionEffects.Traits.ToArray(), snapshot.PositionEffects.Observed.Append(extra).ToArray(), null));
        var scorer = new InventoryOptimizationScorer(mixed, InventoryOptimizationPolicyResolver.Resolve(mixed, InventoryDamagePriorityFixture.Preferences(new[] { 1 })));
        var loss = new ProjectedInventorySettlement(true, after.Cells.ToArray(), after.Artifacts.ToArray(), after.ComboCounts.ToDictionary(p => p.Key, p => p.Value), Array.Empty<string>(), positionEffects: after.PositionEffects.ToArray());
        Check(scorer.Score(moved, loss).PositionEffectRegressions == 1, "unrelated damage loss survives transfer waiver");
        var broken = new ProjectedInventorySettlement(true, after.Cells.ToArray(), after.Artifacts.ToArray(), after.ComboCounts.ToDictionary(p => p.Key, p => p.Value), Array.Empty<string>(), positionEffects: new[] { extra });
        Check(scorer.Score(moved, broken).PositionEffectRegressions == 2, "broken chain without new recipient stays protected");
        var inactive = after.Artifacts.Select(a => a.ItemKey == snapshot.Items[1].ItemKey
            ? new ProjectedInventoryArtifactSettlement(a.ItemKey, false, true, a.DisplayedLevel, 0) : a).ToArray();
        var inactiveResult = new ProjectedInventorySettlement(true, after.Cells.ToArray(), inactive, after.ComboCounts.ToDictionary(p => p.Key, p => p.Value), Array.Empty<string>(), positionEffects: after.PositionEffects.Append(extra).ToArray());
        Check(scorer.Score(moved, inactiveResult).OrderedPriorityDamageBonuses[0] == 0 && scorer.Score(moved, inactiveResult).PositionEffectRegressions == 2,
            "inactive recipient cannot justify a transfer");
    }

    private static void VerifyScarceLevels()
    {
        var fixture = new InventoryDamagePriorityFixture(new[] { 0, 0, 6, 0, 0, 0 });
        var snapshot = fixture.Snapshot(InventoryDamagePriorityFixture.Original);
        foreach (bool hard in new[] { false, true })
        {
            var preferences = new InventoryOptimizationPreferences(InventorySearchEffort.Thorough, false, new[] {
                new ArtifactOptimizationPreference(0, snapshot.Items[0].EntityId, InventoryPreferenceLevel.Priority, 0, 0),
                new ArtifactOptimizationPreference(1, snapshot.Items[1].EntityId, InventoryPreferenceLevel.Priority, 6, 1,
                    strength: hard ? InventoryConstraintStrength.Hard : InventoryConstraintStrength.Soft) }, Array.Empty<ComboOptimizationPreference>());
            var policy = InventoryOptimizationPolicyResolver.Resolve(snapshot, preferences);
            var scorer = new InventoryOptimizationScorer(snapshot, policy);
            InventoryOptimizationScore? best = null;
            foreach (var cells in Permute(Enumerable.Range(0, 6).ToArray()))
            {
                var layout = new InventoryLayoutProjection(cells, new int[6]);
                var settlement = InventorySettlementProjector.Evaluate(snapshot, layout);
                Check(InventorySettlementDifferentialVerifier.Compare(snapshot, layout, settlement, fixture.Snapshot(cells)).Matched, "independent settlement");
                var score = scorer.Score(layout, settlement);
                if (best == null || score.CompareTo(best) > 0) best = score;
            }
            Check(best!.HardConstraintsSatisfied && best.OrderedPriorityDamageBonuses[0] == (hard ? 0 : 42), "hard requirement overrides main damage, soft follows order");
        }
    }

    private static IEnumerable<int[]> Permute(int[] values)
    {
        if (values.Length == 0) { yield return Array.Empty<int>(); yield break; }
        foreach (int first in values) foreach (var tail in Permute(values.Where(v => v != first).ToArray())) yield return new[] { first }.Concat(tail).ToArray();
    }
    private static void Check(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
