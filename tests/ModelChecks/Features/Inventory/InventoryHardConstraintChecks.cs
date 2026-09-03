using SephiriaEnhancements.Inventory;
using SephiriaEnhancements.ModelChecks.Runtime.Inventory;
using SephiriaEnhancements.Runtime.Inventory;

namespace SephiriaEnhancements.ModelChecks.Features.Inventory;

internal static class InventoryHardConstraintChecks
{
    private static readonly InventorySearchBudget Budget = new(8, 1000, 0, useElapsedTimeLimit: false);

    internal static void Run()
    {
        HardPrecedesSoftAndKeepsSoftOrder();
        JointConflictAndBudgetAreDifferent();
        ExhaustiveFeasibilityMatchesIndependentEnumeration();
        ExclusionAndComboConstraints();
        EditingAndPersistence();
        PersistentRulesRemainVisibleAndRemovable();
        HardFeedbackIsBinary();
        Console.WriteLine("InventoryHardConstraints: HardPrecedesSoftAndKeepsSoftOrder; JointConflictAndBudgetAreDifferent; " +
            "ExhaustiveFeasibilityMatchesIndependentEnumeration; ExclusionAndComboConstraints; " +
            "EditingAndPersistence; PersistentRulesRemainVisibleAndRemovable; HardFeedbackIsBinary passed");
    }

    private static InventoryOptimizationPreferences Queue(InventorySnapshot board)
    {
        var preferences = InventoryOptimizationPreferences.Default;
        for (int i = 0; i < board.Items.Count; i++)
            preferences = InventoryArtifactIntentEditor.PlacePriority(preferences, board.Items[i].InstanceId, board.Items[i].EntityId, i);
        return preferences;
    }

    private static InventoryOptimizationProposal Solve(InventorySnapshot board, InventoryOptimizationPreferences preferences, bool exact)
    {
        var request = new InventoryOptimizationRequest(board, InventoryOptimizationPolicyResolver.Resolve(board, preferences), Budget);
        IInventoryLayoutOptimizer optimizer = exact ? new ExactInventoryLayoutOptimizer() : new BoundedInventoryLayoutOptimizer();
        Check(optimizer.TryOptimize(request, default, out var result), "fixture must fit optimizer budget");
        return result;
    }

    private static void HardPrecedesSoftAndKeepsSoftOrder()
    {
        var board = InventorySnapshotFixture.ArtifactsAtLevels(new[] { 6, 5, 4 }, new[] { 0, 1, 2 }, 6);
        var preferences = InventoryArtifactIntentEditor.SetStrength(Queue(board), board.Items[2].ItemKey, InventoryConstraintStrength.Hard);
        foreach (bool exact in new[] { true, false })
        {
            var result = Solve(board, preferences, exact);
            Check(result.Succeeded && result.Improved && result.HardConstraintStatus == InventoryHardConstraintStatus.Feasible,
                "hard third slot must override earlier soft requests");
            var actual = InventorySettlementProjector.Evaluate(board, result.Layout);
            Check(actual.Artifacts.Select(a => a.CappedEffectiveLevel).SequenceEqual(new[] { 5, 4, 6 }),
                "hard slot gets six; remaining soft slots retain their order");
            Check(result.OptimalityProven == exact, "bounded search must not claim an optimality proof");
        }
    }

    private static void JointConflictAndBudgetAreDifferent()
    {
        var board = InventorySnapshotFixture.ArtifactsAtLevels(new[] { 6, 5 }, new[] { 0, 1 }, 6);
        var preferences = Queue(board);
        foreach (var item in board.Items)
            preferences = InventoryArtifactIntentEditor.SetStrength(preferences, item.ItemKey, InventoryConstraintStrength.Hard);
        var exact = Solve(board, preferences, true);
        Check(!exact.Succeeded && exact.Layout == null && !exact.Improved && !exact.OptimalityProven &&
            exact.HardConstraintStatus == InventoryHardConstraintStatus.ProvenInfeasible,
            "mutually incompatible hard goals must not return a layout or a claimed optimal solution");
        Check(exact.TargetEvaluations.All(target => target.MaximumObservedValue == 6),
            "each goal is individually reachable; the failure is joint feasibility");
        var bounded = Solve(board, preferences, false);
        Check(!bounded.Succeeded && bounded.Layout == null && bounded.HardConstraintStatus == InventoryHardConstraintStatus.NotFound,
            "a local search is not an infeasibility proof");
        var feasible = InventoryArtifactIntentEditor.SetStrength(preferences, board.Items[0].ItemKey, InventoryConstraintStrength.Soft);
        var policy = InventoryOptimizationPolicyResolver.Resolve(board, feasible);
        var exhausted = InventoryOptimizer.Solve(board, policy, new InventorySearchBudget(1, 1, 0, false));
        Check(!exhausted.Succeeded && exhausted.Layout == null && exhausted.HardConstraintStatus == InventoryHardConstraintStatus.NotFound &&
            exhausted.TerminationReason == InventorySearchTerminationReason.CandidateEvaluationLimit,
            "budget can run out before a feasible swap is evaluated");
        Check(Solve(board, feasible, true).Succeeded, "the budget-limited problem really does have a feasible answer");
        var alreadyFeasible = InventoryArtifactIntentEditor.SetStrength(Queue(board), board.Items[0].ItemKey, InventoryConstraintStrength.Hard);
        var current = InventoryOptimizer.Solve(board, InventoryOptimizationPolicyResolver.Resolve(board, alreadyFeasible),
            new InventorySearchBudget(1, 1, 0, false));
        Check(current.Succeeded && current.Layout.ContentEquals(InventoryLayoutProjection.Current(board)),
            "budget exhaustion must preserve a feasible current layout as a valid answer");
    }

    private static void ExhaustiveFeasibilityMatchesIndependentEnumeration()
    {
        // A tiny complete domain tests activation at zero as well as level goals.
        // Expected feasibility comes directly from the two assignments, not the scorer.
        for (int a = -1; a <= 2; a++)
            for (int b = -1; b <= 2; b++)
                for (int mask = 0; mask < 4; mask++)
                    foreach (int firstGoal in new[] { 0, 2 })
                        foreach (int secondGoal in new[] { 0, 2 })
                        {
                            var board = InventorySnapshotFixture.ArtifactsAtLevels(new[] { a, b }, new[] { 0, 1 }, 2);
                            var preferences = Queue(board);
                            int[] goals = { firstGoal, secondGoal };
                            for (int i = 0; i < 2; i++)
                            {
                                preferences = InventoryArtifactIntentEditor.SetMinimumEffectiveLevel(preferences, board, board.Items[i].ItemKey, goals[i]);
                                preferences = InventoryArtifactIntentEditor.SetStrength(preferences, board.Items[i].ItemKey,
                                    (mask & (1 << i)) == 0 ? InventoryConstraintStrength.Soft : InventoryConstraintStrength.Hard);
                            }
                            bool Meets(int x, int y) => ((mask & 1) == 0 || x >= firstGoal) && ((mask & 2) == 0 || y >= secondGoal);
                            bool exists = Meets(a, b) || Meets(b, a);
                            var result = Solve(board, preferences, true);
                            Check(result.Succeeded == exists, $"independent feasibility mismatch: {a},{b}; mask {mask}; goals {firstGoal},{secondGoal}");
                            if (exists)
                            {
                                int[] cells = result.Layout.CopyCells();
                                int[] levels = { a, b };
                                Check(Meets(levels[cells[0]], levels[cells[1]]) && result.OptimalityProven,
                                    "every returned exact layout must satisfy all hard conditions");
                            }
                            else Check(result.HardConstraintStatus == InventoryHardConstraintStatus.ProvenInfeasible && result.Layout == null,
                                "complete enumeration must distinguish infeasibility from search failure");
                        }
    }

    private static void ExclusionAndComboConstraints()
    {
        var board = InventorySnapshotFixture.ArtifactsAtLevels(new[] { 6, -1 }, new[] { 0, 1 }, 6);
        var preferences = InventoryArtifactIntentEditor.PlaceAvoid(Queue(board), board.Items[0].InstanceId, board.Items[0].EntityId, 0);
        preferences = InventoryArtifactIntentEditor.SetStrength(preferences, board.Items[0].ItemKey, InventoryConstraintStrength.Hard);
        foreach (bool exact in new[] { true, false })
        {
            var result = Solve(board, preferences, exact);
            Check(result.Succeeded && result.Layout.GetCell(0) == 1, "hard exclusion requires disabled, not merely level zero");
        }
        var row = InventorySnapshotFixture.RowDependentArtifact();
        foreach (var kind in new[] { InventoryPreferenceLevel.Priority, InventoryPreferenceLevel.Avoid })
        {
            var combos = new[]
            {
                new ComboOptimizationPreference(kind == InventoryPreferenceLevel.Priority ? "ICE" : "FIRE", kind,
                    kind == InventoryPreferenceLevel.Priority ? 1 : 0, InventoryConstraintStrength.Hard),
                new ComboOptimizationPreference("FIRE", InventoryPreferenceLevel.Priority, 1)
            };
            // Do not overwrite the hard FIRE rule with a duplicate soft rule.
            if (kind == InventoryPreferenceLevel.Avoid) combos = combos.Take(1).ToArray();
            var rules = new InventoryOptimizationPreferences(InventorySearchEffort.Balanced, true, Array.Empty<ArtifactOptimizationPreference>(), combos);
            foreach (bool exact in new[] { true, false })
            {
                var result = Solve(row, rules, exact);
                Check(result.Succeeded && result.Layout.GetCell(0) >= 2, "row-dependent category counts must satisfy the hard rule");
            }
        }
        var impossible = new InventoryOptimizationPreferences(InventorySearchEffort.Balanced, true, Array.Empty<ArtifactOptimizationPreference>(),
            new[] { new ComboOptimizationPreference("ICE", InventoryPreferenceLevel.Priority, 2, InventoryConstraintStrength.Hard) });
        Check(Solve(row, impossible, true).HardConstraintStatus == InventoryHardConstraintStatus.ProvenInfeasible,
            "unreachable hard combo minimum must fail");
    }

    private static void EditingAndPersistence()
    {
        var board = InventorySnapshotFixture.ArtifactsAtLevels(new[] { 6, 5 }, new[] { 0, 1 }, 6);
        var key = board.Items[0].ItemKey;
        var preferences = InventoryArtifactIntentEditor.SetStrength(Queue(board), key, InventoryConstraintStrength.Hard);
        preferences = InventoryArtifactIntentEditor.SetMinimumEffectiveLevel(preferences, board, key, 5);
        preferences = InventoryArtifactIntentEditor.SetAutomatic(preferences, key);
        preferences = InventoryArtifactIntentEditor.PlacePriority(preferences, key.NativeInstanceId, key.EntityId, 1);
        preferences = InventoryArtifactIntentEditor.PlaceAvoid(preferences, key.NativeInstanceId, key.EntityId, 0);
        preferences = InventoryArtifactIntentEditor.PlacePriority(preferences, key.NativeInstanceId, key.EntityId, 0);
        Check(preferences.ArtifactPreferences.Single(rule => rule.ItemKey == key).Strength == InventoryConstraintStrength.Hard,
            "hard strength must follow the item across goal changes, swaps and zones");
        var target = new InventoryComboTarget("ICE", InventoryPreferenceChoice.Priority, 1, 2);
        preferences = InventoryComboTargetEditor.SetChoice(preferences, target, target.Choice);
        preferences = InventoryComboTargetEditor.SetStrength(preferences, target, InventoryConstraintStrength.Hard);
        target = new InventoryComboTarget("ICE", InventoryPreferenceChoice.Priority, 1, 2, InventoryConstraintStrength.Hard);
        preferences = InventoryComboTargetEditor.SetRequiredValue(preferences, target, 2);
        Check(preferences.ComboPreferences.Single().Strength == InventoryConstraintStrength.Hard, "changing count must preserve strength");
        string encoded = InventoryOptimizationPreferencesCodec.Encode(preferences);
        Check(InventoryOptimizationPreferencesCodec.TryDecode(encoded, InventorySearchEffort.Fast, true, out var restored) &&
            restored.ComboPreferences.Single().Strength == InventoryConstraintStrength.Hard && restored.ArtifactPreferences.Count == 0,
            "persistent combo strength must round-trip; run-specific artifact identities must not persist");
        Check(!InventoryOptimizationPreferencesCodec.TryDecode("v4\nC|ICE|1|2|99", InventorySearchEffort.Fast, true, out _),
            "unknown strength must not silently become soft");
    }

    private static void HardFeedbackIsBinary()
    {
        var board = InventorySnapshotFixture.ArtifactsAtLevels(new[] { -1, 0, 5, 6 }, new[] { 0, 1, 2, 3 }, 6);
        Check(board.Items.Select(item => new InventoryArtifactGoalFeedback(item.Artifact, InventoryPreferenceLevel.Priority, 6,
            InventoryConstraintStrength.Hard).State).SequenceEqual(new[] { InventoryIntentSatisfaction.Unmet, InventoryIntentSatisfaction.Unmet,
                InventoryIntentSatisfaction.Unmet, InventoryIntentSatisfaction.Satisfied }), "hard failure must be red, including partial levels");
        Check(new InventoryArtifactGoalFeedback(board.Items[2].Artifact, InventoryPreferenceLevel.Priority, 6).State == InventoryIntentSatisfaction.Partial,
            "soft partial progress must remain yellow");
    }

    private static void PersistentRulesRemainVisibleAndRemovable()
    {
        var oldPolicy = PersistentInventoryOptimizationPolicyStore.Capture();
        var oldIntent = ExplorationInventoryIntentStore.Capture();
        try
        {
            var board = InventorySnapshotFixture.ArtifactsAtLevels(new[] { 6 }, new[] { 0 }, 6);
            Check(InventoryOptimizationPreferencesCodec.TryDecode("v4\nC|ABSENT|1|2|1",
                InventorySearchEffort.Balanced, true, out var saved), "fixture must decode");
            PersistentInventoryOptimizationPolicyStore.Replace(saved);
            ExplorationInventoryIntentStore.Replace(Queue(board));
            ExplorationInventoryIntentStore.RestorePersistentCombos();
            var restored = ExplorationInventoryIntentStore.Capture();
            Check(restored.ArtifactPreferences.Count == 1 && restored.ComboPreferences.Single().Strength == InventoryConstraintStrength.Hard,
                "late configuration loading must retain artifact marks and expose persisted hard rules");
            ExplorationInventoryIntentStore.Clear();
            restored = ExplorationInventoryIntentStore.Capture();
            Check(restored.ArtifactPreferences.Count == 0 && restored.ComboPreferences.Single().CategoryId == "ABSENT",
                "new exploration clears instance marks but keeps the saved category policy");
            var target = InventoryComboTargetEditor.BuildTargets(board, restored).Single();
            Check(target.CategoryId == "ABSENT" && target.Strength == InventoryConstraintStrength.Hard && target.RequiredValue == 2,
                "a missing category's hard rule must stay visible and editable");
            var cleared = InventoryComboTargetEditor.SetChoice(restored, target, InventoryPreferenceChoice.Automatic);
            Check(InventoryOptimizationPreferencesCodec.TryDecode(InventoryOptimizationPreferencesCodec.Encode(cleared),
                InventorySearchEffort.Balanced, true, out var reloaded), "cleared policy must persist as a valid empty configuration");
            PersistentInventoryOptimizationPolicyStore.Replace(reloaded);
            ExplorationInventoryIntentStore.Clear();
            Check(InventoryComboTargetEditor.BuildTargets(board, ExplorationInventoryIntentStore.Capture()).Count == 0 &&
                Solve(board, ExplorationInventoryIntentStore.Capture(), true).Succeeded,
                "switching to Automatic must remove the persisted conflict instead of resurrecting it in the next run");
        }
        finally
        {
            PersistentInventoryOptimizationPolicyStore.Replace(oldPolicy);
            ExplorationInventoryIntentStore.Replace(oldIntent);
        }
    }

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
