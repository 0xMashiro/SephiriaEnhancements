#nullable enable
using System;
using System.Collections.Generic;
namespace SephiriaEnhancements.Configuration
{
    internal enum OptionsCategory
    {
        General,
        CombatAndDisplay,
        ResourceBarValues,
        ControlsAndCamera,
        Multiplayer,
        AboutAndUpdates
    }
    internal static class OptionsCategoryVisibility
    {
        internal static bool IsVisible(OptionsCategory memberCategory, OptionsCategory selectedCategory) => memberCategory == selectedCategory;
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
            "SephiriaEnhancements.OptionsCategory.ResourceBarValues",
            "SephiriaEnhancements.OptionsCategory.ControlsAndCamera",
            "SephiriaEnhancements.OptionsCategory.Multiplayer",
            "SephiriaEnhancements.OptionsCategory.AboutAndUpdates"
        };
        private static readonly string[] Keys =
        {
            Setting, Help, CategoryKeys[0], CategoryKeys[1], CategoryKeys[2],
            CategoryKeys[3], CategoryKeys[4], CategoryKeys[5]
        };
        private static readonly Dictionary<string, string[]> Texts = new()
        {
            ["en-US"] = new[] { "Settings Category", "Choose which Sephiria Enhancements settings group is shown below.", "General", "Combat and Display", "Bars and Numbers", "Controls and Camera", "Multiplayer", "About and Updates" },
            ["zh-CN"] = new[] { "设置分类", "选择下方显示的 Sephiria 增强设置组。", "基础功能", "战斗与显示", "血条与数值", "操作与镜头", "多人游戏", "关于与更新" },
            ["zh-TW"] = new[] { "設定分類", "選擇下方顯示的 Sephiria 增強設定群組。", "基礎功能", "戰鬥與顯示", "血條與數值", "操作與鏡頭", "多人遊戲", "關於與更新" },
            ["ko-KR"] = new[] { "설정 분류", "아래에 표시할 Sephiria Enhancements 설정 분류를 선택합니다.", "일반", "전투 및 표시", "상태 막대와 수치", "조작 및 카메라", "멀티플레이", "정보 및 업데이트" },
            ["ja-JP"] = new[] { "設定カテゴリ", "下に表示する Sephiria Enhancements の設定カテゴリを選びます。", "基本", "戦闘と表示", "ゲージと数値", "操作とカメラ", "マルチプレイ", "情報と更新" },
            ["de-DE"] = new[] { "Einstellungskategorie", "Wählt die unten angezeigte Einstellungsgruppe von Sephiria Enhancements.", "Allgemein", "Kampf und Anzeige", "Leisten und Zahlen", "Steuerung und Kamera", "Mehrspieler", "Info und Updates" },
            ["es-ES"] = new[] { "Categoría de ajustes", "Elige qué grupo de ajustes de Sephiria Enhancements se muestra debajo.", "General", "Combate y visualización", "Barras y cifras", "Controles y cámara", "Multijugador", "Información y actualizaciones" },
            ["fr-FR"] = new[] { "Catégorie de paramètres", "Choisissez le groupe de paramètres de Sephiria Enhancements à afficher ci-dessous.", "Général", "Combat et affichage", "Barres et valeurs", "Commandes et caméra", "Multijoueur", "Informations et mises à jour" },
            ["it-IT"] = new[] { "Categoria impostazioni", "Scegli il gruppo di impostazioni di Sephiria Enhancements da mostrare qui sotto.", "Generali", "Combattimento e visualizzazione", "Barre e valori", "Comandi e telecamera", "Multigiocatore", "Informazioni e aggiornamenti" },
            ["pl-PL"] = new[] { "Kategoria ustawień", "Wybierz grupę ustawień Sephiria Enhancements wyświetlaną poniżej.", "Ogólne", "Walka i wyświetlanie", "Paski i wartości", "Sterowanie i kamera", "Tryb wieloosobowy", "Informacje i aktualizacje" },
            ["pt-BR"] = new[] { "Categoria de configurações", "Escolha o grupo de configurações do Sephiria Enhancements exibido abaixo.", "Geral", "Combate e exibição", "Barras e valores", "Controles e câmera", "Multijogador", "Informações e atualizações" },
            ["ru-RU"] = new[] { "Категория настроек", "Выберите группу настроек Sephiria Enhancements для отображения ниже.", "Общие", "Бой и отображение", "Шкалы и числа", "Управление и камера", "Сетевая игра", "Информация и обновления" },
            ["sv-SE"] = new[] { "Inställningskategori", "Välj vilken grupp av inställningar för Sephiria Enhancements som visas nedan.", "Allmänt", "Strid och visning", "Mätare och siffror", "Kontroller och kamera", "Flerspelarläge", "Om och uppdateringar" },
            ["th-TH"] = new[] { "หมวดการตั้งค่า", "เลือกหมวดการตั้งค่า Sephiria Enhancements ที่จะแสดงด้านล่าง", "ทั่วไป", "การต่อสู้และการแสดงผล", "แถบและตัวเลข", "การควบคุมและกล้อง", "ผู้เล่นหลายคน", "ข้อมูลและอัปเดต" },
            ["tr-TR"] = new[] { "Ayar kategorisi", "Aşağıda gösterilecek Sephiria Enhancements ayar grubunu seçin.", "Genel", "Savaş ve görünüm", "Çubuklar ve sayılar", "Kontroller ve kamera", "Çok oyunculu", "Hakkında ve güncellemeler" }
        };
        internal static void Register(Action<string, string, string> addText,
            IEnumerable<string> languages)
        {
            LocalizationGroup.Register(addText, languages, Keys, Texts);
        }
    }
}
