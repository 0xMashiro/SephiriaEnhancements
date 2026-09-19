using SephiriaEnhancements.Runtime;
using SephiriaEnhancements.Runtime.GameBridge;
using System;
using System.Reflection;
using HarmonyLib;
using Mirror;
using SephiriaEnhancements.Diagnostics;
using SephiriaEnhancements.Integration;
using UnityEngine;

namespace SephiriaEnhancements.DefeatRetry
{
    internal sealed class DefeatRetryRuntime : MonoBehaviour
    {
        private void Update()
        {
            if (!FeatureFailure.IsAvailable(FeatureId.DefeatRetry))
            {
                return;
            }

            try
            {
                UpdateCore();
            }
            catch (System.Exception exception)
            {
                FeatureFailure.Disable(FeatureId.DefeatRetry, exception);
                return;
            }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private void UpdateCore()
        {
            DefeatRetryBridge.Tick();
            NativeRetryCapture.Tick();
            try
            {
                DefeatRetryClientRestore.Tick();
            }
            catch (Exception exception)
            {
                DefeatRetryClientRestore.Fail(exception);
            }
        }
    }

    internal static class DefeatRetryClientRestore
    {
        private static readonly MethodInfo BindFloor = AccessTools.Method(typeof(GameCamera), "UpdateFloor");
        internal static bool IsAvailable => BindFloor != null;
        private static NetworkConnectionToServer connection;
        private static PlayerAvatar player;
        private static string floor;
        private static string runFile;
        private static long retryId;
        private static bool notified, traveled, worldLoaded;
        private static Vector3 destination;
        private static NativeRetryArrival arrival;
        private static double deadline;
        private static FloorGenerator requestedCameraFloor;
        private static GameCamera requestedCamera;
        internal static bool PreserveClientRun { get; private set; }
        internal static bool IsRestoring => player != null;
        private static NativeRetryPlayerState playerState;
        private static NativeRetryControls controls;
        private static bool readyReported;

        internal static void Begin(string floorGuid, long id, Vector3 position, NativeRetryPlayerState account,
            long checkpointId = 0)
        {
            Clear();
            NativeRetryFailure.Clear();
            destination = position;
            deadline = Time.realtimeSinceStartupAsDouble + DefeatRetryBridge.RecoveryTimeout;
            player = LocalPlayerResolver.Resolve();
            if (player == null) return;
            connection = NetworkClient.connection;
            floor = floorGuid;
            arrival = new NativeRetryArrival(player, floorGuid, position);
            retryId = id;
            playerState = account ?? throw new InvalidOperationException("Retry account checkpoint is missing.");
            // Closing cancels the old settlement's delayed save callback before
            // the retained player and the owner's persistent account are restored.
            UI_GameOverLabel panel = UIManager.Instance?.GetElement<UI_GameOverLabel>();
            if (panel != null && panel.IsOpened) panel.Close();
            NativeRetryAccount.Restore(playerState.AccountCheckpointId);
            playerState.RestoreOwner(player.GetComponent<PlayerSpawner>());
            player.localDataStorage.NetworkreadyToLeave = false;
            player.localDataStorage.NetworkgoToEachOtherSessionOnGameOver_Local = 0;
            player.OnTravelPreparedClientside += OnTravelPrepared;
            NativeLocalPlayerData.BeginRestore(checkpointId, floorGuid);
            controls = new NativeRetryControls(player, playerState.SkillArtifacts, playerState.ArtifactKeys);
            SupportLogger.Record("retry_client_prepared", "player=" + player.netId);
        }

        private static void OnTravelPrepared()
        {
            if (!FeatureFailure.IsAvailable(FeatureId.DefeatRetry))
            {
                return;
            }

            try
            {
                OnTravelPreparedCore();
            }
            catch (System.Exception exception)
            {
                FeatureFailure.Disable(FeatureId.DefeatRetry, exception);
                return;
            }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static void OnTravelPreparedCore()
        {
            arrival?.Reset();
            traveled = true;
        }

        internal static void ObserveWorldSession(bool isSavedSession)
        {
            if (player == null) return;
            if (!isSavedSession || worldLoaded) { ReportFailure(); Clear(); }
            else worldLoaded = true;
        }

        internal static bool BeginNotification(PlayerSpawner spawner)
        {
            bool matches = player != null && spawner.PlayerAvatar == player &&
                connection == NetworkClient.connection && LocalPlayerResolver.IsLocal(player) && worldLoaded && !notified;
            runFile = matches ? SaveManager.CurrentRun?.BindedFileName : null;
            PreserveClientRun = matches;
            return matches;
        }

        internal static bool PreserveRunFile(string fileName, bool creation = false) =>
            PreserveClientRun && !string.IsNullOrEmpty(runFile) &&
            (fileName == runFile || (creation && fileName + "TMP" == runFile));

        internal static void EndNotification(bool matched, Exception exception)
        {
            PreserveClientRun = false;
            if (!matched) return;
            if (exception != null) { ReportFailure(); Clear(); return; }
            playerState.RestoreOwner(player.GetComponent<PlayerSpawner>());
            SaveManager.Save(saveCurrent: true, saveCurrentRun: true);
            notified = true;
        }

        // This is the owner's single readiness boundary. Required asynchronous state
        // joins these checks; optional presentation must not send a separate receipt.
        internal static void Tick()
        {
            if (player == null)
            {
                if (!ReferenceEquals(player, null)) { ReportFailure(); Clear(); }
                return;
            }
            if (!NetworkClient.active || connection != NetworkClient.connection || !LocalPlayerResolver.IsLocal(player))
            {
                ReportFailure();
                Clear();
                return;
            }
            if (Time.realtimeSinceStartupAsDouble >= deadline)
            {
                RecordFailureContext();
                ReportFailure();
                NativeRetryFailure.Show(RetryRecoveryFailure.TimedOut);
                Clear();
                return;
            }
            if (readyReported)
            {
                if (!NativeLocalPlayerData.IsRestoring || !arrival.IsCurrent ||
                    GameCamera.Instance == null || GameCamera.Instance.Observer != player ||
                    GameCamera.Instance.CurrentSeeingFloor != FloorGenerator.FindByGuid(floor) ||
                    !playerState.Matches(player.GetComponent<PlayerSpawner>()) || !controls.TryRestore(player))
                { ReportFailure(); Clear(); }
                return;
            }
            if (!notified || !traveled || !arrival.Observe()) return;
            if (!playerState.Matches(player.GetComponent<PlayerSpawner>())) return;
            FloorGenerator generator = FloorGenerator.FindByGuid(floor);
            GameCamera camera = GameCamera.Instance;
            if (generator == null || !generator.GenerateSuccess || camera == null) return;
            // Same GUID does not mean the same Unity object after world reconstruction.
            if (camera.Observer != player) camera.SetObserver(player, true);
            if (camera.CurrentSeeingFloor != generator)
            {
                if (requestedCameraFloor != generator || requestedCamera != camera)
                {
                    requestedCamera = camera;
                    requestedCameraFloor = generator;
                    BindFloor.Invoke(camera, new object[] { floor });
                }
                return;
            }
            if (camera.Observer != player) return;
            NativeLocalPlayerData.LoadProgress();
            if (!NativeLocalPlayerData.IsRestoring)
                throw new InvalidOperationException("Local recovery ownership was invalidated.");
            if (!controls.TryRestore(player)) return;
            CheckPersistence();
            readyReported = true;
            SupportLogger.Record("retry_client_ready", "player=" + player.netId);
            DefeatRetryBridge.ReportReady(retryId);
        }

        internal static void Fail(Exception exception)
        {
            SupportLogger.Failure("retry_client_restore_failed", exception);
            ReportFailure();
            Clear();
            NativeRetryFailure.Show(RetryRecoveryFailure.RestoreFailed);
        }

        internal static void Complete(long id)
        {
            if (id != retryId || !readyReported) return;
            NativeLocalPlayerData.CompleteRestore();
            Clear(completed: true);
        }

        internal static void ReportFailure()
        {
            if (!ReferenceEquals(player, null) && NetworkClient.active && connection == NetworkClient.connection)
                DefeatRetryBridge.ReportReady(retryId, success: false);
        }

        private static void CheckPersistence()
        {
            // Disk persistence is diagnostic, not a prerequisite for resuming play.
            if (SaveManager.IsSaving != SaveManager.ESaveState.None)
            {
                SupportLogger.Record("retry_persistence_pending", "retry=" + retryId);
                return;
            }
            try
            {
                bool accountSaved = NativeRetryPersistence.Matches(SaveManager.Current);
                bool runSaved = NativeRetryPersistence.Matches(SaveManager.CurrentRun);
                if (!accountSaved || !runSaved)
                    SupportLogger.Record("retry_persistence_mismatch",
                        "accountSaved=" + accountSaved + " runSaved=" + runSaved, "WARN");
            }
            catch (Exception exception)
            {
                SupportLogger.Failure("retry_persistence_check_failed", exception);
            }
        }

        internal static void RecordFailureContext()
        {
            if (player == null) return;
            SupportLogger.Record("retry_client_incomplete", "retry=" + retryId + " " +
                NativeRetryArrival.Describe(player, floor, destination) + " notified=" + notified +
                " traveled=" + traveled + " cameraMatches=" + (GameCamera.Instance != null && GameCamera.Instance.CurrentSeeingFloor != null && GameCamera.Instance.CurrentSeeingFloor == FloorGenerator.FindByGuid(floor)));
        }

        internal static void Clear(bool completed = false)
        {
            if (!completed) NativeLocalPlayerData.CancelRestore();
            if (!ReferenceEquals(player, null)) player.OnTravelPreparedClientside -= OnTravelPrepared;
            player = null;
            connection = null;
            floor = null;
            runFile = null;
            playerState = null;
            controls = null;
            readyReported = false;
            arrival = null;
            requestedCamera = null;
            requestedCameraFloor = null;
            notified = traveled = worldLoaded = PreserveClientRun = false;
        }
    }

    [HarmonyPatch]
    internal static class DefeatRetryClientNotificationPatch
    {
        private static MethodBase TargetMethod() => AccessTools.Method(typeof(PlayerSpawner),
            "UserCode_TargetRestartNewGame__NetworkConnectionToClient");

        private static void Prefix(PlayerSpawner __instance, out bool __state)
        {
            __state = default;
            if (!FeatureFailure.IsAvailable(FeatureId.DefeatRetry))
            {
                return;
            }

            try
            {
                PrefixCore(__instance, out __state);
            }
            catch (System.Exception exception)
            {
                FeatureFailure.Disable(FeatureId.DefeatRetry, exception);
                return;
            }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static void PrefixCore(PlayerSpawner __instance, out bool __state) => __state = DefeatRetryClientRestore.BeginNotification(__instance);

        private static void Finalizer(bool __state, Exception __exception)
        {
            try
            {
                FinalizerCore(__state, __exception);
            }
            catch (System.Exception exception)
            {
                FeatureFailure.Disable(FeatureId.DefeatRetry, exception);
                return;
            }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static void FinalizerCore(bool __state, Exception __exception) => DefeatRetryClientRestore.EndNotification(__state, __exception);
    }
}
