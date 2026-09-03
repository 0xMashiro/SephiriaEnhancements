using System;
using System.Collections.Generic;
using SephiriaEnhancements.MultiplayerRules.Presentation;
using SephiriaEnhancements.MultiplayerAccess.Presentation;
using SephiriaEnhancements.CombatVisuals;

namespace SephiriaEnhancements.Configuration
{
    internal static class ModLocalization
    {
        internal const string Section = "SephiriaEnhancements.Section";
        internal const string SettingMasterEnabled = "SephiriaEnhancements.Setting.Enabled";
        internal const string HelpMasterEnabled = "SephiriaEnhancements.Help.Enabled";
        internal const string SettingCombatRelationOutlines =
            "SephiriaEnhancements.Setting.CombatRelationOutlines";
        internal const string HelpCombatRelationOutlines =
            "SephiriaEnhancements.Help.CombatRelationOutlines";
        internal const string SettingHitStreakFeedback =
            "SephiriaEnhancements.Setting.HitStreakFeedback";
        internal const string SettingDamageStatisticsScale =
            "SephiriaEnhancements.Setting.DamageStatisticsScale";
        internal const string HelpHitStreakFeedback =
            "SephiriaEnhancements.Help.HitStreakFeedback";
        internal const string HelpDamageStatisticsScale =
            "SephiriaEnhancements.Help.DamageStatisticsScale";
        internal const string SettingDisplayPolicy = "SephiriaEnhancements.Setting.DisplayPolicy";
        internal const string HelpDisplayPolicy = "SephiriaEnhancements.Help.DisplayPolicy";
        internal const string SettingNativeCompanion =
            "SephiriaEnhancements.Setting.NativeCompanion";
        internal const string HelpNativeCompanion =
            "SephiriaEnhancements.Help.NativeCompanion";
        internal const string SettingDeveloperConsole =
            "SephiriaEnhancements.Setting.DeveloperConsole";
        internal const string HelpDeveloperConsole =
            "SephiriaEnhancements.Help.DeveloperConsole";
        internal const string SettingDeveloperPlayerDamage =
            "SephiriaEnhancements.Setting.DeveloperPlayerDamage";
        internal const string HelpDeveloperPlayerDamage =
            "SephiriaEnhancements.Help.DeveloperPlayerDamage";
        internal const string SettingDefeatRetry =
            "SephiriaEnhancements.Setting.DefeatRetry";
        internal const string HelpDefeatRetry =
            "SephiriaEnhancements.Help.DefeatRetry";
        internal const string DeveloperConsoleShortcut =
            "SephiriaEnhancements.Controls.DeveloperConsole";
        internal const string SuiteOff = "SephiriaEnhancements.Suite.Off";
        internal const string SuiteOn = "SephiriaEnhancements.Suite.On";
        internal const string InsightsDisabled =
            "SephiriaEnhancements.CombatInsights.Disabled";
        internal const string DamageStatisticsDisplayHidden =
            "SephiriaEnhancements.CombatInsights.DisplayHidden";
        internal const string DamageStatisticsDisplayRestored =
            "SephiriaEnhancements.CombatInsights.DisplayRestored";
        internal const string EncounterReportOpened =
            "SephiriaEnhancements.CombatInsights.ReportOpened";
        internal const string EncounterReportClosed =
            "SephiriaEnhancements.CombatInsights.ReportClosed";
        internal const string EncounterReportUnavailable =
            "SephiriaEnhancements.CombatInsights.ReportUnavailable";
        internal const string EncounterReportLoading =
            "SephiriaEnhancements.CombatInsights.ReportLoading";
        internal const string EncounterReportScreenTransition =
            "SephiriaEnhancements.CombatInsights.ReportScreenTransition";
        internal const string EncounterReportCutscene =
            "SephiriaEnhancements.CombatInsights.ReportCutscene";
        internal const string EncounterReportMenu =
            "SephiriaEnhancements.CombatInsights.ReportMenu";
        internal const string EncounterReportHudUnavailable =
            "SephiriaEnhancements.CombatInsights.ReportHudUnavailable";
        internal const string DeveloperConsoleOff =
            "SephiriaEnhancements.DeveloperConsole.Off";
        internal const string DeveloperConsoleOn =
            "SephiriaEnhancements.DeveloperConsole.On";
        internal static readonly string[] DeveloperPlayerDamageMultiplierKeys =
        {
            "SephiriaEnhancements.DeveloperPlayerDamage.1x",
            "SephiriaEnhancements.DeveloperPlayerDamage.2x",
            "SephiriaEnhancements.DeveloperPlayerDamage.5x",
            "SephiriaEnhancements.DeveloperPlayerDamage.10x",
            "SephiriaEnhancements.DeveloperPlayerDamage.100x"
        };
        internal const string DefeatRetryOff =
            "SephiriaEnhancements.DefeatRetry.Off";
        internal const string DefeatRetryOn =
            "SephiriaEnhancements.DefeatRetry.On";
        internal static readonly string[] NativeCompanionModeKeys =
        {
            SuiteOff,
            "SephiriaEnhancements.NativeCompanion.SoloOnly",
            "SephiriaEnhancements.NativeCompanion.SmartFill",
            "SephiriaEnhancements.NativeCompanion.AlwaysHost"
        };
        internal const string Dps = "SephiriaEnhancements.Hud.Dps";
        internal const string Defeated = "SephiriaEnhancements.Hud.Defeated";
        internal const string FinalBlows = "SephiriaEnhancements.Hud.FinalBlows";
        internal const string NormalEnemy = "SephiriaEnhancements.Hud.Normal";
        // Mirrors EMonsterType.Miniboss; do not broaden this back to "Elite".
        internal const string MinibossEnemy = "SephiriaEnhancements.Hud.Miniboss";
        internal const string BossEnemy = "SephiriaEnhancements.Hud.Boss";
        internal const string CombatSummary = "SephiriaEnhancements.Hud.CombatSummary";
        internal const string DamageShare = "SephiriaEnhancements.Hud.DamageShare";
        internal const string DamageAverageDps =
            "SephiriaEnhancements.Hud.DamageAverageDps";
        internal const string ReportDamage =
            "SephiriaEnhancements.Hud.ReportDamage";
        internal const string ReportShare =
            "SephiriaEnhancements.Hud.ReportShare";
        internal const string ReportAverageDps =
            "SephiriaEnhancements.Hud.ReportAverageDps";
        internal const string ReportDamageMix =
            "SephiriaEnhancements.Hud.ReportDamageMix";
        internal const string DamagePhysical =
            "SephiriaEnhancements.Hud.DamagePhysical";
        internal const string DamageFire =
            "SephiriaEnhancements.Hud.DamageFire";
        internal const string DamageIce =
            "SephiriaEnhancements.Hud.DamageIce";
        internal const string DamageLightning =
            "SephiriaEnhancements.Hud.DamageLightning";
        internal const string DamageChaos =
            "SephiriaEnhancements.Hud.DamageChaos";
        internal const string DamageNormal =
            "SephiriaEnhancements.Hud.DamageNormal";
        internal const string DamageMixed =
            "SephiriaEnhancements.Hud.DamageMixed";
        internal const string DamageOther =
            "SephiriaEnhancements.Hud.DamageOther";
        internal const string Off = "SephiriaEnhancements.Off";
        internal const string On = "SephiriaEnhancements.On";
        internal const string RetryFloor = "SephiriaEnhancements.RetryFloor";
        internal const string RetryBossEncounter =
            "SephiriaEnhancements.RetryBossEncounter";

        internal static readonly string[] ScaleKeys =
        {
            "SephiriaEnhancements.Scale.65", "SephiriaEnhancements.Scale.80",
            "SephiriaEnhancements.Scale.100", "SephiriaEnhancements.Scale.115",
            "SephiriaEnhancements.Scale.130"
        };

        internal static readonly string[] DisplayPolicyKeys =
        {
            "SephiriaEnhancements.DisplayPolicy.Smart",
            "SephiriaEnhancements.DisplayPolicy.BossOnly",
            "SephiriaEnhancements.DisplayPolicy.AllCombat",
            InsightsDisabled
        };

        private static readonly Dictionary<string, Dictionary<string, string>> SuiteTexts =
            new Dictionary<string, Dictionary<string, string>>
            {
                ["en-US"] = Q("Sephiria Enhancements",
                    "Enable or disable every Sephiria Enhancements feature.",
                    "Native combat companion",
                    "Solo Only stays offline. Smart Fill also helps an online host until another player joins. Always for Host keeps the companion with a multiplayer party. It uses no player slot, and remote players need no AddOn.",
                    "Solo only", "Smart fill", "Always for host", "Off", "On"),
                ["zh-CN"] = Q("Sephiria 增强",
                    "启用或停用全部 Sephiria 增强功能。", "原生战斗伙伴",
                    "“仅单机”只在离线游戏中启用；“智能补位”还会在多人游戏中主机独自游戏时陪伴，并在真人加入后脱战离队；“主机始终启用”会在多人队伍中保留伙伴。不占玩家位置，其他玩家无需安装本 MOD。",
                    "仅单机", "智能补位", "主机始终启用", "关闭", "开启"),
                ["zh-TW"] = Q("Sephiria 增強",
                    "啟用或停用全部 Sephiria 增強功能。", "原生戰鬥夥伴",
                    "「僅單機」只在離線遊戲中啟用；「智慧補位」也會在多人遊戲中主機獨自遊戲時陪伴，並在真人加入後脫戰離隊；「主機始終啟用」會在多人隊伍中保留夥伴。不佔玩家位置，其他玩家無需安裝本 MOD。",
                    "僅單機", "智慧補位", "主機始終啟用", "關閉", "開啟")
            };

        private static readonly Dictionary<string, Dictionary<string, string>> InsightsTexts =
            new Dictionary<string, Dictionary<string, string>>
            {
                ["en-US"] = I("Damage statistics", "Smart", "Boss only", "Every fight",
                    "DPS", "defeated", "my final blows", "normal", "miniboss", "boss",
                    "Combat summary", "DMG · SHARE", "DMG · AVG DPS",
                    "DMG", "SHARE", "AVG DPS",
                    "DAMAGE MIX", "physical", "fire", "ice", "lightning",
                    "chaos", "normal", "mixed", "other",
                    "Smart uses rolling 5-second DPS in ordinary fights. Boss fights use cumulative damage ranking, contribution share, and a live MVP marker. The centered encounter report shows totals, average DPS, elemental damage mix, and defeated enemy types.",
                    "Disabled", "Damage statistics display hidden",
                    "Damage statistics display restored",
                    "Latest combat report opened", "Combat report closed",
                    "No combat report available on this floor",
                    "Statistics display unavailable while loading",
                    "Statistics display unavailable during a screen transition",
                    "Statistics display unavailable during a cutscene",
                    "Close the menu to view statistics",
                    "Combat report display is not ready yet"),
                ["zh-CN"] = I("伤害统计", "智能", "仅 BOSS", "每场战斗",
                    "DPS", "击败", "我的终结", "普通", "小头目", "BOSS",
                    "战斗统计", "DMG · 占比", "DMG · 平均 DPS",
                    "DMG", "占比", "平均 DPS",
                    "伤害构成", "物理", "火焰", "冰霜", "闪电", "混沌",
                    "普通", "混合", "其他",
                    "智能模式在普通战斗中显示近 5 秒 DPS；BOSS 战按累计伤害实时排名，显示贡献占比并标记当前 MVP。居中的单场战报显示总伤害、整场平均 DPS、元素伤害构成与各类敌人击败数量。",
                    "关闭", "伤害统计显示已隐藏", "伤害统计显示已恢复",
                    "已打开最近一场战报", "战报已收起", "本层暂无可查看的战报",
                    "正在加载，暂时无法显示统计", "画面切换中，暂时无法显示统计",
                    "剧情播放中，暂时无法显示统计", "请关闭菜单后查看统计",
                    "战报界面尚未就绪"),
                ["zh-TW"] = I("傷害統計", "智慧", "僅 BOSS", "每場戰鬥",
                    "DPS", "擊敗", "我的終結", "普通", "小頭目", "BOSS",
                    "戰鬥統計", "DMG · 佔比", "DMG · 平均 DPS",
                    "DMG", "佔比", "平均 DPS",
                    "傷害構成", "物理", "火焰", "冰霜", "閃電", "混沌",
                    "普通", "混合", "其他",
                    "智慧模式在普通戰鬥中顯示近 5 秒 DPS；BOSS 戰按累計傷害即時排名，顯示貢獻占比並標記目前 MVP。置中的單場戰報顯示總傷害、整場平均 DPS、元素傷害構成與各類敵人擊敗數量。",
                    "關閉", "傷害統計顯示已隱藏", "傷害統計顯示已恢復",
                    "已開啟最近一場戰報", "戰報已收起", "本層暫無可查看的戰報",
                    "正在載入，暫時無法顯示統計", "畫面切換中，暫時無法顯示統計",
                    "劇情播放中，暫時無法顯示統計", "請關閉選單後查看統計",
                    "戰報介面尚未就緒")
            };

        private static readonly Dictionary<string, Dictionary<string, string>> Texts =
            new Dictionary<string, Dictionary<string, string>>
            {
                ["en-US"] = L("Off", "On"),
                ["zh-CN"] = L("关闭", "开启"),
                ["zh-TW"] = L("關閉", "開啟"),
                ["ko-KR"] = L("끄기", "켜기"),
                ["ja-JP"] = L("オフ", "オン"),
                ["de-DE"] = L("Aus", "Ein"),
                ["es-ES"] = L("No", "Sí"),
                ["fr-FR"] = L("Non", "Oui"),
                ["it-IT"] = L("No", "Sì"),
                ["pl-PL"] = L("Wył.", "Wł."),
                ["pt-BR"] = L("Desl.", "Lig."),
                ["ru-RU"] = L("Выкл.", "Вкл."),
                ["sv-SE"] = L("Av", "På"),
                ["th-TH"] = L("ปิด", "เปิด"),
                ["tr-TR"] = L("Kapalı", "Açık")
            };

        private static readonly Dictionary<string, Dictionary<string, string>> AdditionalTexts =
            new Dictionary<string, Dictionary<string, string>>
            {
                ["en-US"] = A("Statistics size"),
                ["zh-CN"] = A("统计界面大小"),
                ["zh-TW"] = A("統計介面大小"),
                ["ko-KR"] = A("통계 패널 크기"),
                ["ja-JP"] = A("統計パネルのサイズ"),
                ["de-DE"] = A("Größe der Statistik"),
                ["es-ES"] = A("Tamaño de las estadísticas"),
                ["fr-FR"] = A("Taille des statistiques"),
                ["it-IT"] = A("Dimensione statistiche"),
                ["pl-PL"] = A("Rozmiar statystyk"),
                ["pt-BR"] = A("Tamanho das estatísticas"),
                ["ru-RU"] = A("Размер статистики"),
                ["sv-SE"] = A("Statistikstorlek"),
                ["th-TH"] = A("ขนาดแผงสถิติ"),
                ["tr-TR"] = A("İstatistik boyutu")
            };

        private static readonly Dictionary<string, string> RetryFloorTexts =
            new Dictionary<string, string>
            {
                ["en-US"] = "Retry floor",
                ["zh-CN"] = "重试本层",
                ["zh-TW"] = "重試本層",
                ["ko-KR"] = "현재 층 재시작",
                ["ja-JP"] = "この階をやり直す",
                ["de-DE"] = "Ebene wiederholen",
                ["es-ES"] = "Reintentar piso",
                ["fr-FR"] = "Recommencer l'étage",
                ["it-IT"] = "Riprova piano",
                ["pl-PL"] = "Powtórz piętro",
                ["pt-BR"] = "Repetir andar",
                ["ru-RU"] = "Переиграть этаж",
                ["sv-SE"] = "Försök våningen igen",
                ["th-TH"] = "ลองชั้นนี้ใหม่",
                ["tr-TR"] = "Katı yeniden dene"
            };

        private static readonly Dictionary<string, string> RetryBossEncounterTexts =
            new Dictionary<string, string>
            {
                ["en-US"] = "Retry BOSS fight",
                ["zh-CN"] = "重试 BOSS 战",
                ["zh-TW"] = "重試 BOSS 戰",
                ["ko-KR"] = "보스전 재시작",
                ["ja-JP"] = "ボス戦をやり直す",
                ["de-DE"] = "Bosskampf wiederholen",
                ["es-ES"] = "Reintentar jefe",
                ["fr-FR"] = "Recommencer le boss",
                ["it-IT"] = "Riprova il boss",
                ["pl-PL"] = "Powtórz walkę z bossem",
                ["pt-BR"] = "Repetir chefe",
                ["ru-RU"] = "Повторить бой с боссом",
                ["sv-SE"] = "Försök bossen igen",
                ["th-TH"] = "สู้บอสใหม่",
                ["tr-TR"] = "Boss savaşını yeniden dene"
            };

        private static readonly Dictionary<string, Dictionary<string, string>>
            DefeatRetryTexts =
                new Dictionary<string, Dictionary<string, string>>
                {
                    ["en-US"] = R("Retry after defeat",
                        "Disabled by default. After a party wipe, choose to retry from the floor-entry checkpoint or from immediately before the current BOSS fight. Available offline; online play also requires the game's rejoin/midsave support.",
                        "Off", "On"),
                    ["zh-CN"] = R("失败后重试",
                        "默认关闭。全队死亡后，可选择从本层入口检查点或当前 BOSS 战开始前重试。单机可用；多人游戏还需游戏开启重连/中途存档支持。",
                        "关闭", "开启"),
                    ["zh-TW"] = R("失敗後重試",
                        "預設關閉。全隊死亡後，可選擇從本層入口檢查點或目前 BOSS 戰開始前重試。單機可用；多人遊戲還需遊戲開啟重連/中途存檔支援。",
                        "關閉", "開啟")
                };

        private static readonly Dictionary<string, Dictionary<string, string>> HelpTexts =
            new Dictionary<string, Dictionary<string, string>>
            {
                // "combo" here names the game's native HUD text, not CombatInsights hit streaks.
                ["en-US"] = H("Scale only the statistics panel; the game's native HUD and combo text are unchanged."),
                ["zh-CN"] = H("仅缩放统计界面，不改变游戏原生 HUD 与连击文字。"),
                ["zh-TW"] = H("僅縮放統計介面，不改變遊戲原生 HUD 與連擊文字。"),
                ["ko-KR"] = H("게임 HUD는 그대로 두고 통계 패널만 조절합니다."),
                ["ja-JP"] = H("ゲーム本体の HUD は変えず、統計パネルのみ拡大縮小します。"),
                ["de-DE"] = H("Skaliert nur die Statistik; das Spiel-HUD bleibt unverändert."),
                ["es-ES"] = H("Escala solo las estadísticas; no altera el HUD del juego."),
                ["fr-FR"] = H("Redimensionne uniquement les statistiques, sans modifier le HUD du jeu."),
                ["it-IT"] = H("Ridimensiona solo le statistiche; l'HUD del gioco non cambia."),
                ["pl-PL"] = H("Skaluje tylko statystyki; HUD gry pozostaje bez zmian."),
                ["pt-BR"] = H("Redimensiona apenas as estatísticas; o HUD do jogo não muda."),
                ["ru-RU"] = H("Масштабирует только статистику, не меняя интерфейс игры."),
                ["sv-SE"] = H("Skalar bara statistiken; spelets HUD ändras inte."),
                ["th-TH"] = H("ปรับขนาดเฉพาะสถิติโดยไม่เปลี่ยน HUD ของเกม"),
                ["tr-TR"] = H("Yalnızca istatistikleri ölçekler; oyun HUD'u değişmez.")
            };

        private static readonly Dictionary<string, Dictionary<string, string>>
            HitStreakFeedbackTexts =
            new Dictionary<string, Dictionary<string, string>>
            {
                ["en-US"] = HitStreak("Hit-streak indicator", "Show the local player's consecutive-hit count at the hit position. Critical hits, executions, and milestones receive stronger emphasis. Identified damage-over-time ticks do not extend the streak."),
                ["zh-CN"] = HitStreak("连续命中提示", "在实际命中位置显示本地玩家的连续命中计数；暴击、处决与里程碑会获得更强强调。游戏可识别的持续伤害不会延长连续命中。"),
                ["zh-TW"] = HitStreak("連續命中提示", "在實際命中位置顯示本機玩家的連續命中計數；暴擊、處決與里程碑會獲得更強強調。遊戲可辨識的持續傷害不會延長連續命中。"),
                ["ko-KR"] = HitStreak("연속 타격 표시", "최근 타격 위치에 로컬 플레이어의 연속 타격 횟수를 표시합니다. 식별된 지속 피해는 연속 타격을 연장하지 않습니다."),
                ["ja-JP"] = HitStreak("連続ヒット表示", "直近の命中位置にローカルプレイヤーの連続ヒット数を表示します。識別できた継続ダメージは連続ヒットを延長しません。"),
                ["de-DE"] = HitStreak("Trefferfolgen-Anzeige", "Zeigt die lokale Trefferfolge an der letzten Trefferposition. Erkannter Schaden über Zeit verlängert die Trefferfolge nicht."),
                ["es-ES"] = HitStreak("Indicador de golpes consecutivos", "Muestra la racha local de golpes en la última posición de impacto. El daño prolongado identificado no alarga la racha."),
                ["fr-FR"] = HitStreak("Indicateur de série de coups", "Affiche la série de coups locale au dernier point d'impact. Les dégâts sur la durée identifiés ne prolongent pas la série."),
                ["it-IT"] = HitStreak("Indicatore serie di colpi", "Mostra la serie locale di colpi nell'ultima posizione colpita. I danni nel tempo identificati non prolungano la serie."),
                ["pl-PL"] = HitStreak("Wskaźnik serii trafień", "Pokazuje lokalną serię trafień przy ostatnim trafieniu. Rozpoznane obrażenia w czasie nie przedłużają serii."),
                ["pt-BR"] = HitStreak("Indicador de sequência de acertos", "Mostra a sequência local de acertos na última posição atingida. Dano contínuo identificado não prolonga a sequência."),
                ["ru-RU"] = HitStreak("Индикатор серии попаданий", "Показывает локальную серию попаданий в точке последнего удара. Распознанный периодический урон не продлевает серию."),
                ["sv-SE"] = HitStreak("Indikator för träffserie", "Visar den lokala träffserien vid den senaste träffen. Identifierad skada över tid förlänger inte träffserien."),
                ["th-TH"] = HitStreak("ตัวแสดงการโจมตีต่อเนื่อง", "แสดงจำนวนการโจมตีต่อเนื่องของผู้เล่นในตำแหน่งที่โจมตีล่าสุด ดาเมจต่อเนื่องที่ตรวจพบจะไม่ต่อเวลาการโจมตีต่อเนื่อง"),
                ["tr-TR"] = HitStreak("Seri vuruş göstergesi", "Yerel seri vuruş sayısını son vuruş konumunda gösterir. Tanımlanan süreli hasar seriyi uzatmaz.")
            };

        private static readonly Dictionary<string, Dictionary<string, string>> OutlineTexts =
            new Dictionary<string, Dictionary<string, string>>
            {
                ["en-US"] = O("Ally & enemy outlines", "Highlights allies in cyan and enemies in red. Follow Game limits outlines to multiplayer; other combat visual presets use their configured outline scope. The local player is never outlined."),
                ["zh-CN"] = O("敌我描边", "以青色标出友方、红色标出敌方。“跟随游戏”仅在多人游戏中显示；其他战斗视觉预设使用各自配置的描边范围。本地玩家始终不会被描边。"),
                ["zh-TW"] = O("敵我描邊", "以青色標示友方、紅色標示敵方。「跟隨遊戲」僅在多人遊戲中顯示；其他戰鬥視覺預設使用各自設定的描邊範圍。本機玩家始終不會被描邊。")
            };

        private static readonly Dictionary<string, Dictionary<string, string>>
            DeveloperConsoleTexts =
                new Dictionary<string, Dictionary<string, string>>
                {
                    ["en-US"] = D("Developer console",
                        "Unlocks the game's built-in developer console. Commands can modify saves, unlocks, achievements, and multiplayer state. Disabled by default.",
                        "Open developer console", "Off", "On"),
                    ["zh-CN"] = D("开发者控制台",
                        "解锁游戏内置的开发者控制台。命令可能修改存档、解锁内容、成就及多人游戏状态。默认关闭。",
                        "打开开发者控制台", "关闭", "开启"),
                    ["zh-TW"] = D("開發者控制台",
                        "解鎖遊戲內建的開發者控制台。指令可能修改存檔、解鎖內容、成就及多人遊戲狀態。預設關閉。",
                        "開啟開發者控制台", "關閉", "開啟")
                };

        private static readonly Dictionary<string, Dictionary<string, string>>
            DeveloperPlayerDamageTexts =
                new Dictionary<string, Dictionary<string, string>>
                {
                    ["en-US"] = P("Player damage multiplier",
                        "Multiplies damage created by the local player and their summoned units. Online clients cannot alter server-authoritative damage. Available only in developer builds."),
                    ["zh-CN"] = P("玩家伤害倍率",
                        "放大本地玩家及其召唤单位创建的伤害；联机客户端无法修改由服务器裁定的伤害。仅在开发构建中提供。"),
                    ["zh-TW"] = P("玩家傷害倍率",
                        "放大本機玩家及其召喚單位建立的傷害；連線用戶端無法修改由伺服器判定的傷害。僅在開發版本中提供。")
                };

        internal static void Register(HorayModLocalizationContext context)
        {
            Register((language, key, value) => context.AddText(language, key, value));
        }

        internal static bool RegisterCurrent()
        {
            LocalizationManager manager = LocalizationManager.Instance;
            if (manager == null || manager.Languages == null || manager.Languages.Count == 0)
            {
                return false;
            }

            Register(manager.AddModText);
            return true;
        }

        private static void Register(Action<string, string, string> addText)
        {
            foreach (KeyValuePair<string, Dictionary<string, string>> language in Texts)
            {
                addText(language.Key, Section, "SEPHIRIA ENHANCEMENTS · by 0xMashiro");
                addText(language.Key, RetryFloor,
                    RetryFloorTexts.TryGetValue(language.Key, out string retryFloor)
                        ? retryFloor : RetryFloorTexts["en-US"]);
                addText(language.Key, RetryBossEncounter,
                    RetryBossEncounterTexts.TryGetValue(language.Key,
                        out string retryBossEncounter)
                        ? retryBossEncounter : RetryBossEncounterTexts["en-US"]);
                foreach (KeyValuePair<string, string> text in language.Value)
                {
                    addText(language.Key, text.Key, text.Value);
                }

                Dictionary<string, string> suite = SuiteTexts.TryGetValue(language.Key,
                    out Dictionary<string, string> localizedSuite)
                    ? localizedSuite : SuiteTexts["en-US"];
                foreach (KeyValuePair<string, string> text in suite)
                {
                    addText(language.Key, text.Key, text.Value);
                }

                Dictionary<string, string> insights = InsightsTexts.TryGetValue(language.Key,
                    out Dictionary<string, string> localizedInsights)
                    ? localizedInsights : InsightsTexts["en-US"];
                foreach (KeyValuePair<string, string> text in insights)
                {
                    addText(language.Key, text.Key, text.Value);
                }

                Dictionary<string, string> developerConsole =
                    DeveloperConsoleTexts.TryGetValue(language.Key,
                        out Dictionary<string, string> localizedDeveloperConsole)
                        ? localizedDeveloperConsole
                        : DeveloperConsoleTexts["en-US"];
                foreach (KeyValuePair<string, string> text in developerConsole)
                {
                    addText(language.Key, text.Key, text.Value);
                }

                Dictionary<string, string> developerPlayerDamage =
                    DeveloperPlayerDamageTexts.TryGetValue(language.Key,
                        out Dictionary<string, string> localizedDeveloperPlayerDamage)
                        ? localizedDeveloperPlayerDamage
                        : DeveloperPlayerDamageTexts["en-US"];
                foreach (KeyValuePair<string, string> text in developerPlayerDamage)
                {
                    addText(language.Key, text.Key, text.Value);
                }

                Dictionary<string, string> defeatRetry =
                    DefeatRetryTexts.TryGetValue(language.Key,
                        out Dictionary<string, string> localizedDefeatRetry)
                        ? localizedDefeatRetry : DefeatRetryTexts["en-US"];
                foreach (KeyValuePair<string, string> text in defeatRetry)
                {
                    addText(language.Key, text.Key, text.Value);
                }
            }

            foreach (KeyValuePair<string, Dictionary<string, string>> language in AdditionalTexts)
            {
                foreach (KeyValuePair<string, string> text in language.Value)
                {
                    addText(language.Key, text.Key, text.Value);
                }

                for (int index = 0; index < ScaleKeys.Length; index++)
                {
                    int[] percentages = { 65, 80, 100, 115, 130 };
                    addText(language.Key, ScaleKeys[index], percentages[index] + "%");
                }
            }

            foreach (KeyValuePair<string, Dictionary<string, string>> language in HelpTexts)
            {
                foreach (KeyValuePair<string, string> text in language.Value)
                {
                    addText(language.Key, text.Key, text.Value);
                }
            }

            foreach (KeyValuePair<string, Dictionary<string, string>> language in
                HitStreakFeedbackTexts)
            {
                foreach (KeyValuePair<string, string> text in language.Value)
                {
                    addText(language.Key, text.Key, text.Value);
                }
            }

            foreach (string language in Texts.Keys)
            {
                Dictionary<string, string> outline = OutlineTexts.TryGetValue(
                    language, out Dictionary<string, string> localizedOutline)
                    ? localizedOutline : OutlineTexts["en-US"];
                foreach (KeyValuePair<string, string> text in outline)
                {
                    addText(language, text.Key, text.Value);
                }
            }

            ControlLocalization.Register(addText);
            MapEnhancements.MapEnhancementsLocalization.Register(addText, Texts.Keys);
            OptionsCategoryLocalization.Register(addText, Texts.Keys);
            CombatVisualLocalization.Register(addText, Texts.Keys);
            Inventory.InventoryOptimizationLocalization.Register(addText);
            MultiplayerRulesLocalization.Register(addText, Texts.Keys);
            MultiplayerAccessLocalization.Register(addText, Texts.Keys);

        }

        internal static string Get(string key)
        {
            LocalizationManager manager = LocalizationManager.Instance;
            if (manager == null || string.IsNullOrEmpty(manager.CurrentLanguage))
            {
                if (Texts["en-US"].TryGetValue(key, out string primary))
                {
                    return primary;
                }

                if (key == RetryFloor)
                {
                    return RetryFloorTexts["en-US"];
                }

                if (key == RetryBossEncounter)
                {
                    return RetryBossEncounterTexts["en-US"];
                }

                return AdditionalTexts["en-US"].TryGetValue(key, out string additional)
                    ? additional
                    : HelpTexts["en-US"].TryGetValue(key, out string help) ? help
                    : HitStreakFeedbackTexts["en-US"].TryGetValue(key, out string feedback)
                        ? feedback
                    : OutlineTexts["en-US"].TryGetValue(key, out string outline)
                        ? outline
                    : SuiteTexts["en-US"].TryGetValue(key, out string suite)
                        ? suite
                    : InsightsTexts["en-US"].TryGetValue(key, out string insights)
                        ? insights
                    : DeveloperConsoleTexts["en-US"].TryGetValue(key,
                        out string developerConsole) ? developerConsole
                    : DeveloperPlayerDamageTexts["en-US"].TryGetValue(key,
                        out string developerPlayerDamage) ? developerPlayerDamage
                    : DefeatRetryTexts["en-US"].TryGetValue(key,
                        out string defeatRetry) ? defeatRetry : key;
            }

            return manager.GetText(manager.CurrentLanguage, key);
        }

        private static Dictionary<string, string> A(string uiScale)
        {
            return new Dictionary<string, string>
            {
                [SettingDamageStatisticsScale] = uiScale
            };
        }

        private static Dictionary<string, string> H(string uiScale)
        {
            return new Dictionary<string, string>
            {
                [HelpDamageStatisticsScale] = uiScale
            };
        }

        private static Dictionary<string, string> HitStreak(string label, string help)
        {
            return new Dictionary<string, string>
            {
                [SettingHitStreakFeedback] = label,
                [HelpHitStreakFeedback] = help
            };
        }

        private static Dictionary<string, string> L(string off, string on)
        {
            return new Dictionary<string, string>
            {
                [Off] = off,
                [On] = on
            };
        }

        private static Dictionary<string, string> Q(string master, string masterHelp,
            string nativeCompanion, string nativeCompanionHelp, string soloOnly,
            string smartFill, string alwaysHost, string off, string on)
        {
            return new Dictionary<string, string>
            {
                [SettingMasterEnabled] = master,
                [HelpMasterEnabled] = masterHelp,
                [SettingNativeCompanion] = nativeCompanion,
                [HelpNativeCompanion] = nativeCompanionHelp,
                [SuiteOff] = off,
                [SuiteOn] = on,
                [NativeCompanionModeKeys[1]] = soloOnly,
                [NativeCompanionModeKeys[2]] = smartFill,
                [NativeCompanionModeKeys[3]] = alwaysHost
            };
        }

        private static Dictionary<string, string> O(string label, string help)
        {
            return new Dictionary<string, string>
            {
                [SettingCombatRelationOutlines] = label,
                [HelpCombatRelationOutlines] = help
            };
        }

        private static Dictionary<string, string> D(string label, string help,
            string shortcut, string off, string on)
        {
            return new Dictionary<string, string>
            {
                [SettingDeveloperConsole] = label,
                [HelpDeveloperConsole] = help,
                [DeveloperConsoleShortcut] = shortcut,
                [DeveloperConsoleOff] = off,
                [DeveloperConsoleOn] = on
            };
        }

        private static Dictionary<string, string> P(string label, string help)
        {
            return new Dictionary<string, string>
            {
                [SettingDeveloperPlayerDamage] = label,
                [HelpDeveloperPlayerDamage] = help,
                [DeveloperPlayerDamageMultiplierKeys[0]] = "1×",
                [DeveloperPlayerDamageMultiplierKeys[1]] = "2×",
                [DeveloperPlayerDamageMultiplierKeys[2]] = "5×",
                [DeveloperPlayerDamageMultiplierKeys[3]] = "10×",
                [DeveloperPlayerDamageMultiplierKeys[4]] = "100×"
            };
        }

        private static Dictionary<string, string> R(string label, string help,
            string off, string on)
        {
            return new Dictionary<string, string>
            {
                [SettingDefeatRetry] = label,
                [HelpDefeatRetry] = help,
                [DefeatRetryOff] = off,
                [DefeatRetryOn] = on
            };
        }

        private static Dictionary<string, string> I(string setting, string smart,
            string bossOnly, string allCombat, string dps, string defeated,
            string finalBlows, string normal, string miniboss, string boss,
            string combatSummary, string damageShare, string damageAverageDps,
            string reportDamage, string reportShare, string reportAverageDps,
            string reportDamageMix, string damagePhysical, string damageFire,
            string damageIce, string damageLightning, string damageChaos,
            string damageNormal, string damageMixed, string damageOther,
            string policyHelp, string disabled, string displayHidden,
            string displayRestored, string reportOpened, string reportClosed,
            string reportUnavailable, string reportLoading, string reportScreenTransition,
            string reportCutscene, string reportMenu, string reportHudUnavailable)
        {
            return new Dictionary<string, string>
            {
                [SettingDisplayPolicy] = setting,
                [DisplayPolicyKeys[0]] = smart,
                [DisplayPolicyKeys[1]] = bossOnly,
                [DisplayPolicyKeys[2]] = allCombat,
                [InsightsDisabled] = disabled,
                [DamageStatisticsDisplayHidden] = displayHidden,
                [DamageStatisticsDisplayRestored] = displayRestored,
                [EncounterReportOpened] = reportOpened,
                [EncounterReportClosed] = reportClosed,
                [EncounterReportUnavailable] = reportUnavailable,
                [EncounterReportLoading] = reportLoading,
                [EncounterReportScreenTransition] = reportScreenTransition,
                [EncounterReportCutscene] = reportCutscene,
                [EncounterReportMenu] = reportMenu,
                [EncounterReportHudUnavailable] = reportHudUnavailable,
                [HelpDisplayPolicy] = policyHelp,
                [Dps] = dps,
                [Defeated] = defeated,
                [FinalBlows] = finalBlows,
                [NormalEnemy] = normal,
                [MinibossEnemy] = miniboss,
                [BossEnemy] = boss,
                [CombatSummary] = combatSummary,
                [DamageShare] = damageShare,
                [DamageAverageDps] = damageAverageDps,
                [ReportDamage] = reportDamage,
                [ReportShare] = reportShare,
                [ReportAverageDps] = reportAverageDps,
                [ReportDamageMix] = reportDamageMix,
                [DamagePhysical] = damagePhysical,
                [DamageFire] = damageFire,
                [DamageIce] = damageIce,
                [DamageLightning] = damageLightning,
                [DamageChaos] = damageChaos,
                [DamageNormal] = damageNormal,
                [DamageMixed] = damageMixed,
                [DamageOther] = damageOther
            };
        }
    }
}
