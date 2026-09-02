#nullable enable
using System;
using System.Collections.Generic;

namespace SephiriaEnhancements.MultiplayerAccess.Presentation
{
    internal static class MultiplayerAccessLocalization
    {
        internal const string AllowJoinAndReconnectSetting =
            "SephiriaEnhancements.MultiplayerAccess.AllowMidRunJoinAndReconnect";
        internal const string AllowJoinAndReconnectHelp =
            AllowJoinAndReconnectSetting + ".Help";

        private static readonly Dictionary<string, string[]> Texts = new()
        {
            ["en-US"] = new[]
            {
                "Mid-run Join and Reconnect",
                "Host option, applied to the next run. Keeps the Steam room open during exploration, lets new players join with a new character and save slot, and enables the game's GUID reconnect support. Joining players do not need this Mod. Detected multiplayer extensions retain ownership of admission."
            },
            ["zh-CN"] = new[]
            {
                "中途加入与重连",
                "房主选项，下次探索生效。探索期间保持 Steam 房间开放；新玩家以新角色和新存档槽加入；同时启用游戏的 GUID 重连支持。加入方无需安装本 MOD。检测到联机扩展时，由该扩展负责准入。"
            },
            ["zh-TW"] = new[]
            {
                "中途加入與重連",
                "房主選項，下次探索生效。探索期間保持 Steam 房間開放；新玩家以新角色和新存檔槽加入；同時啟用遊戲的 GUID 重新連線支援。加入方不必安裝本 MOD。偵測到連線擴充套件時，由該擴充套件負責准入。"
            }
        };

        internal static void Register(Action<string, string, string> addText,
            IEnumerable<string> languages)
        {
            foreach (string language in languages)
            {
                string[] text = Texts.TryGetValue(language,
                    out string[]? localized) && localized != null
                    ? localized : Texts["en-US"];
                addText(language, AllowJoinAndReconnectSetting, text[0]);
                addText(language, AllowJoinAndReconnectHelp, text[1]);
            }
        }
    }
}
