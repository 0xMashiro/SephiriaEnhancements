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
        private struct Request : NetworkMessage { internal long Id; internal string Floor; internal bool Release; }
        private struct Reply : NetworkMessage { internal long Id; internal NativeRetrySapphire State; }
        private static long nextId, pendingId, localId;
        private static string floor;
        private static double deadline, localDeadline;
        private static NetworkConnectionToServer localConnection;
        private static PlayerAvatar localPlayer;
        private static Action resume;
        private static Action resumeWithoutCheckpoint;
        private static Action queuedBattle;
        private static bool applying;
        internal static bool Unavailable { get; private set; }
        private static readonly Dictionary<NetworkConnectionToClient, uint> waiting = new Dictionary<NetworkConnectionToClient, uint>();
        private static readonly Dictionary<uint, NativeRetrySapphire> states = new Dictionary<uint, NativeRetrySapphire>();
        internal static bool Pending => resume != null;
        internal static bool BlocksPurchases => localId != 0 || DefeatRetryClientRestore.IsRestoring ||
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
            Writer<Request>.write = (w, m) => { w.WriteLong(m.Id); w.WriteString(m.Floor); w.WriteBool(m.Release); };
            Reader<Request>.read = r => new Request { Id = r.ReadLong(), Floor = r.ReadString(), Release = r.ReadBool() };
            Writer<Reply>.write = (w, m) => { w.WriteLong(m.Id); NativeRetrySapphire.Write(w, m.State); };
            Reader<Reply>.read = r => new Reply { Id = r.ReadLong(), State = NativeRetrySapphire.Read(r) };
        }

        // Owners stop spending before replying on the same reliable channel as
        // native purchase Commands. The server serializes only after those replies.
        internal static bool Prepare(string floorGuid, Action continuation, Action onUnavailable = null)
        {
            if (applying) return true;
            if (Pending)
            {
                // Repeated native boss detection must not bypass an in-flight
                // capture. A one-shot trigger arriving during floor capture runs next.
                if (resumeWithoutCheckpoint == null && queuedBattle == null) queuedBattle = onUnavailable;
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
            deadline = Time.realtimeSinceStartupAsDouble + 5;
            states.Clear();
            waiting.Clear();
            foreach (var peer in NetworkServer.connections.Values)
            {
                PlayerAvatar player = peer.identity?.GetComponent<PlayerAvatar>();
                if (player == null || player.currentFloorGuid != floor) { Cancel(true); return false; }
                waiting.Add(peer, player.netId);
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
            if (request.Release)
            {
                if (localId == request.Id) ReleaseLocal();
                return;
            }
            PlayerAvatar player = LocalPlayerResolver.Resolve();
            NativeRetrySapphire state = null;
            if (player != null && !player.IsDead && player.currentFloorGuid == request.Floor && SaveManager.Current != null &&
                SaveManager.CurrentRun != null && !player.localDataStorage.runSapphireSettled &&
                UIManager.Instance?.GetElement<UI_GameOverLabel>()?.IsOpened != true)
            {
                localId = request.Id;
                localConnection = NetworkClient.connection;
                localPlayer = player;
                localDeadline = Time.realtimeSinceStartupAsDouble + 5;
                state = NativeRetrySapphire.Capture(player.localDataStorage);
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

        internal static Dictionary<uint, NativeRetrySapphire> CapturePlayers()
        {
            if (!applying) throw new InvalidOperationException("Retry capture has not been confirmed by all owners.");
            var result = new Dictionary<uint, NativeRetrySapphire>();
            foreach (var peer in NetworkServer.connections.Values)
            {
                PlayerSpawner player = peer.identity.GetComponent<PlayerSpawner>();
                result.Add(player.netId, states[player.netId].WithWorld(player));
            }
            return result;
        }

        private static void FinishIfReady()
        {
            if (!Pending || waiting.Count != 0) return;
            if (states.Count != NetworkServer.connections.Count) { Cancel(true); return; }
            foreach (var peer in NetworkServer.connections.Values)
            {
                PlayerAvatar player = peer.identity?.GetComponent<PlayerAvatar>();
                if (player == null || player.IsDead || player.currentFloorGuid != floor || !states.ContainsKey(player.netId)) { Cancel(true); return; }
            }
            Action action = resume;
            applying = true;
            try { action(); }
            finally { applying = false; Cancel(runQueuedBattle: true); }
        }

        internal static void Tick()
        {
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

        private static void ReleaseLocal() { localId = 0; localConnection = null; localPlayer = null; }

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
            Action nextBattle = continueBattle || runQueuedBattle ? queuedBattle : null;
            if (Pending && NetworkServer.active)
                foreach (var peer in NetworkServer.connections.Values)
                    if (peer != NetworkServer.localConnection) peer.Send(new Request { Id = pendingId, Release = true });
            ReleaseLocal();
            resume = null;
            resumeWithoutCheckpoint = null;
            queuedBattle = null;
            waiting.Clear();
            states.Clear();
            ContinueWithoutCheckpoint(fallback);
            nextBattle?.Invoke();
        }

        internal static void Shutdown()
        {
            Cancel();
            NetworkServer.UnregisterHandler<Reply>();
            NetworkClient.UnregisterHandler<Request>();
        }
    }
}
