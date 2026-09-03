using SephiriaEnhancements.Core;
using SephiriaEnhancements.Integration;
using UnityEngine;

namespace SephiriaEnhancements.Combat
{
    internal sealed partial class CombatInsightsController
    {
        private readonly StatisticsRetryCheckpoint retryStatistics = new StatisticsRetryCheckpoint();
        private bool encounterDefeated;

        internal void ObserveStatisticsRetry(StatisticsRetryTransition transition, long id, string floor)
        {
            if (transition == StatisticsRetryTransition.Cancel)
            {
                retryStatistics.Cancel();
                return;
            }
            PlayerAvatar local = LocalPlayerResolver.Resolve();
            if (!StatisticsCaptureEnabled) return;
            if (transition == StatisticsRetryTransition.CaptureBoss)
            {
                if (local == null || local.NetworkcurrentFloorGuid != floor || local.loadingScreenType != -1) return;
                floorStatistics.ObserveFloor(floor);
                floorStatistics.UpdateClock(Time.time, bossEncounter.Active ? bossEncounter.IsTiming : encounterActive);
                retryStatistics.Capture(id, local.netId, floorStatistics);
                return;
            }
            retryStatistics.Begin(transition == StatisticsRetryTransition.RetryBoss, id, floor);
            floorStatistics.UpdateClock(Time.time, false);
        }

        private void TickStatisticsRetry()
        {
            StatisticsRetryBridge.Tick();
            PlayerAvatar local = LocalPlayerResolver.Resolve();
            if (local != null && local.loadingScreenType != -1) retryStatistics.ObserveTravelStarted();
            bool ready = local != null && local.loadingScreenType == -1 &&
                !string.IsNullOrEmpty(local.NetworkcurrentFloorGuid);
            if (retryStatistics.TryRestore(local?.NetworkcurrentFloorGuid, local != null ? local.netId : 0,
                    ready, floorStatistics))
                encounterDefeated = false;
        }

        internal void FinishDefeatedEncounter()
        {
            if (encounterDefeated || retryStatistics.Pending) return;
            if (bossEncounter.Active) CompleteBossEncounter();
            else EndEncounter(Time.unscaledTime);
            encounterDefeated = true;
            hitStreakFeedback.Reset();
        }

        private bool AnyParticipantInBattle()
        {
            foreach (PlayerDamageState state in states.Values)
            {
                PlayerAvatar player = state.Avatar;
                if (player == null || player.IsDead || !player.IsInBattle || encounterScope == null ||
                    player.NetworkcurrentFloorGuid != encounterScope.FloorGuid) continue;
                Vector3 position = player.transform.position;
                if (encounterScope.Contains(position.x, position.y)) return true;
            }
            return false;
        }
    }
}
