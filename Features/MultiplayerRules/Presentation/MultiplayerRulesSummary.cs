using System;
using System.Collections.Generic;
using System.Linq;

namespace SephiriaEnhancements.MultiplayerRules.Presentation
{
    internal static class MultiplayerRulesSummary
    {
        internal static IEnumerable<string> SplitForNativeChat(string text)
        {
            foreach (string line in text.Split('\n'))
                foreach (string part in SplitLine(line, 120)) yield return part;
        }

        internal static IEnumerable<string> ReportLines(string text)
        {
            var lines = text.Split('\n').SelectMany(line => SplitLine(line, 108)).ToArray();
            for (int i = 0; i < lines.Length; i++)
                yield return lines.Length == 1 ? lines[i] : $"[{i + 1}/{lines.Length}] {lines[i]}";
        }

        private static IEnumerable<string> SplitLine(string text, int limit)
        {
            while (text.Length > limit)
            {
                int length = text.LastIndexOf(' ', limit - 1, limit);
                if (length < limit / 2) length = char.IsHighSurrogate(text[limit - 1]) ? limit - 1 : limit;
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
            if (notice != MultiplayerRulesNotice.Saved && state.OverrideCount > 0)
            {
                foreach (var rule in MultiplayerRuleCatalog.All)
                {
                    var value = state.Rules.Rules.Get(rule.Id, state.Participants);
                    if (!value.TryGetOverride(out _)) continue;
                    message += "\n" + text(MultiplayerRulesLocalization.RuleLabelKey(rule.Id)) + ": " + DescribeValue(value, rule, text);
                }
                message += "\n" + text(MultiplayerRulesLocalization.HealthCombinationSetting) + ": " +
                    text(MultiplayerRulesLocalization.HealthCombinationKeys[(int)state.Rules.HealthModifierCombination]);
            }
            return message;
        }

        internal static string DescribeChanges(ActiveExplorationMultiplayerRules before, bool beforeStacking,
            ActiveExplorationMultiplayerRules after, bool afterStacking, Func<string, string> text)
        {
            var lines = new List<string>();
            for (int count = 1; count <= 4; count++)
                foreach (var rule in MultiplayerRuleCatalog.All)
                {
                    var previous = before.Rules.Get(rule.Id, count);
                    var next = after.Rules.Get(rule.Id, count);
                    if (previous.Equals(next)) continue;
                    lines.Add(string.Format(text(MultiplayerRulesLocalization.ParticipantsValue), count) + " · " +
                        text(MultiplayerRulesLocalization.RuleLabelKey(rule.Id)) + ": " +
                        DescribeValue(previous, rule, text) + " → " + DescribeValue(next, rule, text));
                }
            if (before.HealthModifierCombination != after.HealthModifierCombination)
                lines.Add(text(MultiplayerRulesLocalization.HealthCombinationSetting) + ": " +
                    text(MultiplayerRulesLocalization.HealthCombinationKeys[(int)before.HealthModifierCombination]) + " → " +
                    text(MultiplayerRulesLocalization.HealthCombinationKeys[(int)after.HealthModifierCombination]));
            if (beforeStacking != afterStacking)
                lines.Add(text(MultiplayerRulesLocalization.ExternalRuleStackingSetting) + ": " +
                    text(beforeStacking ? MultiplayerRulesLocalization.ToggleEnabled : MultiplayerRulesLocalization.ToggleDisabled) + " → " +
                    text(afterStacking ? MultiplayerRulesLocalization.ToggleEnabled : MultiplayerRulesLocalization.ToggleDisabled));
            return string.Join("\n", lines);
        }

        private static string DescribeValue(MultiplayerRuleValue<float> value, MultiplayerRuleDefinition rule, Func<string, string> text) =>
            value.TryGetOverride(out float number) ? rule.Unit == MultiplayerRuleUnit.Toggle
                ? text(number > 0 ? MultiplayerRulesLocalization.ToggleEnabled : MultiplayerRulesLocalization.ToggleDisabled)
                : MultiplayerRulesLocalization.FormatValue(number, rule.Unit) : text(MultiplayerRulesLocalization.UseGameBehavior);
    }
}
