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
            if (string.IsNullOrWhiteSpace(navigation[(language, MapNavigationLocalization.FloorEntrance)]) ||
                string.IsNullOrWhiteSpace(navigation[(language, MapNavigationLocalization.FloorExit)]) ||
                navigation[(language, MapNavigationLocalization.FloorEntrance)] == navigation[(language, MapNavigationLocalization.FloorExit)])
                throw new InvalidOperationException("Floor entrances and exits must have distinct localized labels.");
            if (navigation[(language, MapNavigationLocalization.Travel)] == navigation[(language, MapNavigationLocalization.DestinationTravel)] ||
                navigation[(language, MapNavigationLocalization.TravelUnavailable)] == navigation[(language, MapNavigationLocalization.NoLanding)] ||
                navigation[(language, MapNavigationLocalization.MapNotReady)] == navigation[(language, MapNavigationLocalization.NoLanding)])
                throw new InvalidOperationException("Map travel must distinguish nearby placement, authored destinations, loading and no landing found.");
            foreach (string key in new[] { MapNavigationLocalization.Guide, MapNavigationLocalization.PointerGuide })
                if (!string.Format(navigation[(language, key)], "BoundSubmit").Contains("BoundSubmit"))
                    throw new InvalidOperationException("Map preview help must display the current submit binding.");
            foreach (string key in new[] { MapNavigationLocalization.Travel, MapNavigationLocalization.RoomTravel,
                MapNavigationLocalization.DestinationTravel })
                if (!string.Format(navigation[(language, key)], "SelectedTarget").Contains("SelectedTarget"))
                    throw new InvalidOperationException("Every travel action must name its selected target.");
        }
        if (string.Format(navigation[("zh-CN", MapNavigationLocalization.Travel)], "人物") != "传送到「人物」附近" ||
            navigation[("en-US", MapNavigationLocalization.Travel)].Contains("nearest", StringComparison.OrdinalIgnoreCase) ||
            navigation[("und", MapNavigationLocalization.PointerGuide)] != navigation[("en-US", MapNavigationLocalization.PointerGuide)] ||
            !navigation[("zh-CN", MapNavigationLocalization.PointerGuide)].Contains("单击") ||
            !navigation[("zh-CN", MapNavigationLocalization.PointerGuide)].Contains("悬停"))
            throw new InvalidOperationException("Nearby travel must not promise a fixed teleport point; missing languages fall back as a group.");
        Console.WriteLine("MapNavigationLocalization: nearby placement, exact destinations and distinct unavailability reasons passed");
    }
}
