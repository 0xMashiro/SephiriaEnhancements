using SephiriaEnhancements.ModelChecks.Runtime.Inventory;
using SephiriaEnhancements.Runtime.Inventory;
using SephiriaEnhancements.Inventory;

namespace SephiriaEnhancements.ModelChecks.Features.Inventory;

internal static class InventoryEntityTargetChecks
{
    internal static string Run()
    {
        VerifyEntityTargetCountsOnce();
        VerifyInstanceRuleOverridesEntityMembership();
        return "any matching instance;single entity score;instance override passed";
    }

    private static void VerifyEntityTargetCountsOnce()
    {
        InventorySnapshot snapshot =
            PresetSnapshot();
        var preferences = InventoryOptimizationPreferences.Default;
        ResolvedInventoryOptimizationPolicy policy =
            InventoryOptimizationPolicyResolver.Resolve(snapshot, preferences);
        var scorer = new InventoryOptimizationScorer(snapshot, policy);
        ProjectedInventorySettlement settlement =
            InventorySettlementProjector.Evaluate(snapshot,
                InventoryLayoutProjection.Current(snapshot));
        InventoryOptimizationScore score = scorer.Score(
            InventoryLayoutProjection.Current(snapshot), settlement);
        InventoryOptimizationTargetEvaluation[] evaluations =
            scorer.EvaluateTargets(settlement, settlement);

        if (policy.ArtifactInstanceRules.Count != 0 ||
            policy.ArtifactEntityRules.Count != 1 ||
            score.PresetTargetsSatisfied != 1 ||
            score.PresetTargetCompletionPoints !=
                InventoryOptimizationScorer.TargetCompletionScale ||
            evaluations.Length != 1 ||
            evaluations[0].Target != "Artifact:1000:*" ||
            !evaluations[0].AfterConditionReached)
        {
            throw new InvalidOperationException(
                "an entity target must be one goal satisfied by any matching instance");
        }
    }

    private static void VerifyInstanceRuleOverridesEntityMembership()
    {
        InventorySnapshot snapshot =
            PresetSnapshot();
        var preferences = new InventoryOptimizationPreferences(
            InventorySearchEffort.Balanced, allowStoneTabletRotation: true,
            new[]
            {
                new ArtifactOptimizationPreference(100, 1000,
                    InventoryPreferenceLevel.Avoid, 0)
            }, Array.Empty<ComboOptimizationPreference>());
        ResolvedInventoryOptimizationPolicy policy =
            InventoryOptimizationPolicyResolver.Resolve(snapshot, preferences);
        var scorer = new InventoryOptimizationScorer(snapshot, policy);
        ProjectedInventorySettlement settlement =
            InventorySettlementProjector.Evaluate(snapshot,
                InventoryLayoutProjection.Current(snapshot));
        InventoryOptimizationTargetEvaluation[] evaluations =
            scorer.EvaluateTargets(settlement, settlement);

        if (policy.ArtifactInstanceRules.Count != 1 ||
            policy.ArtifactEntityRules.Count != 1 ||
            evaluations.Count(evaluation => evaluation.Target ==
                "Artifact:1000:100") != 1 ||
            evaluations.Count(evaluation => evaluation.Target ==
                "Artifact:1000:*") != 1)
        {
            throw new InvalidOperationException(
                "an instance rule must be evaluated separately and excluded from its entity group");
        }
    }
    private static InventorySnapshot PresetSnapshot()
    {
        var source = InventorySnapshotFixture.DuplicateArtifactsAtLevels(
            new[] { 4, 4 }, new[] { 0, 1 }, maxLevel: 5);
        return new InventorySnapshot(source.Width, source.Storage, source.Cells.ToArray(), source.Items.ToArray(),
            nativePreset: new NativePresetSnapshot(0, true, "Fire", 0, "Scholar",
                new[] { 1000 }, Array.Empty<string>()));
    }

}
