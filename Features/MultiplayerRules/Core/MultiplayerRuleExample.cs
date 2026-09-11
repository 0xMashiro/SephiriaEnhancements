namespace SephiriaEnhancements.MultiplayerRules
{
    internal static class MultiplayerRuleExample
    {
        // Explicit illustrative inputs, never presented as a live enemy's statistics.
        internal static bool TryCalculate(ActiveExplorationMultiplayerRules rules,
            MultiplayerRuleId id, int participants, out float result, out bool health)
        {
            result = 0;
            health = id == MultiplayerRuleId.RegularEnemyHealthMultiplier ||
                id == MultiplayerRuleId.EliteEnemyHealthMultiplier ||
                id == MultiplayerRuleId.StandardBossHealthMultiplier ||
                id == MultiplayerRuleId.RandomEncounterHealthMultiplier ||
                id == MultiplayerRuleId.KrazBossHealthMultiplier ||
                id == MultiplayerRuleId.MindEaterRootSummonHealthMultiplier;
            if (rules == null || !rules.Rules.Get(id, participants).TryGetOverride(out float value)) return false;
            if (health)
                result = 100f * EnemyHealthRuleCalculator.Combine(value, 50f, rules.HealthModifierCombination);
            else if (MultiplayerRuleCatalog.Get(id).Unit == MultiplayerRuleUnit.PercentagePoints)
                result = 100f + value;
            else return false;
            return true;
        }
    }
}
