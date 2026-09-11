using SephiriaEnhancements.Configuration;

namespace SephiriaEnhancements.ModelChecks.Configuration;

internal static class OptionsCategoryChecks
{
    internal static void Run()
    {
        var optionsCategoryTexts =
            new Dictionary<(string Language, string Key), string>();
        OptionsCategoryLocalization.Register(
            (language, key, value) => optionsCategoryTexts[(language, key)] = value,
            new[] { "en-US", "zh-CN", "und" });
        if (Enum.GetValues<OptionsCategory>().Length !=
                OptionsCategoryLocalization.CategoryKeys.Length ||
            optionsCategoryTexts[("zh-CN", OptionsCategoryLocalization.Setting)] !=
                "设置分类" ||
            optionsCategoryTexts[("zh-CN",
                OptionsCategoryLocalization.CategoryKeys[
                    (int)OptionsCategory.ControlsAndCamera])] != "操作与镜头" ||
            optionsCategoryTexts[("und",
                OptionsCategoryLocalization.CategoryKeys[
                    (int)OptionsCategory.Multiplayer])] != "Multiplayer")
        {
            throw new InvalidOperationException(
                "options categories must preserve enum/key alignment and complete fallback");
        }
        string[] expected = { "General", "CombatAndDisplay", "ResourceBarValues", "ControlsAndCamera", "Multiplayer", "AboutAndUpdates" };
        foreach (OptionsCategory category in Enum.GetValues<OptionsCategory>())
        {
            if (OptionsCategoryLocalization.CategoryKeys[(int)category] != "SephiriaEnhancements.OptionsCategory." + expected[(int)category])
                throw new InvalidOperationException("Category selector order and localization must agree.");
            foreach (OptionsCategory selected in Enum.GetValues<OptionsCategory>())
                if (OptionsCategoryVisibility.IsVisible(category, selected) != (category == selected))
                    throw new InvalidOperationException("Unrelated settings must not leak into the selected category.");
        }
        if (optionsCategoryTexts[("zh-CN", OptionsCategoryLocalization.CategoryKeys[(int)OptionsCategory.ResourceBarValues])] != "血条与数值" ||
            optionsCategoryTexts[("zh-CN", OptionsCategoryLocalization.CategoryKeys[(int)OptionsCategory.AboutAndUpdates])] != "关于与更新")
            throw new InvalidOperationException("New categories must describe their contents.");
        Console.WriteLine("OptionsCategoryLocalization: category alignment and fallback passed");

    }
}
