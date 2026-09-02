using HarmonyLib;
using Mirror;
using UnityEngine;

namespace SephiriaEnhancements.MultiplayerRules.Integration
{
    [HarmonyPatch(typeof(BossSpawner), nameof(BossSpawner.OnStartServer))]
    internal static class StandardBossRulesPatch
    {
        private const string NativeBossHealthPerExtraPlayer =
            "bossBonusHpByPlayerNumber";
        private const string NativeBossDamagePerExtraPlayer =
            "bossBonusDamageByPlayerNumber";

        private static void Postfix(BossSpawner __instance)
        {
            if (!NetworkServer.active || __instance.NetworkbossObject == null)
                return;

            UnitAvatar boss = __instance.NetworkbossObject;
            int participantCount = ServerParticipantCountReader.Read();
            // The native BossSpawner formula uses raw server connections.
            int nativeAdditionalConnections = NetworkServer.connections.Count - 1;
            if (MultiplayerRulesController.TryGetAuthoritativeActiveRules(
                    out ActiveExplorationMultiplayerRules activeRules) &&
                activeRules.Rules.Get(MultiplayerRuleId.StandardBossHealthMultiplier,
                    participantCount).TryGetOverride(
                        out float participantMultiplier))
            {
                float baseHealth = boss.maxHp;
                float nativeParticipantMultiplier = 1f +
                    nativeAdditionalConnections *
                    KeywordDatabase.GetConstValue(
                        NativeBossHealthPerExtraPlayer) / 100f;
                float nativeFinalMultiplier = baseHealth > 0f
                    ? boss.MaxHp / baseHealth : 1f;
                float otherModifierPercent =
                    (nativeFinalMultiplier / nativeParticipantMultiplier - 1f) *
                    100f;
                float healthRatio = boss.HpRatio;
                float configuredMultiplier = EnemyHealthRuleCalculator.Combine(
                    participantMultiplier, otherModifierPercent,
                    activeRules.HealthModifierCombination);
                boss.NetworkmaxHp = baseHealth * configuredMultiplier;
                boss.SetHp(boss.MaxHp * healthRatio);
            }

            if (MultiplayerRulesController.TryGetActiveOverride(
                    MultiplayerRuleId.BossEncounterDamageBonus, participantCount,
                    out float configuredDamageBonus))
            {
                int nativeDamageBonus = nativeAdditionalConnections *
                    KeywordDatabase.GetConstValue(
                        NativeBossDamagePerExtraPlayer);
                boss.AddCustomStat(ECustomStat.AllDamageBonus,
                    Mathf.RoundToInt(configuredDamageBonus) - nativeDamageBonus);
            }
        }
    }
}
