using SephiriaEnhancements.Runtime;
using System.Collections.Generic;
using Mirror;
using SephiriaEnhancements.Configuration;
using SephiriaEnhancements.MultiplayerAccess.Presentation;
using UnityEngine;

namespace SephiriaEnhancements.MultiplayerAccess.Integration
{
    internal static class JoiningSupplyBridge
    {
        private const string ProtocolKey = "SephiriaEnhancements.JoiningSuppliesProtocol";
        private struct Hello : NetworkMessage { internal byte Version; }
        private struct Request : NetworkMessage { internal int Revision; }
        private struct MiracleChoice : NetworkMessage { internal int Revision, Instance; internal uint Object; internal string Id; }
        private struct AnvilSelection : NetworkMessage { internal int Revision; internal uint Object; internal string Selection; }
        private struct FacilityChoice : NetworkMessage
        { internal int Revision, SelectionRevision, Weapon, Instance; internal uint Object; internal ItemPosition Position; }
        private struct Status : NetworkMessage
        {
            internal int Revision, Remaining;
            internal bool Prepared, RecordedFromStart;
            internal byte Result;
            internal uint Object;
            internal string Selection;
            internal int SelectionRevision;
        }
        private static readonly Dictionary<NetworkConnectionToClient, Status> peers = new Dictionary<NetworkConnectionToClient, Status>();
        private static readonly Dictionary<NetworkConnectionToClient, float> lastRequest = new Dictionary<NetworkConnectionToClient, float>();
        private static bool serverRegistered, clientRegistered;
        private static NetworkConnectionToServer connection;
        private static Status current;
        private static float requestedAt = -100f;
        private static int announcedRevision = -1;
        private static bool announce;
        private static bool restoreAnvil;
        internal static bool Available => MidRunAdmissionRuntime.IsAvailable && current.Prepared && current.Remaining > 0;
        internal static bool IsCurrent(uint objectId) => objectId != 0 && current.Object == objectId;

        internal static void Initialize()
        {
            current = default; announce = false; restoreAnvil = false; announcedRevision = -1;
            Writer<Hello>.write = (writer, message) => writer.WriteByte(message.Version);
            Reader<Hello>.read = reader => new Hello { Version = reader.ReadByte() };
            Writer<Request>.write = (writer, message) => writer.WriteInt(message.Revision);
            Reader<Request>.read = reader => new Request { Revision = reader.ReadInt() };
            Writer<MiracleChoice>.write = (writer, message) =>
            { writer.WriteInt(message.Revision); writer.WriteUInt(message.Object); writer.WriteString(message.Id); writer.WriteInt(message.Instance); };
            Reader<MiracleChoice>.read = reader => new MiracleChoice
            { Revision = reader.ReadInt(), Object = reader.ReadUInt(), Id = reader.ReadString(), Instance = reader.ReadInt() };
            Writer<AnvilSelection>.write = (writer, message) =>
            { writer.WriteInt(message.Revision); writer.WriteUInt(message.Object); writer.WriteString(message.Selection); };
            Reader<AnvilSelection>.read = reader => new AnvilSelection
            { Revision = reader.ReadInt(), Object = reader.ReadUInt(), Selection = reader.ReadString() };
            Writer<FacilityChoice>.write = (writer, message) =>
            {
                writer.WriteInt(message.Revision); writer.WriteInt(message.SelectionRevision); writer.WriteUInt(message.Object);
                writer.WriteInt(message.Weapon); writer.Write(message.Position); writer.WriteInt(message.Instance);
            };
            Reader<FacilityChoice>.read = reader => new FacilityChoice
            {
                Revision = reader.ReadInt(), SelectionRevision = reader.ReadInt(), Object = reader.ReadUInt(),
                Weapon = reader.ReadInt(), Position = reader.Read<ItemPosition>(), Instance = reader.ReadInt()
            };
            Writer<Status>.write = (writer, message) =>
            {
                writer.WriteInt(message.Revision); writer.WriteInt(message.Remaining);
                writer.WriteBool(message.Prepared); writer.WriteBool(message.RecordedFromStart); writer.WriteByte(message.Result);
                writer.WriteUInt(message.Object);
                writer.WriteString(message.Selection);
                writer.WriteInt(message.SelectionRevision);
            };
            Reader<Status>.read = reader => new Status
            {
                Revision = reader.ReadInt(), Remaining = reader.ReadInt(), Prepared = reader.ReadBool(),
                RecordedFromStart = reader.ReadBool(), Result = reader.ReadByte(), Object = reader.ReadUInt(), Selection = reader.ReadString(),
                SelectionRevision = reader.ReadInt()
            };
        }

        internal static void Shutdown()
        {
            if (serverRegistered)
            {
                NetworkServer.UnregisterHandler<Hello>(); NetworkServer.UnregisterHandler<Request>();
                NetworkServer.UnregisterHandler<MiracleChoice>(); NetworkServer.UnregisterHandler<AnvilSelection>();
                NetworkServer.UnregisterHandler<FacilityChoice>();
            }
            if (clientRegistered) NetworkClient.UnregisterHandler<Status>();
            serverRegistered = false; clientRegistered = false; connection = null;
            current = default; peers.Clear(); lastRequest.Clear(); announce = false; restoreAnvil = false;
            if (NetworkServer.active && DungeonManager.Instance != null) DungeonManager.Instance.constValueDictionary.Remove(ProtocolKey);
        }

        internal static void Tick()
        {
            if (NetworkServer.active)
            {
                if (!serverRegistered)
                {
                    NetworkServer.RegisterHandler<Hello>((peer, hello) => FeatureFailure.Run(FeatureId.MultiplayerAccess, () =>
                    {
                        if (hello.Version == 1) peers[peer] = new Status { Revision = -1 };
                    }));
                    NetworkServer.RegisterHandler<Request>((peer, request) => FeatureFailure.Run(FeatureId.MultiplayerAccess, () =>
                    {
                        if (peers.ContainsKey(peer)) HandleRequest(peer, request.Revision);
                    }));
                    NetworkServer.RegisterHandler<MiracleChoice>((peer, choice) => FeatureFailure.Run(FeatureId.MultiplayerAccess, () =>
                    { if (peers.ContainsKey(peer)) HandleMiracle(peer, choice); }));
                    NetworkServer.RegisterHandler<AnvilSelection>((peer, selection) => FeatureFailure.Run(FeatureId.MultiplayerAccess, () =>
                    {
                        if (peers.ContainsKey(peer)) NativeJoiningSupplies.Instance.SaveAnvil(peer.identity?.GetComponent<PlayerSpawner>(),
                            selection.Revision, selection.Object, selection.Selection);
                    }));
                    NetworkServer.RegisterHandler<FacilityChoice>((peer, choice) => FeatureFailure.Run(FeatureId.MultiplayerAccess, () =>
                    { if (peers.ContainsKey(peer)) HandleFacility(peer, choice); }));
                    serverRegistered = true;
                }
                if (MidRunAdmissionRuntime.IsAvailable && DungeonManager.Instance != null)
                    DungeonManager.Instance.constValueDictionary[ProtocolKey] = 1;
            }
            else { serverRegistered = false; peers.Clear(); lastRequest.Clear(); }
            if (!NetworkClient.active)
            {
                clientRegistered = false; connection = null; current = default; announce = false; announcedRevision = -1;
                return;
            }
            if (!clientRegistered)
            {
                NetworkClient.RegisterHandler<Status>(status => FeatureFailure.Run(FeatureId.MultiplayerAccess, () => Receive(status)));
                clientRegistered = true;
            }
            if (connection != NetworkClient.connection) current = default;
            if (NetworkClient.ready && NetworkClient.connection != null && connection != NetworkClient.connection &&
                DungeonManager.Instance != null && DungeonManager.Instance.constValueDictionary.TryGetValue(ProtocolKey, out int version) && version == 1)
            {
                connection = NetworkClient.connection;
                if (!NetworkServer.active) NetworkClient.Send(new Hello { Version = 1 });
            }
            if (announce && NetworkClient.localPlayer != null &&
                NativeJoiningSupplyRecipes.CanOpen(NetworkClient.localPlayer.GetComponent<PlayerAvatar>()))
            {
                announce = false;
                string message = string.Format(ModLocalization.Get(JoiningSupplyLocalization.Ready), current.Remaining);
                if (!current.RecordedFromStart) message += "\n" + ModLocalization.Get(JoiningSupplyLocalization.MissingHistory);
                UIManager.Instance.GetElement<UI_SystemMessage>().Open(message, 6f);
            }
            if (restoreAnvil && NetworkClient.spawned.TryGetValue(current.Object, out var objectIdentity) &&
                objectIdentity.TryGetComponent<Anvil>(out var anvil))
            {
                JsonUtility.FromJson<NativeJoiningSupplyAnvil>(current.Selection).Restore(anvil);
                restoreAnvil = false;
            }
        }

        internal static void Publish()
        {
            if (NetworkServer.localConnection != null) Receive(Read(NetworkServer.localConnection));
            foreach (var peer in new List<NetworkConnectionToClient>(peers.Keys))
            {
                if (!NetworkServer.connections.TryGetValue(peer.connectionId, out var actual) || actual != peer)
                { peers.Remove(peer); lastRequest.Remove(peer); continue; }
                var value = Read(peer);
                if (value.Equals(peers[peer])) continue;
                peers[peer] = value;
                peer.Send(value);
            }
        }

        private static Status Read(NetworkConnectionToClient peer)
        {
            var runtime = NativeJoiningSupplies.Instance;
            var player = peer.identity?.GetComponent<PlayerSpawner>();
            return new Status { Revision = runtime.Revision, Remaining = runtime.Remaining(player),
                Prepared = runtime.Prepared(player), RecordedFromStart = runtime.RecordedFromStart(player), Object = runtime.CurrentObject(player),
                Selection = runtime.ClientSelection(player), SelectionRevision = runtime.SelectionRevision(player),
                Result = runtime.HasFailure(player) ? (byte)3 : (byte)0 };
        }

        internal static void Claim()
        {
            if (!Available || Time.unscaledTime - requestedAt < 1f) return;
            requestedAt = Time.unscaledTime;
            if (NetworkServer.active) HandleRequest(NetworkServer.localConnection, current.Revision);
            else if (NetworkClient.ready) NetworkClient.Send(new Request { Revision = current.Revision });
        }

        private static void HandleRequest(NetworkConnectionToClient peer, int revision)
        {
            if (peer == null || lastRequest.TryGetValue(peer, out float time) && Time.unscaledTime - time < 1f) return;
            lastRequest[peer] = Time.unscaledTime;
            bool success = NativeJoiningSupplies.Instance.Request(peer.identity?.GetComponent<PlayerSpawner>(), revision);
            var status = Read(peer);
            if (status.Result != 3) status.Result = success ? (status.Object == 0 ? (byte)4 : (byte)1) : (byte)2;
            if (peer == NetworkServer.localConnection) Receive(status); else peer.Send(status);
        }

        private static void Receive(Status status)
        {
            if (current.Equals(status)) return;
            restoreAnvil = !string.IsNullOrEmpty(status.Selection) && (restoreAnvil || current.Object != status.Object || current.Selection != status.Selection);
            current = status;
            if (status.Prepared && status.Remaining > 0 && announcedRevision != status.Revision)
            { announcedRevision = status.Revision; announce = true; }
            if (status.Result == 0 || UIManager.Instance == null) return;
            string key = status.Result == 1 ? JoiningSupplyLocalization.Placed :
                status.Result == 3 ? JoiningSupplyLocalization.Failed : status.Result == 4 ? JoiningSupplyLocalization.Claimed :
                status.Result == 5 ? JoiningSupplyLocalization.ChoiceChanged : JoiningSupplyLocalization.Unavailable;
            UIManager.Instance.GetElement<UI_SystemMessage>().Open(ModLocalization.Get(key), 4f);
        }

        internal static void SelectMiracle(uint objectId, string id, int instance)
        {
            var choice = new MiracleChoice { Revision = current.Revision, Object = objectId, Id = id, Instance = instance };
            if (NetworkServer.active) HandleMiracle(NetworkServer.localConnection, choice);
            else if (NetworkClient.ready) NetworkClient.Send(choice);
        }

        private static void HandleMiracle(NetworkConnectionToClient peer, MiracleChoice choice)
        {
            bool success = NativeJoiningSupplies.Instance.SelectMiracle(peer.identity?.GetComponent<PlayerSpawner>(),
                choice.Revision, choice.Object, choice.Id, choice.Instance);
            var status = Read(peer);
            if (!success && status.Result != 3) status.Result = 5;
            if (peer == NetworkServer.localConnection) Receive(status); else peer.Send(status);
        }

        internal static void SaveAnvil(uint objectId, string selection)
        {
            if (NetworkServer.active) NativeJoiningSupplies.Instance.SaveAnvil(NetworkClient.localPlayer?.GetComponent<PlayerSpawner>(),
                current.Revision, objectId, selection);
            else if (NetworkClient.ready) NetworkClient.Send(new AnvilSelection
                { Revision = current.Revision, Object = objectId, Selection = selection });
        }

        internal static void SelectFacility(uint objectId, int weapon, ItemPosition position, int instance)
        {
            var choice = new FacilityChoice { Revision = current.Revision, SelectionRevision = current.SelectionRevision,
                Object = objectId, Weapon = weapon, Position = position, Instance = instance };
            if (NetworkServer.active) HandleFacility(NetworkServer.localConnection, choice);
            else if (NetworkClient.ready) NetworkClient.Send(choice);
        }

        private static void HandleFacility(NetworkConnectionToClient peer, FacilityChoice choice)
        {
            bool success = NativeJoiningSupplies.Instance.SelectFacility(peer.identity?.GetComponent<PlayerSpawner>(),
                choice.Revision, choice.SelectionRevision, choice.Object, choice.Weapon, choice.Position, choice.Instance);
            var status = Read(peer);
            if (!success && status.Result != 3) status.Result = 5;
            if (peer == NetworkServer.localConnection) Receive(status); else peer.Send(status);
        }
    }
}
