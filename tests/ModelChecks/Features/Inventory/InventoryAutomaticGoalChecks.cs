using SephiriaEnhancements.Inventory;
using SephiriaEnhancements.ModelChecks.Runtime.Inventory;
using SephiriaEnhancements.Runtime;
using SephiriaEnhancements.Runtime.Inventory;

namespace SephiriaEnhancements.ModelChecks.Features.Inventory;

internal static class InventoryAutomaticGoalChecks
{
    internal static void Run()
    {
        VerifyAutomaticQueue();
        VerifyModeOwnership();
        VerifyTradeoffs();
        VerifyFeedback();
        Console.WriteLine("InventoryAutomaticGoals: zero-configuration ordering, modes, penalties and verified color state passed");
    }

    private static InventoryOptimizationPreferences AutoQueue(InventorySnapshot snapshot)
    {
        var preferences = InventoryOptimizationPreferences.Default;
        for (int slot = 0; slot < snapshot.Items.Count; slot++)
        {
            var item = snapshot.Items[slot];
            preferences = InventoryArtifactIntentEditor.PlacePriority(preferences, item.InstanceId, item.EntityId, slot);
        }
        return preferences;
    }

    private static void VerifyAutomaticQueue()
    {
        var snapshot = InventorySnapshotFixture.ArtifactsAtLevels(new[] { 1, 2, 3, 4, 5, 6 },
            new[] { 0, 1, 2, 3, 4, 5 }, maxLevel: 6);
        var preferences = AutoQueue(snapshot);
        var policy = InventoryOptimizationPolicyResolver.Resolve(snapshot, preferences);
        Require(policy.ArtifactInstanceRules.Values.All(rule => rule.MinimumEffectiveLevel == 6),
            "new priority marks must target the artifact cap without editing a level");
        foreach (var proposal in new[]
        {
            InventoryOptimizerSelector.Solve(snapshot, policy, new InventorySearchBudget(16, 10000, 10000)),
            InventoryOptimizer.Solve(snapshot, policy, new InventorySearchBudget(16, 10000, 10000))
        })
        {
            Require(proposal.Succeeded, "automatic queue must be solvable");
            var after = InventorySettlementProjector.Evaluate(snapshot, proposal.Layout);
            for (int slot = 0; slot < 6; slot++)
                Require(after.Artifacts.Single(a => a.ItemKey == snapshot.Items[slot].ItemKey).CappedEffectiveLevel == 6 - slot,
                    "unreachable earlier targets must allow best-effort allocation to later slots");
        }
        // Displayed cell levels above the cap must not inflate automatic goals.
        var capped = InventorySnapshotFixture.ArtifactsAtLevels(new[] { 1, 4 }, new[] { 0, 1 }, maxLevel: 1);
        var cappedPolicy = InventoryOptimizationPolicyResolver.Resolve(capped, AutoQueue(capped));
        Require(cappedPolicy.ArtifactInstanceRules.Values.All(rule => rule.MinimumEffectiveLevel == 1),
            "automatic targets must use effective artifact caps, not displayed cell levels");
        var zero = InventorySnapshotFixture.ArtifactsAtLevels(new[] { -1, 0 }, new[] { 0, 1 }, maxLevel: 6);
        var zeroProposal = InventoryOptimizerSelector.Solve(zero,
            InventoryOptimizationPolicyResolver.Resolve(zero, AutoQueue(zero)), new InventorySearchBudget(8, 100, 10000));
        Require(zeroProposal.Improved && zeroProposal.Layout.GetCell(0) == 1,
            "when only level zero is available, activation must still prefer slot 1 over slot 2");
    }

    private static void VerifyModeOwnership()
    {
        var snapshot = InventorySnapshotFixture.ArtifactsAtLevels(new[] { 1, 2 }, new[] { 0, 1 }, maxLevel: 6);
        var first = snapshot.Items[0];
        var second = snapshot.Items[1];
        var preferences = AutoQueue(snapshot);
        preferences = InventoryArtifactIntentEditor.SetMinimumEffectiveLevel(preferences, snapshot, first.ItemKey, 3);
        preferences = InventoryArtifactIntentEditor.PlacePriority(preferences, first.InstanceId, first.EntityId, 1);
        Require(preferences.ArtifactPreferences.Single(r => r.ItemKey == first.ItemKey).TargetMode == ArtifactLevelTargetMode.SpecifiedLevel &&
            preferences.ArtifactPreferences.Single(r => r.ItemKey == second.ItemKey).TargetMode == ArtifactLevelTargetMode.Automatic,
            "both mode and target must follow the artifact across a swap");
        preferences = InventoryArtifactIntentEditor.SetMinimumEffectiveLevel(preferences, snapshot, second.ItemKey, 0);
        Require(InventoryOptimizationPolicyResolver.Resolve(snapshot, preferences).ArtifactInstanceRules[second.ItemKey].MinimumEffectiveLevel == 0 &&
            preferences.ArtifactPreferences.Single(r => r.ItemKey == second.ItemKey).TargetMode == ArtifactLevelTargetMode.ActiveOnly,
            "switching auto to active-only must work even when the stored minimum was already zero");
        preferences = InventoryArtifactIntentEditor.SetAutomatic(preferences, second.ItemKey);
        Require(InventoryOptimizationPolicyResolver.Resolve(snapshot, preferences).ArtifactInstanceRules[second.ItemKey].MinimumEffectiveLevel == 6,
            "restoring automatic must restore the cap target");
        preferences = InventoryArtifactIntentEditor.PlaceAvoid(preferences, first.InstanceId, first.EntityId, 0);
        preferences = InventoryArtifactIntentEditor.PlacePriority(preferences, first.InstanceId, first.EntityId, 0);
        Require(preferences.ArtifactPreferences.Single(r => r.ItemKey == first.ItemKey).TargetMode == ArtifactLevelTargetMode.Automatic,
            "returning an excluded artifact to priority must default to auto");
    }

    private static void VerifyTradeoffs()
    {
        Require(ArtifactAutomaticLevelPolicy.SafeLevel(6, 1, new[] { new[] { 300, 600, 900, 1200, 1500, 1800, 2100 } }) == 6,
            "Faultfinder Needle's positive critical curve must retain automatic max-level targeting");
        Require(ArtifactAutomaticLevelPolicy.SafeLevel(3, 1, new[] { new[] { 4, 8, 12, 16 }, new[] { -4, -6, -8, -10 } }) == 1,
            "Silver Bracelet upgrades must not automatically worsen the movement penalty");
        Require(ArtifactAutomaticLevelPolicy.SafeLevel(4, 0, new[] { new[] { 14, 16, 18, 21, 24 }, new[] { -2, -4, -6, -8, -10 } }) == 0,
            "Lizard Plate Armor must not blindly maximize worsening penalties");
        Require(ArtifactAutomaticLevelPolicy.SafeLevel(3, 0, new[] { new[] { -4, -4, -4, -5 } }) == 2,
            "safe upgrades with an unchanged penalty should remain available");
        var snapshot = InventorySnapshotFixture.ArtifactsAtLevels(new[] { 1, 3 }, new[] { 0, 1 }, maxLevel: 3,
            safeAutomaticLevels: new[] { 1, 3 });
        var preferences = AutoQueue(snapshot);
        var policy = InventoryOptimizationPolicyResolver.Resolve(snapshot, preferences);
        var current = InventoryLayoutProjection.Current(snapshot);
        var swapped = current.WithCellsSwapped(0, 1);
        var scorer = new InventoryOptimizationScorer(snapshot, policy);
        Require(scorer.Score(current, InventorySettlementProjector.Evaluate(snapshot, current)).CompareTo(
            scorer.Score(swapped, InventorySettlementProjector.Evaluate(snapshot, swapped))) > 0,
            "other scoring goals must not bypass the automatic penalty ceiling");
        preferences = InventoryArtifactIntentEditor.SetMinimumEffectiveLevel(preferences, snapshot, snapshot.Items[0].ItemKey, 3);
        policy = InventoryOptimizationPolicyResolver.Resolve(snapshot, preferences);
        var proposal = InventoryOptimizerSelector.Solve(snapshot, policy, new InventorySearchBudget(8, 100, 10000));
        Require(proposal.Improved && proposal.Layout.GetCell(0) == 1 && proposal.BestScore.AutomaticLevelRegressions == 0,
            "an explicit level request must allow the user's chosen tradeoff");
    }

    private static void VerifyFeedback()
    {
        var snapshot = InventorySnapshotFixture.ArtifactsAtLevels(new[] { -1, 0, 4, 6 }, new[] { 0, 1, 2, 3 }, maxLevel: 6);
        var states = snapshot.Items.Select(item => new InventoryArtifactGoalFeedback(item.Artifact,
            InventoryPreferenceLevel.Priority, 6).State).ToArray();
        Require(states.SequenceEqual(new[] { InventoryIntentSatisfaction.Unmet, InventoryIntentSatisfaction.Partial,
            InventoryIntentSatisfaction.Partial, InventoryIntentSatisfaction.Satisfied }),
            "inactive is red, active below target is yellow, achieved target is green");
        Require(new InventoryArtifactGoalFeedback(snapshot.Items[1].Artifact, InventoryPreferenceLevel.Priority, 0).State == InventoryIntentSatisfaction.Satisfied &&
            new InventoryArtifactGoalFeedback(snapshot.Items[0].Artifact, InventoryPreferenceLevel.Priority, 0).State == InventoryIntentSatisfaction.Unmet,
            "active-only must distinguish an enabled level-zero artifact from a disabled artifact");
        Require(new InventoryArtifactGoalFeedback(snapshot.Items[0].Artifact, InventoryPreferenceLevel.Avoid, 0).State == InventoryIntentSatisfaction.Satisfied &&
            new InventoryArtifactGoalFeedback(snapshot.Items[3].Artifact, InventoryPreferenceLevel.Avoid, 0).State == InventoryIntentSatisfaction.Unmet,
            "exclusion colors must be the reverse of activation colors");
        var preferences = AutoQueue(snapshot);
        var policy = InventoryOptimizationPolicyResolver.Resolve(snapshot, preferences);
        RuntimeStateSnapshot Runtime(long revision = 3, long epoch = 1, uint player = 5,
            RuntimeConsistencyState consistency = RuntimeConsistencyState.Consistent) => new("fixture", epoch, 1, revision, 1, player,
                RuntimeCapabilities.InventorySnapshot | RuntimeCapabilities.SettledInventoryObservation, consistency, 0, "");
        var feedback = new InventoryIntentResultFeedback(snapshot, policy, preferences, Runtime());
        Require(feedback.IsCurrent(Runtime(), preferences) &&
            !feedback.IsCurrent(Runtime(revision: 4), preferences) && !feedback.IsCurrent(Runtime(epoch: 2), preferences) &&
            !feedback.IsCurrent(Runtime(player: 6), preferences) && !feedback.IsCurrent(Runtime(consistency: RuntimeConsistencyState.PendingSettlement), preferences) &&
            !feedback.IsCurrent(Runtime(), InventoryArtifactIntentEditor.SetAutomatic(preferences, snapshot.Items[0].ItemKey)),
            "color results must invalidate on changed inventory, context, player, settlement or preferences");
        var localized = new Dictionary<string, Dictionary<string, string>>();
        InventoryOptimizationLocalization.Register((language, key, value) =>
        {
            if (!localized.ContainsKey(language)) localized[language] = new Dictionary<string, string>();
            localized[language][key] = value;
        });
        foreach (var entries in localized.Values)
        {
            foreach (var item in snapshot.Items)
            {
                var rule = preferences.ArtifactPreferences.Single(r => r.ItemKey == item.ItemKey);
                string description = InventoryOptimizationLocalization.FormatArtifactFeedback(rule, item.Artifact,
                    feedback.Find(item.ItemKey), key => entries[key]);
                string pending = InventoryOptimizationLocalization.FormatArtifactFeedback(rule, item.Artifact,
                    null, key => entries[key]);
                Require(description.Split('\n').Length == 2 && !description.Contains("{0}") &&
                    pending.Contains(entries[InventoryOptimizationLocalization.HudResultPending]),
                    "all locales must provide compact goal, current state and verified/pending feedback");
            }
        }
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
