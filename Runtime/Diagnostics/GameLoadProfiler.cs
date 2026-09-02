using SephiriaEnhancements.Runtime.Inventory;
using System.Diagnostics;
using SephiriaEnhancements.Integration;
using UnityEngine;

namespace SephiriaEnhancements.Diagnostics
{
    internal static class GameLoadProfiler
    {
#if SEPHIRIA_ENHANCEMENTS_DEVTOOLS
        private const string UnknownExplorationStartMode = "unknown";

        private static long loadAttemptStartedAt;
        private static int loadAttemptId;
        private static string trigger;
        private static string explorationStartMode;
        private static string floorGuid;
        private static string floorName;
        private static bool observingLoadAttempt;
        private static bool serverObserved;
        private static bool clientObserved;
        private static bool localPlayerObserved;
        private static bool nativeLoadingCompleted;
        private static bool clientReadyObserved;

        internal static void Reset()
        {
            loadAttemptStartedAt = 0;
            loadAttemptId = 0;
            trigger = null;
            explorationStartMode = UnknownExplorationStartMode;
            floorGuid = null;
            floorName = null;
            observingLoadAttempt = false;
            serverObserved = false;
            clientObserved = false;
            localPlayerObserved = false;
            nativeLoadingCompleted = false;
            clientReadyObserved = false;
        }

        internal static int ObserveNativeOperationStarted(string operation)
        {
            if (operation == "DungeonManager.LoadDungeon")
            {
                StartAttempt("native_world_load");
            }
            else if (operation == "DungeonManager.FloorAlloc")
            {
                EnsureAttempt("floor_allocation");
            }

            return observingLoadAttempt ? loadAttemptId : 0;
        }

        internal static void ObserveClientExplorationStarted(
            bool isSavedSession)
        {
            EnsureAttempt("client_exploration_start");
            clientObserved = true;
            explorationStartMode = ToExplorationStartMode(isSavedSession);
            RecordMilestone("client_exploration_started", null);
        }

        internal static void ObserveServerExplorationStarted(
            bool isSavedSession)
        {
            EnsureAttempt("server_exploration_start");
            serverObserved = true;
            explorationStartMode = ToExplorationStartMode(isSavedSession);
            RecordMilestone("server_exploration_started", null);
        }

        internal static void ObserveFloorAllocated(string guid, string name)
        {
            EnsureAttempt("floor_allocation");
            clientObserved = true;
            floorGuid = guid;
            floorName = name;
            RecordMilestone("floor_allocated", null);
        }

        internal static void ObserveGameplayContextReset()
        {
            if (observingLoadAttempt)
            {
                RecordMilestone("mod_gameplay_context_reset", null);
            }
        }

        internal static void ObserveFloorRenderFinalized(string milestone,
            string guid)
        {
            if (!observingLoadAttempt)
            {
                return;
            }

            floorGuid = guid;
            RecordMilestone(milestone, null);
            if (milestone == "floor_render_completed")
            {
                observingLoadAttempt = false;
            }
        }

        internal static void ObserveNativeLoadingTransition(PlayerAvatar player,
            int previousLoadingScreenType, int loadingScreenType)
        {
            if (!observingLoadAttempt || player == null ||
                !LocalPlayerResolver.IsLocal(player))
            {
                return;
            }

            RecordMilestone("native_loading_state_changed",
                previousLoadingScreenType + "->" + loadingScreenType);
            if (loadingScreenType == -1)
            {
                nativeLoadingCompleted = true;
                RecordMilestone("native_loading_completed", null);
            }
        }

        internal static void Poll()
        {
            if (!observingLoadAttempt)
            {
                return;
            }

            PlayerAvatar player = CombatManager.Instance?.CurrentPlayer;
            if (!localPlayerObserved && player != null &&
                LocalPlayerResolver.IsLocal(player))
            {
                localPlayerObserved = true;
                RecordMilestone("local_player_resolved", null);
            }

            if (player != null && !nativeLoadingCompleted &&
                LocalPlayerResolver.IsLocal(player) &&
                player.loadingScreenType == -1)
            {
                nativeLoadingCompleted = true;
                RecordMilestone("native_loading_completed", null);
            }

            ScreenFader fader = ScreenFader.Instance;
            bool overlayHidden = fader == null ||
                fader.loadingScreenImage == null ||
                fader.loadingScreenImage.alpha <= 0.01f;
            bool fadeCompleted = fader == null || !fader.IsFading;
            if (!clientReadyObserved && localPlayerObserved &&
                nativeLoadingCompleted && overlayHidden && fadeCompleted)
            {
                clientReadyObserved = true;
                RecordMilestone("client_ready", null);
            }
        }

        private static void EnsureAttempt(string attemptTrigger)
        {
            if (!observingLoadAttempt)
            {
                StartAttempt(attemptTrigger);
            }
        }

        private static void StartAttempt(string attemptTrigger)
        {
            if (observingLoadAttempt)
            {
                RecordMilestone("load_attempt_interrupted", null);
            }

            loadAttemptId++;
            loadAttemptStartedAt = Stopwatch.GetTimestamp();
            trigger = attemptTrigger;
            explorationStartMode = UnknownExplorationStartMode;
            floorGuid = null;
            floorName = null;
            observingLoadAttempt = true;
            serverObserved = false;
            clientObserved = false;
            localPlayerObserved = false;
            nativeLoadingCompleted = false;
            clientReadyObserved = false;
            RecordMilestone("load_attempt_started", null);
        }

        private static void RecordMilestone(string milestone, string detail)
        {
            DeveloperLogger.RecordLoadingMilestone(loadAttemptId, milestone,
                trigger, explorationStartMode, serverObserved, clientObserved,
                ElapsedMilliseconds(loadAttemptStartedAt), floorGuid, floorName,
                detail);
        }

        private static string ToExplorationStartMode(bool isSavedSession)
        {
            return isSavedSession ? "resume" : "new";
        }

        private static float ElapsedMilliseconds(long startedAt)
        {
            return (float)((Stopwatch.GetTimestamp() - startedAt) * 1000d /
                Stopwatch.Frequency);
        }
#else
        internal static void Reset() { }

        internal static int ObserveNativeOperationStarted(string operation)
        {
            return 0;
        }

        internal static void ObserveClientExplorationStarted(
            bool isSavedSession)
        { }

        internal static void ObserveServerExplorationStarted(
            bool isSavedSession)
        { }

        internal static void ObserveFloorAllocated(string guid, string name) { }
        internal static void ObserveGameplayContextReset() { }
        internal static void ObserveFloorRenderFinalized(string milestone,
            string guid)
        { }

        internal static void ObserveNativeLoadingTransition(PlayerAvatar player,
            int previousLoadingScreenType, int loadingScreenType)
        { }

        internal static void Poll() { }
#endif
    }
}
