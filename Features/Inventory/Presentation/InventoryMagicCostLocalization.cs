using System;
using System.Collections.Generic;
using SephiriaEnhancements.Configuration;

namespace SephiriaEnhancements.Inventory
{
    internal static class InventoryMagicCostLocalization
    {
        private const string Prefix = "SephiriaEnhancements.InventoryMagicCost.";
        internal const string KeepCost = Prefix + "KeepCost", AllowCost = Prefix + "AllowCost";
        internal const string Help = Prefix + "Help", Unavailable = Prefix + "Unavailable", Change = Prefix + "Change";
        private static readonly string[] Keys = { KeepCost, AllowCost, Help, Unavailable, Change };
        private static readonly Dictionary<string, string[]> Texts = new()
        {
            ["en-US"] = new[] { "Do not increase MP cost", "Allow higher MP cost", "Controls MP increases from automatic artifact upgrades. Manually chosen levels take precedence.", "No artifacts here need more MP when upgraded.", "{0}: change option" },
            ["zh-CN"] = new[] { "不增加耗蓝", "允许增加耗蓝", "控制自动提升神器等级时是否增加耗蓝。手动指定等级时，以指定等级为准。", "当前没有升级后更耗蓝的神器。", "{0}：切换选项" },
            ["zh-TW"] = new[] { "不增加耗魔", "允許增加耗魔", "控制自動提升神器等級時是否增加耗魔。手動指定等級時，以指定等級為準。", "目前沒有升級後更耗魔的神器。", "{0}：切換選項" },
            ["ja-JP"] = new[] { "MP消費を増やさない", "MP消費の増加を許可", "自動レベルアップによるMP消費増加を許可するか選びます。手動指定のレベルを優先します。", "レベルアップでMP消費が増えるアーティファクトはありません。", "{0}：設定を切り替え" },
            ["ko-KR"] = new[] { "MP 소모 유지", "MP 소모 증가 허용", "자동 레벨 상승 시 MP 소모 증가를 허용할지 선택합니다. 직접 지정한 레벨이 우선합니다.", "레벨 상승 시 MP 소모가 늘어나는 아티팩트가 없습니다.", "{0}: 설정 변경" },
            ["de-DE"] = new[] { "MP-Kosten nicht erhöhen", "Höhere MP-Kosten erlauben", "Bestimmt, ob automatische Artefaktaufwertungen mehr MP kosten dürfen. Manuell gewählte Stufen haben Vorrang.", "Keine Artefakte mit höheren MP-Kosten bei Aufwertung vorhanden.", "{0}: Option wechseln" },
            ["es-ES"] = new[] { "No aumentar el gasto de PM", "Permitir gastar más PM", "Permite o evita gastar más PM al subir artefactos de nivel automáticamente. Los niveles elegidos manualmente tienen prioridad.", "No hay artefactos que gasten más PM al subir de nivel.", "{0}: cambiar opción" },
            ["fr-FR"] = new[] { "Ne pas dépenser plus de PM", "Autoriser plus de PM", "Autorise ou évite un surcoût en PM lors des améliorations automatiques. Les niveaux choisis manuellement sont prioritaires.", "Aucun artefact ne coûte plus de PM après amélioration.", "{0} : changer le réglage" },
            ["it-IT"] = new[] { "Non aumentare il costo PM", "Consenti più spesa di PM", "Consente o evita costi PM maggiori con gli aumenti automatici di livello. I livelli scelti manualmente hanno priorità.", "Nessun artefatto richiede più PM salendo di livello.", "{0}: cambia opzione" },
            ["pl-PL"] = new[] { "Nie zwiększaj kosztu PM", "Zezwól na wyższy koszt PM", "Określa, czy automatyczne ulepszanie artefaktów może zwiększyć koszt PM. Ręcznie wybrane poziomy mają pierwszeństwo.", "Brak artefaktów wymagających więcej PM po ulepszeniu.", "{0}: zmień opcję" },
            ["pt-BR"] = new[] { "Não aumentar gasto de PM", "Permitir gastar mais PM", "Permite ou evita gastar mais PM ao elevar níveis automaticamente. Níveis escolhidos manualmente têm prioridade.", "Nenhum artefato exige mais PM ao subir de nível.", "{0}: alterar opção" },
            ["ru-RU"] = new[] { "Не увеличивать расход маны", "Разрешить больший расход маны", "Определяет, допустим ли рост затрат маны при автоматическом повышении уровня. Уровни, заданные вручную, важнее.", "Нет артефактов, которым нужно больше маны после повышения уровня.", "{0}: сменить настройку" },
            ["sv-SE"] = new[] { "Öka inte MP-kostnaden", "Tillåt högre MP-kostnad", "Styr om automatisk nivåhöjning får öka MP-kostnaden. Manuellt valda nivåer har företräde.", "Inga artefakter här kostar mer MP när nivån höjs.", "{0}: byt alternativ" },
            ["th-TH"] = new[] { "ไม่เพิ่มการใช้ MP", "อนุญาตให้ใช้ MP เพิ่ม", "เลือกว่าจะเพิ่มการใช้ MP เมื่อเพิ่มระดับอาร์ติแฟกต์อัตโนมัติหรือไม่ โดยให้ความสำคัญกับระดับที่กำหนดเองก่อน", "ไม่มีอาร์ติแฟกต์ที่ใช้ MP เพิ่มเมื่อเพิ่มระดับ", "{0}: เปลี่ยนตัวเลือก" },
            ["tr-TR"] = new[] { "MP maliyetini artırma", "Daha çok MP harcamaya izin ver", "Otomatik seviye artışlarının MP maliyetini artırıp artıramayacağını belirler. Elle seçilen seviyeler önceliklidir.", "Seviye atladığında daha çok MP harcayan eser yok.", "{0}: seçeneği değiştir" },
        };

        internal static void Register(Action<string, string, string> addText) =>
            LocalizationGroup.Register(addText, LocalizationLanguages.All, Keys, Texts);
    }
}
