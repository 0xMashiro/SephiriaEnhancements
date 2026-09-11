using System.Collections.Generic;
using System.Linq;
using Mirror;
using SephiriaEnhancements.Configuration;
using SephiriaEnhancements.MultiplayerRules.Presentation;

namespace SephiriaEnhancements.MultiplayerRules.Integration
{
    internal static class NativeRulesBroadcast
    {
        internal static void Send(ActiveExplorationMultiplayerRules rules, int participants,
            ActiveExplorationMultiplayerRules previous = null)
        {
            if (!NetworkServer.active || !NetworkClient.active || DungeonManager.Instance == null || participants < 1 || participants > 4) return;
            var parts = new List<string> { string.Format(T(previous == null
                ? MultiplayerRulesLocalization.BroadcastStarted : MultiplayerRulesLocalization.BroadcastApplied), participants) };
            foreach (var definition in MultiplayerRuleCatalog.All)
            {
                var value = rules.Rules.Get(definition.Id, participants);
                if (previous == null ? value.Source != MultiplayerRuleValueSource.Override
                    : value.Equals(previous.Rules.Get(definition.Id, participants))) continue;
                parts.Add(T(MultiplayerRulesLocalization.RuleLabelKey(definition.Id)) + ": " +
                    (previous == null ? "" : Describe(previous.Rules.Get(definition.Id, participants), definition) + " → ") + Describe(value, definition));
            }
            if (MultiplayerRulePresentationGroups.All.First(g => g.LocalizationKey == MultiplayerRulesLocalization.GroupEnemyHealth).RuleIds.Any(id => rules.Rules.Get(id, participants).Source == MultiplayerRuleValueSource.Override) &&
                (previous == null || rules.HealthModifierCombination != previous.HealthModifierCombination))
                parts.Add(T(MultiplayerRulesLocalization.HealthCombinationSetting) + ": " +
                    T(MultiplayerRulesLocalization.HealthCombinationKeys[(int)rules.HealthModifierCombination]));
            if (parts.Count == 1 && previous == null) parts.Add(T(MultiplayerRulesLocalization.UseGameBehavior));
            if (parts.Count == 1) return;
            string message = "";
            foreach (var part in parts)
            {
                if (message.Length > 0 && message.Length + part.Length + 3 > 120)
                { Chat(message); message = ""; }
                string remaining = part;
                while (remaining.Length > 120)
                {
                    int length = char.IsHighSurrogate(remaining[119]) ? 119 : 120;
                    Chat(remaining.Substring(0, length));
                    remaining = remaining.Substring(length);
                }
                message += (message.Length == 0 ? "" : " · ") + remaining;
            }
            if (message.Length > 0) Chat(message);
        }

        private static void Chat(string message) => DungeonManager.Instance.Chat(null, "[Sephiria Enhancements]", message);
        private static string Describe(MultiplayerRuleValue<float> value, MultiplayerRuleDefinition definition) =>
            !value.TryGetOverride(out float number) ? T(MultiplayerRulesLocalization.UseGameBehavior)
                : definition.Unit == MultiplayerRuleUnit.Toggle ? T(number > 0 ? MultiplayerRulesLocalization.ToggleEnabled : MultiplayerRulesLocalization.ToggleDisabled)
                : MultiplayerRulesLocalization.FormatValue(number, definition.Unit);
        private static string T(string key) => ModLocalization.Get(key);
    }
}
