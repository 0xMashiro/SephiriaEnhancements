using System;
using System.Collections.Generic;
using SephiriaEnhancements.Configuration;

namespace SephiriaEnhancements.DeveloperConsole
{
    internal static class DeveloperConsoleLocalization
    {
        internal const string SettingDeveloperConsole =
            "SephiriaEnhancements.Setting.DeveloperConsole";
        internal const string HelpDeveloperConsole =
            "SephiriaEnhancements.Help.DeveloperConsole";
        internal const string DeveloperConsoleShortcut =
            "SephiriaEnhancements.Controls.DeveloperConsole";
        internal const string DeveloperConsoleOff =
            "SephiriaEnhancements.DeveloperConsole.Off";
        internal const string DeveloperConsoleOn =
            "SephiriaEnhancements.DeveloperConsole.On";

        private static readonly Dictionary<string, Dictionary<string, string>>
            DeveloperConsoleTexts =
                new Dictionary<string, Dictionary<string, string>>
                {
                    ["en-US"] = CreateTexts("Developer console",
                        "Unlocks the game's built-in developer console. Commands can modify saves, unlocks, achievements, and multiplayer state. Disabled by default.",
                        "Open developer console", "Off", "On"),
                    ["zh-CN"] = CreateTexts("开发者控制台",
                        "解锁游戏内置的开发者控制台。命令可能修改存档、解锁内容、成就及多人游戏状态。默认关闭。",
                        "打开开发者控制台", "关闭", "开启"),
                    ["zh-TW"] = CreateTexts("開發者控制台",
                        "解鎖遊戲內建的開發者控制台。指令可能修改存檔、解鎖內容、成就及多人遊戲狀態。預設關閉。",
                        "開啟開發者控制台", "關閉", "開啟"),
                    ["ko-KR"] = CreateTexts("개발자 콘솔", "게임에 내장된 개발자 콘솔을 엽니다. 명령어는 저장, 해금, 업적, 멀티플레이 상태를 변경할 수 있습니다. 기본적으로 꺼져 있습니다.", "개발자 콘솔 열기", "끄기", "켜기"),
                    ["ja-JP"] = CreateTexts("開発者コンソール", "ゲーム内蔵の開発者コンソールを有効にします。コマンドはセーブ、アンロック、実績、マルチプレイ状態を変更する場合があります。初期設定はオフです。", "開発者コンソールを開く", "オフ", "オン"),
                    ["de-DE"] = CreateTexts("Entwicklerkonsole",
                    "Schaltet die eingebaute Entwicklerkonsole frei. Befehle können Spielstände, Freischaltungen, Erfolge und Mehrspielerzustände verändern. Standardmäßig aus.",
                    "Entwicklerkonsole öffnen",
                    "Aus",
                    "Ein"),
                    ["es-ES"] = CreateTexts("Consola de desarrollo",
                    "Activa la consola de desarrollo del juego. Los comandos pueden modificar partidas guardadas, desbloqueos, logros y el estado multijugador. Desactivada por defecto.",
                    "Abrir consola de desarrollo",
                    "Desactivado",
                    "Activado"),
                    ["fr-FR"] = CreateTexts("Console de développement",
                    "Active la console intégrée au jeu. Ses commandes peuvent modifier les sauvegardes, déblocages, succès et l’état multijoueur. Désactivée par défaut.",
                    "Ouvrir la console de développement",
                    "Désactivé",
                    "Activé"),
                    ["it-IT"] = CreateTexts("Console sviluppatore",
                    "Abilita la console integrata nel gioco. I comandi possono modificare salvataggi, sblocchi, obiettivi e stato multigiocatore. Disattivata per impostazione predefinita.",
                    "Apri console sviluppatore",
                    "Disattivato",
                    "Attivato"),
                    ["pl-PL"] = CreateTexts("Konsola deweloperska",
                    "Odblokowuje konsolę wbudowaną w grę. Polecenia mogą zmieniać zapisy, odblokowania, osiągnięcia i stan rozgrywki wieloosobowej. Domyślnie wyłączona.",
                    "Otwórz konsolę deweloperską",
                    "Wył.",
                    "Wł."),
                    ["pt-BR"] = CreateTexts("Console de desenvolvimento",
                    "Ativa o console integrado ao jogo. Os comandos podem alterar salvamentos, desbloqueios, conquistas e o estado multijogador. Desativado por padrão.",
                    "Abrir console de desenvolvimento",
                    "Desativado",
                    "Ativado"),
                    ["ru-RU"] = CreateTexts("Консоль разработчика",
                    "Открывает доступ к встроенной консоли. Команды могут менять сохранения, разблокировки, достижения и состояние сетевой игры. По умолчанию отключена.",
                    "Открыть консоль разработчика",
                    "Выкл.",
                    "Вкл."),
                    ["sv-SE"] = CreateTexts("Utvecklarkonsol", "Aktiverar spelets inbyggda utvecklarkonsol. Kommandon kan ändra sparfiler, upplåsningar, prestationer och flerspelartillstånd. Av som standard.", "Öppna utvecklarkonsolen", "Av", "På"),
                    ["th-TH"] = CreateTexts("คอนโซลผู้พัฒนา", "เปิดใช้คอนโซลผู้พัฒนาที่มีในเกม คำสั่งอาจเปลี่ยนข้อมูลบันทึก สิ่งที่ปลดล็อก ความสำเร็จ และสถานะผู้เล่นหลายคน ปิดไว้ตามค่าเริ่มต้น", "เปิดคอนโซลผู้พัฒนา", "ปิด", "เปิด"),
                    ["tr-TR"] = CreateTexts("Geliştirici konsolu",
                    "Oyunun yerleşik geliştirici konsolunu açar. Komutlar kayıtları, açılan içerikleri, başarımları ve çok oyunculu durumu değiştirebilir. Varsayılan olarak kapalıdır.",
                    "Geliştirici konsolunu aç",
                    "Kapalı",
                    "Açık")
                };

        private static Dictionary<string, string> CreateTexts(string label, string help,
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

        internal static void Register(Action<string, string, string> addText, IEnumerable<string> languages)
        {
            LocalizationGroup.Register(addText, languages, DeveloperConsoleTexts);
        }
    }
}
