using System.Reflection;
using HeathenEngineering.SteamworksIntegration;
using UnityEngine;

namespace SephiriaEnhancements.Runtime.GameBridge
{
    internal static class NativeLobbyAccess
    {
        private const BindingFlags InstanceFields = BindingFlags.Instance |
            BindingFlags.Public | BindingFlags.NonPublic;
        private const BindingFlags StaticFields = BindingFlags.Static |
            BindingFlags.Public | BindingFlags.NonPublic;

        // SteamInvitation private members and SteamManager are native integration
        // contracts. Feature code consumes only lobby presence or metadata access.
        private static readonly FieldInfo SteamInvitationInstance =
            typeof(SteamInvitation).GetField("instance", StaticFields);
        private static readonly FieldInfo SteamInvitationLobbyManager =
            typeof(SteamInvitation).GetField("lobbyManager", InstanceFields);
        private static readonly PropertyInfo SteamHasLobby =
            SteamInvitationLobbyManager?.FieldType.GetProperty("HasLobby",
                InstanceFields);

        internal static bool TryReadPresence(out bool hasLobby)
        {
            bool providerAvailable = false;
            hasLobby = false;

            try
            {
                EOSLobbyManager eos = EOSLobbyManager.Instance;
                if (eos != null)
                {
                    providerAvailable = true;
                    hasLobby = eos.HasLobby;
                }
            }
            catch
            {
            }

            try
            {
                object invitation = SteamInvitationInstance?.GetValue(null);
                object lobbyManager = invitation == null
                    ? null
                    : SteamInvitationLobbyManager?.GetValue(invitation);
                if (lobbyManager != null && SteamHasLobby != null)
                {
                    providerAvailable = true;
                    hasLobby |= (bool)SteamHasLobby.GetValue(lobbyManager);
                }
            }
            catch
            {
            }

            return providerAvailable;
        }

        internal static bool TryOpenOwnedSteamRoomForJoin()
        {
            if (!TryGetSteamLobbyManager(out LobbyManager lobbyManager) ||
                !lobbyManager.HasLobby || !lobbyManager.Lobby.IsOwner)
            {
                return false;
            }

            // "pw" is the game's native Steam-room joinability metadata key.
            var lobby = lobbyManager.Lobby;
            lobby["pw"] = "open";
            return true;
        }

        internal static bool TryWriteOwnedSteamMetadata(string key, string value)
        {
            if (!TryGetSteamLobbyManager(out LobbyManager lobby) ||
                !lobby.HasLobby || !lobby.IsPlayerOwner)
            {
                return false;
            }

            lobby[key] = value;
            return true;
        }

        internal static bool TryReadSteamMetadata(string key, out string value)
        {
            value = string.Empty;
            if (!TryGetSteamLobbyManager(out LobbyManager lobby) || !lobby.HasLobby)
                return false;

            value = lobby[key];
            return true;
        }

        private static bool TryGetSteamLobbyManager(out LobbyManager lobby)
        {
            GameObject steamManager = SingletonObject.Find("SteamManager");
            lobby = null;
            return steamManager != null &&
                steamManager.TryGetComponent<LobbyManager>(out lobby);
        }
    }
}
