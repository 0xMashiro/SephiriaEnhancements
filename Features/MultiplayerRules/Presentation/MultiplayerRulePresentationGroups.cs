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
            G(MultiplayerRulesLocalization.GroupEnemyHealth,
                MultiplayerRuleId.RegularEnemyHealthMultiplier,
                MultiplayerRuleId.EliteEnemyHealthMultiplier,
                MultiplayerRuleId.StandardBossHealthMultiplier,
                MultiplayerRuleId.RandomEncounterHealthMultiplier,
                MultiplayerRuleId.KrazBossHealthMultiplier,
                MultiplayerRuleId.MindEaterRootSummonHealthMultiplier),
            G(MultiplayerRulesLocalization.GroupEnemyDamage,
                MultiplayerRuleId.RegularEnemyDamageBonus,
                MultiplayerRuleId.EliteEnemyDamageBonus,
                MultiplayerRuleId.BossEncounterDamageBonus,
                MultiplayerRuleId.RandomEncounterDamageBonus,
                MultiplayerRuleId.MindEaterRootSummonDamageBonus),
            G(MultiplayerRulesLocalization.GroupSpawnAndDifficulty,
                MultiplayerRuleId.MonsterSpawnEntryMultiplier,
                MultiplayerRuleId.EnemyGroupDifficultyOffset,
                MultiplayerRuleId.RandomEncounterLivingEnemyLimit),
            G(MultiplayerRulesLocalization.GroupRewardsAndSupplies,
                MultiplayerRuleId.TargetedExperienceOrbDivisor,
                MultiplayerRuleId.SharedMoneyAwardFactorPerParticipant,
                MultiplayerRuleId.FestivalOfBloodEnemyHealingMultiplier,
                MultiplayerRuleId.HiddenRoomBreakableRewardCount,
                MultiplayerRuleId.ExplorationFloorHealingSupply),
            G(MultiplayerRulesLocalization.GroupMerchants,
                MultiplayerRuleId.WanderingMerchantArtifactCandidateBonus,
                MultiplayerRuleId.WanderingMerchantTabletCandidateCount,
                MultiplayerRuleId.MerchantGuildArtifactCandidateBonus,
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
