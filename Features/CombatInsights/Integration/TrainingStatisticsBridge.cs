using System;
using System.Collections.Generic;
using Mirror;
using SephiriaEnhancements.Runtime;

namespace SephiriaEnhancements.Integration
{
    internal enum TrainingStatisticsStatus : byte { Ready, Unavailable, TooLarge }

    internal static class TrainingStatisticsBridge
    {
        private const string ProtocolKey = "SephiriaEnhancements.TrainingStatisticsProtocol";
        private const byte Version = 1;
        private struct Request : NetworkMessage
        {
            internal long Id;
            internal uint Player;
            internal int World;
            internal string Floor;
            internal bool Clear;
            internal long Generation;
        }
        internal struct Snapshot : NetworkMessage
        {
            internal long RequestId;
            internal uint Player;
            internal int World;
            internal string Floor;
            internal TrainingStatisticsStatus Status;
            internal Dictionary<DamageKey, float> Damage;
            internal long Generation;
        }

        private static bool serverRegistered, clientRegistered;
        private static NetworkConnectionToServer connection;
        private static readonly Dictionary<NetworkConnectionToClient, long> requests = new Dictionary<NetworkConnectionToClient, long>();
        private static readonly List<NetworkConnectionToClient> removed = new List<NetworkConnectionToClient>();
        private static long nextRequest, minimumRequest, acceptedRequest;
        private static uint selectedPlayer;
        private static float receivedAt;
        internal static Snapshot? Latest { get; private set; }
        internal static bool HasRecentReply => Latest.HasValue && UnityEngine.Time.unscaledTime - receivedAt < 3f;

        internal static bool Supported => TrainingDamageStatistics.Available &&
            (NetworkServer.active || NetworkClient.ready && DungeonManager.Instance != null &&
                DungeonManager.Instance.constValueDictionary.TryGetValue(ProtocolKey, out int version) && version == Version);

        internal static void Initialize()
        {
            Writer<Request>.write = (writer, message) =>
            {
                writer.WriteLong(message.Id); writer.WriteUInt(message.Player);
                writer.WriteInt(message.World); writer.WriteString(message.Floor); writer.WriteBool(message.Clear);
                writer.WriteLong(message.Generation);
            };
            Reader<Request>.read = reader => new Request
            {
                Id = reader.ReadLong(), Player = reader.ReadUInt(), World = reader.ReadInt(),
                Floor = reader.ReadString(), Clear = reader.ReadBool(), Generation = reader.ReadLong()
            };
            Writer<Snapshot>.write = (writer, message) =>
            {
                writer.WriteLong(message.RequestId); writer.WriteUInt(message.Player);
                writer.WriteInt(message.World); writer.WriteString(message.Floor); writer.WriteByte((byte)message.Status);
                writer.WriteLong(message.Generation);
                writer.WriteInt(message.Damage?.Count ?? 0);
                if (message.Damage == null) return;
                foreach (var pair in message.Damage)
                {
                    writer.WriteString(pair.Key.Id); writer.WriteInt((int)pair.Key.ElementalType); writer.WriteFloat(pair.Value);
                }
            };
            Reader<Snapshot>.read = reader =>
            {
                var result = new Snapshot
                {
                    RequestId = reader.ReadLong(), Player = reader.ReadUInt(), World = reader.ReadInt(),
                    Floor = reader.ReadString(), Status = (TrainingStatisticsStatus)reader.ReadByte(),
                    Generation = reader.ReadLong(),
                    Damage = new Dictionary<DamageKey, float>()
                };
                int count = reader.ReadInt();
                if (count < 0 || count > reader.Remaining / 10) throw new FormatException("Invalid training statistics length.");
                for (int index = 0; index < count; index++)
                {
                    var key = new DamageKey(reader.ReadString(), (EDamageElementalType)reader.ReadInt());
                    float damage = reader.ReadFloat();
                    if (float.IsNaN(damage) || float.IsInfinity(damage) || damage < 0f)
                        throw new FormatException("Invalid training damage.");
                    result.Damage.Add(key, damage);
                }
                return result;
            };
            Tick();
        }

        internal static void Tick()
        {
            if (NetworkServer.active)
            {
                if (!serverRegistered)
                {
                    NetworkServer.RegisterHandler<Request>((peer, request) =>
                        FeatureFailure.Run(FeatureId.CombatInsights, () => ReceiveRequest(peer, request)));
                    serverRegistered = true;
                }
                if (DungeonManager.Instance != null)
                {
                    if (TrainingDamageStatistics.Available) DungeonManager.Instance.constValueDictionary[ProtocolKey] = Version;
                    else DungeonManager.Instance.constValueDictionary.Remove(ProtocolKey);
                }
                removed.Clear();
                foreach (var peer in requests.Keys)
                    if (!NetworkServer.connections.TryGetValue(peer.connectionId, out var active) || active != peer) removed.Add(peer);
                foreach (var peer in removed) requests.Remove(peer);
            }
            else { serverRegistered = false; requests.Clear(); }

            if (!NetworkClient.active) { clientRegistered = false; connection = null; ResetView(); return; }
            if (connection != NetworkClient.connection)
            {
                connection = NetworkClient.connection;
                ResetView();
            }
            if (!clientRegistered)
            {
                NetworkClient.RegisterHandler<Snapshot>(message =>
                {
                    if (!NetworkServer.active)
                        FeatureFailure.Run(FeatureId.CombatInsights, () => Accept(message));
                });
                clientRegistered = true;
            }
        }

        internal static void ResetView()
        {
            Latest = null;
            selectedPlayer = 0;
            minimumRequest = nextRequest + 1;
        }

        internal static bool Query(PlayerAvatar player, bool clear = false)
        {
            Tick();
            if (!Supported || !TrainingDamageStatistics.Ready(player) || DungeonManager.Instance == null) return false;
            var local = LocalPlayerResolver.Resolve();
            if (!TrainingDamageStatistics.Ready(local) || player.NetworkcurrentFloorGuid != local.NetworkcurrentFloorGuid ||
                clear && player != local) return false;
            if (clear && !CanClear(player)) return false;
            long generation = clear ? Latest.Value.Generation : 0;
            if (selectedPlayer != player.netId || clear)
            {
                ResetView();
                selectedPlayer = player.netId;
            }
            var request = new Request
            {
                Id = ++nextRequest, Player = player.netId, World = DungeonManager.Instance.sessionSerial,
                Floor = player.NetworkcurrentFloorGuid, Clear = clear, Generation = generation
            };
            if (NetworkServer.active) Accept(Capture(request, player));
            else NetworkClient.Send(request);
            return true;
        }

        internal static bool CanClear(PlayerAvatar player) => Supported && TrainingDamageStatistics.Ready(player) &&
            player == LocalPlayerResolver.Resolve() && HasRecentReply && Latest.Value.Status == TrainingStatisticsStatus.Ready &&
            Latest.Value.Damage != null && Latest.Value.Damage.Count > 0 &&
            Latest.Value.Player == player.netId && Latest.Value.Floor == player.NetworkcurrentFloorGuid &&
            DungeonManager.Instance != null && Latest.Value.World == DungeonManager.Instance.sessionSerial;

        private static void ReceiveRequest(NetworkConnectionToClient peer, Request request)
        {
            // Requests are authenticated by Mirror. A peer can inspect an observed
            // same-floor player, but can only clear its own authoritative record.
            if (!NetworkServer.active || !TrainingDamageStatistics.Available || !peer.isReady || request.Id <= 0) return;
            if (requests.TryGetValue(peer, out long previous) && request.Id <= previous) return;
            requests[peer] = request.Id;
            PlayerAvatar local = peer.identity?.GetComponent<PlayerAvatar>();
            PlayerAvatar target = NetworkServer.spawned.TryGetValue(request.Player, out var identity)
                ? identity.GetComponent<PlayerAvatar>() : null;
            bool valid = TrainingDamageStatistics.Ready(local) && TrainingDamageStatistics.Ready(target) &&
                DungeonManager.Instance != null && request.World == DungeonManager.Instance.sessionSerial &&
                request.Floor == local.NetworkcurrentFloorGuid && request.Floor == target.NetworkcurrentFloorGuid &&
                (target == local || identity.observers.ContainsKey(peer.connectionId)) && (!request.Clear || target == local);
            Snapshot snapshot = Capture(request, valid ? target : null);
            using (var writer = NetworkWriterPool.Get())
            {
                Writer<Snapshot>.write(writer, snapshot);
                if (writer.Position > NetworkMessages.MaxContentSize(Channels.Reliable))
                {
                    snapshot.Status = TrainingStatisticsStatus.TooLarge;
                    snapshot.Damage = null;
                }
            }
            // Reply only to a compatible client which explicitly requested data.
            peer.Send(snapshot);
        }

        private static Snapshot Capture(Request request, PlayerAvatar target)
        {
            long generation = target != null ? TrainingDamageStatistics.Instance.GetGeneration(target) : 0;
            if (request.Clear)
            {
                // A delayed clear must not erase training recorded after travel,
                // a world rebuild or replacement of an avatar with the same netId.
                if (generation == 0 || request.Generation != generation) target = null;
                else
                {
                    TrainingDamageStatistics.Instance.Clear(target);
                    generation = TrainingDamageStatistics.Instance.GetGeneration(target);
                }
            }
            var damage = target != null ? TrainingDamageStatistics.Instance.Capture(target) : null;
            return new Snapshot
            {
                RequestId = request.Id, Player = request.Player, World = request.World, Floor = request.Floor,
                Status = damage == null ? TrainingStatisticsStatus.Unavailable : TrainingStatisticsStatus.Ready, Damage = damage,
                Generation = generation
            };
        }

        private static void Accept(Snapshot snapshot)
        {
            PlayerAvatar local = LocalPlayerResolver.Resolve();
            if (!Supported || snapshot.RequestId < minimumRequest || snapshot.RequestId <= acceptedRequest ||
                snapshot.RequestId > nextRequest || snapshot.Player != selectedPlayer ||
                DungeonManager.Instance == null || snapshot.World != DungeonManager.Instance.sessionSerial ||
                !TrainingDamageStatistics.Ready(local) || snapshot.Floor != local.NetworkcurrentFloorGuid) return;
            acceptedRequest = snapshot.RequestId;
            receivedAt = UnityEngine.Time.unscaledTime;
            Latest = snapshot; // Complete replacement; never add a snapshot to a previous total.
        }

        internal static void Shutdown()
        {
            NetworkServer.UnregisterHandler<Request>();
            NetworkClient.UnregisterHandler<Snapshot>();
            serverRegistered = clientRegistered = false;
            requests.Clear(); connection = null; ResetView();
            if (NetworkServer.active && DungeonManager.Instance != null)
                DungeonManager.Instance.constValueDictionary.Remove(ProtocolKey);
        }
    }
}
