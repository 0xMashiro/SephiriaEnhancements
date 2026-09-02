using System;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace SephiriaEnhancements.MultiplayerRules.Integration
{
    internal static class HiddenRoomRewardReplacementContext
    {
        [ThreadStatic] internal static bool SuppressNativeBreakables;
        [ThreadStatic] internal static bool CreatingConfiguredBreakables;
    }

    [HarmonyPatch(typeof(HiddenRoomRewardSpawner),
        nameof(HiddenRoomRewardSpawner.OnStartServer))]
    internal static class HiddenRoomBreakableRewardCountRulePatch
    {
        private const int MultiplayerBreakableRewardRoll = 1;
        // SpawnProp is a private native integration contract.
        private static readonly MethodInfo SpawnPropMethod = AccessTools.Method(
            typeof(HiddenRoomRewardSpawner), "SpawnProp");

        private static void Prefix(HiddenRoomRewardSpawner __instance,
            out int __state)
        {
            __state = -1;
            int participantCount = ServerParticipantCountReader.Read();
            if (!MultiplayerRulesController.TryGetActiveOverride(
                    MultiplayerRuleId.HiddenRoomBreakableRewardCount,
                    participantCount,
                    out float configuredCount)) return;

            var previewRandom = new System.Random(__instance.RandomID);
            if (previewRandom.Next(0, 5) != MultiplayerBreakableRewardRoll) return;
            __state = Mathf.RoundToInt(configuredCount);
            HiddenRoomRewardReplacementContext.SuppressNativeBreakables = true;
        }

        private static void Postfix(HiddenRoomRewardSpawner __instance, int __state)
        {
            if (__state < 0) return;
            HiddenRoomRewardReplacementContext.SuppressNativeBreakables = false;
            if (SpawnPropMethod == null || __state == 0) return;

            var random = new System.Random(__instance.RandomID);
            random.Next(0, 5);
            HiddenRoomRewardReplacementContext.CreatingConfiguredBreakables = true;
            try
            {
                for (int index = 0; index < __state; index++)
                {
                    Vector3 position = __instance.transform.position +
                        (Vector3)HorayUtility.GetDiceAlignedPoint(__state, index) * 2.5f;
                    position.x += UnityEngine.Random.Range(-0.125f, 0.125f);
                    position.y += UnityEngine.Random.Range(-0.125f, 0.125f);
                    GameObject prefab = PropDatabase.GetRandomMPBreakable(random).propPrefab;
                    SpawnPropMethod.Invoke(__instance,
                        new object[] { prefab, position, random });
                }
            }
            finally
            {
                HiddenRoomRewardReplacementContext.CreatingConfiguredBreakables = false;
            }
        }

        private static Exception Finalizer(Exception __exception, int __state)
        {
            if (__state >= 0)
                HiddenRoomRewardReplacementContext.SuppressNativeBreakables = false;
            return __exception;
        }
    }

    [HarmonyPatch(typeof(HiddenRoomRewardSpawner), "SpawnProp")]
    internal static class HiddenRoomNativeBreakableSuppressionPatch
    {
        private static bool Prefix() =>
            !HiddenRoomRewardReplacementContext.SuppressNativeBreakables ||
            HiddenRoomRewardReplacementContext.CreatingConfiguredBreakables;
    }
}
