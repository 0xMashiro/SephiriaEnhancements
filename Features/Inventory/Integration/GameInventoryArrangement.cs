#nullable disable
using SephiriaEnhancements.Runtime.Inventory;

namespace SephiriaEnhancements.Inventory
{
    internal static class GameInventoryArrangement
    {
        private const int MaximumImprovementPasses = 2;

        internal static void Request(GridInventory inventory,
            bool allowStoneTabletRotation)
        {
            if (inventory == null)
            {
                return;
            }

            // Native integration boundary. This API dispatches a Mirror command
            // for clients and executes directly when the inventory owns the
            // server-side state. Keep the native method name confined here.
            inventory.RequestAutoArrangeInventoryForBestCharmLevels(
                MaximumImprovementPasses,
                allowStoneTabletRotation);
        }
    }
}
