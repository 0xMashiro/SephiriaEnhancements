using SephiriaEnhancements.Runtime;
using System;
using System.Collections.Generic;
using HarmonyLib;
using SephiriaEnhancements.Runtime.Execution;
using UnityEngine;

namespace SephiriaEnhancements.MultiplayerRules.Integration
{
    [HarmonyPatch(typeof(MonsterSpawnPhase), nameof(MonsterSpawnPhase.GenerateSpawnData))]
    internal static class MonsterSpawnEntryMultiplierPatch
    {
        private static bool Prefix(MonsterSpawnPhase __instance, int multiplayerCount, int proliferate, ref MonsterSpawnPhase __result)
        {
            if (!FeatureFailure.IsAvailable(FeatureId.MultiplayerRules))
            {
                return true;
            }

            var original___result = __result;
            try
            {
                return PrefixCore(__instance, multiplayerCount, proliferate, ref __result);
            }
            catch (System.Exception exception)
            {
                __result = original___result;
                FeatureFailure.Disable(FeatureId.MultiplayerRules, exception);
                return true;
            }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static bool PrefixCore(MonsterSpawnPhase __instance, int multiplayerCount, int proliferate, ref MonsterSpawnPhase __result)
        {
            // multiplayerCount is the native API parameter name. Domain code uses
            // participantCount so the native name does not leak past this boundary.
            int participantCount = ServerParticipantCountReader.Read();
            if (!MultiplayerRulesController.TryGetActiveOverride(MultiplayerRuleId.MonsterSpawnEntryMultiplier, participantCount, out float configuredMultiplier))
            {
                return true;
            }

            float effectiveMultiplier = configuredMultiplier * (1f + proliferate / 100f);
            var generated = new MonsterSpawnPhase
            {
                clearType = __instance.clearType,
                spawnDatas = new List<MonsterSpawnData>()
            };
            int generatedEntryCount = Mathf.RoundToInt(__instance.spawnDatas.Count * effectiveMultiplier);
            for (int index = 0; index < generatedEntryCount; index++)
            {
                generated.spawnDatas.Add(__instance.spawnDatas.SafeRandomAccess(index, ArrayExtensions.ERandomAccessType.Repeat).GetClone());
            }

            foreach (MonsterSpawnData spawnData in generated.spawnDatas)
            {
                spawnData.moneyDropPercent /= effectiveMultiplier;
            }

            __result = generated;
            return false;
        }
    }

    [HarmonyPatch(typeof(Exp), nameof(Exp.OnStartServer))]
    internal static class TargetedExperienceOrbDivisorPatch
    {
        private static void Prefix(Exp __instance, out int __state)
        {
            __state = default;
            if (!FeatureFailure.IsAvailable(FeatureId.MultiplayerRules))
            {
                return;
            }

            try
            {
                PrefixCore(__instance, out __state);
            }
            catch (System.Exception exception)
            {
                FeatureFailure.Disable(FeatureId.MultiplayerRules, exception);
                return;
            }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static void PrefixCore(Exp __instance, out int __state)
        {
            __state = __instance.amount;
        }

        private static void Postfix(Exp __instance, int __state)
        {
            if (!FeatureFailure.IsAvailable(FeatureId.MultiplayerRules)) return;
            try
            {
                PostfixCore(__instance, __state);
            }
            catch (System.Exception exception)
            {
                FeatureFailure.Disable(FeatureId.MultiplayerRules, exception);
                return;
            }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static void PostfixCore(Exp __instance, int __state)
        {
            if (__instance.ignoreAdjustment || !MultiplayerRulesController.TryGetActiveOverride(MultiplayerRuleId.TargetedExperienceOrbDivisor, ServerParticipantCountReader.Read(), out float divisor))
            {
                return;
            }

            __instance.amount = Mathf.RoundToInt(__state / divisor);
        }
    }

    internal static class MoneyAwardContext
    {
        internal static IDisposable Enter(Money money, int factor)
        {
            return AmbientExecutionContext<Frame>.Enter(
                new Frame(money, factor));
        }

        internal static bool TryCalculate(PlayerAvatar player, out int amount)
        {
            amount = 0;
            Frame current = AmbientExecutionContext<Frame>.Current;
            if (current == null) return false;
            amount = current.Money.containedMoney * current.Factor;
            int bonus = player.GetCustomStat(ECustomStat.MoneyDrop);
            if (bonus > 0) amount += (int)(amount * (bonus / 100f));
            return true;
        }

        private sealed class Frame
        {
            internal Frame(Money money, int factor)
            {
                Money = money;
                Factor = factor;
            }

            internal Money Money { get; }
            internal int Factor { get; }
        }
    }

    [HarmonyPatch(typeof(Money), "AddToInventory")]
    internal static class MoneyAwardRulePatch
    {
        private static void Prefix(Money __instance, out IDisposable __state)
        {
            __state = default;
            if (!FeatureFailure.IsAvailable(FeatureId.MultiplayerRules))
            {
                return;
            }

            try
            {
                PrefixCore(__instance, out __state);
            }
            catch (System.Exception exception)
            {
                FeatureFailure.Disable(FeatureId.MultiplayerRules, exception);
                return;
            }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static void PrefixCore(Money __instance, out IDisposable __state)
        {
            __state = null;
            if (MultiplayerRulesController.TryGetActiveOverride(MultiplayerRuleId.SharedMoneyAwardFactorPerParticipant, ServerParticipantCountReader.Read(), out float factor))
            {
                __state = MoneyAwardContext.Enter(__instance, Mathf.RoundToInt(factor));
            }
        }

        private static Exception Finalizer(Exception __exception, IDisposable __state)
        {
            try
            {
                return FinalizerCore(__exception, __state);
            }
            catch (System.Exception exception)
            {
                FeatureFailure.Disable(FeatureId.MultiplayerRules, exception);
                return __exception;
            }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static Exception FinalizerCore(Exception __exception, IDisposable __state)
        {
            __state?.Dispose();
            return __exception;
        }
    }

    [HarmonyPatch(typeof(UnitAvatar), nameof(UnitAvatar.AddMoney),
        new[] { typeof(int) })]
    internal static class PlayerMoneyAwardAmountPatch
    {
        private static void Prefix(UnitAvatar __instance, ref int m)
        {
            if (!FeatureFailure.IsAvailable(FeatureId.MultiplayerRules))
            {
                return;
            }

            var original_m = m;
            try
            {
                PrefixCore(__instance, ref m);
            }
            catch (System.Exception exception)
            {
                m = original_m;
                FeatureFailure.Disable(FeatureId.MultiplayerRules, exception);
                return;
            }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static void PrefixCore(UnitAvatar __instance, ref int m)
        {
            // m is the native API parameter name. amount remains the domain term.
            if (__instance is PlayerAvatar player && MoneyAwardContext.TryCalculate(player, out int configuredAmount))
            {
                m = configuredAmount;
            }
        }
    }
}
