using System.Collections.Generic;
namespace SephiriaEnhancements.MultiplayerRules.Presentation
{
    internal static partial class MultiplayerRulesLocalization
    {
        internal const string BroadcastSaved = "SephiriaEnhancements.MultiplayerRules.BroadcastSaved";
        internal const string BroadcastStarted = "SephiriaEnhancements.MultiplayerRules.BroadcastStarted";
        internal const string BroadcastTeamChanged = "SephiriaEnhancements.MultiplayerRules.BroadcastTeamChanged";
        internal const string BroadcastSummary = "SephiriaEnhancements.MultiplayerRules.BroadcastSummary";
        private static readonly string[] BroadcastKeys = { BroadcastSaved, BroadcastStarted, BroadcastTeamChanged, BroadcastSummary };
        private static readonly Dictionary<string, string[]> BroadcastTexts = new()
        {
            ["en-US"] = new[] { "Host saved {1} changes · {0} players.", "Exploration started · {0} players · {1} custom rules.", "Team now has {0} players · {1} custom rules for later spawns and rewards.", "Current team: {0} players · {1} custom rules." },
            ["zh-CN"] = new[] { "房主已保存 {1} 项修改 · 当前 {0} 人。", "探索开始 · {0} 人 · {1} 项自定义规则。", "队伍现有 {0} 人 · {1} 项自定义规则，后续生成和奖励按此人数使用。", "当前 {0} 人 · {1} 项自定义规则。" },
            ["zh-TW"] = new[] { "房主已儲存 {1} 項修改 · 目前 {0} 人。", "探索開始 · {0} 人 · {1} 項自訂規則。", "隊伍現有 {0} 人 · {1} 項自訂規則，後續生成和獎勵按此人數使用。", "目前 {0} 人 · {1} 項自訂規則。" },
            ["ja-JP"] = new[] { "ホストが{1}件の変更を保存・現在{0}人。", "探索開始・{0}人・カスタムルール{1}件。", "チームが{0}人に変更・以後の出現と報酬にカスタムルール{1}件。", "現在{0}人・カスタムルール{1}件。" },
            ["ko-KR"] = new[] { "호스트가 변경 {1}개 저장 · 현재 {0}명.", "탐험 시작 · {0}명 · 사용자 규칙 {1}개.", "현재 {0}명 · 이후 생성과 보상에 사용자 규칙 {1}개 적용.", "현재 {0}명 · 사용자 규칙 {1}개." },
            ["de-DE"] = new[] { "Host speichert {1} Änderungen · {0} Spieler.", "Erkundung beginnt · {0} Spieler · {1} eigene Regeln.", "Team jetzt: {0} Spieler · {1} eigene Regeln für spätere Gegner und Belohnungen.", "Aktuell {0} Spieler · {1} eigene Regeln." },
            ["fr-FR"] = new[] { "Hôte : {1} modifications enregistrées · {0} joueurs.", "Départ · {0} joueurs · {1} règles personnalisées.", "Équipe : {0} joueurs · {1} règles personnalisées pour les apparitions et récompenses suivantes.", "Équipe : {0} joueurs · {1} règles personnalisées." },
            ["es-ES"] = new[] { "Anfitrión: {1} cambios guardados · {0} jugadores.", "Exploración iniciada · {0} jugadores · {1} reglas propias.", "Equipo de {0} jugadores · {1} reglas propias para próximas apariciones y recompensas.", "Equipo actual: {0} jugadores · {1} reglas propias." },
            ["it-IT"] = new[] { "Host: {1} modifiche salvate · {0} giocatori.", "Esplorazione iniziata · {0} giocatori · {1} regole proprie.", "Squadra di {0} giocatori · {1} regole proprie per apparizioni e ricompense successive.", "Ora {0} giocatori · {1} regole proprie." },
            ["pt-BR"] = new[] { "Anfitrião salvou {1} alterações · {0} jogadores.", "Exploração iniciada · {0} jogadores · {1} regras próprias.", "Equipe com {0} jogadores · {1} regras próprias para próximas aparições e recompensas.", "Agora {0} jogadores · {1} regras próprias." },
            ["pl-PL"] = new[] { "Gospodarz zapisał {1} zmian · {0} graczy.", "Początek wyprawy · {0} graczy · {1} własnych reguł.", "Teraz {0} graczy · {1} własnych reguł dla kolejnych wrogów i nagród.", "Obecnie {0} graczy · {1} własnych reguł." },
            ["ru-RU"] = new[] { "Хост сохранил {1} правок · {0} игроков.", "Исследование началось · {0} игроков · {1} своих правил.", "В команде {0} игроков · {1} своих правил для последующих появлений и наград.", "Сейчас {0} игроков · {1} своих правил." },
            ["sv-SE"] = new[] { "Värden sparade {1} ändringar · {0} spelare.", "Utforskningen börjar · {0} spelare · {1} egna regler.", "Nu {0} spelare · {1} egna regler för senare fiender och belöningar.", "Nu {0} spelare · {1} egna regler." },
            ["tr-TR"] = new[] { "Ev sahibi {1} değişiklik kaydetti · {0} oyuncu.", "Keşif başladı · {0} oyuncu · {1} özel kural.", "Takımda {0} oyuncu · sonraki düşmanlar ve ödüller için {1} özel kural.", "Şu an {0} oyuncu · {1} özel kural." },
            ["th-TH"] = new[] { "โฮสต์บันทึกการแก้ไข {1} รายการ · {0} คน", "เริ่มสำรวจ · {0} คน · กฎกำหนดเอง {1} ข้อ", "ทีมมี {0} คน · กฎกำหนดเอง {1} ข้อสำหรับศัตรูและรางวัลครั้งต่อไป", "ปัจจุบัน {0} คน · กฎกำหนดเอง {1} ข้อ" },
        };
    }
}
