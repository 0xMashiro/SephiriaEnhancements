#nullable disable
using SephiriaEnhancements.Runtime.Inventory;

using System;
using System.Collections.Generic;

namespace SephiriaEnhancements.Inventory
{
    internal static class InventoryOptimizationLocalization
    {
        internal const string HudHard = "SephiriaEnhancements.InventoryHud.Hard";
        internal const string HudSoft = "SephiriaEnhancements.InventoryHud.Soft";
        internal const string HardInfeasible = "SephiriaEnhancements.Inventory.HardInfeasible";
        internal const string HardNotFound = "SephiriaEnhancements.Inventory.HardNotFound";
        internal const string Analyzing =
            "SephiriaEnhancements.Inventory.Analyzing";
        internal const string Applying =
            "SephiriaEnhancements.Inventory.Applying";
        internal const string Completed =
            "SephiriaEnhancements.Inventory.Completed";
        internal const string NoImprovementFound =
            "SephiriaEnhancements.Inventory.NoImprovementFound";
        internal const string Unavailable =
            "SephiriaEnhancements.Inventory.Unavailable";
        internal const string RuntimeNotReady =
            "SephiriaEnhancements.Inventory.RuntimeNotReady";
        internal const string EmptyInventory =
            "SephiriaEnhancements.Inventory.EmptyInventory";
        internal const string ItemIdentityConflict =
            "SephiriaEnhancements.Inventory.ItemIdentityConflict";
        internal const string PositionEffectsUnavailable =
            "SephiriaEnhancements.Inventory.PositionEffectsUnavailable";
        internal const string Unsupported =
            "SephiriaEnhancements.Inventory.Unsupported";
        internal const string Changed =
            "SephiriaEnhancements.Inventory.Changed";
        internal const string InventoryClosed =
            "SephiriaEnhancements.Inventory.InventoryClosed";
        internal const string GameplayContextChanged =
            "SephiriaEnhancements.Inventory.GameplayContextChanged";
        internal const string ApplyTimedOut =
            "SephiriaEnhancements.Inventory.ApplyTimedOut";
        internal const string Failed =
            "SephiriaEnhancements.Inventory.Failed";
        internal const string VerificationFailed =
            "SephiriaEnhancements.Inventory.VerificationFailed";
        internal const string Busy =
            "SephiriaEnhancements.Inventory.Busy";
        internal const string FinishMovingItem =
            "SephiriaEnhancements.Inventory.FinishMovingItem";
        internal const string MovingItemInterrupted =
            "SephiriaEnhancements.Inventory.MovingItemInterrupted";
        internal const string DisabledForGameplayContext =
            "SephiriaEnhancements.Inventory.DisabledForGameplayContext";
        internal const string SettingOptimizationTendency =
            "SephiriaEnhancements.Setting.InventoryOptimizationTendency";
        internal const string HelpOptimizationTendency =
            "SephiriaEnhancements.Help.InventoryOptimizationTendency";
        internal static readonly string[] OptimizationTendencyKeys =
        {
            "SephiriaEnhancements.InventoryOptimizationTendency.Automatic",
            "SephiriaEnhancements.InventoryOptimizationTendency.Stable",
            "SephiriaEnhancements.InventoryOptimizationTendency.Aggressive"
        };
        internal const string HudTitle =
            "SephiriaEnhancements.InventoryHud.Title";
        internal const string HudComboTargets =
            "SephiriaEnhancements.InventoryHud.ComboTargets";
        internal const string HudOptimize =
            "SephiriaEnhancements.InventoryHud.Optimize";
        internal const string HudMarkArtifacts =
            "SephiriaEnhancements.InventoryHud.MarkArtifacts";
        internal const string HudFinishMarking =
            "SephiriaEnhancements.InventoryHud.FinishMarking";
        internal const string HudMarkingHint =
            "SephiriaEnhancements.InventoryHud.MarkingHint";
        internal const string HudMarkedCount =
            "SephiriaEnhancements.InventoryHud.MarkedCount";
        internal const string HudMarkedAndAdjustmentCount =
            "SephiriaEnhancements.InventoryHud.MarkedAndAdjustmentCount";
        internal const string HudPriorityQueue =
            "SephiriaEnhancements.InventoryHud.PriorityQueue";
        internal const string HudAvoidZone =
            "SephiriaEnhancements.InventoryHud.AvoidZone";
        internal const string HudIntentBoardHint =
            "SephiriaEnhancements.InventoryHud.IntentBoardHint";
        internal const string HudEditGoals = "SephiriaEnhancements.InventoryHud.EditGoals";
        internal const string HudEditGoalsShortcut = "SephiriaEnhancements.InventoryHud.EditGoalsShortcut";
        internal const string HudConstraintHelp = "SephiriaEnhancements.InventoryHud.ConstraintHelp";
        internal const string HudComboPersistence = "SephiriaEnhancements.InventoryHud.ComboPersistence";
        internal const string HudControllerBoardHint = "SephiriaEnhancements.InventoryHud.ControllerBoardHint";
        internal const string HudControllerChooseIntentSlot = "SephiriaEnhancements.InventoryHud.ControllerChooseIntentSlot";
        internal const string HudLevelEditUnbound =
            "SephiriaEnhancements.InventoryHud.LevelEditUnbound";
        internal const string HudChooseIntentSlot =
            "SephiriaEnhancements.InventoryHud.ChooseIntentSlot";
        internal const string HudOpen =
            "SephiriaEnhancements.InventoryHud.Open";
        internal const string HudAdjustTargets =
            "SephiriaEnhancements.InventoryHud.AdjustTargets";
        internal const string HudHideTargets =
            "SephiriaEnhancements.InventoryHud.HideTargets";
        internal const string HudAutomaticPreset =
            "SephiriaEnhancements.InventoryHud.AutomaticPreset";
        internal const string HudAutomaticInventory =
            "SephiriaEnhancements.InventoryHud.AutomaticInventory";
        internal const string HudAdjustmentCount =
            "SephiriaEnhancements.InventoryHud.AdjustmentCount";
        internal const string HudEnabled =
            "SephiriaEnhancements.InventoryHud.Enabled";
        internal const string HudAutomaticTarget =
            "SephiriaEnhancements.InventoryHud.AutomaticTarget";
        internal const string HudMinimumLevel =
            "SephiriaEnhancements.InventoryHud.MinimumLevel";
        internal const string HudArtifactAuto = "SephiriaEnhancements.InventoryHud.ArtifactAuto";
        internal const string HudArtifactSafeAuto = "SephiriaEnhancements.InventoryHud.ArtifactSafeAuto";
        internal const string HudResultPending = "SephiriaEnhancements.InventoryHud.ResultPending";
        internal const string HudResultSatisfied = "SephiriaEnhancements.InventoryHud.ResultSatisfied";
        internal const string HudResultPartial = "SephiriaEnhancements.InventoryHud.ResultPartial";
        internal const string HudResultUnmet = "SephiriaEnhancements.InventoryHud.ResultUnmet";
        internal const string HudCurrentLevel = "SephiriaEnhancements.InventoryHud.CurrentLevel";
        internal const string HudCurrentActive = "SephiriaEnhancements.InventoryHud.CurrentActive";
        internal const string HudCurrentInactive = "SephiriaEnhancements.InventoryHud.CurrentInactive";
        internal const string HudAvoidGoal = "SephiriaEnhancements.InventoryHud.AvoidGoal";
        internal const string HudMinimumCount =
            "SephiriaEnhancements.InventoryHud.MinimumCount";
        internal const string HudMaximumCount =
            "SephiriaEnhancements.InventoryHud.MaximumCount";
        internal const string HudNoMinimumCount =
            "SephiriaEnhancements.InventoryHud.NoMinimumCount";
        internal const string HudNoTargets =
            "SephiriaEnhancements.InventoryHud.NoTargets";
        internal const string HudPage =
            "SephiriaEnhancements.InventoryHud.Page";
        internal const string HudSearching =
            "SephiriaEnhancements.InventoryHud.Searching";
        internal const string HudApplying =
            "SephiriaEnhancements.InventoryHud.Applying";
        internal static readonly string[] PreferenceChoiceKeys =
        {
            "SephiriaEnhancements.InventoryPreference.Automatic",
            "SephiriaEnhancements.InventoryPreference.Priority",
            "SephiriaEnhancements.InventoryPreference.Avoid"
        };

        internal static string FormatTargetCondition(
            InventoryComboTarget target, Func<string, string> localize)
        {
            if (target.Choice == InventoryPreferenceChoice.Automatic)
            {
                return localize(HudAutomaticTarget);
            }
            return target.Choice == InventoryPreferenceChoice.Priority && target.RequiredValue == 0
                ? localize(HudNoMinimumCount)
                : string.Format(localize(target.Choice == InventoryPreferenceChoice.Avoid
                    ? HudMaximumCount : HudMinimumCount), target.RequiredValue);
        }

        internal static string FormatArtifactMinimumLevel(int level, Func<string, string> localize) =>
            level == 0 ? localize(HudEnabled) : string.Format(localize(HudMinimumLevel), level);

        internal static string FormatArtifactTarget(ArtifactOptimizationPreference rule,
            ArtifactSnapshot artifact, Func<string, string> localize, int? targetLevel = null)
        {
            if (rule.Level == InventoryPreferenceLevel.Avoid) return localize(HudAvoidGoal);
            int target = targetLevel ?? rule.ResolveTargetLevel(artifact);
            string condition = FormatArtifactMinimumLevel(target, localize);
            return rule.TargetMode != ArtifactLevelTargetMode.Automatic ? condition
                : string.Format(localize(artifact.SafeAutomaticLevel < artifact.MaxLevel
                    ? HudArtifactSafeAuto : HudArtifactAuto), condition);
        }

        internal static string FormatArtifactFeedback(ArtifactOptimizationPreference rule,
            ArtifactSnapshot artifact, InventoryArtifactGoalFeedback feedback, Func<string, string> localize)
        {
            int target = feedback?.TargetLevel ?? rule.ResolveTargetLevel(artifact);
            bool active = feedback?.Active ?? artifact.EffectEnabled;
            string current = !active ? localize(HudCurrentInactive)
                : target == 0 || rule.Level == InventoryPreferenceLevel.Avoid ? localize(HudCurrentActive)
                : string.Format(localize(HudCurrentLevel), feedback?.CurrentLevel ?? artifact.LimitedEffectEnabledLevel);
            string state = localize((feedback?.State ?? InventoryIntentSatisfaction.NotEvaluated) switch
            {
                InventoryIntentSatisfaction.Satisfied => HudResultSatisfied,
                InventoryIntentSatisfaction.Partial => HudResultPartial,
                InventoryIntentSatisfaction.Unmet => HudResultUnmet,
                _ => HudResultPending
            });
            return localize(rule.Strength == InventoryConstraintStrength.Hard ? HudHard : HudSoft) + " · " +
                FormatArtifactTarget(rule, artifact, localize, target) + "\n" + current + " · " + state;
        }

        private static readonly string[] Languages =
        {
            "en-US", "zh-CN", "zh-TW", "ko-KR", "ja-JP", "de-DE",
            "es-ES", "fr-FR", "it-IT", "pl-PL", "pt-BR", "ru-RU",
            "sv-SE", "th-TH", "tr-TR"
        };

        internal static void Register(Action<string, string, string> addText)
        {
            foreach (string language in Languages)
            {
                bool simplifiedChinese = language == "zh-CN";
                bool traditionalChinese = language == "zh-TW";
                addText(language, Analyzing, simplifiedChinese
                    ? "正在寻找更好的摆放……"
                    : traditionalChinese ? "正在尋找更好的擺放……" : "Finding a better arrangement…");
                addText(language, Applying, simplifiedChinese
                    ? "正在移动物品……"
                    : traditionalChinese ? "正在移動物品……" : "Moving items…");
                addText(language, Completed, simplifiedChinese
                    ? "背包整理完成。"
                    : traditionalChinese ? "背包整理完成。" : "Inventory arrangement complete.");
                addText(language, NoImprovementFound, simplifiedChinese
                    ? "本次未找到更好的摆放，背包保持原样。"
                    : traditionalChinese ? "本次未找到更好的擺放，背包保持原樣。" : "No better arrangement found this time. Inventory unchanged.");
                addText(language, Unavailable, simplifiedChinese
                    ? "请先打开背包，再使用智能整理。"
                    : traditionalChinese ? "請先開啟背包，再使用智慧整理。" : "Open your inventory to use Smart Arrange.");
                addText(language, RuntimeNotReady, simplifiedChinese
                    ? "背包尚未就绪，请稍后重试。"
                    : traditionalChinese ? "背包尚未就緒，請稍後重試。" : "Inventory not ready. Try again shortly.");
                addText(language, EmptyInventory, simplifiedChinese
                    ? "背包中没有可整理的物品。"
                    : traditionalChinese ? "背包中沒有可整理的物品。" : "No items to arrange.");
                addText(language, ItemIdentityConflict, simplifiedChinese
                    ? "无法识别部分物品，整理已停止。"
                    : traditionalChinese ? "無法識別部分物品，整理已停止。" : "Some items could not be identified. Arrangement stopped.");
                addText(language, Unsupported, simplifiedChinese
                    ? "暂不支持部分物品的效果，本次未整理。"
                    : traditionalChinese ? "暫不支援部分物品的效果，本次未整理。" : "Some item effects are not supported yet. Inventory unchanged.");
                addText(language, HudHard, simplifiedChinese
                    ? "必须"
                    : traditionalChinese ? "必須" : "Must");
                addText(language, HudSoft, simplifiedChinese
                    ? "尽量"
                    : traditionalChinese ? "盡量" : "Try");
                addText(language, HardInfeasible, simplifiedChinese
                    ? "无法同时满足全部「必须」要求，背包保持原样。请调整要求。"
                    : traditionalChinese ? "無法同時滿足全部「必須」要求，背包保持原樣。請調整要求。" : "Your Must requirements cannot all be met. Inventory unchanged; adjust the requirements.");
                addText(language, HardNotFound, simplifiedChinese
                    ? "本次未找到满足全部「必须」要求的摆放，背包保持原样。可尝试精细整理或调整要求。"
                    : traditionalChinese ? "本次未找到滿足全部「必須」要求的擺放，背包保持原樣。可嘗試精細整理或調整要求。" : "No arrangement meeting all Must requirements found this time. Inventory unchanged. Try Thorough or adjust your requirements.");
                addText(language, PositionEffectsUnavailable, simplifiedChinese
                    ? "无法确认物品摆放后的效果，整理已停止。"
                    : traditionalChinese ? "無法確認物品擺放後的效果，整理已停止。" : "Could not confirm item effects after moving. Arrangement stopped.");
                addText(language, Changed, simplifiedChinese
                    ? "背包已发生变化，本次整理已取消。"
                    : traditionalChinese ? "背包已發生變化，本次整理已取消。" : "Inventory changed. Arrangement cancelled.");
                addText(language, InventoryClosed, simplifiedChinese
                    ? "背包已关闭，本次整理已取消。"
                    : traditionalChinese ? "背包已關閉，本次整理已取消。" : "Inventory closed. Arrangement cancelled.");
                addText(language, GameplayContextChanged, simplifiedChinese
                    ? "游戏场景已变化，本次整理已取消。"
                    : traditionalChinese ? "遊戲場景已變化，本次整理已取消。" : "Game context changed. Arrangement cancelled.");
                addText(language, ApplyTimedOut, simplifiedChinese
                    ? "移动物品耗时过长，整理已停止。"
                    : traditionalChinese ? "移動物品耗時過長，整理已停止。" : "Moving items took too long. Arrangement stopped.");
                addText(language, Failed, simplifiedChinese
                    ? "暂时无法完成物品移动，本次未整理。"
                    : traditionalChinese ? "暫時無法完成物品移動，本次未整理。" : "Could not prepare the item moves. Inventory unchanged.");
                addText(language, VerificationFailed, simplifiedChinese
                    ? "物品移动后的效果与预期不符，整理已停止。部分物品可能已移动，请检查背包。"
                    : traditionalChinese ? "物品移動後的效果與預期不符，整理已停止。部分物品可能已移動，請檢查背包。" : "Item effects differed from what was expected. Arrangement stopped. Some items may have moved; check your inventory.");
                addText(language, Busy, simplifiedChinese
                    ? "正在整理背包。"
                    : traditionalChinese ? "正在整理背包。" : "Inventory arrangement is already in progress.");
                addText(language, FinishMovingItem, simplifiedChinese
                    ? "请先放下或取消当前拿起的物品。"
                    : traditionalChinese
                        ? "請先放下或取消目前拿起的物品。"
                        : "Place or cancel the item you are holding first.");
                addText(language, MovingItemInterrupted, simplifiedChinese
                    ? "你拿起了物品，本次整理已停止。"
                    : traditionalChinese ? "你拿起了物品，本次整理已停止。" : "Arrangement stopped because you picked up an item.");
                addText(language, DisabledForGameplayContext, simplifiedChinese
                    ? "整理出错，本层暂时无法使用。"
                    : traditionalChinese ? "整理出錯，本層暫時無法使用。" : "Arrangement failed and is unavailable for this floor.");
                addText(language, SettingOptimizationTendency, simplifiedChinese
                    ? "智能整理方式"
                    : traditionalChinese ? "智慧整理方式" : "Smart Arrange mode");
                addText(language, HelpOptimizationTendency, simplifiedChinese
                    ? "自动兼顾耗时和效果；快速减少等待；精细花更多时间寻找更好的摆放。所有方式都遵循你的规则优先级。"
                    : traditionalChinese ? "自動兼顧耗時和效果；快速減少等待；精細花更多時間尋找更好的擺放。所有方式都遵循你的規則優先級。" : "Automatic balances time and results. Quick reduces waiting. Thorough spends more time finding a better arrangement. All modes follow your rule priorities.");
                addText(language, OptimizationTendencyKeys[0],
                    simplifiedChinese ? "自动"
                    : traditionalChinese ? "自動" : "Automatic");
                addText(language, OptimizationTendencyKeys[1], simplifiedChinese
                    ? "快速"
                    : traditionalChinese ? "快速" : "Quick");
                addText(language, OptimizationTendencyKeys[2], simplifiedChinese
                    ? "精细"
                    : traditionalChinese ? "精細" : "Thorough");
                addText(language, HudTitle, simplifiedChinese
                    ? "智能整理"
                    : traditionalChinese ? "智慧整理" : "SMART ARRANGE");
                addText(language, HudComboTargets, simplifiedChinese
                    ? "连击设置"
                    : traditionalChinese ? "連擊設定" : "COMBO SETTINGS");
                addText(language, HudOptimize, simplifiedChinese
                    ? "整理"
                    : traditionalChinese ? "整理" : "ARRANGE");
                addText(language, HudMarkArtifacts, simplifiedChinese
                    ? "标记神器"
                    : traditionalChinese ? "標記神器" : "MARK ARTIFACTS");
                addText(language, HudFinishMarking, simplifiedChinese
                    ? "完成标记"
                    : traditionalChinese ? "完成標記" : "FINISH MARKING");
                addText(language, HudMarkingHint, simplifiedChinese
                    ? "选择本次探索的优先神器 · 已标记 {0} 件"
                    : traditionalChinese ? "選擇本次探索的優先神器 · 已標記 {0} 件" : "Select priority artifacts for this run · {0} marked");
                addText(language, HudMarkedCount, simplifiedChinese
                    ? "本次探索：{0} 件优先神器"
                    : traditionalChinese ? "本次探索：{0} 件優先神器" : "{0} priority artifacts for this run");
                addText(language, HudMarkedAndAdjustmentCount, simplifiedChinese
                    ? "优先神器 {0} 件 · 其他要求 {1} 项"
                    : traditionalChinese ? "優先神器 {0} 件 · 其他要求 {1} 項" : "{0} prioritized · {1} other requirements");
                addText(language, HudPriorityQueue, simplifiedChinese
                    ? "优先神器 · 按顺序满足"
                    : traditionalChinese ? "優先神器 · 按順序滿足" : "PRIORITY · IN ORDER");
                addText(language, HudAvoidZone, simplifiedChinese
                    ? "保持不生效 · 同等优先"
                    : traditionalChinese ? "保持不生效 · 同等優先" : "KEEP INACTIVE · EQUAL PRIORITY");
                addText(language, HudIntentBoardHint, simplifiedChinese
                    ? "点击或拖动标记换位；右键移除标记\n{0}\n绿：满足　黄：部分　红：未满足"
                    : traditionalChinese ? "點擊或拖動標記換位；右鍵移除標記\n{0}\n綠：滿足　黃：部分　紅：未滿足" : "Click/drag marks; right-click removes marks.\n{0}\nGreen met · Yellow partial · Red unmet");
                addText(language, HudEditGoals, simplifiedChinese
                    ? "调整要求"
                    : traditionalChinese ? "調整要求" : "EDIT GOALS");
                addText(language, HudEditGoalsShortcut, simplifiedChinese
                    ? "{0}：调整要求"
                    : traditionalChinese ? "{0}：調整要求" : "{0}: edit goals");
                addText(language, HudConstraintHelp, simplifiedChinese
                    ? "尽量：争取满足；必须（!）：全部满足才整理。"
                    : traditionalChinese ? "盡量：爭取滿足；必須（!）：全部滿足才整理。" : "Try: best effort. Must (!): required to arrange.");
                addText(language, HudComboPersistence, simplifiedChinese
                    ? "连击设置跨探索保留。必须：全部满足才整理。"
                    : traditionalChinese ? "連擊設定跨探索保留。必須：全部滿足才整理。" : "Saved for future runs. Must: required to arrange.");
                addText(language, HudControllerBoardHint, simplifiedChinese
                    ? "确认：移动标记；{0}：移除标记\n选中神器后可调整要求\n绿：满足　黄：部分　红：未满足"
                    : traditionalChinese ? "確認：移動標記；{0}：移除標記\n選取神器後可調整要求\n綠：滿足　黃：部分　紅：未滿足" : "Confirm: move mark; {0}: remove mark\nSelect an artifact to edit goals\nGreen met · Yellow partial · Red unmet");
                addText(language, HudControllerChooseIntentSlot, simplifiedChinese
                    ? "选择格子并确认，放下或交换标记。\n{0}：取消"
                    : traditionalChinese ? "選擇格子並確認，放下或交換標記。\n{0}：取消" : "Choose a slot and confirm to place or swap.\n{0}: cancel");
                addText(language, HudLevelEditUnbound, simplifiedChinese
                    ? "选中神器后，点击「调整要求」"
                    : traditionalChinese ? "選取神器後，點擊「調整要求」" : "Select an artifact, then choose Edit goals");
                addText(language, HudChooseIntentSlot, simplifiedChinese
                    ? "选择格子放下或交换。\n右键取消；滚轮翻页。"
                    : traditionalChinese
                        ? "選擇格子放下或交換。\n右鍵取消；滾輪翻頁。"
                        : "Choose a slot to place or swap.\nRight-click to cancel.\nScroll to turn pages.");
                addText(language, HudOpen, simplifiedChinese
                    ? "智能整理"
                    : traditionalChinese ? "智慧整理" : "SMART ARRANGE");
                addText(language, HudAdjustTargets, simplifiedChinese
                    ? "连击设置"
                    : traditionalChinese ? "連擊設定" : "COMBO SETTINGS");
                addText(language, HudHideTargets, simplifiedChinese
                    ? "神器设置"
                    : traditionalChinese ? "神器設定" : "ARTIFACT SETTINGS");
                addText(language, HudAutomaticPreset, simplifiedChinese
                    ? "整理时参考游戏预设"
                    : traditionalChinese ? "整理時參考遊戲預設" : "Uses your game preset when arranging");
                addText(language, HudAutomaticInventory, simplifiedChinese
                    ? "按当前背包自动整理"
                    : traditionalChinese ? "依目前背包自動整理" : "Uses your current items when arranging");
                addText(language, HudAdjustmentCount, simplifiedChinese
                    ? "已设置 {0} 项要求"
                    : traditionalChinese ? "已設定 {0} 項要求" : "{0} requirements set");
                addText(language, HudEnabled, simplifiedChinese
                    ? "只需生效" : traditionalChinese ? "只需生效" : "Keep active");
                addText(language, HudAutomaticTarget, simplifiedChinese
                    ? "自动"
                    : traditionalChinese ? "自動" : "Automatic");
                addText(language, HudMinimumLevel, simplifiedChinese
                    ? "至少 {0} 级" : traditionalChinese ? "至少 {0} 級" : "Level {0} or higher");
                addText(language, HudArtifactAuto, simplifiedChinese
                    ? "自动 · {0}" : traditionalChinese ? "自動 · {0}" : "Auto · {0}");
                addText(language, HudArtifactSafeAuto, simplifiedChinese
                    ? "自动（控制负面效果）· {0}"
                    : traditionalChinese ? "自動（控制負面效果）· {0}" : "Auto (limit penalties) · {0}");
                addText(language, HudResultPending, simplifiedChinese
                    ? "暂无有效结果"
                    : traditionalChinese ? "暫無有效結果" : "No current result");
                addText(language, HudResultSatisfied, simplifiedChinese
                    ? "已满足"
                    : traditionalChinese ? "已滿足" : "Met");
                addText(language, HudResultPartial, simplifiedChinese
                    ? "部分满足"
                    : traditionalChinese ? "部分滿足" : "Partly met");
                addText(language, HudResultUnmet, simplifiedChinese
                    ? "本次未满足"
                    : traditionalChinese ? "本次未滿足" : "Unmet");
                addText(language, HudCurrentLevel, simplifiedChinese
                    ? "当前 {0} 级"
                    : traditionalChinese ? "目前 {0} 級" : "Level {0}");
                addText(language, HudCurrentActive, simplifiedChinese
                    ? "已生效"
                    : traditionalChinese ? "已生效" : "Active");
                addText(language, HudCurrentInactive, simplifiedChinese
                    ? "未生效"
                    : traditionalChinese ? "未生效" : "Inactive");
                addText(language, HudAvoidGoal, simplifiedChinese
                    ? "保持不生效" : traditionalChinese ? "保持不生效" : "Keep inactive");
                addText(language, HudMinimumCount, simplifiedChinese
                    ? "至少 {0}"
                    : traditionalChinese ? "至少 {0}" : "MIN {0}");
                addText(language, HudMaximumCount, simplifiedChinese
                    ? "最多 {0}"
                    : traditionalChinese ? "最多 {0}" : "MAX {0}");
                addText(language, HudNoMinimumCount, simplifiedChinese
                    ? "不设下限"
                    : traditionalChinese ? "不設下限" : "No minimum");
                addText(language, HudNoTargets, simplifiedChinese
                    ? "暂无连击可设置"
                    : traditionalChinese ? "暫無連擊可設定" : "No combos to configure");
                addText(language, HudPage, simplifiedChinese
                    ? "第 {0}/{1} 页"
                    : traditionalChinese
                        ? "第 {0}/{1} 頁"
                        : "PAGE {0}/{1}");
                addText(language, HudSearching, simplifiedChinese
                    ? "正在寻找更好的摆放……"
                    : traditionalChinese ? "正在尋找更好的擺放……" : "Finding a better arrangement…");
                addText(language, HudApplying, simplifiedChinese
                    ? "正在移动物品……"
                    : traditionalChinese ? "正在移動物品……" : "Moving items…");
                addText(language, PreferenceChoiceKeys[0], simplifiedChinese
                    ? "自动" : traditionalChinese ? "自動" : "AUTO");
                addText(language, PreferenceChoiceKeys[1], simplifiedChinese
                    ? "至少"
                    : traditionalChinese ? "至少" : "MIN");
                addText(language, PreferenceChoiceKeys[2], simplifiedChinese
                    ? "最多"
                    : traditionalChinese ? "最多" : "MAX");
            }
        }
    }
}
