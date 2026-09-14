using System;
using System.Collections.Generic;
using HarmonyLib;
using Mirror;
using SephiriaEnhancements.Configuration;
using SephiriaEnhancements.Diagnostics;
using SephiriaEnhancements.Integration;
using SephiriaEnhancements.Runtime;
using UnityEngine;

namespace SephiriaEnhancements.DefeatRetry
{
    // Observe after DungeonManager.Update: its travel completion writes the floor
    // before the spawn point. Neither the SyncVar hook nor camera rendering is
    // an authoritative, fully populated arrival notification.
    internal static class NativeRetryFloorEntry
    {
        private sealed class Entry
        {
            internal double Deadline;
            internal bool Closed;
        }

        private sealed class PlayerObservation
        {
            internal string Floor;
            internal Action<DamageInstance> Damaged;
            internal Action<UnitAvatar, DamageInstance> Attacked;
        }

        private static readonly Dictionary<string, Entry> entries = new Dictionary<string, Entry>();
        private static readonly Dictionary<PlayerAvatar, PlayerObservation> players = new Dictionary<PlayerAvatar, PlayerObservation>();
        private static readonly HashSet<PlayerAvatar> present = new HashSet<PlayerAvatar>();
        private static readonly HashSet<string> occupiedFloors = new HashSet<string>();
        private static readonly List<PlayerAvatar> removedPlayers = new List<PlayerAvatar>();
        private static readonly List<string> vacatedFloors = new List<string>();
        private static string capturingFloor;

        internal static void ObserveArrivals()
        {
            if (!NetworkServer.active) { Clear(); return; }
            if (DefeatRetryFeature.IsRetrying) return;
            present.Clear();
            occupiedFloors.Clear();
            foreach (var peer in NetworkServer.connections.Values)
            {
                PlayerAvatar player = peer.identity?.GetComponent<PlayerAvatar>();
                if (player == null) continue;
                present.Add(player);
                if (!string.IsNullOrEmpty(player.currentFloorGuid)) occupiedFloors.Add(player.currentFloorGuid);
                if (!players.TryGetValue(player, out PlayerObservation observation))
                {
                    observation = new PlayerObservation
                    {
                        Damaged = _ => FeatureFailure.Run(FeatureId.DefeatRetry, () => CombatStarted(player.currentFloorGuid)),
                        Attacked = (_, __) => FeatureFailure.Run(FeatureId.DefeatRetry, () => CombatStarted(player.currentFloorGuid))
                    };
                    players.Add(player, observation);
                    player.OnDamagedServerside += observation.Damaged;
                    player.OnAttackUnitBeforeOperation += observation.Attacked;
                }
                if (player.loadingScreenType != -1 || string.IsNullOrEmpty(player.currentFloorGuid) ||
                    observation.Floor == player.currentFloorGuid) continue;
                observation.Floor = player.currentFloorGuid;
                if (!entries.ContainsKey(observation.Floor))
                    entries.Add(observation.Floor, new Entry { Deadline = Time.realtimeSinceStartupAsDouble + 5 });
            }
            removedPlayers.Clear();
            foreach (PlayerAvatar player in players.Keys)
                if (!present.Contains(player)) removedPlayers.Add(player);
            foreach (PlayerAvatar player in removedPlayers) Unbind(player);

            vacatedFloors.Clear();
            foreach (var entry in entries)
            {
                if (!occupiedFloors.Contains(entry.Key)) vacatedFloors.Add(entry.Key);
                if (!entry.Value.Closed && Time.realtimeSinceStartupAsDouble >= entry.Value.Deadline)
                    Close(entry.Key, "arrival_window_expired");
            }
            foreach (string vacatedFloor in vacatedFloors)
            {
                Close(vacatedFloor, "floor_left");
                entries.Remove(vacatedFloor);
            }

            PlayerAvatar host = LocalPlayerResolver.Resolve();
            if (host == null || !CanCapture(host.currentFloorGuid) || NativeRetryCapture.Pending ||
                !DefeatRetryBridge.AllPlayersReady()) return;
            string floor = host.currentFloorGuid;
            capturingFloor = floor;
            NativeRetryCapture.Prepare(floor, () =>
            {
                if (CanCapture(floor)) DefeatRetryFeature.CaptureFloorEntryCheckpoint(floor);
                Close(floor, "capture_finished");
            }, () => Close(floor, "owner_capture_failed"));
        }

        internal static bool CanCapture(string floor)
        {
            if (!DefeatRetryPolicy.ShouldCaptureFloorEntryCheckpoint(EnhancementsSettings.Enabled,
                    DefeatRetrySettings.Enabled, DefeatRetryFeature.IsRetrying, NetworkServer.active,
                    SaveManager.Current != null, SaveManager.CurrentRun != null,
                    SaveManager.CurrentRun?.GetBool("RunStarted", false) == true) ||
                string.IsNullOrEmpty(floor) || !entries.TryGetValue(floor, out Entry entry) || entry.Closed ||
                Time.realtimeSinceStartupAsDouble >= entry.Deadline || NetworkServer.connections.Count == 0)
                return false;
            FloorGenerator generator = FloorGenerator.FindByGuid(floor);
            if (generator == null || !generator.GenerateSuccess) return false;
            foreach (var peer in NetworkServer.connections.Values)
            {
                PlayerAvatar player = peer.identity?.GetComponent<PlayerAvatar>();
                if (player == null || player.IsDead || player.loadingScreenType != -1 || player.currentFloorGuid != floor)
                    return false;
            }
            return true;
        }

        internal static bool BeforeBoss(string floor, object boss, Action startBattle)
        {
            if (CanCapture(floor) && !NativeRetryCapture.Pending) ObserveArrivals();
            if (capturingFloor == floor && NativeRetryCapture.Pending)
            {
                // Use the existing capture queue so a one-shot automatic trigger
                // resumes only after the floor snapshot has finished or failed.
                NativeRetryCapture.QueueBattle(boss, startBattle);
                return false;
            }
            return true;
        }

        internal static void CombatStarted(string floor) => Close(floor, "combat_started");

        private static void Close(string floor, string reason)
        {
            if (string.IsNullOrEmpty(floor)) return;
            if (!entries.TryGetValue(floor, out Entry entry))
                entries.Add(floor, entry = new Entry());
            if (entry.Closed) return;
            entry.Closed = true;
            SupportLogger.Record("retry_floor_entry_closed", "reason=" + reason + " floor=" + floor);
            if (capturingFloor == floor)
            {
                capturingFloor = null;
                NativeRetryCapture.Cancel(continueBattle: true);
            }
        }

        private static void Unbind(PlayerAvatar player)
        {
            PlayerObservation observation = players[player];
            player.OnDamagedServerside -= observation.Damaged;
            player.OnAttackUnitBeforeOperation -= observation.Attacked;
            players.Remove(player);
        }

        internal static void Clear()
        {
            foreach (PlayerAvatar player in new List<PlayerAvatar>(players.Keys)) Unbind(player);
            entries.Clear();
            capturingFloor = null;
        }
    }

    [HarmonyPatch(typeof(DungeonManager), "Update")]
    internal static class FloorEntryRetryCheckpointPatch
    {
        private static void Postfix()
        {
            if (!FeatureFailure.IsAvailable(FeatureId.DefeatRetry)) return;
            try { PostfixCore(); }
            catch (System.Exception exception) { FeatureFailure.Disable(FeatureId.DefeatRetry, exception); }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static void PostfixCore() => NativeRetryFloorEntry.ObserveArrivals();
    }
}
