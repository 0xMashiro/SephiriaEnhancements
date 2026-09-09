using System;
using System.Collections.Generic;

namespace SephiriaEnhancements.Configuration
{
    internal static class SettingsInteractionLocalization
    {
        internal const string ValueWithStatus = "SephiriaEnhancements.SettingsInteraction.ValueWithStatus";
        internal const string HostOnly = "SephiriaEnhancements.SettingsInteraction.HostOnly";
        internal const string Exploration = "SephiriaEnhancements.SettingsInteraction.Exploration";
        internal const string ReconnectPending = "SephiriaEnhancements.SettingsInteraction.ReconnectPending";
        internal const string Connecting = "SephiriaEnhancements.SettingsInteraction.Connecting";
        internal const string Extension = "SephiriaEnhancements.SettingsInteraction.Extension";
        internal const string Unavailable = "SephiriaEnhancements.SettingsInteraction.Unavailable";
        internal const string ModDisabled = "SephiriaEnhancements.SettingsInteraction.ModDisabled";
        internal const string Connected = "SephiriaEnhancements.SettingsInteraction.Connected";
        internal static string Reason(SettingLockReason reason) => "SephiriaEnhancements.SettingsInteraction." + reason;
        private static readonly string[] Keys = { ValueWithStatus, HostOnly, Exploration, ReconnectPending, Connecting, Extension, Unavailable, ModDisabled, Connected };
        private static readonly Dictionary<string, string[]> Texts = new()
        {
            ["en-US"] = new[] { "{0}\n{1}", "Host only", "Locked during exploration", "Reconnect pending", "Connecting", "Managed by another Mod", "Unavailable", "Mod disabled", "Connected to host" },
            ["zh-CN"] = new[] { "{0}\n{1}", "仅房主", "探索中锁定", "等待重连", "正在连接", "其他 Mod 接管", "不可用", "Mod 已关闭", "已连接房主" },
            ["zh-TW"] = new[] { "{0}\n{1}", "僅房主", "探索中鎖定", "等待重連", "正在連線", "其他 Mod 接管", "無法使用", "Mod 已關閉", "已連接房主" },
            ["ko-KR"] = new[] { "{0}\n{1}", "호스트 전용", "탐험 중 잠김", "재접속 대기", "연결 중", "다른 Mod가 관리", "사용 불가", "Mod 꺼짐", "호스트에 연결됨" },
            ["ja-JP"] = new[] { "{0}\n{1}", "ホストのみ", "探索中は固定", "再接続待ち", "接続中", "他のModが管理", "利用不可", "Modはオフ", "ホストに接続中" },
            ["de-DE"] = new[] { "{0}\n{1}", "Nur Host", "In Erkundung gesperrt", "Wiederbeitritt ausstehend", "Verbindung läuft", "Andere Mod zuständig", "Nicht verfügbar", "Mod deaktiviert", "Mit Host verbunden" },
            ["es-ES"] = new[] { "{0}\n{1}", "Solo anfitrión", "Bloqueado en exploración", "Reconexión pendiente", "Conectando", "Lo gestiona otro Mod", "No disponible", "Mod desactivado", "Conectado al anfitrión" },
            ["fr-FR"] = new[] { "{0}\n{1}", "Hôte uniquement", "Verrouillé en exploration", "Reconnexion en attente", "Connexion en cours", "Géré par un autre Mod", "Indisponible", "Mod désactivé", "Connecté à l’hôte" },
            ["it-IT"] = new[] { "{0}\n{1}", "Solo host", "Bloccato in esplorazione", "Riconnessione in attesa", "Connessione in corso", "Gestito da un altro Mod", "Non disponibile", "Mod disattivato", "Connesso all’host" },
            ["pl-PL"] = new[] { "{0}\n{1}", "Tylko gospodarz", "Blokada w eksploracji", "Oczekiwanie na powrót", "Łączenie", "Zarządza inny mod", "Niedostępne", "Mod wyłączony", "Połączono z gospodarzem" },
            ["pt-BR"] = new[] { "{0}\n{1}", "Só anfitrião", "Bloqueado na exploração", "Reconexão pendente", "Conectando", "Outro Mod gerencia", "Indisponível", "Mod desativado", "Conectado ao anfitrião" },
            ["ru-RU"] = new[] { "{0}\n{1}", "Только хост", "В походе заблокировано", "Ожидание переподключения", "Подключение", "Управляет другой мод", "Недоступно", "Мод отключён", "Подключено к хосту" },
            ["sv-SE"] = new[] { "{0}\n{1}", "Endast värd", "Låst under utforskning", "Återanslutning väntar", "Ansluter", "Annan mod styr", "Ej tillgängligt", "Mod avstängd", "Ansluten till värd" },
            ["th-TH"] = new[] { "{0}\n{1}", "เฉพาะโฮสต์", "ล็อกระหว่างสำรวจ", "รอเชื่อมต่อใหม่", "กำลังเชื่อมต่อ", "Mod อื่นจัดการอยู่", "ใช้งานไม่ได้", "Mod ปิดอยู่", "เชื่อมต่อกับโฮสต์อยู่" },
            ["tr-TR"] = new[] { "{0}\n{1}", "Yalnızca ev sahibi", "Keşifte kilitli", "Yeniden bağlantı bekliyor", "Bağlanıyor", "Başka Mod yönetiyor", "Kullanılamıyor", "Mod kapalı", "Ev sahibine bağlı" },
        };
        internal static void Register(Action<string, string, string> addText, IEnumerable<string> languages) =>
            LocalizationGroup.Register(addText, languages, Keys, Texts);
    }
}
