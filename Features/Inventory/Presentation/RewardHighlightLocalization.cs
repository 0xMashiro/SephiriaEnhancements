using System;
using System.Collections.Generic;
using SephiriaEnhancements.Configuration;

namespace SephiriaEnhancements.Inventory
{
    internal static class RewardHighlightLocalization
    {
        internal const string Favorite = "SephiriaEnhancements.RewardHighlight.Favorite";
        internal const string Fruit = "SephiriaEnhancements.RewardHighlight.Fruit";
        private static readonly string[] Keys = { Favorite, Fruit };
        private static readonly Dictionary<string, string[]> Texts = new()
        {
            ["en-US"] = new[] { "Preferred artifact", "Fruit preference: {0} · {1}/{2} to next tier" },
            ["zh-CN"] = new[] { "偏好神器", "符合水果串偏好：{0} · 当前 {1}，下一档 {2}" },
            ["zh-TW"] = new[] { "偏好神器", "符合水果串偏好：{0} · 目前 {1}，下一階 {2}" },
            ["ja-JP"] = new[] { "お気に入りの神器", "フルーツの優先カテゴリ：{0} · 現在 {1}、次の段階 {2}" },
            ["ko-KR"] = new[] { "선호 유물", "과일 선호: {0} · 현재 {1}, 다음 단계 {2}" },
            ["de-DE"] = new[] { "Bevorzugtes Artefakt", "Fruchtvorliebe: {0} · {1}/{2} zur nächsten Stufe" },
            ["es-ES"] = new[] { "Artefacto preferido", "Preferencia de frutas: {0} · {1}/{2} para el siguiente nivel" },
            ["fr-FR"] = new[] { "Artéfact préféré", "Préférence des fruits : {0} · {1}/{2} avant le prochain palier" },
            ["it-IT"] = new[] { "Artefatto preferito", "Preferenza della frutta: {0} · {1}/{2} per la prossima soglia" },
            ["pl-PL"] = new[] { "Preferowany artefakt", "Preferencja owoców: {0} · {1}/{2} do następnego progu" },
            ["pt-BR"] = new[] { "Artefato preferido", "Preferência de frutas: {0} · {1}/{2} para o próximo patamar" },
            ["ru-RU"] = new[] { "Предпочитаемый артефакт", "Предпочтение фруктов: {0} · {1}/{2} до следующего порога" },
            ["sv-SE"] = new[] { "Föredragen artefakt", "Fruktpreferens: {0} · {1}/{2} till nästa nivå" },
            ["th-TH"] = new[] { "วัตถุโบราณที่ชื่นชอบ", "ความชอบผลไม้: {0} · ปัจจุบัน {1}, ขั้นถัดไป {2}" },
            ["tr-TR"] = new[] { "Tercih edilen eser", "Meyve tercihi: {0} · Sonraki aşamaya {1}/{2}" },
        };

        internal static void Register(Action<string, string, string> addText) =>
            LocalizationGroup.Register(addText, LocalizationLanguages.All, Keys, Texts);
    }
}
