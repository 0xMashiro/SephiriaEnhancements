using System.Linq;

namespace SephiriaEnhancements.MultiplayerRules
{
    internal enum MultiplayerRulesAvailability
    {
        Available,
        ExternalExtension,
        UnsupportedTeam,
        Disabled,
        Unavailable
    }

    internal enum MultiplayerRulesNotice
    {
        None,
        Saved,
        Started,
        TeamChanged,
        Summary
    }

    // The host's effective rules, distinct from the editor's unsaved draft.
    internal sealed class MultiplayerRulesState
    {
        internal MultiplayerRulesState(ActiveExplorationMultiplayerRules rules, int participants,
            bool explorationStarted, MultiplayerRulesAvailability availability, bool allowExternalStacking)
        {
            Rules = availability == MultiplayerRulesAvailability.Available ? rules
                : ActiveExplorationMultiplayerRules.FromPreset(MultiplayerRulesPreset.Original);
            Participants = participants;
            ExplorationStarted = explorationStarted;
            Availability = availability;
            AllowExternalStacking = allowExternalStacking;
        }

        internal ActiveExplorationMultiplayerRules Rules { get; }
        internal int Participants { get; }
        internal bool ExplorationStarted { get; }
        internal MultiplayerRulesAvailability Availability { get; }
        internal bool AllowExternalStacking { get; }
        internal int OverrideCount => Participants < 1 || Participants > 4 ? 0 :
            MultiplayerRuleCatalog.All.Count(d => Rules.Rules.Get(d.Id, Participants).Source == MultiplayerRuleValueSource.Override);

        internal bool IsEquivalentTo(MultiplayerRulesState other) => other != null &&
            Participants == other.Participants && ExplorationStarted == other.ExplorationStarted &&
            Availability == other.Availability && AllowExternalStacking == other.AllowExternalStacking &&
            Rules.HealthModifierCombination == other.Rules.HealthModifierCombination &&
            Rules.Rules.IsEquivalentTo(other.Rules.Rules);
    }
}
