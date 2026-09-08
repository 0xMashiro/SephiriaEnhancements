using System;
using System.Collections.Generic;

namespace SephiriaEnhancements.ResourceBarValues
{
    internal static class ResourceBarValueLocalization
    {
        internal static string Setting(ResourceBarValueSetting setting) =>
            "SephiriaEnhancements.ResourceBarValues.Setting." + setting;
        internal static string Help(ResourceBarValueSetting setting) =>
            "SephiriaEnhancements.ResourceBarValues.Help." + setting;
        internal const string Off = "SephiriaEnhancements.ResourceBarValues.Off";
        internal const string On = "SephiriaEnhancements.ResourceBarValues.On";

        private static readonly Dictionary<string, string[]> Texts = new Dictionary<string, string[]>
        {
            ["en-US"] = new[] {
                "Ordinary creature health numbers", "Show health and shield numbers on ordinary creature bars. Off by default.",
                "Ordinary creature super armor numbers", "Show current / maximum super armor beside ordinary creature bars when the native super armor bar is visible. Off by default.",
                "Miniboss health numbers", "Show health and shield numbers on miniboss bars. On by default.",
                "Miniboss super armor numbers", "Show current / maximum super armor beside miniboss bars when the native super armor bar is visible. On by default.",
                "Boss health numbers", "Show health and shield numbers on boss bars, including separate parts. On by default.",
                "Destructible prop health numbers", "Show health numbers on existing destructible prop bars, including combat totems. Off by default.",
                "Off", "On" },
            ["zh-CN"] = new[] {
                "普通生物生命数值", "显示普通生物血条的生命与护盾数值。默认关闭。",
                "普通生物超级护甲数值", "当原有超级护甲条可见时，在普通生物血条旁显示超级护甲当前值 / 上限。默认关闭。",
                "小 BOSS 生命数值", "显示小 BOSS 血条的生命与护盾数值。默认开启。",
                "小 BOSS 超级护甲数值", "当原有超级护甲条可见时，在小 BOSS 血条旁显示超级护甲当前值 / 上限。默认开启。",
                "BOSS 生命数值", "显示 BOSS 血条的生命与护盾数值，包含独立部位。默认开启。",
                "可破坏物生命数值", "显示可破坏物已有血条的生命数值，包含战斗图腾。默认关闭。",
                "关闭", "开启" },
            ["zh-TW"] = new[] {
                "普通生物生命數值", "顯示普通生物血條的生命與護盾數值。預設關閉。",
                "普通生物超級護甲數值", "當原有超級護甲條可見時，在普通生物血條旁顯示超級護甲目前值 / 上限。預設關閉。",
                "小 BOSS 生命數值", "顯示小 BOSS 血條的生命與護盾數值。預設開啟。",
                "小 BOSS 超級護甲數值", "當原有超級護甲條可見時，在小 BOSS 血條旁顯示超級護甲目前值 / 上限。預設開啟。",
                "BOSS 生命數值", "顯示 BOSS 血條的生命與護盾數值，包含獨立部位。預設開啟。",
                "可破壞物生命數值", "顯示可破壞物已有血條的生命數值，包含戰鬥圖騰。預設關閉。",
                "關閉", "開啟" },
            ["ko-KR"] = new[] {
                "일반 생물 체력 수치", "일반 생물의 체력 바에 체력과 보호막 수치를 표시합니다. 기본값은 꺼짐입니다.",
                "일반 생물 슈퍼 아머 수치", "기존 슈퍼 아머 바가 보일 때 일반 생물 바 옆에 현재 / 최대 슈퍼 아머를 표시합니다. 기본값은 꺼짐입니다.",
                "미니보스 체력 수치", "미니보스의 체력 바에 체력과 보호막 수치를 표시합니다. 기본값은 켜짐입니다.",
                "미니보스 슈퍼 아머 수치", "기존 슈퍼 아머 바가 보일 때 미니보스 바 옆에 현재 / 최대 슈퍼 아머를 표시합니다. 기본값은 켜짐입니다.",
                "보스 체력 수치", "개별 부위를 포함한 보스의 체력 바에 체력과 보호막 수치를 표시합니다. 기본값은 켜짐입니다.",
                "파괴 가능 오브젝트 체력 수치", "전투 토템을 포함한 파괴 가능 오브젝트의 기존 체력 바에 체력 수치를 표시합니다. 기본값은 꺼짐입니다.",
                "꺼짐", "켜짐" },
            ["ja-JP"] = new[] {
                "通常の生物のHP数値", "通常の生物のHPバーにHPとシールドの数値を表示します。初期設定はオフです。",
                "通常の生物のスーパーアーマー数値", "元のスーパーアーマーバーが表示されているとき、通常の生物のバーの横に現在値 / 最大値を表示します。初期設定はオフです。",
                "ミニボスのHP数値", "ミニボスのHPバーにHPとシールドの数値を表示します。初期設定はオンです。",
                "ミニボスのスーパーアーマー数値", "元のスーパーアーマーバーが表示されているとき、ミニボスのバーの横に現在値 / 最大値を表示します。初期設定はオンです。",
                "ボスのHP数値", "独立した部位を含むボスのHPバーにHPとシールドの数値を表示します。初期設定はオンです。",
                "破壊可能オブジェクトのHP数値", "戦闘トーテムを含む破壊可能オブジェクトの既存のHPバーにHP数値を表示します。初期設定はオフです。",
                "オフ", "オン" },
            ["de-DE"] = new[] {
                "Lebenspunkte normaler Kreaturen", "Zeigt Lebens- und Schildwerte an den Leisten normaler Kreaturen. Standardmäßig aus.",
                "Superrüstungswerte normaler Kreaturen", "Zeigt aktuelle / maximale Superrüstung neben normalen Kreaturen, wenn die ursprüngliche Superrüstungsleiste sichtbar ist. Standardmäßig aus.",
                "Lebenspunkte von Minibossen", "Zeigt Lebens- und Schildwerte an Minibossleisten. Standardmäßig an.",
                "Superrüstungswerte von Minibossen", "Zeigt aktuelle / maximale Superrüstung neben Minibossleisten, wenn die ursprüngliche Superrüstungsleiste sichtbar ist. Standardmäßig an.",
                "Lebenspunkte von Bossen", "Zeigt Lebens- und Schildwerte an Bossleisten, einschließlich einzelner Körperteile. Standardmäßig an.",
                "Lebenspunkte zerstörbarer Objekte", "Zeigt Lebenswerte an vorhandenen Leisten zerstörbarer Objekte, einschließlich Kampftotems. Standardmäßig aus.",
                "Aus", "An" },
            ["es-ES"] = new[] {
                "Vida numérica de criaturas comunes", "Muestra la vida y el escudo en las barras de criaturas comunes. Desactivado por defecto.",
                "Superarmadura de criaturas comunes", "Muestra la superarmadura actual / máxima junto a las barras de criaturas comunes cuando su barra original es visible. Desactivado por defecto.",
                "Vida numérica de minijefes", "Muestra la vida y el escudo en las barras de minijefes. Activado por defecto.",
                "Superarmadura numérica de minijefes", "Muestra la superarmadura actual / máxima junto a las barras de minijefes cuando su barra original es visible. Activado por defecto.",
                "Vida numérica de jefes", "Muestra la vida y el escudo en las barras de jefes, incluidas sus partes independientes. Activado por defecto.",
                "Vida de objetos destructibles", "Muestra la vida en las barras existentes de objetos destructibles, incluidos tótems de combate. Desactivado por defecto.",
                "Desactivado", "Activado" },
            ["fr-FR"] = new[] {
                "PV chiffrés des créatures ordinaires", "Affiche les PV et le bouclier sur les barres des créatures ordinaires. Désactivé par défaut.",
                "Super armure des créatures ordinaires", "Affiche la super armure actuelle / maximale près des barres des créatures ordinaires lorsque sa barre d’origine est visible. Désactivé par défaut.",
                "PV chiffrés des mini-boss", "Affiche les PV et le bouclier sur les barres des mini-boss. Activé par défaut.",
                "Super armure chiffrée des mini-boss", "Affiche la super armure actuelle / maximale près des barres des mini-boss lorsque sa barre d’origine est visible. Activé par défaut.",
                "PV chiffrés des boss", "Affiche les PV et le bouclier sur les barres des boss, y compris leurs parties distinctes. Activé par défaut.",
                "PV des objets destructibles", "Affiche les PV sur les barres existantes des objets destructibles, y compris les totems de combat. Désactivé par défaut.",
                "Désactivé", "Activé" },
            ["it-IT"] = new[] {
                "Vita numerica delle creature comuni", "Mostra vita e scudo sulle barre delle creature comuni. Disattivato per impostazione predefinita.",
                "Super armatura delle creature comuni", "Mostra la super armatura attuale / massima accanto alle barre delle creature comuni quando la barra originale è visibile. Disattivato per impostazione predefinita.",
                "Vita numerica dei miniboss", "Mostra vita e scudo sulle barre dei miniboss. Attivato per impostazione predefinita.",
                "Super armatura numerica dei miniboss", "Mostra la super armatura attuale / massima accanto alle barre dei miniboss quando la barra originale è visibile. Attivato per impostazione predefinita.",
                "Vita numerica dei boss", "Mostra vita e scudo sulle barre dei boss, incluse le parti separate. Attivato per impostazione predefinita.",
                "Vita degli oggetti distruttibili", "Mostra la vita sulle barre esistenti degli oggetti distruttibili, inclusi i totem da combattimento. Disattivato per impostazione predefinita.",
                "Disattivato", "Attivato" },
            ["pl-PL"] = new[] {
                "Liczbowe zdrowie zwykłych stworzeń", "Pokazuje zdrowie i tarczę na paskach zwykłych stworzeń. Domyślnie wyłączone.",
                "Superpancerz zwykłych stworzeń", "Pokazuje bieżący / maksymalny superpancerz obok pasków zwykłych stworzeń, gdy widoczny jest jego oryginalny pasek. Domyślnie wyłączone.",
                "Liczbowe zdrowie minibossów", "Pokazuje zdrowie i tarczę na paskach minibossów. Domyślnie włączone.",
                "Liczbowy superpancerz minibossów", "Pokazuje bieżący / maksymalny superpancerz obok pasków minibossów, gdy widoczny jest jego oryginalny pasek. Domyślnie włączone.",
                "Liczbowe zdrowie bossów", "Pokazuje zdrowie i tarczę na paskach bossów, w tym oddzielnych części. Domyślnie włączone.",
                "Zdrowie zniszczalnych obiektów", "Pokazuje zdrowie na istniejących paskach zniszczalnych obiektów, w tym totemów bojowych. Domyślnie wyłączone.",
                "Wyłączone", "Włączone" },
            ["pt-BR"] = new[] {
                "Vida numérica de criaturas comuns", "Exibe vida e escudo nas barras de criaturas comuns. Desativado por padrão.",
                "Superarmadura de criaturas comuns", "Exibe a superarmadura atual / máxima ao lado das barras de criaturas comuns quando a barra original está visível. Desativado por padrão.",
                "Vida numérica de minichefes", "Exibe vida e escudo nas barras de minichefes. Ativado por padrão.",
                "Superarmadura numérica de minichefes", "Exibe a superarmadura atual / máxima ao lado das barras de minichefes quando a barra original está visível. Ativado por padrão.",
                "Vida numérica de chefes", "Exibe vida e escudo nas barras de chefes, incluindo partes separadas. Ativado por padrão.",
                "Vida de objetos destrutíveis", "Exibe a vida nas barras existentes de objetos destrutíveis, incluindo totens de combate. Desativado por padrão.",
                "Desativado", "Ativado" },
            ["ru-RU"] = new[] {
                "Числа здоровья обычных существ", "Показывает здоровье и щит на полосках обычных существ. По умолчанию выключено.",
                "Числа суперброни обычных существ", "Показывает текущую / максимальную суперброню рядом с полосками обычных существ, когда видна исходная полоска суперброни. По умолчанию выключено.",
                "Числа здоровья мини-боссов", "Показывает здоровье и щит на полосках мини-боссов. По умолчанию включено.",
                "Числа суперброни мини-боссов", "Показывает текущую / максимальную суперброню рядом с полосками мини-боссов, когда видна исходная полоска суперброни. По умолчанию включено.",
                "Числа здоровья боссов", "Показывает здоровье и щит на полосках боссов, включая отдельные части. По умолчанию включено.",
                "Здоровье разрушаемых объектов", "Показывает здоровье на существующих полосках разрушаемых объектов, включая боевые тотемы. По умолчанию выключено.",
                "Выключено", "Включено" },
            ["sv-SE"] = new[] {
                "Hälsotal för vanliga varelser", "Visar hälsa och sköld på vanliga varelsers mätare. Av som standard.",
                "Superpansartal för vanliga varelser", "Visar aktuellt / maximalt superpansar vid vanliga varelsers mätare när den ursprungliga superpansarmätaren syns. Av som standard.",
                "Hälsotal för minibossar", "Visar hälsa och sköld på minibossars mätare. På som standard.",
                "Superpansartal för minibossar", "Visar aktuellt / maximalt superpansar vid minibossars mätare när den ursprungliga superpansarmätaren syns. På som standard.",
                "Hälsotal för bossar", "Visar hälsa och sköld på bossars mätare, inklusive separata delar. På som standard.",
                "Hälsotal för förstörbara föremål", "Visar hälsa på befintliga mätare för förstörbara föremål, inklusive stridstotem. Av som standard.",
                "Av", "På" },
            ["th-TH"] = new[] {
                "ตัวเลขพลังชีวิตของสิ่งมีชีวิตทั่วไป", "แสดงตัวเลขพลังชีวิตและโล่บนแถบของสิ่งมีชีวิตทั่วไป ปิดไว้เป็นค่าเริ่มต้น",
                "ตัวเลขซูเปอร์อาร์เมอร์ของสิ่งมีชีวิตทั่วไป", "แสดงซูเปอร์อาร์เมอร์ปัจจุบัน / สูงสุดข้างแถบของสิ่งมีชีวิตทั่วไปเมื่อแถบซูเปอร์อาร์เมอร์เดิมปรากฏ ปิดไว้เป็นค่าเริ่มต้น",
                "ตัวเลขพลังชีวิตของมินิบอส", "แสดงตัวเลขพลังชีวิตและโล่บนแถบของมินิบอส เปิดไว้เป็นค่าเริ่มต้น",
                "ตัวเลขซูเปอร์อาร์เมอร์ของมินิบอส", "แสดงซูเปอร์อาร์เมอร์ปัจจุบัน / สูงสุดข้างแถบของมินิบอสเมื่อแถบซูเปอร์อาร์เมอร์เดิมปรากฏ เปิดไว้เป็นค่าเริ่มต้น",
                "ตัวเลขพลังชีวิตของบอส", "แสดงตัวเลขพลังชีวิตและโล่บนแถบของบอส รวมถึงชิ้นส่วนแยก เปิดไว้เป็นค่าเริ่มต้น",
                "ตัวเลขพลังชีวิตของวัตถุที่ทำลายได้", "แสดงตัวเลขพลังชีวิตบนแถบเดิมของวัตถุที่ทำลายได้ รวมถึงโทเท็มต่อสู้ ปิดไว้เป็นค่าเริ่มต้น",
                "ปิด", "เปิด" },
            ["tr-TR"] = new[] {
                "Normal yaratıkların can sayıları", "Normal yaratık çubuklarında can ve kalkan sayılarını gösterir. Varsayılan olarak kapalıdır.",
                "Normal yaratıkların süper zırh sayıları", "Asıl süper zırh çubuğu görünürken normal yaratık çubuklarının yanında mevcut / azami süper zırhı gösterir. Varsayılan olarak kapalıdır.",
                "Mini boss can sayıları", "Mini boss çubuklarında can ve kalkan sayılarını gösterir. Varsayılan olarak açıktır.",
                "Mini boss süper zırh sayıları", "Asıl süper zırh çubuğu görünürken mini boss çubuklarının yanında mevcut / azami süper zırhı gösterir. Varsayılan olarak açıktır.",
                "Boss can sayıları", "Ayrı parçalar dahil boss çubuklarında can ve kalkan sayılarını gösterir. Varsayılan olarak açıktır.",
                "Yok edilebilir nesnelerin can sayıları", "Savaş totemleri dahil yok edilebilir nesnelerin mevcut çubuklarında can sayılarını gösterir. Varsayılan olarak kapalıdır.",
                "Kapalı", "Açık" }
        };

        internal static void Register(Action<string, string, string> addText, IEnumerable<string> languages)
        {
            var keys = new List<string>();
            foreach (ResourceBarValueSetting setting in Enum.GetValues(typeof(ResourceBarValueSetting)))
            {
                keys.Add(Setting(setting));
                keys.Add(Help(setting));
            }
            keys.Add(Off);
            keys.Add(On);
            Configuration.LocalizationGroup.Register(addText, languages, keys.ToArray(), Texts);
        }
    }
}
