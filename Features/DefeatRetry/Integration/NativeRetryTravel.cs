using System.Collections;
using System.Reflection;
using HarmonyLib;
using SephiriaEnhancements.Diagnostics;

namespace SephiriaEnhancements.DefeatRetry
{
    internal static class NativeRetryTravel
    {
        private static readonly FieldInfo Transactions = AccessTools.Field(typeof(DungeonManager), "travelingTransactions");
        private static readonly FieldInfo MovingViaMap = AccessTools.Field(typeof(PlayerAvatar), "movingFloorViaWorldmap");
        private static readonly MethodInfo SetHostTraveling = AccessTools.PropertySetter(typeof(DungeonManager), nameof(DungeonManager.IsHostTraveling));
        internal static bool IsAvailable => Transactions != null && MovingViaMap != null && SetHostTraveling != null;

        internal static void CancelDefeatedWorldTravel(DungeonManager dungeon)
        {
            // The native manager and players survive world reconstruction.
            // Each queued transaction owns one targetability decrement.
            var transactions = (IList)Transactions.GetValue(dungeon);
            int count = transactions.Count;
            foreach (object transaction in transactions)
            {
                var avatar = (PlayerAvatar)AccessTools.Field(transaction.GetType(), "avatar").GetValue(transaction);
                if (avatar == null) continue;
                avatar.NetworkcanBeTarget = (sbyte)(avatar.canBeTarget + 1);
                avatar.StopInvulnerable();
            }
            transactions.Clear();
            SetHostTraveling.Invoke(dungeon, new object[] { false });
            dungeon.requestLeaveOnHost = false;
            dungeon.eachPlayersPosition.Clear();
            dungeon.dungeonReleaseCounter.Clear();
            foreach (PlayerSpawner player in PlayerSpawner.MultiplayerList)
            {
                if (player?.PlayerAvatar == null) continue;
                MovingViaMap.SetValue(player.PlayerAvatar, false);
                player.gotoMySessionOnGameOverServerside = 0;
            }
            SupportLogger.Record("retry_previous_travel_cancelled", "requests=" + count);
        }
    }
}
