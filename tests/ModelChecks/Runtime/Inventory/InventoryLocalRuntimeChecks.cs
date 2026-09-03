using SephiriaEnhancements.Inventory;
using SephiriaEnhancements.Runtime.Inventory;

namespace SephiriaEnhancements.ModelChecks.Runtime.Inventory;

internal static class InventoryLocalRuntimeChecks
{
    internal static string Run()
    {
        VerifySettledBaselineInference();
        VerifyNativeOperationConfirmation();
        VerifyStepRejectsConcurrentInventoryChanges();
        VerifyStepRejectsChangedSettlement();
        VerifyIntermediateHardRequirements();
        return "baseline inversion; VerifyStepRejectsConcurrentInventoryChanges; " +
            "VerifyStepRejectsChangedSettlement; VerifyIntermediateHardRequirements passed";
    }

    private static void VerifySettledBaselineInference()
    {
        var contributions = new InventoryKnownCellContributions(
            enchantLevel: 2, fixedLevel: 3, fixedDisableCount: 1,
            fixedCriteriaBypassCount: 2, fixedLevelMultiplier: 1,
            tabletLevel: 4, tabletDisableCount: 2,
            tabletCriteriaBypassCount: 1, tabletLevelMultiplier: 2);
        if (!InventoryBaselineInference.TryInfer(settledLevel: 56,
                settledMaximumLevel: 9, settledTemporaryLevel: 6,
                settledLevelMultiplier: 4, settledDisableCount: 5,
                settledCriteriaBypassCount: 4,
                artifactEffectsEnabled: true, artifactOccupiesCell: true,
                contributions, out InventoryCellSettlementSnapshot result) ||
            !result.BaselineKnown || result.BaselineLevel != 5 ||
            result.BaselineMaximumLevel != -1 ||
            result.BaselineTemporaryLevel != 6 ||
            result.BaselineLevelMultiplier != 1 ||
            result.BaselineDisableCount != 2 ||
            result.BaselineCriteriaBypassCount != 1)
        {
            throw new InvalidOperationException(
                "settled client matrices must invert to the stable baseline");
        }

        if (InventoryBaselineInference.TryInfer(settledLevel: 57,
                settledMaximumLevel: 9, settledTemporaryLevel: 0,
                settledLevelMultiplier: 4, settledDisableCount: 5,
                settledCriteriaBypassCount: 4,
                artifactEffectsEnabled: true, artifactOccupiesCell: true,
                contributions, out _))
        {
            throw new InvalidOperationException(
                "non-integral native level inversion must fail closed");
        }
    }

    private static void VerifyNativeOperationConfirmation()
    {
        InventorySnapshot source = InventorySnapshotFixture.ArtifactsAtLevels(
            new[] { 0, 0 }, new[] { 0, 1 });
        InventorySnapshot swapped = InventorySnapshotFixture.ArtifactsAtLevels(
            new[] { 0, 0 }, new[] { 1, 0 });
        var operation = new InventorySwapOperation(firstCell: 0,
            secondCell: 1, expectedFirstItemKey: source.Items[0].ItemKey,
            expectedSecondItemKey: source.Items[1].ItemKey);
        var target = new InventoryLayoutProjection(new[] { 1, 0 },
            new[] { 0, 0 });

        if (InventoryApplicationConfirmation.IsSwapObserved(source,
                operation) ||
            !InventoryApplicationConfirmation.IsSwapObserved(swapped,
                operation) ||
            InventoryApplicationConfirmation.MatchesTarget(source, source,
                target) ||
            !InventoryApplicationConfirmation.MatchesTarget(swapped, source,
                target))
        {
            throw new InvalidOperationException(
                "application must advance only after the locally observed snapshot confirms the native operation");
        }
    }

    private static void VerifyStepRejectsConcurrentInventoryChanges()
    {
        int[] levels = { 1, 2, 3, 4 };
        var source = InventorySnapshotFixture.ArtifactsAtLevels(levels, new[] { 0, 1, 2 });
        var expected = InventoryLayoutProjection.Current(source).WithCellsSwapped(0, 1);
        var swapped = InventorySnapshotFixture.ArtifactsAtLevels(levels, new[] { 1, 0, 2 });
        var operation = new InventorySwapOperation(0, 1, source.Items[0].ItemKey, source.Items[1].ItemKey);
        if (!InventoryApplicationConfirmation.VerifyStep(swapped, source, expected).Matched)
            throw new InvalidOperationException("an independently settled swap must be confirmed");

        var changedItems = swapped.Items.ToArray();
        var third = changedItems[2];
        changedItems[2] = new InventoryItemSnapshot(third.InstanceId, third.EntityId, 2,
            third.CellIndex, third.X, third.Y, third.Name, third.NameKey,
            third.NativeItemTypeName, third.Rarity, third.BaseCategories.ToArray(),
            third.Kind, third.Artifact, third.StoneTablet);
        var changedQuantity = new InventorySnapshot(swapped.Width, swapped.Storage,
            swapped.Cells.ToArray(), changedItems);
        var interruptions = new[]
        {
            InventorySnapshotFixture.ArtifactsAtLevels(levels, new[] { 1, 0, 3 }),
            InventorySnapshotFixture.ArtifactsAtLevels(levels, new[] { 1, 0 }),
            InventorySnapshotFixture.ArtifactsAtLevels(levels, new[] { 1, 0, 2, 3 }),
            changedQuantity
        };
        foreach (var actual in interruptions)
        {
            if (!InventoryApplicationConfirmation.IsSwapObserved(actual, operation) ||
                InventoryApplicationConfirmation.VerifyStep(actual, source, expected).Matched)
                throw new InvalidOperationException(
                    "the expected pair moved, but an unrelated move, removal, addition or quantity change must stop the plan");
        }
    }

    private static void VerifyStepRejectsChangedSettlement()
    {
        var source = InventorySnapshotFixture.ArtifactsAtLevels(new[] { 1, 2, 3 }, new[] { 0, 1, 2 });
        var expected = InventoryLayoutProjection.Current(source).WithCellsSwapped(0, 1);
        var actual = InventorySnapshotFixture.ArtifactsAtLevels(new[] { 1, 2, 0 }, new[] { 1, 0, 2 });
        var report = InventoryApplicationConfirmation.VerifyStep(actual, source, expected);
        if (!InventoryApplicationConfirmation.MatchesTarget(actual, source, expected) || report.Matched ||
            !report.Mismatches.Contains("CellLevel:2"))
            throw new InvalidOperationException(
                "matching positions must not hide an unrelated artifact's changed settlement");
    }

    private static void VerifyIntermediateHardRequirements()
    {
        int[] levels = { 1, 2, 3 };
        var source = InventorySnapshotFixture.ArtifactsAtLevels(levels, new[] { 0, 1, 2 }, maxLevel: 3);
        var preferences = InventoryArtifactIntentEditor.PlacePriority(
            InventoryOptimizationPreferences.Default, source.Items[0].InstanceId, source.Items[0].EntityId, 0);
        preferences = InventoryArtifactIntentEditor.SetStrength(preferences,
            source.Items[0].ItemKey, InventoryConstraintStrength.Hard);
        var scorer = new InventoryOptimizationScorer(source,
            InventoryOptimizationPolicyResolver.Resolve(source, preferences));
        var intermediate = InventoryLayoutProjection.Current(source).WithCellsSwapped(0, 1);
        var final = intermediate.WithCellsSwapped(1, 2);
        var intermediateActual = InventorySnapshotFixture.ArtifactsAtLevels(levels, new[] { 1, 0, 2 }, maxLevel: 3);
        var finalActual = InventorySnapshotFixture.ArtifactsAtLevels(levels, new[] { 2, 0, 1 }, maxLevel: 3);
        if (scorer.Score(intermediate, InventorySettlementProjector.Evaluate(source, intermediate)).HardConstraintsSatisfied ||
            !scorer.Score(final, InventorySettlementProjector.Evaluate(source, final)).HardConstraintsSatisfied ||
            !InventoryApplicationConfirmation.VerifyStep(intermediateActual, source, intermediate).Matched ||
            !InventoryApplicationConfirmation.VerifyStep(finalActual, source, final).Matched)
            throw new InvalidOperationException(
                "verified intermediate moves may be below a Hard target that the final layout satisfies");
    }
}
