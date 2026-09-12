using SephiriaEnhancements.Runtime;
using System;
using System.Reflection;
using HarmonyLib;
using Mirror;
using SephiriaEnhancements.Diagnostics;
using SephiriaEnhancements.Integration;
using UnityEngine;
using SephiriaEnhancements.AutoCasting.Integration;

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
            try
            {
                DefeatRetryClientRestore.Tick();
            }
            catch (Exception exception)
            {
                SupportLogger.Failure("retry_client_restore_failed", exception);
                DefeatRetryClientRestore.ReportFailure();
                DefeatRetryClientRestore.Clear();
                throw;
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
        private static double deadline;
        private static FloorGenerator requestedCameraFloor;
        private static GameCamera requestedCamera;
        internal static bool PreserveClientRun { get; private set; }

        internal static void Begin(string floorGuid, long id, Vector3 position)
        {
            Clear();
            NativeRetryFailure.Clear();
            destination = position;
            deadline = Time.realtimeSinceStartupAsDouble + DefeatRetryBridge.RecoveryTimeout;
            player = LocalPlayerResolver.Resolve();
            if (player == null) return;
            connection = NetworkClient.connection;
            floor = floorGuid;
            retryId = id;
            player.localDataStorage.NetworkreadyToLeave = false;
            player.localDataStorage.NetworkgoToEachOtherSessionOnGameOver_Local = 0;
            player.OnTravelPreparedClientside += OnTravelPrepared;
            FeatureFailure.Run(FeatureId.AutoCasting, () => NativeAutoCasting.Current?.BeginRetry());
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
        private static void OnTravelPreparedCore() => traveled = true;

        internal static void ObserveWorldSession(bool isSavedSession)
        {
            if (player == null) return;
            if (!isSavedSession || worldLoaded) Clear();
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
            // Defeat disables saving and deletes the file, but retains this object.
            if (!NetworkServer.active && SaveManager.CurrentRun != null)
            {
                SaveManager.CurrentRun.enableSave = true;
                SaveManager.Save(saveCurrent: false, saveCurrentRun: true);
            }
            notified = true;
        }

        internal static void Tick()
        {
            if (player == null) { Clear(); return; }
            if (!NetworkClient.active || connection != NetworkClient.connection || !LocalPlayerResolver.IsLocal(player))
            {
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
            if (!notified || !traveled || !NativeRetryArrival.IsAtDestination(player, floor, destination)) return;
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
            SupportLogger.Record("retry_client_arrived", "player=" + player.netId);
            FeatureFailure.Run(FeatureId.AutoCasting, () => NativeAutoCasting.Current?.CompleteRetry());
            DefeatRetryBridge.ReportArrival(retryId);
            Clear();
        }

        internal static void ReportFailure() { if (player != null) DefeatRetryBridge.ReportArrival(retryId, success: false); }

        internal static void RecordFailureContext()
        {
            if (player == null) return;
            SupportLogger.Record("retry_client_incomplete", "retry=" + retryId + " " +
                NativeRetryArrival.Describe(player, floor, destination) + " notified=" + notified +
                " traveled=" + traveled + " cameraMatches=" + (GameCamera.Instance != null && GameCamera.Instance.CurrentSeeingFloor != null && GameCamera.Instance.CurrentSeeingFloor == FloorGenerator.FindByGuid(floor)));
        }

        internal static void Clear()
        {
            FeatureFailure.Run(FeatureId.AutoCasting, () => NativeAutoCasting.Current?.CancelRetry());
            if (!ReferenceEquals(player, null)) player.OnTravelPreparedClientside -= OnTravelPrepared;
            player = null;
            connection = null;
            floor = null;
            runFile = null;
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
