using System;
using System.Collections.Generic;
using SephiriaEnhancements.Configuration;

namespace SephiriaEnhancements.DefeatRetry
{
    internal static class DefeatRetryAvailabilityLocalization
    {
        internal const string PlayersNotReady = "SephiriaEnhancements.DefeatRetry.PlayersNotReady";
        private static readonly Dictionary<string, string> Texts = new Dictionary<string, string>
        {
            ["en-US"] = "Retry: player Mods not ready",
            ["zh-CN"] = "重试：玩家 Mod 未就绪",
            ["zh-TW"] = "重試：玩家 Mod 未就緒",
            ["ko-KR"] = "재시도: 플레이어 모드 준비 안 됨",
            ["ja-JP"] = "再挑戦：参加者のMod準備待ち",
            ["de-DE"] = "Neustart: Spieler-Mods nicht bereit",
            ["es-ES"] = "Reintento: mods de jugadores no listos",
            ["fr-FR"] = "Réessai : mods des joueurs non prêts",
            ["it-IT"] = "Riprova: mod dei giocatori non pronte",
            ["pt-BR"] = "Tentar de novo: mods dos jogadores não prontos",
            ["pl-PL"] = "Ponów: mody graczy niegotowe",
            ["ru-RU"] = "Повтор: моды игроков не готовы",
            ["tr-TR"] = "Tekrar: oyuncu modları hazır değil",
            ["sv-SE"] = "Försök igen: spelarnas moddar är inte redo",
            ["th-TH"] = "ลองใหม่: ม็อดของผู้เล่นยังไม่พร้อม"
        };

        internal static void Register(Action<string, string, string> addText, IEnumerable<string> languages) =>
            LocalizationGroup.Register(addText, languages, PlayersNotReady, Texts);
    }
}
