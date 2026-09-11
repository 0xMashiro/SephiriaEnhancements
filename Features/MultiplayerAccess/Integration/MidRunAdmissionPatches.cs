using SephiriaEnhancements.Runtime;
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
        private static void Prefix(NetworkConnectionToClient conn, HorayNetworkAuthenticator.VersionMessage message, out bool __state)
        {
            __state = default;
            if (!FeatureFailure.IsAvailable(FeatureId.MultiplayerAccess))
            {
                return;
            }

            try
            {
                PrefixCore(conn, message, out __state);
            }
            catch (System.Exception exception)
            {
                FeatureFailure.Disable(FeatureId.MultiplayerAccess, exception);
                return;
            }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static void PrefixCore(NetworkConnectionToClient conn, HorayNetworkAuthenticator.VersionMessage message, out bool __state)
        {
            MidRunAdmissionRuntime.BeginAuthentication(conn, message, out __state);
        }

        private static void Finalizer(NetworkConnectionToClient conn, bool __state, Exception __exception)
        {
            try
            {
                FinalizerCore(conn, __state, __exception);
            }
            catch (System.Exception exception)
            {
                FeatureFailure.Disable(FeatureId.MultiplayerAccess, exception);
                return;
            }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static void FinalizerCore(NetworkConnectionToClient conn, bool __state, Exception __exception)
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
            if (!FeatureFailure.IsAvailable(FeatureId.MultiplayerAccess))
            {
                return true;
            }

            var original___result = __result;
            try
            {
                return PrefixCore(ref __result);
            }
            catch (System.Exception exception)
            {
                __result = original___result;
                FeatureFailure.Disable(FeatureId.MultiplayerAccess, exception);
                return true;
            }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static bool PrefixCore(ref bool __result)
        {
            if (!MidRunAdmissionRuntime.IsDungeonGateBypassActive)
                return true;
            __result = false;
            return false;
        }
    }

    [HarmonyPatch(typeof(HorayNetworkManager), "get_AllowRejoin")]
    internal static class MidRunReconnectSupportPatch
    {
        private static bool Prefix(ref bool __result)
        {
            if (!FeatureFailure.IsAvailable(FeatureId.MultiplayerAccess))
            {
                return true;
            }

            var original___result = __result;
            try
            {
                return PrefixCore(ref __result);
            }
            catch (System.Exception exception)
            {
                __result = original___result;
                FeatureFailure.Disable(FeatureId.MultiplayerAccess, exception);
                return true;
            }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static bool PrefixCore(ref bool __result)
        {
            if (!MidRunAdmissionRuntime.CanEnableNativeReconnect())
                return true;
            __result = true;
            return false;
        }
    }

    [HarmonyPatch(typeof(GridInventory), nameof(GridInventory.AddStartingItem))]
    internal static class FreshPlayerStartingItemPatch
    {
        private static void Prefix(GridInventory __instance, out bool __state)
        {
            __state = default;
            if (!FeatureFailure.IsAvailable(FeatureId.MultiplayerAccess))
            {
                return;
            }

            try
            {
                PrefixCore(__instance, out __state);
            }
            catch (System.Exception exception)
            {
                FeatureFailure.Disable(FeatureId.MultiplayerAccess, exception);
                return;
            }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static void PrefixCore(GridInventory __instance, out bool __state)
        {
            MidRunAdmissionRuntime.BeginFreshPlayerInitialization(__instance, out __state);
        }

        private static void Finalizer(bool __state)
        {
            try
            {
                FinalizerCore(__state);
            }
            catch (System.Exception exception)
            {
                FeatureFailure.Disable(FeatureId.MultiplayerAccess, exception);
                return;
            }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static void FinalizerCore(bool __state)
        {
            MidRunAdmissionRuntime.EndFreshPlayerInitialization(__state);
        }
    }

    [HarmonyPatch(typeof(DungeonManager), "LoadStageAndMove")]
    internal static class MidRunLobbyAvailabilityPatch
    {
        private static void Postfix()
        {
            if (!FeatureFailure.IsAvailable(FeatureId.MultiplayerAccess))
            {
                return;
            }

            try
            {
                PostfixCore();
            }
            catch (System.Exception exception)
            {
                FeatureFailure.Disable(FeatureId.MultiplayerAccess, exception);
                return;
            }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static void PostfixCore()
        {
            if (NetworkServer.active && MidRunAdmissionRuntime.CanKeepRoomOpenForPlayers())
                NativeLobbyAccess.TryOpenOwnedSteamRoomForJoin();
        }
    }

    [HarmonyPatch(typeof(PlayerSpawner),
        "ResolveCurrentPlayerIdxForSave")]
    internal static class FreshPlayerSaveSlotPatch
    {
        private static bool Prefix(PlayerSpawner __instance, string playerGuid)
        {
            if (!FeatureFailure.IsAvailable(FeatureId.MultiplayerAccess))
            {
                return true;
            }

            try
            {
                return PrefixCore(__instance, playerGuid);
            }
            catch (System.Exception exception)
            {
                FeatureFailure.Disable(FeatureId.MultiplayerAccess, exception);
                return true;
            }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static bool PrefixCore(PlayerSpawner __instance, string playerGuid)
        {
            if (MidRunAdmissionRuntime.HasSavedPlayer(playerGuid))
            {
                MidRunAdmissionRuntime.RemoveConnection(__instance.connectionToClient);
                return true;
            }

            if (!MidRunAdmissionRuntime.IsFreshConnection(__instance.connectionToClient) || SaveManager.CurrentRun == null)
            {
                return true;
            }

            int newSlot = Math.Max(0, SaveManager.CurrentRun.GetInt("SavedPlayerCount", 0));
            __instance.NetworkcurrentPlayerIdxForSave = newSlot;
            SaveManager.CurrentRun.SetInt("SavedPlayerCount", newSlot + 1);
            if (!string.IsNullOrWhiteSpace(playerGuid))
                SaveManager.CurrentRun.SetString($"Player{newSlot}Guid", playerGuid);
            SupportLogger.Info("mid_run_save_slot_assigned", "[SephiriaEnhancements] Fresh mid-run participant " + "assigned save slot " + newSlot + ".");
            return false;
        }
    }

    [HarmonyPatch(typeof(HorayNetworkManager),
        nameof(HorayNetworkManager.OnServerDisconnect))]
    internal static class MidRunDisconnectCleanupPatch
    {
        private static void Prefix(NetworkConnectionToClient conn)
        {
            if (!FeatureFailure.IsAvailable(FeatureId.MultiplayerAccess))
            {
                return;
            }

            try
            {
                PrefixCore(conn);
            }
            catch (System.Exception exception)
            {
                FeatureFailure.Disable(FeatureId.MultiplayerAccess, exception);
                return;
            }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static void PrefixCore(NetworkConnectionToClient conn) => MidRunAdmissionRuntime.RemoveConnection(conn);
    }

    [HarmonyPatch(typeof(HorayNetworkManager), "OnStopServer")]
    internal static class MidRunServerCleanupPatch
    {
        private static void Postfix()
        {
            if (!FeatureFailure.IsAvailable(FeatureId.MultiplayerAccess))
            {
                return;
            }

            try
            {
                PostfixCore();
            }
            catch (System.Exception exception)
            {
                FeatureFailure.Disable(FeatureId.MultiplayerAccess, exception);
                return;
            }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static void PostfixCore()
        {
            MidRunAdmissionRuntime.Clear();
            NativeJoiningSupplies.Instance?.Stopped();
        }
    }
}
