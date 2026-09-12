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
                    Check(lines.Length <= 2 && lines.All(line => line.Length > 0 && line.Length <= 120),
                        "even a full custom configuration must fit at most two native chat messages: " + language);
                    if (availability != MultiplayerRulesAvailability.Available)
                        Check(text.Contains(translations[language][MultiplayerRulesSummary.AvailabilityKey(availability)]),
                            "announcements must preserve the actual reason custom rules do not apply");
                    summaries++;
                }
        var health = MultiplayerRuleCatalog.Get(MultiplayerRuleId.RegularEnemyHealthMultiplier);
        Check(MultiplayerRuleInput.Adjust("2", health, 1, 1) == "2.05" &&
            MultiplayerRuleInput.Adjust("8", health, 1, 1) == "8" &&
            MultiplayerRuleInput.Adjust("0.25", health, 1, -1) == "0.25" &&
            MultiplayerRuleInput.Adjust("", health, 2, -1) == "1.95", "step buttons use native reference, legal increments and bounds");
        string unicode = new string('字', 119) + "😀" + new string('字', 119);
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
