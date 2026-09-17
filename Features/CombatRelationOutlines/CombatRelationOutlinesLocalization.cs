using System;
using System.Collections.Generic;
using SephiriaEnhancements.Configuration;

namespace SephiriaEnhancements.CombatRelationOutlines
{
    internal static class CombatRelationOutlinesLocalization
    {
        internal const string SettingCombatRelationOutlines =
            "SephiriaEnhancements.Setting.CombatRelationOutlines";
        internal const string HelpCombatRelationOutlines =
            "SephiriaEnhancements.Help.CombatRelationOutlines";

        private static readonly Dictionary<string, Dictionary<string, string>> OutlineTexts =
            new Dictionary<string, Dictionary<string, string>>
            {
                ["en-US"] = CreateTexts("Ally & enemy outlines",
                    "Highlights allies in cyan and enemies in red. Follow Game limits outlines to multiplayer; other combat visual presets use their configured outline scope. The local player is never outlined."),
                ["zh-CN"] = CreateTexts("敌我描边", "以青色标出友方、红色标出敌方。“跟随游戏”仅在多人游戏中显示；其他战斗视觉预设使用各自配置的描边范围。本地玩家始终不会被描边。"),
                ["zh-TW"] = CreateTexts("敵我描邊", "以青色標示友方、紅色標示敵方。「跟隨遊戲」僅在多人遊戲中顯示；其他戰鬥視覺預設使用各自設定的描邊範圍。本機玩家始終不會被描邊。"),
                ["ko-KR"] = CreateTexts("아군 및 적 윤곽선", "아군은 청록색, 적은 빨간색으로 표시합니다. 게임 설정 따르기에서는 멀티플레이에서만 표시하며, 다른 전투 시각 프리셋은 설정된 범위를 사용합니다. 로컬 플레이어는 표시하지 않습니다."),
                ["ja-JP"] = CreateTexts("味方と敵の輪郭", "味方をシアン、敵を赤で強調します。「ゲームに従う」ではマルチプレイのみ、他の戦闘表示プリセットでは設定した範囲に表示します。自分の輪郭は表示しません。"),
                ["de-DE"] = CreateTexts("Umrisse für Freund und Feind",
                    "Markiert Verbündete türkis und Feinde rot. Spielvorgabe zeigt Umrisse nur im Mehrspielermodus; andere Kampfansichten nutzen ihren festgelegten Umfang. Der lokale Spieler wird nie umrandet."),
                ["es-ES"] = CreateTexts("Contornos de aliados y enemigos",
                    "Resalta aliados en cian y enemigos en rojo. Seguir el juego solo muestra contornos en multijugador; los otros preajustes usan su alcance configurado. Nunca se resalta al jugador local."),
                ["fr-FR"] = CreateTexts("Contours des alliés et ennemis",
                    "Affiche les alliés en cyan et les ennemis en rouge. Suivre le jeu limite les contours au multijoueur ; les autres préréglages utilisent la portée configurée. Le joueur local n’a jamais de contour."),
                ["it-IT"] = CreateTexts("Contorni di alleati e nemici",
                    "Evidenzia gli alleati in ciano e i nemici in rosso. Segui il gioco limita i contorni al multigiocatore; gli altri profili usano l’ambito configurato. Il giocatore locale non viene mai evidenziato."),
                ["pl-PL"] = CreateTexts("Obrysy sojuszników i wrogów",
                    "Wyróżnia sojuszników na turkusowo, a wrogów na czerwono. Zgodnie z grą pokazuje obrysy tylko w trybie wieloosobowym; inne profile stosują ustawiony zakres. Lokalny gracz nigdy nie ma obrysu."),
                ["pt-BR"] = CreateTexts("Contornos de aliados e inimigos",
                    "Destaca aliados em ciano e inimigos em vermelho. Seguir o jogo limita os contornos ao multijogador; as outras predefinições usam o alcance configurado. O jogador local nunca recebe contorno."),
                ["ru-RU"] = CreateTexts("Контуры союзников и врагов", "Выделяет союзников бирюзовым, врагов красным. Как в игре показывает контуры только по сети; другие профили используют заданный охват. Локальный игрок никогда не выделяется."),
                ["sv-SE"] = CreateTexts("Konturer för vän och fiende",
                    "Markerar allierade i cyan och fiender i rött. Följ spelet visar konturer endast i flerspelarläge; andra förval använder inställd omfattning. Den lokala spelaren markeras aldrig."),
                ["th-TH"] = CreateTexts("เส้นขอบฝ่ายเดียวกันและศัตรู", "เน้นฝ่ายเดียวกันด้วยสีฟ้าอมเขียวและศัตรูด้วยสีแดง ตามเกมจะแสดงเส้นขอบเฉพาะเมื่อเล่นหลายคน ส่วนชุดอื่นใช้ขอบเขตที่ตั้งไว้ ไม่แสดงเส้นขอบผู้เล่นในเครื่อง"),
                ["tr-TR"] = CreateTexts("Dost ve düşman hatları", "Dostları camgöbeği, düşmanları kırmızıyla vurgular. Oyunu izle yalnızca çok oyunculu modda hat gösterir; diğer ön ayarlar kendi kapsamını kullanır. Yerel oyuncu asla vurgulanmaz.")
            };

        private static Dictionary<string, string> CreateTexts(string label, string help)
        {
            return new Dictionary<string, string>
            {
                [SettingCombatRelationOutlines] = label,
                [HelpCombatRelationOutlines] = help
            };
        }

        internal static void Register(Action<string, string, string> addText, IEnumerable<string> languages)
        {
            LocalizationGroup.Register(addText, languages, OutlineTexts);
        }
    }
}
