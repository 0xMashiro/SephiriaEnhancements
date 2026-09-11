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
            ["en-US"] = new[] { "Confirm adds this value to the draft. Apply the draft before departure.", "{0}\nPlayers: {1} · Original player scaling: {2}\nRange: {3}–{4} · Step: {5}\nClear the input to restore game behavior.", "Varies with the encounter or other game conditions", "Standard bosses {0}; Kraz +0%" },
            ["zh-CN"] = new[] { "确认后只写入本次修改，出发前还需应用。", "{0}\n适用人数：{1} · 原版人数部分：{2}\n范围：{3}–{4} · 步长：{5}\n清空输入后确认，可恢复此项原版行为。", "随遭遇或其他游戏条件变化", "常规 Boss {0}；克拉兹 +0%" },
            ["zh-TW"] = new[] { "確認後僅寫入本次修改，出發前仍需套用。", "{0}\n適用人數：{1} · 原版人數部分：{2}\n範圍：{3}–{4} · 步長：{5}\n清空輸入後確認，可恢復此項原版行為。", "隨遭遇或其他遊戲條件變化", "一般 Boss {0}；克拉茲 +0%" },
            ["ja-JP"] = new[] { "決定で下書きに保存します。出発前に適用してください。", "{0}\n人数：{1} · 標準値：{2}\n範囲：{3}–{4} · 刻み：{5}\n空欄で決定すると標準に戻ります。", "遭遇やゲームの条件によって変化", "通常ボス {0}、クラーズ +0%" },
            ["ko-KR"] = new[] { "확인하면 초안에 저장됩니다. 출발 전에 적용하세요.", "{0}\n인원: {1} · 기본값: {2}\n범위: {3}–{4} · 간격: {5}\n입력을 비우고 확인하면 기본 동작으로 돌아갑니다.", "전투 또는 게임 조건에 따라 달라짐", "일반 보스 {0}, 크라즈 +0%" },
            ["de-DE"] = new[] { "Bestätigen speichert im Entwurf. Vor Abreise anwenden.", "{0}\nSpieler: {1} · Originale Spieleranpassung: {2}\nBereich: {3}–{4} · Schritt: {5}\nLeere Eingabe stellt das Spielverhalten wieder her.", "Abhängig von Begegnung und Spielbedingungen", "Normale Bosse {0}; Kraz +0%" },
            ["fr-FR"] = new[] { "Valider modifie le brouillon. Appliquez avant de partir.", "{0}\nJoueurs : {1} · Origine : {2}\nPlage : {3}–{4} · Pas : {5}\nValidez une saisie vide pour rétablir le jeu d’origine.", "Varie selon la rencontre et les conditions du jeu", "Boss habituels {0} ; Kraz +0%" },
            ["es-ES"] = new[] { "Confirmar guarda en el borrador. Aplica antes de salir.", "{0}\nJugadores: {1} · Ajuste original por jugadores: {2}\nRango: {3}–{4} · Paso: {5}\nConfirma el campo vacío para restaurar el comportamiento del juego.", "Varía según el encuentro y las condiciones del juego", "Jefes normales {0}; Kraz +0%" },
            ["it-IT"] = new[] { "Confermare aggiorna la bozza. Applica prima di partire.", "{0}\nGiocatori: {1} · Originale: {2}\nIntervallo: {3}–{4} · Passo: {5}\nConferma il campo vuoto per ripristinare il comportamento del gioco.", "Varia con lo scontro e le condizioni del gioco", "Boss normali {0}; Kraz +0%" },
            ["pt-BR"] = new[] { "Confirmar salva no rascunho. Aplique antes de partir.", "{0}\nJogadores: {1} · Ajuste original por jogadores: {2}\nIntervalo: {3}–{4} · Passo: {5}\nConfirme o campo vazio para restaurar o comportamento do jogo.", "Varia conforme o encontro e as condições do jogo", "Chefes comuns {0}; Kraz +0%" },
            ["pl-PL"] = new[] { "Zatwierdzenie zapisuje szkic. Zastosuj przed wyjściem.", "{0}\nGracze: {1} · Oryginał: {2}\nZakres: {3}–{4} · Krok: {5}\nZatwierdź puste pole, aby przywrócić działanie gry.", "Zależy od starcia i warunków gry", "Zwykli bossowie {0}; Kraz +0%" },
            ["ru-RU"] = new[] { "Подтверждение сохраняет в черновик. Примените до выхода.", "{0}\nИгроков: {1} · Оригинал: {2}\nДиапазон: {3}–{4} · Шаг: {5}\nПодтвердите пустое поле, чтобы вернуть поведение игры.", "Зависит от боя и условий игры", "Обычные боссы {0}; Краз +0%" },
            ["sv-SE"] = new[] { "Bekräfta sparar i utkastet. Tillämpa före avfärd.", "{0}\nSpelare: {1} · Ursprunglig spelaranpassning: {2}\nIntervall: {3}–{4} · Steg: {5}\nBekräfta ett tomt fält för att återställa spelets beteende.", "Varierar med mötet och spelets villkor", "Vanliga bossar {0}; Kraz +0%" },
            ["tr-TR"] = new[] { "Onaylamak taslağa kaydeder. Ayrılmadan önce uygulayın.", "{0}\nOyuncu: {1} · Özgün: {2}\nAralık: {3}–{4} · Adım: {5}\nOyun davranışını geri yüklemek için boş alanı onaylayın.", "Karşılaşmaya ve oyun koşullarına göre değişir", "Normal bosslar {0}; Kraz +0%" },
            ["th-TH"] = new[] { "ยืนยันจะบันทึกในแบบร่าง ต้องใช้ก่อนออกเดินทาง", "{0}\nผู้เล่น: {1} · ค่าเดิม: {2}\nช่วง: {3}–{4} · ขั้น: {5}\nเว้นว่างแล้วกดยืนยันเพื่อคืนค่าการทำงานเดิม", "ขึ้นอยู่กับการเผชิญหน้าและเงื่อนไขในเกม", "บอสทั่วไป {0}; คราซ +0%" },
        };
    }
}
