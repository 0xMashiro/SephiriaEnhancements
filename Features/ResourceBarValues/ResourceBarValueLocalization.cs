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
        internal const string DisableAllNumbers = "SephiriaEnhancements.ResourceBarValues.DisableAllNumbers";
        internal const string DisableAllNumbersHelp = "SephiriaEnhancements.ResourceBarValues.DisableAllNumbersHelp";
        internal const string DisableAll = "SephiriaEnhancements.ResourceBarValues.DisableAll";
        internal const string AllOff = "SephiriaEnhancements.ResourceBarValues.AllOff";

        private static readonly Dictionary<string, string[]> Texts = new Dictionary<string, string[]>
        {
            ["en-US"] = new[] {
                "Ordinary creature health numbers", "Show health and shield numbers on ordinary creature bars. Off by default.",
                "Ordinary creature super armor numbers", "Show current / maximum super armor beside ordinary creature bars when the native super armor bar is visible. Off by default.",
                "Miniboss health numbers", "Show health and shield numbers on miniboss bars. On by default.",
                "Miniboss super armor numbers", "Show current / maximum super armor beside miniboss bars when the native super armor bar is visible. On by default.",
                "Boss health numbers", "Show health and shield numbers on boss bars, including separate parts. On by default.",
                "Destructible prop health numbers", "Show health numbers on existing destructible prop bars, including combat totems. Off by default.",
                "Teammate HP/MP numbers", "Show health, shield and mana numbers beside teammates in the multiplayer HUD. Off by default.",
                "Combat companion health numbers", "Show health and shield numbers in the combat companion HUD. Off by default. Disabling the Mod restores the original display.",
                "Mana reservation numbers", "Show reserved mana as an extra number on your mana bar. Off by default; the original mana numbers remain visible.",
                "Disable all extra numbers", "Turn off the numeric display options below. Your original HP/MP numbers remain visible. Companion HUD numbers are also hidden while the Mod is enabled.", "Disable all", "All off",
                "Off", "On" },
            ["zh-CN"] = new[] {
                "普通生物生命数值", "显示普通生物血条的生命与护盾数值。默认关闭。",
                "普通生物超级护甲数值", "当原有超级护甲条可见时，在普通生物血条旁显示超级护甲当前值 / 上限。默认关闭。",
                "小 BOSS 生命数值", "显示小 BOSS 血条的生命与护盾数值。默认开启。",
                "小 BOSS 超级护甲数值", "当原有超级护甲条可见时，在小 BOSS 血条旁显示超级护甲当前值 / 上限。默认开启。",
                "BOSS 生命数值", "显示 BOSS 血条的生命与护盾数值，包含独立部位。默认开启。",
                "可破坏物生命数值", "显示可破坏物已有血条的生命数值，包含战斗图腾。默认关闭。",
                "队友 HP/MP 数值", "在联机队友 HUD 旁显示生命、护盾和法力数值。默认关闭。",
                "战斗伙伴生命数值", "显示战斗伙伴 HUD 的生命与护盾数值。默认关闭；关闭 Mod 后恢复原有显示。",
                "法力占用数值", "在自身法力条上额外显示被占用的法力数值。默认关闭；原有法力数值仍会显示。",
                "关闭全部额外数值", "关闭下方数值显示选项。自身原有 HP/MP 数字仍保留；Mod 开启时也会隐藏伙伴 HUD 数字。", "全部关闭", "已全部关闭",
                "关闭", "开启" },
            ["zh-TW"] = new[] {
                "普通生物生命數值", "顯示普通生物血條的生命與護盾數值。預設關閉。",
                "普通生物超級護甲數值", "當原有超級護甲條可見時，在普通生物血條旁顯示超級護甲目前值 / 上限。預設關閉。",
                "小 BOSS 生命數值", "顯示小 BOSS 血條的生命與護盾數值。預設開啟。",
                "小 BOSS 超級護甲數值", "當原有超級護甲條可見時，在小 BOSS 血條旁顯示超級護甲目前值 / 上限。預設開啟。",
                "BOSS 生命數值", "顯示 BOSS 血條的生命與護盾數值，包含獨立部位。預設開啟。",
                "可破壞物生命數值", "顯示可破壞物已有血條的生命數值，包含戰鬥圖騰。預設關閉。",
                "隊友 HP/MP 數值", "在連線隊友 HUD 旁顯示生命、護盾與魔力數值。預設關閉。",
                "戰鬥夥伴生命數值", "顯示戰鬥夥伴 HUD 的生命與護盾數值。預設關閉；關閉 Mod 後恢復原有顯示。",
                "魔力占用數值", "在自身魔力條上額外顯示被占用的魔力數值。預設關閉；原有魔力數值仍會顯示。",
                "關閉全部額外數值", "關閉下方數值顯示選項。自身原有 HP/MP 數字仍保留；Mod 開啟時也會隱藏夥伴 HUD 數字。", "全部關閉", "已全部關閉",
                "關閉", "開啟" },
            ["ko-KR"] = new[] {
                "일반 생물 체력 수치", "일반 생물의 체력 바에 체력과 보호막 수치를 표시합니다. 기본값은 꺼짐입니다.",
                "일반 생물 슈퍼 아머 수치", "기존 슈퍼 아머 바가 보일 때 일반 생물 바 옆에 현재 / 최대 슈퍼 아머를 표시합니다. 기본값은 꺼짐입니다.",
                "미니보스 체력 수치", "미니보스의 체력 바에 체력과 보호막 수치를 표시합니다. 기본값은 켜짐입니다.",
                "미니보스 슈퍼 아머 수치", "기존 슈퍼 아머 바가 보일 때 미니보스 바 옆에 현재 / 최대 슈퍼 아머를 표시합니다. 기본값은 켜짐입니다.",
                "보스 체력 수치", "개별 부위를 포함한 보스의 체력 바에 체력과 보호막 수치를 표시합니다. 기본값은 켜짐입니다.",
                "파괴 가능 오브젝트 체력 수치", "전투 토템을 포함한 파괴 가능 오브젝트의 기존 체력 바에 체력 수치를 표시합니다. 기본값은 꺼짐입니다.",
                "팀원 HP/MP 수치", "멀티플레이 HUD의 팀원 옆에 체력, 보호막, 마나 수치를 표시합니다. 기본값은 꺼짐입니다.",
                "전투 동료 체력 수치", "전투 동료 HUD에 체력과 보호막 수치를 표시합니다. 기본값은 꺼짐이며 Mod를 끄면 원래 표시로 돌아갑니다.",
                "마나 점유 수치", "자신의 마나 바에 점유된 마나 수치를 추가로 표시합니다. 기본값은 꺼짐이며 기존 마나 수치는 유지됩니다.",
                "추가 수치 모두 끄기", "아래 수치 표시 옵션을 모두 끕니다. 자신의 기존 HP/MP 수치는 유지되며 Mod가 켜져 있는 동안 동료 HUD 수치도 숨깁니다.", "모두 끄기", "모두 꺼짐",
                "꺼짐", "켜짐" },
            ["ja-JP"] = new[] {
                "通常の生物のHP数値", "通常の生物のHPバーにHPとシールドの数値を表示します。初期設定はオフです。",
                "通常の生物のスーパーアーマー数値", "元のスーパーアーマーバーが表示されているとき、通常の生物のバーの横に現在値 / 最大値を表示します。初期設定はオフです。",
                "ミニボスのHP数値", "ミニボスのHPバーにHPとシールドの数値を表示します。初期設定はオンです。",
                "ミニボスのスーパーアーマー数値", "元のスーパーアーマーバーが表示されているとき、ミニボスのバーの横に現在値 / 最大値を表示します。初期設定はオンです。",
                "ボスのHP数値", "独立した部位を含むボスのHPバーにHPとシールドの数値を表示します。初期設定はオンです。",
                "破壊可能オブジェクトのHP数値", "戦闘トーテムを含む破壊可能オブジェクトの既存のHPバーにHP数値を表示します。初期設定はオフです。",
                "味方プレイヤーのHP/MP数値", "マルチプレイHUDの味方の横にHP、シールド、MPの数値を表示します。初期設定はオフです。",
                "戦闘仲間のHP数値", "戦闘仲間のHUDにHPとシールドの数値を表示します。初期設定はオフです。Modをオフにすると元の表示に戻ります。",
                "MP予約量の数値", "自分のMPバーに予約されたMPの数値を追加表示します。初期設定はオフです。元のMP数値は引き続き表示されます。",
                "追加数値をすべてオフ", "以下の数値表示をオフにします。自分の元のHP/MP数値は残ります。Modがオンの間は仲間HUDの数値も非表示になります。", "すべてオフ", "すべてオフ済み",
                "オフ", "オン" },
            ["de-DE"] = new[] {
                "Lebenspunkte normaler Kreaturen", "Zeigt Lebens- und Schildwerte an den Leisten normaler Kreaturen. Standardmäßig aus.",
                "Superrüstungswerte normaler Kreaturen", "Zeigt aktuelle / maximale Superrüstung neben normalen Kreaturen, wenn die ursprüngliche Superrüstungsleiste sichtbar ist. Standardmäßig aus.",
                "Lebenspunkte von Minibossen", "Zeigt Lebens- und Schildwerte an Minibossleisten. Standardmäßig an.",
                "Superrüstungswerte von Minibossen", "Zeigt aktuelle / maximale Superrüstung neben Minibossleisten, wenn die ursprüngliche Superrüstungsleiste sichtbar ist. Standardmäßig an.",
                "Lebenspunkte von Bossen", "Zeigt Lebens- und Schildwerte an Bossleisten, einschließlich einzelner Körperteile. Standardmäßig an.",
                "Lebenspunkte zerstörbarer Objekte", "Zeigt Lebenswerte an vorhandenen Leisten zerstörbarer Objekte, einschließlich Kampftotems. Standardmäßig aus.",
                "LP/MP-Zahlen der Mitspieler", "Zeigt Leben, Schild und Mana neben Mitspielern im Mehrspieler-HUD. Standardmäßig aus.",
                "Lebenszahlen des Kampfbegleiters", "Zeigt Leben und Schild im Kampfbegleiter-HUD. Standardmäßig aus. Das Deaktivieren der Mod stellt die ursprüngliche Anzeige wieder her.",
                "Zahlen für reserviertes Mana", "Zeigt reserviertes Mana als zusätzliche Zahl an deiner Manaleiste. Standardmäßig aus; die ursprünglichen Manazahlen bleiben sichtbar.",
                "Alle Zusatzwerte ausblenden", "Schaltet die folgenden Zahlenanzeigen aus. Eigene ursprüngliche LP/MP bleiben sichtbar; Begleiter-HUD-Zahlen werden bei aktiver Mod ebenfalls ausgeblendet.", "Alle ausschalten", "Alle aus",
                "Aus", "An" },
            ["es-ES"] = new[] {
                "Vida numérica de criaturas comunes", "Muestra la vida y el escudo en las barras de criaturas comunes. Desactivado por defecto.",
                "Superarmadura de criaturas comunes", "Muestra la superarmadura actual / máxima junto a las barras de criaturas comunes cuando su barra original es visible. Desactivado por defecto.",
                "Vida numérica de minijefes", "Muestra la vida y el escudo en las barras de minijefes. Activado por defecto.",
                "Superarmadura numérica de minijefes", "Muestra la superarmadura actual / máxima junto a las barras de minijefes cuando su barra original es visible. Activado por defecto.",
                "Vida numérica de jefes", "Muestra la vida y el escudo en las barras de jefes, incluidas sus partes independientes. Activado por defecto.",
                "Vida de objetos destructibles", "Muestra la vida en las barras existentes de objetos destructibles, incluidos tótems de combate. Desactivado por defecto.",
                "PV/PM de compañeros de equipo", "Muestra vida, escudo y maná junto a los compañeros en el HUD multijugador. Desactivado por defecto.",
                "Vida del compañero de combate", "Muestra vida y escudo en el HUD del compañero de combate. Desactivado por defecto. Al desactivar el Mod se restaura la visualización original.",
                "Maná reservado en números", "Añade la cantidad de maná reservado a tu barra de maná. Desactivado por defecto; los números originales de maná siguen visibles.",
                "Ocultar todos los números extra", "Desactiva las opciones numéricas de abajo. Tus PV/PM originales siguen visibles; también se ocultan los números del HUD del compañero mientras el Mod esté activo.", "Desactivar todo", "Todo desactivado",
                "Desactivado", "Activado" },
            ["fr-FR"] = new[] {
                "PV chiffrés des créatures ordinaires", "Affiche les PV et le bouclier sur les barres des créatures ordinaires. Désactivé par défaut.",
                "Super armure des créatures ordinaires", "Affiche la super armure actuelle / maximale près des barres des créatures ordinaires lorsque sa barre d’origine est visible. Désactivé par défaut.",
                "PV chiffrés des mini-boss", "Affiche les PV et le bouclier sur les barres des mini-boss. Activé par défaut.",
                "Super armure chiffrée des mini-boss", "Affiche la super armure actuelle / maximale près des barres des mini-boss lorsque sa barre d’origine est visible. Activé par défaut.",
                "PV chiffrés des boss", "Affiche les PV et le bouclier sur les barres des boss, y compris leurs parties distinctes. Activé par défaut.",
                "PV des objets destructibles", "Affiche les PV sur les barres existantes des objets destructibles, y compris les totems de combat. Désactivé par défaut.",
                "PV/PM des coéquipiers", "Affiche les PV, le bouclier et le mana près des coéquipiers dans le HUD multijoueur. Désactivé par défaut.",
                "PV du compagnon de combat", "Affiche les PV et le bouclier dans le HUD du compagnon de combat. Désactivé par défaut. Désactiver le Mod rétablit l’affichage d’origine.",
                "Mana réservé chiffré", "Ajoute la quantité de mana réservé à votre barre de mana. Désactivé par défaut ; les chiffres d’origine restent visibles.",
                "Masquer tous les chiffres ajoutés", "Désactive les options chiffrées ci-dessous. Vos PV/PM d’origine restent visibles ; les chiffres du compagnon sont aussi masqués tant que le Mod est actif.", "Tout désactiver", "Tout désactivé",
                "Désactivé", "Activé" },
            ["it-IT"] = new[] {
                "Vita numerica delle creature comuni", "Mostra vita e scudo sulle barre delle creature comuni. Disattivato per impostazione predefinita.",
                "Super armatura delle creature comuni", "Mostra la super armatura attuale / massima accanto alle barre delle creature comuni quando la barra originale è visibile. Disattivato per impostazione predefinita.",
                "Vita numerica dei miniboss", "Mostra vita e scudo sulle barre dei miniboss. Attivato per impostazione predefinita.",
                "Super armatura numerica dei miniboss", "Mostra la super armatura attuale / massima accanto alle barre dei miniboss quando la barra originale è visibile. Attivato per impostazione predefinita.",
                "Vita numerica dei boss", "Mostra vita e scudo sulle barre dei boss, incluse le parti separate. Attivato per impostazione predefinita.",
                "Vita degli oggetti distruttibili", "Mostra la vita sulle barre esistenti degli oggetti distruttibili, inclusi i totem da combattimento. Disattivato per impostazione predefinita.",
                "PV/PM dei compagni di squadra", "Mostra vita, scudo e mana accanto ai compagni nel HUD multigiocatore. Disattivato per impostazione predefinita.",
                "Vita del compagno di combattimento", "Mostra vita e scudo nel HUD del compagno di combattimento. Disattivato per impostazione predefinita. Disattivando il Mod si ripristina la visualizzazione originale.",
                "Mana riservato in cifre", "Aggiunge la quantità di mana riservato alla tua barra del mana. Disattivato per impostazione predefinita; le cifre originali restano visibili.",
                "Nascondi tutti i numeri extra", "Disattiva le opzioni numeriche sotto. I tuoi PV/PM originali restano visibili; i numeri del compagno sono nascosti finché il Mod è attivo.", "Disattiva tutto", "Tutto disattivato",
                "Disattivato", "Attivato" },
            ["pl-PL"] = new[] {
                "Liczbowe zdrowie zwykłych stworzeń", "Pokazuje zdrowie i tarczę na paskach zwykłych stworzeń. Domyślnie wyłączone.",
                "Superpancerz zwykłych stworzeń", "Pokazuje bieżący / maksymalny superpancerz obok pasków zwykłych stworzeń, gdy widoczny jest jego oryginalny pasek. Domyślnie wyłączone.",
                "Liczbowe zdrowie minibossów", "Pokazuje zdrowie i tarczę na paskach minibossów. Domyślnie włączone.",
                "Liczbowy superpancerz minibossów", "Pokazuje bieżący / maksymalny superpancerz obok pasków minibossów, gdy widoczny jest jego oryginalny pasek. Domyślnie włączone.",
                "Liczbowe zdrowie bossów", "Pokazuje zdrowie i tarczę na paskach bossów, w tym oddzielnych części. Domyślnie włączone.",
                "Zdrowie zniszczalnych obiektów", "Pokazuje zdrowie na istniejących paskach zniszczalnych obiektów, w tym totemów bojowych. Domyślnie wyłączone.",
                "PZ/PM członków drużyny", "Pokazuje zdrowie, tarczę i manę obok graczy w HUD trybu wieloosobowego. Domyślnie wyłączone.",
                "Zdrowie towarzysza walki", "Pokazuje zdrowie i tarczę w HUD towarzysza walki. Domyślnie wyłączone. Wyłączenie moda przywraca oryginalny wygląd.",
                "Liczby zarezerwowanej many", "Dodaje liczbę zarezerwowanej many do twojego paska many. Domyślnie wyłączone; oryginalne liczby many pozostają widoczne.",
                "Ukryj wszystkie dodatkowe liczby", "Wyłącza poniższe opcje liczbowe. Twoje oryginalne PZ/PM pozostają widoczne; liczby towarzysza są ukryte, gdy mod jest włączony.", "Wyłącz wszystko", "Wszystko wyłączone",
                "Wyłączone", "Włączone" },
            ["pt-BR"] = new[] {
                "Vida numérica de criaturas comuns", "Exibe vida e escudo nas barras de criaturas comuns. Desativado por padrão.",
                "Superarmadura de criaturas comuns", "Exibe a superarmadura atual / máxima ao lado das barras de criaturas comuns quando a barra original está visível. Desativado por padrão.",
                "Vida numérica de minichefes", "Exibe vida e escudo nas barras de minichefes. Ativado por padrão.",
                "Superarmadura numérica de minichefes", "Exibe a superarmadura atual / máxima ao lado das barras de minichefes quando a barra original está visível. Ativado por padrão.",
                "Vida numérica de chefes", "Exibe vida e escudo nas barras de chefes, incluindo partes separadas. Ativado por padrão.",
                "Vida de objetos destrutíveis", "Exibe a vida nas barras existentes de objetos destrutíveis, incluindo totens de combate. Desativado por padrão.",
                "PV/PM dos colegas de equipe", "Exibe vida, escudo e mana ao lado dos colegas no HUD multijogador. Desativado por padrão.",
                "Vida do companheiro de combate", "Exibe vida e escudo no HUD do companheiro de combate. Desativado por padrão. Desativar o Mod restaura a exibição original.",
                "Mana reservada em números", "Adiciona a quantidade de mana reservada à sua barra de mana. Desativado por padrão; os números originais de mana continuam visíveis.",
                "Ocultar todos os números extras", "Desativa as opções numéricas abaixo. Seus PV/PM originais permanecem visíveis; os números do companheiro também ficam ocultos enquanto o Mod estiver ativo.", "Desativar tudo", "Tudo desativado",
                "Desativado", "Ativado" },
            ["ru-RU"] = new[] {
                "Числа здоровья обычных существ", "Показывает здоровье и щит на полосках обычных существ. По умолчанию выключено.",
                "Числа суперброни обычных существ", "Показывает текущую / максимальную суперброню рядом с полосками обычных существ, когда видна исходная полоска суперброни. По умолчанию выключено.",
                "Числа здоровья мини-боссов", "Показывает здоровье и щит на полосках мини-боссов. По умолчанию включено.",
                "Числа суперброни мини-боссов", "Показывает текущую / максимальную суперброню рядом с полосками мини-боссов, когда видна исходная полоска суперброни. По умолчанию включено.",
                "Числа здоровья боссов", "Показывает здоровье и щит на полосках боссов, включая отдельные части. По умолчанию включено.",
                "Здоровье разрушаемых объектов", "Показывает здоровье на существующих полосках разрушаемых объектов, включая боевые тотемы. По умолчанию выключено.",
                "Числа ОЗ/ОМ союзных игроков", "Показывает здоровье, щит и ману рядом с союзниками в сетевом интерфейсе. По умолчанию выключено.",
                "Числа здоровья боевого спутника", "Показывает здоровье и щит в интерфейсе боевого спутника. По умолчанию выключено. Отключение мода возвращает исходное отображение.",
                "Числа зарезервированной маны", "Добавляет количество зарезервированной маны на вашу полоску маны. По умолчанию выключено; исходные числа маны остаются видны.",
                "Скрыть все дополнительные числа", "Отключает числовые параметры ниже. Ваши исходные ОЗ/ОМ остаются видны; числа спутника скрыты, пока мод включён.", "Отключить всё", "Всё отключено",
                "Выключено", "Включено" },
            ["sv-SE"] = new[] {
                "Hälsotal för vanliga varelser", "Visar hälsa och sköld på vanliga varelsers mätare. Av som standard.",
                "Superpansartal för vanliga varelser", "Visar aktuellt / maximalt superpansar vid vanliga varelsers mätare när den ursprungliga superpansarmätaren syns. Av som standard.",
                "Hälsotal för minibossar", "Visar hälsa och sköld på minibossars mätare. På som standard.",
                "Superpansartal för minibossar", "Visar aktuellt / maximalt superpansar vid minibossars mätare när den ursprungliga superpansarmätaren syns. På som standard.",
                "Hälsotal för bossar", "Visar hälsa och sköld på bossars mätare, inklusive separata delar. På som standard.",
                "Hälsotal för förstörbara föremål", "Visar hälsa på befintliga mätare för förstörbara föremål, inklusive stridstotem. Av som standard.",
                "Lagkamraters HP/MP-tal", "Visar hälsa, sköld och mana bredvid lagkamrater i flerspelargränssnittet. Av som standard.",
                "Stridsföljeslagarens hälsotal", "Visar hälsa och sköld i stridsföljeslagarens gränssnitt. Av som standard. När modden stängs av återställs originalvisningen.",
                "Tal för reserverad mana", "Lägger till mängden reserverad mana på din manamätare. Av som standard; de ursprungliga manatalen förblir synliga.",
                "Dölj alla extra tal", "Stänger av siffervisningen nedan. Dina ursprungliga HP/MP förblir synliga; följeslagarens tal döljs också medan modden är aktiv.", "Stäng av alla", "Alla av",
                "Av", "På" },
            ["th-TH"] = new[] {
                "ตัวเลขพลังชีวิตของสิ่งมีชีวิตทั่วไป", "แสดงตัวเลขพลังชีวิตและโล่บนแถบของสิ่งมีชีวิตทั่วไป ปิดไว้เป็นค่าเริ่มต้น",
                "ตัวเลขซูเปอร์อาร์เมอร์ของสิ่งมีชีวิตทั่วไป", "แสดงซูเปอร์อาร์เมอร์ปัจจุบัน / สูงสุดข้างแถบของสิ่งมีชีวิตทั่วไปเมื่อแถบซูเปอร์อาร์เมอร์เดิมปรากฏ ปิดไว้เป็นค่าเริ่มต้น",
                "ตัวเลขพลังชีวิตของมินิบอส", "แสดงตัวเลขพลังชีวิตและโล่บนแถบของมินิบอส เปิดไว้เป็นค่าเริ่มต้น",
                "ตัวเลขซูเปอร์อาร์เมอร์ของมินิบอส", "แสดงซูเปอร์อาร์เมอร์ปัจจุบัน / สูงสุดข้างแถบของมินิบอสเมื่อแถบซูเปอร์อาร์เมอร์เดิมปรากฏ เปิดไว้เป็นค่าเริ่มต้น",
                "ตัวเลขพลังชีวิตของบอส", "แสดงตัวเลขพลังชีวิตและโล่บนแถบของบอส รวมถึงชิ้นส่วนแยก เปิดไว้เป็นค่าเริ่มต้น",
                "ตัวเลขพลังชีวิตของวัตถุที่ทำลายได้", "แสดงตัวเลขพลังชีวิตบนแถบเดิมของวัตถุที่ทำลายได้ รวมถึงโทเท็มต่อสู้ ปิดไว้เป็นค่าเริ่มต้น",
                "ตัวเลข HP/MP ของเพื่อนร่วมทีม", "แสดงตัวเลขพลังชีวิต โล่ และมานาข้างเพื่อนร่วมทีมใน HUD ผู้เล่นหลายคน ปิดไว้เป็นค่าเริ่มต้น",
                "ตัวเลขพลังชีวิตของสหายร่วมรบ", "แสดงตัวเลขพลังชีวิตและโล่ใน HUD ของสหายร่วมรบ ปิดไว้เป็นค่าเริ่มต้น เมื่อปิด Mod จะคืนการแสดงผลเดิม",
                "ตัวเลขมานาที่ถูกจอง", "เพิ่มตัวเลขมานาที่ถูกจองบนแถบมานาของคุณ ปิดไว้เป็นค่าเริ่มต้น ตัวเลขมานาเดิมยังคงแสดงอยู่",
                "ซ่อนตัวเลขเพิ่มเติมทั้งหมด", "ปิดตัวเลือกตัวเลขด้านล่าง ตัวเลข HP/MP เดิมของคุณยังแสดงอยู่ และซ่อนตัวเลข HUD ของสหายขณะเปิด Mod", "ปิดทั้งหมด", "ปิดทั้งหมดแล้ว",
                "ปิด", "เปิด" },
            ["tr-TR"] = new[] {
                "Normal yaratıkların can sayıları", "Normal yaratık çubuklarında can ve kalkan sayılarını gösterir. Varsayılan olarak kapalıdır.",
                "Normal yaratıkların süper zırh sayıları", "Asıl süper zırh çubuğu görünürken normal yaratık çubuklarının yanında mevcut / azami süper zırhı gösterir. Varsayılan olarak kapalıdır.",
                "Mini boss can sayıları", "Mini boss çubuklarında can ve kalkan sayılarını gösterir. Varsayılan olarak açıktır.",
                "Mini boss süper zırh sayıları", "Asıl süper zırh çubuğu görünürken mini boss çubuklarının yanında mevcut / azami süper zırhı gösterir. Varsayılan olarak açıktır.",
                "Boss can sayıları", "Ayrı parçalar dahil boss çubuklarında can ve kalkan sayılarını gösterir. Varsayılan olarak açıktır.",
                "Yok edilebilir nesnelerin can sayıları", "Savaş totemleri dahil yok edilebilir nesnelerin mevcut çubuklarında can sayılarını gösterir. Varsayılan olarak kapalıdır.",
                "Takım arkadaşlarının HP/MP sayıları", "Çok oyunculu HUD üzerinde takım arkadaşlarının yanında can, kalkan ve mana sayılarını gösterir. Varsayılan olarak kapalıdır.",
                "Savaş yoldaşının can sayıları", "Savaş yoldaşının HUD alanında can ve kalkan sayılarını gösterir. Varsayılan olarak kapalıdır. Mod kapatılınca asıl görünüm geri gelir.",
                "Ayrılmış mana sayıları", "Mana çubuğuna ayrılmış mana miktarını ekler. Varsayılan olarak kapalıdır; asıl mana sayıları görünür kalır.",
                "Tüm ek sayıları gizle", "Aşağıdaki sayı seçeneklerini kapatır. Kendi asıl HP/MP sayılarınız görünür kalır; Mod açıkken yoldaş sayıları da gizlenir.", "Tümünü kapat", "Tümü kapalı",
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
            keys.Add(DisableAllNumbers);
            keys.Add(DisableAllNumbersHelp);
            keys.Add(DisableAll);
            keys.Add(AllOff);
            keys.Add(Off);
            keys.Add(On);
            Configuration.LocalizationGroup.Register(addText, languages, keys.ToArray(), Texts);
        }
    }
}
