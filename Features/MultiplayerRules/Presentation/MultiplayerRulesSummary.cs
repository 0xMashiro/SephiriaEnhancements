using System;
using System.Collections.Generic;

namespace SephiriaEnhancements.MultiplayerRules.Presentation
{
    internal static class MultiplayerRulesSummary
    {
        internal static IEnumerable<string> SplitForNativeChat(string text)
        {
            while (text.Length > 120)
            {
                int length = text.LastIndexOf(' ', 119, 120);
                if (length < 60) length = char.IsHighSurrogate(text[119]) ? 119 : 120;
                yield return text.Substring(0, length).Trim();
                text = text.Substring(length).TrimStart();
            }
            if (text.Length != 0) yield return text;
        }

        internal static string AvailabilityKey(MultiplayerRulesAvailability availability) => availability switch
        {
            MultiplayerRulesAvailability.ExternalExtension => MultiplayerRulesLocalization.StateExternal,
            MultiplayerRulesAvailability.UnsupportedTeam => MultiplayerRulesLocalization.TeamUnsupported,
            MultiplayerRulesAvailability.Disabled => MultiplayerRulesLocalization.StateDisabled,
            MultiplayerRulesAvailability.Unavailable => MultiplayerRulesLocalization.StateUnavailable,
            _ => MultiplayerRulesLocalization.StateAvailable
        };

        internal static string Format(MultiplayerRulesState state, MultiplayerRulesNotice notice, int changes, Func<string, string> text)
        {
            string key = notice == MultiplayerRulesNotice.Saved ? MultiplayerRulesLocalization.BroadcastSaved
                : notice == MultiplayerRulesNotice.Started ? MultiplayerRulesLocalization.BroadcastStarted
                : notice == MultiplayerRulesNotice.TeamChanged ? MultiplayerRulesLocalization.BroadcastTeamChanged
                : MultiplayerRulesLocalization.BroadcastSummary;
            string message = string.Format(text(key), state.Participants,
                notice == MultiplayerRulesNotice.Saved ? changes : state.OverrideCount);
            if (state.Availability != MultiplayerRulesAvailability.Available)
                message += " " + text(AvailabilityKey(state.Availability));
            return message;
        }
    }
}
