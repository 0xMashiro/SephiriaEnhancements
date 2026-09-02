using SephiriaEnhancements.MultiplayerRules;
using SephiriaEnhancements.MultiplayerRules.Presentation;

namespace SephiriaEnhancements.ModelChecks.Features.MultiplayerRules;

internal static class MultiplayerRulePresentationGroupsChecks
{
    internal static void Run()
    {
        var groupedMultiplayerRuleIds = new HashSet<MultiplayerRuleId>();
        foreach (MultiplayerRulePresentationGroup group in
            MultiplayerRulePresentationGroups.All)
        {
            if (string.IsNullOrEmpty(group.LocalizationKey) || group.RuleIds.Count == 0)
                throw new InvalidOperationException(
                    "multiplayer-rule presentation groups must be named and non-empty");
            foreach (MultiplayerRuleId ruleId in group.RuleIds)
            {
                if (!groupedMultiplayerRuleIds.Add(ruleId))
                    throw new InvalidOperationException(
                        "multiplayer-rule presentation groups must not duplicate " + ruleId);
            }
        }
        if (groupedMultiplayerRuleIds.Count !=
                Enum.GetValues<MultiplayerRuleId>().Length)
            throw new InvalidOperationException(
                "multiplayer-rule presentation groups must cover the full catalog");
        Console.WriteLine("MultiplayerRulePresentationGroups: exact catalog coverage passed");
    }
}
