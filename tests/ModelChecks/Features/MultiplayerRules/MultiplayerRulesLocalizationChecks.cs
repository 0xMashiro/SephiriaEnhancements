using SephiriaEnhancements.MultiplayerRules;
using SephiriaEnhancements.MultiplayerRules.Presentation;

namespace SephiriaEnhancements.ModelChecks.Features.MultiplayerRules;

internal static class MultiplayerRulesLocalizationChecks
{
    internal static void Run()
    {
        var multiplayerRulesTexts = new Dictionary<(string Language, string Key), string>();
        int multiplayerLocalizationRegistrations = 0;
        MultiplayerRulesLocalization.Register(
            (language, key, value) =>
            {
                multiplayerLocalizationRegistrations++;
                multiplayerRulesTexts[(language, key)] = value;
            },
            new[] { "en-US", "zh-CN", "fr-FR" });
        if (multiplayerRulesTexts[("zh-CN", MultiplayerRulesLocalization.Section)] !=
                "多人游戏" ||
            multiplayerRulesTexts[("zh-CN", MultiplayerRulesLocalization.PresetSetting)] !=
                "规则预设" ||
            multiplayerRulesTexts[("fr-FR", MultiplayerRulesLocalization.Section)] !=
                "Multiplayer" ||
            multiplayerRulesTexts[("fr-FR", MultiplayerRulesLocalization.OptimizedPreset)] !=
                "Optimized" ||
            multiplayerRulesTexts[("zh-CN",
                MultiplayerRulesLocalization.CopyParticipantValuesSetting)] !=
                "复制当前人数参数" ||
            multiplayerRulesTexts[("zh-CN",
                MultiplayerRulesLocalization.ExternalRuleStackingSetting)] !=
                "与联机扩展叠加规则" ||
            multiplayerRulesTexts[("fr-FR",
                MultiplayerRulesLocalization.GroupEncountersAndBosses)] !=
                "Encounters and Bosses")
        {
            throw new InvalidOperationException(
                "multiplayer-rule localization must use one complete language group");
        }
        MultiplayerRuleDefinition regularHealthDefinition = MultiplayerRuleCatalog.Get(
            MultiplayerRuleId.RegularEnemyHealthMultiplier);
        MultiplayerRuleDefinition eliteHealthDefinition = MultiplayerRuleCatalog.Get(
            MultiplayerRuleId.EliteEnemyHealthMultiplier);
        MultiplayerRuleDefinition regularDamageDefinition = MultiplayerRuleCatalog.Get(
            MultiplayerRuleId.RegularEnemyDamageBonus);
        if (MultiplayerRulesLocalization.NumericValueKey(regularHealthDefinition, 15) !=
                MultiplayerRulesLocalization.NumericValueKey(eliteHealthDefinition, 15) ||
            MultiplayerRulesLocalization.NumericValueKey(regularHealthDefinition, 15) ==
                MultiplayerRulesLocalization.NumericValueKey(regularDamageDefinition, 0) ||
            multiplayerLocalizationRegistrations >= 3000)
        {
            throw new InvalidOperationException(
                "multiplayer-rule localization must share identical unit/value keys");
        }
        foreach (MultiplayerRuleDefinition definition in MultiplayerRuleCatalog.All)
        {
            int valueCount = MultiplayerRulesLocalization.NumericValueCount(definition);
            for (int stepIndex = 0; stepIndex < valueCount; stepIndex++)
            {
                string key = MultiplayerRulesLocalization.NumericValueKey(
                    definition, stepIndex);
                foreach (string language in new[] { "en-US", "zh-CN", "fr-FR" })
                {
                    if (!multiplayerRulesTexts.TryGetValue((language, key),
                            out string? value) || string.IsNullOrEmpty(value))
                    {
                        throw new InvalidOperationException(
                            $"missing multiplayer-rule value text: {language}/{key}");
                    }
                }
            }
        }
        Console.WriteLine("MultiplayerRulesLocalization: native terms, group fallback and " +
            $"{multiplayerLocalizationRegistrations} deduplicated registrations passed");
    }
}
