using System.Collections.Generic;
using Mirror;
using SephiriaEnhancements.Combat;
using SephiriaEnhancements.Core;

namespace SephiriaEnhancements.Integration
{
    internal static class StatisticsRetryBridge
    {
        // The native runtime constant table is synchronized and rebuilt on world load.
        // Advertise here rather than persisting protocol state in the player's save.
        private const string ProtocolKey = "SephiriaEnhancements.StatisticsRetryProtocol";
        private static CombatInsightsController controller;
        private static bool serverRegistered, clientRegistered;
        private static NetworkConnectionToServer registeredConnection;
        private static readonly HashSet<NetworkConnectionToClient> peers = new HashSet<NetworkConnectionToClient>();
        private static long nextCheckpointId;

        private struct Hello : NetworkMessage { internal byte Version; }
        private struct Notification : NetworkMessage
        {
            internal StatisticsRetryTransition Transition;
            internal long CheckpointId;
            internal string FloorGuid;
        }

        internal static void Initialize(CombatInsightsController value)
        {
            controller = value;
            Writer<Hello>.write = (writer, message) => writer.WriteByte(message.Version);
            Reader<Hello>.read = reader => new Hello { Version = reader.ReadByte() };
            Writer<Notification>.write = (writer, message) =>
            {
                writer.WriteByte((byte)message.Transition);
                writer.WriteLong(message.CheckpointId);
                writer.WriteString(message.FloorGuid);
            };
            Reader<Notification>.read = reader => new Notification
            {
                Transition = (StatisticsRetryTransition)reader.ReadByte(),
                CheckpointId = reader.ReadLong(), FloorGuid = reader.ReadString()
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
                    serverRegistered = true;
                }
                if (DungeonManager.Instance != null)
                    DungeonManager.Instance.constValueDictionary[ProtocolKey] = 1;
                peers.RemoveWhere(peer => !NetworkServer.connections.TryGetValue(peer.connectionId, out var current) || current != peer);
            }
            else
            {
                serverRegistered = false;
                peers.Clear();
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
                        controller?.ObserveStatisticsRetry(message.Transition, message.CheckpointId, message.FloorGuid);
                });
                clientRegistered = true;
            }
            if (!NetworkServer.active && NetworkClient.ready && NetworkClient.connection != null &&
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
            controller?.ObserveStatisticsRetry(transition, id, floorGuid);
            var message = new Notification { Transition = transition, CheckpointId = id, FloorGuid = floorGuid };
            foreach (var peer in peers)
                if (peer.isReady && peer != NetworkServer.localConnection) peer.Send(message);
        }

        internal static void ObserveTeamDefeat() => controller?.FinishDefeatedEncounter();

        internal static void Shutdown()
        {
            if (NetworkServer.active && DungeonManager.Instance != null)
                DungeonManager.Instance.constValueDictionary.Remove(ProtocolKey);
            NetworkServer.UnregisterHandler<Hello>();
            NetworkClient.UnregisterHandler<Notification>();
            controller = null;
            peers.Clear();
            serverRegistered = clientRegistered = false;
            registeredConnection = null;
        }
    }
}
