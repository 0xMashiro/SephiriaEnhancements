using SephiriaEnhancements.Runtime.Inventory;

namespace SephiriaEnhancements.Inventory.Integration
{
    internal sealed class NativeInventorySubBagTransfer
    {
        private readonly GridInventory inventory;
        private readonly InventorySnapshot source;
        private readonly InventoryItemSnapshot item;
        private readonly sbyte slot;

        internal NativeInventorySubBagTransfer(GridInventory inventory, InventorySnapshot source,
            InventoryItemSnapshot item, sbyte slot)
        {
            this.inventory = inventory;
            this.source = source;
            this.item = item;
            this.slot = slot;
        }

        internal static string UnavailableReason(GridInventory inventory, InventoryItemSnapshot item,
            out sbyte slot)
        {
            slot = -1;
            var native = inventory.FindItem(inventory.IdxToPos(item.CellIndex));
            if (native == null || native.InstanceID != item.InstanceId || native.EntityID != item.EntityId ||
                native.Quantity != item.Quantity) return InventoryOptimizationLocalization.Changed;
            if (native.Entity == null || native.Entity.cannotStoreInSubBag ||
                GridInventory.IsDestructibleItem(native.InstanceID)) return InventoryItemRecoveryLocalization.CannotStore;
            for (int index = 0; index < inventory.numberOfSubBagStorage && index <= sbyte.MaxValue; index++)
                if (!inventory.subBagMatrix.ContainsKey((sbyte)index))
                {
                    slot = (sbyte)index;
                    return null;
                }
            return InventoryItemRecoveryLocalization.NoSpace;
        }

        internal void Issue()
        {
            // Native boundary: the authority performs removal and serializes the item;
            // remote owners use the native Command, never mutate synchronized containers.
            var position = inventory.IdxToPos(item.CellIndex);
            if (inventory.isServer) inventory.ServerSwapSubBagAndInventory(position, slot);
            else inventory.CmdSwapSubBagAndInventory(position, slot);
        }

        internal bool Confirmed(InventorySnapshot observed) =>
            inventory.subBagMatrix.TryGetValue(slot, out var stored) &&
            stored.entityID == item.EntityId && stored.instanceID == item.InstanceId && stored.quantity == item.Quantity &&
            InventorySubBagTransferConfirmation.MatchesRemainingInventory(source, item.ItemKey, observed);
    }
}
