using System;
using System.Collections.Generic;
using SephiriaEnhancements.Configuration;

namespace SephiriaEnhancements.DefeatRetry
{
    internal static class DefeatRetryAvailabilityLocalization
    {
        internal const string PlayersNotReady = "SephiriaEnhancements.DefeatRetry.PlayersNotReady";
        internal const string NoFloorEntry = "SephiriaEnhancements.DefeatRetry.NoFloorEntry";
        internal const string PartyChanged = "SephiriaEnhancements.DefeatRetry.PartyChanged";
        internal const string Preparing = "SephiriaEnhancements.DefeatRetry.Preparing";
        private static readonly Dictionary<string, string[]> Texts = new Dictionary<string, string[]>
        {
            ["en-US"] = new[] { "Retry: player Mods not ready", "Retry unavailable: floor start not saved", "Retry unavailable: party or floor changed", "Retry: preparing" },
            ["zh-CN"] = new[] { "重试：玩家 Mod 未就绪", "无法重试：本层没有可恢复的起点", "无法重试：队伍或楼层已变化", "重试：正在准备" },
            ["zh-TW"] = new[] { "重試：玩家 Mod 未就緒", "無法重試：本層沒有可恢復的起點", "無法重試：隊伍或樓層已變更", "重試：準備中" },
            ["ko-KR"] = new[] { "재시도: 플레이어 모드 준비 안 됨", "재시도 불가: 층 시작 상태 없음", "재시도 불가: 파티 또는 층 변경됨", "재시도: 준비 중" },
            ["ja-JP"] = new[] { "再挑戦：参加者のMod準備待ち", "再挑戦不可：この階の開始状態がありません", "再挑戦不可：パーティーか階が変わりました", "再挑戦：準備中" },
            ["de-DE"] = new[] { "Neustart: Spieler-Mods nicht bereit", "Neustart nicht möglich: kein Etagenstart gespeichert", "Neustart nicht möglich: Gruppe oder Etage geändert", "Neustart wird vorbereitet" },
            ["es-ES"] = new[] { "Reintento: mods de jugadores no listos", "No se puede reintentar: sin estado inicial del piso", "No se puede reintentar: grupo o piso cambiado", "Reintento: preparando" },
            ["fr-FR"] = new[] { "Réessai : mods des joueurs non prêts", "Réessai impossible : début d’étage indisponible", "Réessai impossible : groupe ou étage modifié", "Réessai : préparation" },
            ["it-IT"] = new[] { "Riprova: mod dei giocatori non pronte", "Riprova non disponibile: manca lo stato iniziale del piano", "Riprova non disponibile: gruppo o piano cambiato", "Riprova: preparazione" },
            ["pt-BR"] = new[] { "Tentar de novo: mods dos jogadores não prontos", "Não é possível tentar de novo: início do andar indisponível", "Não é possível tentar de novo: grupo ou andar mudou", "Nova tentativa: preparando" },
            ["pl-PL"] = new[] { "Ponów: mody graczy niegotowe", "Nie można ponowić: brak stanu z początku piętra", "Nie można ponowić: zmieniono drużynę lub piętro", "Ponowienie: przygotowanie" },
            ["ru-RU"] = new[] { "Повтор: моды игроков не готовы", "Повтор недоступен: нет состояния начала этажа", "Повтор недоступен: группа или этаж изменились", "Повтор: подготовка" },
            ["tr-TR"] = new[] { "Tekrar: oyuncu modları hazır değil", "Tekrar kullanılamıyor: kat başlangıcı kaydedilmemiş", "Tekrar kullanılamıyor: grup veya kat değişti", "Tekrar: hazırlanıyor" },
            ["sv-SE"] = new[] { "Försök igen: spelarnas moddar är inte redo", "Kan inte försöka igen: våningens start saknas", "Kan inte försöka igen: gruppen eller våningen har ändrats", "Nytt försök: förbereder" },
            ["th-TH"] = new[] { "ลองใหม่: ม็อดของผู้เล่นยังไม่พร้อม", "ลองใหม่ไม่ได้: ไม่มีสถานะตอนเริ่มชั้นนี้", "ลองใหม่ไม่ได้: กลุ่มหรือชั้นเปลี่ยนไปแล้ว", "ลองใหม่: กำลังเตรียม" }
        };

        internal static void Register(Action<string, string, string> addText, IEnumerable<string> languages) =>
            LocalizationGroup.Register(addText, languages,
                new[] { PlayersNotReady, NoFloorEntry, PartyChanged, Preparing }, Texts);
    }
}
