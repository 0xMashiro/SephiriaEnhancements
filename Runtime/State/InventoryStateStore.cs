#nullable disable
using SephiriaEnhancements.Runtime.Inventory;

namespace SephiriaEnhancements.Runtime
{
    internal sealed class InventoryStateStore
    {
        private InventorySnapshot current;
        private long gameplayContextEpoch;
        private long inventoryRevision;

        internal void Publish(InventorySnapshot snapshot,
            long publishedGameplayContextEpoch,
            long publishedInventoryRevision)
        {
            current = snapshot;
            gameplayContextEpoch = publishedGameplayContextEpoch;
            inventoryRevision = publishedInventoryRevision;
        }

        internal void Clear()
        {
            current = null;
            gameplayContextEpoch = 0;
            inventoryRevision = 0;
        }

        internal bool TryGetProjectable(RuntimeStateSnapshot runtimeState,
            out InventorySnapshot snapshot)
        {
            if (!TryGetLatest(runtimeState, out snapshot) ||
                !runtimeState.CanProjectInventoryLayouts)
            {
                snapshot = null;
                return false;
            }
            return true;
        }

        internal bool TryGetSettled(RuntimeStateSnapshot runtimeState,
            out InventorySnapshot snapshot)
        {
            if (!TryGetLatest(runtimeState, out snapshot) ||
                !runtimeState.HasSettledInventoryObservation)
            {
                snapshot = null;
                return false;
            }
            return true;
        }

        internal bool TryGetLatest(RuntimeStateSnapshot runtimeState,
            out InventorySnapshot snapshot)
        {
            if (current == null || runtimeState == null ||
                runtimeState.GameplayContextEpoch != gameplayContextEpoch ||
                runtimeState.InventoryRevision != inventoryRevision)
            {
                snapshot = null;
                return false;
            }

            snapshot = current;
            return true;
        }
    }
}
