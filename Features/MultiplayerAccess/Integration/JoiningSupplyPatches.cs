using SephiriaEnhancements.Runtime;
using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using Mirror;
using UnityEngine;

namespace SephiriaEnhancements.MultiplayerAccess.Integration
{
    [HarmonyPatch(typeof(PlayerSpawner), "Initialize")]
    internal static class JoiningSupplyInitializationPatch
    {
        private static void Postfix(PlayerSpawner __instance, bool __result)
        {
            if (!FeatureFailure.IsAvailable(FeatureId.MultiplayerAccess))
            {
                return;
            }

            try
            {
                PostfixCore(__instance, __result);
            }
            catch (System.Exception exception)
            {
                FeatureFailure.Disable(FeatureId.MultiplayerAccess, exception);
                return;
            }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static void PostfixCore(PlayerSpawner __instance, bool __result)
        {
            if (__result && NetworkServer.active)
                NativeJoiningSupplies.Instance?.Initialized(__instance);
        }
    }

    [HarmonyPatch(typeof(DungeonManager), "LoadStageAndMove")]
    internal static class JoiningSupplyExplorationPatch
    {
        private static void Prefix(DungeonManager __instance)
        {
            if (!FeatureFailure.IsAvailable(FeatureId.MultiplayerAccess))
            {
                return;
            }

            try
            {
                PrefixCore(__instance);
            }
            catch (System.Exception exception)
            {
                FeatureFailure.Disable(FeatureId.MultiplayerAccess, exception);
                return;
            }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static void PrefixCore(DungeonManager __instance)
        {
            if (NetworkServer.active && !__instance.isRunStarted)
                NativeJoiningSupplies.Instance?.BindRun(true);
        }
    }

    [HarmonyPatch(typeof(DungeonManager), "LoadDungeon")]
    internal static class JoiningSupplyLoadPatch
    {
        private static void Prefix()
        {
            if (!FeatureFailure.IsAvailable(FeatureId.MultiplayerAccess))
            {
                return;
            }

            try
            {
                PrefixCore();
            }
            catch (System.Exception exception)
            {
                FeatureFailure.Disable(FeatureId.MultiplayerAccess, exception);
                return;
            }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static void PrefixCore() => NativeJoiningSupplies.Instance?.BindRun();
    }

    [HarmonyPatch(typeof(NetworkServer), "SpawnObject", new[] { typeof(GameObject), typeof(NetworkConnectionToClient) })]
    internal static class JoiningSupplySpawnPatch
    {
        private static void Postfix(GameObject obj, NetworkConnectionToClient ownerConnection)
        {
            if (!FeatureFailure.IsAvailable(FeatureId.MultiplayerAccess))
            {
                return;
            }

            try
            {
                PostfixCore(obj, ownerConnection);
            }
            catch (System.Exception exception)
            {
                FeatureFailure.Disable(FeatureId.MultiplayerAccess, exception);
                return;
            }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static void PostfixCore(GameObject obj, NetworkConnectionToClient ownerConnection)
        {
            if (obj != null && obj.TryGetComponent<NetworkIdentity>(out var identity) && identity.isServer && identity.netId != 0)
                NativeJoiningSupplies.Instance?.Spawned(obj, ownerConnection);
        }
    }

    [HarmonyPatch(typeof(PlayerAvatar), "FinishSephiriteAcquire")]
    internal static class JoiningSupplyFinishPatch
    {
        private static bool Prefix(PlayerAvatar __instance, Sephirite sephirite)
        {
            if (!FeatureFailure.IsAvailable(FeatureId.MultiplayerAccess))
            {
                return true;
            }

            try
            {
                return PrefixCore(__instance, sephirite);
            }
            catch (System.Exception exception)
            {
                FeatureFailure.Disable(FeatureId.MultiplayerAccess, exception);
                return true;
            }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static bool PrefixCore(PlayerAvatar __instance, Sephirite sephirite) => sephirite == null || (NativeJoiningSupplies.Instance?.AllowNativeFinish(__instance, sephirite) ?? true);
    }

    [HarmonyPatch(typeof(AltarOfTablet), "UserCode_CmdSpawnReward__AltarOfTabletInteractable__PlayerSpawner")]
    internal static class JoiningSupplyTabletPatch
    {
        private static void Prefix(AltarOfTablet __instance, out (bool Captured, AltarOfTablet Value) __state)
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
        private static void PrefixCore(AltarOfTablet __instance, out (bool Captured, AltarOfTablet Value) __state)
        {
            __state = (true, NativeJoiningSupplies.SelectingTablet);
            NativeJoiningSupplies.SelectingTablet = __instance;
        }
        private static void Finalizer((bool Captured, AltarOfTablet Value) __state)
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
        private static void FinalizerCore((bool Captured, AltarOfTablet Value) __state) { if (__state.Captured) NativeJoiningSupplies.SelectingTablet = __state.Value; }
    }

    [HarmonyPatch(typeof(SaveData), nameof(SaveData.Copy))]
    internal static class JoiningSupplyCheckpointPatch
    {
        private static void Prefix(SaveData __instance)
        {
            if (!FeatureFailure.IsAvailable(FeatureId.MultiplayerAccess))
            {
                return;
            }

            try
            {
                PrefixCore(__instance);
            }
            catch (System.Exception exception)
            {
                FeatureFailure.Disable(FeatureId.MultiplayerAccess, exception);
                return;
            }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static void PrefixCore(SaveData __instance)
        {
            if (ReferenceEquals(__instance, SaveManager.CurrentRun))
                NativeJoiningSupplies.Instance?.Flush();
        }
    }

    [HarmonyPatch(typeof(PlayerSpawner), nameof(PlayerSpawner.SaveCurrentSessionData))]
    internal static class JoiningSupplySavePatch
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
        private static void PostfixCore() => NativeJoiningSupplies.Instance?.Flush();
    }

    [HarmonyPatch(typeof(HorayNetworkManager), nameof(HorayNetworkManager.OnServerDisconnect))]
    internal static class JoiningSupplyDisconnectPatch
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
        private static void PrefixCore(NetworkConnectionToClient conn) => NativeJoiningSupplies.Instance?.Disconnect(conn);
    }

    [HarmonyPatch(typeof(NetworkServer), "OnCommandMessage")]
    internal static class JoiningSupplyCommandOwnershipPatch
    {
        private static bool Prefix(NetworkConnectionToClient conn, CommandMessage msg)
        {
            if (!FeatureFailure.IsAvailable(FeatureId.MultiplayerAccess))
            {
                return true;
            }

            try
            {
                return PrefixCore(conn, msg);
            }
            catch (System.Exception exception)
            {
                FeatureFailure.Disable(FeatureId.MultiplayerAccess, exception);
                return true;
            }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static bool PrefixCore(NetworkConnectionToClient conn, CommandMessage msg) => !NetworkServer.spawned.TryGetValue(msg.netId, out var identity) || (NativeJoiningSupplies.Instance?.Owns(identity.gameObject, conn) ?? true);
    }

    [HarmonyPatch]
    internal static class JoiningSupplyRewardOwnershipPatch
    {
        private static IEnumerable<MethodBase> TargetMethods()
        {
            yield return AccessTools.Method(typeof(PlayerAvatar), "LocalSelectSephiriteReward");
            yield return AccessTools.Method(typeof(PlayerAvatar), "LocalSelectSephiriteRewardToSubBag");
        }
        private static bool Prefix(PlayerAvatar __instance, Sephirite sephirite)
        {
            if (!FeatureFailure.IsAvailable(FeatureId.MultiplayerAccess))
            {
                return true;
            }

            try
            {
                return PrefixCore(__instance, sephirite);
            }
            catch (System.Exception exception)
            {
                FeatureFailure.Disable(FeatureId.MultiplayerAccess, exception);
                return true;
            }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static bool PrefixCore(PlayerAvatar __instance, Sephirite sephirite) => sephirite == null || (NativeJoiningSupplies.Instance?.Owns(sephirite.gameObject, __instance.connectionToClient) ?? true);
    }

    [HarmonyPatch(typeof(SaveData), nameof(SaveData.Save), new[] { typeof(string) })]
    internal static class JoiningSupplyWriteSavePatch
    {
        private static void Prefix(SaveData __instance)
        {
            if (!FeatureFailure.IsAvailable(FeatureId.MultiplayerAccess))
            {
                return;
            }

            try
            {
                PrefixCore(__instance);
            }
            catch (System.Exception exception)
            {
                FeatureFailure.Disable(FeatureId.MultiplayerAccess, exception);
                return;
            }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static void PrefixCore(SaveData __instance)
        {
            if (ReferenceEquals(__instance, SaveManager.CurrentRun))
                NativeJoiningSupplies.Instance?.Flush();
        }
    }

    [HarmonyPatch(typeof(NetworkServer), nameof(NetworkServer.Destroy), new[] { typeof(GameObject) })]
    internal static class JoiningSupplyDespawnPatch
    {
        private static void Prefix(GameObject obj)
        {
            if (!FeatureFailure.IsAvailable(FeatureId.MultiplayerAccess))
            {
                return;
            }

            try
            {
                PrefixCore(obj);
            }
            catch (System.Exception exception)
            {
                FeatureFailure.Disable(FeatureId.MultiplayerAccess, exception);
                return;
            }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static void PrefixCore(GameObject obj) => NativeJoiningSupplies.Instance?.Despawning(obj);
    }

    [HarmonyPatch(typeof(DiceSpawner), "ServerCreateDice")]
    internal static class JoiningSupplyFacilityDicePatch
    {
        private static void Prefix(out bool? __state)
        {
            __state = default;
            if (!FeatureFailure.IsAvailable(FeatureId.MultiplayerAccess))
            {
                return;
            }

            try
            {
                PrefixCore(out __state);
            }
            catch (System.Exception exception)
            {
                FeatureFailure.Disable(FeatureId.MultiplayerAccess, exception);
                return;
            }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static void PrefixCore(out bool? __state)
        {
            __state = NativeJoiningSupplies.SpawningFacilityDice;
            NativeJoiningSupplies.SpawningFacilityDice = true;
        }
        private static void Finalizer(bool? __state)
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
        private static void FinalizerCore(bool? __state) { if (__state.HasValue) NativeJoiningSupplies.SpawningFacilityDice = __state.Value; }
    }

    [HarmonyPatch]
    internal static class JoiningSupplyInteractionOwnershipPatch
    {
        private static IEnumerable<MethodBase> TargetMethods()
        {
            yield return AccessTools.Method(typeof(MiracleSelector2), "HandleInteraction");
            yield return AccessTools.Method(typeof(Anvil), "HandleInteraction");
            yield return AccessTools.Method(typeof(AltarOfEnchant), "HandleInteractive");
            yield return AccessTools.Method(typeof(AltarOfTablet), "Select");
            yield return AccessTools.Method(typeof(Sephirite), "HandleInteraction");
        }
        private static bool Prefix(Component __instance, GameObject actor)
        {
            if (!FeatureFailure.IsAvailable(FeatureId.MultiplayerAccess))
            {
                return true;
            }

            try
            {
                return PrefixCore(__instance, actor);
            }
            catch (System.Exception exception)
            {
                FeatureFailure.Disable(FeatureId.MultiplayerAccess, exception);
                return true;
            }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static bool PrefixCore(Component __instance, GameObject actor) => !NetworkServer.active || actor == null || (NativeJoiningSupplies.Instance?.Owns(__instance.gameObject, actor.GetComponent<PlayerAvatar>()?.connectionToClient) ?? true);
    }

    [HarmonyPatch(typeof(MiracleSelector2), "GenerateMiracles")]
    internal static class JoiningSupplyEmbeddedMiraclePatch
    {
        private static void Postfix(MiracleSelector2 __instance, NetworkIdentity identity)
        {
            if (!FeatureFailure.IsAvailable(FeatureId.MultiplayerAccess))
            {
                return;
            }

            try
            {
                PostfixCore(__instance, identity);
            }
            catch (System.Exception exception)
            {
                FeatureFailure.Disable(FeatureId.MultiplayerAccess, exception);
                return;
            }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static void PostfixCore(MiracleSelector2 __instance, NetworkIdentity identity) => NativeJoiningSupplies.Instance?.ObservedEmbeddedReward(__instance, identity);
    }

    [HarmonyPatch(typeof(Anvil), "UserCode_CmdMarkEnhanced__NetworkConnectionToClient")]
    internal static class JoiningSupplyEmbeddedAnvilPatch
    {
        private static void Postfix(Anvil __instance, NetworkConnectionToClient sender)
        {
            if (!FeatureFailure.IsAvailable(FeatureId.MultiplayerAccess))
            {
                return;
            }

            try
            {
                PostfixCore(__instance, sender);
            }
            catch (System.Exception exception)
            {
                FeatureFailure.Disable(FeatureId.MultiplayerAccess, exception);
                return;
            }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static void PostfixCore(Anvil __instance, NetworkConnectionToClient sender) => NativeJoiningSupplies.Instance?.ObservedEmbeddedReward(__instance, sender?.identity);
    }
}
