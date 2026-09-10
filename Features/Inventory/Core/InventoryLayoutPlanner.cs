#nullable disable
using SephiriaEnhancements.Runtime.Inventory;

using System;
using System.Collections.Generic;
using System.Threading;

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
            out string issue, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
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

            bool preserveMysticCount = InventorySettlementValidator.HasDynamicMysticContribution(snapshot);
            var categoryOccupancy = preserveMysticCount ? new int[snapshot.Storage] : null;
            var swaps = new List<InventorySwapOperation>();
            var intermediate = InventoryLayoutProjection.Current(snapshot);
            if (!Safe(intermediate) || !Safe(layout))
            {
                issue = "LayoutIntermediateCategoriesUnavailable";
                return false;
            }
            while (Placed(intermediate) < snapshot.Items.Count)
            {
                cancellationToken.ThrowIfCancellationRequested();
                bool advanced = false;
                // Prefer putting a misplaced item directly into its final cell.
                for (int targetCell = 0; targetCell < snapshot.Storage; targetCell++)
                {
                    InventoryItemKey? targetItem = targetItemAtCell[targetCell];
                    if (!targetItem.HasValue || itemAtCell[targetCell] == targetItem) continue;
                    int sourceCell = cellByItem[targetItem.Value];
                    var next = intermediate.WithCellsSwapped(targetCell, sourceCell);
                    if (!Safe(next)) continue;
                    AppendSwap(targetCell, sourceCell);
                    advanced = true;
                    break;
                }
                if (advanced) continue;

                // One temporary swap can unblock a placement. Accept the pair only
                // when more items end in their final cells, bounding the whole plan.
                int placed = Placed(intermediate);
                for (int first = 0; first < snapshot.Storage && !advanced; first++)
                    for (int second = first + 1; second < snapshot.Storage && !advanced; second++)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        if (!itemAtCell[first].HasValue && !itemAtCell[second].HasValue) continue;
                        var temporary = intermediate.WithCellsSwapped(first, second);
                        if (!Safe(temporary)) continue;
                        for (int item = 0; item < snapshot.Items.Count; item++)
                        {
                            int from = temporary.GetCell(item), to = layout.GetCell(item);
                            if (from == to) continue;
                            var next = temporary.WithCellsSwapped(from, to);
                            if (!Safe(next) || Placed(next) <= placed) continue;
                            AppendSwap(first, second);
                            AppendSwap(from, to);
                            advanced = true;
                            break;
                        }
                    }
                if (!advanced)
                {
                    issue = "LayoutIntermediateCategoriesUnavailable";
                    return false;
                }
            }

            bool Safe(InventoryLayoutProjection candidate)
            {
                if (!InventorySettlementValidator.NeighborCategoryInputsIndependent(snapshot, candidate)) return false;
                if (!preserveMysticCount) return true;
                Array.Fill(categoryOccupancy, -1);
                for (int item = 0; item < snapshot.Items.Count; item++)
                    categoryOccupancy[candidate.GetCell(item)] = item;
                return InventorySettlementProjector.MysticCountPreserved(snapshot,
                    InventorySettlementProjector.CountCombos(snapshot, candidate, categoryOccupancy));
            }

            int Placed(InventoryLayoutProjection candidate)
            {
                int count = 0;
                for (int item = 0; item < snapshot.Items.Count; item++)
                    if (candidate.GetCell(item) == layout.GetCell(item)) count++;
                return count;
            }

            void AppendSwap(int first, int second)
            {
                InventoryItemKey? a = itemAtCell[first], b = itemAtCell[second];
                swaps.Add(new InventorySwapOperation(first, second, a, b));
                itemAtCell[first] = b;
                itemAtCell[second] = a;
                if (a.HasValue) cellByItem[a.Value] = second;
                if (b.HasValue) cellByItem[b.Value] = first;
                intermediate = intermediate.WithCellsSwapped(first, second);
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
