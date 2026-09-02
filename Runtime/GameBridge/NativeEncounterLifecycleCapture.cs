#nullable disable
using SephiriaEnhancements.Runtime.Inventory;

using System;
using HarmonyLib;
using UnityEngine;

namespace SephiriaEnhancements.Runtime
{
    internal static class NativeEncounterLifecycleCapture
    {
        private static Action<EncounterLifecycleObservation> observer;
        private static BossSpawner activeBossSpawner;
        private static SeedBossSpawner activeSeedBossSpawner;

        internal static void SetObserver(
            Action<EncounterLifecycleObservation> value)
        {
            observer = value;
            if (value == null)
            {
                ResetGameplayContext();
            }
        }

        internal static void ResetGameplayContext()
        {
            activeBossSpawner = null;
            activeSeedBossSpawner = null;
        }

        internal static bool IsTrackedBossTarget(UnitAvatar target)
        {
            UnitAvatar spawnedBoss = activeBossSpawner != null
                ? activeBossSpawner.NetworkbossObject
                : activeSeedBossSpawner != null
                    ? activeSeedBossSpawner.NetworkbossObject
                    : null;
            UnitAvatar current = target;
            for (int depth = 0; current != null && depth < 8; depth++)
            {
                if (current == spawnedBoss && spawnedBoss != null)
                {
                    return true;
                }

                UnitAvatar leader = current.NetworkLeader;
                if (leader == null || leader == current)
                {
                    break;
                }
                current = leader;
            }
            return false;
        }

        internal static void ReportOrdinaryCleared(NetworkAreaProp source,
            bool oldValue, bool newValue)
        {
            if (oldValue || !newValue || !IsOrdinarySpawner(source))
            {
                return;
            }

            Publish(EncounterKind.Ordinary, EncounterTransition.Cleared,
                source.GetInstanceID());
        }

        internal static void ReportStarted(BossSpawner source)
        {
            if (source == null) return;
            activeBossSpawner = source;
            activeSeedBossSpawner = null;
            Publish(EncounterKind.Boss, EncounterTransition.Started,
                source.GetInstanceID());
        }

        internal static void ReportStarted(SeedBossSpawner source)
        {
            if (source == null) return;
            activeBossSpawner = null;
            activeSeedBossSpawner = source;
            Publish(EncounterKind.Boss, EncounterTransition.Started,
                source.GetInstanceID());
        }

        internal static void ReportPaused(BossSpawner source)
        {
            PublishBoss(source, EncounterTransition.Paused);
        }

        internal static void ReportResumed(BossSpawner source)
        {
            PublishBoss(source, EncounterTransition.Resumed);
        }

        internal static void ReportCompletionStarted(BossSpawner source)
        {
            PublishBoss(source, EncounterTransition.CompletionStarted);
        }

        internal static void ReportCompletionStarted(SeedBossSpawner source)
        {
            PublishBoss(source, EncounterTransition.CompletionStarted);
        }

        internal static void ReportCompleted(BossSpawner source)
        {
            if (source == null) return;
            BossSpawner continuation = FindContinuation(source);
            if (continuation != null)
            {
                activeBossSpawner = continuation;
                activeSeedBossSpawner = null;
                Publish(EncounterKind.Boss,
                    EncounterTransition.ContinuationPrepared,
                    source.GetInstanceID(), continuation.GetInstanceID());
                return;
            }

            activeBossSpawner = null;
            activeSeedBossSpawner = null;
            Publish(EncounterKind.Boss, EncounterTransition.Cleared,
                source.GetInstanceID());
        }

        internal static void ReportCompleted(SeedBossSpawner source)
        {
            if (source == null) return;
            activeBossSpawner = null;
            activeSeedBossSpawner = null;
            Publish(EncounterKind.Boss, EncounterTransition.Cleared,
                source.GetInstanceID());
        }

        internal static void ReportDefeated(BossSpawner source)
        {
            if (source == null) return;
            activeBossSpawner = null;
            activeSeedBossSpawner = null;
            Publish(EncounterKind.Boss, EncounterTransition.Defeated,
                source.GetInstanceID());
        }

        internal static void ReportDefeated(SeedBossSpawner source)
        {
            if (source == null) return;
            activeBossSpawner = null;
            activeSeedBossSpawner = null;
            Publish(EncounterKind.Boss, EncounterTransition.Defeated,
                source.GetInstanceID());
        }

        private static bool IsOrdinarySpawner(NetworkAreaProp source) =>
            source is EnemySpawner || source is CommonEnemySpawner ||
            source is RandomEnemyPhaseSpawner;

        private static void PublishBoss(BossSpawner source,
            EncounterTransition transition)
        {
            if (source != null)
            {
                Publish(EncounterKind.Boss, transition,
                    source.GetInstanceID());
            }
        }

        private static void PublishBoss(SeedBossSpawner source,
            EncounterTransition transition)
        {
            if (source != null)
            {
                Publish(EncounterKind.Boss, transition,
                    source.GetInstanceID());
            }
        }

        private static void Publish(EncounterKind kind,
            EncounterTransition transition, int sourceInstanceId,
            int relatedSourceInstanceId = 0)
        {
            observer?.Invoke(new EncounterLifecycleObservation(kind,
                transition, sourceInstanceId, relatedSourceInstanceId,
                Time.unscaledTime));
        }

        private static BossSpawner FindContinuation(BossSpawner source)
        {
            UnitAI_BossBasic bossAI = source.NetworkbossAI;
            BossEnvironment_QQBoss environment =
                bossAI?.Environment as BossEnvironment_QQBoss;
            BossSpawner continuation = environment?.qqqBossSpawner;
            return continuation != null && continuation != source
                ? continuation
                : null;
        }
    }

    [HarmonyPatch(typeof(NetworkAreaProp), "HookMapElementUsed")]
    internal static class NativeOrdinaryEncounterClearedPatch
    {
        private static void Postfix(NetworkAreaProp __instance,
            bool oldValue, bool newValue) =>
            NativeEncounterLifecycleCapture.ReportOrdinaryCleared(__instance,
                oldValue, newValue);
    }

    [HarmonyPatch(typeof(BossSpawner), "UserCode_RpcStartBattle")]
    internal static class NativeBossEncounterStartedPatch
    {
        private static void Postfix(BossSpawner __instance) =>
            NativeEncounterLifecycleCapture.ReportStarted(__instance);
    }

    [HarmonyPatch(typeof(BossSpawner), "UserCode_RpcStopBattle")]
    internal static class NativeBossEncounterDefeatedPatch
    {
        private static void Postfix(BossSpawner __instance) =>
            NativeEncounterLifecycleCapture.ReportDefeated(__instance);
    }

    [HarmonyPatch(typeof(BossSpawner), "UserCode_RpcByeBegin__Boolean")]
    internal static class NativeBossEncounterCompletionStartedPatch
    {
        private static void Postfix(BossSpawner __instance) =>
            NativeEncounterLifecycleCapture.ReportCompletionStarted(__instance);
    }

    [HarmonyPatch(typeof(BossSpawner), "UserCode_RpcByeEnd")]
    internal static class NativeBossEncounterCompletedPatch
    {
        private static void Postfix(BossSpawner __instance) =>
            NativeEncounterLifecycleCapture.ReportCompleted(__instance);
    }

    [HarmonyPatch(typeof(BossSpawner), "UserCode_RpcPhaseChangeBegin")]
    internal static class NativeBossEncounterPausedPatch
    {
        private static void Postfix(BossSpawner __instance) =>
            NativeEncounterLifecycleCapture.ReportPaused(__instance);
    }

    [HarmonyPatch(typeof(BossSpawner), "UserCode_RpcPhaseChangeEnd")]
    internal static class NativeBossEncounterResumedPatch
    {
        private static void Postfix(BossSpawner __instance) =>
            NativeEncounterLifecycleCapture.ReportResumed(__instance);
    }

    [HarmonyPatch(typeof(SeedBossSpawner), "UserCode_RpcStartBattle")]
    internal static class NativeSeedBossEncounterStartedPatch
    {
        private static void Postfix(SeedBossSpawner __instance) =>
            NativeEncounterLifecycleCapture.ReportStarted(__instance);
    }

    [HarmonyPatch(typeof(SeedBossSpawner), "UserCode_RpcStopBattle")]
    internal static class NativeSeedBossEncounterDefeatedPatch
    {
        private static void Postfix(SeedBossSpawner __instance) =>
            NativeEncounterLifecycleCapture.ReportDefeated(__instance);
    }

    [HarmonyPatch(typeof(SeedBossSpawner), "UserCode_RpcByeBegin")]
    internal static class NativeSeedBossEncounterCompletionStartedPatch
    {
        private static void Postfix(SeedBossSpawner __instance) =>
            NativeEncounterLifecycleCapture.ReportCompletionStarted(__instance);
    }

    [HarmonyPatch(typeof(SeedBossSpawner), "UserCode_RpcByeEnd")]
    internal static class NativeSeedBossEncounterCompletedPatch
    {
        private static void Postfix(SeedBossSpawner __instance) =>
            NativeEncounterLifecycleCapture.ReportCompleted(__instance);
    }
}
