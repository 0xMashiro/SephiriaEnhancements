using System.Collections.Generic;
using System.Linq;
using SephiriaEnhancements.Runtime.Inventory;

namespace SephiriaEnhancements.Inventory
{
    internal sealed class InventoryRecoverySelection
    {
        private readonly HashSet<InventoryItemKey> selected = new();
        internal int Count => selected.Count;
        internal bool Contains(InventoryItemKey key) => selected.Contains(key);
        internal void Toggle(InventoryItemKey key, bool eligible, int freeSlots)
        {
            if (selected.Remove(key)) return;
            if (eligible && selected.Count < freeSlots) selected.Add(key);
        }
        internal InventoryItemKey[] Selected(IEnumerable<InventoryItemSnapshot> items) =>
            items.Where(item => selected.Contains(item.ItemKey)).Select(item => item.ItemKey).ToArray();
        internal void Retain(IEnumerable<InventoryItemKey> eligible, int freeSlots)
        {
            selected.IntersectWith(eligible);
            if (selected.Count > freeSlots) selected.Clear();
        }
    }
}
