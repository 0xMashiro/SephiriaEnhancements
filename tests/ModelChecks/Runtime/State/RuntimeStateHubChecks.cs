using SephiriaEnhancements.ModelChecks.Runtime.Inventory;
using SephiriaEnhancements.Runtime;
using SephiriaEnhancements.Runtime.Inventory;

namespace SephiriaEnhancements.ModelChecks.Runtime.State;

internal static class RuntimeStateHubChecks
{
    internal static void Run()
    {
        InventorySnapshot inventorySnapshot = InventorySnapshotFixture.WithRestrictedArtifact(out _, out _);
        var runtimeHub = new RuntimeStateHub("game=test");
        RuntimeStateSnapshot initialRuntime = runtimeHub.Current;
        if (initialRuntime.Consistency != RuntimeConsistencyState.Unavailable ||
            initialRuntime.ContractVersion != RuntimeStateSnapshot.CurrentContractVersion)
            throw new InvalidOperationException("runtime state must begin unavailable");
        RuntimeStateSnapshot catalogRuntime = runtimeHub.PublishInventoryCatalog(0.5f);
        RuntimeStateSnapshot gameplayContextRuntime =
            runtimeHub.BeginGameplayContext(1f);
        RuntimeStateSnapshot attachedRuntime = runtimeHub.AttachPlayer(42,
            RuntimeCapabilities.LocalPlayer | RuntimeCapabilities.GridInventory |
            RuntimeCapabilities.GridInventoryEvents |
            RuntimeCapabilities.InventoryCatalog, 2f);
        RuntimeStateSnapshot provisionalRuntime = runtimeHub.PublishInventory(
            settledObservation: false, 2.5f);
        var inventoryStore = new InventoryStateStore();
        inventoryStore.Publish(inventorySnapshot,
            provisionalRuntime.GameplayContextEpoch,
            provisionalRuntime.InventoryRevision);
        if (inventoryStore.TryGetProjectable(provisionalRuntime, out _) ||
            inventoryStore.TryGetSettled(provisionalRuntime, out _))
            throw new InvalidOperationException("provisional inventory must not be consumable");
        if (!inventoryStore.TryGetLatest(provisionalRuntime,
                out InventorySnapshot diagnosticInventory) ||
            diagnosticInventory != inventorySnapshot)
            throw new InvalidOperationException(
                "same-revision diagnostic inventory must remain observable");
        RuntimeStateSnapshot publishedRuntime = runtimeHub.PublishInventory(
            settledObservation: true, 3f);
        inventoryStore.Publish(inventorySnapshot,
            publishedRuntime.GameplayContextEpoch,
            publishedRuntime.InventoryRevision);
        if (catalogRuntime.CatalogRevision != 1 ||
            gameplayContextRuntime.GameplayContextEpoch != 1 ||
            gameplayContextRuntime.CatalogRevision != 1 ||
            attachedRuntime.PlayerNetId != 42 ||
            attachedRuntime.Consistency != RuntimeConsistencyState.PendingSettlement ||
            provisionalRuntime.Consistency != RuntimeConsistencyState.PendingSettlement ||
            provisionalRuntime.CanProjectInventoryLayouts ||
            publishedRuntime.Consistency != RuntimeConsistencyState.Consistent ||
            !publishedRuntime.CanProjectInventoryLayouts ||
            publishedRuntime.InventoryRevision != 2 ||
            (publishedRuntime.Capabilities &
                RuntimeCapabilities.SettledInventoryObservation) == 0 ||
            !inventoryStore.TryGetProjectable(publishedRuntime,
                out InventorySnapshot storedInventory) ||
            !inventoryStore.TryGetSettled(publishedRuntime, out _) ||
            storedInventory != inventorySnapshot)
            throw new InvalidOperationException("runtime state attach or publication failed");
        RuntimeStateSnapshot invalidInputRuntime = runtimeHub.PublishInventory(
            settledObservation: true, 3.5f, layoutProjectionReady: false);
        inventoryStore.Publish(inventorySnapshot,
            invalidInputRuntime.GameplayContextEpoch,
            invalidInputRuntime.InventoryRevision);
        if (invalidInputRuntime.CanProjectInventoryLayouts ||
            (invalidInputRuntime.Capabilities &
                RuntimeCapabilities.InventoryLayoutProjection) != 0 ||
            !inventoryStore.TryGetSettled(invalidInputRuntime, out _) ||
            inventoryStore.TryGetProjectable(invalidInputRuntime, out _))
            throw new InvalidOperationException(
                "unprojectable inventory must block layout projection");
        long pendingRevision = runtimeHub.MarkInventoryPending(4f).RuntimeRevision;
        inventoryStore.Clear();
        if (runtimeHub.MarkInventoryPending(5f).RuntimeRevision != pendingRevision ||
            runtimeHub.Current.CanProjectInventoryLayouts ||
            inventoryStore.TryGetProjectable(publishedRuntime, out _))
            throw new InvalidOperationException("runtime dirty events must coalesce");
        RuntimeStateSnapshot nextFloorRuntime =
            runtimeHub.BeginGameplayContext(6f);
        if (nextFloorRuntime.GameplayContextEpoch != 2 ||
            runtimeHub.Current.InventoryRevision != 0 ||
            runtimeHub.Current.CatalogRevision != 1 ||
            runtimeHub.Current.PlayerNetId != 0 ||
            inventoryStore.TryGetLatest(nextFloorRuntime, out _))
            throw new InvalidOperationException(
                "new gameplay context must invalidate floor-bound runtime state");
        Console.WriteLine("RuntimeStateHub: gameplay-context epoch, revision, settlement " +
            "and coalescing checks passed");
    }
}
