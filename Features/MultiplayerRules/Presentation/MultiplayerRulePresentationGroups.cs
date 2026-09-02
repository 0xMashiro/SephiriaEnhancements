using System.Collections.Generic;

namespace SephiriaEnhancements.MultiplayerRules.Presentation
{
    internal readonly struct MultiplayerRulePresentationGroup
    {
        internal MultiplayerRulePresentationGroup(string localizationKey,
            params MultiplayerRuleId[] ruleIds)
        {
            LocalizationKey = localizationKey;
            RuleIds = ruleIds;
        }

        internal string LocalizationKey { get; }
        internal IReadOnlyList<MultiplayerRuleId> RuleIds { get; }
    }

    internal static class MultiplayerRulePresentationGroups
    {
        private static readonly MultiplayerRulePresentationGroup[] Groups =
        {
            G(MultiplayerRulesLocalization.GroupSpawnAndDifficulty,
                MultiplayerRuleId.MonsterSpawnEntryMultiplier,
                MultiplayerRuleId.EnemyGroupDifficultyOffset),
            G(MultiplayerRulesLocalization.GroupEnemyStats,
                MultiplayerRuleId.RegularEnemyHealthMultiplier,
                MultiplayerRuleId.RegularEnemyDamageBonus,
                MultiplayerRuleId.EliteEnemyHealthMultiplier,
                MultiplayerRuleId.EliteEnemyDamageBonus),
            G(MultiplayerRulesLocalization.GroupEncountersAndBosses,
                MultiplayerRuleId.StandardBossHealthMultiplier,
                MultiplayerRuleId.BossEncounterDamageBonus,
                MultiplayerRuleId.RandomEncounterHealthMultiplier,
                MultiplayerRuleId.RandomEncounterDamageBonus,
                MultiplayerRuleId.RandomEncounterLivingEnemyLimit,
                MultiplayerRuleId.SeedEncounterBossHealthMultiplier,
                MultiplayerRuleId.MindEaterRootSummonHealthMultiplier,
                MultiplayerRuleId.MindEaterRootSummonDamageBonus),
            G(MultiplayerRulesLocalization.GroupRewardsAndSupplies,
                MultiplayerRuleId.TargetedExperienceOrbDivisor,
                MultiplayerRuleId.SharedMoneyAwardFactorPerParticipant,
                MultiplayerRuleId.FestivalOfBloodEnemyHealingMultiplier,
                MultiplayerRuleId.HiddenRoomBreakableRewardCount,
                MultiplayerRuleId.LifeSupplyOnPositiveProgressFloor),
            G(MultiplayerRulesLocalization.GroupMerchants,
                MultiplayerRuleId.WanderingMerchantCharmCandidateBonus,
                MultiplayerRuleId.WanderingMerchantTabletCandidateCount,
                MultiplayerRuleId.MerchantGuildCharmCandidateBonus,
                MultiplayerRuleId.MerchantGuildTabletCandidateCount,
                MultiplayerRuleId.RestorativePotionQuantity,
                MultiplayerRuleId.RegenerationSamplePotionQuantity),
            G(MultiplayerRulesLocalization.GroupQliphoth,
                MultiplayerRuleId.QliphothSealTeamMultiplier,
                MultiplayerRuleId.QliphothFinalBattleGridRegionCount,
                MultiplayerRuleId.QliphothFinalBattleEntryAttackTracksParticipant,
                MultiplayerRuleId.QliphothTempleTrioActiveCount)
        };

        internal static IReadOnlyList<MultiplayerRulePresentationGroup> All => Groups;

        private static MultiplayerRulePresentationGroup G(string localizationKey,
            params MultiplayerRuleId[] ruleIds) =>
            new MultiplayerRulePresentationGroup(localizationKey, ruleIds);
    }
}
