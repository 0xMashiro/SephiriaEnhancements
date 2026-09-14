using System;
using System.Collections.Generic;
using Mirror;
using UnityEngine;
using SephiriaEnhancements.Integration;
using SephiriaEnhancements.Runtime;
using SephiriaEnhancements.Diagnostics;

namespace SephiriaEnhancements.DefeatRetry
{
    internal static class NativeRetryCapture
    {
        private struct Request : NetworkMessage { internal long Id; internal string Floor; internal bool Release; internal RetryCheckpointKind Kind; internal string BossName; }
        private struct Reply : NetworkMessage { internal long Id; internal NativeRetryPlayerState State; }
        private static long nextId, pendingId, localId;
        private static string floor;
        private static Request? awaitingFloor;
        private static NetworkConnectionToServer awaitingConnection;
        private static double deadline, localDeadline;
        private static NetworkConnectionToServer localConnection;
        private static PlayerAvatar localPlayer;
        private static Action resume;
        private static Action resumeWithoutCheckpoint;
        private static object activeBattleOwner;
        private static readonly Dictionary<object, Action> queuedBattles = new Dictionary<object, Action>();
        private static bool applying;
        internal static bool Unavailable { get; private set; }
        private static readonly Dictionary<NetworkConnectionToClient, uint> waiting = new Dictionary<NetworkConnectionToClient, uint>();
        private static readonly Dictionary<NetworkConnectionToClient, PlayerAvatar> participants = new Dictionary<NetworkConnectionToClient, PlayerAvatar>();
        private static readonly Dictionary<uint, NativeRetryPlayerState> states = new Dictionary<uint, NativeRetryPlayerState>();
        internal static bool Pending => resume != null;
        internal static void QueueBattle(object owner, Action battle)
        {
            if (Pending && owner != null && owner != activeBattleOwner && battle != null && !queuedBattles.ContainsKey(owner))
                queuedBattles.Add(owner, battle);
        }
        internal static bool BlocksPurchases => awaitingFloor.HasValue || localId != 0 || DefeatRetryClientRestore.IsRestoring ||
            DefeatRetryBridge.LocalRecoveryPending;

        internal static void RegisterServer()
        {
            NetworkServer.RegisterHandler<Reply>((peer, message) => FeatureFailure.Run(FeatureId.DefeatRetry,
                () => Accept(peer, message)));
        }

        internal static void RegisterClient()
        {
            NetworkClient.RegisterHandler<Request>(message => FeatureFailure.Run(FeatureId.DefeatRetry,
                () => Receive(message)));
        }

        internal static void Initialize()
        {
            Writer<Request>.write = (w, m) => { w.WriteLong(m.Id); w.WriteString(m.Floor); w.WriteBool(m.Release); w.WriteByte((byte)m.Kind); w.WriteString(m.BossName); };
            Reader<Request>.read = r => new Request { Id = r.ReadLong(), Floor = r.ReadString(), Release = r.ReadBool(), Kind = (RetryCheckpointKind)r.ReadByte(), BossName = r.ReadString() };
            Writer<Reply>.write = (w, m) => { w.WriteLong(m.Id); NativeRetryPlayerState.Write(w, m.State); };
            Reader<Reply>.read = r => new Reply { Id = r.ReadLong(), State = NativeRetryPlayerState.Read(r) };
        }

        // Owners stop spending before replying on the same reliable channel as
        // native purchase Commands. The server serializes only after those replies.
        internal static bool Prepare(string floorGuid, Action continuation, Action onUnavailable = null, object battleOwner = null)
        {
            if (applying) return true;
            if (Pending)
            {
                // Repeated native boss detection must not bypass an in-flight
                // capture. A one-shot trigger arriving during floor capture runs next.
                if (battleOwner != null || resumeWithoutCheckpoint == null)
                    QueueBattle(battleOwner ?? (object)onUnavailable, onUnavailable);
                return false;
            }
            if (!DefeatRetryBridge.AllPlayersReady())
            {
                ContinueWithoutCheckpoint(onUnavailable);
                return false;
            }
            pendingId = ++nextId;
            floor = floorGuid;
            resume = continuation;
            resumeWithoutCheckpoint = onUnavailable;
            activeBattleOwner = battleOwner;
            deadline = Time.realtimeSinceStartupAsDouble + 5;
            states.Clear();
            waiting.Clear();
            participants.Clear();
            foreach (var peer in NetworkServer.connections.Values)
            {
                PlayerAvatar player = peer.identity?.GetComponent<PlayerAvatar>();
                if (player == null || player.currentFloorGuid != floor) { Cancel(true); return false; }
                waiting.Add(peer, player.netId);
                participants.Add(peer, player);
            }
            var request = new Request { Id = pendingId, Floor = floor };
            foreach (var peer in new List<NetworkConnectionToClient>(waiting.Keys))
            {
                if (!Pending) break;
                if (peer == NetworkServer.localConnection) Receive(request);
                else peer.Send(request);
            }
            FinishIfReady();
            return false;
        }

        private static void Receive(Request request)
        {
            if (request.Kind != RetryCheckpointKind.None)
            {
                NativeRetryAccount.Commit(request.Id, request.Kind, request.BossName);
                return;
            }
            if (request.Release)
            {
                if (localId == request.Id || awaitingFloor?.Id == request.Id) ReleaseLocal();
                return;
            }
            awaitingFloor = request;
            awaitingConnection = NetworkClient.connection;
            localDeadline = Time.realtimeSinceStartupAsDouble + 5;
            ReplyWhenArrived();
        }

        private static void ReplyWhenArrived()
        {
            if (!awaitingFloor.HasValue) return;
            if (!NetworkClient.active || awaitingConnection != NetworkClient.connection) { ReleaseLocal(); return; }
            Request request = awaitingFloor.Value;
            PlayerAvatar player = LocalPlayerResolver.Resolve();
            // A request sent after authoritative arrival can precede the SyncVars
            // on the owner's client. Wait for those fields, not an arbitrary delay.
            if (Time.realtimeSinceStartupAsDouble < localDeadline && player != null && !player.IsDead &&
                (player.currentFloorGuid != request.Floor || player.loadingScreenType != -1)) return;
            awaitingFloor = null;
            NativeRetryPlayerState state = null;
            if (Time.realtimeSinceStartupAsDouble < localDeadline && player != null && !player.IsDead && player.loadingScreenType == -1 && player.currentFloorGuid == request.Floor && SaveManager.Current != null &&
                SaveManager.CurrentRun != null && !player.localDataStorage.runSapphireSettled &&
                UIManager.Instance?.GetElement<UI_GameOverLabel>()?.IsOpened != true)
            {
                localId = request.Id;
                localConnection = NetworkClient.connection;
                localPlayer = player;
                localDeadline = Time.realtimeSinceStartupAsDouble + 5;
                NativeRetryAccount.Capture(request.Id);
                state = NativeRetryPlayerState.Capture(player.localDataStorage, request.Id);
            }
            var reply = new Reply { Id = request.Id, State = state };
            if (NetworkServer.active) Accept(NetworkServer.localConnection, reply);
            else NetworkClient.Send(reply);
        }

        private static void Accept(NetworkConnectionToClient peer, Reply reply)
        {
            if (!Pending || reply.Id != pendingId || !waiting.TryGetValue(peer, out uint player)) return;
            if (reply.State == null) { Cancel(true); return; }
            states.Add(player, reply.State);
            waiting.Remove(peer);
            // Completion is driven after the message dispatch, never recursively
            // while Prepare is still sending the remaining requests.
        }

        internal static Dictionary<uint, NativeRetryPlayerState> CapturePlayers()
        {
            if (!applying) throw new InvalidOperationException("Retry capture has not been confirmed by all owners.");
            var result = new Dictionary<uint, NativeRetryPlayerState>();
            foreach (var peer in NetworkServer.connections.Values)
            {
                PlayerSpawner player = peer.identity.GetComponent<PlayerSpawner>();
                result.Add(player.netId, states[player.netId].WithWorld(player));
            }
            return result;
        }

        internal static void Commit(RetryCheckpointKind kind, string bossName)
        {
            if (!applying) throw new InvalidOperationException("Owner capture is not active.");
            var message = new Request { Id = pendingId, Kind = kind, BossName = bossName };
            Receive(message);
            foreach (var peer in NetworkServer.connections.Values)
                if (peer != NetworkServer.localConnection) peer.Send(message);
        }

        private static void FinishIfReady()
        {
            if (!Pending || waiting.Count != 0) return;
            if (states.Count != NetworkServer.connections.Count) { Cancel(true); return; }
            foreach (var peer in NetworkServer.connections.Values)
            {
                PlayerAvatar player = peer.identity?.GetComponent<PlayerAvatar>();
                if (player == null || player.IsDead || player.currentFloorGuid != floor || !states.ContainsKey(player.netId) ||
                    !participants.TryGetValue(peer, out PlayerAvatar original) || original != player) { Cancel(true); return; }
            }
            Action action = resume;
            applying = true;
            try { action(); }
            finally { applying = false; Cancel(runQueuedBattle: true); }
        }

        internal static void Tick()
        {
            ReplyWhenArrived();
            if (localId != 0 && (localConnection != NetworkClient.connection || localPlayer != LocalPlayerResolver.Resolve() ||
                Time.realtimeSinceStartupAsDouble >= localDeadline)) ReleaseLocal();
            if (!Pending) return;
            if (!NetworkServer.active || Time.realtimeSinceStartupAsDouble >= deadline)
            {
                SupportLogger.Record("retry_checkpoint_unavailable", "Owner capture did not complete.");
                Cancel(true);
                return;
            }
            foreach (var peer in waiting.Keys)
                if (!NetworkServer.connections.TryGetValue(peer.connectionId, out var current) || current != peer) { Cancel(true); return; }
            FinishIfReady();
        }

        private static void ReleaseLocal() { NativeRetryAccount.Release(localId); awaitingFloor = null; awaitingConnection = null; localId = 0; localConnection = null; localPlayer = null; }

        private static void ContinueWithoutCheckpoint(Action action)
        {
            if (action == null) return;
            Unavailable = true;
            try { action(); }
            finally { Unavailable = false; }
        }

        internal static void Cancel(bool continueBattle = false, bool runQueuedBattle = false)
        {
            if (applying) return;
            Action fallback = continueBattle ? resumeWithoutCheckpoint : null;
            var nextBattles = continueBattle || runQueuedBattle
                ? new List<Action>(queuedBattles.Values) : null;
            if (Pending && NetworkServer.active)
                foreach (var peer in NetworkServer.connections.Values)
                    if (peer != NetworkServer.localConnection) peer.Send(new Request { Id = pendingId, Release = true });
            ReleaseLocal();
            resume = null;
            resumeWithoutCheckpoint = null;
            activeBattleOwner = null;
            queuedBattles.Clear();
            waiting.Clear();
            participants.Clear();
            states.Clear();
            ContinueWithoutCheckpoint(fallback);
            if (nextBattles != null)
                foreach (Action battle in nextBattles) battle();
        }

        internal static void Shutdown()
        {
            Cancel();
            NativeRetryAccount.Clear();
            NetworkServer.UnregisterHandler<Reply>();
            NetworkClient.UnregisterHandler<Request>();
        }
    }
}
