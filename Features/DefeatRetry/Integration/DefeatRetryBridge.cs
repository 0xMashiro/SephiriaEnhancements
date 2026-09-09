using System.Collections.Generic;
using Mirror;
using SephiriaEnhancements.Combat;
using SephiriaEnhancements.Core;
using SephiriaEnhancements.DefeatRetry;
using SephiriaEnhancements.Diagnostics;
using System;

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
        private static readonly HashSet<NetworkConnectionToClient> awaitingArrival = new HashSet<NetworkConnectionToClient>();

        internal static void SetIntegrationAvailable(bool available) => integrationAvailable = available;

        private struct Hello : NetworkMessage { internal byte Version; }
        private struct Arrival : NetworkMessage { internal long RetryId; internal bool Success; }
        private struct Notification : NetworkMessage
        {
            internal StatisticsRetryTransition Transition;
            internal long CheckpointId;
            internal string FloorGuid;
            internal long RetryId;
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
            };
            Reader<Notification>.read = reader => new Notification
            {
                Transition = (StatisticsRetryTransition)reader.ReadByte(),
                CheckpointId = reader.ReadLong(), FloorGuid = reader.ReadString(), RetryId = reader.ReadLong()
            };
            Tick();
        }

        internal static void Tick()
        {
            if (NetworkServer.active)
            {
                if (!serverRegistered)
                {
                    NetworkServer.RegisterHandler<Hello>((connection, message) =>
                    {
                        if (message.Version == 1) peers.Add(connection);
                    });
                    NetworkServer.RegisterHandler<Arrival>((connection, message) =>
                    {
                        if (message.RetryId == retryId && awaitingArrival.Remove(connection))
                            SupportLogger.Record("retry_remote_arrival", "connection=" + connection.connectionId +
                                " success=" + message.Success, message.Success ? "INFO" : "ERROR");
                    });
                    serverRegistered = true;
                }
                if (integrationAvailable && DungeonManager.Instance != null)
                    DungeonManager.Instance.constValueDictionary[ProtocolKey] = 1;
                peers.RemoveWhere(peer => !NetworkServer.connections.TryGetValue(peer.connectionId, out var current) || current != peer);
                awaitingArrival.RemoveWhere(peer => !NetworkServer.connections.TryGetValue(peer.connectionId, out var current) || current != peer);
            }
            else
            {
                serverRegistered = false;
                peers.Clear();
                awaitingArrival.Clear();
            }
            if (!NetworkClient.active)
            {
                clientRegistered = false;
                registeredConnection = null;
                return;
            }
            if (!clientRegistered)
            {
                NetworkClient.RegisterHandler<Notification>(message =>
                {
                    if (!NetworkServer.active && message.Transition <= StatisticsRetryTransition.Cancel)
                        Receive(message);
                });
                clientRegistered = true;
            }
            if (integrationAvailable && !NetworkServer.active && NetworkClient.ready && NetworkClient.connection != null &&
                registeredConnection != NetworkClient.connection && DungeonManager.Instance != null &&
                DungeonManager.Instance.constValueDictionary.TryGetValue(ProtocolKey, out int version) && version == 1)
            {
                NetworkClient.Send(new Hello { Version = 1 });
                registeredConnection = NetworkClient.connection;
            }
        }

        internal static long CaptureBoss(string floorGuid)
        {
            long id = ++nextCheckpointId;
            Publish(StatisticsRetryTransition.CaptureBoss, id, floorGuid);
            return id;
        }

        internal static void Publish(StatisticsRetryTransition transition, long id, string floorGuid)
        {
            if (!NetworkServer.active) return;
            Tick();
            if (transition == StatisticsRetryTransition.RetryFloor || transition == StatisticsRetryTransition.RetryBoss)
            {
                retryId++;
                awaitingArrival.Clear();
                foreach (var peer in NetworkServer.connections.Values) awaitingArrival.Add(peer);
            }
            if (transition == StatisticsRetryTransition.Cancel) awaitingArrival.Clear();
            var message = new Notification { Transition = transition, CheckpointId = id, FloorGuid = floorGuid, RetryId = retryId };
            Receive(message);
            foreach (var peer in peers)
                if (peer.isReady && peer != NetworkServer.localConnection) peer.Send(message);
        }

        internal static bool AllPlayersReady()
        {
            if (!integrationAvailable || awaitingArrival.Count != 0) return false;
            foreach (var connection in NetworkServer.connections.Values)
                if (connection != NetworkServer.localConnection &&
                    (connection == null || !connection.isReady || !peers.Contains(connection))) return false;
            return true;
        }

        internal static void CancelPlayer(PlayerAvatar player)
        {
            awaitingArrival.Remove(player.connectionToClient);
            var message = new Notification { Transition = StatisticsRetryTransition.Cancel };
            if (LocalPlayerResolver.IsLocal(player)) Receive(message);
            else if (player.connectionToClient != null && peers.Contains(player.connectionToClient))
                player.connectionToClient.Send(message);
        }

        internal static bool IsAwaitingArrival(PlayerAvatar player) =>
            player != null && player.connectionToClient != null && awaitingArrival.Contains(player.connectionToClient);

        internal static void ClearArrivals() => awaitingArrival.Clear();
        internal static bool HasPendingArrivals => awaitingArrival.Count != 0;

        internal static void ReportArrival(long id, bool success = true)
        {
            if (NetworkServer.active)
            {
                if (id == retryId) awaitingArrival.Remove(NetworkServer.localConnection);
            }
            else if (NetworkClient.active && NetworkClient.connection != null)
                NetworkClient.Send(new Arrival { RetryId = id, Success = success });
        }

        private static void Receive(Notification message)
        {
            if (message.Transition == StatisticsRetryTransition.RetryFloor ||
                message.Transition == StatisticsRetryTransition.RetryBoss)
            {
                NativeRetryBoss.Begin(message.FloorGuid);
                DefeatRetryClientRestore.Begin(message.FloorGuid, message.RetryId);
            }
            else if (message.Transition == StatisticsRetryTransition.Cancel)
            {
                NativeRetryBoss.Clear();
                DefeatRetryClientRestore.Clear();
            }
            try { controller?.ObserveStatisticsRetry(message.Transition, message.CheckpointId, message.FloorGuid); }
            catch (Exception exception) { SupportLogger.Failure("retry_statistics_failed", exception); }
        }

        internal static void ObserveTeamDefeat()
        {
            try { controller?.FinishDefeatedEncounter(); }
            catch (Exception exception) { SupportLogger.Failure("retry_statistics_failed", exception); }
        }

        internal static void Shutdown()
        {
            if (NetworkServer.active && DungeonManager.Instance != null)
                DungeonManager.Instance.constValueDictionary.Remove(ProtocolKey);
            NetworkServer.UnregisterHandler<Hello>();
            NetworkServer.UnregisterHandler<Arrival>();
            NetworkClient.UnregisterHandler<Notification>();
            controller = null;
            peers.Clear();
            awaitingArrival.Clear();
            serverRegistered = clientRegistered = false;
            registeredConnection = null;
            DefeatRetryClientRestore.Clear();
            NativeRetryBoss.Clear();
            integrationAvailable = false;
        }
    }
}
