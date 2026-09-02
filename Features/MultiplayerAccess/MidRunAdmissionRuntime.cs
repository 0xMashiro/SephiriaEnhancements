using SephiriaEnhancements.Diagnostics;
using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using Mirror;
using SephiriaEnhancements.Configuration;
using SephiriaEnhancements.Runtime.GameBridge;
using UnityEngine;

namespace SephiriaEnhancements.MultiplayerAccess
{
    internal static class MidRunAdmissionRuntime
    {
        private static readonly FieldInfo VersionApprovedConnectionIds =
            AccessTools.Field(typeof(HorayNetworkManager),
                "versionApprovedConnIds");
        private static readonly HashSet<int> FreshConnectionIds = new();
        private static int dungeonGateBypassDepth;
        private static bool integrationAvailable;

        internal static bool IsDungeonGateBypassActive =>
            dungeonGateBypassDepth > 0;

        internal static bool IsAvailable => integrationAvailable &&
            !MultiplayerExtensionDiscovery.HasDetectedExtension;

        internal static void SetIntegrationAvailable(bool available)
        {
            integrationAvailable = available;
            if (!available) Clear();
        }

        internal static bool CanEnableNativeReconnect()
        {
            return MidRunAdmissionPolicy.CanEnableNativeReconnect(
                EnhancementsSettings.Enabled,
                MidRunAdmissionSettings.AllowJoinAndReconnect,
                integrationAvailable,
                MultiplayerExtensionDiscovery.HasDetectedExtension);
        }

        internal static bool CanAdvertiseMidRunJoin()
        {
            return MidRunAdmissionPolicy.CanOwnAdmission(
                EnhancementsSettings.Enabled,
                MidRunAdmissionSettings.AllowJoinAndReconnect,
                integrationAvailable, NetworkServer.active,
                HorayNetworkAuthenticator.AccessDeny_InDungeon,
                SaveManager.CurrentRun != null && SaveManager.SaveVersion != 0,
                MultiplayerExtensionDiscovery.HasDetectedExtension);
        }

        internal static void BeginAuthentication(NetworkConnectionToClient connection,
            HorayNetworkAuthenticator.VersionMessage message, out bool state)
        {
            state = false;
            HorayNetworkManager manager = NetworkManager.singleton as
                HorayNetworkManager;
            bool hasPerPlayerRunSave = SaveManager.CurrentRun != null &&
                SaveManager.SaveVersion != 0;
            if (!MidRunAdmissionPolicy.CanOwnAdmission(
                    EnhancementsSettings.Enabled,
                    MidRunAdmissionSettings.AllowJoinAndReconnect,
                    integrationAvailable, NetworkServer.active,
                    HorayNetworkAuthenticator.AccessDeny_InDungeon,
                    hasPerPlayerRunSave,
                    MultiplayerExtensionDiscovery.HasDetectedExtension) ||
                connection == null || connection == NetworkServer.localConnection ||
                manager == null || manager.IsRejoinBanned(message.playerGuid) ||
                manager.IsInRejoinWhitelist(message.playerGuid))
            {
                return;
            }

            state = true;
            dungeonGateBypassDepth++;
        }

        internal static void EndAuthentication(NetworkConnectionToClient connection,
            bool state, Exception exception)
        {
            if (!state) return;
            dungeonGateBypassDepth--;
            if (exception != null || connection == null) return;

            if (VersionApprovedConnectionIds?.GetValue(
                    NetworkManager.singleton as HorayNetworkManager) is
                HashSet<int> approved && approved.Contains(connection.connectionId))
            {
                FreshConnectionIds.Add(connection.connectionId);
                SupportLogger.Info("mid_run_participant_admitted", "[SephiriaEnhancements] Fresh mid-run participant " +
                    "admitted on connection " + connection.connectionId + ".");
            }
        }

        internal static void BeginFreshPlayerInitialization(GridInventory inventory,
            out bool state)
        {
            PlayerSpawner player = inventory != null
                ? inventory.GetComponent<PlayerSpawner>() : null;
            state = player?.connectionToClient != null &&
                FreshConnectionIds.Contains(player.connectionToClient.connectionId);
            if (state) dungeonGateBypassDepth++;
        }

        internal static void EndFreshPlayerInitialization(bool state)
        {
            if (state) dungeonGateBypassDepth--;
        }

        internal static bool IsFreshConnection(NetworkConnectionToClient connection)
        {
            return connection != null &&
                FreshConnectionIds.Contains(connection.connectionId);
        }

        internal static void RemoveConnection(NetworkConnectionToClient connection)
        {
            if (connection != null)
                FreshConnectionIds.Remove(connection.connectionId);
        }

        internal static void Clear()
        {
            FreshConnectionIds.Clear();
            dungeonGateBypassDepth = 0;
        }
    }
}
