using System;
using System.Collections.Generic;
using SephiriaEnhancements.MultiplayerRules.Presentation;
using SephiriaEnhancements.MultiplayerAccess.Presentation;
using SephiriaEnhancements.CombatVisuals;

namespace SephiriaEnhancements.Configuration
{
    internal static partial class ModLocalization
    {
        internal const string Off = "SephiriaEnhancements.Off";
        internal const string On = "SephiriaEnhancements.On";
        internal const string Section = "SephiriaEnhancements.Section";
        internal const string SettingMasterEnabled = "SephiriaEnhancements.Setting.Enabled";
        internal const string HelpMasterEnabled = "SephiriaEnhancements.Help.Enabled";
        internal const string SettingNativeCompanion =
            "SephiriaEnhancements.Setting.NativeCompanion";
        internal const string HelpNativeCompanion =
            "SephiriaEnhancements.Help.NativeCompanion";
        internal const string SuiteOff = "SephiriaEnhancements.Suite.Off";
        internal const string SuiteOn = "SephiriaEnhancements.Suite.On";

        internal static readonly string[] NativeCompanionModeKeys =
        {
            SuiteOff,
            "SephiriaEnhancements.NativeCompanion.SoloOnly",
            "SephiriaEnhancements.NativeCompanion.SmartFill",
            "SephiriaEnhancements.NativeCompanion.AlwaysHost"
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

        internal static void Register(Action<string, string, string> addText)
        {
            string[] languages = LocalizationLanguages.All;
            LocalizationGroup.Register(addText, languages, Texts);
            LocalizationGroup.Register(addText, languages, SuiteTexts);
            foreach (string language in languages)
                addText(language, Section, "SEPHIRIA ENHANCEMENTS · by 0xMashiro");
            Combat.CombatInsightsLocalization.Register(addText, languages);
            DeveloperConsole.DeveloperConsoleLocalization.Register(addText, languages);
            DeveloperTools.DeveloperPlayerDamageLocalization.Register(addText, languages);
            DefeatRetry.DefeatRetryLocalization.Register(addText, languages);
            CombatRelationOutlines.CombatRelationOutlinesLocalization.Register(addText, languages);

            ControlLocalization.Register(addText);
#if SEPHIRIA_ENHANCEMENTS_DEVTOOLS
            Diagnostics.InventoryReproductionLocalization.Register(addText);
#endif
            MapEnhancements.MapEnhancementsLocalization.Register(addText, languages);
            MapEnhancements.MapNavigationLocalization.Register(addText, languages);
            ModJournal.ModJournalLocalization.Register(addText, languages);
            CostumeAppearance.CostumeAppearanceLocalization.Register(addText, languages);
            StageRewardAutoClaim.StageRewardAutoClaimLocalization.Register(addText, languages);
            EffectStats.EffectStatsLocalization.Register(addText, languages);
            OptionsCategoryLocalization.Register(addText, languages);
            SettingsInteractionLocalization.Register(addText, languages);
            CombatVisualLocalization.Register(addText, languages);
            ResourceBarValues.ResourceBarValueLocalization.Register(addText, languages);
            Combat.DamageSourcesLocalization.Register(addText, languages);
            AutoCasting.AutoCastingLocalization.Register(addText, languages);
            DefeatRetry.DefeatRetryAvailabilityLocalization.Register(addText, languages);
            DefeatRetry.DefeatRetryCutsceneLocalization.Register(addText, languages);
            DefeatRetry.RetryRecoveryLocalization.Register(addText, languages);
            Inventory.InventoryOptimizationLocalization.Register(addText);
            Inventory.InventoryItemRecoveryLocalization.Register(addText);
            Inventory.InventoryMagicCostLocalization.Register(addText);
            Inventory.InventoryPresetIntentLocalization.Register(addText);
            Inventory.RewardHighlightLocalization.Register(addText);
            MultiplayerRulesLocalization.Register(addText, languages);
            MultiplayerAccessLocalization.Register(addText, languages);
            JoiningSupplyLocalization.Register(addText, languages);
            ModInformation.ModInformationLocalization.Register(addText, languages);
            Runtime.FeatureFailureLocalization.Register(addText, languages);

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

    }
}
