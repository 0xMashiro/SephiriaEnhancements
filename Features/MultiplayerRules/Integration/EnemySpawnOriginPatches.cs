using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using Mirror;
using UnityEngine;

namespace SephiriaEnhancements.MultiplayerRules.Integration
{
    [HarmonyPatch]
    internal static class EnemySpawnRoutineOriginPatch
    {
        private const string FixedEncounterRoutine = "SpawnCoroutine";
        private const string RandomEncounterRoutine = "SpawnEnemy";

        private static IEnumerable<MethodBase> TargetMethods()
        {
            // These native names are integration contracts. Keep them at this boundary;
            // domain terminology must not replace them during semantic refactors.
            yield return RequireRoutine(typeof(EnemySpawner), FixedEncounterRoutine,
                typeof(int));
            yield return RequireRoutine(typeof(CommonEnemySpawner), FixedEncounterRoutine,
                typeof(int));
            yield return RequireRoutine(typeof(RandomEnemyPhaseSpawner),
                RandomEncounterRoutine);
        }

        private static MethodBase RequireRoutine(Type owner, string methodName,
            params Type[] parameterTypes)
        {
            MethodInfo method = AccessTools.Method(owner, methodName, parameterTypes);
            if (method == null || !typeof(IEnumerator).IsAssignableFrom(method.ReturnType))
            {
                throw new MissingMethodException(owner.FullName, methodName);
            }

            return method;
        }

        private static void Postfix(object __instance, ref IEnumerator __result)
        {
            if (!EnemySpawnOriginCapture.IsObserved || __result == null)
            {
                return;
            }

            EnemySpawnOrigin origin = __instance is RandomEnemyPhaseSpawner
                ? EnemySpawnOrigin.RandomEncounter
                : EnemySpawnOrigin.RegularEncounter;
            __result = EnemySpawnRoutineContext.Wrap(__result, origin, __instance);
        }
    }

    [HarmonyPatch(typeof(AvatarSpawnEntity), nameof(AvatarSpawnEntity.Spawn))]
    internal static class AvatarSpawnOriginCapturePatch
    {
        private static void Postfix(UnitAvatar __result)
        {
            EnemySpawnRoutineFrame frame = EnemySpawnRoutineContext.CurrentFrame;
            if (__result != null && frame != null)
            {
                EnemySpawnOriginCapture.Publish(__result, frame);
            }
        }
    }

    [HarmonyPatch(typeof(NetworkServer), nameof(NetworkServer.Spawn),
        new[] { typeof(GameObject), typeof(NetworkConnectionToClient) })]
    internal static class NetworkSpawnOriginCapturePatch
    {
        private static void Prefix(GameObject obj)
        {
            EnemySpawnRoutineFrame frame = EnemySpawnRoutineContext.CurrentFrame;
            if (obj != null && frame != null &&
                obj.TryGetComponent<UnitAvatar>(out UnitAvatar unit))
                EnemySpawnOriginCapture.Publish(unit, frame);
        }
    }
}
