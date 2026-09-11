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
            new[] { "en-US", "zh-CN", "und" });
        if (multiplayerRulesTexts[("zh-CN", MultiplayerRulesLocalization.Section)] !=
                "多人游戏" ||
            multiplayerRulesTexts[("zh-CN", MultiplayerRulesLocalization.PresetSetting)] !=
                "规则预设" ||
            multiplayerRulesTexts[("und", MultiplayerRulesLocalization.Section)] !=
                "Multiplayer" ||
            multiplayerRulesTexts[("und", MultiplayerRulesLocalization.OptimizedPreset)] !=
                "Optimized" ||
            multiplayerRulesTexts[("zh-CN",
                MultiplayerRulesLocalization.ExternalRuleStackingSetting)] !=
                "与联机扩展叠加规则" ||
            multiplayerRulesTexts[("und",
                MultiplayerRulesLocalization.GroupEnemyDamage)] !=
                "Enemy damage")
        {
            throw new InvalidOperationException(
                "multiplayer-rule localization must use one complete language group");
        }
        Console.WriteLine("MultiplayerRulesLocalization: native terms, group fallback and " +
            $"{multiplayerLocalizationRegistrations} deduplicated registrations passed");
    }
}
