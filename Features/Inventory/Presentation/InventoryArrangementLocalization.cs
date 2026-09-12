using System;
using System.Collections.Generic;

namespace SephiriaEnhancements.Inventory
{
    internal static class InventoryArrangementLocalization
    {
        internal const string ArtifactPriorities = "SephiriaEnhancements.InventoryArrangement.ArtifactPriorities";
        internal const string ComboPriorities = "SephiriaEnhancements.InventoryArrangement.ComboPriorities";
        internal const string BackToArrangement = "SephiriaEnhancements.InventoryArrangement.BackToArrangement";
        internal const string Undo = "SephiriaEnhancements.InventoryArrangement.Undo";
        internal const string Undone = "SephiriaEnhancements.InventoryArrangement.Undone";
        internal const string ClearArtifactPriorities = "SephiriaEnhancements.InventoryArrangement.ClearArtifactPriorities";
        private static readonly string[] Keys = { ArtifactPriorities, BackToArrangement, Undo, Undone, ComboPriorities, ClearArtifactPriorities };
        private static readonly Dictionary<string, string[]> Texts = new()
        {
            ["en-US"] = new[] { "Artifact priorities", "Back to arrangement", "Undo arrangement", "Arrangement undone.", "Combo priorities", "Clear" },
            ["zh-CN"] = new[] { "神器优先级", "返回整理", "撤销整理", "已撤销整理。", "连招优先级", "清空" },
            ["zh-TW"] = new[] { "神器優先順序", "返回整理", "復原整理", "已復原整理。", "連招優先順序", "清空" },
            ["ko-KR"] = new[] { "아티팩트 우선순위", "정리로 돌아가기", "정리 되돌리기", "정리를 되돌렸습니다.", "콤보 우선순위", "비우기" },
            ["ja-JP"] = new[] { "アーティファクト優先順位", "整理に戻る", "整理を元に戻す", "整理を元に戻しました。", "コンボ優先順位", "クリア" },
            ["de-DE"] = new[] { "Artefaktprioritäten", "Zurück zur Sortierung", "Sortierung rückgängig", "Sortierung rückgängig gemacht.", "Komboprioritäten", "Leeren" },
            ["es-ES"] = new[] { "Prioridad de artefactos", "Volver a ordenar", "Deshacer ordenación", "Ordenación deshecha.", "Prioridad de combos", "Vaciar" },
            ["fr-FR"] = new[] { "Priorités des artefacts", "Retour au rangement", "Annuler le rangement", "Rangement annulé.", "Priorités des combos", "Vider" },
            ["it-IT"] = new[] { "Priorità artefatti", "Torna al riordino", "Annulla riordino", "Riordino annullato.", "Priorità combo", "Svuota" },
            ["pl-PL"] = new[] { "Priorytety artefaktów", "Wróć do porządkowania", "Cofnij porządkowanie", "Cofnięto porządkowanie.", "Priorytety kombinacji", "Wyczyść" },
            ["pt-BR"] = new[] { "Prioridade de artefatos", "Voltar à organização", "Desfazer organização", "Organização desfeita.", "Prioridade de combos", "Limpar" },
            ["ru-RU"] = new[] { "Приоритет артефактов", "Назад к сортировке", "Отменить сортировку", "Сортировка отменена.", "Приоритет комбинаций", "Очистить" },
            ["sv-SE"] = new[] { "Artefaktprioritering", "Tillbaka till sortering", "Ångra sortering", "Sorteringen ångrades.", "Komboprioritering", "Rensa" },
            ["th-TH"] = new[] { "ลำดับความสำคัญอาร์ติแฟกต์", "กลับไปหน้าจัดเรียง", "เลิกทำการจัด", "เลิกทำการจัดแล้ว", "ลำดับความสำคัญคอมโบ", "ล้าง" },
            ["tr-TR"] = new[] { "Eser öncelikleri", "Düzenlemeye dön", "Düzenlemeyi geri al", "Düzenleme geri alındı.", "Kombo öncelikleri", "Temizle" },
        };

        internal static void Register(Action<string, string, string> addText)
        {
            Configuration.LocalizationGroup.Register(addText, Configuration.LocalizationLanguages.All, Keys, Texts);
        }
    }
}
