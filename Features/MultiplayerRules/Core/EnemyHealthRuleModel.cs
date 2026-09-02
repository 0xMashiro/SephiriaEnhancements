namespace SephiriaEnhancements.MultiplayerRules
{
    internal enum MultiplayerRulesPreset
    {
        Original,
        Optimized,
        Custom
    }

    internal enum EnemyHealthModifierCombination
    {
        ParticipantRuleOnly,
        Additive,
        Multiplicative
    }

    internal enum EnemySpawnOrigin
    {
        RegularEncounter,
        RandomEncounter,
        StandardBoss,
        SeedEncounterBoss,
        MindEaterRootSummon
    }

    internal static class EnemyHealthRuleCalculator
    {
        internal static float Combine(float participantMultiplier,
            float otherModifierPercent,
            EnemyHealthModifierCombination combination)
        {
            switch (combination)
            {
                case EnemyHealthModifierCombination.ParticipantRuleOnly:
                    return participantMultiplier;
                case EnemyHealthModifierCombination.Additive:
                    return participantMultiplier + otherModifierPercent / 100f;
                case EnemyHealthModifierCombination.Multiplicative:
                    return participantMultiplier * (1f + otherModifierPercent / 100f);
                default:
                    return participantMultiplier;
            }
        }
    }
}
