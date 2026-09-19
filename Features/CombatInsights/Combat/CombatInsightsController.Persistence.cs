using SephiriaEnhancements.Runtime.GameBridge;
using SephiriaEnhancements.Core;
using SephiriaEnhancements.Integration;
using UnityEngine;

namespace SephiriaEnhancements.Combat
{
    internal sealed partial class CombatInsightsController
    {
        private bool encounterDefeated;

        private void PrepareStatisticsSave()
        {
            if (!StatisticsCaptureEnabled) return;
            PlayerAvatar local = LocalPlayerResolver.Resolve();
            if (local == null || local.loadingScreenType != -1) return;
            floorStatistics.ObserveFloor(local.NetworkcurrentFloorGuid);
            floorStatistics.UpdateClock(Time.time, bossEncounter.Active ? bossEncounter.IsTiming : encounterActive);
        }

        internal void FinishDefeatedEncounter()
        {
            if (encounterDefeated || NativeLocalPlayerData.ProgressPending) return;
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
