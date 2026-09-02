using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace SephiriaEnhancements.DefeatRetry
{
    [HarmonyPatch(typeof(UI_GameOverLabel), nameof(UI_GameOverLabel.OnOpened))]
    internal static class GameOverDefeatRetryButtonPatch
    {
        private static void Postfix(UI_GameOverLabel __instance)
        {
            DefeatRetryFeature.AddButton(__instance);
        }
    }

    [HarmonyPatch(typeof(BossSpawner), nameof(BossSpawner.StartBattle))]
    internal static class BossEncounterRetryCheckpointPatch
    {
        private static void Prefix(BossSpawner __instance, PlayerAvatar player,
            Vector3 position, string name)
        {
            DefeatRetryFeature.CaptureBossEncounterSnapshot(__instance, player,
                position, name);
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

        private static void Prefix(SeedBossSpawner __instance, PlayerAvatar player)
        {
            DefeatRetryFeature.CaptureSeedBossEncounterSnapshot(__instance, player);
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
            DefeatRetryFeature.CaptureRenderedCombatFloorFallback(floorGuid);
        }
    }

    [HarmonyPatch(typeof(DungeonManager), nameof(DungeonManager.MoveFloor))]
    internal static class ApplyDefeatRetryPlacementPatch
    {
        private static void Prefix(PlayerAvatar avatar, string floorGuid,
            ref string spawnPoint, ref Vector3? overridePosition)
        {
            DefeatRetryFeature.ApplyPendingPlacement(avatar, floorGuid,
                ref spawnPoint, ref overridePosition);
        }
    }

    [HarmonyPatch(typeof(SaveManager), nameof(SaveManager.DeleteFile))]
    internal static class PreserveDefeatRetrySaveDeletionPatch
    {
        private static bool Prefix(string fileName)
        {
            return !DefeatRetryFeature.PreserveRunFile(fileName);
        }
    }

    [HarmonyPatch(typeof(SaveManager), nameof(SaveManager.CreateNewTMP))]
    internal static class PreserveDefeatRetrySaveCreationPatch
    {
        private static bool Prefix(string fileName)
        {
            return !DefeatRetryFeature.PreserveRunCreation(fileName);
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
            return !DefeatRetryFeature.IsRetrying;
        }
    }

    [HarmonyPatch(typeof(HorayNetworkManager), nameof(HorayNetworkManager.NewGame))]
    internal static class DefeatRetryNewGamePatch
    {
        private static void Prefix()
        {
            DefeatRetryFeature.Reset();
        }

        private static void Postfix()
        {
            if (DefeatRetryFeature.IsRetrying)
            {
                DefeatRetryFeature.CompleteRestart();
            }
        }
    }
}
