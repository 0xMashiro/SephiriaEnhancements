namespace SephiriaEnhancements.MultiplayerRules
{
    internal enum EnemyHealthCategory
    {
        Regular,
        Elite
    }

    internal static class EnemyHealthRuleResolver
    {
        internal static bool TryResolveMultiplier(
            ActiveExplorationMultiplayerRules activeRules,
            EnemySpawnOrigin spawnOrigin,
            EnemyHealthCategory healthCategory,
            int participantCount,
            float otherModifierPercent,
            out float multiplier)
        {
            multiplier = 1f;
            if (activeRules == null)
            {
                return false;
            }

            MultiplayerRuleId ruleId = SelectRuleId(
                spawnOrigin, healthCategory);
            if (!activeRules.Rules.Get(ruleId, participantCount).TryGetOverride(
                    out float participantMultiplier))
            {
                return false;
            }

            multiplier = EnemyHealthRuleCalculator.Combine(participantMultiplier,
                otherModifierPercent,
                activeRules.HealthModifierCombination);
            return true;
        }

        private static MultiplayerRuleId SelectRuleId(
            EnemySpawnOrigin spawnOrigin,
            EnemyHealthCategory healthCategory)
        {
            switch (spawnOrigin)
            {
                case EnemySpawnOrigin.RandomEncounter:
                    return MultiplayerRuleId.RandomEncounterHealthMultiplier;
                case EnemySpawnOrigin.StandardBoss:
                    return MultiplayerRuleId.StandardBossHealthMultiplier;
                case EnemySpawnOrigin.SeedEncounterBoss:
                    return MultiplayerRuleId.SeedEncounterBossHealthMultiplier;
                case EnemySpawnOrigin.MindEaterRootSummon:
                    return MultiplayerRuleId.MindEaterRootSummonHealthMultiplier;
                default:
                    return healthCategory == EnemyHealthCategory.Regular
                        ? MultiplayerRuleId.RegularEnemyHealthMultiplier
                        : MultiplayerRuleId.EliteEnemyHealthMultiplier;
            }
        }
    }
}
