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
            return GetItemKey(snapshot, operation.FirstCell) ==
                    operation.ExpectedSecondItemKey &&
                GetItemKey(snapshot, operation.SecondCell) ==
                    operation.ExpectedFirstItemKey;
        }

        internal static bool IsRotationStepObserved(InventorySnapshot snapshot,
            InventoryRotationOperation operation, int previousRotation)
        {
            InventoryItemSnapshot item = snapshot?.Items.FirstOrDefault(value =>
                value.ItemKey == operation.ItemKey);
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
                target.ItemCount != source.Items.Count ||
                actual.Items.Count != source.Items.Count ||
                actual.Items.Select(item => item.ItemKey).Distinct().Count() !=
                    actual.Items.Count)
            {
                return false;
            }

            for (int index = 0; index < source.Items.Count; index++)
            {
                InventoryItemSnapshot expected = source.Items[index];
                InventoryItemSnapshot observed = actual.Items.FirstOrDefault(
                    item => item.ItemKey == expected.ItemKey);
                if (observed?.CellIndex != target.GetCell(index) ||
                    observed.Quantity != expected.Quantity ||
                    expected.StoneTablet != null &&
                    (observed.StoneTablet == null ||
                     observed.StoneTablet.Rotation != target.GetRotation(index)))
                {
                    return false;
                }
            }
            return true;
        }

        private static InventoryItemKey? GetItemKey(InventorySnapshot snapshot, int cell)
        {
            return snapshot?.Items.FirstOrDefault(item =>
                item.CellIndex == cell)?.ItemKey;
        }
    }
}
