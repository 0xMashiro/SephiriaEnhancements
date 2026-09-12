using System;
using System.Collections.Generic;
using SephiriaEnhancements.Configuration;

namespace SephiriaEnhancements.Inventory
{
    internal static class InventoryMagicCostLocalization
    {
        private const string Prefix = "SephiriaEnhancements.InventoryMagicCost.";
        internal const string KeepCost = Prefix + "KeepCost", AllowCost = Prefix + "AllowCost";
        private static readonly string[] Keys = { KeepCost, AllowCost };
        private static readonly Dictionary<string, string[]> Texts = new()
        {
            ["en-US"] = new[] { "Do not increase MP cost", "Allow higher MP cost" },
            ["zh-CN"] = new[] { "不增加耗蓝", "允许增加耗蓝" },
            ["zh-TW"] = new[] { "不增加耗魔", "允許增加耗魔" },
            ["ja-JP"] = new[] { "MP消費を増やさない", "MP消費の増加を許可" },
            ["ko-KR"] = new[] { "MP 소모 유지", "MP 소모 증가 허용" },
            ["de-DE"] = new[] { "MP-Kosten nicht erhöhen", "Höhere MP-Kosten erlauben" },
            ["es-ES"] = new[] { "No aumentar el gasto de PM", "Permitir gastar más PM" },
            ["fr-FR"] = new[] { "Ne pas dépenser plus de PM", "Autoriser plus de PM" },
            ["it-IT"] = new[] { "Non aumentare il costo PM", "Consenti più spesa di PM" },
            ["pl-PL"] = new[] { "Nie zwiększaj kosztu PM", "Zezwól na wyższy koszt PM" },
            ["pt-BR"] = new[] { "Não aumentar gasto de PM", "Permitir gastar mais PM" },
            ["ru-RU"] = new[] { "Не увеличивать расход маны", "Разрешить больший расход маны" },
            ["sv-SE"] = new[] { "Öka inte MP-kostnaden", "Tillåt högre MP-kostnad" },
            ["th-TH"] = new[] { "ไม่เพิ่มการใช้ MP", "อนุญาตให้ใช้ MP เพิ่ม" },
            ["tr-TR"] = new[] { "MP maliyetini artırma", "Daha çok MP harcamaya izin ver" },
        };

        internal static void Register(Action<string, string, string> addText) =>
            LocalizationGroup.Register(addText, LocalizationLanguages.All, Keys, Texts);
    }
}
