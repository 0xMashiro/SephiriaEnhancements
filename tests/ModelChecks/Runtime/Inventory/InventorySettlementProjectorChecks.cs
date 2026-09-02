using SephiriaEnhancements.Runtime.Inventory;

namespace SephiriaEnhancements.ModelChecks.Runtime.Inventory;

internal static class InventorySettlementProjectorChecks
{
    internal static void Run()
    {
        InventorySnapshot rowSnapshot = InventorySnapshotFixture.RowDependentArtifact();
        ProjectedInventorySettlement movedRow =
            InventorySettlementProjector.Evaluate(rowSnapshot,
                new InventoryLayoutProjection(new[] { 2 }, new[] { 0 }));
        if (!rowSnapshot.SettlementValidation.LayoutProjectionReady || !movedRow.Succeeded ||
            movedRow.ComboCounts["FIRE"] != 0 || movedRow.ComboCounts["ICE"] != 1)
            throw new InvalidOperationException(
                "row-dependent categories must follow the candidate row");
        Console.WriteLine("InventorySettlementProjector: dynamic row categories passed");
    }
}
