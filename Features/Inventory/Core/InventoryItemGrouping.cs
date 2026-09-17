#nullable disable
using System;
using System.Linq;
using SephiriaEnhancements.Runtime.Inventory;

namespace SephiriaEnhancements.Inventory
{
    internal static class InventoryItemGrouping
    {
        // Grouping needs intact item identities and positions, not an effect model.
        internal static bool TryCreate(InventorySnapshot snapshot, out InventoryLayoutProjection layout)
        {
            layout = null;
            if (snapshot == null || snapshot.Width <= 0 || snapshot.Storage < 0 ||
                snapshot.Items.Count > snapshot.Storage || snapshot.Items.Any(item => item == null ||
                    item.Quantity <= 0 || item.CellIndex < 0 || item.CellIndex >= snapshot.Storage ||
                    item.X != item.CellIndex % snapshot.Width || item.Y != item.CellIndex / snapshot.Width) ||
                snapshot.Items.Select(item => item.ItemKey).Distinct().Count() != snapshot.Items.Count ||
                snapshot.Items.Select(item => item.CellIndex).Distinct().Count() != snapshot.Items.Count)
                return false;

            var cells = new int[snapshot.Items.Count];
            int destination = 0;
            foreach (int index in Enumerable.Range(0, snapshot.Items.Count)
                .OrderBy(index => snapshot.Items[index].NativeType)
                .ThenBy(index => snapshot.Items[index].EntityId)
                .ThenBy(index => snapshot.Items[index].CellIndex))
                cells[index] = destination++;
            layout = new InventoryLayoutProjection(cells,
                snapshot.Items.Select(item => item.StoneTablet?.Rotation ?? 0).ToArray());
            return true;
        }
    }
}
