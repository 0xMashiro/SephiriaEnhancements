using SephiriaEnhancements.CombatVisuals;

namespace SephiriaEnhancements.ModelChecks.Features.CombatVisuals;

internal static class CombatVisualLocalizationChecks
{
    internal static void Run()
    {
        var combatVisualTexts = new Dictionary<(string Language, string Key), string>();
        CombatVisualLocalization.Register(
            (language, key, value) => combatVisualTexts[(language, key)] = value,
            new[] { "en-US", "zh-CN", "fr-FR" });
        if (combatVisualTexts[("zh-CN", CombatVisualLocalization.SettingPreset)] !=
                "战斗视觉预设" ||
            combatVisualTexts[("zh-CN", CombatVisualLocalization.PresetKeys[
                (int)CombatVisualPreset.Balanced])] != "均衡清晰（推荐）" ||
            combatVisualTexts[("fr-FR", CombatVisualLocalization.SettingPreset)] !=
                "Combat visual preset" ||
            combatVisualTexts.Count != 3 * (8 +
                CombatVisualLocalization.PresetKeys.Length +
                CombatVisualLocalization.TransparencyKeys.Length +
                CombatVisualLocalization.OutlineScopeKeys.Length))
            throw new InvalidOperationException(
                "combat visual localization must use complete feature-group fallback");
        Console.WriteLine("CombatVisualLocalization: localized group fallback passed");
    }
}
