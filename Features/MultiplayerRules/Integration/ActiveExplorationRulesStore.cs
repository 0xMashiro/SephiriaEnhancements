namespace SephiriaEnhancements.MultiplayerRules.Integration
{
    internal static class ActiveExplorationRulesStore
    {
        private const int CurrentSchemaVersion = 4;
        private const string SchemaVersionKey =
            "SephiriaEnhancements.MultiplayerRules.ActiveExploration.SchemaVersion";
        private const string PresetKey =
            "SephiriaEnhancements.MultiplayerRules.ActiveExploration.Preset";
        private const string HealthCombinationKey =
            "SephiriaEnhancements.MultiplayerRules.ActiveExploration.HealthCombination";

        internal static void Write(ActiveExplorationMultiplayerRules activeRules)
        {
            SaveData currentRun = SaveManager.CurrentRun;
            if (currentRun == null || activeRules == null)
            {
                return;
            }

            currentRun.SetInt(SchemaVersionKey, CurrentSchemaVersion);
            currentRun.SetInt(PresetKey, (int)activeRules.Preset);
            currentRun.SetInt(HealthCombinationKey,
                (int)activeRules.HealthModifierCombination);
            foreach (MultiplayerRuleDefinition definition in MultiplayerRuleCatalog.All)
            {
                for (int participantCount = 1; participantCount <= 4;
                    participantCount++)
                {
                    MultiplayerRuleValue<float> value = activeRules.Rules.Get(
                        definition.Id, participantCount);
                    bool overridden = value.TryGetOverride(out float overrideValue);
                    currentRun.SetBool(SourceKey(definition.Id,
                        participantCount), overridden);
                    if (overridden)
                    {
                        currentRun.SetFloat(ValueKey(definition.Id,
                            participantCount),
                            overrideValue);
                    }
                }
            }
        }

        internal static bool TryRead(out ActiveExplorationMultiplayerRules activeRules)
        {
            activeRules = null;
            SaveData currentRun = SaveManager.CurrentRun;
            if (currentRun == null || !currentRun.ContainsKey(SchemaVersionKey) ||
                currentRun.GetInt(SchemaVersionKey, 0) != CurrentSchemaVersion)
            {
                return false;
            }

            int storedPreset = currentRun.GetInt(PresetKey,
                (int)MultiplayerRulesPreset.Original);
            MultiplayerRulesPreset preset = storedPreset >= 0 && storedPreset <= 2
                ? (MultiplayerRulesPreset)storedPreset
                : MultiplayerRulesPreset.Original;
            int storedCombination = currentRun.GetInt(HealthCombinationKey,
                (int)EnemyHealthModifierCombination.ParticipantRuleOnly);
            EnemyHealthModifierCombination combination =
                storedCombination >= 0 && storedCombination <= 2
                    ? (EnemyHealthModifierCombination)storedCombination
                    : EnemyHealthModifierCombination.ParticipantRuleOnly;
            MultiplayerRuleSnapshot rules = MultiplayerRuleSnapshot.Create(
                (id, participantCount) => ReadValue(currentRun, id,
                    participantCount));
            if (preset != MultiplayerRulesPreset.Custom)
            {
                ActiveExplorationMultiplayerRules expected =
                    ActiveExplorationMultiplayerRules.FromPreset(preset);
                if (!rules.IsEquivalentTo(expected.Rules) ||
                    combination != expected.HealthModifierCombination)
                    return false;
            }
            activeRules = new ActiveExplorationMultiplayerRules(preset, rules,
                combination);
            return true;
        }

        private static MultiplayerRuleValue<float> ReadValue(SaveData currentRun,
            MultiplayerRuleId id, int participantCount)
        {
            if (!currentRun.GetBool(SourceKey(id, participantCount), false))
            {
                return MultiplayerRuleValue<float>.UseGameBehavior();
            }

            MultiplayerRuleDefinition definition = MultiplayerRuleCatalog.Get(id);
            float value = currentRun.GetFloat(ValueKey(id, participantCount),
                definition.Minimum);
            return definition.IsValidOverride(value)
                ? MultiplayerRuleValue<float>.Override(value)
                : MultiplayerRuleValue<float>.UseGameBehavior();
        }

        private static string SourceKey(MultiplayerRuleId id,
            int participantCount) =>
            "SephiriaEnhancements.MultiplayerRules.ActiveExploration." + id +
            ".Participants" + participantCount + ".Override";

        private static string ValueKey(MultiplayerRuleId id,
            int participantCount) =>
            "SephiriaEnhancements.MultiplayerRules.ActiveExploration." + id +
            ".Participants" + participantCount + ".Value";
    }
}
