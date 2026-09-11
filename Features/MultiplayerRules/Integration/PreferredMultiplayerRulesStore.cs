using System;

namespace SephiriaEnhancements.MultiplayerRules.Integration
{
    internal static class PreferredMultiplayerRulesStore
    {
        internal const string PresetKey =
            "SephiriaEnhancements.MultiplayerRules.PreferredPreset";
        private const string HealthCombinationKey =
            "SephiriaEnhancements.MultiplayerRules.Custom.HealthCombination";
        private const string AllowExternalRuleStackingKey =
            "SephiriaEnhancements.MultiplayerRules.AllowExternalRuleStacking";

        internal static PreferredMultiplayerRules Read()
        {
            int storedPreset = OptionsBinding.Instance?.DeviceOptions?.GetInt(
                PresetKey, (int)MultiplayerRulesPreset.Original) ??
                (int)MultiplayerRulesPreset.Original;
            MultiplayerRulesPreset preset = storedPreset >= 0 && storedPreset <= 2
                ? (MultiplayerRulesPreset)storedPreset
                : MultiplayerRulesPreset.Original;
            int storedCombination = OptionsBinding.Instance?.DeviceOptions?.GetInt(
                HealthCombinationKey,
                (int)EnemyHealthModifierCombination.ParticipantRuleOnly) ?? 0;
            EnemyHealthModifierCombination combination =
                storedCombination >= 0 && storedCombination <= 2
                    ? (EnemyHealthModifierCombination)storedCombination
                    : EnemyHealthModifierCombination.ParticipantRuleOnly;
            return new PreferredMultiplayerRules(preset,
                MultiplayerRuleSnapshot.Create(ReadCustomValue), combination);
        }

        internal static void WritePreset(MultiplayerRulesPreset preset)
        {
            OptionsBinding.Instance?.DeviceOptions?.SetInt(PresetKey,
                (int)preset);
        }

        internal static bool ReadAllowExternalRuleStacking() =>
            OptionsBinding.Instance?.DeviceOptions?.GetBool(
                AllowExternalRuleStackingKey, false) ?? false;

        internal static void WriteAllowExternalRuleStacking(bool enabled)
        {
            OptionsBinding.Instance?.DeviceOptions?.SetBool(
                AllowExternalRuleStackingKey, enabled);
        }

        internal static void WriteCustomHealthCombination(
            EnemyHealthModifierCombination combination)
        {
            OptionsBinding.Instance?.DeviceOptions?.SetInt(HealthCombinationKey,
                (int)combination);
        }

        internal static void WriteCustomValue(MultiplayerRuleId id,
            int participantCount,
            MultiplayerRuleValue<float> value)
        {
            bool overridden = value.TryGetOverride(out float overrideValue);
            OptionsBinding.Instance?.DeviceOptions?.SetBool(
                SourceKey(id, participantCount),
                overridden);
            if (overridden)
            {
                OptionsBinding.Instance?.DeviceOptions?.SetFloat(
                    ValueKey(id, participantCount), overrideValue);
            }
        }

        internal static void Save()
        {
            OptionsBinding.Instance?.DeviceOptions?.Save();
        }

        internal static void Apply(MultiplayerRulesDraft draft, int participants)
        {
            var rules = draft.RulesForCurrentTeam(participants, Read());
            WritePreset(draft.Preset);
            WriteCustomHealthCombination(draft.HealthCombination);
            WriteAllowExternalRuleStacking(draft.AllowExternalStacking);
            foreach (var definition in MultiplayerRuleCatalog.All)
                for (int count = 1; count <= 4; count++)
                    WriteCustomValue(definition.Id, count, rules.Get(definition.Id, count));
            Save();
        }

        private static MultiplayerRuleValue<float> ReadCustomValue(
            MultiplayerRuleId id, int participantCount)
        {
            bool overridden = OptionsBinding.Instance?.DeviceOptions?.GetBool(
                SourceKey(id, participantCount), false) ?? false;
            if (!overridden)
            {
                return MultiplayerRuleValue<float>.UseGameBehavior();
            }

            MultiplayerRuleDefinition definition = MultiplayerRuleCatalog.Get(id);
            float value = OptionsBinding.Instance?.DeviceOptions?.GetFloat(
                ValueKey(id, participantCount), definition.Minimum) ??
                definition.Minimum;
            return definition.IsValidOverride(value)
                ? MultiplayerRuleValue<float>.Override(value)
                : MultiplayerRuleValue<float>.UseGameBehavior();
        }

        private static string SourceKey(MultiplayerRuleId id,
            int participantCount) =>
            "SephiriaEnhancements.MultiplayerRules.Custom." + id +
            ".Participants" + participantCount + ".Override";

        private static string ValueKey(MultiplayerRuleId id,
            int participantCount) =>
            "SephiriaEnhancements.MultiplayerRules.Custom." + id +
            ".Participants" + participantCount + ".Value";
    }
}
