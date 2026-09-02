#nullable disable
using SephiriaEnhancements.Runtime.Inventory;

namespace SephiriaEnhancements.Inventory
{
    internal enum InventoryArrangementOperationPhase
    {
        Idle,
        Searching
    }

    internal enum InventoryArrangementInvalidationReason
    {
        None,
        FeatureDisabled,
        StandardInventoryClosed,
        GameplayContextChanged,
        InventoryStateChanged,
        InventoryLayoutChanged
    }

    // Arrangement intentionally describes the concrete layout-application operation;
    // the player-facing feature and shortcut are inventory optimization.
    internal static class InventoryArrangementLifecyclePolicy
    {
        internal static bool HasSameCapacity(int sourceWidth,
            int sourceStorage, int currentWidth, int currentStorage)
        {
            return sourceWidth == currentWidth &&
                sourceStorage == currentStorage;
        }

        internal static InventoryArrangementInvalidationReason Evaluate(
            InventoryArrangementOperationPhase phase, bool featureEnabled,
            bool standardInventoryOpen, bool gameplayContextMatches,
            bool sourceInventoryRevisionMatches,
            bool sourceLayoutMatches)
        {
            if (phase == InventoryArrangementOperationPhase.Idle)
            {
                return InventoryArrangementInvalidationReason.None;
            }
            if (!featureEnabled)
            {
                return InventoryArrangementInvalidationReason.FeatureDisabled;
            }
            if (!standardInventoryOpen)
            {
                return InventoryArrangementInvalidationReason.
                    StandardInventoryClosed;
            }
            if (!gameplayContextMatches)
            {
                return InventoryArrangementInvalidationReason.
                    GameplayContextChanged;
            }
            if (phase == InventoryArrangementOperationPhase.Searching &&
                !sourceInventoryRevisionMatches)
            {
                return InventoryArrangementInvalidationReason.
                    InventoryStateChanged;
            }
            if (phase == InventoryArrangementOperationPhase.Searching &&
                !sourceLayoutMatches)
            {
                return InventoryArrangementInvalidationReason.
                    InventoryLayoutChanged;
            }
            return InventoryArrangementInvalidationReason.None;
        }
    }
}
