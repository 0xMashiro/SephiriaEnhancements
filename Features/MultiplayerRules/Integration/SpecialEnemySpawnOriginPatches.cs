using System;
using HarmonyLib;

namespace SephiriaEnhancements.MultiplayerRules.Integration
{
    [HarmonyPatch(typeof(SeedBossSpawner), "SpawnBoss", new Type[] { })]
    internal static class SeedEncounterBossSpawnOriginPatch
    {
        private static void Prefix(SeedBossSpawner __instance, out IDisposable __state)
        {
            // SpawnBoss is a private native integration contract.
            __state = EnemySpawnRoutineContext.Enter(EnemySpawnOrigin.SeedEncounterBoss,
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
