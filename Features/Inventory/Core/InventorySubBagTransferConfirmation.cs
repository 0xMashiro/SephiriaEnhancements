using System.Linq;
using SephiriaEnhancements.Runtime.Inventory;

namespace SephiriaEnhancements.Inventory
{
    internal static class InventorySubBagTransferConfirmation
    {
        internal static bool MatchesRemainingInventory(InventorySnapshot source, InventoryItemKey moved,
            InventorySnapshot observed) => observed != null && source.Width == observed.Width &&
            source.Items.Count(item => item.ItemKey == moved) == 1 &&
            observed.Items.Select(item => item.ItemKey).Distinct().Count() == observed.Items.Count &&
            source.Storage == observed.Storage && observed.Items.Count == source.Items.Count - 1 &&
            !observed.Items.Any(item => item.ItemKey == moved) &&
            source.Items.Where(item => item.ItemKey != moved).All(item => observed.Items.Any(other =>
                other.ItemKey == item.ItemKey && other.CellIndex == item.CellIndex && other.Quantity == item.Quantity &&
                other.StoneTablet?.Rotation == item.StoneTablet?.Rotation));
    }
}
