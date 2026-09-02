using System;
using System.Collections.Generic;

namespace SephiriaEnhancements.CombatVisuals
{
    internal static class CombatVisualLocalization
    {
        internal const string SettingPreset =
            "SephiriaEnhancements.CombatVisuals.Setting.Preset";
        internal const string HelpPreset =
            "SephiriaEnhancements.CombatVisuals.Help.Preset";
        internal const string SettingCompanionBody =
            "SephiriaEnhancements.CombatVisuals.Setting.CompanionBody";
        internal const string HelpCompanionBody =
            "SephiriaEnhancements.CombatVisuals.Help.CompanionBody";
        internal const string SettingCompanionEffects =
            "SephiriaEnhancements.CombatVisuals.Setting.CompanionEffects";
        internal const string HelpCompanionEffects =
            "SephiriaEnhancements.CombatVisuals.Help.CompanionEffects";
        internal const string SettingOutlineScope =
            "SephiriaEnhancements.CombatVisuals.Setting.OutlineScope";
        internal const string HelpOutlineScope =
            "SephiriaEnhancements.CombatVisuals.Help.OutlineScope";

        internal static readonly string[] PresetKeys =
        {
            "SephiriaEnhancements.CombatVisuals.Preset.FollowGame",
            "SephiriaEnhancements.CombatVisuals.Preset.Balanced",
            "SephiriaEnhancements.CombatVisuals.Preset.Minimal",
            "SephiriaEnhancements.CombatVisuals.Preset.Custom"
        };

        internal static readonly string[] TransparencyKeys =
        {
            "SephiriaEnhancements.CombatVisuals.Transparency.Normal",
            "SephiriaEnhancements.CombatVisuals.Transparency.Slight",
            "SephiriaEnhancements.CombatVisuals.Transparency.Very",
            "SephiriaEnhancements.CombatVisuals.Transparency.Complete"
        };

        internal static readonly string[] OutlineScopeKeys =
        {
            "SephiriaEnhancements.CombatVisuals.Outline.Off",
            "SephiriaEnhancements.CombatVisuals.Outline.HostileOnly",
            "SephiriaEnhancements.CombatVisuals.Outline.HostileAndFriendly"
        };

        private static readonly Dictionary<string, string> English = Create(
            "Combat visual preset",
            "Follow Game preserves the official behavior. Balanced keeps companions readable while reducing their effects. Minimal hides companion effects but keeps their body faintly visible.",
            "Companion body",
            "Custom preset transparency for companions led by the local player.",
            "Companion projectiles and effects",
            "Custom preset transparency for projectiles, melee swings, and supported effects created by local companions.",
            "Combat outline scope",
            "Custom preset outline scope. The existing outline switch remains the master control.",
            "Follow Game", "Balanced (Recommended)", "Minimal", "Custom",
            "Normal", "Slightly Transparent", "Very Transparent",
            "Completely Transparent", "Off", "Hostile Only",
            "Hostile and Friendly");

        private static readonly Dictionary<string, string> SimplifiedChinese = Create(
            "战斗视觉预设",
            "“跟随游戏”保留官方行为；“均衡清晰”在保持同伴可辨识的同时降低其特效；“极简战斗”隐藏同伴特效，但仍让同伴本体保持微弱可见。",
            "同伴本体",
            "自定义本机玩家所带同伴的本体透明度。",
            "同伴弹道与特效",
            "自定义本机同伴产生的弹道、近战挥砍及已支持特效的透明度。",
            "战斗描边范围",
            "自定义描边范围；现有的敌我描边开关仍是总开关。",
            "跟随游戏", "均衡清晰（推荐）", "极简战斗", "自定义",
            "普通", "稍微透明", "非常透明", "完全透明", "关闭",
            "仅敌方", "敌方与友方");

        private static readonly Dictionary<string, string> TraditionalChinese = Create(
            "戰鬥視覺預設",
            "「跟隨遊戲」保留官方行為；「均衡清晰」在保持同伴可辨識的同時降低其特效；「極簡戰鬥」隱藏同伴特效，但仍讓同伴本體保持微弱可見。",
            "同伴本體",
            "自訂本機玩家所帶同伴的本體透明度。",
            "同伴彈道與特效",
            "自訂本機同伴產生的彈道、近戰揮砍及已支援特效的透明度。",
            "戰鬥描邊範圍",
            "自訂描邊範圍；現有的敵我描邊開關仍是總開關。",
            "跟隨遊戲", "均衡清晰（推薦）", "極簡戰鬥", "自訂",
            "普通", "稍微透明", "非常透明", "完全透明", "關閉",
            "僅敵方", "敵方與友方");

        internal static void Register(Action<string, string, string> addText,
            IEnumerable<string> languages)
        {
            foreach (string language in languages)
            {
                Dictionary<string, string> texts = language == "zh-CN"
                    ? SimplifiedChinese : language == "zh-TW"
                        ? TraditionalChinese : English;
                foreach (KeyValuePair<string, string> text in texts)
                {
                    addText(language, text.Key, text.Value);
                }
            }
        }

        private static Dictionary<string, string> Create(string preset,
            string presetHelp, string body, string bodyHelp, string effects,
            string effectsHelp, string outline, string outlineHelp,
            string followGame, string balanced, string minimal, string custom,
            string normal, string slight, string very, string complete,
            string off, string hostileOnly, string hostileAndFriendly)
        {
            return new Dictionary<string, string>
            {
                [SettingPreset] = preset,
                [HelpPreset] = presetHelp,
                [SettingCompanionBody] = body,
                [HelpCompanionBody] = bodyHelp,
                [SettingCompanionEffects] = effects,
                [HelpCompanionEffects] = effectsHelp,
                [SettingOutlineScope] = outline,
                [HelpOutlineScope] = outlineHelp,
                [PresetKeys[0]] = followGame,
                [PresetKeys[1]] = balanced,
                [PresetKeys[2]] = minimal,
                [PresetKeys[3]] = custom,
                [TransparencyKeys[0]] = normal,
                [TransparencyKeys[1]] = slight,
                [TransparencyKeys[2]] = very,
                [TransparencyKeys[3]] = complete,
                [OutlineScopeKeys[0]] = off,
                [OutlineScopeKeys[1]] = hostileOnly,
                [OutlineScopeKeys[2]] = hostileAndFriendly
            };
        }
    }
}
