using System.Collections.Generic;
using Mirror;
using SephiriaEnhancements.Integration;
using SephiriaEnhancements.Runtime;
using UnityEngine;

namespace SephiriaEnhancements.MultiplayerRules.Integration
{
    // Negotiate through replicated world data before sending Mod messages.
    // Unmodified peers receive only the native chat RPC.
    internal static class MultiplayerRulesBridge
    {
        private const string ProtocolKey = "SephiriaEnhancements.MultiplayerRulesProtocol";
        private const string WorldRevisionKey = "SephiriaEnhancements.MultiplayerRulesWorldRevision";
        private const byte ProtocolVersion = 1;
        private struct Hello : NetworkMessage { internal uint World; internal int Session, WorldRevision; internal byte Version; }
        private struct StateMessage : NetworkMessage
        {
            internal uint World;
            internal int Session, WorldRevision, Revision, Participants, Changes;
            internal bool ExplorationStarted, AllowExternalStacking;
            internal byte Availability, Notice;
            internal string Rules;
        }
        private sealed class Peer
        {
            internal NetworkIdentity Player;
            internal bool Negotiated;
            internal int Revision = -1;
            internal float ReadyAt;
            internal MultiplayerRulesNotice Notice = MultiplayerRulesNotice.Summary;
            internal int Changes;
        }

        private static readonly Dictionary<NetworkConnectionToClient, Peer> peers = new();
        private static bool serverRegistered, clientRegistered;
        private static DungeonManager world;
        private static int session, revision;
        private static int worldRevision, nextWorldRevision;
        private static NetworkConnectionToServer connection;
        private static NetworkIdentity helloPlayer;
        private static MultiplayerRulesState published, received;
        private static readonly Queue<StateMessage> pendingStates = new();
        private static int receivedRevision = -1;
        private static MultiplayerRulesNotice localNotice;
        private static int localChanges;
        private static float nextPublish;

        internal static MultiplayerRulesState Received => received;
        internal static bool HostSupportsRules => DungeonManager.Instance != null &&
            DungeonManager.Instance.constValueDictionary.TryGetValue(ProtocolKey, out int version) && version == ProtocolVersion &&
            DungeonManager.Instance.constValueDictionary.TryGetValue(WorldRevisionKey, out int value) && value > 0;

        internal static void Initialize()
        {
            Writer<Hello>.write = (w, m) => { w.WriteUInt(m.World); w.WriteInt(m.Session); w.WriteInt(m.WorldRevision); w.WriteByte(m.Version); };
            Reader<Hello>.read = r => new Hello { World = r.ReadUInt(), Session = r.ReadInt(), WorldRevision = r.ReadInt(), Version = r.ReadByte() };
            Writer<StateMessage>.write = (w, m) =>
            {
                w.WriteUInt(m.World); w.WriteInt(m.Session); w.WriteInt(m.WorldRevision); w.WriteInt(m.Revision); w.WriteInt(m.Participants);
                w.WriteBool(m.ExplorationStarted); w.WriteBool(m.AllowExternalStacking); w.WriteByte(m.Availability);
                w.WriteString(m.Rules); w.WriteByte(m.Notice); w.WriteInt(m.Changes);
            };
            Reader<StateMessage>.read = r => new StateMessage
            {
                World = r.ReadUInt(), Session = r.ReadInt(), WorldRevision = r.ReadInt(), Revision = r.ReadInt(), Participants = r.ReadInt(),
                ExplorationStarted = r.ReadBool(), AllowExternalStacking = r.ReadBool(), Availability = r.ReadByte(),
                Rules = r.ReadString(), Notice = r.ReadByte(), Changes = r.ReadInt()
            };
        }

        internal static void ObserveWorld()
        {
            var current = DungeonManager.Instance;
            int advertised = current != null && current.constValueDictionary.TryGetValue(WorldRevisionKey, out int value) ? value : 0;
            if (ReferenceEquals(world, current) && (current == null || session == current.sessionSerial) &&
                worldRevision == advertised) return;
            world = current;
            session = current == null ? 0 : current.sessionSerial;
            worldRevision = NetworkServer.active && current != null ? ++nextWorldRevision : advertised;
            if (NetworkServer.active && current != null) current.constValueDictionary[WorldRevisionKey] = worldRevision;
            peers.Clear(); published = received = null; pendingStates.Clear();
            revision = 0; receivedRevision = -1; helloPlayer = null;
            localNotice = MultiplayerRulesNotice.None;
            if (!NetworkServer.active) MultiplayerRulesController.ClearHostRulesForClientDisplay();
        }

        internal static void Tick()
        {
            ObserveWorld();
            if (NetworkServer.active)
            {
                if (!serverRegistered)
                {
                    NetworkServer.RegisterHandler<Hello>((peer, hello) => FeatureFailure.Run(FeatureId.MultiplayerRules, () =>
                    {
                        if (world == null || hello.World != world.netId || hello.Session != world.sessionSerial ||
                            hello.WorldRevision != worldRevision || hello.Version != ProtocolVersion || peer.identity == null) return;
                        if (!peers.TryGetValue(peer, out var known) || known.Player != peer.identity)
                            peers[peer] = known = NewPeer(peer);
                        known.Negotiated = true;
                        known.Revision = -1;
                    }));
                    serverRegistered = true;
                }
                if (world != null && (!world.constValueDictionary.TryGetValue(ProtocolKey, out int value) || value != ProtocolVersion))
                    world.constValueDictionary[ProtocolKey] = ProtocolVersion;
                if (Time.unscaledTime >= nextPublish)
                {
                    nextPublish = Time.unscaledTime + .2f;
                    Publish();
                }
            }
            else { serverRegistered = false; peers.Clear(); }

            if (!NetworkClient.active)
            {
                clientRegistered = false; connection = null; helloPlayer = null;
                received = null; pendingStates.Clear(); localNotice = MultiplayerRulesNotice.None;
                MultiplayerRulesController.ClearHostRulesForClientDisplay();
                return;
            }
            if (!clientRegistered)
            {
                NetworkClient.RegisterHandler<StateMessage>(message =>
                {
                    if (!NetworkServer.active) pendingStates.Enqueue(message);
                });
                clientRegistered = true;
            }
            if (connection != NetworkClient.connection)
            {
                connection = NetworkClient.connection; helloPlayer = null;
                received = null; receivedRevision = -1; pendingStates.Clear();
                if (!NetworkServer.active)
                {
                    localNotice = MultiplayerRulesNotice.None;
                    MultiplayerRulesController.ClearHostRulesForClientDisplay();
                }
            }
            if (!NetworkServer.active && NetworkClient.ready && connection != null && HostSupportsRules &&
                NetworkClient.localPlayer != null && helloPlayer != NetworkClient.localPlayer)
            {
                helloPlayer = NetworkClient.localPlayer;
                receivedRevision = -1;
                NetworkClient.Send(new Hello { World = world.netId, Session = session, WorldRevision = worldRevision, Version = ProtocolVersion });
            }
            if (!NetworkServer.active && !HostSupportsRules)
            {
                received = null; pendingStates.Clear(); helloPlayer = null;
                localNotice = MultiplayerRulesNotice.None;
                MultiplayerRulesController.ClearHostRulesForClientDisplay();
            }
            while (pendingStates.Count != 0)
            {
                var next = pendingStates.Dequeue();
                if (world == null || next.World != world.netId || next.Session != session || next.WorldRevision != worldRevision) continue;
                if (next.Revision > receivedRevision && next.Participants >= 1 && next.Changes >= 0 &&
                    next.Availability <= (byte)MultiplayerRulesAvailability.Unavailable && next.Notice <= (byte)MultiplayerRulesNotice.Summary &&
                    ActiveExplorationRulesPayloadCodec.TryDecode(next.Rules, out var rules))
                {
                    receivedRevision = next.Revision;
                    received = new MultiplayerRulesState(rules, next.Participants, next.ExplorationStarted,
                        (MultiplayerRulesAvailability)next.Availability, next.AllowExternalStacking);
                    if (received.ExplorationStarted) MultiplayerRulesController.ApplyHostRulesForClientDisplay(received.Rules);
                    else MultiplayerRulesController.ClearHostRulesForClientDisplay();
                    if (next.Notice != 0) { localNotice = (MultiplayerRulesNotice)next.Notice; localChanges = next.Changes; }
                }
            }
            var state = NetworkServer.active ? published : received;
            if (state != null && localNotice != MultiplayerRulesNotice.None &&
                NativeModNotifications.Chat(() => NativeRulesBroadcast.Describe(state, localNotice, localChanges)))
                localNotice = MultiplayerRulesNotice.None;
        }

        internal static void Publish()
        {
            if (!NetworkServer.active || DungeonManager.Instance == null) return;
            ObserveWorld();
            var next = MultiplayerRulesController.ReadServerState();
            bool teamChanged = published != null && published.Participants != next.Participants;
            if (!next.IsEquivalentTo(published)) { published = next; revision++; }
            if (teamChanged) QueueNotice(MultiplayerRulesNotice.TeamChanged, 0);

            foreach (var peer in new List<NetworkConnectionToClient>(peers.Keys))
                if (!NetworkServer.connections.TryGetValue(peer.connectionId, out var actual) || actual != peer)
                    peers.Remove(peer);
            foreach (var peer in NetworkServer.connections.Values)
            {
                if (peer == NetworkServer.localConnection || !peer.isReady || peer.identity == null) continue;
                if (!peers.TryGetValue(peer, out var known) || known.Player != peer.identity)
                    peers[peer] = known = NewPeer(peer);
                var player = peer.identity.GetComponent<PlayerAvatar>();
                if (player == null || player.loadingScreenType != -1) continue;
                if (known.Negotiated && (known.Revision != revision || known.Notice != MultiplayerRulesNotice.None))
                {
                    peer.Send(new StateMessage
                    {
                        World = world.netId, Session = session, WorldRevision = worldRevision, Revision = revision, Participants = published.Participants,
                        ExplorationStarted = published.ExplorationStarted, Availability = (byte)published.Availability,
                        AllowExternalStacking = published.AllowExternalStacking,
                        Rules = ActiveExplorationRulesPayloadCodec.Encode(published.Rules), Notice = (byte)known.Notice, Changes = known.Changes
                    });
                    known.Revision = revision;
                    known.Notice = MultiplayerRulesNotice.None;
                }
                else if (!known.Negotiated && known.Notice != MultiplayerRulesNotice.None && Time.unscaledTime >= known.ReadyAt)
                {
                    NativeRulesBroadcast.SendNative(peer, published, known.Notice, known.Changes);
                    known.Notice = MultiplayerRulesNotice.None;
                }
            }
        }

        private static Peer NewPeer(NetworkConnectionToClient peer) => new Peer
        { Player = peer.identity, ReadyAt = Time.unscaledTime + 1f };

        internal static void Announce(MultiplayerRulesNotice notice, int changes = 0)
        {
            if (!NetworkServer.active) return;
            Publish();
            QueueNotice(notice, changes);
            Publish();
        }

        private static void QueueNotice(MultiplayerRulesNotice notice, int changes)
        {
            revision++;
            localNotice = notice; localChanges = changes;
            foreach (var known in peers.Values) { known.Notice = notice; known.Changes = changes; }
        }

        internal static void Shutdown()
        {
            if (serverRegistered) NetworkServer.UnregisterHandler<Hello>();
            if (clientRegistered) NetworkClient.UnregisterHandler<StateMessage>();
            if (NetworkServer.active && DungeonManager.Instance != null) DungeonManager.Instance.constValueDictionary.Remove(ProtocolKey);
            if (NetworkServer.active && DungeonManager.Instance != null) DungeonManager.Instance.constValueDictionary.Remove(WorldRevisionKey);
            serverRegistered = clientRegistered = false;
            peers.Clear(); world = null; connection = null; helloPlayer = null;
            published = received = null; pendingStates.Clear(); localNotice = MultiplayerRulesNotice.None;
        }
    }
}
