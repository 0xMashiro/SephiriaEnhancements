using System;
using System.Collections.Generic;
using System.Globalization;

namespace SephiriaEnhancements.MultiplayerRules.Presentation
{
    internal static class MultiplayerRulesLocalization
    {
        internal const string Section = "SephiriaEnhancements.MultiplayerRules.Section";
        internal const string PresetSetting = "SephiriaEnhancements.MultiplayerRules.PresetSetting";
        internal const string PresetHelp = "SephiriaEnhancements.MultiplayerRules.PresetHelp";
        internal const string ExternalRuleStackingSetting =
            "SephiriaEnhancements.MultiplayerRules.ExternalRuleStacking";
        internal const string ExternalRuleStackingHelp =
            "SephiriaEnhancements.MultiplayerRules.ExternalRuleStacking.Help";
        internal const string ParticipantCountSetting = "SephiriaEnhancements.MultiplayerRules.ParticipantCount";
        internal const string ParticipantCountHelp = "SephiriaEnhancements.MultiplayerRules.ParticipantCount.Help";
        internal const string CopyParticipantValuesSetting = "SephiriaEnhancements.MultiplayerRules.CopyParticipantValues";
        internal const string CopyParticipantValuesHelp = "SephiriaEnhancements.MultiplayerRules.CopyParticipantValues.Help";
        internal const string SelectCopyTarget = "SephiriaEnhancements.MultiplayerRules.CopyParticipantValues.SelectTarget";
        internal const string HealthCombinationSetting = "SephiriaEnhancements.MultiplayerRules.HealthCombination";
        internal const string HealthCombinationHelp = "SephiriaEnhancements.MultiplayerRules.HealthCombination.Help";
        internal const string OriginalPreset = "SephiriaEnhancements.MultiplayerRules.Preset.Original";
        internal const string OptimizedPreset = "SephiriaEnhancements.MultiplayerRules.Preset.Optimized";
        internal const string CustomPreset = "SephiriaEnhancements.MultiplayerRules.Preset.Custom";
        internal const string UseGameBehavior = "SephiriaEnhancements.MultiplayerRules.Value.UseGameBehavior";
        internal const string ToggleDisabled = "SephiriaEnhancements.MultiplayerRules.Value.Disabled";
        internal const string ToggleEnabled = "SephiriaEnhancements.MultiplayerRules.Value.Enabled";
        internal const string GroupSpawnAndDifficulty = "SephiriaEnhancements.MultiplayerRules.Group.SpawnAndDifficulty";
        internal const string GroupEnemyStats = "SephiriaEnhancements.MultiplayerRules.Group.EnemyStats";
        internal const string GroupEncountersAndBosses = "SephiriaEnhancements.MultiplayerRules.Group.EncountersAndBosses";
        internal const string GroupRewardsAndSupplies = "SephiriaEnhancements.MultiplayerRules.Group.RewardsAndSupplies";
        internal const string GroupMerchants = "SephiriaEnhancements.MultiplayerRules.Group.Merchants";
        internal const string GroupQliphoth = "SephiriaEnhancements.MultiplayerRules.Group.Qliphoth";
        internal const string RuleGroupSetting =
            "SephiriaEnhancements.MultiplayerRules.RuleGroup";
        internal const string RuleGroupHelp =
            "SephiriaEnhancements.MultiplayerRules.RuleGroup.Help";

        internal static readonly string[] PresetKeys =
        {
            OriginalPreset, OptimizedPreset, CustomPreset
        };

        internal static readonly string[] HealthCombinationKeys =
        {
            "SephiriaEnhancements.MultiplayerRules.HealthCombination.ParticipantRuleOnly",
            "SephiriaEnhancements.MultiplayerRules.HealthCombination.Additive",
            "SephiriaEnhancements.MultiplayerRules.HealthCombination.Multiplicative"
        };

        private static readonly string[] CommonTextKeys =
        {
            Section, PresetSetting, PresetHelp, ExternalRuleStackingSetting,
            ExternalRuleStackingHelp, ParticipantCountSetting,
            ParticipantCountHelp, CopyParticipantValuesSetting,
            CopyParticipantValuesHelp, SelectCopyTarget,
            HealthCombinationSetting, HealthCombinationHelp,
            OriginalPreset, OptimizedPreset, CustomPreset, UseGameBehavior,
            HealthCombinationKeys[0], HealthCombinationKeys[1],
            HealthCombinationKeys[2], ToggleDisabled, ToggleEnabled,
            GroupSpawnAndDifficulty, GroupEnemyStats, GroupEncountersAndBosses,
            GroupRewardsAndSupplies, GroupMerchants, GroupQliphoth,
            RuleGroupSetting, RuleGroupHelp
        };

        private static readonly Dictionary<string, string[]> CommonTexts = new()
        {
            ["en-US"] = new[]
            {
                "Multiplayer", "Rule Preset",
                "The host's selection is frozen when exploration starts. Original delegates every value to the current game. Optimized fixes only confirmed health-scaling anomalies. Custom enables the rules below.",
                "Stack Rules with Multiplayer Extensions", "Advanced compatibility option. Disabled lets detected multiplayer extensions own scaling and other rules. Enable only when you intentionally want both rule systems to apply; parties above four always use external or game behavior.",
                "Editing Participant Count", "Select which 1–4 participant value the custom rows edit.",
                "Copy Current Participant Values", "Copy every custom rule from the currently edited participant count to the selected participant count. The target values are overwritten immediately.", "Select target",
                "Health Modifier Combination", "Controls how a custom participant health multiplier combines with floor and Hard Mode health modifiers.",
                "Original", "Optimized", "Custom", "Use game behavior",
                "Participant rule only", "Additive", "Multiplicative", "Disabled", "Enabled",
                "Spawning and Difficulty", "Enemy Stats", "Encounters and Bosses",
                "Rewards and Supplies", "Merchants", "Qliphoth",
                "Rule Group", "Choose which custom multiplayer-rule group is shown below."
            },
            ["zh-CN"] = new[]
            {
                "多人游戏", "规则预设",
                "开始探索时冻结主机选择。原版将每项数值交给当前游戏处理；优化仅修正确认的生命缩放异常；自定义启用下方规则。",
                "与联机扩展叠加规则", "高级兼容选项。禁用时，由检测到的联机扩展负责缩放与其他规则。仅在明确希望两套规则同时生效时启用；超过四人的队伍始终使用外部扩展或游戏行为。",
                "正在编辑的参与人数", "选择下方自定义参数当前编辑 1–4 人中的哪一组。",
                "复制当前人数参数", "将当前正在编辑人数的全部自定义规则复制到所选人数。目标人数的参数会立即被覆盖。", "选择目标人数",
                "生命修正组合方式", "决定自定义人数生命倍率如何与楼层及困难模式生命修正组合。",
                "原版", "优化", "自定义", "使用游戏行为",
                "仅人数规则", "相加", "相乘", "禁用", "启用",
                "生成与难度", "敌人属性", "遭遇与 Boss", "奖励与补给", "商人", "克里弗",
                "规则分组", "选择下方显示的自定义多人游戏规则组。"
            },
            ["zh-TW"] = new[]
            {
                "多人遊戲", "規則預設",
                "開始探索時凍結主機選擇。原版將每項數值交給目前遊戲處理；最佳化僅修正已確認的生命縮放異常；自訂啟用下方規則。",
                "與連線擴充套件疊加規則", "進階相容選項。停用時，由偵測到的連線擴充套件負責縮放與其他規則。僅在明確希望兩套規則同時生效時啟用；超過四人的隊伍一律使用外部擴充套件或遊戲行為。",
                "正在編輯的參與人數", "選擇下方自訂參數目前編輯 1–4 人中的哪一組。",
                "複製目前人數參數", "將目前正在編輯人數的全部自訂規則複製到所選人數。目標人數的參數會立即被覆寫。", "選擇目標人數",
                "生命修正組合方式", "決定自訂人數生命倍率如何與樓層及困難模式生命修正組合。",
                "原版", "最佳化", "自訂", "使用遊戲行為",
                "僅人數規則", "相加", "相乘", "停用", "啟用",
                "產生與難度", "敵人屬性", "遭遇與 Boss", "獎勵與補給", "商人", "克里弗",
                "規則分組", "選擇下方顯示的自訂多人遊戲規則群組。"
            }
        };

        private static readonly Dictionary<MultiplayerRuleId, string[]> RuleTexts = new()
        {
            [MultiplayerRuleId.MonsterSpawnEntryMultiplier] = T("Monster-spawn entry multiplier", "Multiplier applied to generated monster-spawn entries.", "刷怪条目倍率", "生成怪物刷怪条目时使用的倍率。", "刷怪條目倍率", "產生怪物刷怪條目時使用的倍率。"),
            [MultiplayerRuleId.EnemyGroupDifficultyOffset] = T("Enemy-group difficulty offset", "Offset used when selecting floor enemy-group data.", "敌群难度偏移", "选择楼层敌群数据时使用的难度偏移。", "敵群難度偏移", "選擇樓層敵群資料時使用的難度偏移。"),
            [MultiplayerRuleId.RegularEnemyHealthMultiplier] = T("Regular enemy health", "Health multiplier for regular enemies.", "普通敌人生命倍率", "普通敌人生成时的生命倍率。", "普通敵人生命倍率", "普通敵人產生時的生命倍率。"),
            [MultiplayerRuleId.RegularEnemyDamageBonus] = T("Regular enemy damage bonus", "Percentage points added to regular enemies' damage bonus.", "普通敌人伤害加成", "为普通敌人的伤害加成增加的百分点。", "普通敵人傷害加成", "為普通敵人的傷害加成增加的百分點。"),
            [MultiplayerRuleId.EliteEnemyHealthMultiplier] = T("Elite enemy health", "Health multiplier for elite enemies and minibosses.", "强敌与小 Boss 生命倍率", "强敌和小 Boss 生成时的生命倍率。", "強敵與小 Boss 生命倍率", "強敵和小 Boss 產生時的生命倍率。"),
            [MultiplayerRuleId.EliteEnemyDamageBonus] = T("Elite enemy damage bonus", "Percentage points added to elite enemies' and minibosses' damage bonus.", "强敌与小 Boss 伤害加成", "为强敌和小 Boss 的伤害加成增加的百分点。", "強敵與小 Boss 傷害加成", "為強敵和小 Boss 的傷害加成增加的百分點。"),
            [MultiplayerRuleId.StandardBossHealthMultiplier] = T("Standard boss health", "Health multiplier for standard boss encounters.", "常规 Boss 生命倍率", "常规 Boss 遭遇中 Boss 生成时的生命倍率。", "一般 Boss 生命倍率", "一般 Boss 遭遇中 Boss 產生時的生命倍率。"),
            [MultiplayerRuleId.BossEncounterDamageBonus] = T("Boss encounter damage bonus", "Percentage points added to standard and Seed-encounter bosses' damage bonus.", "Boss 遭遇伤害加成", "为常规 Boss 与种子遭遇 Boss 的伤害加成增加的百分点。", "Boss 遭遇傷害加成", "為一般 Boss 與種子遭遇 Boss 的傷害加成增加的百分點。"),
            [MultiplayerRuleId.RandomEncounterHealthMultiplier] = T("Random encounter health", "Health multiplier for enemies spawned by random encounters.", "随机遭遇敌人生命倍率", "随机遭遇生成敌人的生命倍率。", "隨機遭遇敵人生命倍率", "隨機遭遇產生敵人的生命倍率。"),
            [MultiplayerRuleId.RandomEncounterDamageBonus] = T("Random encounter damage bonus", "Percentage points added to random-encounter enemies' damage bonus.", "随机遭遇敌人伤害加成", "为随机遭遇敌人的伤害加成增加的百分点。", "隨機遭遇敵人傷害加成", "為隨機遭遇敵人的傷害加成增加的百分點。"),
            [MultiplayerRuleId.RandomEncounterLivingEnemyLimit] = T("Random encounter living-enemy limit", "Maximum simultaneously living enemies before the next random-encounter spawn waits.", "随机遭遇同时存活上限", "达到此存活敌人数后，随机遭遇会等待再生成下一只。", "隨機遭遇同時存活上限", "達到此存活敵人數後，隨機遭遇會等待再產生下一隻。"),
            [MultiplayerRuleId.SeedEncounterBossHealthMultiplier] = T("Seed encounter boss health", "Health multiplier for bosses created by Seed encounters.", "种子遭遇 Boss 生命倍率", "种子遭遇创建 Boss 时使用的生命倍率。", "種子遭遇 Boss 生命倍率", "種子遭遇建立 Boss 時使用的生命倍率。"),
            [MultiplayerRuleId.MindEaterRootSummonHealthMultiplier] = T("Mind-Eater Root summon health", "Health multiplier for summons created by the Mind-Eater Root.", "噬心之根召唤物生命倍率", "噬心之根创建召唤物时使用的生命倍率。", "噬心之根召喚物生命倍率", "噬心之根建立召喚物時使用的生命倍率。"),
            [MultiplayerRuleId.MindEaterRootSummonDamageBonus] = T("Mind-Eater Root summon damage bonus", "Percentage points added to the Mind-Eater Root summons' damage bonus.", "噬心之根召唤物伤害加成", "为噬心之根召唤物的伤害加成增加的百分点。", "噬心之根召喚物傷害加成", "為噬心之根召喚物的傷害加成增加的百分點。"),
            [MultiplayerRuleId.TargetedExperienceOrbDivisor] = T("Targeted experience-orb divisor", "Divisor applied to each targeted experience orb.", "定向经验球除数", "每个定向经验球的数值除数。", "定向經驗球除數", "每個定向經驗球的數值除數。"),
            [MultiplayerRuleId.SharedMoneyAwardFactorPerParticipant] = T("Shared money factor per participant", "Per-participant factor applied when a money pickup is shared with the party.", "每名参与者金币领取系数", "金币拾取物共享给队伍时，每名参与者获得金币所用的系数。", "每名參與者金幣領取係數", "金幣拾取物分享給隊伍時，每名參與者獲得金幣所用的係數。"),
            [MultiplayerRuleId.FestivalOfBloodEnemyHealingMultiplier] = T("Festival of Blood enemy-healing multiplier", "Participant multiplier applied when Festival of Blood heals an attacking enemy.", "血之祭典敌人治疗倍率", "敌人攻击触发血之祭典治疗时使用的参与人数倍率。", "血之祭典敵人治療倍率", "敵人攻擊觸發血之祭典治療時使用的參與人數倍率。"),
            [MultiplayerRuleId.HiddenRoomBreakableRewardCount] = T("Hidden-room breakable reward count", "Number of multiplayer breakable props generated by the applicable hidden-room reward roll.", "隐藏房可破坏物奖励数量", "隐藏房命中对应奖励类型时生成的多人可破坏物数量。", "隱藏房可破壞物獎勵數量", "隱藏房命中對應獎勵類型時產生的多人可破壞物數量。"),
            [MultiplayerRuleId.LifeSupplyOnPositiveProgressFloor] = T("Life supply on each progressed floor", "Whether procedural and library floors with positive progress replace one breakable prop with a life-supply prop.", "进度楼层生命补给", "程序化与图书馆楼层进度大于 0 时，是否将一个可破坏物替换为生命补给。", "進度樓層生命補給", "程序化與圖書館樓層進度大於 0 時，是否將一個可破壞物替換為生命補給。"),
            [MultiplayerRuleId.WanderingMerchantCharmCandidateBonus] = T("Wandering Merchant charm candidate bonus", "Additional charm candidates considered when a Wandering Merchant's stock is generated.", "流浪商人护符候选加成", "生成流浪商人库存时额外参与选择的护符候选数。", "流浪商人護符候選加成", "產生流浪商人庫存時額外參與選擇的護符候選數。"),
            [MultiplayerRuleId.WanderingMerchantTabletCandidateCount] = T("Wandering Merchant tablet candidates", "Stone-tablet candidates considered when a Wandering Merchant's stock is generated.", "流浪商人石板候选数", "生成流浪商人库存时参与选择的石板候选数量。", "流浪商人石板候選數", "產生流浪商人庫存時參與選擇的石板候選數量。"),
            [MultiplayerRuleId.MerchantGuildCharmCandidateBonus] = T("Merchant Guild charm candidate bonus", "Additional charm candidates considered when Merchant Guild stock is generated.", "商业联盟护符候选加成", "生成商业联盟库存时额外参与选择的护符候选数。", "商業公會護符候選加成", "產生商業公會庫存時額外參與選擇的護符候選數。"),
            [MultiplayerRuleId.MerchantGuildTabletCandidateCount] = T("Merchant Guild tablet candidates", "Stone-tablet candidates considered when Merchant Guild stock is generated.", "商业联盟石板候选数", "生成商业联盟库存时参与选择的石板候选数量。", "商業公會石板候選數", "產生商業公會庫存時參與選擇的石板候選數量。"),
            [MultiplayerRuleId.RestorativePotionQuantity] = T("Restorative Potion quantity", "Restorative Potion quantity stocked by applicable wandering merchants.", "再生药水数量", "适用的流浪商人库存中再生药水的数量。", "再生藥水數量", "適用的流浪商人庫存中再生藥水的數量。"),
            [MultiplayerRuleId.RegenerationSamplePotionQuantity] = T("Regeneration Sample quantity", "Potion of Regeneration (Sample) quantity stocked by potion merchants.", "再生药剂（样品）数量", "药水商人库存中再生药剂（样品）的数量。", "再生藥水（試飲樣品）數量", "藥水商人庫存中再生藥水（試飲樣品）的數量。"),
            [MultiplayerRuleId.QliphothSealTeamMultiplier] = T("Qliphoth seal team multiplier", "Team multiplier divided across participants for each seal interaction during the Qliphoth battle.", "克里弗封印团队倍率", "克里弗战斗中每次封印交互前按参与人数分摊的团队倍率。", "克里弗封印團隊倍率", "克里弗戰鬥中每次封印互動前按參與人數分攤的團隊倍率。"),
            [MultiplayerRuleId.QliphothFinalBattleGridRegionCount] = T("Qliphoth final-battle grid regions", "Region count used to partition Qliphoth's grid attack in the final battle.", "克里弗最终战网格区域数", "克里弗最终战中划分网格攻击时使用的区域数量。", "克里弗最終戰網格區域數", "克里弗最終戰中劃分網格攻擊時使用的區域數量。"),
            [MultiplayerRuleId.QliphothFinalBattleEntryAttackTracksParticipant] = T("Qliphoth final-battle entry tracking", "When the target-position entry attack is selected, track a participant instead of choosing a random position.", "克里弗最终战入场攻击追踪", "最终战抽到目标位置型入场攻击时，追踪一名参与者而不是选择随机位置。", "克里弗最終戰進場攻擊追蹤", "最終戰抽到目標位置型進場攻擊時，追蹤一名參與者而不是選擇隨機位置。"),
            [MultiplayerRuleId.QliphothTempleTrioActiveCount] = T("Qliphoth's Temple trio active count", "Number of trio members fighting at the same time in Qliphoth's Temple.", "克里弗神殿三人组同时参战数", "克里弗神殿三人组中同时参战的成员数量。", "克里弗神殿三人組同時參戰數", "克里弗神殿三人組中同時參戰的成員數量。")
        };

        internal static string RuleLabelKey(MultiplayerRuleId id) =>
            "SephiriaEnhancements.MultiplayerRules.Rule." + id;

        internal static string RuleHelpKey(MultiplayerRuleId id) =>
            RuleLabelKey(id) + ".Help";

        internal static string ParticipantCountValueKey(int participantCount) =>
            "SephiriaEnhancements.MultiplayerRules.ParticipantCount.Participants" +
            participantCount;

        internal static string NumericValueKey(MultiplayerRuleDefinition definition,
            int stepIndex)
        {
            float value = definition.Minimum + definition.Step * stepIndex;
            if (definition.Unit == MultiplayerRuleUnit.Toggle)
                return value <= 0f ? ToggleDisabled : ToggleEnabled;
            return "SephiriaEnhancements.MultiplayerRules.Value." + definition.Unit + "." +
                value.ToString("0.####", CultureInfo.InvariantCulture);
        }

        internal static int NumericValueCount(MultiplayerRuleDefinition definition) =>
            (int)Math.Round((definition.Maximum - definition.Minimum) /
                definition.Step) + 1;

        internal static void Register(Action<string, string, string> addText,
            IEnumerable<string> languages)
        {
            foreach (string language in languages)
            {
                var registeredNumericKeys = new HashSet<string>(
                    StringComparer.Ordinal);
                string resolvedLanguage = CommonTexts.ContainsKey(language)
                    ? language : "en-US";
                string[] values = CommonTexts[resolvedLanguage];
                for (int index = 0; index < CommonTextKeys.Length; index++)
                    addText(language, CommonTextKeys[index], values[index]);
                for (int participantCount = 1; participantCount <= 4;
                    participantCount++)
                    addText(language, ParticipantCountValueKey(participantCount),
                        participantCount.ToString(CultureInfo.InvariantCulture));

                int languageOffset = resolvedLanguage == "en-US" ? 0 :
                    resolvedLanguage == "zh-CN" ? 2 : 4;
                foreach (MultiplayerRuleDefinition definition in MultiplayerRuleCatalog.All)
                {
                    string[] ruleText = RuleTexts[definition.Id];
                    addText(language, RuleLabelKey(definition.Id), ruleText[languageOffset]);
                    addText(language, RuleHelpKey(definition.Id), ruleText[languageOffset + 1]);
                    if (definition.Unit == MultiplayerRuleUnit.Toggle) continue;
                    int count = NumericValueCount(definition);
                    for (int stepIndex = 0; stepIndex < count; stepIndex++)
                    {
                        string key = NumericValueKey(definition, stepIndex);
                        if (!registeredNumericKeys.Add(key)) continue;
                        float number = definition.Minimum + definition.Step * stepIndex;
                        addText(language, key, FormatValue(number, definition.Unit));
                    }
                }
            }
        }

        private static string FormatValue(float value, MultiplayerRuleUnit unit)
        {
            string number = value.ToString(value % 1f == 0f ? "0" : "0.##",
                CultureInfo.InvariantCulture);
            return unit switch
            {
                MultiplayerRuleUnit.Multiplier => number + "×",
                MultiplayerRuleUnit.PercentagePoints => "+" + number,
                MultiplayerRuleUnit.DifficultyOffset => "+" + number,
                _ => number
            };
        }

        private static string[] T(string enLabel, string enHelp,
            string zhCnLabel, string zhCnHelp, string zhTwLabel, string zhTwHelp) =>
            new[] { enLabel, enHelp, zhCnLabel, zhCnHelp, zhTwLabel, zhTwHelp };
    }
}
