#nullable disable
using System;
using System.Diagnostics;
using SephiriaEnhancements.Diagnostics;
using SephiriaEnhancements.Integration;
using SephiriaEnhancements.Runtime.Inventory;
using UnityEngine;

namespace SephiriaEnhancements.Runtime.GameBridge.Inventory
{
    internal sealed class NativeInventoryObservation : IDisposable
    {
        private const int CaptureQuietFrames = 2;
        private const int MaximumCaptureCoalescingFrames = 4;
        private readonly InventoryStateStore inventoryStateStore =
            new InventoryStateStore();
        private TabletProjectionReader tabletProjectionReader;
        private PlayerAvatar attachedPlayer;
        private GridInventory attachedGridInventory;
        private InventoryCatalogSnapshot inventoryCatalog;
        private NativePresetSnapshot nativePreset;
        private long observedNativePresetRevision;
        private int lastCaptureFrame = -1;
        private bool inventoryCapturePending;
        private bool settledInventoryCapturePending;
        private int inventoryCaptureNotBeforeFrame;
        private int inventoryCaptureDeadlineFrame;
        private RuntimeStateHub stateHub;
        private RuntimeMetrics metrics;

        internal bool InventoryCapturePending => inventoryCapturePending;

        internal void RefreshForArrangement()
        {
            if (attachedGridInventory != null && attachedGridInventory.IsPickable && !inventoryCapturePending &&
                stateHub.Current.HasSettledInventoryObservation)
                CaptureInventory(settledObservation: true);
        }

        internal void Initialize(RuntimeStateHub stateHub, RuntimeMetrics metrics)
        {
            this.stateHub = stateHub;
            this.metrics = metrics;
            tabletProjectionReader = new TabletProjectionReader(metrics);
            observedNativePresetRevision = NativePresetChangeSignal.Revision;
        }

        internal void Subscribe()
        {
            HorayModAPI.GridInventoryStartPermission +=
                OnGridInventoryStartPermission;
            HorayModAPI.GridInventoryEndPermission +=
                OnGridInventoryEndPermission;
            HorayModAPI.OnAllDatabasesReady += OnAllDatabasesReady;
        }

        internal void ResetGameplayContext()
        {
            DetachGridInventory();
            InventoryEvaluationOrderTraceSignal.Clear();
            inventoryStateStore.Clear();
            nativePreset = null;
            tabletProjectionReader?.Clear();
        }

        internal void CancelPendingCapture()
        {
            inventoryCapturePending = false;
            settledInventoryCapturePending = false;
            inventoryCaptureNotBeforeFrame = 0;
            inventoryCaptureDeadlineFrame = 0;
        }

        public void Dispose()
        {
            SephiriaEnhancementsMod.CleanupFeature(FeatureId.Inventory, () => HorayModAPI.GridInventoryStartPermission -= OnGridInventoryStartPermission);
            SephiriaEnhancementsMod.CleanupFeature(FeatureId.Inventory, () => HorayModAPI.GridInventoryEndPermission -= OnGridInventoryEndPermission);
            SephiriaEnhancementsMod.CleanupFeature(FeatureId.Inventory, () => HorayModAPI.OnAllDatabasesReady -= OnAllDatabasesReady);
            SephiriaEnhancementsMod.CleanupFeature(FeatureId.Inventory, DetachGridInventory);
            inventoryStateStore.Clear();
            inventoryCatalog = null;
            nativePreset = null;
            SephiriaEnhancementsMod.CleanupFeature(FeatureId.Inventory, () => tabletProjectionReader?.Clear());
            tabletProjectionReader = null;
        }

        internal bool TryGetProjectableInventorySnapshot(
            out InventorySnapshot snapshot,
            out RuntimeStateSnapshot runtimeState)
        {
            FeatureFailure.Run(FeatureId.Inventory, RefreshNativePresetIfChanged);
            runtimeState = stateHub?.Current;
            return inventoryStateStore.TryGetProjectable(runtimeState,
                out snapshot);
        }

        internal bool TryGetSettledInventorySnapshot(
            out InventorySnapshot snapshot,
            out RuntimeStateSnapshot runtimeState)
        {
            runtimeState = stateHub?.Current;
            return inventoryStateStore.TryGetSettled(runtimeState,
                out snapshot);
        }

        internal bool TryGetLatestInventorySnapshot(out InventorySnapshot snapshot,
            out RuntimeStateSnapshot runtimeState)
        {
            runtimeState = stateHub?.Current;
            return inventoryStateStore.TryGetLatest(runtimeState, out snapshot);
        }

        internal void ReconcileLocalPlayer(PlayerAvatar player, bool isTraveling)
        {
            metrics.RecordEvent(RuntimeEventKind.Reconciliation);
            if (isTraveling) return;
            if (player == null)
            {
                if (attachedPlayer != null || attachedGridInventory != null)
                {
                    DetachGridInventory();
                    inventoryStateStore.Clear();
                    nativePreset = null;
                    stateHub.Detach(Time.realtimeSinceStartup);
                }
                return;
            }

            if (player != attachedPlayer || player.Inventory != attachedGridInventory)
            {
                AttachGridInventory(player);
            }
        }

        private void AttachGridInventory(PlayerAvatar player)
        {
            DetachGridInventory();
            attachedPlayer = player;
            attachedGridInventory = player?.Inventory;
            if (attachedGridInventory == null)
            {
                stateHub.AttachPlayer(player?.netId ?? 0,
                    RuntimeCapabilities.LocalPlayer, Time.realtimeSinceStartup);
                return;
            }

            if (inventoryCatalog == null &&
                !RefreshInventoryCatalog(player, invalidateInventory: false))
            {
                DetachGridInventory();
                stateHub.PublishIssue("Inventory catalog capture failed.",
                    invalid: false, Time.realtimeSinceStartup);
                return;
            }

            nativePreset = CaptureNativePreset();

            attachedGridInventory.OnItemUpdatedForClient += OnItemUpdated;
            attachedGridInventory.OnItemAddedForClient += OnItemAdded;
            attachedGridInventory.OnItemRemovedForClient += OnItemRemoved;
            attachedGridInventory.OnInventoryStorageChangedClientside +=
                OnInventoryStorageChanged;
            attachedGridInventory.OnInventoryHeightChangedClientside +=
                OnInventoryHeightChanged;
            attachedGridInventory.OnUniquePairEnchantedClientside +=
                OnUniquePairEnchanted;
            attachedGridInventory.OnTabletRotatedClientside += OnTabletRotated;
            attachedGridInventory.OnItemIdentified += OnItemIdentified;
            attachedGridInventory.OnCharmEffectRefreshedForClient +=
                OnCharmEffectRefreshed;
            attachedGridInventory.OnClear += OnInventoryCleared;

            RuntimeCapabilities capabilities = RuntimeCapabilities.LocalPlayer |
                RuntimeCapabilities.GridInventory |
                RuntimeCapabilities.GridInventoryEvents |
                RuntimeCapabilities.InventoryCatalog;
            stateHub.AttachPlayer(player.netId, capabilities,
                Time.realtimeSinceStartup);
            ScheduleInventoryCapture(settledObservation: true);
        }

        private void DetachGridInventory()
        {
            GridInventory detachingInventory = attachedGridInventory;
            if (attachedGridInventory != null)
            {
                attachedGridInventory.OnItemUpdatedForClient -= OnItemUpdated;
                attachedGridInventory.OnItemAddedForClient -= OnItemAdded;
                attachedGridInventory.OnItemRemovedForClient -= OnItemRemoved;
                attachedGridInventory.OnInventoryStorageChangedClientside -=
                    OnInventoryStorageChanged;
                attachedGridInventory.OnInventoryHeightChangedClientside -=
                    OnInventoryHeightChanged;
                attachedGridInventory.OnUniquePairEnchantedClientside -=
                    OnUniquePairEnchanted;
                attachedGridInventory.OnTabletRotatedClientside -= OnTabletRotated;
                attachedGridInventory.OnItemIdentified -= OnItemIdentified;
                attachedGridInventory.OnCharmEffectRefreshedForClient -=
                    OnCharmEffectRefreshed;
                attachedGridInventory.OnClear -= OnInventoryCleared;
            }

            attachedGridInventory = null;
            attachedPlayer = null;
            InventoryEvaluationOrderTraceSignal.Clear(detachingInventory);
        }

        private void OnGridInventoryStartPermission(GridInventory gridInventory, PlayerAvatar player)
        {
            if (!FeatureFailure.IsAvailable(FeatureId.Inventory))
            {
                return;
            }

            try
            {
                OnGridInventoryStartPermissionCore(gridInventory, player);
            }
            catch (System.Exception exception)
            {
                FeatureFailure.Disable(FeatureId.Inventory, exception);
                return;
            }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private void OnGridInventoryStartPermissionCore(GridInventory gridInventory, PlayerAvatar player)
        {
            if (!IsAttachedLocalInventory(gridInventory, player))
            {
                return;
            }

            metrics.RecordEvent(RuntimeEventKind.GridInventoryStartPermission);
            MarkInventoryPending();
        }

        private void OnGridInventoryEndPermission(GridInventory gridInventory, PlayerAvatar player)
        {
            if (!FeatureFailure.IsAvailable(FeatureId.Inventory))
            {
                return;
            }

            try
            {
                OnGridInventoryEndPermissionCore(gridInventory, player);
            }
            catch (System.Exception exception)
            {
                FeatureFailure.Disable(FeatureId.Inventory, exception);
                return;
            }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private void OnGridInventoryEndPermissionCore(GridInventory gridInventory, PlayerAvatar player)
        {
            if (!IsAttachedLocalInventory(gridInventory, player))
            {
                return;
            }

            metrics.RecordEvent(RuntimeEventKind.GridInventoryEndPermission);
            ScheduleInventoryCapture(settledObservation: true);
        }

        private bool IsAttachedLocalInventory(GridInventory gridInventory,
            PlayerAvatar player)
        {
            if (gridInventory == null || player == null ||
                !LocalPlayerResolver.IsLocal(player))
            {
                return false;
            }

            if (attachedGridInventory != gridInventory || attachedPlayer != player)
            {
                AttachGridInventory(player);
            }
            return attachedGridInventory == gridInventory;
        }

        private void OnItemUpdated(NewItemOwnInstance item, ItemPosition position)
        {
            if (!FeatureFailure.IsAvailable(FeatureId.Inventory))
            {
                return;
            }

            try
            {
                OnItemUpdatedCore(item, position);
            }
            catch (System.Exception exception)
            {
                FeatureFailure.Disable(FeatureId.Inventory, exception);
                return;
            }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private void OnItemUpdatedCore(NewItemOwnInstance item, ItemPosition position)
        {
            metrics.RecordEvent(RuntimeEventKind.ItemUpdated);
            MarkInventoryPending();
        }

        private void OnItemAdded(NewItemOwnInstance item)
        {
            if (!FeatureFailure.IsAvailable(FeatureId.Inventory))
            {
                return;
            }

            try
            {
                OnItemAddedCore(item);
            }
            catch (System.Exception exception)
            {
                FeatureFailure.Disable(FeatureId.Inventory, exception);
                return;
            }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private void OnItemAddedCore(NewItemOwnInstance item)
        {
            metrics.RecordEvent(RuntimeEventKind.ItemAdded);
            MarkInventoryPending();
        }

        private void OnItemRemoved(ItemPosition position)
        {
            if (!FeatureFailure.IsAvailable(FeatureId.Inventory))
            {
                return;
            }

            try
            {
                OnItemRemovedCore(position);
            }
            catch (System.Exception exception)
            {
                FeatureFailure.Disable(FeatureId.Inventory, exception);
                return;
            }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private void OnItemRemovedCore(ItemPosition position)
        {
            metrics.RecordEvent(RuntimeEventKind.ItemRemoved);
            MarkInventoryPending();
        }

        private void OnInventoryStorageChanged(int oldStorage, int newStorage)
        {
            if (!FeatureFailure.IsAvailable(FeatureId.Inventory))
            {
                return;
            }

            try
            {
                OnInventoryStorageChangedCore(oldStorage, newStorage);
            }
            catch (System.Exception exception)
            {
                FeatureFailure.Disable(FeatureId.Inventory, exception);
                return;
            }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private void OnInventoryStorageChangedCore(int oldStorage, int newStorage)
        {
            metrics.RecordEvent(RuntimeEventKind.InventoryStorageChanged);
            DeveloperLogger.RecordInventoryStorageChanged(attachedGridInventory?.Width ?? 0, oldStorage, newStorage);
            tabletProjectionReader?.Clear();
            MarkInventoryPending();
        }

        private void OnInventoryHeightChanged(int oldHeight, int newHeight)
        {
            if (!FeatureFailure.IsAvailable(FeatureId.Inventory))
            {
                return;
            }

            try
            {
                OnInventoryHeightChangedCore(oldHeight, newHeight);
            }
            catch (System.Exception exception)
            {
                FeatureFailure.Disable(FeatureId.Inventory, exception);
                return;
            }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private void OnInventoryHeightChangedCore(int oldHeight, int newHeight)
        {
            metrics.RecordEvent(RuntimeEventKind.InventoryHeightChanged);
            DeveloperLogger.RecordInventoryHeightChanged(oldHeight, newHeight);
            tabletProjectionReader?.Clear();
            MarkInventoryPending();
        }

        private void OnUniquePairEnchanted(ItemPosition position)
        {
            if (!FeatureFailure.IsAvailable(FeatureId.Inventory))
            {
                return;
            }

            try
            {
                OnUniquePairEnchantedCore(position);
            }
            catch (System.Exception exception)
            {
                FeatureFailure.Disable(FeatureId.Inventory, exception);
                return;
            }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private void OnUniquePairEnchantedCore(ItemPosition position)
        {
            metrics.RecordEvent(RuntimeEventKind.UniquePairEnchanted);
            MarkInventoryPending();
        }

        private void OnTabletRotated(StoneTablet tablet, int rotation)
        {
            if (!FeatureFailure.IsAvailable(FeatureId.Inventory))
            {
                return;
            }

            try
            {
                OnTabletRotatedCore(tablet, rotation);
            }
            catch (System.Exception exception)
            {
                FeatureFailure.Disable(FeatureId.Inventory, exception);
                return;
            }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private void OnTabletRotatedCore(StoneTablet tablet, int rotation)
        {
            metrics.RecordEvent(RuntimeEventKind.TabletRotated);
            MarkInventoryPending();
        }

        private void OnItemIdentified(EItemIdentificationResult result, Vector2Int position, NewItemOwnInstance item)
        {
            if (!FeatureFailure.IsAvailable(FeatureId.Inventory))
            {
                return;
            }

            try
            {
                OnItemIdentifiedCore(result, position, item);
            }
            catch (System.Exception exception)
            {
                FeatureFailure.Disable(FeatureId.Inventory, exception);
                return;
            }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private void OnItemIdentifiedCore(EItemIdentificationResult result, Vector2Int position, NewItemOwnInstance item)
        {
            metrics.RecordEvent(RuntimeEventKind.ItemIdentified);
            MarkInventoryPending();
        }

        private void OnCharmEffectRefreshed()
        {
            if (!FeatureFailure.IsAvailable(FeatureId.Inventory))
            {
                return;
            }

            try
            {
                OnCharmEffectRefreshedCore();
            }
            catch (System.Exception exception)
            {
                FeatureFailure.Disable(FeatureId.Inventory, exception);
                return;
            }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private void OnCharmEffectRefreshedCore()
        {
            metrics.RecordEvent(RuntimeEventKind.CharmEffectRefreshed);
            ScheduleInventoryCapture(settledObservation: true);
        }

        private void ScheduleInventoryCapture(bool settledObservation)
        {
            int frame = Time.frameCount;
            if (!inventoryCapturePending)
            {
                inventoryCaptureDeadlineFrame = frame +
                    MaximumCaptureCoalescingFrames;
            }
            inventoryCapturePending = true;
            settledInventoryCapturePending |= settledObservation;
            inventoryCaptureNotBeforeFrame = frame + CaptureQuietFrames;
        }

        internal void CapturePendingInventory()
        {
            if (!inventoryCapturePending ||
                (Time.frameCount < inventoryCaptureNotBeforeFrame &&
                 Time.frameCount < inventoryCaptureDeadlineFrame))
            {
                return;
            }

            bool settledObservation = settledInventoryCapturePending;
            inventoryCapturePending = false;
            settledInventoryCapturePending = false;
            CaptureInventory(settledObservation);
        }

        private void OnInventoryCleared()
        {
            if (!FeatureFailure.IsAvailable(FeatureId.Inventory))
            {
                return;
            }

            try
            {
                OnInventoryClearedCore();
            }
            catch (System.Exception exception)
            {
                FeatureFailure.Disable(FeatureId.Inventory, exception);
                return;
            }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private void OnInventoryClearedCore()
        {
            metrics.RecordEvent(RuntimeEventKind.InventoryCleared);
            MarkInventoryPending();
        }

        private void MarkInventoryPending()
        {
            inventoryStateStore.Clear();
            stateHub.MarkInventoryPending(Time.realtimeSinceStartup);
        }

        private void CaptureInventory(bool settledObservation)
        {
            if (attachedGridInventory == null)
            {
                return;
            }

            if (inventoryCatalog == null)
            {
                stateHub.PublishIssue("Inventory catalog is unavailable.",
                    invalid: false, Time.realtimeSinceStartup);
                return;
            }

            int frame = Time.frameCount;
            if (lastCaptureFrame == frame &&
                stateHub.Current.Consistency == RuntimeConsistencyState.Consistent &&
                (!settledObservation ||
                    (stateHub.Current.Capabilities & RuntimeCapabilities.
                        SettledInventoryObservation) != 0))
            {
                return;
            }

            long started = Stopwatch.GetTimestamp();
            bool captured;
            InventorySnapshot snapshot;
            try
            {
                captured = InventorySnapshotReader.TryCapture(
                    attachedGridInventory, out snapshot, nativePreset,
                    inventoryCatalog, tabletProjectionReader);
            }
            catch (Exception exception)
            {
                float failedElapsedMilliseconds = (float)(
                    (Stopwatch.GetTimestamp() - started) * 1000d /
                    Stopwatch.Frequency);
                metrics.RecordCapture(failedElapsedMilliseconds, false);
                lastCaptureFrame = frame;
                inventoryStateStore.Clear();
                string failureDetails = NativeInventoryRead.FailureDetails(exception);
                FeatureFailure.Disable(FeatureId.Inventory, exception);
                stateHub.PublishIssue(
                    "Inventory snapshot capture failed: " +
                    failureDetails, invalid: false,
                    Time.realtimeSinceStartup);
                SupportLogger.Record("inventory_capture_failed",
                    failureDetails, "ERROR");
                return;
            }
            float elapsedMilliseconds = (float)((Stopwatch.GetTimestamp() - started) *
                1000d / Stopwatch.Frequency);
            metrics.RecordCapture(elapsedMilliseconds, captured);
            lastCaptureFrame = frame;

            if (!captured)
            {
                inventoryStateStore.Clear();
                stateHub.PublishIssue("GridInventory snapshot capture failed.",
                    invalid: false, Time.realtimeSinceStartup);
                return;
            }

            long publishedInventoryRevision =
                stateHub.Current.InventoryRevision + 1;
            inventoryStateStore.Publish(snapshot,
                stateHub.Current.GameplayContextEpoch,
                publishedInventoryRevision);
            stateHub.PublishInventory(settledObservation,
                Time.realtimeSinceStartup,
                snapshot.SettlementValidation.CurrentLayoutVerified,
                snapshot.SettlementValidation.LayoutProjectionReady);
            DeveloperLogger.RecordInventorySettlementValidation(
                snapshot.SettlementValidation, stateHub.Current);
            DeveloperLogger.RecordInventoryEvaluationOrder(
                snapshot.EvaluationOrder, stateHub.Current);
            DeveloperLogger.RecordInventoryPositionEffects(
                snapshot.PositionEffects, stateHub.Current);
        }

        private void OnAllDatabasesReady()
        {
            if (!FeatureFailure.IsAvailable(FeatureId.Inventory))
            {
                return;
            }

            try
            {
                OnAllDatabasesReadyCore();
            }
            catch (System.Exception exception)
            {
                FeatureFailure.Disable(FeatureId.Inventory, exception);
                return;
            }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private void OnAllDatabasesReadyCore()
        {
            StartupProfiler.RecordMilestone("all_game_databases_ready");
            tabletProjectionReader?.Clear();
            inventoryCatalog = null;
            nativePreset = null;
            if (attachedPlayer != null)
            {
                if (RefreshInventoryCatalog(attachedPlayer, invalidateInventory: true))
                {
                    nativePreset = CaptureNativePreset();
                }
            }
        }

        internal void RefreshNativePresetIfChanged()
        {
            long revision = NativePresetChangeSignal.Revision;
            if (revision == observedNativePresetRevision)
            {
                return;
            }

            observedNativePresetRevision = revision;
            if (inventoryCatalog == null)
            {
                return;
            }

            NativePresetSnapshot refreshed = CaptureNativePreset();
            if ((nativePreset == null && refreshed == null) ||
                nativePreset?.ContentEquals(refreshed) == true)
            {
                return;
            }

            nativePreset = refreshed;
            metrics.RecordEvent(RuntimeEventKind.NativePresetRefreshed);
            if (attachedGridInventory != null)
            {
                bool settled = stateHub.Current.Consistency ==
                    RuntimeConsistencyState.Consistent;
                stateHub.MarkInventoryPending(Time.realtimeSinceStartup);
                ScheduleInventoryCapture(settled);
            }
        }

        internal bool MatchesNativePreset(NativePresetSnapshot source)
        {
            FeatureFailure.Run(FeatureId.Inventory, RefreshNativePresetIfChanged);
            return source == null ? nativePreset == null : source.ContentEquals(nativePreset);
        }

        private bool RefreshInventoryCatalog(UnitAvatar avatar,
            bool invalidateInventory)
        {
            long started = Stopwatch.GetTimestamp();
            bool captured = InventoryCatalogReader.TryCapture(avatar,
                out InventoryCatalogSnapshot catalog);
            float elapsedMilliseconds = (float)((Stopwatch.GetTimestamp() - started) *
                1000d / Stopwatch.Frequency);
            metrics.RecordCatalogCapture(elapsedMilliseconds, captured);
            if (!captured)
            {
                metrics.RecordEvent(RuntimeEventKind.InventoryCatalogRefreshFailed);
                return false;
            }

            if (invalidateInventory)
            {
                inventoryStateStore.Clear();
                stateHub.MarkInventoryPending(Time.realtimeSinceStartup);
            }
            inventoryCatalog = catalog;
            metrics.RecordEvent(RuntimeEventKind.InventoryCatalogRefreshed);
            stateHub.PublishInventoryCatalog(Time.realtimeSinceStartup);
            return true;
        }

        private NativePresetSnapshot CaptureNativePreset()
        {
            long started = Stopwatch.GetTimestamp();
            NativePresetSnapshot snapshot =
                InventorySnapshotReader.CaptureNativePreset(inventoryCatalog);
            float elapsedMilliseconds = (float)((Stopwatch.GetTimestamp() - started) *
                1000d / Stopwatch.Frequency);
            metrics.RecordPresetCapture(elapsedMilliseconds, snapshot != null);
            return snapshot;
        }

    }
}
