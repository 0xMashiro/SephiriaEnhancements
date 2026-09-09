using System;
using HarmonyLib;

namespace SephiriaEnhancements.MultiplayerRules.Integration
{
    [HarmonyPatch(typeof(SeedBossSpawner), "SpawnBoss", new Type[] { })]
    internal static class KrazBossSpawnOriginPatch
    {
        private static void Prefix(SeedBossSpawner __instance, out IDisposable __state)
        {
            // SeedBossSpawner is the native spawner for Kraz; retain its API name here.
            __state = EnemySpawnRoutineContext.Enter(EnemySpawnOrigin.KrazBoss,
                __instance);
        }

        private static Exception Finalizer(Exception __exception, IDisposable __state)
        {
            __state?.Dispose();
            return __exception;
        }
    }

    [HarmonyPatch(typeof(Unit_RootDemon), nameof(Unit_RootDemon.SummonUnit))]
    internal static class MindEaterRootSummonOriginPatch
    {
        private static void Prefix(Unit_RootDemon __instance, out IDisposable __state)
        {
            __state = EnemySpawnRoutineContext.Enter(
                EnemySpawnOrigin.MindEaterRootSummon, __instance);
        }

        private static Exception Finalizer(Exception __exception, IDisposable __state)
        {
            __state?.Dispose();
            return __exception;
        }
    }
}
