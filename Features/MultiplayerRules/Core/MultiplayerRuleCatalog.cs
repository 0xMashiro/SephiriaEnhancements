using System;
using System.Collections.Generic;

namespace SephiriaEnhancements.MultiplayerRules
{
    internal enum MultiplayerRuleId
    {
        MonsterSpawnEntryMultiplier,
        EnemyGroupDifficultyOffset,
        RegularEnemyHealthMultiplier,
        RegularEnemyDamageBonus,
        EliteEnemyHealthMultiplier,
        EliteEnemyDamageBonus,
        StandardBossHealthMultiplier,
        BossEncounterDamageBonus,
        RandomEncounterHealthMultiplier,
        RandomEncounterDamageBonus,
        RandomEncounterLivingEnemyLimit,
        SeedEncounterBossHealthMultiplier,
        MindEaterRootSummonHealthMultiplier,
        MindEaterRootSummonDamageBonus,
        TargetedExperienceOrbDivisor,
        SharedMoneyAwardFactorPerParticipant,
        FestivalOfBloodEnemyHealingMultiplier,
        HiddenRoomBreakableRewardCount,
        LifeSupplyOnPositiveProgressFloor,
        WanderingMerchantCharmCandidateBonus,
        WanderingMerchantTabletCandidateCount,
        MerchantGuildCharmCandidateBonus,
        MerchantGuildTabletCandidateCount,
        RestorativePotionQuantity,
        RegenerationSamplePotionQuantity,
        QliphothSealTeamMultiplier,
        QliphothFinalBattleGridRegionCount,
        QliphothFinalBattleEntryAttackTracksParticipant,
        QliphothTempleTrioActiveCount
    }

    internal enum MultiplayerRuleUnit
    {
        Multiplier,
        PercentagePoints,
        Count,
        DifficultyOffset,
        Divisor,
        Toggle
    }

    internal readonly struct MultiplayerRuleDefinition
    {
        internal MultiplayerRuleDefinition(MultiplayerRuleId id,
            MultiplayerRuleUnit unit, float minimum, float maximum, float step)
        {
            Id = id;
            Unit = unit;
            Minimum = minimum;
            Maximum = maximum;
            Step = step;
        }

        internal MultiplayerRuleId Id { get; }
        internal MultiplayerRuleUnit Unit { get; }
        internal float Minimum { get; }
        internal float Maximum { get; }
        internal float Step { get; }

        internal bool IsValidOverride(float value)
        {
            if (float.IsNaN(value) || float.IsInfinity(value) ||
                value < Minimum || value > Maximum)
            {
                return false;
            }

            float steps = (value - Minimum) / Step;
            return Math.Abs(steps - (float)Math.Round(steps)) < 0.001f;
        }
    }

    internal static class MultiplayerRuleCatalog
    {
        private static readonly MultiplayerRuleDefinition[] Definitions =
        {
            D(MultiplayerRuleId.MonsterSpawnEntryMultiplier, MultiplayerRuleUnit.Multiplier, 0.25f, 4f, 0.05f),
            D(MultiplayerRuleId.EnemyGroupDifficultyOffset, MultiplayerRuleUnit.DifficultyOffset, 0f, 5f, 1f),
            D(MultiplayerRuleId.RegularEnemyHealthMultiplier, MultiplayerRuleUnit.Multiplier, 0.25f, 8f, 0.05f),
            D(MultiplayerRuleId.RegularEnemyDamageBonus, MultiplayerRuleUnit.PercentagePoints, 0f, 300f, 5f),
            D(MultiplayerRuleId.EliteEnemyHealthMultiplier, MultiplayerRuleUnit.Multiplier, 0.25f, 8f, 0.05f),
            D(MultiplayerRuleId.EliteEnemyDamageBonus, MultiplayerRuleUnit.PercentagePoints, 0f, 300f, 5f),
            D(MultiplayerRuleId.StandardBossHealthMultiplier, MultiplayerRuleUnit.Multiplier, 0.25f, 8f, 0.05f),
            D(MultiplayerRuleId.BossEncounterDamageBonus, MultiplayerRuleUnit.PercentagePoints, 0f, 300f, 5f),
            D(MultiplayerRuleId.RandomEncounterHealthMultiplier, MultiplayerRuleUnit.Multiplier, 0.25f, 8f, 0.05f),
            D(MultiplayerRuleId.RandomEncounterDamageBonus, MultiplayerRuleUnit.PercentagePoints, 0f, 300f, 5f),
            D(MultiplayerRuleId.RandomEncounterLivingEnemyLimit, MultiplayerRuleUnit.Count, 1f, 30f, 1f),
            D(MultiplayerRuleId.SeedEncounterBossHealthMultiplier, MultiplayerRuleUnit.Multiplier, 0.25f, 8f, 0.05f),
            D(MultiplayerRuleId.MindEaterRootSummonHealthMultiplier, MultiplayerRuleUnit.Multiplier, 0.25f, 8f, 0.05f),
            D(MultiplayerRuleId.MindEaterRootSummonDamageBonus, MultiplayerRuleUnit.PercentagePoints, 0f, 300f, 5f),
            D(MultiplayerRuleId.TargetedExperienceOrbDivisor, MultiplayerRuleUnit.Divisor, 1f, 8f, 1f),
            D(MultiplayerRuleId.SharedMoneyAwardFactorPerParticipant, MultiplayerRuleUnit.Multiplier, 0f, 20f, 1f),
            D(MultiplayerRuleId.FestivalOfBloodEnemyHealingMultiplier, MultiplayerRuleUnit.Multiplier, 0f, 2f, 0.05f),
            D(MultiplayerRuleId.HiddenRoomBreakableRewardCount, MultiplayerRuleUnit.Count, 0f, 10f, 1f),
            D(MultiplayerRuleId.LifeSupplyOnPositiveProgressFloor, MultiplayerRuleUnit.Toggle, 0f, 1f, 1f),
            D(MultiplayerRuleId.WanderingMerchantCharmCandidateBonus, MultiplayerRuleUnit.Count, 0f, 8f, 1f),
            D(MultiplayerRuleId.WanderingMerchantTabletCandidateCount, MultiplayerRuleUnit.Count, 0f, 10f, 1f),
            D(MultiplayerRuleId.MerchantGuildCharmCandidateBonus, MultiplayerRuleUnit.Count, 0f, 8f, 1f),
            D(MultiplayerRuleId.MerchantGuildTabletCandidateCount, MultiplayerRuleUnit.Count, 0f, 10f, 1f),
            D(MultiplayerRuleId.RestorativePotionQuantity, MultiplayerRuleUnit.Count, 0f, 10f, 1f),
            D(MultiplayerRuleId.RegenerationSamplePotionQuantity, MultiplayerRuleUnit.Count, 0f, 10f, 1f),
            D(MultiplayerRuleId.QliphothSealTeamMultiplier, MultiplayerRuleUnit.Multiplier, 0.25f, 3f, 0.05f),
            D(MultiplayerRuleId.QliphothFinalBattleGridRegionCount, MultiplayerRuleUnit.Count, 1f, 300f, 1f),
            D(MultiplayerRuleId.QliphothFinalBattleEntryAttackTracksParticipant, MultiplayerRuleUnit.Toggle, 0f, 1f, 1f),
            D(MultiplayerRuleId.QliphothTempleTrioActiveCount, MultiplayerRuleUnit.Count, 1f, 3f, 1f)
        };

        private static readonly Dictionary<MultiplayerRuleId, MultiplayerRuleDefinition>
            ById = CreateLookup();

        internal static IReadOnlyList<MultiplayerRuleDefinition> All => Definitions;

        internal static MultiplayerRuleDefinition Get(MultiplayerRuleId id)
        {
            return ById[id];
        }

        private static MultiplayerRuleDefinition D(MultiplayerRuleId id,
            MultiplayerRuleUnit unit, float minimum, float maximum, float step) =>
            new MultiplayerRuleDefinition(id, unit, minimum, maximum, step);

        private static Dictionary<MultiplayerRuleId, MultiplayerRuleDefinition>
            CreateLookup()
        {
            var lookup = new Dictionary<MultiplayerRuleId, MultiplayerRuleDefinition>();
            foreach (MultiplayerRuleDefinition definition in Definitions)
            {
                lookup.Add(definition.Id, definition);
            }
            return lookup;
        }
    }
}
