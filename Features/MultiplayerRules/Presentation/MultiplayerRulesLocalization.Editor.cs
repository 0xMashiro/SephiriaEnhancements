using System.Collections.Generic;

namespace SephiriaEnhancements.MultiplayerRules.Presentation
{
    internal static partial class MultiplayerRulesLocalization
    {
        internal const string EditorHint = "SephiriaEnhancements.MultiplayerRules.EditorHint";
        internal const string EditorPrompt = "SephiriaEnhancements.MultiplayerRules.EditorPrompt";
        internal const string VariableReference = "SephiriaEnhancements.MultiplayerRules.VariableReference";
        internal const string BossReference = "SephiriaEnhancements.MultiplayerRules.BossReference";
        private static readonly string[] EditorKeys = { EditorHint, EditorPrompt, VariableReference, BossReference };
        private static readonly Dictionary<string, string[]> EditorTexts = new()
        {
            ["en-US"] = new[] { "Confirm keeps this edit. Save from Team rules before departure.", "{0} · {1} players\nOriginal: {2}\nRange: {3}–{4} · Step: {5}", "Varies with the encounter or other game conditions", "Standard bosses {0}; Kraz +0%" },
            ["zh-CN"] = new[] { "确认后保留本次修改，出发前需在队伍规则中保存。", "{0} · {1} 人\n原版：{2}\n范围：{3}–{4} · 步长：{5}", "随遭遇或其他游戏条件变化", "常规 Boss {0}；克拉兹 +0%" },
            ["zh-TW"] = new[] { "確認後保留本次修改，出發前需在隊伍規則中儲存。", "{0} · {1} 人\n原版：{2}\n範圍：{3}–{4} · 步長：{5}", "隨遭遇或其他遊戲條件變化", "一般 Boss {0}；克拉茲 +0%" },
            ["ja-JP"] = new[] { "確定でこの変更を保持します。出発前にチームルールで保存してください。", "{0}・{1}人\n元の値：{2}\n範囲：{3}–{4}・刻み：{5}", "遭遇やゲームの条件によって変化", "通常ボス {0}、クラーズ +0%" },
            ["ko-KR"] = new[] { "확인하면 이 변경을 유지합니다. 출발 전에 팀 규칙에서 저장하세요.", "{0} · {1}명\n원본: {2}\n범위: {3}–{4} · 간격: {5}", "전투 또는 게임 조건에 따라 달라짐", "일반 보스 {0}, 크라즈 +0%" },
            ["de-DE"] = new[] { "Bestätigen übernimmt diese Eingabe. Vor der Abreise in den Teamregeln speichern.", "{0} · {1} Spieler\nOriginal: {2}\nBereich: {3}–{4} · Schritt: {5}", "Abhängig von Begegnung und Spielbedingungen", "Normale Bosse {0}; Kraz +0%" },
            ["fr-FR"] = new[] { "Confirmer garde cette saisie. Enregistrez dans les règles d’équipe avant le départ.", "{0} · {1} joueurs\nOriginal : {2}\nPlage : {3}–{4} · Pas : {5}", "Varie selon la rencontre et les conditions du jeu", "Boss habituels {0} ; Kraz +0%" },
            ["es-ES"] = new[] { "Confirmar conserva este cambio. Guarda en Reglas del equipo antes de salir.", "{0} · {1} jugadores\nOriginal: {2}\nRango: {3}–{4} · Paso: {5}", "Varía según el encuentro y las condiciones del juego", "Jefes normales {0}; Kraz +0%" },
            ["it-IT"] = new[] { "Confermare mantiene la modifica. Salva nelle regole della squadra prima di partire.", "{0} · {1} giocatori\nOriginale: {2}\nIntervallo: {3}–{4} · Passo: {5}", "Varia con lo scontro e le condizioni del gioco", "Boss normali {0}; Kraz +0%" },
            ["pt-BR"] = new[] { "Confirmar mantém esta alteração. Salve nas regras da equipe antes de partir.", "{0} · {1} jogadores\nOriginal: {2}\nIntervalo: {3}–{4} · Passo: {5}", "Varia conforme o encontro e as condições do jogo", "Chefes comuns {0}; Kraz +0%" },
            ["pl-PL"] = new[] { "Zatwierdzenie zachowuje tę zmianę. Przed wyruszeniem zapisz reguły drużyny.", "{0} · {1} graczy\nOryginał: {2}\nZakres: {3}–{4} · Krok: {5}", "Zależy od starcia i warunków gry", "Zwykli bossowie {0}; Kraz +0%" },
            ["ru-RU"] = new[] { "Подтверждение оставляет изменение. Перед выходом сохраните правила команды.", "{0} · {1} игроков\nОригинал: {2}\nДиапазон: {3}–{4} · Шаг: {5}", "Зависит от боя и условий игры", "Обычные боссы {0}; Краз +0%" },
            ["sv-SE"] = new[] { "Bekräfta behåller ändringen. Spara i lagreglerna före avfärd.", "{0} · {1} spelare\nOriginal: {2}\nIntervall: {3}–{4} · Steg: {5}", "Varierar med mötet och spelets villkor", "Vanliga bossar {0}; Kraz +0%" },
            ["tr-TR"] = new[] { "Onaylamak bu değişikliği tutar. Ayrılmadan önce takım kurallarından kaydedin.", "{0} · {1} oyuncu\nÖzgün: {2}\nAralık: {3}–{4} · Adım: {5}", "Karşılaşmaya ve oyun koşullarına göre değişir", "Normal bosslar {0}; Kraz +0%" },
            ["th-TH"] = new[] { "ยืนยันเพื่อเก็บการแก้ไขนี้ แล้วบันทึกในกฎของทีมก่อนออกเดินทาง", "{0} · {1} คน\nค่าเดิม: {2}\nช่วง: {3}–{4} · ขั้น: {5}", "ขึ้นอยู่กับการเผชิญหน้าและเงื่อนไขในเกม", "บอสทั่วไป {0}; คราซ +0%" },
        };
    }
}
