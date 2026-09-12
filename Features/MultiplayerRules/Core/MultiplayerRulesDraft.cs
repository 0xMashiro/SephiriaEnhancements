using System;
using System.Collections.Generic;
using System.Linq;

namespace SephiriaEnhancements.MultiplayerRules
{
    // Editing owns an immutable copy; cancelling never writes preferred or active rules.
    internal sealed class MultiplayerRulesDraft
    {
        internal MultiplayerRulesDraft(PreferredMultiplayerRules preferred, bool externalStacking)
        {
            Preset = preferred.Preset;
            Rules = preferred.CustomRules;
            HealthCombination = preferred.CustomHealthModifierCombination;
            AllowExternalStacking = externalStacking;
        }

        internal MultiplayerRulesPreset Preset { get; set; }
        internal MultiplayerRuleSnapshot Rules { get; private set; }
        internal EnemyHealthModifierCombination HealthCombination { get; set; }
        internal bool AllowExternalStacking { get; set; }

        internal PreferredMultiplayerRules ToPreferred() =>
            new PreferredMultiplayerRules(Preset, Rules, HealthCombination);

        internal void Set(MultiplayerRuleId id, int participants, MultiplayerRuleValue<float> value)
        {
            CheckParticipants(participants);
            BeginCustom();
            var before = Rules;
            Rules = MultiplayerRuleSnapshot.Create((key, count) =>
                key == id && count == participants ? value : before.Get(key, count));
            Preset = MultiplayerRulesPreset.Custom;
        }

        internal void RestoreGroup(int participants, IReadOnlyList<MultiplayerRuleId> ids)
        {
            CheckParticipants(participants);
            BeginCustom();
            var before = Rules;
            Rules = MultiplayerRuleSnapshot.Create((key, count) => count == participants && ids.Contains(key)
                ? MultiplayerRuleValue<float>.UseGameBehavior() : before.Get(key, count));
            Preset = MultiplayerRulesPreset.Custom;
        }

        internal void BeginCustom()
        {
            if (Preset == MultiplayerRulesPreset.Custom) return;
            var effective = ToPreferred().Freeze();
            Rules = effective.Rules;
            HealthCombination = Preset == MultiplayerRulesPreset.Original
                ? EnemyHealthModifierCombination.Additive : effective.HealthModifierCombination;
            Preset = MultiplayerRulesPreset.Custom;
        }

        internal int CountChanges(ActiveExplorationMultiplayerRules saved, bool externalStacking)
        {
            var edited = ToPreferred().Freeze();
            return Enumerable.Range(1, 4).Sum(count => MultiplayerRuleCatalog.All.Count(d =>
                !edited.Rules.Get(d.Id, count).Equals(saved.Rules.Get(d.Id, count)))) +
                (edited.HealthModifierCombination != saved.HealthModifierCombination ? 1 : 0) +
                (AllowExternalStacking != externalStacking ? 1 : 0);
        }

        internal bool HasChanges(PreferredMultiplayerRules preferred, bool externalStacking) =>
            Preset != preferred.Preset || HealthCombination != preferred.CustomHealthModifierCombination ||
            AllowExternalStacking != externalStacking || !Rules.IsEquivalentTo(preferred.CustomRules);

        private static void CheckParticipants(int participants)
        {
            if (participants < 1 || participants > 4)
                throw new ArgumentOutOfRangeException(nameof(participants));
        }
    }
}
