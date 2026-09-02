#nullable enable
using System;
using System.Collections.Generic;

namespace SephiriaEnhancements.Configuration
{
    internal enum OptionsCategory
    {
        General,
        CombatAndDisplay,
        ControlsAndCamera,
        InventoryArrangement,
        Multiplayer
    }

    internal static class OptionsCategoryVisibility
    {
        internal static bool IsVisible(OptionsCategory memberCategory,
            OptionsCategory selectedCategory, bool requiresCustomPreset,
            bool customPresetVisible, int memberMultiplayerRuleGroup,
            int selectedMultiplayerRuleGroup)
        {
            if (memberCategory != selectedCategory ||
                requiresCustomPreset && !customPresetVisible)
            {
                return false;
            }

            return memberMultiplayerRuleGroup < 0 ||
                memberMultiplayerRuleGroup == selectedMultiplayerRuleGroup;
        }
    }

    internal static class OptionsCategoryLocalization
    {
        internal const string Setting =
            "SephiriaEnhancements.OptionsCategory.Setting";
        internal const string Help =
            "SephiriaEnhancements.OptionsCategory.Help";

        internal static readonly string[] CategoryKeys =
        {
            "SephiriaEnhancements.OptionsCategory.General",
            "SephiriaEnhancements.OptionsCategory.CombatAndDisplay",
            "SephiriaEnhancements.OptionsCategory.ControlsAndCamera",
            "SephiriaEnhancements.OptionsCategory.InventoryArrangement",
            "SephiriaEnhancements.OptionsCategory.Multiplayer"
        };

        private static readonly string[] Keys =
        {
            Setting, Help, CategoryKeys[0], CategoryKeys[1], CategoryKeys[2],
            CategoryKeys[3], CategoryKeys[4]
        };

        private static readonly Dictionary<string, string[]> Texts = new()
        {
            ["en-US"] = new[]
            {
                "Settings Category",
                "Choose which Sephiria Enhancements settings group is shown below.",
                "General", "Combat and Display", "Controls and Camera",
                "Inventory Arrangement", "Multiplayer"
            },
            ["zh-CN"] = new[]
            {
                "设置分类", "选择下方显示的 Sephiria 增强设置组。",
                "基础功能", "战斗与显示", "操作与镜头", "背包整理",
                "多人游戏"
            },
            ["zh-TW"] = new[]
            {
                "設定分類", "選擇下方顯示的 Sephiria 增強設定群組。",
                "基礎功能", "戰鬥與顯示", "操作與鏡頭", "背包整理",
                "多人遊戲"
            }
        };

        internal static void Register(Action<string, string, string> addText,
            IEnumerable<string> languages)
        {
            foreach (string language in languages)
            {
                string[] values = Texts.TryGetValue(language,
                    out string[]? localized) && localized != null
                    ? localized : Texts["en-US"];
                for (int index = 0; index < Keys.Length; index++)
                {
                    addText(language, Keys[index], values[index]);
                }
            }
        }
    }
}
