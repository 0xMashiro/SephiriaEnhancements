using System;
using System.Collections.Generic;

namespace SephiriaEnhancements.EffectStats
{
    internal static class EffectStatsLocalization
    {
        internal const string Title = "SephiriaEnhancements.EffectStats.Title";
        internal const string Note = "SephiriaEnhancements.EffectStats.Note";
        internal const string SolarDamage = "SephiriaEnhancements.EffectStats.SolarDamage";
        private static readonly Dictionary<string, string[]> Texts = new()
        {
            ["en-US"] = new[] { "Additional effect bonuses", "Current build bonuses for supported effects. Zero bonuses are hidden; activation conditions still apply.", "Normal throw damage: {0}\nBefore critical hits and target defenses; excludes empowered throws and recall attacks." },
            ["zh-CN"] = new[] { "额外效果加成", "显示已支持效果的当前构筑加成，隐藏零值。各效果仍需满足触发条件。", "普通投掷伤害：{0}\n未计入暴击与目标防御，不含强化投掷及回收攻击。" },
            ["zh-TW"] = new[] { "額外效果加成", "顯示已支援效果的目前構築加成，隱藏零值。各效果仍須滿足觸發條件。", "普通投擲傷害：{0}\n未計入暴擊與目標防禦，不含強化投擲及回收攻擊。" },
            ["ko-KR"] = new[] { "추가 효과 보너스", "지원되는 효과의 현재 빌드 보너스를 표시합니다. 0은 숨기며, 발동 조건은 그대로 적용됩니다.", "일반 투척 피해: {0}\n치명타와 대상 방어 적용 전 수치입니다. 강화 투척과 회수 공격은 제외됩니다." },
            ["ja-JP"] = new[] { "追加効果ボーナス", "対応する効果の現在の構成によるボーナスを表示します。ゼロは非表示です。発動条件は引き続き適用されます。", "通常投擲ダメージ：{0}\nクリティカルと対象の防御を適用する前の値です。強化投擲と回収攻撃は含みません。" },
            ["de-DE"] = new[] { "Zusätzliche Effektboni", "Aktuelle Boni für unterstützte Effekte. Nullwerte werden ausgeblendet; Auslösebedingungen gelten weiterhin.", "Schaden beim normalen Wurf: {0}\nVor kritischen Treffern und Zielverteidigung; ohne verstärkte Würfe und Rückholangriffe." },
            ["es-ES"] = new[] { "Bonificaciones de efectos adicionales", "Bonificaciones actuales de los efectos compatibles. Se ocultan los valores cero; siguen aplicándose las condiciones de activación.", "Daño de lanzamiento normal: {0}\nAntes de críticos y defensas del objetivo; excluye lanzamientos potenciados y ataques de recuperación." },
            ["fr-FR"] = new[] { "Bonus d’effets supplémentaires", "Bonus actuels des effets pris en charge. Les valeurs nulles sont masquées ; les conditions d’activation restent nécessaires.", "Dégâts du lancer normal : {0}\nAvant les coups critiques et les défenses de la cible ; hors lancers renforcés et attaques de rappel." },
            ["it-IT"] = new[] { "Bonus degli effetti aggiuntivi", "Bonus attuali degli effetti supportati. I valori zero sono nascosti; restano valide le condizioni di attivazione.", "Danni del lancio normale: {0}\nPrima dei critici e delle difese del bersaglio; esclude lanci potenziati e attacchi di richiamo." },
            ["pl-PL"] = new[] { "Dodatkowe premie efektów", "Aktualne premie obsługiwanych efektów. Wartości zerowe są ukryte; warunki aktywacji nadal obowiązują.", "Obrażenia zwykłego rzutu: {0}\nPrzed trafieniami krytycznymi i obroną celu; bez wzmocnionych rzutów i ataków przy powrocie." },
            ["pt-BR"] = new[] { "Bônus de efeitos adicionais", "Bônus atuais dos efeitos compatíveis. Valores zero ficam ocultos; as condições de ativação continuam valendo.", "Dano do arremesso normal: {0}\nAntes de críticos e defesas do alvo; exclui arremessos fortalecidos e ataques de retorno." },
            ["ru-RU"] = new[] { "Дополнительные бонусы эффектов", "Текущие бонусы поддерживаемых эффектов. Нулевые значения скрыты; условия активации по-прежнему действуют.", "Урон обычного броска: {0}\nДо критических попаданий и защиты цели; без усиленных бросков и атак при возврате." },
            ["sv-SE"] = new[] { "Ytterligare effektbonusar", "Aktuella bonusar för effekter som stöds. Nollvärden döljs; aktiveringsvillkoren gäller fortfarande.", "Skada vid normalt kast: {0}\nFöre kritiska träffar och målets försvar; utan förstärkta kast och återkallningsattacker." },
            ["th-TH"] = new[] { "โบนัสเอฟเฟกต์เพิ่มเติม", "แสดงโบนัสปัจจุบันของเอฟเฟกต์ที่รองรับ โดยซ่อนค่าศูนย์ เงื่อนไขการทำงานยังคงมีผล", "ความเสียหายจากการขว้างปกติ: {0}\nก่อนคิดคริติคอลและการป้องกันของเป้าหมาย ไม่รวมการขว้างเสริมพลังและการโจมตีขณะเรียกกลับ" },
            ["tr-TR"] = new[] { "Ek etki bonusları", "Desteklenen etkilerin mevcut bonusları. Sıfır değerler gizlenir; etkinleşme koşulları geçerliliğini korur.", "Normal atış hasarı: {0}\nKritik vuruş ve hedef savunması öncesi; güçlendirilmiş atışlar ve geri çağırma saldırıları hariçtir." }
        };

        internal static void Register(Action<string, string, string> addText, IEnumerable<string> languages)
        {
            Configuration.LocalizationGroup.Register(addText, languages,
                new[] { Title, Note, SolarDamage }, Texts);
        }
    }
}
