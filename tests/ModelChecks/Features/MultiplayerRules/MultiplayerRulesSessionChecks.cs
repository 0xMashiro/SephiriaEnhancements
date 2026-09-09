using SephiriaEnhancements.MultiplayerRules;

namespace SephiriaEnhancements.ModelChecks.Features.MultiplayerRules;

internal static class MultiplayerRulesSessionChecks
{
    internal static void Run()
    {
        var multiplayerRulesSession = new MultiplayerRulesSession();
        if (multiplayerRulesSession.TryGetActive(out _))
            throw new InvalidOperationException(
                "multiplayer rules must be inactive before exploration starts");
        ActiveExplorationMultiplayerRules frozenRules =
            multiplayerRulesSession.BeginNewExploration(new PreferredMultiplayerRules(
                MultiplayerRulesPreset.Optimized, MultiplayerRuleSnapshot.Original(),
                EnemyHealthModifierCombination.ParticipantRuleOnly));
        if (!multiplayerRulesSession.TryGetActive(out var activeRules) ||
            !ReferenceEquals(frozenRules, activeRules) ||
            activeRules.Preset != MultiplayerRulesPreset.Optimized)
        {
            throw new InvalidOperationException(
                "preferred multiplayer rules must freeze when exploration starts");
        }
        if (!EnemyHealthRuleResolver.TryResolveMultiplier(activeRules,
                EnemySpawnOrigin.RandomEncounter, EnemyHealthCategory.Regular,
                participantCount: 4, otherModifierPercent: 20f,
                out float optimizedRandomHealthMultiplier) ||
            Math.Abs(optimizedRandomHealthMultiplier - 1.5f) > 0.001f ||
            EnemyHealthRuleResolver.TryResolveMultiplier(activeRules,
                EnemySpawnOrigin.RandomEncounter, EnemyHealthCategory.Regular,
                participantCount: 3, otherModifierPercent: 20f, out _) ||
            EnemyHealthRuleResolver.TryResolveMultiplier(
                ActiveExplorationMultiplayerRules.FromPreset(MultiplayerRulesPreset.Original),
                EnemySpawnOrigin.RandomEncounter, EnemyHealthCategory.Regular,
                participantCount: 4, otherModifierPercent: 20f, out _))
        {
            throw new InvalidOperationException(
                "enemy health rules must resolve sparse overrides without replacing game behavior");
        }
        multiplayerRulesSession.EndExploration();
        if (multiplayerRulesSession.TryGetActive(out _))
            throw new InvalidOperationException(
                "multiplayer rules must release exploration-owned state when exploration ends");
        var krazRules = new ActiveExplorationMultiplayerRules(MultiplayerRulesPreset.Custom,
            MultiplayerRuleSnapshot.Create((id, count) => id == MultiplayerRuleId.KrazBossHealthMultiplier
                ? MultiplayerRuleValue<float>.Override(count)
                : MultiplayerRuleValue<float>.UseGameBehavior()), EnemyHealthModifierCombination.ParticipantRuleOnly);
        for (int count = 1; count <= 4; count++)
        {
            if (!EnemyHealthRuleResolver.TryResolveMultiplier(krazRules, EnemySpawnOrigin.KrazBoss,
                    EnemyHealthCategory.Elite, count, 0, out float multiplier) || multiplier != count ||
                EnemyHealthRuleResolver.TryResolveMultiplier(krazRules, EnemySpawnOrigin.StandardBoss,
                    EnemyHealthCategory.Elite, count, 0, out _))
                throw new InvalidOperationException("Kraz's rule must not change other boss encounters");
        }
        Console.WriteLine("MultiplayerRulesSession: freeze, sparse resolution and release passed");
    }
}
