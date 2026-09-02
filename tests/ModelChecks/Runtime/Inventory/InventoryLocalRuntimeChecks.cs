using SephiriaEnhancements.Inventory;
using SephiriaEnhancements.Runtime.Inventory;

namespace SephiriaEnhancements.ModelChecks.Runtime.Inventory;

internal static class InventoryLocalRuntimeChecks
{
    internal static string Run()
    {
        VerifySettledBaselineInference();
        VerifyNativeOperationConfirmation();
        return "local baseline inversion and native operation confirmation passed";
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
            secondCell: 1, expectedFirstInstanceId: 100,
            expectedSecondInstanceId: 101);
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
}
