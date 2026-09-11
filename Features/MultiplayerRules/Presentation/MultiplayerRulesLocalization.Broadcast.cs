using System.Collections.Generic;
namespace SephiriaEnhancements.MultiplayerRules.Presentation
{
    internal static partial class MultiplayerRulesLocalization
    {
        internal const string BroadcastApplied = "SephiriaEnhancements.MultiplayerRules.BroadcastApplied";
        internal const string BroadcastStarted = "SephiriaEnhancements.MultiplayerRules.BroadcastStarted";
        private static readonly string[] BroadcastKeys = { BroadcastApplied, BroadcastStarted };
        private static readonly Dictionary<string, string[]> BroadcastTexts = new()
        {
            ["en-US"] = new[] { "Host applied rules · {0} players", "Exploration rules in effect · {0} players" },
            ["zh-CN"] = new[] { "房主已应用规则 · {0} 人", "本次探索规则已生效 · {0} 人" },
            ["zh-TW"] = new[] { "房主已套用規則 · {0} 人", "本次探索規則已生效 · {0} 人" },
            ["ja-JP"] = new[] { "ホストがルールを適用 · {0} 人", "今回の探索ルール · {0} 人" },
            ["ko-KR"] = new[] { "호스트가 규칙 적용 · {0}명", "이번 탐험에 적용된 규칙 · {0}명" },
            ["de-DE"] = new[] { "Host hat Regeln angewendet · {0} Spieler", "Aktive Erkundungsregeln · {0} Spieler" },
            ["fr-FR"] = new[] { "Règles appliquées par l’hôte · {0} joueurs", "Règles actives de cette exploration · {0} joueurs" },
            ["es-ES"] = new[] { "Reglas aplicadas por el anfitrión · {0} jugadores", "Reglas activas de exploración · {0} jugadores" },
            ["it-IT"] = new[] { "Regole applicate dall’host · {0} giocatori", "Regole attive per l’esplorazione · {0} giocatori" },
            ["pt-BR"] = new[] { "Anfitrião aplicou regras · {0} jogadores", "Regras ativas da exploração · {0} jogadores" },
            ["pl-PL"] = new[] { "Gospodarz zastosował reguły · {0} graczy", "Aktywne reguły wyprawy · {0} graczy" },
            ["ru-RU"] = new[] { "Хост применил правила · {0} игроков", "Действующие правила исследования · {0} игроков" },
            ["sv-SE"] = new[] { "Värden har tillämpat regler · {0} spelare", "Aktiva utforskningsregler · {0} spelare" },
            ["tr-TR"] = new[] { "Ev sahibi kuralları uyguladı · {0} oyuncu", "Etkin keşif kuralları · {0} oyuncu" },
            ["th-TH"] = new[] { "โฮสต์ใช้กฎแล้ว · ผู้เล่น {0} คน", "กฎที่ใช้ในการสำรวจนี้ · ผู้เล่น {0} คน" },
        };
    }
}
