using SephiriaEnhancements.MultiplayerRules;

namespace SephiriaEnhancements.ModelChecks.Features.MultiplayerRules;

internal static class MultiplayerRuleCatalogChecks
{
    internal static void Run()
    {
        CheckDraft();
        foreach (char character in "wasdWASDeE+- ")
            if (MultiplayerRuleInput.ValidateCharacter("", 0, character) != '\0')
                throw new InvalidOperationException("Numeric editing must reject letters and non-numeric characters");
        if (MultiplayerRuleInput.ValidateCharacter("2", 1, ',') != '.' ||
            MultiplayerRuleInput.ValidateCharacter("2.5", 3, '.') != '\0' ||
            MultiplayerRuleInput.ValidateCharacter("", 0, '0') != '0')
            throw new InvalidOperationException("Numeric editing must accept digits and one decimal separator");
        var health = MultiplayerRuleCatalog.Get(MultiplayerRuleId.StandardBossHealthMultiplier);
        if (MultiplayerRuleInput.Validate("abc", health, out _) != MultiplayerRuleInputError.Number ||
            MultiplayerRuleInput.Validate("9", health, out _) != MultiplayerRuleInputError.Range ||
            MultiplayerRuleInput.Validate("2.53", health, out _) != MultiplayerRuleInputError.Step ||
            MultiplayerRuleInput.Validate("", health, out _) != MultiplayerRuleInputError.None)
            throw new InvalidOperationException("Input errors must distinguish format, range, increments and restore");
        foreach (string input in new[] { "2.5", "2,5", " 2.50 ", "2.5×" })
            if (!MultiplayerRuleInput.TryParse(input, health, out var parsed) ||
                !parsed.TryGetOverride(out float number) || number != 2.5f)
                throw new InvalidOperationException("Direct rule input must preserve decimal values");
        foreach (string input in new[] { "NaN", "Infinity", "9", "-1", "2.53", "2,000.5", "abc", "2%" })
            if (MultiplayerRuleInput.TryParse(input, health, out _))
                throw new InvalidOperationException("Invalid rule input must not change preferences");
        if (!MultiplayerRuleInput.TryParse(" ", health, out var restored) ||
            restored.Source != MultiplayerRuleValueSource.UseGameBehavior)
            throw new InvalidOperationException("Empty rule input must restore game behavior");
        if (SephiriaEnhancements.MultiplayerRules.Presentation.MultiplayerRulesLocalization.FormatValue(
                265, MultiplayerRuleUnit.PercentagePoints) != "+265%")
            throw new InvalidOperationException("Damage bonus must show its percentage unit");
        if (!MultiplayerRuleInput.TryParse("+265%",
                MultiplayerRuleCatalog.Get(MultiplayerRuleId.BossEncounterDamageBonus), out var damage) ||
            !damage.TryGetOverride(out float damageNumber) || damageNumber != 265)
            throw new InvalidOperationException("Damage input must accept its displayed unit");
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
                MultiplayerRuleId.ExplorationFloorHealingSupply)
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
                MultiplayerRuleId.KrazBossHealthMultiplier,
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

    private static void CheckDraft()
    {
        var preferred = new PreferredMultiplayerRules(MultiplayerRulesPreset.Custom,
            MultiplayerRuleSnapshot.Original(), EnemyHealthModifierCombination.Additive);
        var draft = new MultiplayerRulesDraft(preferred, false);
        var id = MultiplayerRuleId.RegularEnemyHealthMultiplier;
        draft.Set(id, 2, MultiplayerRuleValue<float>.Override(2));
        if (preferred.CustomRules.Get(id, 2).TryGetOverride(out _) || !draft.HasChanges(preferred, false))
            throw new InvalidOperationException("Editing must isolate the draft from saved preferences");
        var frozen = draft.ToPreferred().Freeze();
        draft.Set(id, 3, MultiplayerRuleValue<float>.Override(2));
        draft.RestoreGroup(2, new[] { id });
        if (draft.Rules.Get(id, 2).TryGetOverride(out _) ||
            !draft.Rules.Get(id, 3).TryGetOverride(out float configured) || configured != 2 ||
            !frozen.Rules.Get(id, 2).TryGetOverride(out float retained) || retained != 2)
            throw new InvalidOperationException("Restoring the current team must preserve other team sizes and frozen rules");
        draft.Set(id, 2, MultiplayerRuleValue<float>.Override(4));
        var allCounts = draft.ToPreferred().Freeze().Rules;
        if (!allCounts.Get(id, 2).TryGetOverride(out float previousCount) || previousCount != 4 ||
            !allCounts.Get(id, 3).TryGetOverride(out float current) || current != 2 || draft.CountChanges(preferred.Freeze(), false) != 2)
            throw new InvalidOperationException("Review and save must retain edits across player counts");
        draft.Set(MultiplayerRuleId.BossEncounterDamageBonus, 2, MultiplayerRuleValue<float>.Override(50));
        draft.RestoreGroup(2, new[] { id });
        if (!draft.Rules.Get(MultiplayerRuleId.BossEncounterDamageBonus, 2).TryGetOverride(out _) ||
            !draft.Rules.Get(id, 3).TryGetOverride(out _) || draft.Rules.Get(id, 2).TryGetOverride(out _))
            throw new InvalidOperationException("Group reset must preserve other groups and player counts");
        var originalDraft = new MultiplayerRulesDraft(new PreferredMultiplayerRules(MultiplayerRulesPreset.Original,
            MultiplayerRuleSnapshot.Optimized(), EnemyHealthModifierCombination.ParticipantRuleOnly), false);
        originalDraft.Set(id, 2, MultiplayerRuleValue<float>.Override(2));
        if (originalDraft.Preset != MultiplayerRulesPreset.Custom || originalDraft.HealthCombination != EnemyHealthModifierCombination.Additive ||
            originalDraft.Rules.Get(MultiplayerRuleId.KrazBossHealthMultiplier, 3).TryGetOverride(out _))
            throw new InvalidOperationException("Direct edits must start from displayed rules and preserve other health bonuses");
        foreach (var combination in Enum.GetValues<EnemyHealthModifierCombination>())
        {
            draft.HealthCombination = combination;
            var rules = draft.ToPreferred().Freeze();
            float expected = combination == EnemyHealthModifierCombination.ParticipantRuleOnly ? 200 :
                combination == EnemyHealthModifierCombination.Additive ? 250 : 300;
            if (!MultiplayerRuleExample.TryCalculate(rules, id, 3, out float result, out bool health) ||
                !health || result != expected)
                throw new InvalidOperationException("Health example must use the runtime combination calculator");
        }
        draft.Set(MultiplayerRuleId.BossEncounterDamageBonus, 3, MultiplayerRuleValue<float>.Override(265));
        if (!MultiplayerRuleExample.TryCalculate(draft.ToPreferred().Freeze(),
                MultiplayerRuleId.BossEncounterDamageBonus, 3, out float damage, out bool isHealth) ||
            isHealth || damage != 365)
            throw new InvalidOperationException("Damage preview must replace rather than compound the participant bonus");
        if (MultiplayerRuleExample.TryCalculate(frozen, id, 1, out _, out _))
            throw new InvalidOperationException("Unspecified native behavior cannot claim a calculated result");
    }
}
