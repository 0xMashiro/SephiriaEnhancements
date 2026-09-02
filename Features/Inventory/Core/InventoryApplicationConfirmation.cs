#nullable disable
using SephiriaEnhancements.Runtime.Inventory;

using System.Linq;

namespace SephiriaEnhancements.Inventory
{
    internal static class InventoryApplicationConfirmation
    {
        internal static bool IsSwapObserved(InventorySnapshot snapshot,
            InventorySwapOperation operation)
        {
            return GetInstanceId(snapshot, operation.FirstCell) ==
                    operation.ExpectedSecondInstanceId &&
                GetInstanceId(snapshot, operation.SecondCell) ==
                    operation.ExpectedFirstInstanceId;
        }

        internal static bool IsRotationStepObserved(InventorySnapshot snapshot,
            InventoryRotationOperation operation, int previousRotation)
        {
            InventoryItemSnapshot item = snapshot?.Items.FirstOrDefault(value =>
                value.InstanceId == operation.InstanceId);
            return item?.CellIndex == operation.Cell &&
                item.StoneTablet != null &&
                item.StoneTablet.Rotation != previousRotation;
        }

        internal static bool MatchesTarget(InventorySnapshot actual,
            InventorySnapshot source, InventoryLayoutProjection target)
        {
            if (actual == null || source == null || target == null ||
                actual.Width != source.Width ||
                actual.Storage != source.Storage ||
                target.ItemCount != source.Items.Count)
            {
                return false;
            }

            for (int index = 0; index < source.Items.Count; index++)
            {
                InventoryItemSnapshot expected = source.Items[index];
                InventoryItemSnapshot observed = actual.Items.FirstOrDefault(
                    item => item.InstanceId == expected.InstanceId);
                if (observed?.CellIndex != target.GetCell(index) ||
                    expected.StoneTablet != null &&
                    (observed.StoneTablet == null ||
                     observed.StoneTablet.Rotation != target.GetRotation(index)))
                {
                    return false;
                }
            }
            return true;
        }

        private static int GetInstanceId(InventorySnapshot snapshot, int cell)
        {
            return snapshot?.Items.FirstOrDefault(item =>
                item.CellIndex == cell)?.InstanceId ?? -1;
        }
    }
}
