using System.Collections.Generic;
using Mirror;
using SephiriaEnhancements.Combat;
using SephiriaEnhancements.Core;
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
        private const string ProtocolKey = "SephiriaEnhancements.DefeatRetryProtocol";
        private static CombatInsightsController controller;
        private static bool serverRegistered, clientRegistered;
        private static NetworkConnectionToServer registeredConnection;
        private static readonly HashSet<NetworkConnectionToClient> peers = new HashSet<NetworkConnectionToClient>();
        private static long nextCheckpointId;
        private static bool integrationAvailable;
        private static long retryId;
        private static readonly RetryRecovery<NetworkConnectionToClient> recovery = new RetryRecovery<NetworkConnectionToClient>();
        private static readonly Dictionary<NetworkConnectionToClient, Vector3> destinations = new Dictionary<NetworkConnectionToClient, Vector3>();
        private static readonly HashSet<NetworkConnectionToClient> receipts = new HashSet<NetworkConnectionToClient>();
        private static string recoveryFloor;
        private static RetryRecoveryStatus publishedStatus;
        private static long receivedRetryId;
        private static NetworkConnectionToServer receivedConnection;
        internal const float RecoveryTimeout = 90f;

        internal static void SetIntegrationAvailable(bool available) => integrationAvailable = available;

        private struct Hello : NetworkMessage { internal byte Version; }
        private struct Arrival : NetworkMessage { internal long RetryId; internal bool Success; }
        private struct Notification : NetworkMessage
        {
            internal RetryTransition Transition;
            internal long CheckpointId;
            internal string FloorGuid;
            internal long RetryId;
            internal Vector3 Position;
            internal RetryRecoveryFailure Failure;
        }

        internal static void Initialize(CombatInsightsController value)
        {
            controller = value;
            Writer<Hello>.write = (writer, message) => writer.WriteByte(message.Version);
            Reader<Hello>.read = reader => new Hello { Version = reader.ReadByte() };
            Writer<Arrival>.write = (writer, message) => { writer.WriteLong(message.RetryId); writer.WriteByte(message.Success ? (byte)1 : (byte)0); };
            Reader<Arrival>.read = reader => new Arrival { RetryId = reader.ReadLong(), Success = reader.ReadByte() == 1 };
            Writer<Notification>.write = (writer, message) =>
            {
                writer.WriteByte((byte)message.Transition);
                writer.WriteLong(message.CheckpointId);
                writer.WriteString(message.FloorGuid);
                writer.WriteLong(message.RetryId);
                writer.WriteVector3(message.Position);
                writer.WriteByte((byte)message.Failure);
            };
            Reader<Notification>.read = reader => new Notification
            {
                Transition = (RetryTransition)reader.ReadByte(),
                CheckpointId = reader.ReadLong(), FloorGuid = reader.ReadString(), RetryId = reader.ReadLong(), Position = reader.ReadVector3(),
                Failure = (RetryRecoveryFailure)reader.ReadByte()
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
                        if (message.Version == 2) peers.Add(connection);
                    }));
                    NetworkServer.RegisterHandler<Arrival>((connection, message) => FeatureFailure.Run(FeatureId.DefeatRetry, () =>
                    {
                        AcceptReceipt(connection, message.RetryId, message.Success);
                    }));
                    serverRegistered = true;
                }
                if (integrationAvailable && DungeonManager.Instance != null)
                    DungeonManager.Instance.constValueDictionary[ProtocolKey] = 2;
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
                clientRegistered = false;
                registeredConnection = null;
                return;
            }
            if (!clientRegistered)
            {
                NetworkClient.RegisterHandler<Notification>(message => FeatureFailure.Run(FeatureId.DefeatRetry, () =>
                {
                    if (!NetworkServer.active && message.Transition <= RetryTransition.RecoveryCompleted)
                        Receive(message);
                }));
                clientRegistered = true;
            }
            if (integrationAvailable && !NetworkServer.active && NetworkClient.ready && NetworkClient.connection != null &&
                registeredConnection != NetworkClient.connection && DungeonManager.Instance != null &&
                DungeonManager.Instance.constValueDictionary.TryGetValue(ProtocolKey, out int version) && version == 2)
            {
                NetworkClient.Send(new Hello { Version = 2 });
                registeredConnection = NetworkClient.connection;
            }
        }

        internal static long CaptureBoss(string floorGuid)
        {
            long id = ++nextCheckpointId;
            Publish(RetryTransition.CaptureBoss, id, floorGuid);
            return id;
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
                    if (DefeatRetryFeature.TryGetPendingDestination(avatar, out Vector3 position)) destinations.Add(peer, position);
                }
                recovery.Begin(retryId, destinations.Keys, Time.realtimeSinceStartupAsDouble + RecoveryTimeout);
                if (destinations.Count != NetworkServer.connections.Count) recovery.Fail(RetryRecoveryFailure.RestoreFailed);
                publishedStatus = RetryRecoveryStatus.Waiting;
            }
            if (transition == RetryTransition.Cancel) ClearArrivals();
            var message = new Notification { Transition = transition, CheckpointId = id, FloorGuid = floorGuid, RetryId = retryId };
            destinations.TryGetValue(NetworkServer.localConnection, out message.Position);
            Receive(message);
            foreach (var peer in peers)
                if (peer.isReady && peer != NetworkServer.localConnection)
                {
                    destinations.TryGetValue(peer, out message.Position);
                    peer.Send(message);
                }
        }

        internal static bool AllPlayersReady()
        {
            if (!integrationAvailable || recovery.BlocksBattle) return false;
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
        internal static bool HasRecoveryFailed(long id) => id == retryId && recovery.Status == RetryRecoveryStatus.Failed;
        internal static void FailRecovery()
        {
            if (recovery.Status != RetryRecoveryStatus.Waiting)
                recovery.Begin(++retryId, NetworkServer.connections.Values, Time.realtimeSinceStartupAsDouble);
            recovery.Fail(RetryRecoveryFailure.RestoreFailed);
            receivedRetryId = retryId;
            publishedStatus = RetryRecoveryStatus.Waiting;
        }

        internal static void ReportArrival(long id, bool success = true)
        {
            if (NetworkServer.active) AcceptReceipt(NetworkServer.localConnection, id, success);
            else if (NetworkClient.active && NetworkClient.connection != null)
                NetworkClient.Send(new Arrival { RetryId = id, Success = success });
        }

        private static void AcceptReceipt(NetworkConnectionToClient peer, long id, bool success)
        {
            if (id != retryId || !recovery.IsWaiting(peer)) return;
            if (success) receipts.Add(peer);
            else recovery.Report(id, peer, false);
            SupportLogger.Record("retry_client_receipt", "connection=" + peer.connectionId + " success=" + success,
                success ? "INFO" : "ERROR");
        }

        private static void ConfirmDestinations()
        {
            foreach (var pair in destinations)
                if (receipts.Contains(pair.Key) && recovery.IsWaiting(pair.Key) &&
                    NativeRetryArrival.IsAtDestination(pair.Key.identity?.GetComponent<PlayerAvatar>(), recoveryFloor, pair.Value))
                    recovery.Report(retryId, pair.Key, true);
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
                        " " + NativeRetryArrival.Describe(pair.Key.identity?.GetComponent<PlayerAvatar>(), recoveryFloor, pair.Value));
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
            }
            if (message.Transition == RetryTransition.RecoveryFailed || message.Transition == RetryTransition.RecoveryCompleted)
                if (message.RetryId != receivedRetryId) return;
            if (message.Transition == RetryTransition.RetryFloor ||
                message.Transition == RetryTransition.RetryBoss)
            {
                receivedRetryId = message.RetryId;
                NativeRetryBoss.Begin(message.FloorGuid);
                DefeatRetryClientRestore.Begin(message.FloorGuid, message.RetryId, message.Position);
            }
            else if (message.Transition == RetryTransition.Cancel)
            {
                NativeRetryBoss.Clear();
                DefeatRetryClientRestore.Clear();
            }
            else if (message.Transition == RetryTransition.RecoveryFailed)
            {
                NativeRetryBoss.Clear();
                DefeatRetryClientRestore.RecordFailureContext();
                DefeatRetryClientRestore.Clear();
                NativeRetryFailure.Show(message.Failure);
            }
            // Explicit mapping keeps statistics separate from recovery completion.
            StatisticsRetryTransition statistics;
            switch (message.Transition)
            {
                case RetryTransition.CaptureBoss: statistics = StatisticsRetryTransition.CaptureBoss; break;
                case RetryTransition.RetryBoss: statistics = StatisticsRetryTransition.RetryBoss; break;
                case RetryTransition.RetryFloor: statistics = StatisticsRetryTransition.RetryFloor; break;
                case RetryTransition.Cancel:
                case RetryTransition.RecoveryFailed: statistics = StatisticsRetryTransition.Cancel; break;
                default: return;
            }
            try { controller?.ObserveStatisticsRetry(statistics, message.CheckpointId, message.FloorGuid); }
            catch (Exception exception) { SupportLogger.Failure("retry_statistics_failed", exception); }
        }

        internal static void ObserveTeamDefeat()
        {
            try { controller?.FinishDefeatedEncounter(); }
            catch (Exception exception) { SupportLogger.Failure("retry_statistics_failed", exception); }
        }

        internal static void Shutdown()
        {
            NetworkServer.UnregisterHandler<Hello>();
            NetworkServer.UnregisterHandler<Arrival>();
            NetworkClient.UnregisterHandler<Notification>();
            controller = null;
            peers.Clear();
            ClearArrivals();
            serverRegistered = clientRegistered = false;
            registeredConnection = null;
            DefeatRetryClientRestore.Clear();
            NativeRetryBoss.Clear();
            integrationAvailable = false;
            NativeRetryFailure.Clear();
            if (NetworkServer.active && DungeonManager.Instance != null)
                DungeonManager.Instance.constValueDictionary.Remove(ProtocolKey);
        }

        internal static void StopAfterFeatureFailure()
        {
            integrationAvailable = false;
            bool restoring = DefeatRetryFeature.IsRetrying || recovery.BlocksBattle ||
                DefeatRetryClientRestore.PreserveClientRun || NativeRetryFailure.IsPending;
            try
            {
                if (restoring && NetworkServer.active)
                {
                    FailRecovery();
                    PublishRecoveryResult();
                }
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
