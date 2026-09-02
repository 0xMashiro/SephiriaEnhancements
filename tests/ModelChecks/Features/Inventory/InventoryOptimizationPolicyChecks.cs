using SephiriaEnhancements.ModelChecks.Runtime.Inventory;
using SephiriaEnhancements.Runtime.Inventory;
using SephiriaEnhancements.Inventory;

namespace SephiriaEnhancements.ModelChecks.Features.Inventory;

internal static class InventoryOptimizationPolicyChecks
{
    internal static void Run()
    {
        InventorySnapshot rowSnapshot = InventorySnapshotFixture.RowDependentArtifact();
        var explicitPreferences = new InventoryOptimizationPreferences(
            InventorySearchEffort.Fast, allowStoneTabletRotation: false,
            new[]
            {
                new ArtifactOptimizationPreference(-1, 301,
                    InventoryPreferenceLevel.Prefer),
                new ArtifactOptimizationPreference(31, 301,
                    InventoryPreferenceLevel.Priority)
            },
            new[]
            {
                new ComboOptimizationPreference("ICE",
                    InventoryPreferenceLevel.Priority, minimumCount: 1)
            });
        ResolvedInventoryOptimizationPolicy explicitPolicy =
            InventoryOptimizationPolicyResolver.Resolve(rowSnapshot,
                explicitPreferences);
        if (explicitPolicy.SearchEffort != InventorySearchEffort.Fast ||
            explicitPolicy.AllowStoneTabletRotation ||
            explicitPolicy.ArtifactInstanceRules[31].Source !=
                InventoryPreferenceSource.ManualInstance ||
            explicitPolicy.ArtifactInstanceRules[31].Level !=
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
            thoroughPreferences.ArtifactPreferences.Count != 2 ||
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
}
