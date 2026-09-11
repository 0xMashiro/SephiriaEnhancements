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
            MapEnhancementsLocalization.On,
            MapEnhancementsLocalization.SettingEnabled,
            MapEnhancementsLocalization.HelpEnabled
        };
        foreach (string key in keys)
        {
            if (texts[("und", key)] != texts[("en-US", key)] ||
                string.IsNullOrWhiteSpace(texts[("zh-CN", key)]) ||
                string.IsNullOrWhiteSpace(texts[("zh-TW", key)]))
                throw new InvalidOperationException(
                    "hidden-room settings must use a complete localized group or English fallback");
        }
        if (texts.Count != 24 ||
            texts[("zh-CN", MapEnhancementsLocalization.SettingShowHiddenRooms)] != "显示隐藏房间" ||
            !texts[("zh-CN", MapEnhancementsLocalization.HelpShowHiddenRooms)].Contains("尚未发现") ||
            !texts[("zh-CN", MapEnhancementsLocalization.HelpShowHiddenRooms)].Contains("默认关闭"))
            throw new InvalidOperationException(
                "hidden-room settings must describe undiscovered rooms and the opt-in default");
        Console.WriteLine("MapEnhancementsLocalization: hidden-room scope and complete fallback passed");
        var navigation = new Dictionary<(string Language, string Key), string>();
        MapNavigationLocalization.Register((language, key, value) => navigation.Add((language, key), value),
            SephiriaEnhancements.Configuration.LocalizationLanguages.All.Concat(new[] { "und" }));
        foreach (string language in SephiriaEnhancements.Configuration.LocalizationLanguages.All)
        {
            if (navigation[(language, MapNavigationLocalization.Travel)] == navigation[(language, MapNavigationLocalization.DestinationTravel)] ||
                navigation[(language, MapNavigationLocalization.TravelUnavailable)] == navigation[(language, MapNavigationLocalization.NoLanding)] ||
                navigation[(language, MapNavigationLocalization.MapNotReady)] == navigation[(language, MapNavigationLocalization.NoLanding)])
                throw new InvalidOperationException("Map travel must distinguish nearby placement, authored destinations, loading and no landing found.");
            _ = string.Format(navigation[(language, MapNavigationLocalization.Guide)], "Confirm");
            _ = string.Format(navigation[(language, MapNavigationLocalization.DestinationGuide)], "Confirm");
        }
        if (navigation[("zh-CN", MapNavigationLocalization.Travel)] != "传送到目标附近" ||
            navigation[("en-US", MapNavigationLocalization.Travel)].Contains("nearest", StringComparison.OrdinalIgnoreCase) ||
            navigation[("und", MapNavigationLocalization.DestinationGuide)] != navigation[("en-US", MapNavigationLocalization.DestinationGuide)])
            throw new InvalidOperationException("Nearby travel must not promise a fixed teleport point; missing languages fall back as a group.");
        Console.WriteLine("MapNavigationLocalization: nearby placement, exact destinations and distinct unavailability reasons passed");
    }
}
