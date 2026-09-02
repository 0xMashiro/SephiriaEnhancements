using SephiriaEnhancements.MultiplayerRules;

namespace SephiriaEnhancements.ModelChecks.Features.MultiplayerRules;

internal static class MultiplayerRuleCatalogChecks
{
    internal static void Run()
    {
        if (MultiplayerRuleCatalog.All.Count !=
                Enum.GetValues<MultiplayerRuleId>().Length ||
            !MultiplayerRuleCatalog.Get(MultiplayerRuleId.MonsterSpawnEntryMultiplier)
                .IsValidOverride(1.45f) ||
            MultiplayerRuleCatalog.Get(MultiplayerRuleId.EnemyGroupDifficultyOffset)
                .IsValidOverride(1.5f) ||
            !MultiplayerRuleCatalog.Get(
                MultiplayerRuleId.QliphothFinalBattleEntryAttackTracksParticipant)
                .IsValidOverride(1f) ||
            MultiplayerRuleCatalog.Get(
                MultiplayerRuleId.LifeSupplyOnPositiveProgressFloor)
                .IsValidOverride(0.5f) ||
            MultiplayerRuleCatalog.Get(MultiplayerRuleId.TargetedExperienceOrbDivisor)
                .IsValidOverride(0f))
        {
            throw new InvalidOperationException(
                "multiplayer-rule catalog coverage or value constraints failed");
        }
        MultiplayerRuleSnapshot originalRuleSnapshot = MultiplayerRuleSnapshot.Original();
        foreach (MultiplayerRuleId ruleId in Enum.GetValues<MultiplayerRuleId>())
        {
            for (int participantCount = 1; participantCount <= 4;
                participantCount++)
            {
                if (originalRuleSnapshot.Get(ruleId, participantCount).Source !=
                    MultiplayerRuleValueSource.UseGameBehavior)
                {
                    throw new InvalidOperationException(
                        "original rule snapshot must not contain copied game values");
                }
            }
        }
        MultiplayerRuleSnapshot optimizedRuleSnapshot = MultiplayerRuleSnapshot.Optimized();
        if (!optimizedRuleSnapshot.Get(
                MultiplayerRuleId.RandomEncounterHealthMultiplier, 4)
                .TryGetOverride(out float optimizedRandomSnapshotValue) ||
            Math.Abs(optimizedRandomSnapshotValue - 1.3f) > 0.001f ||
            optimizedRuleSnapshot.Get(
                MultiplayerRuleId.TargetedExperienceOrbDivisor, 4).Source !=
                MultiplayerRuleValueSource.UseGameBehavior ||
            !optimizedRuleSnapshot.HasAnyOverride(
                MultiplayerRuleId.RandomEncounterHealthMultiplier,
                MultiplayerRuleId.SeedEncounterBossHealthMultiplier,
                MultiplayerRuleId.MindEaterRootSummonHealthMultiplier) ||
            optimizedRuleSnapshot.HasAnyOverride(
                MultiplayerRuleId.TargetedExperienceOrbDivisor,
                MultiplayerRuleId.SharedMoneyAwardFactorPerParticipant))
        {
            throw new InvalidOperationException(
                "optimized rule snapshot must remain a sparse confirmed-fix set");
        }
        Console.WriteLine("MultiplayerRuleCatalog: complete catalog, constraints and sparse presets passed");
    }
}
