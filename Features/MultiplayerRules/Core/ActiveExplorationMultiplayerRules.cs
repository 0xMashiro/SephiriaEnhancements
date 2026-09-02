using System;

namespace SephiriaEnhancements.MultiplayerRules
{
    internal sealed class ActiveExplorationMultiplayerRules
    {
        internal ActiveExplorationMultiplayerRules(MultiplayerRulesPreset preset,
            MultiplayerRuleSnapshot rules,
            EnemyHealthModifierCombination healthModifierCombination)
        {
            Preset = preset;
            Rules = rules;
            HealthModifierCombination = healthModifierCombination;
        }

        internal MultiplayerRulesPreset Preset { get; }
        internal MultiplayerRuleSnapshot Rules { get; }
        internal EnemyHealthModifierCombination HealthModifierCombination { get; }

        internal static ActiveExplorationMultiplayerRules FromPreset(
            MultiplayerRulesPreset preset)
        {
            switch (preset)
            {
                case MultiplayerRulesPreset.Original:
                    return new ActiveExplorationMultiplayerRules(preset,
                        MultiplayerRuleSnapshot.Original(),
                        EnemyHealthModifierCombination.ParticipantRuleOnly);
                case MultiplayerRulesPreset.Optimized:
                    return new ActiveExplorationMultiplayerRules(preset,
                        MultiplayerRuleSnapshot.Optimized(),
                        EnemyHealthModifierCombination.Additive);
                default:
                    throw new ArgumentException(
                        "Custom multiplayer rules require an explicit rule snapshot.",
                        nameof(preset));
            }
        }

        internal static ActiveExplorationMultiplayerRules Custom(
            MultiplayerRuleSnapshot rules,
            EnemyHealthModifierCombination healthModifierCombination)
        {
            return new ActiveExplorationMultiplayerRules(MultiplayerRulesPreset.Custom,
                rules ?? throw new ArgumentNullException(nameof(rules)),
                healthModifierCombination);
        }
    }

    internal sealed class MultiplayerRulesSession
    {
        private ActiveExplorationMultiplayerRules activeRules = null!;

        internal bool TryGetActive(out ActiveExplorationMultiplayerRules rules)
        {
            rules = activeRules;
            return rules != null;
        }

        internal ActiveExplorationMultiplayerRules BeginNewExploration(
            PreferredMultiplayerRules preferredRules)
        {
            activeRules = preferredRules.Freeze();
            return activeRules;
        }

        internal void ResumeExploration(ActiveExplorationMultiplayerRules restoredRules)
        {
            activeRules = restoredRules ?? throw new ArgumentNullException(
                nameof(restoredRules));
        }

        internal void EndExploration()
        {
            activeRules = null!;
        }
    }

    internal sealed class PreferredMultiplayerRules
    {
        internal PreferredMultiplayerRules(MultiplayerRulesPreset preset,
            MultiplayerRuleSnapshot customRules,
            EnemyHealthModifierCombination customHealthModifierCombination)
        {
            Preset = preset;
            CustomRules = customRules ?? throw new ArgumentNullException(
                nameof(customRules));
            CustomHealthModifierCombination = customHealthModifierCombination;
        }

        internal MultiplayerRulesPreset Preset { get; }
        internal MultiplayerRuleSnapshot CustomRules { get; }
        internal EnemyHealthModifierCombination CustomHealthModifierCombination { get; }

        internal ActiveExplorationMultiplayerRules Freeze()
        {
            return Preset == MultiplayerRulesPreset.Custom
                ? ActiveExplorationMultiplayerRules.Custom(CustomRules,
                    CustomHealthModifierCombination)
                : ActiveExplorationMultiplayerRules.FromPreset(Preset);
        }
    }
}
