using SephiriaEnhancements.Configuration;
using SephiriaEnhancements.MultiplayerRules;
using SephiriaEnhancements.MultiplayerRules.Presentation;

namespace SephiriaEnhancements.ModelChecks.Features.MultiplayerRules;

internal static class MultiplayerRulesStateChecks
{
    internal static void Run()
    {
        foreach (bool server in new[] { false, true })
            foreach (bool active in new[] { false, true })
                foreach (bool integration in new[] { false, true })
                    foreach (bool extension in new[] { false, true })
                        foreach (bool stacking in new[] { false, true })
                            foreach (int count in new[] { 0, 1, 2, 4, 5 })
                            {
                                var availability = MultiplayerRulesLifecyclePolicy.ResolveAvailability(true, integration, count, extension, stacking);
                                bool canApply = MultiplayerRulesLifecyclePolicy.CanApplyAuthoritativeRules(server, active, integration, count, extension, stacking);
                                Check(canApply == (server && active && availability == MultiplayerRulesAvailability.Available),
                                    "execution and presentation must use the same permission decision");
                            }
        var allCustom = ActiveExplorationMultiplayerRules.Custom(MultiplayerRuleSnapshot.Create((id, _) =>
            MultiplayerRuleValue<float>.Override(MultiplayerRuleCatalog.Get(id).Maximum)), EnemyHealthModifierCombination.Additive);
        var external = new MultiplayerRulesState(allCustom, 2, false, MultiplayerRulesAvailability.ExternalExtension, false);
        var enabled = new MultiplayerRulesState(allCustom, 2, false, MultiplayerRulesAvailability.Available, true);
        Check(external.OverrideCount == 0 && enabled.OverrideCount == MultiplayerRuleCatalog.All.Count && !external.IsEquivalentTo(enabled),
            "extension stacking alone changes effective rules and must not advertise dormant overrides");
        Check(!enabled.IsEquivalentTo(new MultiplayerRulesState(allCustom, 3, false, MultiplayerRulesAvailability.Available, true)),
            "player count is part of the host state");
        Check(!enabled.IsEquivalentTo(new MultiplayerRulesState(allCustom, 2, true, MultiplayerRulesAvailability.Available, true)),
            "departure is distinct from saving preferences");

        var translations = LocalizationLanguages.All.ToDictionary(language => language, _ => new Dictionary<string, string>());
        MultiplayerRulesLocalization.Register((language, key, value) => translations[language].Add(key, value), LocalizationLanguages.All);
        int summaries = 0;
        foreach (string language in LocalizationLanguages.All)
            foreach (var availability in Enum.GetValues<MultiplayerRulesAvailability>())
                foreach (var notice in Enum.GetValues<MultiplayerRulesNotice>().Where(n => n != MultiplayerRulesNotice.None))
                {
                    var state = new MultiplayerRulesState(allCustom, 4, true, availability, true);
                    string text = MultiplayerRulesSummary.Format(state, notice, 118, key => translations[language][key]);
                    var lines = MultiplayerRulesSummary.SplitForNativeChat(text).ToArray();
                    Check(lines.All(line => line.Length > 0 && line.Length <= 120),
                        "complete rule reports must respect each native chat message limit: " + language);
                    if (availability == MultiplayerRulesAvailability.Available && notice != MultiplayerRulesNotice.Saved)
                        foreach (var rule in MultiplayerRuleCatalog.All)
                            Check(text.Contains(translations[language][MultiplayerRulesLocalization.RuleLabelKey(rule.Id)]),
                                "unmodified peers must receive every active override: " + language);
                    if (availability != MultiplayerRulesAvailability.Available)
                        Check(text.Contains(translations[language][MultiplayerRulesSummary.AvailabilityKey(availability)]),
                            "announcements must preserve the actual reason custom rules do not apply");
                    summaries++;
                }
        var health = MultiplayerRuleCatalog.Get(MultiplayerRuleId.RegularEnemyHealthMultiplier);
        var original = ActiveExplorationMultiplayerRules.FromPreset(MultiplayerRulesPreset.Original);
        var changed = ActiveExplorationMultiplayerRules.Custom(MultiplayerRuleSnapshot.Create((id, count) =>
            id == health.Id && count == 3 ? MultiplayerRuleValue<float>.Override(2) : MultiplayerRuleValue<float>.UseGameBehavior()),
            EnemyHealthModifierCombination.Additive);
        foreach (var language in LocalizationLanguages.All)
        {
            string T(string key) => translations[language][key];
            var details = MultiplayerRulesSummary.DescribeChanges(original, false, changed, true, T);
            Check(details.Split('\n').Length == 3 && details.Contains(string.Format(T(MultiplayerRulesLocalization.ParticipantsValue), 3)) &&
                details.Contains(T(MultiplayerRulesLocalization.RuleLabelKey(health.Id))) &&
                details.Contains(T(MultiplayerRulesLocalization.ExternalRuleStackingSetting)), "saved reports include other team sizes and shared settings");
            var restored = MultiplayerRulesSummary.DescribeChanges(changed, true, original, false, T);
            Check(restored.Contains("→ " + T(MultiplayerRulesLocalization.UseGameBehavior)), "restoring native behavior is reported explicitly");
            Check(MultiplayerRulesSummary.DescribeChanges(changed, true, changed, true, T) == "", "unchanged settings are omitted");
        }
        Check(MultiplayerRuleInput.Adjust("2", health, 1, 1) == "2.05" &&
            MultiplayerRuleInput.Adjust("8", health, 1, 1) == "8" &&
            MultiplayerRuleInput.Adjust("0.25", health, 1, -1) == "0.25" &&
            MultiplayerRuleInput.Adjust("", health, 2, -1) == "1.95", "step buttons use native reference, legal increments and bounds");
        string unicode = new string('字', 119) + "😀" + new string('字', 119);
        var report = MultiplayerRulesSummary.ReportLines(unicode + "\n" + unicode).ToArray();
        Check(report.Length > 2 && report.All(line => line.Length <= 120) &&
            report[0].StartsWith($"[1/{report.Length}] ") && report[^1].StartsWith($"[{report.Length}/{report.Length}] "),
            "complete reports reserve space for progress and have explicit first and last parts");
        string restoredReport = string.Concat(report.Select(line => line.Substring(line.IndexOf("] ", StringComparison.Ordinal) + 2)));
        Check(restoredReport == unicode + unicode, "report numbering and splitting preserve every value and surrogate pair");
        var split = MultiplayerRulesSummary.SplitForNativeChat(unicode).ToArray();
        Check(string.Concat(split) == unicode && split.All(part => !char.IsHighSurrogate(part[^1]) && !char.IsLowSurrogate(part[0])),
            "native chat splitting must preserve surrogate pairs");
        Console.WriteLine($"MultiplayerRulesState: authority matrix, extension activation, input increments and {summaries} localized summaries passed");
    }

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
