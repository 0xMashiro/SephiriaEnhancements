using SephiriaEnhancements.ModelChecks.Runtime.Inventory;
using SephiriaEnhancements.Runtime.Inventory;
using SephiriaEnhancements.Inventory;

namespace SephiriaEnhancements.ModelChecks.Features.Inventory;

internal static class InventoryOptimizationPolicyChecks
{
    internal static void Run()
    {
        VerifyPresetAndManualPriorities();
        VerifyComboTargetConditions();
        VerifyMissingComboDoesNotBlockSorting();
        InventorySnapshot rowSnapshot = InventorySnapshotFixture.RowDependentArtifact();
        var explicitPreferences = new InventoryOptimizationPreferences(
            InventorySearchEffort.Fast, allowStoneTabletRotation: false,
            new[]
            {
                new ArtifactOptimizationPreference(31, 301,
                    InventoryPreferenceLevel.Priority)
            },
            new[]
            {
                new ComboOptimizationPreference("ICE",
                    InventoryPreferenceLevel.Priority, targetCount: 1)
            });
        ResolvedInventoryOptimizationPolicy explicitPolicy =
            InventoryOptimizationPolicyResolver.Resolve(rowSnapshot,
                explicitPreferences);
        if (explicitPolicy.SearchEffort != InventorySearchEffort.Fast ||
            explicitPolicy.AllowStoneTabletRotation ||
            explicitPolicy.ArtifactInstanceRules[new InventoryItemKey(301, 31)].Source !=
                InventoryPreferenceSource.ManualInstance ||
            explicitPolicy.ArtifactInstanceRules[new InventoryItemKey(301, 31)].Level !=
                InventoryPreferenceLevel.Priority ||
            explicitPolicy.ComboRules["ICE"].Source !=
                InventoryPreferenceSource.UserCategoryRule)
            throw new InvalidOperationException(
                "explicit inventory preferences must override broader rules");
        InventoryOptimizationPreferences thoroughPreferences =
            explicitPreferences.WithExecutionSettings(
                InventoryOptimizationTendencyPolicy.GetSearchEffort(
                    InventoryOptimizationTendency.Aggressive),
                allowStoneTabletRotation: true);
        if (InventoryOptimizationTendencyPolicy.GetSearchEffort(
                InventoryOptimizationTendency.Stable) != InventorySearchEffort.Fast ||
            InventoryOptimizationTendencyPolicy.GetSearchEffort(
                InventoryOptimizationTendency.Automatic) !=
                    InventorySearchEffort.Balanced ||
            InventoryOptimizationTendencyPolicy.GetSearchEffort(
                InventoryOptimizationTendency.Aggressive) !=
                    InventorySearchEffort.Thorough ||
            thoroughPreferences.SearchEffort != InventorySearchEffort.Thorough ||
            !thoroughPreferences.AllowStoneTabletRotation ||
            thoroughPreferences.ArtifactPreferences.Count != 1 ||
            thoroughPreferences.ComboPreferences.Count != 1)
            throw new InvalidOperationException(
                "optimization tendencies must tune automatic search without losing player intent");
        InventoryOptimizationProposal explicitProposal = InventoryOptimizer.Solve(
            rowSnapshot, explicitPolicy,
            new InventorySearchBudget(maximumImprovementRounds: 4,
                maximumCandidateEvaluations: 100,
                maximumElapsedMilliseconds: 1000));
        InventoryOptimizationTargetEvaluation iceEvaluation =
            explicitProposal.TargetEvaluations.Single(
                evaluation => evaluation.Target == "Combo:ICE");
        if (!explicitProposal.Improved ||
            explicitProposal.BestScore.PriorityTargetsSatisfied <=
                explicitProposal.CurrentScore.PriorityTargetsSatisfied ||
            iceEvaluation.Kind != InventoryOptimizationTargetKind.ComboCategory ||
            iceEvaluation.RequiredValue != 1 ||
            iceEvaluation.BeforeValue != 0 || iceEvaluation.AfterValue != 1 ||
            iceEvaluation.BeforeConditionReached ||
            !iceEvaluation.AfterConditionReached ||
            iceEvaluation.BeforeCompletionPoints != 0 ||
            iceEvaluation.AfterCompletionPoints !=
                InventoryOptimizationScorer.TargetCompletionScale)
            throw new InvalidOperationException(
                "explicit required combo must drive and evaluate the proposal target");
        Console.WriteLine("InventoryOptimizationPolicy: precedence, capture and target evaluation passed");
    }

    private static void VerifyPresetAndManualPriorities()
    {
        InventorySnapshot source = InventorySnapshotFixture.RowDependentArtifact();
        var snapshot = new InventorySnapshot(source.Width, source.Storage,
            source.Cells.ToArray(), source.Items.ToArray(),
            nativePreset: new NativePresetSnapshot(0, true, "Fire", 7, "Scholar",
                new[] { 301 }, new[] { "FIRE" }),
            comboCategories: source.ComboCategories.ToArray());
        var current = InventoryLayoutProjection.Current(snapshot);
        var moved = current.WithCellsSwapped(0, 2);
        var before = InventorySettlementProjector.Evaluate(snapshot, current);
        var after = InventorySettlementProjector.Evaluate(snapshot, moved);
        var automatic = InventoryOptimizationPolicyResolver.Resolve(snapshot,
            InventoryOptimizationPreferences.Default);
        var automaticScore = new InventoryOptimizationScorer(snapshot, automatic).Score(current, before);
        if (automatic.ArtifactEntityRules[301].Source != InventoryPreferenceSource.NativePreset ||
            automatic.ComboRules["FIRE"].Source != InventoryPreferenceSource.NativePreset ||
            automaticScore.PriorityTargetsSatisfied != 0 || automaticScore.PresetTargetsSatisfied != 1)
            throw new InvalidOperationException("native preset preferences must remain automatic, below manual targets");

        var preferences = new InventoryOptimizationPreferences(InventorySearchEffort.Balanced, true,
            new[] { new ArtifactOptimizationPreference(31, 301, InventoryPreferenceLevel.Priority, 0) },
            new[] { new ComboOptimizationPreference("ICE", InventoryPreferenceLevel.Priority, 1) });
        var policy = InventoryOptimizationPolicyResolver.Resolve(snapshot, preferences);
        var scorer = new InventoryOptimizationScorer(snapshot, policy);
        var beforeScore = scorer.Score(current, before);
        var afterScore = scorer.Score(moved, after);
        if (beforeScore.PriorityTargetsSatisfied != 1 || afterScore.PriorityTargetsSatisfied != 2 ||
            beforeScore.PresetTargetsSatisfied != 1 || afterScore.PresetTargetsSatisfied != 0 ||
            afterScore.CompareTo(beforeScore) <= 0)
            throw new InvalidOperationException("manual goals must override preset preferences without extra player priority tiers");

        preferences = InventoryArtifactIntentEditor.Remove(preferences, new InventoryItemKey(301, 31));
        if (InventoryOptimizationPolicyResolver.Resolve(snapshot, preferences).ArtifactEntityRules[301].Source !=
            InventoryPreferenceSource.NativePreset)
            throw new InvalidOperationException("Automatic must restore the native preset preference");
    }

    private static void VerifyComboTargetConditions()
    {
        var snapshot = InventorySnapshotFixture.RowDependentArtifact();
        var current = InventoryLayoutProjection.Current(snapshot);
        var moved = current.WithCellsSwapped(0, 2);
        var before = InventorySettlementProjector.Evaluate(snapshot, current);
        var after = InventorySettlementProjector.Evaluate(snapshot, moved);
        foreach (var intent in new[] { InventoryPreferenceLevel.Priority, InventoryPreferenceLevel.Avoid })
            foreach (int count in new[] { 0, 1, 2 })
            {
                var preferences = new InventoryOptimizationPreferences(InventorySearchEffort.Balanced, true,
                    Array.Empty<ArtifactOptimizationPreference>(),
                    new[] { new ComboOptimizationPreference("ICE", intent, count) });
                var scorer = new InventoryOptimizationScorer(snapshot,
                    InventoryOptimizationPolicyResolver.Resolve(snapshot, preferences));
                bool beforeReached = intent == InventoryPreferenceLevel.Priority ? 0 >= count : 0 <= count;
                bool afterReached = intent == InventoryPreferenceLevel.Priority ? 1 >= count : 1 <= count;
                var evidence = new Dictionary<string, InventoryTargetSearchEvidence>();
                scorer.ObserveTargets(before, evidence);
                var evaluation = scorer.EvaluateTargets(before, after, evidence).Single();
                var beforeScore = scorer.Score(current, before);
                var afterScore = scorer.Score(moved, after);
                if (evaluation.BeforeValue != 0 || evaluation.AfterValue != 1 ||
                    evaluation.BeforeConditionReached != beforeReached || evaluation.AfterConditionReached != afterReached ||
                    evidence["Combo:ICE"].ConditionObserved != beforeReached ||
                    (evaluation.BeforeCompletionPoints == InventoryOptimizationScorer.TargetCompletionScale) != beforeReached ||
                    (evaluation.AfterCompletionPoints == InventoryOptimizationScorer.TargetCompletionScale) != afterReached ||
                    (intent == InventoryPreferenceLevel.Avoid &&
                        (beforeScore.AvoidedTargetsActive != (beforeReached ? 0 : 1) ||
                         afterScore.AvoidedTargetsActive != (afterReached ? 0 : 1))) ||
                    (intent == InventoryPreferenceLevel.Priority &&
                        (beforeScore.PriorityTargetsSatisfied != (beforeReached ? 1 : 0) ||
                         afterScore.PriorityTargetsSatisfied != (afterReached ? 1 : 0))))
                    throw new InvalidOperationException($"combo {intent} {count} must agree across scoring, evidence and result reporting");
            }
    }

    private static void VerifyMissingComboDoesNotBlockSorting()
    {
        var source = InventorySnapshotFixture.ArtifactsAtLevels(new[] { -1, 2 }, new[] { 0 });
        foreach (bool includeCategory in new[] { false, true })
            foreach (int count in new[] { 0, 1 })
            {
                var snapshot = new InventorySnapshot(source.Width, source.Storage,
                    source.Cells.ToArray(), source.Items.ToArray(),
                    comboCategories: includeCategory
                        ? new[] { new ComboCategorySnapshot("MISSING", 0, 0, 0, 0, 0,
                        Array.Empty<int>(), Array.Empty<int>(), false) }
                        : Array.Empty<ComboCategorySnapshot>());
                var preferences = new InventoryOptimizationPreferences(InventorySearchEffort.Balanced, true,
                    Array.Empty<ArtifactOptimizationPreference>(),
                    new[] { new ComboOptimizationPreference("MISSING", InventoryPreferenceLevel.Priority, count) });
                var proposal = InventoryOptimizerSelector.Solve(snapshot,
                    InventoryOptimizationPolicyResolver.Resolve(snapshot, preferences), new InventorySearchBudget(2, 100, 1000));
                var evaluation = proposal.TargetEvaluations.Single();
                if (!proposal.Succeeded || !proposal.Improved || proposal.BestScore.EnabledArtifactCount != 1 ||
                    evaluation.AfterValue != 0 || evaluation.AfterConditionReached != (count == 0) ||
                    proposal.BestScore.PriorityTargetsSatisfied != (count == 0 ? 1 : 0) ||
                    evaluation.Reachability != (count == 0
                        ? InventoryTargetReachability.SelectedLayoutReachesCondition
                        : InventoryTargetReachability.ProvenUnreachable))
                    throw new InvalidOperationException("missing combo artifacts must not block otherwise useful sorting; zero imposes no minimum");
            }
    }
}
