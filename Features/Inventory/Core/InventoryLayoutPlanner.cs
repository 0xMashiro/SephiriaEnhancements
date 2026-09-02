#nullable disable
using SephiriaEnhancements.Runtime.Inventory;

using System;
using System.Collections.Generic;

namespace SephiriaEnhancements.Inventory
{
    internal sealed class InventorySwapOperation
    {
        internal InventorySwapOperation(int firstCell, int secondCell,
            int expectedFirstInstanceId, int expectedSecondInstanceId)
        {
            FirstCell = firstCell;
            SecondCell = secondCell;
            ExpectedFirstInstanceId = expectedFirstInstanceId;
            ExpectedSecondInstanceId = expectedSecondInstanceId;
        }

        internal int FirstCell { get; }
        internal int SecondCell { get; }
        internal int ExpectedFirstInstanceId { get; }
        internal int ExpectedSecondInstanceId { get; }
    }

    internal sealed class InventoryRotationOperation
    {
        internal InventoryRotationOperation(int instanceId, int cell,
            int targetRotation)
        {
            InstanceId = instanceId;
            Cell = cell;
            TargetRotation = targetRotation;
        }

        internal int InstanceId { get; }
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

            var itemAtCell = new int[snapshot.Storage];
            Array.Fill(itemAtCell, -1);
            var cellByInstance = new Dictionary<int, int>();
            var targetInstanceAtCell = new int[snapshot.Storage];
            Array.Fill(targetInstanceAtCell, -1);
            for (int index = 0; index < snapshot.Items.Count; index++)
            {
                InventoryItemSnapshot item = snapshot.Items[index];
                int targetCell = layout.GetCell(index);
                if (item.CellIndex < 0 || item.CellIndex >= snapshot.Storage ||
                    targetCell < 0 || targetCell >= snapshot.Storage ||
                    itemAtCell[item.CellIndex] >= 0 ||
                    targetInstanceAtCell[targetCell] >= 0 ||
                    !cellByInstance.TryAdd(item.InstanceId, item.CellIndex))
                {
                    issue = "LayoutIdentityMismatch";
                    return false;
                }
                itemAtCell[item.CellIndex] = item.InstanceId;
                targetInstanceAtCell[targetCell] = item.InstanceId;
            }

            var swaps = new List<InventorySwapOperation>();
            for (int targetCell = 0; targetCell < snapshot.Storage; targetCell++)
            {
                int targetInstance = targetInstanceAtCell[targetCell];
                if (itemAtCell[targetCell] == targetInstance)
                {
                    continue;
                }
                if (targetInstance < 0 ||
                    !cellByInstance.TryGetValue(targetInstance,
                        out int sourceCell))
                {
                    continue;
                }

                int displacedInstance = itemAtCell[targetCell];
                swaps.Add(new InventorySwapOperation(targetCell, sourceCell,
                    displacedInstance, targetInstance));
                itemAtCell[targetCell] = targetInstance;
                itemAtCell[sourceCell] = displacedInstance;
                cellByInstance[targetInstance] = targetCell;
                if (displacedInstance >= 0)
                {
                    cellByInstance[displacedInstance] = sourceCell;
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
                    issue = "LayoutTabletRotationInvalid:" + item.InstanceId;
                    return false;
                }
                rotations.Add(new InventoryRotationOperation(item.InstanceId,
                    layout.GetCell(index), layout.GetRotation(index)));
            }

            plan = new InventoryApplicationPlan(swaps.ToArray(),
                rotations.ToArray());
            return true;
        }
    }
}
