using System;
using System.Collections.Generic;
using SephiriaEnhancements.Configuration;

namespace SephiriaEnhancements.DefeatRetry
{
    internal static class DefeatRetryCutsceneLocalization
    {
        internal const string Name = "SephiriaEnhancements.DefeatRetry.Cutscenes.Name";
        internal const string Help = "SephiriaEnhancements.DefeatRetry.Cutscenes.Help";
        internal const string On = "SephiriaEnhancements.DefeatRetry.Cutscenes.On";
        internal const string Off = "SephiriaEnhancements.DefeatRetry.Cutscenes.Off";
        private static readonly string[] Keys = { Name, Help, On, Off };
        private static readonly Dictionary<string, string[]> Texts = new Dictionary<string, string[]>
        {
            ["en-US"] = new[] { "Skip Boss cutscenes on retry", "On by default. Vote to skip Boss cutscenes on the retried floor when the game allows it. Off follows the game's cutscene settings and skip controls.", "On", "Off" },
            ["zh-CN"] = new[] { "重试时跳过 Boss 演出", "默认开启。重试后，在游戏允许时投票跳过本层 Boss 演出。关闭后遵循游戏的演出设置，并可使用原生跳过操作。", "开启", "关闭" },
            ["zh-TW"] = new[] { "重試時跳過 Boss 演出", "預設開啟。重試後，在遊戲允許時投票跳過本層 Boss 演出。關閉後遵循遊戲的演出設定，並可使用原生跳過操作。", "開啟", "關閉" },
            ["ja-JP"] = new[] { "再挑戦時にボス演出をスキップ", "初期設定はオンです。再挑戦したフロアでは、ゲームが許可するとボス演出のスキップに投票します。オフではゲームの演出設定とスキップ操作に従います。", "オン", "オフ" },
            ["ko-KR"] = new[] { "재시도 시 보스 연출 건너뛰기", "기본적으로 켜져 있습니다. 재시도한 층에서 게임이 허용하면 보스 연출 건너뛰기에 투표합니다. 끄면 게임의 연출 설정과 건너뛰기 조작을 따릅니다.", "켜기", "끄기" },
            ["de-DE"] = new[] { "Boss-Szenen beim Neustart überspringen", "Standardmäßig an. Stimmt auf der neu gestarteten Ebene für das Überspringen, sobald das Spiel es erlaubt. Aus verwendet die Szeneneinstellungen und Steuerung des Spiels.", "Ein", "Aus" },
            ["es-ES"] = new[] { "Omitir escenas del jefe al reintentar", "Activado por defecto. Vota para omitir escenas del jefe en la planta reintentada cuando el juego lo permite. Desactivado usa los ajustes y controles del juego.", "Activado", "Desactivado" },
            ["fr-FR"] = new[] { "Passer les scènes du boss après reprise", "Activé par défaut. Vote pour passer les scènes du boss à l'étage repris dès que le jeu le permet. Désactivé utilise les réglages et commandes du jeu.", "Activé", "Désactivé" },
            ["it-IT"] = new[] { "Salta le scene del boss quando riprovi", "Attivo per impostazione predefinita. Vota per saltare le scene del boss nel piano riprovato quando il gioco lo consente. Disattivato segue le impostazioni e i comandi del gioco.", "Attivato", "Disattivato" },
            ["pl-PL"] = new[] { "Pomijaj sceny bossa przy powtórce", "Domyślnie włączone. Głosuje za pominięciem scen bossa na powtarzanym piętrze, gdy gra na to pozwala. Wyłączenie przywraca ustawienia i sterowanie gry.", "Wł.", "Wył." },
            ["pt-BR"] = new[] { "Pular cenas do chefe ao tentar de novo", "Ativado por padrão. Vota para pular cenas do chefe no andar reiniciado quando o jogo permite. Desativado segue as configurações e os controles do jogo.", "Ativado", "Desativado" },
            ["ru-RU"] = new[] { "Пропускать сцены босса при повторе", "Включено по умолчанию. Голосует за пропуск сцен босса на повторяемом этаже, когда игра разрешает. При отключении используются настройки и управление игры.", "Вкл.", "Выкл." },
            ["sv-SE"] = new[] { "Hoppa över bossscener vid nytt försök", "På som standard. Röstar för att hoppa över bossscener på våningen som startats om när spelet tillåter det. Av följer spelets inställningar och kontroller.", "På", "Av" },
            ["th-TH"] = new[] { "ข้ามฉากบอสเมื่อลองใหม่", "เปิดตามค่าเริ่มต้น โหวตข้ามฉากบอสในชั้นที่ลองใหม่เมื่อเกมอนุญาต หากปิดจะใช้การตั้งค่าและปุ่มข้ามฉากของเกม", "เปิด", "ปิด" },
            ["tr-TR"] = new[] { "Tekrarda boss sahnelerini atla", "Varsayılan olarak açık. Yeniden başlatılan katta oyun izin verdiğinde boss sahnelerini atlamak için oy verir. Kapalıyken oyunun sahne ayarları ve kontrolleri kullanılır.", "Açık", "Kapalı" }
        };
        internal static void Register(Action<string, string, string> addText, IEnumerable<string> languages) =>
            LocalizationGroup.Register(addText, languages, Keys, Texts);
    }
}
