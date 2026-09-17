using System;
using System.Collections.Generic;
using SephiriaEnhancements.Configuration;

namespace SephiriaEnhancements.Inventory
{
    internal static class InventoryPresetComboLocalization
    {
        private const string Prefix = "SephiriaEnhancements.InventoryPresetCombo.";
        internal const string Enabled = Prefix + "Enabled", Disabled = Prefix + "Disabled";
        internal const string Help = Prefix + "Help", Unavailable = Prefix + "Unavailable";
        private static readonly string[] Keys = { Enabled, Disabled, Help, Unavailable };
        private static readonly Dictionary<string, string[]> Texts = new()
        {
            ["en-US"] = new[] { "Combo priority: On", "Combo priority: Off", "Favor preset combo counts, even if artifact penalties worsen. Off: avoid stronger penalties. Manual goals and MP cost settings take precedence. Saved for future explorations.", "No automatic preset combo preferences." },
            ["zh-CN"] = new[] { "连击优先：开启", "连击优先：关闭", "优先增加预设偏好的连击数，允许加重神器负面属性。关闭后优先避免加重负面属性。手动目标与耗蓝设置优先；设置保留到下次探索。", "当前没有自动采用的预设连击偏好。" },
            ["zh-TW"] = new[] { "連擊優先：開啟", "連擊優先：關閉", "優先增加預設偏好的連擊數，允許加重神器負面屬性。關閉後優先避免加重負面屬性。手動目標與耗魔設定優先；設定保留至下次探索。", "目前沒有自動採用的預設連擊偏好。" },
            ["ja-JP"] = new[] { "コンボ優先：オン", "コンボ優先：オフ", "プリセットで好むコンボ数を優先し、アーティファクトの不利効果増加を許可します。オフでは不利効果の増加を避けます。手動目標とMP設定が優先され、次の探索にも保存されます。", "自動適用するプリセットのコンボ優先設定がありません。" },
            ["ko-KR"] = new[] { "콤보 우선: 켜짐", "콤보 우선: 꺼짐", "프리셋의 선호 콤보 수를 우선하며 아티팩트 불이익 증가를 허용합니다. 끄면 불이익 증가를 피합니다. 수동 목표와 MP 설정이 우선하며 다음 탐험에도 저장됩니다.", "자동 적용할 프리셋 콤보 선호가 없습니다." },
            ["de-DE"] = new[] { "Kombo-Vorrang: An", "Kombo-Vorrang: Aus", "Bevorzugt Preset-Kombos, auch bei stärkeren Artefaktnachteilen. Aus: stärkere Nachteile vermeiden. Manuelle Ziele und MP-Einstellungen gehen vor. Bleibt für weitere Erkundungen gespeichert.", "Keine automatisch verwendeten Preset-Kombovorlieben." },
            ["es-ES"] = new[] { "Prioridad de combos: Sí", "Prioridad de combos: No", "Prioriza los combos preferidos del preajuste aunque aumenten las penalizaciones. Desactivado: evita empeorarlas. Mandan las metas manuales y el gasto de PM. Se guarda para futuras exploraciones.", "No hay preferencias automáticas de combos del preajuste." },
            ["fr-FR"] = new[] { "Priorité combos : Oui", "Priorité combos : Non", "Favorise les combos du préréglage, même avec des malus accrus. Désactivé : évite ces malus. Objectifs manuels et réglage des PM prioritaires. Conservé pour les explorations suivantes.", "Aucune préférence automatique de combo du préréglage." },
            ["it-IT"] = new[] { "Priorità combo: Sì", "Priorità combo: No", "Favorisce le combo del preset anche con penalità maggiori. Disattivo: evita di peggiorarle. Obiettivi manuali e impostazioni PM hanno precedenza. Salvato per le esplorazioni future.", "Nessuna preferenza combo automatica del preset." },
            ["pl-PL"] = new[] { "Priorytet kombinacji: Wł.", "Priorytet kombinacji: Wył.", "Preferuje kombinacje zestawu nawet kosztem silniejszych kar artefaktów. Po wyłączeniu unika zwiększania kar. Cele ręczne i ustawienia PM mają pierwszeństwo. Zachowane na kolejne wyprawy.", "Brak automatycznych preferencji kombinacji zestawu." },
            ["pt-BR"] = new[] { "Prioridade de combos: Sim", "Prioridade de combos: Não", "Prioriza combos preferidos da configuração, mesmo com penalidades maiores. Desativado: evita aumentá-las. Metas manuais e opção de PM têm prioridade. Salvo para próximas explorações.", "Nenhuma preferência automática de combo na configuração." },
            ["ru-RU"] = new[] { "Приоритет комбинаций: Вкл.", "Приоритет комбинаций: Выкл.", "Предпочитает комбинации набора даже ценой усиления штрафов. При отключении избегает усиления штрафов. Ручные цели и настройки маны важнее. Сохраняется для следующих вылазок.", "Нет автоматически применяемых предпочтений комбинаций набора." },
            ["sv-SE"] = new[] { "Komboprioritet: På", "Komboprioritet: Av", "Prioriterar förvalets kombon även om artefakternas nackdelar ökar. Av: undvik större nackdelar. Manuella mål och MP-inställningar går före. Sparas till nästa utforskning.", "Inga automatiska kombopreferenser från förvalet." },
            ["th-TH"] = new[] { "เน้นคอมโบ: เปิด", "เน้นคอมโบ: ปิด", "เน้นจำนวนคอมโบที่พรีเซ็ตต้องการ แม้ผลเสียของอาร์ติแฟกต์จะเพิ่มขึ้น เมื่อปิดจะเลี่ยงผลเสียที่เพิ่มขึ้น เป้าหมายที่ตั้งเองและการใช้ MP สำคัญกว่า บันทึกไว้สำหรับการสำรวจครั้งต่อไป", "ไม่มีความชอบคอมโบจากพรีเซ็ตที่ใช้โดยอัตโนมัติ" },
            ["tr-TR"] = new[] { "Kombo önceliği: Açık", "Kombo önceliği: Kapalı", "Eser cezaları artsa bile hazır ayarın tercih ettiği komboları artırır. Kapalıyken cezaları artırmaz. Elle seçilen hedefler ve MP ayarı önceliklidir. Sonraki keşifler için kaydedilir.", "Otomatik uygulanan hazır ayar kombo tercihi yok." },
        };

        internal static void Register(Action<string, string, string> addText) =>
            LocalizationGroup.Register(addText, LocalizationLanguages.All, Keys, Texts);
    }
}
