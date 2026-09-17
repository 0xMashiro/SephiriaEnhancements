using SephiriaEnhancements.Inventory;
using SephiriaEnhancements.Runtime.Inventory;
using SephiriaEnhancements.ModelChecks.Runtime.Inventory;

namespace SephiriaEnhancements.ModelChecks.Features.Inventory;

internal static class InventoryItemRecoveryChecks
{
    internal static void Run()
    {
        var source = InventorySnapshotFixture.ArtifactsAtLevels(new[] { 2, 1, 0 }, new[] { 0, 1 });
        Require(InventoryArrangementBlockers.Find(source).Length == 0, "Known mechanisms need no item-ID whitelist.");
        var original = source.Items[0];
        var inactive = new ArtifactSnapshot(2, 10, 0, 0, 0, false, false, false, "", true,
            false, false, "Default", new CriteriaSnapshot(ArtifactActivationConditionKind.None,
                CriteriaEvaluationState.NotApplicable, CriteriaEvaluationState.NotApplicable),
            Array.Empty<string>(), Array.Empty<string>(), false, null);
        InventoryItemSnapshot Copy(InventoryItemSnapshot item, ArtifactSnapshot artifact, int? cell = null,
            int? quantity = null) => new(item.InstanceId, item.EntityId, quantity ?? item.Quantity,
                cell ?? item.CellIndex, cell ?? item.X, item.Y, item.Name, item.NameKey, item.NativeItemTypeName,
                item.Rarity, item.BaseCategories.ToArray(), item.Kind, artifact, item.StoneTablet);
        var rejected = new InventorySnapshot(3, 3, source.Cells.ToArray(),
            new[] { Copy(original, inactive), source.Items[1] });
        Require(InventoryArrangementBlockers.Find(rejected).Select(item => item.ItemKey)
            .SequenceEqual(new[] { original.ItemKey }), "Only the mismatching item is offered for removal.");
        string details = InventoryArrangementBlockers.Details(rejected, rejected.Items[0]);
        Require(details.Contains("enabled=False expectedEnabled=True") && details.Contains("expectedLimitedLevel=2") &&
            details.Contains("item=" + original.ItemKey), "Diagnostic retains item identity and both expected values.");
        var remaining = new InventorySnapshot(3, 3, source.Cells.ToArray(), new[] { source.Items[1] });
        Require(InventorySubBagTransferConfirmation.MatchesRemainingInventory(source, original.ItemKey, remaining),
            "Removal can be confirmed without requiring other unsupported effects to become supported.");
        Require(!InventorySubBagTransferConfirmation.MatchesRemainingInventory(source, original.ItemKey, source),
            "Sub-bag synchronization alone cannot confirm removal from the main inventory.");
        Require(!InventorySubBagTransferConfirmation.MatchesRemainingInventory(source, original.ItemKey,
            new InventorySnapshot(3, 3, source.Cells.ToArray(), new[] { Copy(source.Items[1], source.Items[1].Artifact, cell: 2) })),
            "Unrelated movement prevents continuation.");
        Require(!InventorySubBagTransferConfirmation.MatchesRemainingInventory(source, original.ItemKey,
            new InventorySnapshot(3, 3, source.Cells.ToArray(), new[] { Copy(source.Items[1], source.Items[1].Artifact, quantity: 2) })),
            "Unrelated quantity changes prevent continuation.");
        var globalFailure = new InventorySnapshot(3, 3, source.Cells.ToArray(), source.Items.ToArray(),
            arrangementBonusesEnabled: true);
        Require(!globalFailure.SettlementValidation.LayoutProjectionReady && InventoryArrangementBlockers.Find(globalFailure).Length == 0,
            "A global failure must not arbitrarily blame an item.");
        var duplicate = new InventorySnapshot(3, 3, source.Cells.ToArray(),
            new[] { Copy(original, inactive), Copy(original, inactive, cell: 1) });
        Require(InventoryArrangementBlockers.Find(duplicate).Length == 0,
            "Conflicting item identities must not authorize removal.");
        var selection = new InventoryRecoverySelection();
        Require(selection.Count == 0, "Moving effects out must begin with no preselected items.");
        selection.Toggle(original.ItemKey, eligible: true, freeSlots: 1);
        selection.Toggle(source.Items[1].ItemKey, eligible: true, freeSlots: 1);
        Require(selection.Selected(source.Items).SequenceEqual(new[] { original.ItemKey }), "Selection respects free slots.");
        selection.Toggle(original.ItemKey, eligible: true, freeSlots: 0);
        Require(selection.Count == 0, "A selected item can always be unchecked, even if space changes.");
        selection.Toggle(original.ItemKey, eligible: false, freeSlots: 2);
        Require(selection.Count == 0, "Storage-prohibited items cannot be selected.");
        selection.Toggle(original.ItemKey, eligible: true, freeSlots: 2);
        selection.Toggle(source.Items[1].ItemKey, eligible: true, freeSlots: 2);
        selection.Retain(source.Items.Select(item => item.ItemKey), freeSlots: 1);
        Require(selection.Count == 0, "A capacity change requires a new choice instead of silently choosing which effects to remove.");
        float offset = 0;
        for (int row = 0; row < 42; row++)
        {
            offset = InventoryRecoveryListLayout.Reveal(row, 42, offset);
            float top = row * InventoryRecoveryListLayout.RowHeight;
            Require(top >= offset && top + InventoryRecoveryListLayout.RowHeight <= offset + InventoryRecoveryListLayout.ViewportHeight,
                "Every keyboard-selected row must be fully visible.");
        }
        for (int row = 41; row >= 0; row--) offset = InventoryRecoveryListLayout.Reveal(row, 42, offset);
        Require(offset == 0 && InventoryRecoveryListLayout.Reveal(0, 0, 200) == 0,
            "Up navigation returns to the top and an empty list cannot preserve stale scroll.");
        Console.WriteLine("Inventory item recovery: specific blockers, diagnostics, remaining-item identity, movement and global failure checks passed.");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
