using SephiriaEnhancements.Inventory;
using SephiriaEnhancements.ModelChecks.Runtime.Inventory;
using SephiriaEnhancements.Runtime;
using SephiriaEnhancements.Runtime.Inventory;

namespace SephiriaEnhancements.ModelChecks.Features.Inventory;

internal static class InventoryArrangementChecks
{
    internal static void Run()
    {
        UndoUsesIdentityAndContext();
        Console.WriteLine("Inventory arrangement: undo identity/context and native operation plans passed");
    }

    private static void UndoUsesIdentityAndContext()
    {
        var before = InventorySnapshotFixture.Tablets(1, 2);
        var undo = new InventoryArrangementUndo(before, State());
        Check(undo.Matches(State()) && !undo.Matches(null), "undo requires captured local runtime");
        Check(!undo.Matches(State(revision: 8)), "manual move/add/remove revision invalidates undo");
        Check(!undo.Matches(State(epoch: 2)), "local context change invalidates undo");
        Check(!undo.Matches(State(player: 2)), "player replacement invalidates undo");
        var reordered = new InventorySnapshot(before.Width, before.Storage, before.Cells.ToArray(), before.Items.Reverse().ToArray());
        var target = undo.RestoreLayout(reordered);
        Check(target != null && target.GetCell(0) == 1 && target.GetCell(1) == 0 &&
            target.GetRotation(0) == 2 && target.GetRotation(1) == 1, "undo maps cells and rotations by identity, not list order");
        Check(undo.RestoreLayout(InventorySnapshotFixture.ArtifactsAtLevels(new[] { 0, 0 }, new[] { 0, 1 })) == null,
            "replacement identities cannot receive old layout");
        Check(undo.RestoreLayout(InventorySnapshotFixture.Tablets(1, 2)) != null, "matching inventory can restore");
        Check(undo.RestoreLayout(InventorySnapshotFixture.ArtifactsAtLevels(new[] { 0, 0, 0 }, new[] { 0 })) == null,
            "changed capacity/items cannot restore");
        var current = InventoryLayoutProjection.Current(before);
        Check(InventoryLayoutPlanner.TryCreate(before, current, out var plan, out _), "undo target plan exists");
        var application = new InventoryLayoutApplication(before, State(), current, plan,
            InventorySettlementProjector.Evaluate(before, current), 10);
        Check(application.IsUndo && application.Proposal == null && application.TargetLayout == current,
            "undo application does not fabricate an optimization proposal");
    }

    private static RuntimeStateSnapshot State(long epoch = 1, long revision = 7, uint player = 1) =>
        new("test", epoch, 1, revision, 1, player, RuntimeCapabilities.InventorySnapshot,
            RuntimeConsistencyState.Consistent, 0, string.Empty);

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
