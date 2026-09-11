using SephiriaEnhancements.Configuration;
using SephiriaEnhancements.MultiplayerRules.Presentation;

namespace SephiriaEnhancements.MultiplayerRules.Integration
{
    internal static class NativeRuleReference
    {
        // These database keys are native API names. Values describe the player-count
        // contribution, not final health or damage after other encounter modifiers.
        internal static bool TryRead(MultiplayerRuleId id, int players, out float value)
        {
            value = 0;
            string key;
            switch (id)
            {
                case MultiplayerRuleId.MonsterSpawnEntryMultiplier:
                    value = players == 1 ? 1f : players == 2 ? 1.45f : 1.9f; return true;
                case MultiplayerRuleId.EnemyGroupDifficultyOffset:
                    value = players == 1 ? 0 : players < 4 ? 1 : 2; return true;
                case MultiplayerRuleId.TargetedExperienceOrbDivisor:
                    value = System.Math.Min(players, 3); return true;
                case MultiplayerRuleId.SharedMoneyAwardFactorPerParticipant:
                    value = players == 1 ? 10 : players == 2 ? 8 : players == 3 ? 7 : 5; return true;
                case MultiplayerRuleId.FestivalOfBloodEnemyHealingMultiplier:
                    value = players == 1 ? 1f : players == 2 ? 0.66f : players == 3 ? 0.5f : 0.33f; return true;
                case MultiplayerRuleId.WanderingMerchantArtifactCandidateBonus:
                case MultiplayerRuleId.MerchantGuildArtifactCandidateBonus:
                    value = players - 1; return true;
                case MultiplayerRuleId.RandomEncounterLivingEnemyLimit:
                    value = players <= 2 ? 5 : players == 3 ? 6 : 7; return true;
                case MultiplayerRuleId.RegularEnemyHealthMultiplier: key = "enemyBonusHpByPlayerNumber"; break;
                case MultiplayerRuleId.EliteEnemyHealthMultiplier: key = "minibossBonusHpByPlayerNumber"; break;
                case MultiplayerRuleId.StandardBossHealthMultiplier: key = "bossBonusHpByPlayerNumber"; break;
                case MultiplayerRuleId.RegularEnemyDamageBonus: key = "enemyBonusDamageByPlayerNumber"; break;
                case MultiplayerRuleId.EliteEnemyDamageBonus: key = "minibossBonusDamageByPlayerNumber"; break;
                case MultiplayerRuleId.RandomEncounterHealthMultiplier:
                    if (players >= 4) { value = 1.1f; return true; }
                    key = "miniEnemyBonusHpByPlayerNumber";
                    break;
                case MultiplayerRuleId.RandomEncounterDamageBonus:
                    key = "miniEnemyBonusDamageByPlayerNumber";
                    break;
                case MultiplayerRuleId.MindEaterRootSummonHealthMultiplier:
                    value = 1f + (players - 1) * (players < 4 ? 0.1f : 0.09f);
                    return true;
                case MultiplayerRuleId.MindEaterRootSummonDamageBonus:
                    value = 5 * (players - 1);
                    return true;
                default: return false;
            }
            if (!KeywordDatabase.TryGetConstValue(key, out int perExtraPlayer, true)) return false;
            value = perExtraPlayer * (players - 1);
            if (MultiplayerRuleCatalog.Get(id).Unit == MultiplayerRuleUnit.Multiplier)
                value = 1f + value / 100f;
            return true;
        }

        internal static string Describe(MultiplayerRuleDefinition definition, int players)
        {
            if (definition.Id == MultiplayerRuleId.BossEncounterDamageBonus &&
                KeywordDatabase.TryGetConstValue("bossBonusDamageByPlayerNumber", out int bonus, true))
                return string.Format(ModLocalization.Get(MultiplayerRulesLocalization.BossReference),
                    MultiplayerRulesLocalization.FormatValue(bonus * (players - 1), definition.Unit));
            return TryRead(definition.Id, players, out float value)
                ? MultiplayerRulesLocalization.FormatValue(value, definition.Unit)
                : ModLocalization.Get(MultiplayerRulesLocalization.VariableReference);
        }
    }
}
