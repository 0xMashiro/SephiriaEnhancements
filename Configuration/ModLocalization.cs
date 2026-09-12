using System;
using System.Collections.Generic;
using SephiriaEnhancements.MultiplayerRules.Presentation;
using SephiriaEnhancements.MultiplayerAccess.Presentation;
using SephiriaEnhancements.CombatVisuals;

namespace SephiriaEnhancements.Configuration
{
    internal static partial class ModLocalization
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
        internal const string StatisticsOpened =
            "SephiriaEnhancements.CombatInsights.StatisticsOpened";
        internal const string StatisticsClosed =
            "SephiriaEnhancements.CombatInsights.StatisticsClosed";
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
        internal const string ViewStatistics = "SephiriaEnhancements.Hud.ViewStatistics";
        internal const string RecentEncounterStatistics = "SephiriaEnhancements.Hud.RecentEncounterStatistics";
        internal const string CurrentEncounterStatistics = "SephiriaEnhancements.Hud.CurrentEncounterStatistics";
        internal const string CurrentFloorStatistics = "SephiriaEnhancements.Hud.CurrentFloorStatistics";
        internal const string CloseStatistics = "SephiriaEnhancements.Hud.CloseStatistics";
        internal const string FloorBattleDuration = "SephiriaEnhancements.Hud.FloorBattleDuration";
        internal const string FloorBattleTime = "SephiriaEnhancements.Hud.FloorBattleTime";
        internal const string EncounterBattleTime = "SephiriaEnhancements.Hud.EncounterBattleTime";
        internal const string FloorStatisticsEmpty = "SephiriaEnhancements.Hud.FloorStatisticsEmpty";
        internal const string ReportDismissHint =
            "SephiriaEnhancements.Hud.ReportDismissHint";
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
        internal const string RetryBossUnavailable =
            "SephiriaEnhancements.RetryBossUnavailable";

        internal static readonly string[] ScaleKeys =
        {
            "SephiriaEnhancements.Scale.80", "SephiriaEnhancements.Scale.90",
            "SephiriaEnhancements.Scale.100", "SephiriaEnhancements.Scale.110",
            "SephiriaEnhancements.Scale.120"
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
                    "Choose before exploration. Locked during exploration or pending reconnect; display options remain adjustable. Saved explorations retain their rules. Also locked while connected to another host.",
                    "Combat companion",
                    "Solo only adds the companion offline. Smart fill also helps an online host playing alone; when another player joins, the companion leaves after combat. Always for host keeps the companion in multiplayer parties. It uses no player slot, and other players do not need this Mod. Turning this off removes the companion added by this Mod without waiting for combat to end.",
                    "Solo only", "Smart fill", "Always for host", "Off", "On"),
                ["zh-CN"] = Q("Sephiria 增强",
                    "请在探索前设置。探索中或等待重连时锁定；显示选项仍可单独调整。继续已有探索会保留原规则。 连接其他房主期间也会锁定。", "战斗伙伴",
                    "“仅单机”只在离线游戏中启用；“智能补位”还会在多人游戏中房主独自游戏时陪伴，并在真人加入后脱战离队；“房主始终启用”会在多人队伍中保留伙伴。不占玩家位置，其他玩家无需安装本 MOD。 关闭后移除本 Mod 添加的伙伴，不等待战斗结束。",
                    "仅单机", "智能补位", "房主始终启用", "关闭", "开启"),
                ["zh-TW"] = Q("Sephiria 增強",
                    "請在探索前設定。探索中或等待重連時鎖定；顯示選項仍可個別調整。繼續已有探索會保留原規則。 連接其他房主期間也會鎖定。", "戰鬥夥伴",
                    "「僅單機」只在離線遊戲中啟用；「智慧補位」也會在多人遊戲中房主獨自遊戲時陪伴，並在真人加入後脫戰離隊；「房主始終啟用」會在多人隊伍中保留夥伴。不佔玩家位置，其他玩家無需安裝本 MOD。 關閉後移除本 Mod 加入的夥伴，不等待戰鬥結束。",
                    "僅單機", "智慧補位", "房主始終啟用", "關閉", "開啟"),
                ["ko-KR"] = Q("Sephiria Enhancements",
                    "탐험 전에 설정하세요. 탐험 또는 재접속 대기 중에는 잠깁니다. 표시 옵션은 따로 변경할 수 있으며 기존 탐험의 규칙은 유지됩니다. 다른 호스트에 연결되어 있는 동안에도 잠깁니다.",
                    "전투 동료",
                    "오프라인 전용은 오프라인에서만 동료를 추가합니다. 빈자리 보충은 온라인 호스트가 혼자일 때도 동료를 추가하며, 다른 플레이어가 참가하면 전투 종료 후 떠납니다. 호스트일 때 항상은 멀티플레이 파티에서도 동료를 유지합니다. 플레이어 자리를 차지하지 않으며 다른 플레이어에게는 이 Mod가 필요하지 않습니다. 끄면 전투 종료를 기다리지 않고 이 Mod가 추가한 동료를 제거합니다.",
                    "오프라인 전용",
                    "빈자리 보충",
                    "호스트일 때 항상",
                    "끄기",
                    "켜기"),
                ["ja-JP"] = Q("Sephiria Enhancements",
                    "探索前に設定してください。探索中や再接続待ちの間は変更できません。表示設定は個別に変更できます。探索再開時は保存済みのルールを使います。 他のホストに接続している間も変更できません。",
                    "戦闘の仲間",
                    "「オフラインのみ」はオフラインで仲間を追加します。「空きを補充」はオンラインのホストが一人の場合にも仲間を追加し、他のプレイヤーが参加すると戦闘終了後に離脱します。「ホスト時は常に」はマルチプレイでも仲間を維持します。プレイヤー枠を使わず、他のプレイヤーにこの Mod は不要です。 オフにすると、戦闘終了を待たずにこのModが追加した仲間を退場させます。",
                    "オフラインのみ",
                    "空きを補充",
                    "ホスト時は常に",
                    "オフ",
                    "オン"),
                ["de-DE"] = Q("Sephiria Enhancements",
                    "Vor der Erkundung einstellen. Während Erkundung oder ausstehendem Wiederbeitritt gesperrt; Anzeigen bleiben einzeln einstellbar. Gespeicherte Erkundungen behalten ihre Regeln. Auch während einer Verbindung zu einem anderen Host gesperrt.",
                    "Kampfbegleiter",
                    "Nur offline fügt den Begleiter im Offline-Spiel hinzu. Freien Platz ergänzen hilft auch einem allein spielenden Online-Host; tritt ein weiterer Spieler bei, verlässt der Begleiter die Gruppe nach dem Kampf. Immer als Host behält ihn auch in Mehrspielergruppen. Er belegt keinen Spielerplatz. Andere Spieler benötigen diesen Mod nicht. Ausschalten entfernt den von dieser Mod hinzugefügten Begleiter, ohne das Kampfende abzuwarten.",
                    "Nur offline",
                    "Freien Platz ergänzen",
                    "Immer als Host",
                    "Aus",
                    "Ein"),
                ["es-ES"] = Q("Sephiria Enhancements",
                    "Configura antes de explorar. Se bloquea durante la exploración o una reconexión pendiente; las opciones visuales se pueden ajustar aparte. Las exploraciones guardadas conservan sus reglas. También se bloquea mientras estás conectado a otro anfitrión.",
                    "Compañero de combate",
                    "Solo sin conexión añade al compañero en partidas sin conexión. Cubrir vacante también ayuda al anfitrión cuando está solo en línea; si entra otro jugador, el compañero se retira al salir del combate. Siempre como anfitrión lo mantiene en grupos multijugador. No ocupa una plaza y los demás jugadores no necesitan este Mod. Desactivarlo retira al compañero añadido por este Mod sin esperar al final del combate.",
                    "Solo sin conexión",
                    "Cubrir vacante",
                    "Siempre como anfitrión",
                    "Desactivado",
                    "Activado"),
                ["fr-FR"] = Q("Sephiria Enhancements",
                    "À régler avant l’exploration. Verrouillé pendant l’exploration ou une reconnexion en attente ; l’affichage reste réglable séparément. Les explorations sauvegardées gardent leurs règles. Également verrouillé tant que vous êtes connecté à un autre hôte.",
                    "Compagnon de combat",
                    "Hors ligne uniquement ajoute le compagnon hors ligne. Compléter le groupe aide aussi l’hôte seul en ligne ; si un autre joueur rejoint, le compagnon part après le combat. Toujours pour l’hôte le conserve en multijoueur. Il n’occupe aucune place de joueur et les autres n’ont pas besoin de ce Mod. Désactiver retire le compagnon ajouté par ce Mod sans attendre la fin du combat.",
                    "Hors ligne uniquement",
                    "Compléter le groupe",
                    "Toujours pour l’hôte",
                    "Désactivé",
                    "Activé"),
                ["it-IT"] = Q("Sephiria Enhancements",
                    "Imposta prima di esplorare. Bloccato durante esplorazione o riconnessione in attesa; le opzioni visive restano regolabili. Le esplorazioni salvate mantengono le proprie regole. Bloccato anche mentre sei connesso a un altro host.",
                    "Compagno di combattimento",
                    "Solo offline aggiunge il compagno nelle partite offline. Completa il gruppo aiuta anche l’host solo online; se entra un altro giocatore, il compagno lascia il gruppo a combattimento concluso. Sempre per l’host lo mantiene in multigiocatore. Non occupa un posto giocatore e gli altri non devono installare questo Mod. Disattivare rimuove il compagno aggiunto da questo Mod senza aspettare la fine del combattimento.",
                    "Solo offline",
                    "Completa il gruppo",
                    "Sempre per l’host",
                    "Disattivato",
                    "Attivato"),
                ["pl-PL"] = Q("Sephiria Enhancements",
                    "Ustaw przed eksploracją. Zablokowane podczas eksploracji lub oczekiwania na ponowne połączenie; wyświetlanie można zmieniać osobno. Zapisane eksploracje zachowują reguły. Blokada działa także podczas połączenia z innym gospodarzem.",
                    "Towarzysz walki",
                    "Tylko offline dodaje towarzysza w grze offline. Uzupełnianie drużyny pomaga też gospodarzowi grającemu samotnie online; gdy dołączy inny gracz, towarzysz odchodzi po walce. Zawsze u gospodarza zachowuje go także w drużynie wieloosobowej. Nie zajmuje miejsca gracza, a inni nie potrzebują tego moda. Wyłączenie usuwa towarzysza dodanego przez ten mod bez czekania na koniec walki.",
                    "Tylko offline",
                    "Uzupełnianie drużyny",
                    "Zawsze u gospodarza",
                    "Wył.",
                    "Wł."),
                ["pt-BR"] = Q("Sephiria Enhancements",
                    "Configure antes de explorar. Bloqueado durante a exploração ou reconexão pendente; as opções visuais continuam ajustáveis. Explorações salvas mantêm suas regras. Também fica bloqueado enquanto você está conectado a outro anfitrião.",
                    "Companheiro de combate",
                    "Somente offline adiciona o companheiro em partidas offline. Preencher vaga também ajuda o anfitrião sozinho online; quando outro jogador entra, o companheiro sai após o combate. Sempre para o anfitrião o mantém em grupos multijogador. Não ocupa vaga de jogador e os demais não precisam deste Mod. Desativar remove o companheiro adicionado por este Mod sem esperar o fim do combate.",
                    "Somente offline",
                    "Preencher vaga",
                    "Sempre para o anfitrião",
                    "Desativado",
                    "Ativado"),
                ["ru-RU"] = Q("Sephiria Enhancements",
                    "Настройте до похода. Заблокировано во время похода или ожидания переподключения; отображение можно менять отдельно. Сохранённые походы сохраняют правила. Также заблокировано при подключении к другому хосту.",
                    "Боевой спутник",
                    "Только офлайн добавляет спутника в офлайн-игре. Заполнять свободное место помогает и хосту, играющему одному по сети; если входит другой игрок, спутник уходит после боя. Всегда у хоста сохраняет спутника и в сетевой группе. Он не занимает место игрока. Другим игрокам этот мод не нужен. Отключение убирает спутника, добавленного этим модом, не дожидаясь конца боя.",
                    "Только офлайн",
                    "Заполнять свободное место",
                    "Всегда у хоста",
                    "Выкл.",
                    "Вкл."),
                ["sv-SE"] = Q("Sephiria Enhancements",
                    "Ställ in före utforskning. Låst under utforskning eller väntande återanslutning; visningen kan justeras separat. Sparade utforskningar behåller sina regler. Även låst medan du är ansluten till en annan värd.",
                    "Stridsföljeslagare",
                    "Endast offline lägger till följeslagaren i offlinespel. Fyll ledig plats hjälper även en ensam onlinevärd; om en annan spelare ansluter lämnar följeslagaren efter striden. Alltid för värden behåller följeslagaren i flerspelargrupper. Ingen spelarplats tas upp och andra spelare behöver inte denna mod. Avstängning tar bort följeslagaren som modden lagt till utan att vänta på stridens slut.",
                    "Endast offline",
                    "Fyll ledig plats",
                    "Alltid för värden",
                    "Av",
                    "På"),
                ["th-TH"] = Q("Sephiria Enhancements",
                    "ตั้งค่าก่อนสำรวจ ล็อกระหว่างสำรวจหรือรอเชื่อมต่อใหม่ แต่ยังปรับการแสดงผลแยกได้ การสำรวจที่บันทึกไว้จะใช้กฎเดิม ล็อกขณะเชื่อมต่อกับโฮสต์อื่นด้วย",
                    "เพื่อนร่วมรบ",
                    "ออฟไลน์เท่านั้นจะเพิ่มเพื่อนร่วมรบเมื่อเล่นออฟไลน์ เติมที่ว่างจะช่วยโฮสต์ที่อยู่คนเดียวออนไลน์ด้วย และเพื่อนร่วมรบจะออกหลังจบการต่อสู้เมื่อมีผู้เล่นอื่นเข้ามา มีเสมอเมื่อเป็นโฮสต์จะคงเพื่อนร่วมรบไว้แม้อยู่ในทีมหลายคน ไม่ใช้ช่องผู้เล่น และผู้เล่นอื่นไม่ต้องติดตั้ง Mod นี้ การปิดจะนำสหายที่ Mod นี้เพิ่มออกโดยไม่รอให้การต่อสู้จบ",
                    "ออฟไลน์เท่านั้น",
                    "เติมที่ว่าง",
                    "มีเสมอเมื่อเป็นโฮสต์",
                    "ปิด",
                    "เปิด"),
                ["tr-TR"] = Q("Sephiria Enhancements",
                    "Keşiften önce ayarlayın. Keşif veya yeniden bağlantı beklerken kilitlenir; görünüm ayrı ayarlanabilir. Kayıtlı keşifler kendi kurallarını korur. Başka bir ev sahibine bağlıyken de kilitlidir.",
                    "Savaş yoldaşı",
                    "Yalnızca çevrimdışı, çevrimdışı oyuna yoldaş ekler. Boş yeri doldur, çevrimiçi tek başına olan sunucu sahibine de yardım eder; başka bir oyuncu katılınca yoldaş savaş bittikten sonra ayrılır. Sunucu sahibi için daima, çok oyunculu grupta da yoldaşı tutar. Oyuncu yuvası kullanmaz ve diğer oyuncuların bu Modu kurması gerekmez. Kapatmak, bu Modun eklediği yoldaşı savaşın bitmesini beklemeden kaldırır.",
                    "Yalnızca çevrimdışı",
                    "Boş yeri doldur",
                    "Sunucu sahibi için daima",
                    "Kapalı",
                    "Açık")
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
                    "Smart shows recent 5-second DPS in ordinary fights and cumulative damage in BOSS fights; reports show battle and floor totals.\nOutside combat, tap the statistics shortcut or use View statistics in the pause menu. Hold the shortcut for half a second to hide or restore the display without stopping recording.\nTotals are temporary and local: co-op results may differ. Changing floors, reconnecting, reloading or disabling statistics can clear them.",
                    "Disabled", "Damage statistics display hidden",
                    "Damage statistics display restored",
                    "Statistics opened", "Statistics closed",
                    "No combat report available on this floor",
                    "Statistics display unavailable while loading",
                    "Statistics display unavailable during a screen transition",
                    "Statistics display unavailable during a cutscene",
                    "Close the menu to view statistics",
                    "Combat report display is not ready yet", "{0}  ·  Close report",
                    "View statistics", "Recent battle", "Current floor", "Close statistics",
                    "Battle average DPS · accumulated battle time",
                    "Average DPS · encounter duration", "No combat recorded on this floor yet",
                    "This battle",
                    "Total battle time {0}"),
                ["zh-CN"] = I("伤害统计", "智能", "仅 BOSS", "每场战斗",
                    "DPS", "击败", "我的最后一击", "普通", "小头目", "BOSS",
                    "战斗统计", "DMG · 占比", "DMG · 平均 DPS",
                    "DMG", "占比", "平均 DPS",
                    "伤害构成", "物理", "火焰", "冰霜", "闪电", "混沌",
                    "普通", "混合", "其他",
                    "智能模式在普通战斗中显示近 5 秒 DPS，BOSS 战显示累计伤害；战报可查看单场与本层累计。\n脱战后短按统计快捷键，或从暂停菜单选择“查看统计”。长按快捷键半秒可隐藏或恢复显示，隐藏不停止记录。\n统计是本机临时记录，联机结果可能不同；换层、重连、重新加载或关闭统计可能清空数据。",
                    "关闭", "伤害统计显示已隐藏", "伤害统计显示已恢复",
                    "已打开统计", "统计已关闭", "本层暂无可查看的战报",
                    "正在加载，暂时无法显示统计", "画面切换中，暂时无法显示统计",
                    "剧情播放中，暂时无法显示统计", "请关闭菜单后查看统计",
                    "战报界面尚未就绪", "{0}  ·  收起战报",
                    "查看统计", "最近战斗", "本层累计", "关闭统计",
                    "战斗平均 DPS · 按累计战斗时长计算", "平均 DPS · 按单场战斗时长计算",
                    "本层尚未记录战斗数据",
                    "本场战斗",
                    "累计战斗时长 {0}"),
                ["zh-TW"] = I("傷害統計", "智慧", "僅 BOSS", "每場戰鬥",
                    "DPS", "擊敗", "我的最後一擊", "普通", "小頭目", "BOSS",
                    "戰鬥統計", "DMG · 佔比", "DMG · 平均 DPS",
                    "DMG", "佔比", "平均 DPS",
                    "傷害構成", "物理", "火焰", "冰霜", "閃電", "混沌",
                    "普通", "混合", "其他",
                    "智慧模式在普通戰鬥中顯示近 5 秒 DPS，BOSS 戰顯示累計傷害；戰報可查看單場與本層累計。\n脫戰後短按統計快捷鍵，或從暫停選單選擇「查看統計」。長按快捷鍵半秒可隱藏或恢復顯示，隱藏不會停止記錄。\n統計是本機暫存記錄，連線結果可能不同；換層、重新連線、重新載入或關閉統計可能清空資料。",
                    "關閉", "傷害統計顯示已隱藏", "傷害統計顯示已恢復",
                    "已開啟統計", "統計已關閉", "本層暫無可查看的戰報",
                    "正在載入，暫時無法顯示統計", "畫面切換中，暫時無法顯示統計",
                    "劇情播放中，暫時無法顯示統計", "請關閉選單後查看統計",
                    "戰報介面尚未就緒", "{0}  ·  收起戰報",
                    "查看統計", "最近戰鬥", "本層累計", "關閉統計",
                    "戰鬥平均 DPS · 按累計戰鬥時間計算", "平均 DPS · 按單場戰鬥時間計算",
                    "本層尚未記錄戰鬥資料",
                    "本場戰鬥",
                    "累計戰鬥時間 {0}"),
                ["ja-JP"] = I("ダメージ統計",
                    "自動",
                    "ボスのみ",
                    "すべての戦闘",
                    "DPS",
                    "撃破",
                    "自分の最後の一撃",
                    "通常",
                    "中ボス",
                    "BOSS",
                    "戦闘の集計",
                    "DMG · 割合",
                    "DMG · 平均 DPS",
                    "DMG",
                    "割合",
                    "平均 DPS",
                    "ダメージ内訳",
                    "物理",
                    "火",
                    "氷",
                    "雷",
                    "混沌",
                    "通常",
                    "混合",
                    "その他",
                    "自動は通常戦で直近5秒のDPS、BOSS戦で累計ダメージを表示します。レポートでは1戦と現在の階層の合計を確認できます。\n戦闘外で統計ショートカットを短く押すか、ポーズメニューの統計を見るを選択します。0.5秒長押しすると記録を続けたまま表示を切り替えます。\n統計は端末内の一時記録のため、協力プレイでは数値が異なる場合があります。階層移動、再接続、再読み込み、統計の無効化で消えることがあります。",
                    "無効",
                    "ダメージ統計を非表示にしました",
                    "ダメージ統計の表示を再開しました",
                    "統計を開きました",
                    "統計を閉じました",
                    "このフロアの戦闘レポートはまだありません",
                    "ロード中は統計を表示できません",
                    "画面切替中は統計を表示できません",
                    "イベントシーン中は統計を表示できません",
                    "メニューを閉じてから統計を表示してください",
                    "戦闘レポートの準備ができていません",
                    "{0} · レポートを閉じる",
                    "統計を見る",
                    "直近の戦闘",
                    "現在の階層",
                    "統計を閉じる",
                    "戦闘平均 DPS · 累計戦闘時間で計算",
                    "平均 DPS · 1 戦の時間で計算",
                    "この階層の戦闘データはまだありません",
                    "今回の戦闘",
                    "累計戦闘時間 {0}"),
                ["ko-KR"] = I("피해 통계",
                    "자동",
                    "보스만",
                    "모든 전투",
                    "DPS",
                    "처치",
                    "내 마지막 일격",
                    "일반",
                    "중간 보스",
                    "BOSS",
                    "전투 요약",
                    "DMG · 비중",
                    "DMG · 평균 DPS",
                    "DMG",
                    "비중",
                    "평균 DPS",
                    "피해 구성",
                    "물리",
                    "화염",
                    "얼음",
                    "번개",
                    "혼돈",
                    "일반",
                    "혼합",
                    "기타",
                    "자동은 일반 전투에서 최근 5초 DPS, BOSS전에서 누적 피해를 표시합니다. 보고서에서 전투와 현재 층의 합계를 볼 수 있습니다.\n전투 밖에서 통계 단축키를 짧게 누르거나 일시 정지 메뉴의 통계 보기를 사용하세요. 0.5초간 누르면 기록을 유지한 채 표시를 숨기거나 복원합니다.\n통계는 기기에 임시 저장되어 협동 플레이 결과가 다를 수 있습니다. 층 변경, 재접속, 다시 불러오기 또는 통계 끄기로 기록이 지워질 수 있습니다.",
                    "비활성화",
                    "피해 통계를 숨겼습니다",
                    "피해 통계 표시를 복원했습니다",
                    "통계를 열었습니다",
                    "통계를 닫았습니다",
                    "이 층에는 아직 전투 보고서가 없습니다",
                    "로딩 중에는 통계를 표시할 수 없습니다",
                    "화면 전환 중에는 통계를 표시할 수 없습니다",
                    "연출 중에는 통계를 표시할 수 없습니다",
                    "메뉴를 닫은 뒤 통계를 확인하세요",
                    "전투 보고서가 아직 준비되지 않았습니다",
                    "{0} · 보고서 닫기",
                    "통계 보기",
                    "최근 전투",
                    "현재 층",
                    "통계 닫기",
                    "전투 평균 DPS · 누적 전투 시간 기준",
                    "평균 DPS · 해당 전투 시간 기준",
                    "이 층에서 기록된 전투가 없습니다",
                    "이번 전투",
                    "누적 전투 시간 {0}"),
                ["de-DE"] = I("Schadensstatistik",
                    "Automatisch",
                    "Nur Bosse",
                    "Jeder Kampf",
                    "DPS",
                    "besiegt",
                    "meine letzten Treffer",
                    "normal",
                    "Miniboss",
                    "BOSS",
                    "Kampfübersicht",
                    "DMG · ANTEIL",
                    "DMG · Ø DPS",
                    "DMG",
                    "ANTEIL",
                    "Ø DPS",
                    "SCHADENSARTEN",
                    "Physisch",
                    "Feuer",
                    "Eis",
                    "Blitz",
                    "Chaos",
                    "normal",
                    "gemischt",
                    "sonstige",
                    "Automatisch zeigt in normalen Kämpfen die DPS der letzten 5 Sekunden, bei BOSS-Kämpfen den Gesamtschaden. Berichte zeigen Kampf- und Ebenensummen.\nAußerhalb des Kampfes die Statistiktaste kurz drücken oder die Statistik im Pausenmenü öffnen. Eine halbe Sekunde halten blendet die Anzeige aus oder ein; die Aufzeichnung läuft weiter.\nSummen sind lokal und vorübergehend; Koop-Werte können abweichen. Ebenenwechsel, Wiederverbindung, Neuladen oder Abschalten der Statistik können sie löschen.",
                    "Deaktiviert",
                    "Schadensstatistik ausgeblendet",
                    "Schadensstatistik wieder eingeblendet",
                    "Statistik geöffnet",
                    "Statistik geschlossen",
                    "Auf dieser Ebene ist noch kein Kampfbericht verfügbar",
                    "Statistik beim Laden nicht verfügbar",
                    "Statistik während des Bildschirmwechsels nicht verfügbar",
                    "Statistik während Zwischensequenzen nicht verfügbar",
                    "Schließe das Menü, um die Statistik zu sehen",
                    "Der Kampfbericht ist noch nicht bereit",
                    "{0} · Bericht schließen",
                    "Statistik ansehen",
                    "Letzter Kampf",
                    "Aktuelle Etage",
                    "Statistik schließen",
                    "Kampf-DPS · gesamte Kampfzeit",
                    "Ø DPS · Dauer dieses Kampfes",
                    "Noch keine Kampfdaten auf dieser Etage",
                    "Dieser Kampf",
                    "Gesamte Kampfzeit {0}"),
                ["es-ES"] = I("Estadísticas de daño",
                    "Automático",
                    "Solo jefes",
                    "Cada combate",
                    "DPS",
                    "derrotados",
                    "mis golpes finales",
                    "normal",
                    "minijefe",
                    "BOSS",
                    "Resumen de combate",
                    "DMG · APORTE",
                    "DMG · DPS MEDIO",
                    "DMG",
                    "APORTE",
                    "DPS MEDIO",
                    "TIPOS DE DAÑO",
                    "Físico",
                    "Fuego",
                    "Hielo",
                    "Rayo",
                    "Caos",
                    "normal",
                    "mixto",
                    "otro",
                    "Automático muestra los DPS de los últimos 5 segundos en combates normales y el daño acumulado contra BOSS. Los informes muestran totales del combate y la planta.\nFuera del combate, pulsa el atajo de estadísticas o ábrelas desde el menú de pausa. Mantén el atajo medio segundo para ocultar o mostrar el panel sin dejar de registrar.\nLos totales son temporales y locales; pueden diferir en cooperativo. Cambiar de planta, reconectar, recargar o desactivar las estadísticas puede borrarlos.",
                    "Desactivado",
                    "Estadísticas de daño ocultas",
                    "Estadísticas de daño visibles",
                    "Estadísticas abiertas",
                    "Estadísticas cerradas",
                    "Aún no hay un informe de combate de esta planta",
                    "Estadísticas no disponibles durante la carga",
                    "Estadísticas no disponibles durante una transición de pantalla",
                    "Estadísticas no disponibles durante una cinemática",
                    "Cierra el menú para ver las estadísticas",
                    "El informe de combate aún no está listo",
                    "{0} · Cerrar informe",
                    "Ver estadísticas",
                    "Último combate",
                    "Planta actual",
                    "Cerrar estadísticas",
                    "DPS medio · tiempo de combate acumulado",
                    "DPS medio · duración del combate",
                    "Aún no hay combates registrados en esta planta",
                    "Este combate",
                    "Tiempo total de combate {0}"),
                ["fr-FR"] = I("Statistiques de dégâts",
                    "Automatique",
                    "Boss uniquement",
                    "Chaque combat",
                    "DPS",
                    "vaincus",
                    "mes coups finaux",
                    "normal",
                    "mini-boss",
                    "BOSS",
                    "Bilan du combat",
                    "DMG · PART",
                    "DMG · DPS MOYEN",
                    "DMG",
                    "PART",
                    "DPS MOYEN",
                    "TYPES DE DÉGÂTS",
                    "Physique",
                    "Feu",
                    "Glace",
                    "Foudre",
                    "Chaos",
                    "normal",
                    "mixte",
                    "autres",
                    "Automatique affiche les DPS des 5 dernières secondes en combat ordinaire et les dégâts cumulés contre les BOSS. Les rapports présentent les totaux du combat et de l’étage.\nHors combat, appuyez brièvement sur le raccourci des statistiques ou ouvrez-les dans le menu pause. Maintenez-le une demi-seconde pour masquer ou rétablir l’affichage sans arrêter l’enregistrement.\nLes totaux sont temporaires et locaux : ils peuvent différer en coopération. Changer d’étage, se reconnecter, recharger ou désactiver les statistiques peut les effacer.",
                    "Désactivé",
                    "Statistiques de dégâts masquées",
                    "Statistiques de dégâts rétablies",
                    "Statistiques ouvertes",
                    "Statistiques fermées",
                    "Aucun rapport de combat disponible à cet étage",
                    "Statistiques indisponibles pendant le chargement",
                    "Statistiques indisponibles pendant une transition d’écran",
                    "Statistiques indisponibles pendant une cinématique",
                    "Fermez le menu pour voir les statistiques",
                    "Le rapport de combat n’est pas encore prêt",
                    "{0} · Fermer le rapport",
                    "Voir les statistiques",
                    "Dernier combat",
                    "Étage actuel",
                    "Fermer les statistiques",
                    "DPS moyen · temps de combat cumulé",
                    "DPS moyen · durée du combat",
                    "Aucun combat enregistré à cet étage",
                    "Ce combat",
                    "Temps de combat cumulé {0}"),
                ["it-IT"] = I("Statistiche danni",
                    "Automatico",
                    "Solo boss",
                    "Ogni scontro",
                    "DPS",
                    "sconfitti",
                    "i miei colpi finali",
                    "normale",
                    "miniboss",
                    "BOSS",
                    "Riepilogo del combattimento",
                    "DMG · QUOTA",
                    "DMG · DPS MEDIO",
                    "DMG",
                    "QUOTA",
                    "DPS MEDIO",
                    "TIPI DI DANNO",
                    "Fisico",
                    "Fuoco",
                    "Ghiaccio",
                    "Fulmine",
                    "Caos",
                    "normale",
                    "misto",
                    "altro",
                    "Automatico mostra i DPS degli ultimi 5 secondi nei combattimenti normali e i danni cumulativi contro i BOSS. I resoconti mostrano i totali della battaglia e del piano.\nFuori dal combattimento, premi la scorciatoia delle statistiche o aprile dal menu di pausa. Tieni premuto per mezzo secondo per nascondere o ripristinare il pannello senza interrompere la registrazione.\nI totali sono temporanei e locali: in cooperativa possono differire. Cambiare piano, riconnettersi, ricaricare o disattivare le statistiche può cancellarli.",
                    "Disattivato",
                    "Statistiche danni nascoste",
                    "Statistiche danni ripristinate",
                    "Statistiche aperte",
                    "Statistiche chiuse",
                    "Nessun resoconto disponibile su questo piano",
                    "Statistiche non disponibili durante il caricamento",
                    "Statistiche non disponibili durante il cambio schermata",
                    "Statistiche non disponibili durante una scena",
                    "Chiudi il menu per vedere le statistiche",
                    "Il resoconto non è ancora pronto",
                    "{0} · Chiudi resoconto",
                    "Vedi statistiche",
                    "Ultimo scontro",
                    "Piano attuale",
                    "Chiudi statistiche",
                    "DPS medi · tempo di combattimento totale",
                    "DPS medi · durata dello scontro",
                    "Nessun combattimento registrato in questo piano",
                    "Questo scontro",
                    "Tempo totale in battaglia {0}"),
                ["pl-PL"] = I("Statystyki obrażeń",
                    "Automatycznie",
                    "Tylko bossowie",
                    "Każda walka",
                    "DPS",
                    "pokonani",
                    "moje ostatnie ciosy",
                    "zwykły",
                    "miniboss",
                    "BOSS",
                    "Podsumowanie walki",
                    "DMG · UDZIAŁ",
                    "DMG · ŚR. DPS",
                    "DMG",
                    "UDZIAŁ",
                    "ŚR. DPS",
                    "RODZAJE OBRAŻEŃ",
                    "Fizyczne",
                    "Ogień",
                    "Lód",
                    "Błyskawice",
                    "Chaos",
                    "zwykłe",
                    "mieszane",
                    "inne",
                    "Tryb automatyczny pokazuje DPS z ostatnich 5 sekund zwykłej walki i łączne obrażenia w walce z BOSS-em. Raporty zawierają sumy walki i piętra.\nPoza walką naciśnij skrót statystyk lub otwórz je w menu pauzy. Przytrzymanie przez pół sekundy ukrywa lub przywraca panel bez zatrzymywania zapisu.\nSumy są tymczasowe i lokalne, więc wyniki w kooperacji mogą się różnić. Zmiana piętra, ponowne połączenie, wczytanie lub wyłączenie statystyk może je usunąć.",
                    "Wyłączone",
                    "Statystyki obrażeń ukryte",
                    "Statystyki obrażeń przywrócone",
                    "Statystyki otwarte",
                    "Statystyki zamknięte",
                    "Brak raportu walki na tym piętrze",
                    "Statystyki niedostępne podczas ładowania",
                    "Statystyki niedostępne podczas przejścia ekranu",
                    "Statystyki niedostępne podczas przerywnika",
                    "Zamknij menu, aby zobaczyć statystyki",
                    "Raport walki nie jest jeszcze gotowy",
                    "{0} · Zamknij raport",
                    "Zobacz statystyki",
                    "Ostatnia walka",
                    "Obecne piętro",
                    "Zamknij statystyki",
                    "Średnie DPS · łączny czas walki",
                    "Średnie DPS · czas tej walki",
                    "Brak zapisanych walk na tym piętrze",
                    "Ta walka",
                    "Łączny czas walki {0}"),
                ["pt-BR"] = I("Estatísticas de dano",
                    "Automático",
                    "Só chefes",
                    "Cada combate",
                    "DPS",
                    "derrotados",
                    "meus golpes finais",
                    "normal",
                    "minichefe",
                    "BOSS",
                    "Resumo do combate",
                    "DMG · PARCELA",
                    "DMG · DPS MÉDIO",
                    "DMG",
                    "PARCELA",
                    "DPS MÉDIO",
                    "TIPOS DE DANO",
                    "Físico",
                    "Fogo",
                    "Gelo",
                    "Elétrico",
                    "Caos",
                    "normal",
                    "misto",
                    "outro",
                    "Automático mostra o DPS dos últimos 5 segundos em lutas comuns e o dano acumulado contra BOSS. Os relatórios mostram totais da luta e do andar.\nFora de combate, toque no atalho de estatísticas ou abra-as no menu de pausa. Segure por meio segundo para ocultar ou restaurar a exibição sem parar o registro.\nOs totais são temporários e locais; podem diferir no cooperativo. Mudar de andar, reconectar, recarregar ou desativar as estatísticas pode apagá-los.",
                    "Desativado",
                    "Estatísticas de dano ocultas",
                    "Exibição das estatísticas restaurada",
                    "Estatísticas abertas",
                    "Estatísticas fechadas",
                    "Ainda não há relatório de combate neste andar",
                    "Estatísticas indisponíveis durante o carregamento",
                    "Estatísticas indisponíveis durante a transição de tela",
                    "Estatísticas indisponíveis durante cenas",
                    "Feche o menu para ver as estatísticas",
                    "O relatório de combate ainda não está pronto",
                    "{0} · Fechar relatório",
                    "Ver estatísticas",
                    "Último combate",
                    "Andar atual",
                    "Fechar estatísticas",
                    "DPS médio · tempo de combate acumulado",
                    "DPS médio · duração do combate",
                    "Nenhum combate registrado neste andar",
                    "Este combate",
                    "Tempo total de combate {0}"),
                ["ru-RU"] = I("Статистика урона",
                    "Автоматически",
                    "Только боссы",
                    "Каждый бой",
                    "DPS",
                    "побеждено",
                    "мои последние удары",
                    "обычный",
                    "мини-босс",
                    "BOSS",
                    "Итоги боя",
                    "DMG · ДОЛЯ",
                    "DMG · СР. DPS",
                    "DMG",
                    "ДОЛЯ",
                    "СР. DPS",
                    "ТИПЫ УРОНА",
                    "Физический",
                    "Огонь",
                    "Лёд",
                    "Молния",
                    "Хаос",
                    "обычный",
                    "смешанный",
                    "прочий",
                    "Автоматический режим показывает DPS за последние 5 секунд в обычных боях и накопленный урон в боях с BOSS. Отчёты содержат итоги боя и этажа.\nВне боя нажмите клавишу статистики или откройте её в меню паузы. Удерживайте полсекунды, чтобы скрыть или вернуть панель, не прекращая запись.\nИтоги временные и локальные, поэтому в совместной игре могут различаться. Смена этажа, переподключение, перезагрузка или отключение статистики могут их очистить.",
                    "Отключено",
                    "Статистика урона скрыта",
                    "Отображение статистики восстановлено",
                    "Статистика открыта",
                    "Статистика закрыта",
                    "На этом этаже ещё нет отчёта о бое",
                    "Статистика недоступна при загрузке",
                    "Статистика недоступна при переходе экрана",
                    "Статистика недоступна во время сюжетной сцены",
                    "Закройте меню для просмотра статистики",
                    "Отчёт о бое ещё не готов",
                    "{0} · Закрыть отчёт",
                    "Показать статистику",
                    "Последний бой",
                    "Текущий этаж",
                    "Закрыть статистику",
                    "Средний DPS · суммарное время боя",
                    "Средний DPS · длительность боя",
                    "На этом этаже ещё нет записанных боёв",
                    "Этот бой",
                    "Общее время боя {0}"),
                ["sv-SE"] = I("Skadestatistik",
                    "Automatiskt",
                    "Endast bossar",
                    "Varje strid",
                    "DPS",
                    "besegrade",
                    "mina sista slag",
                    "vanlig",
                    "miniboss",
                    "BOSS",
                    "Stridssammanfattning",
                    "DMG · ANDEL",
                    "DMG · SNITT-DPS",
                    "DMG",
                    "ANDEL",
                    "SNITT-DPS",
                    "SKADETYPER",
                    "Fysisk",
                    "Eld",
                    "Is",
                    "Blixt",
                    "Kaos",
                    "normal",
                    "blandad",
                    "övrig",
                    "Automatiskt visar DPS för de senaste 5 sekunderna i vanliga strider och sammanlagd skada mot BOSS. Rapporter visar summor för striden och våningen.\nTryck kort på statistikgenvägen utanför strid eller öppna statistiken i pausmenyn. Håll i en halv sekund för att dölja eller visa panelen utan att stoppa registreringen.\nSummorna är tillfälliga och lokala; samarbetsresultat kan skilja sig. Våningsbyte, återanslutning, omladdning eller avstängd statistik kan rensa dem.",
                    "Inaktiverat",
                    "Skadestatistik dold",
                    "Skadestatistik visas igen",
                    "Statistik öppnad",
                    "Statistik stängd",
                    "Ingen stridsrapport finns på denna våning ännu",
                    "Statistik är inte tillgänglig under laddning",
                    "Statistik är inte tillgänglig vid skärmövergång",
                    "Statistik är inte tillgänglig under en mellansekvens",
                    "Stäng menyn för att visa statistik",
                    "Stridsrapporten är inte redo ännu",
                    "{0} · Stäng rapporten",
                    "Visa statistik",
                    "Senaste striden",
                    "Aktuell våning",
                    "Stäng statistik",
                    "Genomsnittlig DPS · sammanlagd stridstid",
                    "Genomsnittlig DPS · stridens längd",
                    "Inga strider registrerade på denna våning",
                    "Denna strid",
                    "Sammanlagd stridstid {0}"),
                ["th-TH"] = I("สถิติดาเมจ",
                    "อัตโนมัติ",
                    "บอสเท่านั้น",
                    "ทุกการต่อสู้",
                    "DPS",
                    "ปราบแล้ว",
                    "การโจมตีปิดท้ายของฉัน",
                    "ทั่วไป",
                    "มินิบอส",
                    "BOSS",
                    "สรุปการต่อสู้",
                    "DMG · สัดส่วน",
                    "DMG · DPS เฉลี่ย",
                    "DMG",
                    "สัดส่วน",
                    "DPS เฉลี่ย",
                    "ประเภทดาเมจ",
                    "กายภาพ",
                    "ไฟ",
                    "น้ำแข็ง",
                    "สายฟ้า",
                    "โกลาหล",
                    "ทั่วไป",
                    "ผสม",
                    "อื่น ๆ",
                    "โหมดอัตโนมัติแสดง DPS ย้อนหลัง 5 วินาทีในการต่อสู้ทั่วไป และดาเมจสะสมในการต่อสู้กับ BOSS รายงานแสดงยอดรวมของการต่อสู้และชั้นปัจจุบัน\nนอกการต่อสู้ กดปุ่มลัดสถิติสั้น ๆ หรือเปิดสถิติจากเมนูหยุดชั่วคราว กดค้างครึ่งวินาทีเพื่อซ่อนหรือแสดงโดยยังบันทึกต่อ\nยอดรวมเป็นข้อมูลชั่วคราวในเครื่อง ผลในโหมดร่วมมือจึงอาจต่างกัน การเปลี่ยนชั้น เชื่อมต่อใหม่ โหลดใหม่ หรือปิดสถิติอาจล้างข้อมูล",
                    "ปิด",
                    "ซ่อนสถิติดาเมจแล้ว",
                    "แสดงสถิติดาเมจอีกครั้งแล้ว",
                    "เปิดสถิติแล้ว",
                    "ปิดสถิติแล้ว",
                    "ยังไม่มีรายงานการต่อสู้ในชั้นนี้",
                    "แสดงสถิติระหว่างโหลดไม่ได้",
                    "แสดงสถิติระหว่างเปลี่ยนหน้าจอไม่ได้",
                    "แสดงสถิติระหว่างฉากเนื้อเรื่องไม่ได้",
                    "โปรดปิดเมนูเพื่อดูสถิติ",
                    "รายงานการต่อสู้ยังไม่พร้อม",
                    "{0} · ปิดรายงาน",
                    "ดูสถิติ",
                    "การต่อสู้ล่าสุด",
                    "ชั้นปัจจุบัน",
                    "ปิดสถิติ",
                    "DPS เฉลี่ย · เวลาต่อสู้สะสม",
                    "DPS เฉลี่ย · ระยะเวลาการต่อสู้",
                    "ยังไม่มีข้อมูลการต่อสู้ในชั้นนี้",
                    "การต่อสู้นี้",
                    "เวลาต่อสู้สะสม {0}"),
                ["tr-TR"] = I("Hasar istatistikleri",
                    "Otomatik",
                    "Yalnızca bosslar",
                    "Her savaş",
                    "DPS",
                    "yenilen",
                    "son vuruşlarım",
                    "normal",
                    "mini boss",
                    "BOSS",
                    "Savaş özeti",
                    "DMG · PAY",
                    "DMG · ORT. DPS",
                    "DMG",
                    "PAY",
                    "ORT. DPS",
                    "HASAR TÜRLERİ",
                    "Fiziksel",
                    "Ateş",
                    "Buz",
                    "Yıldırım",
                    "Kaos",
                    "normal",
                    "karma",
                    "diğer",
                    "Otomatik mod normal savaşlarda son 5 saniyenin DPS değerini, BOSS savaşlarında toplam hasarı gösterir. Raporlarda savaş ve kat toplamları bulunur.\nSavaş dışında istatistik kısayoluna dokunun veya duraklatma menüsünden istatistikleri açın. Yarım saniye basılı tutmak kaydı durdurmadan gösterimi gizler veya geri getirir.\nToplamlar geçici ve yereldir; ortak oyunda farklı olabilir. Kat değiştirmek, yeniden bağlanmak, yeniden yüklemek veya istatistikleri kapatmak bunları silebilir.",
                    "Kapalı",
                    "Hasar istatistikleri gizlendi",
                    "Hasar istatistikleri yeniden gösteriliyor",
                    "İstatistikler açıldı",
                    "İstatistikler kapatıldı",
                    "Bu katta henüz savaş raporu yok",
                    "Yükleme sırasında istatistikler gösterilemez",
                    "Ekran geçişinde istatistikler gösterilemez",
                    "Ara sahne sırasında istatistikler gösterilemez",
                    "İstatistikleri görmek için menüyü kapatın",
                    "Savaş raporu henüz hazır değil",
                    "{0} · Raporu kapat",
                    "İstatistikleri görüntüle",
                    "Son savaş",
                    "Mevcut kat",
                    "İstatistikleri kapat",
                    "Ortalama DPS · toplam savaş süresi",
                    "Ortalama DPS · bu savaşın süresi",
                    "Bu katta henüz savaş kaydı yok",
                    "Bu savaş",
                    "Toplam savaş süresi {0}")
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

        private static readonly Dictionary<string, string> RetryBossUnavailableTexts =
            new Dictionary<string, string>
            {
                ["en-US"] = "Boss retry unavailable",
                ["zh-CN"] = "Boss 重试不可用",
                ["zh-TW"] = "Boss 重試不可用",
                ["ko-KR"] = "보스 재시도 불가",
                ["ja-JP"] = "ボス再挑戦不可",
                ["de-DE"] = "Boss-Neustart nicht verfügbar",
                ["es-ES"] = "Reintento de jefe no disponible",
                ["fr-FR"] = "Reprise du boss indisponible",
                ["it-IT"] = "Riprova boss non disponibile",
                ["pl-PL"] = "Ponowienie bossa niedostępne",
                ["pt-BR"] = "Repetir chefe indisponível",
                ["ru-RU"] = "Повтор босса недоступен",
                ["sv-SE"] = "Bossförsök inte tillgängligt",
                ["th-TH"] = "ไม่สามารถสู้บอสใหม่ได้",
                ["tr-TR"] = "Boss tekrarı kullanılamıyor"
            };

        private static readonly Dictionary<string, Dictionary<string, string>>
            DefeatRetryTexts =
                new Dictionary<string, Dictionary<string, string>>
                {
                    ["en-US"] = R("Retry after defeat",
                        "Off by default. After a party wipe, retry the floor from entry or a supported boss from before its first phase. Items revert to the selected checkpoint. Boss retry is unavailable if the boss is already defeated, the encounter cannot be recreated, surrounding objects changed, or players changed or left the floor. In co-op, retry requires every player to be connected with a compatible Mod version.",
                        "Off", "On"),
                    ["zh-CN"] = R("失败后重试",
                        "默认关闭。全队死亡后，可选择从入层检查点重试整层，或从第一阶段开战前重试受支持的 Boss。道具恢复至所选检查点。Boss 已被击败、无法重建、周边对象发生变化，或玩家变更、离开本层时，Boss 重试不可用。 联机重试需要所有玩家均已连接，并安装兼容版本的 Mod。",
                        "关闭", "开启"),
                    ["zh-TW"] = R("失敗後重試",
                        "預設關閉。全隊死亡後，可選擇從入層檢查點重試整層，或從第一階段開戰前重試受支援的 Boss。道具恢復至所選檢查點。Boss 已被擊敗、無法重建、周邊物件發生變化，或玩家變更、離開本層時，Boss 重試不可用。 連線重試需要所有玩家均已連線，並安裝相容版本的 Mod。",
                        "關閉", "開啟"),
                    ["ko-KR"] = R("패배 후 재시도", "기본적으로 꺼져 있습니다. 전멸 후 층 입장 시점 또는 지원되는 보스의 첫 단계 시작 전부터 재시도합니다. 아이템은 선택한 체크포인트로 돌아갑니다. 보스를 이미 처치했거나 재생성할 수 없거나 주변 오브젝트가 바뀌거나 플레이어 구성 또는 층이 바뀌면 보스 재시도는 사용할 수 없습니다. 멀티플레이 재시도에는 모든 플레이어가 호환되는 모드 버전으로 연결되어 있어야 합니다.", "끄기", "켜기"),
                    ["ja-JP"] = R("敗北後に再挑戦", "初期設定はオフです。全滅後、フロア入口または対応ボスの第1段階開始前から再挑戦できます。アイテムは選択したチェックポイントに戻ります。ボスを撃破済みの場合、再生成できない場合、周囲のオブジェクトが変化した場合、参加者が変わった場合やフロアを離れた場合はボス再挑戦を利用できません。 マルチプレイでの再挑戦には、全員が対応バージョンのModを導入して接続している必要があります。", "オフ", "オン"),
                    ["de-DE"] = R("Nach Niederlage erneut versuchen",
                    "Standardmäßig aus. Nach einem Gruppen-Tod die Ebene ab Eingang oder einen unterstützten Boss vor Phase eins neu starten. Gegenstände werden auf den gewählten Kontrollpunkt zurückgesetzt. Boss-Neustart ist bei bereits besiegtem oder nicht rekonstruierbarem Boss, veränderten Umgebungsobjekten, Spielerwechsel oder Verlassen der Ebene nicht verfügbar. Im Koop müssen alle Spieler mit einer kompatiblen Mod-Version verbunden sein.",
                    "Aus",
                    "Ein"),
                    ["es-ES"] = R("Reintentar tras la derrota",
                    "Desactivado por defecto. Tras morir todo el grupo, reinicia la planta desde la entrada o un jefe compatible desde antes de su primera fase. Los objetos vuelven al punto elegido. El jefe no se puede reintentar si ya fue derrotado, no puede recrearse, cambia el entorno, cambian los jugadores o salen de la planta. En cooperativo, todos deben estar conectados con una versión compatible del mod para reintentar.",
                    "Desactivado",
                    "Activado"),
                    ["fr-FR"] = R("Réessayer après une défaite",
                    "Désactivé par défaut. Après la mort du groupe, reprenez à l’entrée de l’étage ou avant la première phase d’un boss compatible. Les objets reviennent au point choisi. La reprise du boss est indisponible si le boss est déjà vaincu, si sa recréation échoue, si les objets environnants changent ou si des joueurs changent ou quittent l’étage. En coopération, tous les joueurs doivent être connectés avec une version compatible du mod pour réessayer.",
                    "Désactivé",
                    "Activé"),
                    ["it-IT"] = R("Riprova dopo la sconfitta",
                    "Disattivato per impostazione predefinita. Dopo la sconfitta del gruppo, riparti dall’ingresso del piano o da prima della prima fase di un boss supportato. Gli oggetti tornano al checkpoint scelto. Il boss non è ripetibile se è già stato sconfitto, non può essere ricreato, cambiano gli oggetti circostanti, cambiano i giocatori o lasciano il piano. In cooperativa, tutti devono essere connessi con una versione compatibile della mod per riprovare.",
                    "Disattivato",
                    "Attivato"),
                    ["pl-PL"] = R("Ponów po porażce",
                    "Domyślnie wyłączone. Po śmierci drużyny ponów piętro od wejścia lub obsługiwanego bossa sprzed pierwszej fazy. Przedmioty wracają do wybranego punktu. Powtórka bossa jest niedostępna, gdy boss został już pokonany, nie można go odtworzyć, zmieniły się obiekty otoczenia, skład graczy lub gracze opuścili piętro. W kooperacji wszyscy gracze muszą być połączeni i mieć zgodną wersję moda, aby ponowić próbę.",
                    "Wył.",
                    "Wł."),
                    ["pt-BR"] = R("Tentar novamente após derrota",
                    "Desativado por padrão. Após a derrota da equipe, reinicie o andar pela entrada ou um chefe compatível antes da primeira fase. Os itens voltam ao ponto escolhido. Repetir o chefe fica indisponível se ele já foi derrotado, não puder ser recriado, se objetos ao redor mudarem, se os jogadores mudarem ou saírem do andar. No cooperativo, todos devem estar conectados com uma versão compatível do mod para tentar novamente.",
                    "Desativado",
                    "Ativado"),
                    ["ru-RU"] = R("Повтор после поражения",
                    "По умолчанию отключено. После гибели группы повторите этаж со входа или поддерживаемого босса до начала первой фазы. Предметы возвращаются к выбранной точке. Повтор босса недоступен, если босс уже побеждён, его нельзя воссоздать, изменились окружающие объекты, состав игроков или игроки покинули этаж. Для повтора в кооперативе все игроки должны быть подключены и использовать совместимую версию мода.",
                    "Выкл.",
                    "Вкл."),
                    ["sv-SE"] = R("Försök igen efter nederlag",
                    "Av som standard. Efter gruppens nederlag kan våningen startas om från ingången eller en boss som stöds från före första fasen. Föremål återställs till vald kontrollpunkt. Bossförsök saknas om bossen redan besegrats, inte kan återskapas, omgivningen ändrats eller spelare bytts ut eller lämnat våningen. I samarbete måste alla spelare vara anslutna med en kompatibel modversion för att försöka igen.",
                    "Av",
                    "På"),
                    ["th-TH"] = R("ลองใหม่หลังพ่ายแพ้",
                    "ปิดไว้ตามค่าเริ่มต้น เมื่อทั้งทีมตาย ให้เริ่มชั้นใหม่จากทางเข้าหรือเริ่มบอสที่รองรับใหม่ก่อนเฟสแรก ไอเทมจะกลับสู่จุดบันทึกที่เลือก ไม่สามารถสู้บอสใหม่ได้หากบอสถูกกำจัดแล้ว สร้างบอสใหม่ไม่ได้ วัตถุรอบข้างเปลี่ยนไป ผู้เล่นเปลี่ยน หรือออกจากชั้น ในโหมดร่วมมือ ผู้เล่นทุกคนต้องเชื่อมต่อและใช้ม็อดเวอร์ชันที่รองรับจึงจะลองใหม่ได้",
                    "ปิด",
                    "เปิด"),
                    ["tr-TR"] = R("Yenilgiden sonra yeniden dene",
                    "Varsayılan olarak kapalıdır. Grup yenilince kat girişinden veya desteklenen bossun ilk aşamasından önce yeniden başlatır. Eşyalar seçilen kontrol noktasına döner. Boss zaten yenildiyse, yeniden oluşturulamıyorsa, çevredeki nesneler değişmişse veya oyuncular değişmiş ya da kattan ayrılmışsa boss tekrarı kullanılamaz. Eşli oyunda tekrar denemek için tüm oyuncuların uyumlu bir mod sürümüyle bağlı olması gerekir.",
                    "Kapalı",
                    "Açık")
                };

        private static readonly Dictionary<string, Dictionary<string, string>> HelpTexts =
            new Dictionary<string, Dictionary<string, string>>
            {
                // "combo" here names the game's native HUD text, not CombatInsights hit streaks.
                ["en-US"] = H("Scale live statistics and combat reports. 100% is recommended; reports fit within the screen. The game's native HUD and combo text are unchanged."),
                ["zh-CN"] = H("缩放实时统计和战后战报，推荐 100%；战报自动适配屏幕边界。不改变游戏原生 HUD 与连招文字。"),
                ["zh-TW"] = H("縮放即時統計與戰後戰報，建議 100%；戰報自動適配螢幕邊界。不改變遊戲原生 HUD 與連招文字。"),
                ["ko-KR"] = H("실시간 통계와 전투 보고서의 크기를 조절합니다. 100%를 권장하며 보고서는 화면 안에 맞춰집니다. 게임 기본 HUD와 콤보 표시는 바뀌지 않습니다."),
                ["ja-JP"] = H("リアルタイム統計と戦闘レポートの大きさを変更します。100%を推奨し、レポートは画面内に収まります。ゲーム本体のHUDとコンボ表示は変更しません。"),
                ["de-DE"] = H("Skaliert Live-Statistik und Kampfberichte. 100% wird empfohlen; Berichte passen sich dem Bildschirm an. Das Spiel-HUD und die Kombo-Anzeige bleiben unverändert."),
                ["es-ES"] = H("Ajusta el tamaño de las estadísticas en vivo y los informes. Se recomienda 100%; los informes se adaptan a la pantalla. No cambia el HUD ni el texto de combos del juego."),
                ["fr-FR"] = H("Redimensionne les statistiques en direct et les rapports. 100% est recommandé ; les rapports s’adaptent à l’écran. Le HUD et l’affichage des combos du jeu restent inchangés."),
                ["it-IT"] = H("Ridimensiona le statistiche in tempo reale e i resoconti. Si consiglia 100%; i resoconti si adattano allo schermo. HUD e testo delle combo del gioco restano invariati."),
                ["pl-PL"] = H("Skaluje bieżące statystyki i raporty walki. Zalecane 100%; raporty mieszczą się na ekranie. Nie zmienia HUD-u ani tekstu kombinacji w grze."),
                ["pt-BR"] = H("Ajusta o tamanho das estatísticas em tempo real e dos relatórios. Recomendado: 100%; os relatórios se adaptam à tela. Não altera o HUD nem o texto de combos do jogo."),
                ["ru-RU"] = H("Масштабирует текущую статистику и отчёты о бое. Рекомендуется 100%; отчёты подстраиваются под экран. HUD игры и текст комбо не меняются."),
                ["sv-SE"] = H("Skalar löpande statistik och stridsrapporter. 100% rekommenderas; rapporterna anpassas till skärmen. Spelets HUD och kombinationstext ändras inte."),
                ["th-TH"] = H("ปรับขนาดสถิติสดและรายงานการต่อสู้ แนะนำ 100% โดยรายงานจะปรับให้พอดีหน้าจอ ไม่เปลี่ยน HUD และข้อความคอมโบของเกม"),
                ["tr-TR"] = H("Canlı istatistikleri ve savaş raporlarını ölçekler. %100 önerilir; raporlar ekrana sığar. Oyunun HUD ve kombo metni değişmez.")
            };

        private static readonly Dictionary<string, Dictionary<string, string>>
            HitStreakFeedbackTexts =
            new Dictionary<string, Dictionary<string, string>>
            {
                ["en-US"] = HitStreak("Hit-streak indicator",
                    "Show the local player's consecutive-hit count near the hit target, following its movement. Critical hits, executions, and milestones receive stronger emphasis. Follower attacks and identified damage-over-time ticks do not extend the streak."),
                ["zh-CN"] = HitStreak("连续命中提示", "在受击目标侧上方显示本地玩家本人的连续命中计数，并跟随目标移动；暴击、处决与里程碑会获得更强强调。随从攻击和游戏可识别的持续伤害不会延长连续命中。"),
                ["zh-TW"] = HitStreak("連續命中提示", "在受擊目標側上方顯示本機玩家本人的連續命中計數，並跟隨目標移動；暴擊、處決與里程碑會獲得更強強調。隨從攻擊和遊戲可辨識的持續傷害不會延長連續命中。"),
                ["ko-KR"] = HitStreak("연속 타격 표시", "명중한 대상 근처에 로컬 플레이어 본인의 연속 타격 수를 표시하고 대상의 움직임을 따라갑니다. 치명타, 처형, 연속 타격 이정표를 더 강하게 강조합니다. 추종자의 공격과 식별된 지속 피해는 연속 타격을 연장하지 않습니다."),
                ["ja-JP"] = HitStreak("連続ヒット表示", "攻撃を当てた対象の近くに自分自身の連続ヒット数を表示し、対象の動きに追従します。クリティカル、処刑、節目のヒット数を強く演出します。従者の攻撃と識別できた継続ダメージは連続ヒットを延長しません。"),
                ["de-DE"] = HitStreak("Trefferfolgen-Anzeige",
                    "Zeigt die eigene Trefferfolge nahe dem getroffenen Ziel und folgt dessen Bewegung. Kritische Treffer, Hinrichtungen und Meilensteine werden stärker betont. Angriffe von Begleitern und erkannter Schaden über Zeit verlängern die Trefferfolge nicht."),
                ["es-ES"] = HitStreak("Indicador de golpes consecutivos",
                    "Muestra los golpes consecutivos del propio jugador local cerca del objetivo golpeado y sigue su movimiento. Destaca más los críticos, las ejecuciones y los hitos de la racha. Los ataques de seguidores y el daño periódico identificado no prolongan la racha."),
                ["fr-FR"] = HitStreak("Indicateur de série de coups",
                    "Affiche la série de coups du joueur local près de la cible touchée et suit son déplacement. Les coups critiques, exécutions et paliers sont davantage mis en valeur. Les attaques des compagnons et les dégâts sur la durée identifiés ne prolongent pas la série."),
                ["it-IT"] = HitStreak("Indicatore serie di colpi",
                    "Mostra i colpi consecutivi del giocatore locale vicino al bersaglio colpito e ne segue il movimento. Critici, esecuzioni e traguardi ricevono maggiore enfasi. Gli attacchi dei seguaci e i danni nel tempo identificati non prolungano la serie."),
                ["pl-PL"] = HitStreak("Wskaźnik serii trafień",
                    "Pokazuje serię własnych trafień lokalnego gracza przy trafionym celu i podąża za jego ruchem. Trafienia krytyczne, egzekucje i kolejne progi serii są mocniej wyróżniane. Ataki towarzyszy i rozpoznane obrażenia w czasie nie przedłużają serii."),
                ["pt-BR"] = HitStreak("Indicador de sequência de acertos",
                    "Mostra a sequência de acertos do próprio jogador local perto do alvo atingido e acompanha seu movimento. Críticos, execuções e marcos da sequência recebem mais destaque. Ataques de seguidores e dano periódico identificado não prolongam a sequência."),
                ["ru-RU"] = HitStreak("Индикатор серии попаданий",
                    "Показывает серию собственных попаданий локального игрока рядом с поражённой целью и следует за её движением. Критические удары, казни и этапы серии выделяются сильнее. Атаки спутников и распознанный периодический урон не продлевают серию."),
                ["sv-SE"] = HitStreak("Indikator för träffserie", "Visar den lokala spelarens egen träffserie nära det träffade målet och följer dess rörelse. Kritiska träffar, avrättningar och milstolpar framhävs mer. Följeslagares attacker och identifierad skada över tid förlänger inte serien."),
                ["th-TH"] = HitStreak("ตัวแสดงการโจมตีต่อเนื่อง", "แสดงจำนวนการโจมตีต่อเนื่องของผู้เล่นในเครื่องเองใกล้เป้าหมายที่โดนโจมตี และเคลื่อนตามเป้าหมาย เน้นคริติคอล การประหาร และหลักจำนวนครั้งให้เด่นขึ้น การโจมตีของผู้ติดตามและดาเมจต่อเนื่องที่ตรวจพบจะไม่ต่อเวลาชุดโจมตี"),
                ["tr-TR"] = HitStreak("Seri vuruş göstergesi", "Yerel oyuncunun kendi seri vuruş sayısını vurulan hedefin yakınında gösterir ve hedefin hareketini takip eder. Kritik vuruşlar, infazlar ve seri eşikleri daha güçlü vurgulanır. Takipçilerin saldırıları ve tanımlanan zamanla hasar, seriyi uzatmaz.")
            };

        private static readonly Dictionary<string, Dictionary<string, string>> OutlineTexts =
            new Dictionary<string, Dictionary<string, string>>
            {
                ["en-US"] = O("Ally & enemy outlines",
                    "Highlights allies in cyan and enemies in red. Follow Game limits outlines to multiplayer; other combat visual presets use their configured outline scope. The local player is never outlined."),
                ["zh-CN"] = O("敌我描边", "以青色标出友方、红色标出敌方。“跟随游戏”仅在多人游戏中显示；其他战斗视觉预设使用各自配置的描边范围。本地玩家始终不会被描边。"),
                ["zh-TW"] = O("敵我描邊", "以青色標示友方、紅色標示敵方。「跟隨遊戲」僅在多人遊戲中顯示；其他戰鬥視覺預設使用各自設定的描邊範圍。本機玩家始終不會被描邊。"),
                ["ko-KR"] = O("아군 및 적 윤곽선", "아군은 청록색, 적은 빨간색으로 표시합니다. 게임 설정 따르기에서는 멀티플레이에서만 표시하며, 다른 전투 시각 프리셋은 설정된 범위를 사용합니다. 로컬 플레이어는 표시하지 않습니다."),
                ["ja-JP"] = O("味方と敵の輪郭", "味方をシアン、敵を赤で強調します。「ゲームに従う」ではマルチプレイのみ、他の戦闘表示プリセットでは設定した範囲に表示します。自分の輪郭は表示しません。"),
                ["de-DE"] = O("Umrisse für Freund und Feind",
                    "Markiert Verbündete türkis und Feinde rot. Spielvorgabe zeigt Umrisse nur im Mehrspielermodus; andere Kampfansichten nutzen ihren festgelegten Umfang. Der lokale Spieler wird nie umrandet."),
                ["es-ES"] = O("Contornos de aliados y enemigos",
                    "Resalta aliados en cian y enemigos en rojo. Seguir el juego solo muestra contornos en multijugador; los otros preajustes usan su alcance configurado. Nunca se resalta al jugador local."),
                ["fr-FR"] = O("Contours des alliés et ennemis",
                    "Affiche les alliés en cyan et les ennemis en rouge. Suivre le jeu limite les contours au multijoueur ; les autres préréglages utilisent la portée configurée. Le joueur local n’a jamais de contour."),
                ["it-IT"] = O("Contorni di alleati e nemici",
                    "Evidenzia gli alleati in ciano e i nemici in rosso. Segui il gioco limita i contorni al multigiocatore; gli altri profili usano l’ambito configurato. Il giocatore locale non viene mai evidenziato."),
                ["pl-PL"] = O("Obrysy sojuszników i wrogów",
                    "Wyróżnia sojuszników na turkusowo, a wrogów na czerwono. Zgodnie z grą pokazuje obrysy tylko w trybie wieloosobowym; inne profile stosują ustawiony zakres. Lokalny gracz nigdy nie ma obrysu."),
                ["pt-BR"] = O("Contornos de aliados e inimigos",
                    "Destaca aliados em ciano e inimigos em vermelho. Seguir o jogo limita os contornos ao multijogador; as outras predefinições usam o alcance configurado. O jogador local nunca recebe contorno."),
                ["ru-RU"] = O("Контуры союзников и врагов", "Выделяет союзников бирюзовым, врагов красным. Как в игре показывает контуры только по сети; другие профили используют заданный охват. Локальный игрок никогда не выделяется."),
                ["sv-SE"] = O("Konturer för vän och fiende",
                    "Markerar allierade i cyan och fiender i rött. Följ spelet visar konturer endast i flerspelarläge; andra förval använder inställd omfattning. Den lokala spelaren markeras aldrig."),
                ["th-TH"] = O("เส้นขอบฝ่ายเดียวกันและศัตรู", "เน้นฝ่ายเดียวกันด้วยสีฟ้าอมเขียวและศัตรูด้วยสีแดง ตามเกมจะแสดงเส้นขอบเฉพาะเมื่อเล่นหลายคน ส่วนชุดอื่นใช้ขอบเขตที่ตั้งไว้ ไม่แสดงเส้นขอบผู้เล่นในเครื่อง"),
                ["tr-TR"] = O("Dost ve düşman hatları", "Dostları camgöbeği, düşmanları kırmızıyla vurgular. Oyunu izle yalnızca çok oyunculu modda hat gösterir; diğer ön ayarlar kendi kapsamını kullanır. Yerel oyuncu asla vurgulanmaz.")
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
                        "開啟開發者控制台", "關閉", "開啟"),
                    ["ko-KR"] = D("개발자 콘솔", "게임에 내장된 개발자 콘솔을 엽니다. 명령어는 저장, 해금, 업적, 멀티플레이 상태를 변경할 수 있습니다. 기본적으로 꺼져 있습니다.", "개발자 콘솔 열기", "끄기", "켜기"),
                    ["ja-JP"] = D("開発者コンソール", "ゲーム内蔵の開発者コンソールを有効にします。コマンドはセーブ、アンロック、実績、マルチプレイ状態を変更する場合があります。初期設定はオフです。", "開発者コンソールを開く", "オフ", "オン"),
                    ["de-DE"] = D("Entwicklerkonsole",
                    "Schaltet die eingebaute Entwicklerkonsole frei. Befehle können Spielstände, Freischaltungen, Erfolge und Mehrspielerzustände verändern. Standardmäßig aus.",
                    "Entwicklerkonsole öffnen",
                    "Aus",
                    "Ein"),
                    ["es-ES"] = D("Consola de desarrollo",
                    "Activa la consola de desarrollo del juego. Los comandos pueden modificar partidas guardadas, desbloqueos, logros y el estado multijugador. Desactivada por defecto.",
                    "Abrir consola de desarrollo",
                    "Desactivado",
                    "Activado"),
                    ["fr-FR"] = D("Console de développement",
                    "Active la console intégrée au jeu. Ses commandes peuvent modifier les sauvegardes, déblocages, succès et l’état multijoueur. Désactivée par défaut.",
                    "Ouvrir la console de développement",
                    "Désactivé",
                    "Activé"),
                    ["it-IT"] = D("Console sviluppatore",
                    "Abilita la console integrata nel gioco. I comandi possono modificare salvataggi, sblocchi, obiettivi e stato multigiocatore. Disattivata per impostazione predefinita.",
                    "Apri console sviluppatore",
                    "Disattivato",
                    "Attivato"),
                    ["pl-PL"] = D("Konsola deweloperska",
                    "Odblokowuje konsolę wbudowaną w grę. Polecenia mogą zmieniać zapisy, odblokowania, osiągnięcia i stan rozgrywki wieloosobowej. Domyślnie wyłączona.",
                    "Otwórz konsolę deweloperską",
                    "Wył.",
                    "Wł."),
                    ["pt-BR"] = D("Console de desenvolvimento",
                    "Ativa o console integrado ao jogo. Os comandos podem alterar salvamentos, desbloqueios, conquistas e o estado multijogador. Desativado por padrão.",
                    "Abrir console de desenvolvimento",
                    "Desativado",
                    "Ativado"),
                    ["ru-RU"] = D("Консоль разработчика",
                    "Открывает доступ к встроенной консоли. Команды могут менять сохранения, разблокировки, достижения и состояние сетевой игры. По умолчанию отключена.",
                    "Открыть консоль разработчика",
                    "Выкл.",
                    "Вкл."),
                    ["sv-SE"] = D("Utvecklarkonsol", "Aktiverar spelets inbyggda utvecklarkonsol. Kommandon kan ändra sparfiler, upplåsningar, prestationer och flerspelartillstånd. Av som standard.", "Öppna utvecklarkonsolen", "Av", "På"),
                    ["th-TH"] = D("คอนโซลผู้พัฒนา", "เปิดใช้คอนโซลผู้พัฒนาที่มีในเกม คำสั่งอาจเปลี่ยนข้อมูลบันทึก สิ่งที่ปลดล็อก ความสำเร็จ และสถานะผู้เล่นหลายคน ปิดไว้ตามค่าเริ่มต้น", "เปิดคอนโซลผู้พัฒนา", "ปิด", "เปิด"),
                    ["tr-TR"] = D("Geliştirici konsolu",
                    "Oyunun yerleşik geliştirici konsolunu açar. Komutlar kayıtları, açılan içerikleri, başarımları ve çok oyunculu durumu değiştirebilir. Varsayılan olarak kapalıdır.",
                    "Geliştirici konsolunu aç",
                    "Kapalı",
                    "Açık")
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
                        "放大本機玩家及其召喚單位建立的傷害；連線用戶端無法修改由伺服器判定的傷害。僅在開發版本中提供。"),
                    ["ko-KR"] = P("플레이어 피해 배율", "로컬 플레이어와 소환수가 생성한 피해를 늘립니다. 온라인 클라이언트에서는 서버가 판정한 피해를 변경할 수 없습니다. 개발 빌드에서만 제공됩니다."),
                    ["ja-JP"] = P("プレイヤーのダメージ倍率", "ローカルプレイヤーとその召喚ユニットが生成するダメージを増やします。オンラインのクライアントではサーバーが判定するダメージを変更できません。開発ビルド専用です。"),
                    ["de-DE"] = P("Spielerschaden-Multiplikator",
                    "Verstärkt Schaden des lokalen Spielers und seiner beschworenen Einheiten. Online-Clients können serverseitig bestimmten Schaden nicht ändern. Nur in Entwickler-Builds verfügbar."),
                    ["es-ES"] = P("Multiplicador de daño del jugador",
                    "Amplifica el daño generado por el jugador local y sus invocaciones. Los clientes en línea no pueden modificar el daño resuelto por el servidor. Solo disponible en versiones de desarrollo."),
                    ["fr-FR"] = P("Multiplicateur de dégâts du joueur",
                    "Amplifie les dégâts produits par le joueur local et ses invocations. Les clients en ligne ne peuvent pas modifier les dégâts déterminés par le serveur. Réservé aux versions de développement."),
                    ["it-IT"] = P("Moltiplicatore danni del giocatore",
                    "Aumenta i danni generati dal giocatore locale e dalle sue evocazioni. I client online non possono modificare i danni determinati dal server. Disponibile solo nelle versioni di sviluppo."),
                    ["pl-PL"] = P("Mnożnik obrażeń gracza", "Zwiększa obrażenia tworzone przez lokalnego gracza i jego przywołane jednostki. Klienci online nie mogą zmieniać obrażeń rozstrzyganych przez serwer. Tylko w wersji deweloperskiej."),
                    ["pt-BR"] = P("Multiplicador de dano do jogador",
                    "Amplifica o dano gerado pelo jogador local e suas invocações. Clientes online não podem alterar danos determinados pelo servidor. Disponível apenas em versões de desenvolvimento."),
                    ["ru-RU"] = P("Множитель урона игрока", "Усиливает урон, создаваемый локальным игроком и его призванными существами. Сетевые клиенты не могут менять урон, определяемый сервером. Только для сборок разработки."),
                    ["sv-SE"] = P("Spelarskademultiplikator", "Ökar skada som skapas av den lokala spelaren och dess frammanade enheter. Onlineklienter kan inte ändra serverbestämd skada. Endast i utvecklarbyggen."),
                    ["th-TH"] = P("ตัวคูณดาเมจผู้เล่น", "เพิ่มดาเมจที่ผู้เล่นในเครื่องและยูนิตอัญเชิญสร้างขึ้น ไคลเอนต์ออนไลน์เปลี่ยนดาเมจที่เซิร์ฟเวอร์คำนวณไม่ได้ มีเฉพาะรุ่นพัฒนา"),
                    ["tr-TR"] = P("Oyuncu hasar çarpanı", "Yerel oyuncunun ve çağırdığı birimlerin oluşturduğu hasarı artırır. Çevrimiçi istemciler sunucunun belirlediği hasarı değiştiremez. Yalnızca geliştirme sürümlerinde bulunur.")
                };

        internal static void Register(Action<string, string, string> addText)
        {
            string[] languages = LocalizationLanguages.All;
            foreach (var group in new[] { Texts, SuiteTexts, InsightsTexts, DeveloperConsoleTexts,
                DeveloperPlayerDamageTexts, DefeatRetryTexts, AdditionalTexts, HelpTexts,
                HitStreakFeedbackTexts, OutlineTexts })
                LocalizationGroup.Register(addText, languages, group);
            LocalizationGroup.Register(addText, languages, RetryFloor, RetryFloorTexts);
            LocalizationGroup.Register(addText, languages, RetryBossUnavailable, RetryBossUnavailableTexts);
            LocalizationGroup.Register(addText, languages, RetryBossEncounter, RetryBossEncounterTexts);

            int[] percentages = { 80, 90, 100, 110, 120 };
            foreach (string language in languages)
            {
                addText(language, Section, "SEPHIRIA ENHANCEMENTS · by 0xMashiro");
                for (int index = 0; index < ScaleKeys.Length; index++)
                    addText(language, ScaleKeys[index], percentages[index] + "%");
            }

            ControlLocalization.Register(addText);
#if SEPHIRIA_ENHANCEMENTS_DEVTOOLS
            Diagnostics.InventoryReproductionLocalization.Register(addText);
#endif
            MapEnhancements.MapEnhancementsLocalization.Register(addText, languages);
            MapEnhancements.MapNavigationLocalization.Register(addText, languages);
            ModJournal.ModJournalLocalization.Register(addText, languages);
            EffectStats.EffectStatsLocalization.Register(addText, languages);
            OptionsCategoryLocalization.Register(addText, languages);
            SettingsInteractionLocalization.Register(addText, languages);
            CombatVisualLocalization.Register(addText, languages);
            ResourceBarValues.ResourceBarValueLocalization.Register(addText, languages);
            AutoCasting.AutoCastingLocalization.Register(addText, languages);
            DefeatRetry.DefeatRetryAvailabilityLocalization.Register(addText, languages);
            DefeatRetry.DefeatRetryCutsceneLocalization.Register(addText, languages);
            DefeatRetry.RetryRecoveryLocalization.Register(addText, languages);
            Inventory.InventoryOptimizationLocalization.Register(addText);
            Inventory.InventoryMagicCostLocalization.Register(addText);
            Inventory.InventoryPresetIntentLocalization.Register(addText);
            Inventory.RewardHighlightLocalization.Register(addText);
            MultiplayerRulesLocalization.Register(addText, languages);
            MultiplayerAccessLocalization.Register(addText, languages);
            JoiningSupplyLocalization.Register(addText, languages);
            ModInformation.ModInformationLocalization.Register(addText, languages);
            Runtime.FeatureFailureLocalization.Register(addText, languages);

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
            string displayRestored, string statisticsOpened, string statisticsClosed,
            string reportUnavailable, string reportLoading, string reportScreenTransition,
            string reportCutscene, string reportMenu, string reportHudUnavailable,
            string reportDismissHint, string viewStatistics, string recentEncounterStatistics,
            string currentFloorStatistics, string closeStatistics, string floorBattleTime,
            string encounterBattleTime, string floorStatisticsEmpty, string currentEncounterStatistics,
            string floorBattleDuration)
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
                [StatisticsOpened] = statisticsOpened,
                [StatisticsClosed] = statisticsClosed,
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
                [ReportDismissHint] = reportDismissHint,
                [ViewStatistics] = viewStatistics,
                [RecentEncounterStatistics] = recentEncounterStatistics,
                [CurrentEncounterStatistics] = currentEncounterStatistics,
                [CurrentFloorStatistics] = currentFloorStatistics,
                [CloseStatistics] = closeStatistics,
                [FloorBattleTime] = floorBattleTime,
                [FloorBattleDuration] = floorBattleDuration,
                [EncounterBattleTime] = encounterBattleTime,
                [FloorStatisticsEmpty] = floorStatisticsEmpty,
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
