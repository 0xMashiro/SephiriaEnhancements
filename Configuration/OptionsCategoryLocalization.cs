#nullable enable
using System;
using System.Collections.Generic;

namespace SephiriaEnhancements.Configuration
{
    internal enum OptionsCategory
    {
        General,
        CombatAndDisplay,
        ControlsAndCamera,
        InventoryArrangement,
        Multiplayer
    }

    internal static class OptionsCategoryVisibility
    {
        internal static bool IsVisible(OptionsCategory memberCategory,
            OptionsCategory selectedCategory, bool requiresCustomPreset,
            bool customPresetVisible, int memberMultiplayerRuleGroup,
            int selectedMultiplayerRuleGroup)
        {
            if (memberCategory != selectedCategory ||
                requiresCustomPreset && !customPresetVisible)
            {
                return false;
            }

            return memberMultiplayerRuleGroup < 0 ||
                memberMultiplayerRuleGroup == selectedMultiplayerRuleGroup;
        }
    }

    internal static class OptionsCategoryLocalization
    {
        internal const string Setting =
            "SephiriaEnhancements.OptionsCategory.Setting";
        internal const string Help =
            "SephiriaEnhancements.OptionsCategory.Help";

        internal static readonly string[] CategoryKeys =
        {
            "SephiriaEnhancements.OptionsCategory.General",
            "SephiriaEnhancements.OptionsCategory.CombatAndDisplay",
            "SephiriaEnhancements.OptionsCategory.ControlsAndCamera",
            "SephiriaEnhancements.OptionsCategory.InventoryArrangement",
            "SephiriaEnhancements.OptionsCategory.Multiplayer"
        };

        private static readonly string[] Keys =
        {
            Setting, Help, CategoryKeys[0], CategoryKeys[1], CategoryKeys[2],
            CategoryKeys[3], CategoryKeys[4]
        };

        private static readonly Dictionary<string, string[]> Texts = new()
        {
            ["en-US"] = new[]
            {
                "Settings Category",
                "Choose which Sephiria Enhancements settings group is shown below.",
                "General", "Combat and Display", "Controls and Camera",
                "Inventory Arrangement", "Multiplayer"
            },
            ["zh-CN"] = new[]
            {
                "设置分类", "选择下方显示的 Sephiria 增强设置组。",
                "基础功能", "战斗与显示", "操作与镜头", "背包整理",
                "多人游戏"
            },
            ["zh-TW"] = new[]
            {
                "設定分類", "選擇下方顯示的 Sephiria 增強設定群組。",
                "基礎功能", "戰鬥與顯示", "操作與鏡頭", "背包整理",
                "多人遊戲"
            },
            ["ko-KR"] = new[] { "설정 분류", "아래에 표시할 Sephiria Enhancements 설정 분류를 선택합니다.", "일반", "전투 및 표시", "조작 및 카메라", "인벤토리 정리", "멀티플레이" },
            ["ja-JP"] = new[] { "設定カテゴリ", "下に表示する Sephiria Enhancements の設定カテゴリを選びます。", "基本", "戦闘と表示", "操作とカメラ", "インベントリ整理", "マルチプレイ" },
            ["de-DE"] = new[] { "Einstellungskategorie", "Wählt die unten angezeigte Einstellungsgruppe von Sephiria Enhancements.", "Allgemein", "Kampf und Anzeige", "Steuerung und Kamera", "Inventarverwaltung", "Mehrspieler" },
            ["es-ES"] = new[] { "Categoría de ajustes",
                    "Elige qué grupo de ajustes de Sephiria Enhancements se muestra debajo.",
                    "General",
                    "Combate y visualización",
                    "Controles y cámara",
                    "Organización del inventario",
                    "Multijugador" },
            ["fr-FR"] = new[] { "Catégorie de paramètres",
                    "Choisissez le groupe de paramètres de Sephiria Enhancements à afficher ci-dessous.",
                    "Général",
                    "Combat et affichage",
                    "Commandes et caméra",
                    "Organisation de l’inventaire",
                    "Multijoueur" },
            ["it-IT"] = new[] { "Categoria impostazioni",
                    "Scegli il gruppo di impostazioni di Sephiria Enhancements da mostrare qui sotto.",
                    "Generali",
                    "Combattimento e visualizzazione",
                    "Comandi e telecamera",
                    "Organizzazione inventario",
                    "Multigiocatore" },
            ["pl-PL"] = new[] { "Kategoria ustawień", "Wybierz grupę ustawień Sephiria Enhancements wyświetlaną poniżej.", "Ogólne", "Walka i wyświetlanie", "Sterowanie i kamera", "Organizacja ekwipunku", "Tryb wieloosobowy" },
            ["pt-BR"] = new[] { "Categoria de configurações",
                    "Escolha o grupo de configurações do Sephiria Enhancements exibido abaixo.",
                    "Geral",
                    "Combate e exibição",
                    "Controles e câmera",
                    "Organização do inventário",
                    "Multijogador" },
            ["ru-RU"] = new[] { "Категория настроек", "Выберите группу настроек Sephiria Enhancements для отображения ниже.", "Общие", "Бой и отображение", "Управление и камера", "Организация инвентаря", "Сетевая игра" },
            ["sv-SE"] = new[] { "Inställningskategori", "Välj vilken grupp av inställningar för Sephiria Enhancements som visas nedan.", "Allmänt", "Strid och visning", "Kontroller och kamera", "Inventariehantering", "Flerspelarläge" },
            ["th-TH"] = new[] { "หมวดการตั้งค่า", "เลือกหมวดการตั้งค่า Sephiria Enhancements ที่จะแสดงด้านล่าง", "ทั่วไป", "การต่อสู้และการแสดงผล", "การควบคุมและกล้อง", "การจัดช่องเก็บของ", "ผู้เล่นหลายคน" },
            ["tr-TR"] = new[] { "Ayar kategorisi", "Aşağıda gösterilecek Sephiria Enhancements ayar grubunu seçin.", "Genel", "Savaş ve görünüm", "Kontroller ve kamera", "Envanter düzenleme", "Çok oyunculu" }
        };

        internal static void Register(Action<string, string, string> addText,
            IEnumerable<string> languages)
        {
            foreach (string language in languages)
            {
                string[] values = Texts.TryGetValue(language,
                    out string[]? localized) && localized != null
                    ? localized : Texts["en-US"];
                for (int index = 0; index < Keys.Length; index++)
                {
                    addText(language, Keys[index], values[index]);
                }
            }
        }
    }
}
