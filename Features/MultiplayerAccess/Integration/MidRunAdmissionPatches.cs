using SephiriaEnhancements.Diagnostics;
using System;
using HarmonyLib;
using Mirror;
using SephiriaEnhancements.Runtime.GameBridge;
using UnityEngine;

namespace SephiriaEnhancements.MultiplayerAccess.Integration
{
    [HarmonyPatch(typeof(HorayNetworkAuthenticator),
        "OnServerVersionMessage")]
    internal static class MidRunAuthenticationPatch
    {
        private static void Prefix(NetworkConnectionToClient conn,
            HorayNetworkAuthenticator.VersionMessage message, out bool __state)
        {
            MidRunAdmissionRuntime.BeginAuthentication(conn, message, out __state);
        }

        private static void Finalizer(NetworkConnectionToClient conn,
            bool __state, Exception __exception)
        {
            MidRunAdmissionRuntime.EndAuthentication(conn, __state, __exception);
        }
    }

    [HarmonyPatch(typeof(HorayNetworkAuthenticator),
        "get_AccessDeny_InDungeon")]
    internal static class MidRunDungeonAccessPatch
    {
        private static bool Prefix(ref bool __result)
        {
            if (!MidRunAdmissionRuntime.IsDungeonGateBypassActive) return true;
            __result = false;
            return false;
        }
    }

    [HarmonyPatch(typeof(HorayNetworkManager), "get_AllowRejoin")]
    internal static class MidRunReconnectSupportPatch
    {
        private static bool Prefix(ref bool __result)
        {
            if (!MidRunAdmissionRuntime.CanEnableNativeReconnect()) return true;
            __result = true;
            return false;
        }
    }

    [HarmonyPatch(typeof(GridInventory), nameof(GridInventory.AddStartingItem))]
    internal static class FreshPlayerStartingItemPatch
    {
        private static void Prefix(GridInventory __instance, out bool __state)
        {
            MidRunAdmissionRuntime.BeginFreshPlayerInitialization(__instance,
                out __state);
        }

        private static void Finalizer(bool __state)
        {
            MidRunAdmissionRuntime.EndFreshPlayerInitialization(__state);
        }
    }

    [HarmonyPatch(typeof(DungeonManager), "LoadStageAndMove")]
    internal static class MidRunLobbyAvailabilityPatch
    {
        private static void Postfix()
        {
            if (NetworkServer.active &&
                MidRunAdmissionRuntime.CanAdvertiseMidRunJoin())
                NativeLobbyAccess.TryOpenOwnedSteamRoomForJoin();
        }
    }

    [HarmonyPatch(typeof(PlayerSpawner),
        "ResolveCurrentPlayerIdxForSave")]
    internal static class FreshPlayerSaveSlotPatch
    {
        private static bool Prefix(PlayerSpawner __instance, string playerGuid)
        {
            if (!MidRunAdmissionRuntime.IsFreshConnection(
                    __instance.connectionToClient) ||
                SaveManager.CurrentRun == null)
            {
                return true;
            }

            int newSlot = Math.Max(0,
                SaveManager.CurrentRun.GetInt("SavedPlayerCount", 0));
            __instance.NetworkcurrentPlayerIdxForSave = newSlot;
            SaveManager.CurrentRun.SetInt("SavedPlayerCount", newSlot + 1);
            if (!string.IsNullOrWhiteSpace(playerGuid))
                SaveManager.CurrentRun.SetString(
                    $"Player{newSlot}Guid", playerGuid);
            SupportLogger.Info("mid_run_save_slot_assigned", "[SephiriaEnhancements] Fresh mid-run participant " +
                "assigned save slot " + newSlot + ".");
            return false;
        }
    }

    [HarmonyPatch(typeof(HorayNetworkManager),
        nameof(HorayNetworkManager.OnServerDisconnect))]
    internal static class MidRunDisconnectCleanupPatch
    {
        private static void Prefix(NetworkConnectionToClient conn) =>
            MidRunAdmissionRuntime.RemoveConnection(conn);
    }

    [HarmonyPatch(typeof(HorayNetworkManager), "OnStopServer")]
    internal static class MidRunServerCleanupPatch
    {
        private static void Postfix() => MidRunAdmissionRuntime.Clear();
    }
}
