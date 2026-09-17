#nullable disable
using SephiriaEnhancements.Runtime.GameBridge.Inventory;
using SephiriaEnhancements.Runtime.GameBridge;
using SephiriaEnhancements.Runtime.Inventory;

using System;
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
        private readonly RuntimeMetrics metrics = new RuntimeMetrics();
        private readonly EncounterLifecycleHub encounterLifecycleHub =
            new EncounterLifecycleHub();
        private readonly NativeLocalGameplayContext localGameplayContext =
            new NativeLocalGameplayContext();
        private RuntimeStateHub stateHub;
        private readonly NativeInventoryObservation inventoryObservation = new NativeInventoryObservation();
        private float nextReconciliationAt;
        private float nextMetricsAt;
        private bool initialized;

        internal RuntimeStateSnapshot State => stateHub?.Current;
        internal bool InventoryCapturePending => inventoryObservation.InventoryCapturePending;
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
            inventoryObservation.Initialize(stateHub, metrics);
            stateHub.Changed += ForwardStateChanged;
            encounterLifecycleHub.Changed += ForwardEncounterLifecycleChanged;
            NativeEncounterLifecycleCapture.SetObserver(
                ObserveEncounterLifecycle);
            inventoryObservation.Subscribe();
            nextReconciliationAt = Time.unscaledTime;
            nextMetricsAt = Time.unscaledTime + InitialMetricsInterval;
        }

        internal void BeginWorldSession() =>
            BeginGameplayContext(LocalGameplayContextChange.WorldSessionLoaded);

        private void BeginGameplayContext(LocalGameplayContextChange change)
        {
            if (!FeatureFailure.IsAvailable(FeatureId.Gameplay))
            {
                return;
            }

            try
            {
                BeginGameplayContextCore(change);
            }
            catch (System.Exception exception)
            {
                FeatureFailure.Disable(FeatureId.Gameplay, exception);
                return;
            }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private void BeginGameplayContextCore(LocalGameplayContextChange change)
        {
            FeatureFailure.Run(FeatureId.Inventory, inventoryObservation.ResetGameplayContext);
            RuntimeStateSnapshot runtimeState = stateHub?.BeginGameplayContext(Time.realtimeSinceStartup);
            FeatureFailure.Run(FeatureId.CombatInsights, () =>
            {
                NativeEncounterLifecycleCapture.ResetGameplayContext();
                encounterLifecycleHub.BeginGameplayContext(runtimeState?.GameplayContextEpoch ?? 0, Time.unscaledTime);
            });
            nextReconciliationAt = Time.unscaledTime;
            nextMetricsAt = Math.Min(nextMetricsAt, Time.unscaledTime + InitialMetricsInterval);
            // Cancel queued work even when inventory observation has already failed.
            inventoryObservation.CancelPendingCapture();
            FeatureFailure.Run(FeatureId.DeveloperTools, () =>
            {
                PlayerAvatar player = localGameplayContext.Player;
                DeveloperLogger.RecordLocalGameplayContext(change, runtimeState?.GameplayContextEpoch ?? 0, player != null ? player.netId : 0, localGameplayContext.FloorGuid, localGameplayContext.IsTraveling);
            });
            GameplayContextChanged?.Invoke(change);
        }

        internal bool TryGetProjectableInventorySnapshot(out InventorySnapshot snapshot,
            out RuntimeStateSnapshot runtimeState) =>
            inventoryObservation.TryGetProjectableInventorySnapshot(out snapshot, out runtimeState);

        internal void RefreshInventoryForArrangement() => inventoryObservation.RefreshForArrangement();

        internal bool TryGetSettledInventorySnapshot(out InventorySnapshot snapshot,
            out RuntimeStateSnapshot runtimeState) =>
            inventoryObservation.TryGetSettledInventorySnapshot(out snapshot, out runtimeState);

        internal bool TryGetLatestInventorySnapshot(out InventorySnapshot snapshot,
            out RuntimeStateSnapshot runtimeState) =>
            inventoryObservation.TryGetLatestInventorySnapshot(out snapshot, out runtimeState);

        internal bool MatchesNativePreset(NativePresetSnapshot source) =>
            inventoryObservation.MatchesNativePreset(source);

        public void Dispose()
        {
            if (!initialized)
            {
                return;
            }

            initialized = false;
            SephiriaEnhancementsMod.CleanupFeature(FeatureId.Gameplay, localGameplayContext.Dispose);
            SephiriaEnhancementsMod.CleanupFeature(FeatureId.CombatInsights, () => NativeEncounterLifecycleCapture.SetObserver(null));
            inventoryObservation.Dispose();
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

        private void ForwardEncounterLifecycleChanged(EncounterLifecycleEvent lifecycleEvent)
        {
            if (!FeatureFailure.IsAvailable(FeatureId.CombatInsights))
            {
                return;
            }

            try
            {
                ForwardEncounterLifecycleChangedCore(lifecycleEvent);
            }
            catch (System.Exception exception)
            {
                FeatureFailure.Disable(FeatureId.CombatInsights, exception);
                return;
            }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private void ForwardEncounterLifecycleChangedCore(EncounterLifecycleEvent lifecycleEvent)
        {
            DeveloperLogger.RecordEncounterLifecycle(lifecycleEvent);
            EncounterLifecycleChanged?.Invoke(lifecycleEvent);
        }

        private void Update()
        {
            if (!FeatureFailure.IsAvailable(FeatureId.Gameplay))
            {
                return;
            }

            try
            {
                UpdateCore();
            }
            catch (System.Exception exception)
            {
                FeatureFailure.Disable(FeatureId.Gameplay, exception);
                return;
            }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private void UpdateCore()
        {
            if (!initialized)
            {
                return;
            }

            float now = Time.unscaledTime;
            FeatureFailure.Run(FeatureId.DeveloperTools, StartupProfiler.ObserveFirstFrame);
            localGameplayContext.Poll();
            FeatureFailure.Run(FeatureId.DeveloperTools, GameLoadProfiler.Poll);
            FeatureFailure.Run(FeatureId.Inventory, inventoryObservation.RefreshNativePresetIfChanged);
            FeatureFailure.Run(FeatureId.Inventory, inventoryObservation.CapturePendingInventory);
            if (now >= nextReconciliationAt)
            {
                nextReconciliationAt = now + ReconciliationInterval;
                FeatureFailure.Run(FeatureId.Inventory, () => inventoryObservation.ReconcileLocalPlayer(localGameplayContext.Player, localGameplayContext.IsTraveling));
            }

            if (now >= nextMetricsAt)
            {
                nextMetricsAt = now + MetricsInterval;
                if (DeveloperLogger.IsEnabled)
                {
                    DeveloperLogger.RecordRuntimeMetrics(metrics.TakeSnapshotAndReset(), stateHub.Current);
                }
                else
                {
                    metrics.Reset();
                }
            }
        }

        private void ForwardStateChanged(RuntimeStateSnapshot snapshot)
        {
            if (!FeatureFailure.IsAvailable(FeatureId.Inventory))
            {
                return;
            }

            try
            {
                ForwardStateChangedCore(snapshot);
            }
            catch (System.Exception exception)
            {
                FeatureFailure.Disable(FeatureId.Inventory, exception);
                return;
            }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private void ForwardStateChangedCore(RuntimeStateSnapshot snapshot)
        {
            StateChanged?.Invoke(snapshot);
        }

        private void OnDestroy()
        {
            Dispose();
        }
    }
}
