using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace SephiriaEnhancements.MultiplayerRules.Integration
{
    // Unit_QQBoss, Unit_QQQBoss and FallGuysAttack are native API contracts.
    // Player-facing and domain names use the game's Qliphoth terminology.
    [HarmonyPatch(typeof(Unit_QQBoss), nameof(Unit_QQBoss.Seal))]
    internal static class QliphothSealRulePatch
    {
        private static void Prefix(Unit_QQBoss __instance,
            out ArrayElementRestore __state)
        {
            __state = null;
            int participantCount = ServerParticipantCountReader.Read();
            if (!MultiplayerRulesController.TryGetActiveOverride(
                    MultiplayerRuleId.QliphothSealTeamMultiplier, participantCount,
                    out float configuredMultiplier) ||
                __instance.sealTeamMultipliers == null ||
                __instance.sealTeamMultipliers.Length == 0)
                return;

            // Native Seal indexes by PlayerSpawner.MultiplayerList.Count.
            int nativeParticipantCount = Mathf.Clamp(
                PlayerSpawner.MultiplayerList.Count, 1,
                __instance.sealTeamMultipliers.Length);
            int nativeParticipantIndex = nativeParticipantCount - 1;
            float nativeAdjustedMultiplier = configuredMultiplier *
                nativeParticipantCount / participantCount;
            float previous = __instance.sealTeamMultipliers[nativeParticipantIndex];
            __instance.sealTeamMultipliers[nativeParticipantIndex] =
                nativeAdjustedMultiplier;
            __state = new ArrayElementRestore(__instance.sealTeamMultipliers,
                nativeParticipantIndex, previous);
        }

        private static Exception Finalizer(Exception __exception,
            ArrayElementRestore __state)
        {
            __state?.Dispose();
            return __exception;
        }

        internal sealed class ArrayElementRestore : IDisposable
        {
            private readonly float[] values;
            private readonly int index;
            private readonly float previous;

            internal ArrayElementRestore(float[] values, int index, float previous)
            {
                this.values = values;
                this.index = index;
                this.previous = previous;
            }

            public void Dispose() => values[index] = previous;
        }
    }

    [HarmonyPatch(typeof(Unit_QQQBoss), nameof(Unit_QQQBoss.FallGuysAttack))]
    internal static class QliphothFinalBattleGridRulePatch
    {
        private static void Prefix(Unit_QQQBoss __instance,
            out RegionCountRestore __state)
        {
            __state = null;
            if (!MultiplayerRulesController.TryGetActiveOverride(
                    MultiplayerRuleId.QliphothFinalBattleGridRegionCount,
                    ServerParticipantCountReader.Read(), out float configuredCount))
                return;

            __state = new RegionCountRestore(__instance,
                __instance.fallGuysRegionCount,
                __instance.fallGuysRegionCountMultiplayer);
            int count = Mathf.RoundToInt(configuredCount);
            __instance.fallGuysRegionCount = count;
            __instance.fallGuysRegionCountMultiplayer = count;
        }

        private static Exception Finalizer(Exception __exception,
            RegionCountRestore __state)
        {
            __state?.Dispose();
            return __exception;
        }

        internal sealed class RegionCountRestore : IDisposable
        {
            private readonly Unit_QQQBoss boss;
            private readonly int single;
            private readonly int multiplayer;

            internal RegionCountRestore(Unit_QQQBoss boss, int single,
                int multiplayer)
            {
                this.boss = boss;
                this.single = single;
                this.multiplayer = multiplayer;
            }

            public void Dispose()
            {
                boss.fallGuysRegionCount = single;
                boss.fallGuysRegionCountMultiplayer = multiplayer;
            }
        }
    }

    [HarmonyPatch(typeof(QTempleTrioAIController), "OnPhaseSpawnEnd",
        new[] { typeof(List<UnitAvatar>) })]
    internal static class QliphothTempleTrioActiveCountRulePatch
    {
        // These private members are native integration contracts.
        private static readonly FieldInfo AisField = AccessTools.Field(
            typeof(QTempleTrioAIController), "ais");
        private static readonly FieldInfo IsFullPartyField = AccessTools.Field(
            typeof(QTempleTrioAIController), "isFullParty");
        private static readonly MethodInfo SetAiStateMethod = AccessTools.Method(
            typeof(QTempleTrioAIController), "SetAIState",
            new[] { typeof(IQTempleTrioAI), typeof(bool) });

        private static void Prefix(QTempleTrioAIController __instance,
            out int __state)
        {
            __state = -1;
            if (MultiplayerRulesController.TryGetActiveOverride(
                    MultiplayerRuleId.QliphothTempleTrioActiveCount,
                    ServerParticipantCountReader.Read(), out float configuredCount))
            {
                __state = Mathf.RoundToInt(configuredCount);
                __instance.activeCount = __state;
            }
        }

        private static void Postfix(QTempleTrioAIController __instance, int __state)
        {
            if (__state < 0) return;
            var ais = AisField?.GetValue(__instance) as List<IQTempleTrioAI>;
            if (ais == null || SetAiStateMethod == null) return;
            int activeCount = Mathf.Clamp(__state, 1, ais.Count);
            __instance.activeCount = activeCount;
            IsFullPartyField?.SetValue(__instance, activeCount >= ais.Count);
            if (activeCount >= ais.Count) return;

            foreach (IQTempleTrioAI ai in ais)
                SetAiStateMethod.Invoke(__instance, new object[] { ai, true });
            ais.Shuffle();
            for (int index = 0; index < ais.Count - activeCount; index++)
                SetAiStateMethod.Invoke(__instance,
                    new object[] { ais[index], false });
        }
    }

    [HarmonyPatch(typeof(Unit_QQQBoss), nameof(Unit_QQQBoss.InAndOutAttack),
        new[] { typeof(Unit_QQQBoss.EInAndOutAttackType), typeof(Transform) })]
    internal static class QliphothFinalBattleEntryTrackingRulePatch
    {
        private static void Prefix(ref Unit_QQQBoss.EInAndOutAttackType attackType)
        {
            if (!MultiplayerRulesController.TryGetActiveOverride(
                    MultiplayerRuleId.QliphothFinalBattleEntryAttackTracksParticipant,
                    ServerParticipantCountReader.Read(),
                    out float tracksParticipant))
                return;
            if (attackType == Unit_QQQBoss.EInAndOutAttackType.Target ||
                attackType == Unit_QQQBoss.EInAndOutAttackType.RandomPos)
            {
                attackType = tracksParticipant > 0f
                    ? Unit_QQQBoss.EInAndOutAttackType.Target
                    : Unit_QQQBoss.EInAndOutAttackType.RandomPos;
            }
        }
    }
}
