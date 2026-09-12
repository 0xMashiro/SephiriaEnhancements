using System;
using System.Collections.Generic;
using SephiriaEnhancements.Configuration;

namespace SephiriaEnhancements.Inventory
{
    internal static class InventoryPresetIntentLocalization
    {
        internal const string Help = "SephiriaEnhancements.InventoryPresetIntent.Help";
        internal const string Changed = "SephiriaEnhancements.InventoryPresetIntent.Changed";
        private static readonly string[] Keys = { Help, Changed };
        private static readonly Dictionary<string, string[]> Texts = new()
        {
            ["en-US"] = new[] { "Higher ↑ means higher priority. Uses current favorites and fruit settings to maximize combo counts in priority order. Manual goals come first.", "Arrangement preferences changed. Arrangement stopped." },
            ["zh-CN"] = new[] { "↑ 越大越优先。按当前收藏和水果设置，依次尽量补足高优先级类别的连击数；手动目标优先。", "整理偏好已改变，整理已停止。" },
            ["zh-TW"] = new[] { "↑ 越大越優先。依目前收藏與水果設定，依序盡量補足高優先級類別的連擊數；手動目標優先。", "整理偏好已變更，整理已停止。" },
            ["ko-KR"] = new[] { "↑가 클수록 우선합니다. 현재 즐겨찾기와 과일 설정에 따라 우선순위 순으로 콤보 수를 최대한 높입니다. 수동 목표가 우선합니다.", "정리 선호 설정이 바뀌어 정리를 중단했습니다." },
            ["ja-JP"] = new[] { "↑が大きいほど優先します。現在のお気に入りとフルーツ設定に従い、優先順にコンボ数をできるだけ増やします。手動の目標を優先します。", "整理の優先設定が変わったため、整理を停止しました。" },
            ["de-DE"] = new[] { "Höheres ↑ hat Vorrang. Aktuelle Favoriten und Früchte bestimmen die Reihenfolge zum Maximieren der Kombos. Manuelle Ziele gehen vor.", "Sortiervorlieben geändert. Sortierung gestoppt." },
            ["es-ES"] = new[] { "Un ↑ mayor tiene prioridad. Usa favoritos y frutas actuales para maximizar combos por orden de prioridad. Priman las metas manuales.", "Cambiaron las preferencias. Ordenación detenida." },
            ["fr-FR"] = new[] { "Un ↑ élevé est prioritaire. Favoris et fruits actuels guident la maximisation des combos, par priorité. Vos objectifs manuels priment.", "Préférences modifiées. Rangement arrêté." },
            ["it-IT"] = new[] { "Un ↑ maggiore ha priorità. Preferiti e frutta attuali guidano la massimizzazione delle combo in ordine. Gli obiettivi manuali prevalgono.", "Preferenze cambiate. Riordino interrotto." },
            ["pl-PL"] = new[] { "Wyższe ↑ oznacza wyższy priorytet. Bieżące ulubione i owoce wyznaczają kolejność maksymalizacji kombinacji. Cele ręczne są ważniejsze.", "Preferencje zmienione. Porządkowanie zatrzymane." },
            ["pt-BR"] = new[] { "↑ maior indica maior prioridade. Usa favoritos e frutas atuais para maximizar combos por prioridade. Metas manuais vêm primeiro.", "Preferências alteradas. Organização interrompida." },
            ["ru-RU"] = new[] { "Чем выше ↑, тем выше приоритет. Текущие избранное и фрукты задают порядок увеличения комбинаций до максимума. Ручные цели важнее.", "Настройки предпочтений изменились. Сортировка остановлена." },
            ["sv-SE"] = new[] { "Högre ↑ har högre prioritet. Aktuella favoriter och frukter styr ordningen för att maximera kombon. Manuella mål går först.", "Preferenserna ändrades. Sorteringen stoppades." },
            ["th-TH"] = new[] { "↑ ยิ่งมากยิ่งสำคัญ ใช้รายการโปรดและผลไม้ปัจจุบันเพื่อเพิ่มจำนวนคอมโบให้มากที่สุดตามลำดับความสำคัญ เป้าหมายที่กำหนดเองมาก่อน", "การตั้งค่าความชอบเปลี่ยนไป หยุดจัดเรียงแล้ว" },
            ["tr-TR"] = new[] { "↑ büyüdükçe öncelik artar. Güncel favori ve meyve ayarlarıyla kombo sayısını öncelik sırasına göre artırır. Elle seçilen hedefler önce gelir.", "Düzenleme tercihleri değişti. Düzenleme durduruldu." },
        };

        internal static void Register(Action<string, string, string> addText) =>
            LocalizationGroup.Register(addText, LocalizationLanguages.All, Keys, Texts);
    }
}
