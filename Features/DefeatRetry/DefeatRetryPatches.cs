using SephiriaEnhancements.Runtime;
using System.Reflection;
using HarmonyLib;
using SephiriaEnhancements.Integration;
using UnityEngine;

namespace SephiriaEnhancements.DefeatRetry
{
    [HarmonyPatch(typeof(UI_GameOverLabel), nameof(UI_GameOverLabel.OnOpened))]
    internal static class GameOverDefeatRetryButtonPatch
    {
        private static void Postfix(UI_GameOverLabel __instance)
        {
            if (!FeatureFailure.IsAvailable(FeatureId.DefeatRetry))
            {
                return;
            }

            try
            {
                PostfixCore(__instance);
            }
            catch (System.Exception exception)
            {
                FeatureFailure.Disable(FeatureId.DefeatRetry, exception);
                return;
            }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static void PostfixCore(UI_GameOverLabel __instance)
        {
            if (__instance.openType == 0)
                DefeatRetryBridge.ObserveTeamDefeat();
            DefeatRetryFeature.AddButton(__instance);
        }
    }

    [HarmonyPatch(typeof(BossSpawner), nameof(BossSpawner.StartBattle))]
    internal static class BossEncounterRetryCheckpointPatch
    {
        private static bool Prefix(BossSpawner __instance, PlayerAvatar player, Vector3 position, string name)
        {
            if (!FeatureFailure.IsAvailable(FeatureId.DefeatRetry))
            {
                return true;
            }

            try
            {
                return PrefixCore(__instance, player, position, name);
            }
            catch (System.Exception exception)
            {
                FeatureFailure.Disable(FeatureId.DefeatRetry, exception);
                return true;
            }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static bool PrefixCore(BossSpawner __instance, PlayerAvatar player, Vector3 position, string name)
        {
            if (DefeatRetryFeature.IsRetrying || DefeatRetryBridge.BlocksBossBattle)
                return false;
            DefeatRetryFeature.CaptureBossEncounterSnapshot(__instance, player, position, name);
            return true;
        }
    }

    [HarmonyPatch]
    internal static class SeedBossEncounterRetryCheckpointPatch
    {
        private static MethodBase TargetMethod()
        {
            return AccessTools.Method(typeof(SeedBossSpawner),
                "UserCode_CmdSpawnBoss__PlayerAvatar");
        }

        private static bool Prefix(SeedBossSpawner __instance, PlayerAvatar player)
        {
            if (!FeatureFailure.IsAvailable(FeatureId.DefeatRetry))
            {
                return true;
            }

            try
            {
                return PrefixCore(__instance, player);
            }
            catch (System.Exception exception)
            {
                FeatureFailure.Disable(FeatureId.DefeatRetry, exception);
                return true;
            }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static bool PrefixCore(SeedBossSpawner __instance, PlayerAvatar player)
        {
            if (DefeatRetryFeature.IsRetrying || DefeatRetryBridge.BlocksBossBattle)
                return false;
            DefeatRetryFeature.CaptureSeedBossEncounterSnapshot(__instance, player);
            return true;
        }
    }

    [HarmonyPatch]
    internal static class RenderedCombatFloorRetryCheckpointPatch
    {
        private static System.Collections.Generic.IEnumerable<MethodBase>
            TargetMethods()
        {
            yield return AccessTools.DeclaredMethod(typeof(PlayerLocalDataStorage),
                nameof(PlayerLocalDataStorage.OnFloorRenderFinalizedVeryFirst),
                new[] { typeof(string) });
            yield return AccessTools.DeclaredMethod(typeof(PlayerLocalDataStorage),
                nameof(PlayerLocalDataStorage.OnFloorRenderFinalized),
                new[] { typeof(string) });
        }

        private static void Postfix(string floorGuid)
        {
            if (!FeatureFailure.IsAvailable(FeatureId.DefeatRetry))
            {
                return;
            }

            try
            {
                PostfixCore(floorGuid);
            }
            catch (System.Exception exception)
            {
                FeatureFailure.Disable(FeatureId.DefeatRetry, exception);
                return;
            }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static void PostfixCore(string floorGuid)
        {
            DefeatRetryFeature.CaptureRenderedCombatFloorFallback(floorGuid);
        }
    }

    [HarmonyPatch(typeof(DungeonManager), nameof(DungeonManager.MoveFloor))]
    internal static class ApplyDefeatRetryPlacementPatch
    {
        private static void Prefix(PlayerAvatar avatar, string floorGuid, ref string spawnPoint, ref Vector3? overridePosition)
        {
            if (!FeatureFailure.IsAvailable(FeatureId.DefeatRetry))
            {
                return;
            }

            var original_spawnPoint = spawnPoint;
            var original_overridePosition = overridePosition;
            try
            {
                PrefixCore(avatar, floorGuid, ref spawnPoint, ref overridePosition);
            }
            catch (System.Exception exception)
            {
                spawnPoint = original_spawnPoint;
                overridePosition = original_overridePosition;
                FeatureFailure.Disable(FeatureId.DefeatRetry, exception);
                return;
            }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static void PrefixCore(PlayerAvatar avatar, string floorGuid, ref string spawnPoint, ref Vector3? overridePosition)
        {
            DefeatRetryFeature.ApplyPendingPlacement(avatar, floorGuid, ref spawnPoint, ref overridePosition);
        }
    }

    [HarmonyPatch(typeof(DungeonManager), "LocalMoveFloor")]
    internal static class DefeatRetryTravelRequestPatch
    {
        private static bool Prefix(PlayerAvatar avatar)
        {
            if (!FeatureFailure.IsAvailable(FeatureId.DefeatRetry))
            {
                return true;
            }

            try
            {
                return PrefixCore(avatar);
            }
            catch (System.Exception exception)
            {
                FeatureFailure.Disable(FeatureId.DefeatRetry, exception);
                return true;
            }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static bool PrefixCore(PlayerAvatar avatar)
        {
            return avatar == DefeatRetryPlayerRestorePatch.RestoringPlayer || (!DefeatRetryFeature.IsRetrying && !DefeatRetryBridge.IsAwaitingArrival(avatar));
        }
    }

    [HarmonyPatch(typeof(SaveManager), nameof(SaveManager.DeleteFile))]
    internal static class PreserveDefeatRetrySaveDeletionPatch
    {
        private static bool Prefix(string fileName)
        {
            // Keep the in-flight save guard until recovery cleanup has finished.
            try
            {
                return PrefixCore(fileName);
            }
            catch (System.Exception exception)
            {
                FeatureFailure.Disable(FeatureId.DefeatRetry, exception);
                return false;
            }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static bool PrefixCore(string fileName)
        {
            return !DefeatRetryClientRestore.PreserveRunFile(fileName) && !DefeatRetryFeature.PreserveRunFile(fileName);
        }
    }

    [HarmonyPatch(typeof(SaveManager), nameof(SaveManager.CreateNewTMP))]
    internal static class PreserveDefeatRetrySaveCreationPatch
    {
        private static bool Prefix(string fileName)
        {
            // Keep the in-flight save guard until recovery cleanup has finished.
            try
            {
                return PrefixCore(fileName);
            }
            catch (System.Exception exception)
            {
                FeatureFailure.Disable(FeatureId.DefeatRetry, exception);
                return false;
            }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static bool PrefixCore(string fileName)
        {
            return !DefeatRetryClientRestore.PreserveRunFile(fileName, creation: true) && !DefeatRetryFeature.PreserveRunCreation(fileName);
        }
    }

    [HarmonyPatch]
    internal static class PreserveDefeatRetryLobbyPatch
    {
        private static MethodBase TargetMethod()
        {
            return AccessTools.Method(typeof(HorayNetworkManager),
                "UnlockSteamLobbyAfterRun");
        }

        private static bool Prefix()
        {
            if (!FeatureFailure.IsAvailable(FeatureId.DefeatRetry))
            {
                return true;
            }

            try
            {
                return PrefixCore();
            }
            catch (System.Exception exception)
            {
                FeatureFailure.Disable(FeatureId.DefeatRetry, exception);
                return true;
            }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static bool PrefixCore()
        {
            return !DefeatRetryFeature.IsRetrying;
        }
    }

    [HarmonyPatch]
    internal static class PreserveDefeatRetryRejoinStatePatch
    {
        private static MethodBase TargetMethod()
        {
            return AccessTools.Method(typeof(HorayNetworkManager),
                "ClearRunScopedRejoinState");
        }

        private static bool Prefix()
        {
            if (!FeatureFailure.IsAvailable(FeatureId.DefeatRetry))
            {
                return true;
            }

            try
            {
                return PrefixCore();
            }
            catch (System.Exception exception)
            {
                FeatureFailure.Disable(FeatureId.DefeatRetry, exception);
                return true;
            }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static bool PrefixCore()
        {
            return !DefeatRetryFeature.IsRetrying;
        }
    }

    [HarmonyPatch(typeof(HorayNetworkManager), nameof(HorayNetworkManager.NewGame))]
    internal static class DefeatRetryNewGamePatch
    {
        private static void Prefix()
        {
            if (!FeatureFailure.IsAvailable(FeatureId.DefeatRetry))
            {
                return;
            }

            try
            {
                PrefixCore();
            }
            catch (System.Exception exception)
            {
                FeatureFailure.Disable(FeatureId.DefeatRetry, exception);
                return;
            }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static void PrefixCore()
        {
            DefeatRetryFeature.Reset();
        }

        private static void Postfix()
        {
            if (!FeatureFailure.IsAvailable(FeatureId.DefeatRetry))
            {
                return;
            }

            try
            {
                PostfixCore();
            }
            catch (System.Exception exception)
            {
                FeatureFailure.Disable(FeatureId.DefeatRetry, exception);
                return;
            }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static void PostfixCore()
        {
            if (DefeatRetryFeature.IsRetrying)
            {
                DefeatRetryFeature.CompleteRestart();
            }
        }
    }
}
