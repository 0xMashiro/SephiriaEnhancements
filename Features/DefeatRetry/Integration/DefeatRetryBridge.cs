using SephiriaEnhancements.Runtime.GameBridge;
using System.Collections.Generic;
using Mirror;
using SephiriaEnhancements.Combat;
using SephiriaEnhancements.DefeatRetry;
using SephiriaEnhancements.Diagnostics;
using System;
using SephiriaEnhancements.Runtime;
using UnityEngine;

namespace SephiriaEnhancements.Integration
{
    internal static class DefeatRetryBridge
    {
        // The native runtime constant table is synchronized and rebuilt on world load.
        // Advertise here rather than persisting protocol state in the player's save.
        private const byte ProtocolVersion = 9;
        private const string ProtocolKey = "SephiriaEnhancements.DefeatRetryProtocol";
        private static CombatInsightsController controller;
        private static bool serverRegistered, clientRegistered;
        private static NetworkConnectionToServer registeredConnection;
        private static readonly HashSet<NetworkConnectionToClient> peers = new HashSet<NetworkConnectionToClient>();
        private static long nextCheckpointId;
        private static bool integrationAvailable;
        private static long retryId;
        private static readonly RetryRecovery<NetworkConnectionToClient> recovery = new RetryRecovery<NetworkConnectionToClient>();
        private static readonly Dictionary<NetworkConnectionToClient, NativeRetryArrival> destinations = new Dictionary<NetworkConnectionToClient, NativeRetryArrival>();
        private static readonly HashSet<NetworkConnectionToClient> receipts = new HashSet<NetworkConnectionToClient>();
        private static string recoveryFloor;
        private static RetryRecoveryStatus publishedStatus;
        private static long receivedRetryId;
        private static NetworkConnectionToServer receivedConnection;
        private static bool localRecoveryPending;
        internal static bool LocalRecoveryPending => localRecoveryPending && NetworkClient.active && receivedConnection == NetworkClient.connection;
        internal const float RecoveryTimeout = 90f;

        internal static void SetIntegrationAvailable(bool available) => integrationAvailable = available;

        private struct Hello : NetworkMessage { internal byte Version; }
        private struct ReadyReceipt : NetworkMessage { internal long RetryId; internal bool Success; }
        private struct ConclusionNotification : NetworkMessage { internal RetryConclusionKind Kind; }
        private struct Notification : NetworkMessage
        {
            internal RetryTransition Transition;
            internal long CheckpointId;
            internal string FloorGuid;
            internal long RetryId;
            internal Vector3 Position;
            internal RetryRecoveryFailure Failure;
            internal NativeRetryPlayerState PlayerState;
        }

        internal static void Initialize(CombatInsightsController value)
        {
            controller = value;
            NativeRetryConclusion.Reset();
            NativeRetryCapture.Initialize();
            Writer<Hello>.write = (writer, message) => writer.WriteByte(message.Version);
            Reader<Hello>.read = reader => new Hello { Version = reader.ReadByte() };
            Writer<ReadyReceipt>.write = (writer, message) => { writer.WriteLong(message.RetryId); writer.WriteByte(message.Success ? (byte)1 : (byte)0); };
            Reader<ReadyReceipt>.read = reader => new ReadyReceipt { RetryId = reader.ReadLong(), Success = reader.ReadByte() == 1 };
            Writer<ConclusionNotification>.write = (writer, message) => writer.WriteByte((byte)message.Kind);
            Reader<ConclusionNotification>.read = reader => new ConclusionNotification { Kind = (RetryConclusionKind)reader.ReadByte() };
            Writer<Notification>.write = (writer, message) =>
            {
                writer.WriteByte((byte)message.Transition);
                writer.WriteLong(message.CheckpointId);
                writer.WriteString(message.FloorGuid);
                writer.WriteLong(message.RetryId);
                writer.WriteVector3(message.Position);
                writer.WriteByte((byte)message.Failure);
                NativeRetryPlayerState.Write(writer, message.PlayerState);
            };
            Reader<Notification>.read = reader => new Notification
            {
                Transition = (RetryTransition)reader.ReadByte(),
                CheckpointId = reader.ReadLong(), FloorGuid = reader.ReadString(), RetryId = reader.ReadLong(), Position = reader.ReadVector3(),
                Failure = (RetryRecoveryFailure)reader.ReadByte(), PlayerState = NativeRetryPlayerState.Read(reader)
            };
            Tick();
        }

        internal static void Tick()
        {
            if (NetworkServer.active)
            {
                if (!serverRegistered)
                {
                    NetworkServer.RegisterHandler<Hello>((connection, message) => FeatureFailure.Run(FeatureId.DefeatRetry, () =>
                    {
                        if (message.Version == ProtocolVersion) peers.Add(connection);
                    }));
                    NetworkServer.RegisterHandler<ReadyReceipt>((connection, message) => FeatureFailure.Run(FeatureId.DefeatRetry, () =>
                    {
                        AcceptReadyReceipt(connection, message.RetryId, message.Success);
                    }));
                    serverRegistered = true;
                    NativeRetryCapture.RegisterServer();
                }
                if (integrationAvailable && DungeonManager.Instance != null)
                    DungeonManager.Instance.constValueDictionary[ProtocolKey] = ProtocolVersion;
                peers.RemoveWhere(peer => !NetworkServer.connections.TryGetValue(peer.connectionId, out var current) || current != peer);
                if (recovery.Status == RetryRecoveryStatus.Waiting)
                {
                    foreach (var peer in destinations.Keys)
                        if (!NetworkServer.connections.TryGetValue(peer.connectionId, out var current) || current != peer)
                            recovery.Fail(RetryRecoveryFailure.Disconnected);
                    recovery.CheckDeadline(Time.realtimeSinceStartupAsDouble);
                    ConfirmDestinations();
                }
                PublishRecoveryResult();
            }
            else
            {
                serverRegistered = false;
                peers.Clear();
                ClearArrivals();
            }
            if (!NetworkClient.active)
            {
                NativeRetryConclusion.Reset();
                NativeRetryAccount.Clear();
                clientRegistered = false;
                registeredConnection = null;
                return;
            }
            if (!clientRegistered)
            {
                NetworkClient.RegisterHandler<ConclusionNotification>(message => FeatureFailure.Run(FeatureId.DefeatRetry, () =>
                {
                    if (!NetworkServer.active) NativeRetryConclusion.Receive(message.Kind);
                }));
                NetworkClient.RegisterHandler<Notification>(message => FeatureFailure.Run(FeatureId.DefeatRetry, () =>
                {
                    if (!NetworkServer.active && message.Transition <= RetryTransition.RecoveryCompleted)
                        Receive(message);
                }));
                clientRegistered = true;
                NativeRetryCapture.RegisterClient();
            }
            if (integrationAvailable && !NetworkServer.active && NetworkClient.ready && NetworkClient.connection != null &&
                registeredConnection != NetworkClient.connection && DungeonManager.Instance != null &&
                DungeonManager.Instance.constValueDictionary.TryGetValue(ProtocolKey, out int version) && version == ProtocolVersion)
            {
                NetworkClient.Send(new Hello { Version = ProtocolVersion });
                registeredConnection = NetworkClient.connection;
            }
        }

        internal static long CaptureBoss(string floorGuid)
        {
            long id = ++nextCheckpointId;
            Publish(RetryTransition.CaptureBoss, id, floorGuid);
            return id;
        }

        // Sent on the reliable channel before the native game-over RPC. Clients
        // consume the authority's cause when that RPC opens their result panel.
        internal static void PublishConclusion(RetryConclusionKind kind)
        {
            if (!NetworkServer.active) return;
            Tick();
            NativeRetryConclusion.Receive(kind);
            foreach (var peer in peers)
                if (peer.isReady && peer != NetworkServer.localConnection)
                    peer.Send(new ConclusionNotification { Kind = kind });
        }

        internal static void Publish(RetryTransition transition, long id, string floorGuid)
        {
            if (!NetworkServer.active) return;
            Tick();
            if (transition == RetryTransition.RetryFloor || transition == RetryTransition.RetryBoss)
            {
                retryId++;
                destinations.Clear();
                receipts.Clear();
                recoveryFloor = floorGuid;
                foreach (var peer in NetworkServer.connections.Values)
                {
                    PlayerAvatar avatar = peer.identity?.GetComponent<PlayerAvatar>();
                    if (DefeatRetryFeature.TryGetPendingDestination(avatar, out Vector3 position))
                        destinations.Add(peer, new NativeRetryArrival(avatar, floorGuid, position));
                }
                recovery.Begin(retryId, destinations.Keys, Time.realtimeSinceStartupAsDouble + RecoveryTimeout);
                if (destinations.Count != NetworkServer.connections.Count) recovery.Fail(RetryRecoveryFailure.RestoreFailed);
                publishedStatus = RetryRecoveryStatus.Waiting;
            }
            if (transition == RetryTransition.Cancel) ClearArrivals();
            var message = new Notification { Transition = transition, CheckpointId = id, FloorGuid = floorGuid, RetryId = retryId };
            if (destinations.TryGetValue(NetworkServer.localConnection, out NativeRetryArrival localArrival))
                message.Position = localArrival.Destination;
            if (transition == RetryTransition.RetryFloor || transition == RetryTransition.RetryBoss)
                message.PlayerState = DefeatRetryFeature.GetPendingPlayerState(LocalPlayerResolver.Resolve());
            Receive(message);
            foreach (var peer in peers)
                if (peer.isReady && peer != NetworkServer.localConnection)
                {
                    message.Position = destinations.TryGetValue(peer, out NativeRetryArrival arrival) ? arrival.Destination : default;
                    if (transition == RetryTransition.RetryFloor || transition == RetryTransition.RetryBoss)
                        message.PlayerState = DefeatRetryFeature.GetPendingPlayerState(peer.identity?.GetComponent<PlayerAvatar>());
                    peer.Send(message);
                }
        }

        internal static bool AllPlayersReady()
        {
            if (!integrationAvailable || recovery.BlocksBattle || NativeRetryCapture.Pending) return false;
            foreach (var connection in NetworkServer.connections.Values)
                if (connection != NetworkServer.localConnection &&
                    (connection == null || !connection.isReady || !peers.Contains(connection))) return false;
            return true;
        }

        internal static void CancelPlayer(PlayerAvatar player)
        {
            if (player != null && recovery.IsWaiting(player.connectionToClient))
                recovery.Fail(RetryRecoveryFailure.RestoreFailed);
        }

        internal static bool IsAwaitingArrival(PlayerAvatar player) =>
            recovery.BlocksBattle && player != null && player.connectionToClient != null && destinations.ContainsKey(player.connectionToClient);

        internal static void ClearArrivals()
        {
            recovery.Cancel();
            destinations.Clear();
            receipts.Clear();
            publishedStatus = RetryRecoveryStatus.Cancelled;
        }
        internal static bool BlocksBossBattle => recovery.BlocksBattle;
        internal static long CurrentRecoveryId => retryId;
        internal static long LocalRecoveryId => receivedRetryId;
        internal static bool HasRecoveryFailed(long id) => id == retryId && recovery.Status == RetryRecoveryStatus.Failed;
        internal static bool CanContinueRestart(long id) => id == retryId &&
            (recovery.Status == RetryRecoveryStatus.Waiting || recovery.Status == RetryRecoveryStatus.Completed);
        internal static void FailRecovery()
        {
            if (recovery.Status != RetryRecoveryStatus.Waiting)
                recovery.Begin(++retryId, NetworkServer.connections.Values, Time.realtimeSinceStartupAsDouble);
            recovery.Fail(RetryRecoveryFailure.RestoreFailed);
            receivedRetryId = retryId;
            publishedStatus = RetryRecoveryStatus.Waiting;
        }

        internal static void ReportReady(long id, bool success = true)
        {
            if (NetworkServer.active) AcceptReadyReceipt(NetworkServer.localConnection, id, success);
            else if (NetworkClient.active && NetworkClient.connection != null)
                NetworkClient.Send(new ReadyReceipt { RetryId = id, Success = success });
        }

        private static void AcceptReadyReceipt(NetworkConnectionToClient peer, long id, bool success)
        {
            if (id != retryId || recovery.Status != RetryRecoveryStatus.Waiting || !destinations.ContainsKey(peer)) return;
            if (success)
            {
                if (!recovery.IsWaiting(peer)) return;
                receipts.Add(peer);
            }
            else recovery.Fail(RetryRecoveryFailure.RestoreFailed);
            SupportLogger.Record("retry_client_ready_receipt", "connection=" + peer.connectionId + " success=" + success,
                success ? "INFO" : "ERROR");
        }

        private static void ConfirmDestinations()
        {
            if (DefeatRetryFeature.IsRetrying || recovery.Status != RetryRecoveryStatus.Waiting) return;
            // Validate every participant before the last receipt can complete
            // the recovery; dictionary order must not decide whether failure wins.
            foreach (var pair in destinations)
            {
                PlayerAvatar player = pair.Key.identity?.GetComponent<PlayerAvatar>();
                if (player == null || player != pair.Value.Player ||
                    (!recovery.IsWaiting(pair.Key) && !pair.Value.IsCurrent))
                {
                    recovery.Fail(RetryRecoveryFailure.RestoreFailed);
                    return;
                }
            }
            foreach (var pair in destinations)
            {
                if (!recovery.IsWaiting(pair.Key)) continue;
                PlayerAvatar player = pair.Value.Player;
                // Initialize owns the move request. Never observe the defeated
                // world before that request has been issued for this player.
                if (DefeatRetryFeature.HasPendingPlacement(player)) continue;
                bool arrived = pair.Value.Observe();
                if (arrived && receipts.Contains(pair.Key) &&
                    DefeatRetryFeature.GetPendingPlayerState(player)?.Matches(player.GetComponent<PlayerSpawner>()) == true)
                    recovery.Report(retryId, pair.Key, true);
            }
        }

        private static void PublishRecoveryResult()
        {
            if (recovery.Status == publishedStatus) return;
            publishedStatus = recovery.Status;
            if (publishedStatus != RetryRecoveryStatus.Failed && publishedStatus != RetryRecoveryStatus.Completed) return;
            if (publishedStatus == RetryRecoveryStatus.Failed)
            {
                foreach (var pair in destinations)
                    SupportLogger.Record("retry_server_incomplete", "retry=" + retryId + " receipt=" + receipts.Contains(pair.Key) +
                        " " + NativeRetryArrival.Describe(pair.Key.identity?.GetComponent<PlayerAvatar>(), recoveryFloor, pair.Value.Destination));
                DefeatRetryFeature.AbortRecovery();
            }
            var message = new Notification { RetryId = retryId, Failure = recovery.Failure,
                Transition = publishedStatus == RetryRecoveryStatus.Failed ? RetryTransition.RecoveryFailed : RetryTransition.RecoveryCompleted };
            Receive(message);
            foreach (var peer in peers)
                if (peer.isReady && peer != NetworkServer.localConnection) peer.Send(message);
            SupportLogger.Record("retry_recovery_finished", "retry=" + retryId + " status=" + publishedStatus + " reason=" + recovery.Failure);
        }

        private static void Receive(Notification message)
        {
            if (receivedConnection != NetworkClient.connection)
            {
                receivedConnection = NetworkClient.connection;
                receivedRetryId = 0;
                localRecoveryPending = false;
            }
            if (message.Transition == RetryTransition.RecoveryFailed || message.Transition == RetryTransition.RecoveryCompleted)
                if (message.RetryId != receivedRetryId) return;
            if (message.Transition == RetryTransition.RecoveryFailed || message.Transition == RetryTransition.RecoveryCompleted ||
                message.Transition == RetryTransition.Cancel) localRecoveryPending = false;
            if (message.Transition == RetryTransition.RetryFloor ||
                message.Transition == RetryTransition.RetryBoss)
            {
                receivedRetryId = message.RetryId;
                localRecoveryPending = true;
                NativeRetryBoss.Begin(message.FloorGuid);
                try
                {
                    DefeatRetryClientRestore.Begin(message.FloorGuid, message.RetryId, message.Position, message.PlayerState,
                        message.Transition == RetryTransition.RetryBoss ? message.CheckpointId : 0);
                }
                catch (Exception exception) { DefeatRetryClientRestore.Fail(exception); }
            }
            else if (message.Transition == RetryTransition.Cancel)
            {
                NativeRetryBoss.Clear();
                NativeRetryAccount.Clear();
                DefeatRetryClientRestore.Clear();
            }
            else if (message.Transition == RetryTransition.RecoveryFailed)
            {
                NativeRetryBoss.Clear();
                DefeatRetryClientRestore.RecordFailureContext();
                DefeatRetryClientRestore.Clear();
                NativeRetryFailure.Show(message.Failure);
            }
            if (message.Transition == RetryTransition.RecoveryCompleted)
                DefeatRetryClientRestore.Complete(message.RetryId);
            if (message.Transition == RetryTransition.CaptureBoss)
            {
                try { NativeLocalPlayerData.CaptureCheckpoint(message.CheckpointId, message.FloorGuid); }
                catch (Exception exception) { SupportLogger.Failure("local_player_data_capture_failed", exception); }
            }
        }

        internal static void ObserveTeamDefeat()
        {
            try { controller?.FinishDefeatedEncounter(); }
            catch (Exception exception) { SupportLogger.Failure("retry_statistics_failed", exception); }
        }

        internal static void Shutdown()
        {
            NativeRetryConclusion.Reset();
            NativeRetryFloorEntry.Clear();
            NativeRetryCapture.Shutdown();
            NetworkServer.UnregisterHandler<Hello>();
            NetworkServer.UnregisterHandler<ReadyReceipt>();
            NetworkClient.UnregisterHandler<Notification>();
            NetworkClient.UnregisterHandler<ConclusionNotification>();
            controller = null;
            peers.Clear();
            ClearArrivals();
            serverRegistered = clientRegistered = false;
            registeredConnection = null;
            localRecoveryPending = false;
            DefeatRetryClientRestore.Clear();
            NativeRetryBoss.Clear();
            integrationAvailable = false;
            NativeRetryFailure.Clear();
            if (NetworkServer.active && DungeonManager.Instance != null)
                DungeonManager.Instance.constValueDictionary.Remove(ProtocolKey);
        }

        internal static void StopAfterFeatureFailure()
        {
            NativeRetryFloorEntry.Clear();
            SephiriaEnhancementsMod.CleanupFeature(FeatureId.DefeatRetry,
                () => NativeRetryCapture.Cancel(continueBattle: true));
            integrationAvailable = false;
            bool restoring = DefeatRetryFeature.IsRetrying || recovery.BlocksBattle ||
                DefeatRetryClientRestore.IsRestoring || LocalRecoveryPending || NativeRetryFailure.IsPending;
            try
            {
                if (restoring && NetworkServer.active)
                {
                    FailRecovery();
                    PublishRecoveryResult();
                }
                else if (LocalRecoveryPending) ReportReady(receivedRetryId, success: false);
                else if (restoring) DefeatRetryClientRestore.ReportFailure();
            }
            finally
            {
                SephiriaEnhancementsMod.CleanupFeature(FeatureId.DefeatRetry, DefeatRetryFeature.AbortRecovery);
                SephiriaEnhancementsMod.CleanupFeature(FeatureId.DefeatRetry, Shutdown);
                if (restoring) NativeRetryFailure.Show(RetryRecoveryFailure.RestoreFailed);
            }
        }
    }
}
