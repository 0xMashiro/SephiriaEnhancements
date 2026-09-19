using System;
using System.Collections.Generic;
using SephiriaEnhancements.Configuration;

namespace SephiriaEnhancements.FixedExplorationSeed
{
    internal static class ExplorationSeedLocalization
    {
        internal const string Setting = "SephiriaEnhancements.FixedExplorationSeed.Setting";
        internal const string Help = "SephiriaEnhancements.FixedExplorationSeed.Help";
        internal const string Random = "SephiriaEnhancements.FixedExplorationSeed.Random";
        internal const string Confirm = "SephiriaEnhancements.FixedExplorationSeed.Confirm";
        internal const string Cancel = "SephiriaEnhancements.FixedExplorationSeed.Cancel";
        internal const string Clear = "SephiriaEnhancements.FixedExplorationSeed.Clear";
        internal const string InputHelp = "SephiriaEnhancements.FixedExplorationSeed.InputHelp";
        internal const string Invalid = "SephiriaEnhancements.FixedExplorationSeed.Invalid";
        internal const string HostOnly = "SephiriaEnhancements.FixedExplorationSeed.HostOnly";
        internal const string Unavailable = "SephiriaEnhancements.FixedExplorationSeed.Unavailable";
        internal const string Prompt = Setting;
        private static readonly string[] Keys = { Setting, Help, Random, Confirm, Cancel, Clear, InputHelp, Invalid, HostOnly, Unavailable };
        private static readonly Dictionary<string, string[]> Texts = new()
        {
            ["en-US"] = new[] { "Exploration seed", "Used for new games you host. Saved games and retries keep their seed. Progress, difficulty and party can change generated content.", "Random", "Confirm seed", "Cancel edit", "Clear seed", "Leave empty for random. Applies when starting over, not when departing town.", "Enter a whole number from -2147483648 to 2147483647, or leave empty.", "Set by host", "Editing is unavailable. Cancel to return." },
            ["zh-CN"] = new[] { "探索种子", "用于你主持的新一局。继续存档和战败重试保留原种子。进度、难度和队伍可能改变生成内容。", "随机", "确认种子", "取消编辑", "清空种子", "留空为随机。重新开始时生效，不会在离开城镇时更换。", "请输入 -2147483648 到 2147483647 的整数，或留空。", "由房主设置", "暂时无法编辑，请取消返回。" },
            ["zh-TW"] = new[] { "探索種子", "用於你主持的新一局。繼續存檔和戰敗重試保留原種子。進度、難度和隊伍可能改變生成內容。", "隨機", "確認種子", "取消編輯", "清空種子", "留空為隨機。重新開始時生效，不會在離開城鎮時更換。", "請輸入 -2147483648 到 2147483647 的整數，或留空。", "由房主設定", "暫時無法編輯，請取消返回。" },
            ["ja-JP"] = new[] { "探索シード", "自分がホストの新しいゲームに適用。セーブの続きと敗北後の再挑戦では元のシードを保持します。進行度、難易度、仲間によって生成内容は変わります。", "ランダム", "シードを確定", "編集をキャンセル", "シードを消去", "空欄ならランダム。最初からやり直すと適用されます。町からの出発時には変わりません。", "-2147483648～2147483647 の整数を入力するか、空欄にしてください。", "ホストが設定", "現在は編集できません。キャンセルで戻ってください。" },
            ["ko-KR"] = new[] { "탐험 시드", "자신이 호스트인 새 게임에 적용됩니다. 저장된 게임을 이어 하거나 패배 후 재도전하면 기존 시드를 유지합니다. 진행도, 난이도, 파티에 따라 생성 내용이 달라질 수 있습니다.", "무작위", "시드 확인", "편집 취소", "시드 지우기", "비워 두면 무작위입니다. 마을 출발이 아닌 처음부터 다시 시작할 때 적용됩니다.", "-2147483648에서 2147483647 사이의 정수를 입력하거나 비워 두세요.", "호스트가 설정", "지금은 편집할 수 없습니다. 취소하여 돌아가세요." },
            ["de-DE"] = new[] { "Erkundungsseed", "Gilt für neue Spiele, die du hostest. Spielstände und Wiederholungen nach Niederlagen behalten ihren Seed. Fortschritt, Schwierigkeit und Gruppe können die Inhalte ändern.", "Zufällig", "Seed bestätigen", "Bearbeitung abbrechen", "Seed leeren", "Leer lassen für Zufall. Gilt beim Neustart, nicht bei der Abreise aus der Stadt.", "Ganze Zahl von -2147483648 bis 2147483647 eingeben oder leer lassen.", "Vom Host festgelegt", "Bearbeitung nicht verfügbar. Mit Abbrechen zurück." },
            ["es-ES"] = new[] { "Semilla de exploración", "Se usa en las nuevas partidas que organizas. Las partidas guardadas y los reintentos conservan su semilla. El progreso, la dificultad y el grupo pueden cambiar el contenido.", "Aleatoria", "Confirmar semilla", "Cancelar edición", "Borrar semilla", "Deja vacío para elegir al azar. Se aplica al empezar de nuevo, no al salir del pueblo.", "Introduce un entero entre -2147483648 y 2147483647 o deja vacío.", "La elige el anfitrión", "No se puede editar ahora. Cancela para volver." },
            ["fr-FR"] = new[] { "Graine d’exploration", "Utilisée pour les nouvelles parties que vous hébergez. Sauvegardes et nouvelles tentatives conservent leur graine. Progression, difficulté et groupe peuvent changer le contenu.", "Aléatoire", "Confirmer la graine", "Annuler la saisie", "Effacer la graine", "Laissez vide pour un choix aléatoire. S’applique en recommençant, pas en quittant le village.", "Entrez un entier entre -2147483648 et 2147483647, ou laissez vide.", "Choisie par l’hôte", "Modification indisponible. Annulez pour revenir." },
            ["it-IT"] = new[] { "Seme esplorazione", "Usato nelle nuove partite che ospiti. Salvataggi e nuovi tentativi mantengono il seme. Progressi, difficoltà e gruppo possono cambiare i contenuti.", "Casuale", "Conferma seme", "Annulla modifica", "Cancella seme", "Lascia vuoto per un seme casuale. Si applica ricominciando, non lasciando il villaggio.", "Inserisci un intero tra -2147483648 e 2147483647 oppure lascia vuoto.", "Scelto dall’host", "Modifica non disponibile. Annulla per tornare indietro." },
            ["pl-PL"] = new[] { "Ziarno wyprawy", "Używane w nowych grach, których jesteś gospodarzem. Zapisy i ponowne próby zachowują ziarno. Postęp, trudność i drużyna mogą zmieniać zawartość.", "Losowe", "Zatwierdź ziarno", "Anuluj edycję", "Wyczyść ziarno", "Puste pole oznacza losowanie. Działa po rozpoczęciu od nowa, nie po wyjściu z miasta.", "Wpisz liczbę całkowitą od -2147483648 do 2147483647 lub zostaw puste pole.", "Ustawia gospodarz", "Edycja jest niedostępna. Anuluj, aby wrócić." },
            ["pt-BR"] = new[] { "Semente da exploração", "Usada em novas partidas que você hospeda. Jogos salvos e novas tentativas mantêm a semente. Progresso, dificuldade e grupo podem alterar o conteúdo.", "Aleatória", "Confirmar semente", "Cancelar edição", "Limpar semente", "Deixe vazio para sortear. Aplica-se ao recomeçar, não ao sair da vila.", "Insira um inteiro entre -2147483648 e 2147483647 ou deixe vazio.", "Definida pelo anfitrião", "Não é possível editar agora. Cancele para voltar." },
            ["ru-RU"] = new[] { "Зерно экспедиции", "Для новых игр, где вы — хозяин. Сохранения и повторные попытки сохраняют зерно. Прогресс, сложность и состав группы могут менять содержимое.", "Случайное", "Применить зерно", "Отменить ввод", "Очистить зерно", "Оставьте пустым для случайного выбора. Применяется при начале заново, а не при выходе из города.", "Введите целое число от -2147483648 до 2147483647 или оставьте поле пустым.", "Задаёт хозяин", "Изменение недоступно. Отмените ввод, чтобы вернуться." },
            ["sv-SE"] = new[] { "Utforskningsfrö", "Används i nya spel som du är värd för. Sparade spel och nya försök behåller sitt frö. Framsteg, svårighet och grupp kan ändra innehållet.", "Slumpmässigt", "Bekräfta frö", "Avbryt redigering", "Rensa frö", "Lämna tomt för slumpval. Gäller när du börjar om, inte när du lämnar staden.", "Ange ett heltal från -2147483648 till 2147483647 eller lämna tomt.", "Värden bestämmer", "Redigering är inte tillgänglig. Avbryt för att gå tillbaka." },
            ["th-TH"] = new[] { "ซีดการสำรวจ", "ใช้กับเกมใหม่ที่คุณเป็นโฮสต์ การเล่นต่อจากเซฟและลองใหม่หลังพ่ายแพ้จะใช้ซีดเดิม ความคืบหน้า ความยาก และทีมอาจเปลี่ยนเนื้อหาที่สร้างขึ้น", "สุ่ม", "ยืนยันซีด", "ยกเลิกการแก้ไข", "ล้างซีด", "เว้นว่างเพื่อสุ่ม มีผลเมื่อเริ่มใหม่ ไม่ใช่เมื่อออกจากเมือง", "ป้อนจำนวนเต็มตั้งแต่ -2147483648 ถึง 2147483647 หรือเว้นว่าง", "โฮสต์เป็นผู้กำหนด", "ไม่สามารถแก้ไขได้ในขณะนี้ ยกเลิกเพื่อกลับ" },
            ["tr-TR"] = new[] { "Keşif tohumu", "Ev sahipliği yaptığın yeni oyunlarda kullanılır. Kayıtlar ve yeniden denemeler mevcut tohumu korur. İlerleme, zorluk ve ekip içeriği değiştirebilir.", "Rastgele", "Tohumu onayla", "Düzenlemeyi iptal et", "Tohumu temizle", "Rastgele seçim için boş bırak. Kasabadan ayrılınca değil, baştan başlayınca uygulanır.", "-2147483648 ile 2147483647 arasında bir tam sayı gir veya boş bırak.", "Ev sahibi belirler", "Şu anda düzenlenemiyor. Geri dönmek için iptal et." },
        };

        internal static void Register(Action<string, string, string> addText, IEnumerable<string> languages) =>
            LocalizationGroup.Register(addText, languages, Keys, Texts);
    }
}
