using SephiriaEnhancements.ModelChecks.Runtime.Inventory;
using SephiriaEnhancements.Runtime.Inventory;
using SephiriaEnhancements.Inventory;

namespace SephiriaEnhancements.ModelChecks.Features.Inventory;

internal static class InventoryArtifactLevelBoundaryChecks
{
    internal static string Run()
    {
        VerifyActivationBoundaryAndSearchCoverage();
        VerifyUnregisteredUniqueArtifactCanReactivate();
        VerifyZeroMinimumRequiresActivation();
        VerifyTargetCompletionUsesOneScale();
        VerifyMinimumTargetsSaturate();
        VerifyNegativeLevelsRemainSemanticallyEquivalent();
        VerifyNativeLevelArithmeticBoundaries();
        VerifyFullInventoryUsesOccupiedCellSwap();
        VerifyComboTargetAllowsZero();
        return "negative/zero/positive activation; unique reactivation; zero target; normalized " +
            "completion; saturated minimum; negative equivalence; native " +
            "arithmetic; combo minimum; full inventory swap passed";
    }

    private static void VerifyUnregisteredUniqueArtifactCanReactivate()
    {
        InventorySnapshot snapshot = InventorySnapshotFixture.
            UnregisteredUniqueArtifactAtLevels(new[] { -1, 0 }, itemCell: 0);
        InventoryLayoutProjection current = InventoryLayoutProjection.Current(
            snapshot);
        ProjectedInventorySettlement before =
            InventorySettlementProjector.Evaluate(snapshot, current);
        ProjectedInventorySettlement after =
            InventorySettlementProjector.Evaluate(snapshot,
                current.WithCellsSwapped(0, 1));

        if (!snapshot.SettlementValidation.CurrentLayoutVerified ||
            !snapshot.SettlementValidation.LayoutProjectionReady ||
            !before.Succeeded || before.Artifacts[0].Enabled ||
            !before.Artifacts[0].PenaltyEnabled || !after.Succeeded ||
            !after.Artifacts[0].Enabled ||
            after.Artifacts[0].PenaltyEnabled)
        {
            throw new InvalidOperationException(
                "an unregistered unique artifact must remain disabled in the " +
                "current layout and be eligible to register in a valid candidate layout");
        }
    }

    private static void VerifyActivationBoundaryAndSearchCoverage()
    {
        InventorySnapshot snapshot = InventorySnapshotFixture.ArtifactsAtLevels(
            new[] { -1, 0 },
            new[] { 0 });
        InventoryLayoutProjection current = InventoryLayoutProjection.Current(
            snapshot);
        ProjectedInventorySettlement before =
            InventorySettlementProjector.Evaluate(snapshot, current);
        InventoryLayoutProjection moved = current.WithCellsSwapped(0, 1);
        ProjectedInventorySettlement after =
            InventorySettlementProjector.Evaluate(snapshot, moved);

        if (!before.Succeeded || before.Artifacts[0].Enabled ||
            before.Artifacts[0].DisplayedLevel != -1 ||
            before.Artifacts[0].CappedEffectiveLevel != 0 ||
            !before.Artifacts[0].PenaltyEnabled || !after.Succeeded ||
            !after.Artifacts[0].Enabled ||
            after.Artifacts[0].DisplayedLevel != 0 ||
            after.Artifacts[0].CappedEffectiveLevel != 0 ||
            after.Artifacts[0].PenaltyEnabled)
        {
            throw new InvalidOperationException(
                "artifact activation boundary must be level >= 0");
        }

        ResolvedInventoryOptimizationPolicy policy =
            InventoryOptimizationPolicyResolver.Resolve(snapshot,
                InventoryOptimizationPreferences.Default);
        InventoryOptimizationProposal proposal = InventoryOptimizer.Solve(
            snapshot, policy, new InventorySearchBudget(2, 100, 1000));
        if (!proposal.Succeeded || !proposal.Improved ||
            proposal.Layout.GetCell(0) != 1 ||
            proposal.CurrentScore.EnabledArtifactCount != 0 ||
            proposal.BestScore.EnabledArtifactCount != 1)
        {
            throw new InvalidOperationException(
                "negative-level artifacts must remain searchable and move to " +
                "an available level-zero cell: succeeded=" +
                proposal.Succeeded + ", improved=" + proposal.Improved +
                ", cell=" + (proposal.Layout?.GetCell(0) ?? -1) +
                ", beforeEnabled=" +
                proposal.CurrentScore?.EnabledArtifactCount +
                ", afterEnabled=" + proposal.BestScore?.EnabledArtifactCount +
                ", issues=" + string.Join(",", proposal.Issues));
        }
    }

    private static void VerifyZeroMinimumRequiresActivation()
    {
        InventorySnapshot snapshot = InventorySnapshotFixture.ArtifactsAtLevels(
            new[] { -1, 0 },
            new[] { 0 });
        var preferences = new InventoryOptimizationPreferences(
            InventorySearchEffort.Balanced, allowStoneTabletRotation: true,
            new[]
            {
                new ArtifactOptimizationPreference(100, 1000,
                    InventoryPreferenceLevel.Priority,
                    minimumEffectiveLevel: 0)
            }, Array.Empty<ComboOptimizationPreference>());
        ResolvedInventoryOptimizationPolicy policy =
            InventoryOptimizationPolicyResolver.Resolve(snapshot, preferences);
        var scorer = new InventoryOptimizationScorer(snapshot, policy);
        InventoryLayoutProjection current = InventoryLayoutProjection.Current(
            snapshot);
        InventoryOptimizationScore before = scorer.Score(current,
            InventorySettlementProjector.Evaluate(snapshot, current));
        InventoryLayoutProjection moved = current.WithCellsSwapped(0, 1);
        InventoryOptimizationScore after = scorer.Score(moved,
            InventorySettlementProjector.Evaluate(snapshot, moved));
        InventoryOptimizationTargetEvaluation evaluation = scorer.
            EvaluateTargets(
                InventorySettlementProjector.Evaluate(snapshot,
                    current),
                InventorySettlementProjector.Evaluate(snapshot,
                    moved)).Single();

        if (before.PriorityTargetsSatisfied != 0 ||
            after.PriorityTargetsSatisfied != 1 ||
            before.PriorityTargetCompletionPoints != 0 ||
            after.PriorityTargetCompletionPoints !=
                InventoryOptimizationScorer.TargetCompletionScale ||
            evaluation.Kind != InventoryOptimizationTargetKind.Artifact ||
            evaluation.RequiredValue != 0 ||
            evaluation.BeforeValue != 0 || evaluation.AfterValue != 0 ||
            evaluation.BeforeConditionReached ||
            !evaluation.AfterConditionReached ||
            evaluation.BeforeCompletionPoints != 0 ||
            evaluation.AfterCompletionPoints !=
                InventoryOptimizationScorer.TargetCompletionScale ||
            after.CompareTo(before) <= 0)
        {
            throw new InvalidOperationException(
                "minimum artifact level zero must mean enabled at zero, not " +
                "disabled below zero");
        }
    }

    private static void VerifyTargetCompletionUsesOneScale()
    {
        int artifactHalf =
            InventoryOptimizationScorer.CalculateTargetCompletionPoints(
                active: true, currentValue: 2, minimumValue: 4);
        int comboHalf =
            InventoryOptimizationScorer.CalculateTargetCompletionPoints(
                active: true, currentValue: 3, minimumValue: 6);
        int saturated =
            InventoryOptimizationScorer.CalculateTargetCompletionPoints(
                active: true, currentValue: 5, minimumValue: 4);
        int disabledZeroTarget =
            InventoryOptimizationScorer.CalculateTargetCompletionPoints(
                active: false, currentValue: 0, minimumValue: 0);
        int enabledZeroTarget =
            InventoryOptimizationScorer.CalculateTargetCompletionPoints(
                active: true, currentValue: 0, minimumValue: 0);

        if (artifactHalf != 5_000 || comboHalf != artifactHalf ||
            saturated != InventoryOptimizationScorer.TargetCompletionScale ||
            disabledZeroTarget != 0 ||
            enabledZeroTarget !=
                InventoryOptimizationScorer.TargetCompletionScale)
        {
            throw new InvalidOperationException(
                "artifact levels and combo counts must share one saturated " +
                "target-completion scale");
        }
    }

    private static void VerifyMinimumTargetsSaturate()
    {
        InventorySnapshot snapshot = InventorySnapshotFixture.ArtifactsAtLevels(
            new[] { 5, 4, 0, -1 }, new[] { 0, 3 });
        InventoryLayoutProjection current = InventoryLayoutProjection.Current(
            snapshot);
        var redistributed = new InventoryLayoutProjection(
            new[] { 1, 2 }, new[] { 0, 0 });

        var preferences = new InventoryOptimizationPreferences(
            InventorySearchEffort.Balanced,
            allowStoneTabletRotation: true,
            new[]
            {
                new ArtifactOptimizationPreference(100, 1000, InventoryPreferenceLevel.Priority,
                    minimumEffectiveLevel: 4)
            }, Array.Empty<ComboOptimizationPreference>());
        ResolvedInventoryOptimizationPolicy policy =
            InventoryOptimizationPolicyResolver.Resolve(snapshot,
                preferences);
        var scorer = new InventoryOptimizationScorer(snapshot, policy);
        InventoryOptimizationScore before = scorer.Score(current,
            InventorySettlementProjector.Evaluate(snapshot,
                current));
        InventoryOptimizationScore after = scorer.Score(redistributed,
            InventorySettlementProjector.Evaluate(snapshot,
                redistributed));

        if (before.PriorityTargetsSatisfied != 1 ||
            after.PriorityTargetsSatisfied != 1 ||
            before.PriorityTargetCompletionPoints !=
                InventoryOptimizationScorer.TargetCompletionScale ||
            after.PriorityTargetCompletionPoints !=
                InventoryOptimizationScorer.TargetCompletionScale ||
            before.EnabledArtifactCount != 1 ||
            after.EnabledArtifactCount != 2 ||
            after.CompareTo(before) <= 0)
        {
            throw new InvalidOperationException(
                "artifact minimum four must saturate at four");
        }
    }

    private static void VerifyNegativeLevelsRemainSemanticallyEquivalent()
    {
        InventorySnapshot snapshot = InventorySnapshotFixture.ArtifactsAtLevels(
            new[] { -5, -1 },
            new[] { 0 });
        ResolvedInventoryOptimizationPolicy policy =
            InventoryOptimizationPolicyResolver.Resolve(snapshot,
                InventoryOptimizationPreferences.Default);
        var scorer = new InventoryOptimizationScorer(snapshot, policy);
        InventoryLayoutProjection current = InventoryLayoutProjection.Current(
            snapshot);
        InventoryLayoutProjection moved = current.WithCellsSwapped(0, 1);
        InventoryOptimizationScore before = scorer.Score(current,
            InventorySettlementProjector.Evaluate(snapshot, current));
        InventoryOptimizationScore after = scorer.Score(moved,
            InventorySettlementProjector.Evaluate(snapshot, moved));

        if (before.EnabledArtifactCount != 0 ||
            after.EnabledArtifactCount != 0 ||
            before.CappedEffectiveArtifactLevelTotal != 0 ||
            after.CappedEffectiveArtifactLevelTotal != 0 ||
            after.CompareTo(before) >= 0)
        {
            throw new InvalidOperationException(
                "all negative levels must remain disabled and must not cause " +
                "a no-effect move");
        }
    }

    private static void VerifyComboTargetAllowsZero()
    {
        var preference = new ComboOptimizationPreference("EMBER",
            InventoryPreferenceLevel.Priority, targetCount: 0);
        if (preference.TargetCount != 0)
        {
            throw new InvalidOperationException(
                "combo target zero must not impose a minimum count of one");
        }
    }

    private static void VerifyFullInventoryUsesOccupiedCellSwap()
    {
        const int storage = 30;
        const int width = 6;
        InventorySnapshot snapshot =
            InventorySnapshotFixture.FullWithArtifactAndBlockers(width,
                storage, artifactCell: 0, targetLevelCell: storage - 1);
        ResolvedInventoryOptimizationPolicy policy =
            InventoryOptimizationPolicyResolver.Resolve(snapshot,
                InventoryOptimizationPreferences.Default);
        InventoryOptimizationProposal proposal = InventoryOptimizer.Solve(
            snapshot, policy, new InventorySearchBudget(3, 1500, 1000));
        bool planCreated = InventoryLayoutPlanner.TryCreate(snapshot,
            proposal.Layout, out InventoryApplicationPlan plan,
            out string issue);
        if (!proposal.Succeeded || !proposal.Improved ||
            proposal.Layout.GetCell(0) != storage - 1 ||
            proposal.BestScore.EnabledArtifactCount != 1 ||
            !planCreated ||
            plan.Swaps.Count != 1 || plan.Rotations.Count != 0 ||
            plan.Swaps[0].FirstCell != 0 ||
            plan.Swaps[0].SecondCell != storage - 1 ||
            plan.Swaps[0].ExpectedFirstItemKey != snapshot.Items[0].ItemKey ||
            plan.Swaps[0].ExpectedSecondItemKey != snapshot.Items[storage - 1].ItemKey)
        {
            throw new InvalidOperationException(
                "full inventory must optimize through one occupied-cell swap: " +
                "succeeded=" + proposal.Succeeded + ", improved=" +
                proposal.Improved + ", artifactCell=" +
                (proposal.Layout?.GetCell(0) ?? -1) + ", enabled=" +
                proposal.BestScore?.EnabledArtifactCount + ", plan=" +
                planCreated + ", swaps=" + (plan?.Swaps.Count ?? -1) +
                ", issue=" + issue);
        }
    }

    private static void VerifyNativeLevelArithmeticBoundaries()
    {
        AssertSingleArtifactSettlement("enchant reaches zero",
            baselineLevel: -1, enchantLevel: 1, multiplier: 0,
            disableCount: 0, globalActiveValue: 1, maxLevel: 5,
            expectedDisplayedLevel: 0, expectedEnabled: true,
            expectedCappedLevel: 0);
        AssertSingleArtifactSettlement("zero multiplier is identity",
            baselineLevel: 2, enchantLevel: 0, multiplier: 0,
            disableCount: 0, globalActiveValue: 1, maxLevel: 5,
            expectedDisplayedLevel: 2, expectedEnabled: true,
            expectedCappedLevel: 2);
        AssertSingleArtifactSettlement("negative multiplier crosses zero",
            baselineLevel: 2, enchantLevel: 0, multiplier: -1,
            disableCount: 0, globalActiveValue: 1, maxLevel: 5,
            expectedDisplayedLevel: -2, expectedEnabled: false,
            expectedCappedLevel: 0);
        AssertSingleArtifactSettlement("positive disable count disables",
            baselineLevel: 0, enchantLevel: 0, multiplier: 0,
            disableCount: 1, globalActiveValue: 1, maxLevel: 5,
            expectedDisplayedLevel: 0, expectedEnabled: false,
            expectedCappedLevel: 0);
        AssertSingleArtifactSettlement("zero global active value disables",
            baselineLevel: 0, enchantLevel: 0, multiplier: 0,
            disableCount: 0, globalActiveValue: 0, maxLevel: 5,
            expectedDisplayedLevel: 0, expectedEnabled: false,
            expectedCappedLevel: 0);
        InventorySnapshot capped = AssertSingleArtifactSettlement(
            "level above maximum is capped", baselineLevel: 7,
            enchantLevel: 0, multiplier: 0, disableCount: 0,
            globalActiveValue: 1, maxLevel: 5,
            expectedDisplayedLevel: 7, expectedEnabled: true,
            expectedCappedLevel: 5);
        ResolvedInventoryOptimizationPolicy policy =
            InventoryOptimizationPolicyResolver.Resolve(capped,
                InventoryOptimizationPreferences.Default);
        InventoryOptimizationScore cappedScore =
            new InventoryOptimizationScorer(capped, policy).Score(
                InventoryLayoutProjection.Current(capped),
                InventorySettlementProjector.Evaluate(capped,
                    InventoryLayoutProjection.Current(capped)));
        if (cappedScore.CappedEffectiveArtifactLevelTotal != 5 ||
            cappedScore.ExcessArtifactLevelTotal != 2)
        {
            throw new InvalidOperationException(
                "levels above max must cap effect and expose only the excess " +
                "as waste");
        }
    }

    private static InventorySnapshot AssertSingleArtifactSettlement(
        string scenario, int baselineLevel, int enchantLevel, int multiplier,
        int disableCount, int globalActiveValue, int maxLevel,
        int expectedDisplayedLevel, bool expectedEnabled,
        int expectedCappedLevel)
    {
        InventorySnapshot snapshot =
            InventorySnapshotFixture.SingleArtifactWithContributions(
                scenario, baselineLevel, enchantLevel, multiplier,
                disableCount, globalActiveValue, maxLevel, expectedEnabled,
                expectedCappedLevel);
        ProjectedInventorySettlement evaluated =
            InventorySettlementProjector.Evaluate(snapshot,
                InventoryLayoutProjection.Current(snapshot));
        if (!evaluated.Succeeded ||
            evaluated.Artifacts[0].DisplayedLevel != expectedDisplayedLevel ||
            evaluated.Artifacts[0].Enabled != expectedEnabled ||
            evaluated.Artifacts[0].CappedEffectiveLevel != expectedCappedLevel)
        {
            throw new InvalidOperationException(
                "native numeric boundary failed: " + scenario);
        }
        return snapshot;
    }

}
