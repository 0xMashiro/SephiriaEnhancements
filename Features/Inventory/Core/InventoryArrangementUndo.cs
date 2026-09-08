#nullable enable
using System.Linq;
using SephiriaEnhancements.Runtime;
using SephiriaEnhancements.Runtime.Inventory;

namespace SephiriaEnhancements.Inventory
{
    // One successfully confirmed arrangement, owned by the local inventory context.
    internal sealed class InventoryArrangementUndo
    {
        private readonly InventorySnapshot before;
        private readonly RuntimeStateSnapshot afterRuntime;

        internal InventoryArrangementUndo(InventorySnapshot before, RuntimeStateSnapshot afterRuntime)
        {
            this.before = before;
            this.afterRuntime = afterRuntime;
        }

        internal bool Matches(RuntimeStateSnapshot? current) => current != null &&
            current.GameplayContextEpoch == afterRuntime.GameplayContextEpoch &&
            current.PlayerNetId == afterRuntime.PlayerNetId &&
            current.InventoryRevision == afterRuntime.InventoryRevision;

        internal InventoryLayoutProjection? RestoreLayout(InventorySnapshot? current)
        {
            if (current == null || current.Width != before.Width || current.Storage != before.Storage ||
                current.Items.Count != before.Items.Count) return null;
            var original = before.Items.ToDictionary(item => item.ItemKey);
            var cells = new int[current.Items.Count];
            var rotations = new int[current.Items.Count];
            for (int index = 0; index < current.Items.Count; index++)
            {
                var item = current.Items[index];
                if (!original.TryGetValue(item.ItemKey, out var target) ||
                    item.Quantity != target.Quantity || (item.StoneTablet == null) != (target.StoneTablet == null))
                    return null;
                cells[index] = target.CellIndex;
                rotations[index] = target.StoneTablet?.Rotation ?? 0;
            }
            return new InventoryLayoutProjection(cells, rotations);
        }
    }
}
