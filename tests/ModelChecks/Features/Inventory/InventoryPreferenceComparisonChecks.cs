using SephiriaEnhancements.Inventory;
using SephiriaEnhancements.ModelChecks.Runtime.Inventory;
using SephiriaEnhancements.Runtime.Inventory;

namespace SephiriaEnhancements.ModelChecks.Features.Inventory;

internal static class InventoryPreferenceComparisonChecks
{
    internal static void Run()
    {
        ExplicitIntentPrecedesDefaults();
        AmbiguousTradeoffsPreferFewerChanges();
        SameBoardDifferentPreferences();
        ComparatorOrderingLaws();
        Console.WriteLine("InventoryPreferenceComparison: ExplicitIntentPrecedesDefaults; AmbiguousTradeoffsPreferFewerChanges; " +
            "SameBoardDifferentPreferences; ComparatorOrderingLaws passed; objective=" + InventoryOptimizationScore.ObjectiveId);
    }

    private static void ExplicitIntentPrecedesDefaults()
    {
        Higher(Score(exclusions: 0, positionLosses: 9, penaltyRisks: 9),
            Score(exclusions: 1, queue: new[] { 10000 }), "exclusion overrides defaults and priority");
        Higher(Score(queue: new[] { 1, 0 }, positionLosses: 9, penaltyRisks: 9),
            Score(queue: new[] { 0, 10000 }), "earlier slot overrides default protection and later slot");
        Higher(Score(manualTargets: 1, positionLosses: 1), Score(presetTargets: 100), "manual targets override default protection");
        Higher(Score(manualCompletion: 1, penaltyRisks: 1), Score(), "manual progress overrides default protection");
        Higher(Score(), Score(presetTargets: 100, positionLosses: 1), "native preset does not waive default protection");
        Higher(Score(), Score(levels: 100, penaltyRisks: 1), "automatic leveling does not waive default protection");
    }

    private static void AmbiguousTradeoffsPreferFewerChanges()
    {
        var fewerChanges = Score(queue: new[] { 10000 }, positionLosses: 3, moves: 1);
        var moreChanges = Score(queue: new[] { 10000 }, positionLosses: 1, levels: 100, moves: 2);
        Higher(fewerChanges, moreChanges, "loss count and aggregate levels cannot price unknown tradeoffs");
        var tiedChanges = Score(queue: new[] { 10000 }, penaltyRisks: 1, levels: 100, moves: 1);
        Check(fewerChanges.CompareTo(tiedChanges) == 0 && tiedChanges.CompareTo(fewerChanges) == 0,
            "equally disruptive unknown exchanges remain tied, regardless of loss type/count");
        Higher(Score(positionLosses: 1, rotations: 0), Score(positionLosses: 1, rotations: 1), "rotation fallback");
        Check(fewerChanges.CompareUserRequirementsTo(moreChanges) == 0,
            "movement/default protection must not change reported user completion");
    }

    private static void SameBoardDifferentPreferences()
    {
        // A higher-level row switches the source's attribute channel A -> B. Neither channel has a utility weight.
        var board = InventoryRowCategoryStatChecks.Board(2, new[] { 0, 0, 2, 2 });
        var item = board.Items.Single();
        var automatic = InventoryArtifactIntentEditor.PlacePriority(InventoryOptimizationPreferences.Default,
            item.InstanceId, item.EntityId, 0);
        var activeOnly = InventoryArtifactIntentEditor.SetMinimumEffectiveLevel(automatic, board, item.ItemKey, 0);
        var current = InventoryLayoutProjection.Current(board);
        var moved = new InventoryLayoutProjection(new[] { 2 }, new int[1]);
        foreach (var (preferences, expectMove) in new[]
        {
            (InventoryOptimizationPreferences.Default, false), (automatic, true), (activeOnly, false)
        })
        {
            var policy = InventoryOptimizationPolicyResolver.Resolve(board, preferences);
            var scorer = new InventoryOptimizationScorer(board, policy);
            var before = scorer.Score(current, InventorySettlementProjector.Evaluate(board, current));
            var after = scorer.Score(moved, InventorySettlementProjector.Evaluate(board, moved));
            Check(after.PositionEffectRegressions == 1 && after.CappedEffectiveArtifactLevelTotal == 2,
                "fixture must expose a real level gain and attribute loss");
            Check((after.CompareTo(before) > 0) == expectMove, "preference, not layout alone, determines the winner");
            var budget = new InventorySearchBudget(8, 100, 0, useElapsedTimeLimit: false);
            var exact = InventoryOptimizerSelector.Solve(board, policy, budget);
            var bounded = InventoryOptimizer.Solve(board, policy, budget);
            foreach (var result in new[] { exact, bounded })
            {
                Check(result.Succeeded && result.Improved == expectMove, "both solvers must apply the same preference contract");
                Check(expectMove ? result.Layout.GetCell(0) / board.Width == 1 : result.Layout.GetCell(0) == 0,
                    "default/active-only must retain A; automatic priority must reach its target in row B");
            }
            Check(exact.BestScore.CompareTo(bounded.BestScore) == 0, "small fixture optimum must agree");
        }
    }

    private static void ComparatorOrderingLaws()
    {
        var random = new Random(4931);
        var scores = Enumerable.Range(0, 32).Select(_ => Score(exclusions: random.Next(2),
            queue: new[] { random.Next(3), random.Next(3) }, positionLosses: random.Next(3), penaltyRisks: random.Next(2),
            moves: random.Next(3), rotations: random.Next(2), levels: random.Next(5), manualTargets: random.Next(2))).ToArray();
        foreach (var a in scores)
            foreach (var b in scores)
            {
                Check(Math.Sign(a.CompareTo(b)) == -Math.Sign(b.CompareTo(a)), "comparison must be antisymmetric");
                foreach (var c in scores)
                {
                    if (a.CompareTo(b) > 0 && b.CompareTo(c) > 0)
                        Check(a.CompareTo(c) > 0, "comparison must be transitive for search correctness");
                    if (a.CompareTo(b) == 0)
                        Check(Math.Sign(a.CompareTo(c)) == Math.Sign(b.CompareTo(c)), "ties must be substitutable");
                }
            }
    }

    private static InventoryOptimizationScore Score(int exclusions = 0, int[]? queue = null,
        int positionLosses = 0, int penaltyRisks = 0, int moves = 0, int rotations = 0, int levels = 0,
        int manualTargets = 0, int manualCompletion = 0, int presetTargets = 0) =>
        new(manualTargets, manualCompletion, exclusions, presetTargets, 0, 0, 0, 0, levels, 0, moves, rotations,
            queue, positionLosses, penaltyRisks);
    private static void Higher(InventoryOptimizationScore a, InventoryOptimizationScore b, string message) =>
        Check(a.CompareTo(b) > 0 && b.CompareTo(a) < 0, message);
    private static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
