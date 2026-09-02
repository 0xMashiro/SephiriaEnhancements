#nullable disable
using SephiriaEnhancements.Runtime.Inventory;

using System;
using System.Collections.Generic;

namespace SephiriaEnhancements.Inventory
{
    internal static class InventoryOptimizationLocalization
    {
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
        internal const string HudArtifactsTab =
            "SephiriaEnhancements.InventoryHud.ArtifactsTab";
        internal const string HudCombosTab =
            "SephiriaEnhancements.InventoryHud.CombosTab";
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
            "SephiriaEnhancements.InventoryPreference.Prefer",
            "SephiriaEnhancements.InventoryPreference.Core",
            "SephiriaEnhancements.InventoryPreference.Priority",
            "SephiriaEnhancements.InventoryPreference.Avoid",
            "SephiriaEnhancements.InventoryPreference.Ignored"
        };

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
                    ? "正在分析背包布局……"
                    : traditionalChinese
                        ? "正在分析背包配置……"
                        : "Analyzing inventory layout…");
                addText(language, Applying, simplifiedChinese
                    ? "已找到更优布局，正在应用……"
                    : traditionalChinese
                        ? "已找到更佳配置，正在套用……"
                        : "A better layout was found. Applying it…");
                addText(language, Completed, simplifiedChinese
                    ? "背包优化完成。"
                    : traditionalChinese
                        ? "背包最佳化完成。"
                        : "Inventory optimization complete.");
                addText(language, NoImprovementFound, simplifiedChinese
                    ? "本次搜索未找到优于当前布局的结果。"
                    : traditionalChinese
                        ? "本次搜尋未找到優於目前配置的結果。"
                        : "No layout better than the current one was found in this search.");
                addText(language, Unavailable, simplifiedChinese
                    ? "请先打开普通背包界面再使用背包优化。"
                    : traditionalChinese
                        ? "請先開啟一般背包介面再使用背包最佳化。"
                        : "Open the standard inventory before optimizing it.");
                addText(language, RuntimeNotReady, simplifiedChinese
                    ? "背包状态尚未准备完成，请稍后重试。"
                    : traditionalChinese
                        ? "背包狀態尚未準備完成，請稍後重試。"
                        : "Inventory state is not ready yet. Try again shortly.");
                addText(language, EmptyInventory, simplifiedChinese
                    ? "背包中没有可优化的物品。"
                    : traditionalChinese
                        ? "背包中沒有可最佳化的物品。"
                        : "There are no items to optimize.");
                addText(language, Unsupported, simplifiedChinese
                    ? "当前背包含有尚未验证的机制，已安全跳过优化。"
                    : traditionalChinese
                        ? "目前背包含有尚未驗證的機制，已安全略過最佳化。"
                        : "This inventory contains mechanics that are not yet safely supported.");
                addText(language, Changed, simplifiedChinese
                    ? "背包状态已经变化，本次优化已取消。"
                    : traditionalChinese
                        ? "背包狀態已經變更，本次最佳化已取消。"
                        : "The inventory changed, so this optimization was cancelled.");
                addText(language, InventoryClosed, simplifiedChinese
                    ? "背包界面已关闭，本次优化已取消。"
                    : traditionalChinese
                        ? "背包介面已關閉，本次最佳化已取消。"
                        : "The inventory was closed, so this optimization was cancelled.");
                addText(language, GameplayContextChanged, simplifiedChinese
                    ? "游戏已进入新的楼层或流程，本次优化已取消。"
                    : traditionalChinese
                        ? "遊戲已進入新的樓層或流程，本次最佳化已取消。"
                        : "The game entered a new floor or flow, so this optimization was cancelled.");
                addText(language, ApplyTimedOut, simplifiedChinese
                    ? "应用背包布局超时，本次优化已取消。"
                    : traditionalChinese
                        ? "套用背包配置逾時，本次最佳化已取消。"
                        : "Applying the inventory layout timed out and was cancelled.");
                addText(language, Failed, simplifiedChinese
                    ? "无法生成安全的背包操作计划。"
                    : traditionalChinese
                        ? "無法產生安全的背包操作計畫。"
                        : "A safe inventory operation plan could not be created.");
                addText(language, VerificationFailed, simplifiedChinese
                    ? "布局已应用，但游戏结算与预测不一致；本次优化已停止，可以再次尝试。"
                    : traditionalChinese
                        ? "配置已套用，但遊戲結算與預測不一致；本次最佳化已停止，可以再次嘗試。"
                        : "The layout was applied, but the game's settlement differed from the prediction. This attempt was stopped; you can try again.");
                addText(language, Busy, simplifiedChinese
                    ? "背包优化正在进行中。"
                    : traditionalChinese
                        ? "背包最佳化正在進行中。"
                        : "Inventory optimization is already in progress.");
                addText(language, DisabledForGameplayContext, simplifiedChinese
                    ? "背包优化遇到意外错误，已在当前楼层停用。"
                    : traditionalChinese
                        ? "背包最佳化遇到意外錯誤，已在目前樓層停用。"
                        : "Inventory optimization encountered an unexpected error and was disabled for the current floor.");
                addText(language, SettingOptimizationTendency,
                    simplifiedChinese
                    ? "背包优化倾向"
                    : traditionalChinese
                        ? "背包最佳化傾向"
                        : "Inventory optimization tendency");
                addText(language, HelpOptimizationTendency, simplifiedChinese
                    ? "自动会根据背包规模与预期收益选择搜索方式；稳定倾向减少耗时和改动，激进倾向投入更多时间寻找更高收益。"
                    : traditionalChinese
                        ? "自動會依背包規模與預期收益選擇搜尋方式；穩定傾向減少耗時和改動，積極傾向投入更多時間尋找更高收益。"
                        : "Automatic chooses the search method from inventory size and expected gain. Stable favors less work and fewer changes; Aggressive spends more time seeking greater gains.");
                addText(language, OptimizationTendencyKeys[0],
                    simplifiedChinese ? "自动"
                    : traditionalChinese ? "自動" : "Automatic");
                addText(language, OptimizationTendencyKeys[1],
                    simplifiedChinese ? "稳定"
                    : traditionalChinese ? "穩定" : "Stable");
                addText(language, OptimizationTendencyKeys[2],
                    simplifiedChinese ? "激进"
                    : traditionalChinese ? "積極" : "Aggressive");
                addText(language, HudTitle, simplifiedChinese
                    ? "智能整理"
                    : traditionalChinese ? "智慧整理" : "SMART INVENTORY");
                addText(language, HudArtifactsTab, simplifiedChinese
                    ? "神器"
                    : traditionalChinese ? "神器" : "ARTIFACTS");
                addText(language, HudCombosTab, simplifiedChinese
                    ? "连击"
                    : traditionalChinese ? "連擊" : "COMBOS");
                addText(language, HudOptimize, simplifiedChinese
                    ? "智能优化"
                    : traditionalChinese
                        ? "智慧最佳化"
                        : "SMART OPTIMIZE");
                addText(language, HudMarkArtifacts, simplifiedChinese
                    ? "标记神器"
                    : traditionalChinese ? "標記神器" : "MARK ARTIFACTS");
                addText(language, HudFinishMarking, simplifiedChinese
                    ? "完成标记"
                    : traditionalChinese ? "完成標記" : "FINISH MARKING");
                addText(language, HudMarkingHint, simplifiedChinese
                    ? "选择要临时优先的神器；已标记 {0} 件"
                    : traditionalChinese
                        ? "選擇要暫時優先的神器；已標記 {0} 件"
                        : "Select artifacts to prioritize for this run · {0} marked");
                addText(language, HudMarkedCount, simplifiedChinese
                    ? "本次探索临时优先 {0} 件神器"
                    : traditionalChinese
                        ? "本次探索暫時優先 {0} 件神器"
                        : "{0} artifacts prioritized for this run");
                addText(language, HudMarkedAndAdjustmentCount,
                    simplifiedChinese
                    ? "临时优先 {0} 件神器，另有 {1} 项目标调整"
                    : traditionalChinese
                        ? "暫時優先 {0} 件神器，另有 {1} 項目標調整"
                        : "{0} artifacts prioritized · {1} other goal adjustments");
                addText(language, HudPriorityQueue, simplifiedChinese
                    ? "优先队列 · 越靠前越优先"
                    : traditionalChinese
                        ? "優先佇列 · 越靠前越優先"
                        : "PRIORITY QUEUE · LEFTMOST FIRST");
                addText(language, HudAvoidZone, simplifiedChinese
                    ? "排除区 · 优先保持不生效"
                    : traditionalChinese
                        ? "排除區 · 優先保持不生效"
                        : "EXCLUSION · KEEP INACTIVE FIRST");
                addText(language, HudIntentBoardHint, simplifiedChinese
                    ? "从背包拖入神器；右键格子移除"
                    : traditionalChinese
                        ? "從背包拖入神器；右鍵格子移除"
                        : "Drag artifacts from the inventory · right-click a slot to remove");
                addText(language, HudChooseIntentSlot, simplifiedChinese
                    ? "已选中神器：用方向键选择目标格，再按确认放入"
                    : traditionalChinese
                        ? "已選中神器：用方向鍵選擇目標格，再按確認放入"
                        : "Artifact selected · choose a slot and confirm to place");
                addText(language, HudOpen, simplifiedChinese
                    ? "智能整理"
                    : traditionalChinese ? "智慧整理" : "SMART ARRANGE");
                addText(language, HudAdjustTargets, simplifiedChinese
                    ? "调整本次探索目标"
                    : traditionalChinese
                        ? "調整本次探索目標"
                        : "ADJUST THIS RUN'S GOALS");
                addText(language, HudHideTargets, simplifiedChinese
                    ? "收起目标调整"
                    : traditionalChinese
                        ? "收起目標調整"
                        : "HIDE GOAL ADJUSTMENTS");
                addText(language, HudAutomaticPreset, simplifiedChinese
                    ? "自动参考当前游戏预设"
                    : traditionalChinese
                        ? "自動參考目前遊戲預設"
                        : "Automatically using the current game preset");
                addText(language, HudAutomaticInventory, simplifiedChinese
                    ? "自动分析当前背包"
                    : traditionalChinese
                        ? "自動分析目前背包"
                        : "Automatically analyzing the current inventory");
                addText(language, HudAdjustmentCount, simplifiedChinese
                    ? "本次探索有 {0} 项目标调整"
                    : traditionalChinese
                        ? "本次探索有 {0} 項目標調整"
                        : "{0} goal adjustments for this run");
                addText(language, HudEnabled, simplifiedChinese
                    ? "启用" : traditionalChinese ? "啟用" : "ON");
                addText(language, HudNoTargets, simplifiedChinese
                    ? "当前背包没有此类目标"
                    : traditionalChinese
                        ? "目前背包沒有此類目標"
                        : "No targets of this type in the inventory");
                addText(language, HudPage, simplifiedChinese
                    ? "第 {0}/{1} 页"
                    : traditionalChinese
                        ? "第 {0}/{1} 頁"
                        : "PAGE {0}/{1}");
                addText(language, HudSearching, simplifiedChinese
                    ? "正在按当前目标搜索……"
                    : traditionalChinese
                        ? "正在依目前目標搜尋……"
                        : "Searching with the current targets…");
                addText(language, HudApplying, simplifiedChinese
                    ? "正在通过原生背包操作应用……"
                    : traditionalChinese
                        ? "正在透過原生背包操作套用……"
                        : "Applying through native inventory operations…");
                addText(language, PreferenceChoiceKeys[0], simplifiedChinese
                    ? "自动" : traditionalChinese ? "自動" : "AUTO");
                addText(language, PreferenceChoiceKeys[1], simplifiedChinese
                    ? "偏好" : traditionalChinese ? "偏好" : "PREFER");
                addText(language, PreferenceChoiceKeys[2], simplifiedChinese
                    ? "核心" : traditionalChinese ? "核心" : "CORE");
                addText(language, PreferenceChoiceKeys[3], simplifiedChinese
                    ? "优先" : traditionalChinese ? "優先" : "PRIORITY");
                addText(language, PreferenceChoiceKeys[4], simplifiedChinese
                    ? "避免" : traditionalChinese ? "避免" : "AVOID");
                addText(language, PreferenceChoiceKeys[5], simplifiedChinese
                    ? "忽略" : traditionalChinese ? "忽略" : "IGNORE");
            }
        }
    }
}
