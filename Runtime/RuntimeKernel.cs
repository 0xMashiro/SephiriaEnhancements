#nullable disable
using SephiriaEnhancements.Runtime.GameBridge.Inventory;
using SephiriaEnhancements.Runtime.GameBridge;
using SephiriaEnhancements.Runtime.Inventory;

using System;
using System.Diagnostics;
using System.Reflection;
using SephiriaEnhancements.Diagnostics;
using SephiriaEnhancements.Integration;
using UnityEngine;

namespace SephiriaEnhancements.Runtime
{
    internal sealed class RuntimeKernel : MonoBehaviour, IDisposable
    {
        private const float ReconciliationInterval = 0.5f;
        private const float InitialMetricsInterval = 2f;
        private const float MetricsInterval = 30f;
        private const int CaptureQuietFrames = 2;
        private const int MaximumCaptureCoalescingFrames = 4;
        private readonly RuntimeMetrics metrics = new RuntimeMetrics();
        private readonly EncounterLifecycleHub encounterLifecycleHub =
            new EncounterLifecycleHub();
        private readonly InventoryStateStore inventoryStateStore =
            new InventoryStateStore();
        private readonly NativeLocalGameplayContext localGameplayContext =
            new NativeLocalGameplayContext();
        private TabletProjectionReader tabletProjectionReader;
        private RuntimeStateHub stateHub;
        private PlayerAvatar attachedPlayer;
        private GridInventory attachedGridInventory;
        private InventoryCatalogSnapshot inventoryCatalog;
        private NativePresetSnapshot nativePreset;
        private long observedNativePresetRevision;
        private float nextReconciliationAt;
        private float nextMetricsAt;
        private int lastCaptureFrame = -1;
        private bool inventoryCapturePending;
        private bool settledInventoryCapturePending;
        private int inventoryCaptureNotBeforeFrame;
        private int inventoryCaptureDeadlineFrame;
        private bool initialized;

        internal RuntimeStateSnapshot State => stateHub?.Current;
        internal EncounterLifecycleEvent LastEncounterLifecycleEvent =>
            encounterLifecycleHub.Current;

        internal bool IsOrdinaryEncounterCleared(int sourceInstanceId) =>
            encounterLifecycleHub.IsOrdinaryEncounterCleared(sourceInstanceId);

        internal event Action<RuntimeStateSnapshot> StateChanged;
        internal event Action<EncounterLifecycleEvent> EncounterLifecycleChanged;
        internal event Action<LocalGameplayContextChange> GameplayContextChanged;

        internal void Initialize()
        {
            if (initialized)
            {
                return;
            }

            initialized = true;
            localGameplayContext.Changed += BeginGameplayContext;
            Assembly gameAssembly = typeof(HorayModAPI).Assembly;
            string fingerprint = "game=" + Application.version +
                ";unity=" + Application.unityVersion +
                ";assembly=" + gameAssembly.GetName().Version;
            stateHub = new RuntimeStateHub(fingerprint);
            tabletProjectionReader = new TabletProjectionReader(metrics);
            observedNativePresetRevision = NativePresetChangeSignal.Revision;
            stateHub.Changed += ForwardStateChanged;
            encounterLifecycleHub.Changed += ForwardEncounterLifecycleChanged;
            NativeEncounterLifecycleCapture.SetObserver(
                ObserveEncounterLifecycle);
            HorayModAPI.GridInventoryStartPermission +=
                OnGridInventoryStartPermission;
            HorayModAPI.GridInventoryEndPermission +=
                OnGridInventoryEndPermission;
            HorayModAPI.OnAllDatabasesReady += OnAllDatabasesReady;
            nextReconciliationAt = Time.unscaledTime;
            nextMetricsAt = Time.unscaledTime + InitialMetricsInterval;
        }

        internal void BeginWorldSession() =>
            BeginGameplayContext(LocalGameplayContextChange.WorldSessionLoaded);

        private void BeginGameplayContext(LocalGameplayContextChange change)
        {
            DetachGridInventory();
            InventoryEvaluationOrderTraceSignal.Clear();
            inventoryStateStore.Clear();
            nativePreset = null;
            tabletProjectionReader?.Clear();
            NativeEncounterLifecycleCapture.ResetGameplayContext();
            RuntimeStateSnapshot runtimeState =
                stateHub?.BeginGameplayContext(Time.realtimeSinceStartup);
            encounterLifecycleHub.BeginGameplayContext(
                runtimeState?.GameplayContextEpoch ?? 0,
                Time.unscaledTime);
            nextReconciliationAt = Time.unscaledTime;
            nextMetricsAt = Math.Min(nextMetricsAt,
                Time.unscaledTime + InitialMetricsInterval);
            inventoryCapturePending = false;
            settledInventoryCapturePending = false;
            inventoryCaptureNotBeforeFrame = 0;
            inventoryCaptureDeadlineFrame = 0;
            PlayerAvatar player = localGameplayContext.Player;
            DeveloperLogger.RecordLocalGameplayContext(change,
                runtimeState?.GameplayContextEpoch ?? 0,
                player != null ? player.netId : 0,
                localGameplayContext.FloorGuid,
                localGameplayContext.IsTraveling);
            GameplayContextChanged?.Invoke(change);
        }

        internal bool TryGetProjectableInventorySnapshot(
            out InventorySnapshot snapshot,
            out RuntimeStateSnapshot runtimeState)
        {
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

        public void Dispose()
        {
            if (!initialized)
            {
                return;
            }

            initialized = false;
            localGameplayContext.Dispose();
            NativeEncounterLifecycleCapture.SetObserver(null);
            HorayModAPI.GridInventoryStartPermission -=
                OnGridInventoryStartPermission;
            HorayModAPI.GridInventoryEndPermission -=
                OnGridInventoryEndPermission;
            HorayModAPI.OnAllDatabasesReady -= OnAllDatabasesReady;
            DetachGridInventory();
            inventoryStateStore.Clear();
            inventoryCatalog = null;
            nativePreset = null;
            tabletProjectionReader?.Clear();
            tabletProjectionReader = null;
            if (stateHub != null)
            {
                stateHub.Changed -= ForwardStateChanged;
                stateHub.Detach(Time.realtimeSinceStartup);
            }
            encounterLifecycleHub.Changed -= ForwardEncounterLifecycleChanged;
            StateChanged = null;
            EncounterLifecycleChanged = null;
            GameplayContextChanged = null;
        }

        private void ObserveEncounterLifecycle(
            EncounterLifecycleObservation observation)
        {
            encounterLifecycleHub.Observe(observation);
        }

        private void ForwardEncounterLifecycleChanged(
            EncounterLifecycleEvent lifecycleEvent)
        {
            DeveloperLogger.RecordEncounterLifecycle(lifecycleEvent);
            EncounterLifecycleChanged?.Invoke(lifecycleEvent);
        }

        private void Update()
        {
            if (!initialized)
            {
                return;
            }

            float now = Time.unscaledTime;
            StartupProfiler.ObserveFirstFrame();
            localGameplayContext.Poll();
            GameLoadProfiler.Poll();
            RefreshNativePresetIfChanged();
            CapturePendingInventory();
            if (now >= nextReconciliationAt)
            {
                nextReconciliationAt = now + ReconciliationInterval;
                ReconcileLocalPlayer();
            }

            if (now >= nextMetricsAt)
            {
                nextMetricsAt = now + MetricsInterval;
                if (DeveloperLogger.IsEnabled)
                {
                    DeveloperLogger.RecordRuntimeMetrics(
                        metrics.TakeSnapshotAndReset(), stateHub.Current);
                }
                else
                {
                    metrics.Reset();
                }
            }
        }

        private void ReconcileLocalPlayer()
        {
            metrics.RecordEvent(RuntimeEventKind.Reconciliation);
            if (localGameplayContext.IsTraveling) return;
            PlayerAvatar player = localGameplayContext.Player;
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

        private void OnGridInventoryStartPermission(GridInventory gridInventory,
            PlayerAvatar player)
        {
            if (!IsAttachedLocalInventory(gridInventory, player))
            {
                return;
            }

            metrics.RecordEvent(RuntimeEventKind.GridInventoryStartPermission);
            MarkInventoryPending();
        }

        private void OnGridInventoryEndPermission(GridInventory gridInventory,
            PlayerAvatar player)
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
            metrics.RecordEvent(RuntimeEventKind.ItemUpdated);
            MarkInventoryPending();
        }

        private void OnItemAdded(NewItemOwnInstance item)
        {
            metrics.RecordEvent(RuntimeEventKind.ItemAdded);
            MarkInventoryPending();
        }

        private void OnItemRemoved(ItemPosition position)
        {
            metrics.RecordEvent(RuntimeEventKind.ItemRemoved);
            MarkInventoryPending();
        }

        private void OnInventoryStorageChanged(int oldStorage, int newStorage)
        {
            metrics.RecordEvent(RuntimeEventKind.InventoryStorageChanged);
            DeveloperLogger.RecordInventoryStorageChanged(
                attachedGridInventory?.Width ?? 0, oldStorage, newStorage);
            tabletProjectionReader?.Clear();
            MarkInventoryPending();
        }

        private void OnInventoryHeightChanged(int oldHeight, int newHeight)
        {
            metrics.RecordEvent(RuntimeEventKind.InventoryHeightChanged);
            DeveloperLogger.RecordInventoryHeightChanged(oldHeight, newHeight);
            tabletProjectionReader?.Clear();
            MarkInventoryPending();
        }

        private void OnUniquePairEnchanted(ItemPosition position)
        {
            metrics.RecordEvent(RuntimeEventKind.UniquePairEnchanted);
            MarkInventoryPending();
        }

        private void OnTabletRotated(StoneTablet tablet, int rotation)
        {
            metrics.RecordEvent(RuntimeEventKind.TabletRotated);
            MarkInventoryPending();
        }

        private void OnItemIdentified(EItemIdentificationResult result,
            Vector2Int position, NewItemOwnInstance item)
        {
            metrics.RecordEvent(RuntimeEventKind.ItemIdentified);
            MarkInventoryPending();
        }

        private void OnCharmEffectRefreshed()
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

        private void CapturePendingInventory()
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
                stateHub.PublishIssue(
                    "GridInventory snapshot capture failed: " +
                    exception.GetType().Name + ".", invalid: false,
                    Time.realtimeSinceStartup);
                SupportLogger.Warning("inventory_capture_failed",
                    "[SephiriaEnhancements] Inventory snapshot capture " +
                    "failed safely: " + exception.GetType().Name);
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
            StartupProfiler.RecordMilestone("all_game_databases_ready");
            tabletProjectionReader?.Clear();
            inventoryCatalog = null;
            nativePreset = null;
            if (attachedPlayer != null)
            {
                if (RefreshInventoryCatalog(attachedPlayer,
                    invalidateInventory: true))
                {
                    nativePreset = CaptureNativePreset();
                }
            }
        }

        private void RefreshNativePresetIfChanged()
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
                ScheduleInventoryCapture(settled);
            }
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

        private void ForwardStateChanged(RuntimeStateSnapshot snapshot)
        {
            StateChanged?.Invoke(snapshot);
        }

        private void OnDestroy()
        {
            Dispose();
        }
    }
}
