using SephiriaEnhancements.MapEnhancements;

namespace SephiriaEnhancements.ModelChecks.Features.MapEnhancements;

internal static class MapEnhancementsLocalizationChecks
{
    internal static void Run()
    {
        var texts = new Dictionary<(string Language, string Key), string>();
        MapEnhancementsLocalization.Register(
            (language, key, value) => texts.Add((language, key), value),
            new[] { "en-US", "zh-CN", "zh-TW", "und" });
        string[] keys =
        {
            MapEnhancementsLocalization.SettingShowHiddenRooms,
            MapEnhancementsLocalization.HelpShowHiddenRooms,
            MapEnhancementsLocalization.Off,
            MapEnhancementsLocalization.On
        };
        foreach (string key in keys)
        {
            if (texts[("und", key)] != texts[("en-US", key)] ||
                string.IsNullOrWhiteSpace(texts[("zh-CN", key)]) ||
                string.IsNullOrWhiteSpace(texts[("zh-TW", key)]))
                throw new InvalidOperationException(
                    "hidden-room settings must use a complete localized group or English fallback");
        }
        if (texts.Count != 16 ||
            texts[("zh-CN", MapEnhancementsLocalization.SettingShowHiddenRooms)] != "显示隐藏房间" ||
            !texts[("zh-CN", MapEnhancementsLocalization.HelpShowHiddenRooms)].Contains("尚未发现") ||
            !texts[("zh-CN", MapEnhancementsLocalization.HelpShowHiddenRooms)].Contains("默认关闭"))
            throw new InvalidOperationException(
                "hidden-room settings must describe undiscovered rooms and the opt-in default");
        Console.WriteLine("MapEnhancementsLocalization: hidden-room scope and complete fallback passed");
    }
}
