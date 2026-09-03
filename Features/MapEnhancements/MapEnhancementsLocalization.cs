using System;
using System.Collections.Generic;

namespace SephiriaEnhancements.MapEnhancements
{
    internal static class MapEnhancementsLocalization
    {
        internal const string SettingShowHiddenRooms =
            "SephiriaEnhancements.MapEnhancements.Setting.ShowHiddenRooms";
        internal const string HelpShowHiddenRooms =
            "SephiriaEnhancements.MapEnhancements.Help.ShowHiddenRooms";
        internal const string Off = "SephiriaEnhancements.MapEnhancements.Off";
        internal const string On = "SephiriaEnhancements.MapEnhancements.On";

        internal static void Register(Action<string, string, string> addText,
            IEnumerable<string> languages)
        {
            foreach (string language in languages)
            {
                string[] texts = language == "zh-CN"
                    ? new[] { "显示隐藏房间", "在支持的地图及本层地图叠加层中显示尚未发现的隐藏房间。默认关闭；开启会提前揭示秘密位置。", "关闭", "开启" }
                    : language == "zh-TW"
                        ? new[] { "顯示隱藏房間", "在支援的地圖及本層地圖疊加層中顯示尚未發現的隱藏房間。預設關閉；開啟會提前揭示秘密位置。", "關閉", "開啟" }
                        : new[] { "Show hidden rooms", "Show undiscovered hidden rooms on supported maps and the current-floor overlay. Disabled by default; enabling this reveals secret locations early.", "Off", "On" };
                addText(language, SettingShowHiddenRooms, texts[0]);
                addText(language, HelpShowHiddenRooms, texts[1]);
                addText(language, Off, texts[2]);
                addText(language, On, texts[3]);
            }
        }
    }
}
