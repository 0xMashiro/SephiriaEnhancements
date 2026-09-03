#nullable enable
using System;
using System.Collections.Generic;

namespace SephiriaEnhancements.MultiplayerAccess.Presentation
{
    internal static class MultiplayerAccessLocalization
    {
        internal const string AllowJoinAndReconnectSetting =
            "SephiriaEnhancements.MultiplayerAccess.AllowMidRunJoinAndReconnect";
        internal const string AllowJoinAndReconnectHelp =
            AllowJoinAndReconnectSetting + ".Help";

        private static readonly Dictionary<string, string[]> Texts = new()
        {
            ["en-US"] = new[]
            {
                "Mid-run Join and Reconnect",
                "Host option, applied to the next run. Keeps the Steam room open during exploration, lets new players join with a new character and save slot, and enables the game's reconnect support. Joining players do not need this Mod. Detected multiplayer extensions retain ownership of admission."
            },
            ["zh-CN"] = new[]
            {
                "中途加入与重连",
                "房主选项，下次探索生效。探索期间保持 Steam 房间开放；新玩家以新角色和新存档槽加入；同时启用游戏的重连支持。加入方无需安装本 MOD。检测到联机扩展时，由该扩展管理玩家加入。"
            },
            ["zh-TW"] = new[]
            {
                "中途加入與重連",
                "房主選項，下次探索生效。探索期間保持 Steam 房間開放；新玩家以新角色和新存檔槽加入；同時啟用遊戲的重新連線支援。加入方不必安裝本 MOD。偵測到連線擴充套件時，由該擴充套件管理玩家加入。"
            },
            ["ko-KR"] = new[] { "탐험 중 참가 및 재접속", "호스트 설정이며 다음 탐험부터 적용됩니다. 탐험 중 Steam 방을 열어 두어 새 플레이어가 새 캐릭터와 저장 슬롯으로 참가할 수 있게 하고, 게임의 재접속 기능을 활성화합니다. 참가자는 이 Mod를 설치할 필요가 없습니다. 멀티플레이 확장 기능이 감지되면 참가 관리는 해당 확장 기능에 맡깁니다." },
            ["ja-JP"] = new[] { "探索途中の参加と再接続", "ホスト用の設定で、次の探索から適用されます。探索中も Steam ルームを開放し、新規プレイヤーが新しいキャラクターとセーブスロットで参加できるようにします。ゲームの再接続機能も有効にします。参加者にこの Mod は不要です。マルチプレイ拡張が検出された場合、参加の管理はその拡張に任せます。" },
            ["de-DE"] = new[] { "Beitritt und Wiederverbindung im Lauf",
                    "Host-Einstellung für den nächsten Lauf. Hält den Steam-Raum während der Erkundung offen, erlaubt neuen Spielern den Beitritt mit neuem Charakter und Speicherplatz und aktiviert die Wiederverbindungsfunktion des Spiels. Beitretende Spieler benötigen diesen Mod nicht. Erkannte Mehrspieler-Erweiterungen verwalten den Beitritt weiterhin selbst." },
            ["es-ES"] = new[] { "Unirse y reconectar durante la expedición",
                    "Opción del anfitrión para la próxima expedición. Mantiene abierta la sala de Steam, permite que nuevos jugadores se unan con un personaje y una ranura de guardado nuevos y activa la reconexión del juego. Quienes se unan no necesitan este Mod. Si se detectan extensiones multijugador, estas siguen gestionando el acceso." },
            ["fr-FR"] = new[] { "Rejoindre et se reconnecter en exploration",
                    "Option de l’hôte, appliquée à la prochaine exploration. Garde le salon Steam ouvert, permet aux nouveaux joueurs de rejoindre avec un nouveau personnage et emplacement de sauvegarde, et active la reconnexion du jeu. Les joueurs qui rejoignent n’ont pas besoin de ce Mod. Les extensions multijoueurs détectées conservent la gestion des accès." },
            ["it-IT"] = new[] { "Ingresso e riconnessione durante l’esplorazione",
                    "Opzione dell’host, valida dalla prossima esplorazione. Mantiene aperta la stanza Steam, permette ai nuovi giocatori di unirsi con un nuovo personaggio e slot di salvataggio e attiva la riconnessione del gioco. Chi si unisce non deve installare questo Mod. Le estensioni multigiocatore rilevate continuano a gestire gli accessi." },
            ["pl-PL"] = new[] { "Dołączanie i ponowne łączenie w trakcie wyprawy",
                    "Opcja gospodarza obowiązująca od następnej wyprawy. Pozostawia pokój Steam otwarty, pozwala nowym graczom dołączyć z nową postacią i miejscem zapisu oraz włącza ponowne łączenie obsługiwane przez grę. Dołączający nie potrzebują tego moda. Wykryte rozszerzenia wieloosobowe nadal zarządzają dostępem." },
            ["pt-BR"] = new[] { "Entrada e reconexão durante a exploração",
                    "Opção do anfitrião, aplicada à próxima exploração. Mantém a sala Steam aberta, permite a entrada de novos jogadores com um novo personagem e espaço de salvamento e ativa a reconexão do jogo. Quem entra não precisa deste Mod. Extensões multijogador detectadas continuam gerenciando a entrada." },
            ["ru-RU"] = new[] { "Вход и переподключение во время забега",
                    "Настройка хоста для следующего забега. Оставляет комнату Steam открытой, позволяет новым игрокам войти с новым персонажем и слотом сохранения и включает переподключение средствами игры. Входящим игрокам этот мод не нужен. При обнаружении сетевых расширений управление входом остаётся за ними." },
            ["sv-SE"] = new[] { "Anslut och återanslut under en runda",
                    "Värdinställning som gäller nästa runda. Håller Steam-rummet öppet under utforskning, låter nya spelare ansluta med en ny figur och sparplats och aktiverar spelets återanslutning. Anslutande spelare behöver inte denna mod. Upptäckta flerspelartillägg fortsätter att styra anslutningar." },
            ["th-TH"] = new[] { "เข้าร่วมและเชื่อมต่อใหม่ระหว่างการสำรวจ",
                    "ตัวเลือกของโฮสต์ มีผลในการสำรวจครั้งถัดไป เปิดห้อง Steam ไว้ระหว่างสำรวจ ให้ผู้เล่นใหม่เข้าร่วมด้วยตัวละครและช่องบันทึกใหม่ และเปิดใช้การเชื่อมต่อใหม่ของเกม ผู้เข้าร่วมไม่ต้องติดตั้ง Mod นี้ หากตรวจพบส่วนเสริมผู้เล่นหลายคน ส่วนเสริมนั้นจะยังจัดการการเข้าร่วมเอง" },
            ["tr-TR"] = new[] { "Keşif sırasında katılma ve yeniden bağlanma",
                    "Bir sonraki keşifte geçerli olan sunucu sahibi ayarıdır. Keşif sırasında Steam odasını açık tutar, yeni oyuncuların yeni karakter ve kayıt yuvasıyla katılmasını sağlar ve oyunun yeniden bağlanma özelliğini açar. Katılanların bu Modu kurması gerekmez. Algılanan çok oyunculu eklentiler katılımı yönetmeye devam eder." }
        };

        internal static void Register(Action<string, string, string> addText,
            IEnumerable<string> languages)
        {
            foreach (string language in languages)
            {
                string[] text = Texts.TryGetValue(language,
                    out string[]? localized) && localized != null
                    ? localized : Texts["en-US"];
                addText(language, AllowJoinAndReconnectSetting, text[0]);
                addText(language, AllowJoinAndReconnectHelp, text[1]);
            }
        }
    }
}
