using SephiriaEnhancements.Inventory;
using SephiriaEnhancements.Runtime;
using SephiriaEnhancements.Runtime.Inventory;
using SephiriaEnhancements.ModelChecks.Runtime.Inventory;

namespace SephiriaEnhancements.ModelChecks.Features.Inventory;

internal static class InventoryItemGroupingChecks
{
    internal static void Run()
    {
        var known = InventorySnapshotFixture.ArtifactsAtLevels(new[] { 0, 1, 2, 3, 4, 5 }, new[] { 4, 1, 5 });
        var source = new InventorySnapshot(6, 6, known.Cells.ToArray(), known.Items.Reverse().ToArray(),
            arrangementBonusesEnabled: true);
        Require(!source.SettlementValidation.LayoutProjectionReady, "Fixture must have unmodeled effects.");
        Require(InventoryItemGrouping.TryCreate(source, out var target), "Unknown effects must not prevent grouping.");
        Require(source.Items.Select((item, index) => (item.EntityId, Cell: target.GetCell(index)))
            .OrderBy(pair => pair.EntityId).Select(pair => pair.Cell).SequenceEqual(new[] { 0, 1, 2 }),
            "Same-type items must be grouped deterministically with gaps at the end.");
        Require(InventoryLayoutPlanner.TryCreate(source, target, out var plan, out _, verifyEffectInputs: false),
            "Physical planning cannot require a complete effect model.");
        var application = new InventoryLayoutApplication(source, Runtime(1), target, plan, 20);
        Require(!application.IsUndo && !application.VerifyEffects && application.Purpose == InventoryApplicationPurpose.Grouping,
            "Grouping must not masquerade as optimization or undo.");
        long revision = 1;
        while (application.NextSwap < plan.Swaps.Count)
        {
            var step = plan.Swaps[application.NextSwap];
            var layout = application.ConfirmedLayout.WithCellsSwapped(step.FirstCell, step.SecondCell);
            var observed = Place(source, layout);
            application.BeginSwap(revision);
            Require(!application.TryObservePendingOperation(observed, Runtime(revision), out _), "No stale acknowledgements.");
            Require(!application.TryObservePendingOperation(observed, Runtime(revision + 1, player: 2), out _), "No other-player acknowledgements.");
            Require(application.TryObservePendingOperation(observed, Runtime(++revision), out var report) && report.Matched,
                "The actual whole layout must advance despite unverified effects.");
            Require(!application.TryObservePendingOperation(observed, Runtime(revision + 1), out _), "No duplicate acknowledgements.");
        }
        var final = Place(source, target);
        Require(InventoryApplicationConfirmation.MatchesTarget(final, source, target), "Every item and quantity must survive.");
        Require(!InventoryApplicationConfirmation.VerifyStep(final, source, target).Matched,
            "Ordinary optimization must retain effect verification.");
        var changed = new InventorySnapshot(6, 6, final.Cells.ToArray(), final.Items.Select((item, i) =>
            i == 0 ? Copy(item, item.CellIndex, quantity: item.Quantity + 1) : item).ToArray());
        Require(!InventoryApplicationConfirmation.VerifyStep(changed, source, target, verifyEffects: false).Matched,
            "Ignoring effect predictions must never ignore quantity changes.");
        var removed = new InventorySnapshot(6, 6, final.Cells.ToArray(), final.Items.Skip(1).ToArray());
        Require(!InventoryApplicationConfirmation.VerifyStep(removed, source, target, verifyEffects: false).Matched,
            "Missing items cannot be acknowledged.");
        var duplicate = new InventorySnapshot(6, 6, source.Cells.ToArray(), new[] { source.Items[0], Copy(source.Items[0], 0) });
        Require(!InventoryItemGrouping.TryCreate(duplicate, out _), "Duplicate identities cannot be grouped.");
        var collision = new InventorySnapshot(6, 6, source.Cells.ToArray(), new[] { source.Items[0], Copy(source.Items[1], source.Items[0].CellIndex) });
        Require(!InventoryItemGrouping.TryCreate(collision, out _), "Overlapping source cells cannot be grouped.");
        var invalidQuantity = new InventorySnapshot(6, 6, source.Cells.ToArray(), new[] { Copy(source.Items[0], 0, quantity: 0) });
        Require(!InventoryItemGrouping.TryCreate(invalidQuantity, out _), "Invalid quantities cannot be grouped.");
        var tablets = InventorySnapshotFixture.Tablets(3, 1);
        Require(InventoryItemGrouping.TryCreate(tablets, out var tabletLayout) &&
            tabletLayout.GetRotation(0) == 3 && tabletLayout.GetRotation(1) == 1,
            "Grouping must preserve stone tablet rotation.");
        var history = new InventoryArrangementUndo(source, Runtime(revision), verifyEffects: false);
        var restored = history.RestoreLayout(final);
        Require(restored != null && !history.VerifyEffects && history.Matches(Runtime(revision)), "Grouping remains undoable.");
        Require(!history.Matches(Runtime(revision + 1)) && !history.Matches(Runtime(revision, player: 2)),
            "Undo approval cannot survive unrelated changes or owner replacement.");
        Require(InventoryItemGrouping.TryCreate(final, out var repeated) &&
            InventoryLayoutPlanner.TryCreate(final, repeated, out var noChange, out _, verifyEffectInputs: false) && noChange.OperationCount == 0,
            "Grouping is idempotent and must report no movement on repeated use.");
        Console.WriteLine("Inventory grouping: unknown effects, exact item preservation, stale/foreign acknowledgements, rotation, undo and repeat checks passed.");
    }

    private static RuntimeStateSnapshot Runtime(long revision, uint player = 1) => new("fixture", 1, 1, revision, 1, player,
        RuntimeCapabilities.InventorySnapshot | RuntimeCapabilities.SettledInventoryObservation,
        RuntimeConsistencyState.Consistent, 0, "");

    private static InventorySnapshot Place(InventorySnapshot source, InventoryLayoutProjection layout) =>
        new(source.Width, source.Storage, source.Cells.ToArray(), source.Items.Select((item, index) =>
            Copy(item, layout.GetCell(index))).ToArray(), arrangementBonusesEnabled: true);

    private static InventoryItemSnapshot Copy(InventoryItemSnapshot item, int cell, int? quantity = null) =>
        new(item.InstanceId, item.EntityId, quantity ?? item.Quantity, cell, cell, 0, item.Name, item.NameKey,
            item.NativeItemTypeName, item.Rarity, item.BaseCategories.ToArray(), item.Kind, item.Artifact, item.StoneTablet);

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
