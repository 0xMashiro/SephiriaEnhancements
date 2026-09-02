using System;
using System.Reflection;
using HarmonyLib;
using SephiriaEnhancements.Combat;
using SephiriaEnhancements.Configuration;
using SephiriaEnhancements.Core;
using SephiriaEnhancements.Diagnostics;
using SephiriaEnhancements.Runtime;
using UnityEngine;

namespace SephiriaEnhancements.Integration
{
    [HarmonyPatch]
    internal static class DamageFeedbackCapture
    {
        private const string RpcHandlerPrefix =
            "UserCode_RpcShowAllDamageParticles__DamageFeedback";

        private static CombatInsightsController controller;

        internal static void SetController(CombatInsightsController value)
        {
            controller = value;
        }

        private static MethodBase TargetMethod()
        {
            foreach (MethodInfo method in typeof(UnitAvatar).GetMethods(
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            {
                ParameterInfo[] parameters = method.GetParameters();
                if (method.Name.StartsWith(RpcHandlerPrefix, StringComparison.Ordinal) &&
                    parameters.Length == 1 &&
                    parameters[0].ParameterType == typeof(DamageFeedback[]))
                {
                    return method;
                }
            }

            throw new MissingMethodException(typeof(UnitAvatar).FullName, RpcHandlerPrefix);
        }

        private static void Postfix(UnitAvatar __instance, DamageFeedback[] __0)
        {
            DamageFeedback[] damageFeedbacks = __0;
            if (controller == null || damageFeedbacks == null)
            {
                return;
            }

            bool suiteEnabled = EnhancementsSettings.Enabled;
            bool captureStatistics = suiteEnabled &&
                ModSettings.DisplayPolicy !=
                CombatInsightsDisplayPolicy.Disabled;
            bool captureOrdinary = captureStatistics &&
                ModSettings.DisplayPolicy !=
                CombatInsightsDisplayPolicy.BossOnly;
            bool logDamage = DeveloperLogger.IsEnabled;
            bool captureHitStreakFeedback = suiteEnabled && ModSettings.HitStreakFeedback;
            if (!captureStatistics && !logDamage && !captureHitStreakFeedback)
            {
                return;
            }

            foreach (DamageFeedback feedback in damageFeedbacks)
            {
                if (feedback == null)
                {
                    continue;
                }

                UnitAvatar attacker = feedback.attacker;
                PlayerAvatar owner = ResolvePlayer(attacker);

                if (logDamage)
                {
                    DeveloperLogger.RecordDamageFeedback(feedback, owner);
                }

                if (captureHitStreakFeedback && owner != null)
                {
                    controller.RecordHitStreakFeedback(feedback, owner);
                }

                if (feedback.damageValue <= 0)
                {
                    continue;
                }

                UnitAvatar target = feedback.self != null ? feedback.self : __instance;
                EncounterDamageType damageType = captureStatistics &&
                    owner != null
                        ? controller.ResolveDamageType(target, feedback)
                        : EncounterDamageType.Unknown;
                if (captureOrdinary && owner != null && feedback.msgType <=
                    (byte)DamageFeedback.EMsgType.Execution)
                {
                    controller.RecordCombatDamage(target, owner,
                        feedback.damageValue, damageType);
                }

                if (!IsBossTarget(target))
                {
                    continue;
                }

                if (attacker == null)
                {
                    continue;
                }

                if (captureStatistics && owner != null &&
                    controller.EnsureBossEncounterFromDamage())
                {
                    controller.RecordBossDamage(owner, feedback.damageValue,
                        damageType);
                }
            }
        }

        private static PlayerAvatar ResolvePlayer(UnitAvatar unit)
        {
            UnitAvatar current = unit;
            for (int depth = 0; current != null && depth < 8; depth++)
            {
                if (current is PlayerAvatar player)
                {
                    return player;
                }

                UnitAvatar leader = current.NetworkLeader;
                if (leader == null || leader == current)
                {
                    break;
                }
                current = leader;
            }
            return null;
        }

        private static bool IsBossTarget(UnitAvatar target)
        {
            if (target == null || target is PlayerAvatar)
            {
                return false;
            }

            // Retain the damage-first fallback for builds where the native start RPC is missed.
            if (target.monsterType == EMonsterType.Boss)
            {
                return true;
            }

            // Multi-phase bosses can replace their avatar or expose a subordinate
            // damageable object. The reusable native lifecycle bridge owns that
            // game-specific identity mapping.
            return NativeEncounterLifecycleCapture.IsTrackedBossTarget(target);
        }
    }
}
