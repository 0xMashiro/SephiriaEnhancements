#nullable disable
using SephiriaEnhancements.Runtime.Inventory;

using System;
using System.Collections.Generic;

namespace SephiriaEnhancements.Inventory
{
    internal sealed class InventorySwapOperation
    {
        internal InventorySwapOperation(int firstCell, int secondCell,
            InventoryItemKey? expectedFirstItemKey, InventoryItemKey? expectedSecondItemKey)
        {
            FirstCell = firstCell;
            SecondCell = secondCell;
            ExpectedFirstItemKey = expectedFirstItemKey;
            ExpectedSecondItemKey = expectedSecondItemKey;
        }

        internal int FirstCell { get; }
        internal int SecondCell { get; }
        internal InventoryItemKey? ExpectedFirstItemKey { get; }
        internal InventoryItemKey? ExpectedSecondItemKey { get; }
    }

    internal sealed class InventoryRotationOperation
    {
        internal InventoryRotationOperation(InventoryItemKey itemKey, int cell,
            int targetRotation)
        {
            ItemKey = itemKey;
            Cell = cell;
            TargetRotation = targetRotation;
        }

        internal InventoryItemKey ItemKey { get; }
        internal int Cell { get; }
        internal int TargetRotation { get; }
    }

    internal sealed class InventoryApplicationPlan
    {
        internal InventoryApplicationPlan(InventorySwapOperation[] swaps,
            InventoryRotationOperation[] rotations)
        {
            Swaps = Array.AsReadOnly(swaps ?? Array.Empty<InventorySwapOperation>());
            Rotations = Array.AsReadOnly(rotations ??
                Array.Empty<InventoryRotationOperation>());
        }

        internal IReadOnlyList<InventorySwapOperation> Swaps { get; }
        internal IReadOnlyList<InventoryRotationOperation> Rotations { get; }
        internal int OperationCount => Swaps.Count + Rotations.Count;
    }

    internal static class InventoryLayoutPlanner
    {
        internal static bool TryCreate(InventorySnapshot snapshot,
            InventoryLayoutProjection layout, out InventoryApplicationPlan plan,
            out string issue)
        {
            plan = null;
            issue = string.Empty;
            if (snapshot == null || layout == null ||
                layout.ItemCount != snapshot.Items.Count)
            {
                issue = "LayoutInputUnavailable";
                return false;
            }

            var itemAtCell = new InventoryItemKey?[snapshot.Storage];
            var cellByItem = new Dictionary<InventoryItemKey, int>();
            var targetItemAtCell = new InventoryItemKey?[snapshot.Storage];
            for (int index = 0; index < snapshot.Items.Count; index++)
            {
                InventoryItemSnapshot item = snapshot.Items[index];
                int targetCell = layout.GetCell(index);
                if (item.CellIndex < 0 || item.CellIndex >= snapshot.Storage ||
                    targetCell < 0 || targetCell >= snapshot.Storage ||
                    itemAtCell[item.CellIndex].HasValue ||
                    targetItemAtCell[targetCell].HasValue ||
                    !cellByItem.TryAdd(item.ItemKey, item.CellIndex))
                {
                    issue = "LayoutIdentityMismatch";
                    return false;
                }
                itemAtCell[item.CellIndex] = item.ItemKey;
                targetItemAtCell[targetCell] = item.ItemKey;
            }

            var swaps = new List<InventorySwapOperation>();
            for (int targetCell = 0; targetCell < snapshot.Storage; targetCell++)
            {
                InventoryItemKey? targetItem = targetItemAtCell[targetCell];
                if (itemAtCell[targetCell] == targetItem)
                {
                    continue;
                }
                if (!targetItem.HasValue ||
                    !cellByItem.TryGetValue(targetItem.Value,
                        out int sourceCell))
                {
                    continue;
                }

                InventoryItemKey? displacedItem = itemAtCell[targetCell];
                swaps.Add(new InventorySwapOperation(targetCell, sourceCell,
                    displacedItem, targetItem));
                itemAtCell[targetCell] = targetItem;
                itemAtCell[sourceCell] = displacedItem;
                cellByItem[targetItem.Value] = targetCell;
                if (displacedItem.HasValue)
                {
                    cellByItem[displacedItem.Value] = sourceCell;
                }
            }

            var rotations = new List<InventoryRotationOperation>();
            for (int index = 0; index < snapshot.Items.Count; index++)
            {
                InventoryItemSnapshot item = snapshot.Items[index];
                if (item.StoneTablet == null ||
                    layout.GetRotation(index) == item.StoneTablet.Rotation)
                {
                    continue;
                }
                if (!item.StoneTablet.Rotatable)
                {
                    issue = "LayoutTabletRotationInvalid:" + item.ItemKey;
                    return false;
                }
                rotations.Add(new InventoryRotationOperation(item.ItemKey,
                    layout.GetCell(index), layout.GetRotation(index)));
            }

            plan = new InventoryApplicationPlan(swaps.ToArray(),
                rotations.ToArray());
            return true;
        }
    }
}
