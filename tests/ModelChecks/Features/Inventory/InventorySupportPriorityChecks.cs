using System.Text.Json;
using SephiriaEnhancements.Diagnostics;
using SephiriaEnhancements.Inventory;
using SephiriaEnhancements.ModelChecks.Runtime.Diagnostics;
using SephiriaEnhancements.ModelChecks.Runtime.Inventory;
using SephiriaEnhancements.Runtime.Inventory;

namespace SephiriaEnhancements.ModelChecks.Features.Inventory;

internal static class InventorySupportPriorityChecks
{
    private static InventoryItemKey Key(int index) => new(9000 + index, 0);
    private static readonly InventorySearchBudget Budget = new(16, 10000, 0, useElapsedTimeLimit: false);

    internal static void Run()
    {
        foreach (var kind in new[] { InventoryPositionEffectKind.MagicCooldownRecovery,
                     InventoryPositionEffectKind.MagicCostReduction, InventoryPositionEffectKind.AdjacentPlanetEnhancement })
            foreach (bool observationsAvailable in new[] { false, true })
                VerifyRecipients(kind, observationsAvailable);
        VerifyHardRequirements();
        VerifyInactiveAndBrokenTargets();
        VerifyUnrelatedProtections();
        VerifyPlanetEnhancementDoesNotStack();
        VerifyUnits();
        Console.WriteLine("InventorySupportPriority: native offsets, recipient order, host/client observations, hard goals, inactive targets, protections, planet stacking and replay passed");
    }

    private static InventorySnapshot Board(InventoryPositionEffectKind kind, bool observationsAvailable = false,
        int[]? levels = null)
    {
        // The two magic helpers act on opposite horizontal neighbors in the game.
        int offset = kind == InventoryPositionEffectKind.MagicCostReduction ? -1 : 1;
        var rule = new InventoryPositionEffectRule(Key(0), kind, new[] { 1.0, 2.0, 3.0, 4.0 },
            offsets: new[] { new InventoryOffsetSnapshot(offset, 0) }, targetCategory: "TestPlanet");
        int[] positions = { 1, 1 + offset, 5 };
        var board = InventoryPositionEffectChecks.Board(3, levels ?? new int[6], positions,
            new[] { rule }, Array.Empty<InventoryPositionEffectValue>(), observationsAvailable: false);
        return new InventorySnapshot(board.Width, board.Storage, board.Cells.ToArray(), board.Items.ToArray(),
            comboCategories: board.ComboCategories.ToArray(), positionEffects: new InventoryPositionEffectsSnapshot(
                board.PositionEffects.Rules.ToArray(), board.PositionEffects.Traits.ToArray(),
                observationsAvailable ? InventoryPositionEffectProjector.EvaluateCurrent(board) : Array.Empty<InventoryPositionEffectValue>(),
                Array.Empty<string>(), observationsAvailable));
    }

    private static InventoryOptimizationPreferences Priorities(InventorySnapshot board, params int[] recipients)
    {
        var preferences = InventoryOptimizationPreferences.Default;
        for (int slot = 0; slot < recipients.Length; slot++)
        {
            var item = board.Items[recipients[slot]];
            preferences = InventoryArtifactIntentEditor.PlacePriority(preferences, item.InstanceId, item.EntityId, slot);
            preferences = InventoryArtifactIntentEditor.SetMinimumEffectiveLevel(preferences, board, item.ItemKey, 0);
        }
        return preferences;
    }

    private static InventoryOptimizationScore Score(InventorySnapshot board, InventoryOptimizationPreferences preferences,
        InventoryLayoutProjection layout) => new InventoryOptimizationScorer(board,
            InventoryOptimizationPolicyResolver.Resolve(board, preferences)).Score(layout, InventorySettlementProjector.Evaluate(board, layout));

    private static void VerifyRecipients(InventoryPositionEffectKind kind, bool observationsAvailable)
    {
        var board = Board(kind, observationsAvailable);
        var current = InventoryLayoutProjection.Current(board);
        var transferred = current.WithCellsSwapped(board.Items[1].CellIndex, board.Items[2].CellIndex);
        var defaults = InventoryOptimizationPreferences.Default;
        Check(Score(board, defaults, transferred).PositionEffectRegressions == 1 &&
            Score(board, defaults, current).CompareTo(Score(board, defaults, transferred)) > 0,
            "unmarked recipient keeps its existing bonus");
        var preferences = Priorities(board, 1, 2);
        for (int first = 1; first <= 2; first++)
        {
            if (first == 2)
                preferences = InventoryArtifactIntentEditor.PlacePriority(preferences, board.Items[2].InstanceId, board.Items[2].EntityId, 0);
            var policy = InventoryOptimizationPolicyResolver.Resolve(board, preferences);
            var scorer = new InventoryOptimizationScorer(board, policy);
            var a = scorer.Score(current, InventorySettlementProjector.Evaluate(board, current));
            var b = scorer.Score(transferred, InventorySettlementProjector.Evaluate(board, transferred));
            Check(first == 1 ? a.CompareTo(b) > 0 : b.CompareTo(a) > 0, "reordering changes who receives support");
            Check(b.PositionEffectRegressions == 0, "explicit recipients can replace the old relationship");
            var solved = InventoryOptimizerSelector.Solve(board, policy, Budget);
            Check(solved.Succeeded && solved.BestScore.HardConstraintsSatisfied, "live solver succeeds");
            var effects = InventorySettlementProjector.Evaluate(board, solved.Layout).PositionEffects;
            Check(effects.Single().Key.Target == board.Items[first].ItemKey && effects.Single().Value > 0,
                "actual layout serves the first recipient");
            Check(InventoryLayoutPlanner.TryCreate(board, solved.Layout, out _, out _), "native operation plan is valid");
            Check(scorer.Score(current, InventorySettlementProjector.Evaluate(board, current)).CompareTo(a) == 0,
                "repeated evaluation clears transferred-source state");
            using var json = JsonDocument.Parse(InventoryReproductionJson.Serialize(b));
            Check(InventoryReproductionReplay.Read<InventoryOptimizationScore>(json.RootElement).CompareTo(b) == 0,
                "replay retains ordered support");
        }
    }

    private static void VerifyHardRequirements()
    {
        // A stronger helper for the first recipient competes with the second
        // recipient's level goal for the only level-three cell.
        var board = Board(InventoryPositionEffectKind.MagicCooldownRecovery, levels: new[] { 0, 3, 0, 0, 0, 0 });
        var preferences = Priorities(board, 1, 2);
        preferences = InventoryArtifactIntentEditor.SetMinimumEffectiveLevel(preferences, board, Key(2), 3);
        var soft = InventoryOptimizerSelector.Solve(board, InventoryOptimizationPolicyResolver.Resolve(board, preferences), Budget);
        Check(soft.Succeeded && soft.Layout.GetCell(0) == 1 && soft.Layout.GetCell(2) != 1,
            "earlier recipient's support precedes a later soft level goal");
        preferences = InventoryArtifactIntentEditor.SetStrength(preferences, Key(2), InventoryConstraintStrength.Hard);
        var solved = InventoryOptimizerSelector.Solve(board, InventoryOptimizationPolicyResolver.Resolve(board, preferences), Budget);
        Check(solved.Succeeded && solved.BestScore.HardConstraintsSatisfied && solved.Layout.GetCell(2) == 1,
            "all hard goals precede support allocation");
        preferences = InventoryArtifactIntentEditor.PlaceAvoid(preferences, 0, Key(1).EntityId, 0);
        preferences = InventoryArtifactIntentEditor.SetStrength(preferences, Key(1), InventoryConstraintStrength.Hard);
        Check(!InventoryOptimizerSelector.Solve(board, InventoryOptimizationPolicyResolver.Resolve(board, preferences), Budget).Succeeded,
            "support cannot justify a layout that violates a required exclusion");
    }

    private static void VerifyInactiveAndBrokenTargets()
    {
        var board = Board(InventoryPositionEffectKind.MagicCooldownRecovery);
        var current = InventoryLayoutProjection.Current(board);
        var preferences = Priorities(board, 2);
        var scorer = new InventoryOptimizationScorer(board, InventoryOptimizationPolicyResolver.Resolve(board, preferences));
        var moved = current.WithCellsSwapped(board.Items[1].CellIndex, board.Items[2].CellIndex);
        var projected = InventorySettlementProjector.Evaluate(board, moved);
        var inactive = projected.Artifacts.Select(item => item.ItemKey == Key(2)
            ? new ProjectedInventoryArtifactSettlement(item.ItemKey, false, true, -1, 0) : item).ToArray();
        var result = scorer.Score(moved, new ProjectedInventorySettlement(true, projected.Cells.ToArray(), inactive,
            projected.ComboCounts.ToDictionary(pair => pair.Key, pair => pair.Value), Array.Empty<string>(), positionEffects: projected.PositionEffects.ToArray()));
        Check(result.OrderedPrioritySupportPoints[0] == 0 && result.PositionEffectRegressions == 1,
            "inactive recipient earns no support and cannot release old protection");
        var broken = scorer.Score(moved, new ProjectedInventorySettlement(true, projected.Cells.ToArray(), projected.Artifacts.ToArray(),
            projected.ComboCounts.ToDictionary(pair => pair.Key, pair => pair.Value), Array.Empty<string>()));
        Check(broken.OrderedPrioritySupportPoints.All(value => value == 0) && broken.PositionEffectRegressions == 1,
            "broken connection without a recipient keeps its regression");
    }

    private static void VerifyUnrelatedProtections()
    {
        foreach (var rule in new[] {
            new InventoryPositionEffectRule(Key(0), InventoryPositionEffectKind.MagicCooldownRecovery, new[] { -1.0 },
                offsets: new[] { new InventoryOffsetSnapshot(1, 0) }),
            new InventoryPositionEffectRule(Key(0), InventoryPositionEffectKind.HalfBoardWeaponMode, new[] { 1.0 },
                boundary: 1, channels: new[] { "A", "B" }),
            new InventoryPositionEffectRule(Key(0), InventoryPositionEffectKind.SameRowCompanionMode) })
        {
            var board = InventoryPositionEffectChecks.Board(3, new int[6], new[] { 0, 1, 5 },
                new[] { rule }, Array.Empty<InventoryPositionEffectValue>(), observationsAvailable: false);
            var changed = rule.Kind == InventoryPositionEffectKind.HalfBoardWeaponMode
                ? new InventoryLayoutProjection(new[] { 2, 1, 0 }, new int[3])
                : new InventoryLayoutProjection(new[] { 3, 1, 4 }, new int[3]);
            var score = Score(board, Priorities(board, 2), changed);
            Check(score.PositionEffectRegressions > 0 && score.OrderedPrioritySupportPoints.All(value => value == 0),
                "negative effects and mode changes are not reassigned as positive support");
        }
    }

    private static void VerifyPlanetEnhancementDoesNotStack()
    {
        var rules = new[] {
            new InventoryPositionEffectRule(Key(0), InventoryPositionEffectKind.AdjacentPlanetEnhancement,
                offsets: new[] { new InventoryOffsetSnapshot(1, 0) }, targetCategory: "TestPlanet"),
            new InventoryPositionEffectRule(Key(1), InventoryPositionEffectKind.AdjacentPlanetEnhancement,
                offsets: new[] { new InventoryOffsetSnapshot(-1, 0) }, targetCategory: "TestPlanet") };
        var board = InventoryPositionEffectChecks.Board(3, new int[6], new[] { 0, 2, 1 }, rules,
            Array.Empty<InventoryPositionEffectValue>(), observationsAvailable: false);
        var preferences = Priorities(board, 2);
        var current = InventoryLayoutProjection.Current(board);
        Check(Score(board, preferences, current).OrderedPrioritySupportPoints[0] == 1 &&
            Score(board, preferences, new InventoryLayoutProjection(new[] { 0, 5, 1 }, new int[3])).OrderedPrioritySupportPoints[0] == 1,
            "multiple modules do not strengthen an already enhanced planet again");
    }

    private static void VerifyUnits()
    {
        foreach (double units in new[] { 0.01, 1.0, 1000.0 })
        {
            var rule = new InventoryPositionEffectRule(Key(0), InventoryPositionEffectKind.MagicCooldownRecovery,
                new[] { units, 2 * units }, offsets: new[] { new InventoryOffsetSnapshot(1, 0) });
            var board = InventoryPositionEffectChecks.Board(3, new int[6], new[] { 0, 1 }, new[] { rule },
                Array.Empty<InventoryPositionEffectValue>(), observationsAvailable: false);
            Check(Score(board, Priorities(board, 1), InventoryLayoutProjection.Current(board)).OrderedPrioritySupportPoints[0] == 0.5,
                "support preference does not depend on raw numerical units");
        }
    }

    private static void Check(bool valid, string message)
    {
        if (!valid) throw new InvalidOperationException(message);
    }
}
