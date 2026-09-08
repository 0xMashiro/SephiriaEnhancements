using System;
using System.Collections.Generic;

namespace SephiriaEnhancements.MapEnhancements
{
    internal static class MapEnhancementsLocalization
    {
        internal const string SettingEnabled = "SephiriaEnhancements.MapEnhancements.Setting.Enabled";
        internal const string HelpEnabled = "SephiriaEnhancements.MapEnhancements.Help.Enabled";
        internal const string SettingShowHiddenRooms =
            "SephiriaEnhancements.MapEnhancements.Setting.ShowHiddenRooms";
        internal const string HelpShowHiddenRooms =
            "SephiriaEnhancements.MapEnhancements.Help.ShowHiddenRooms";
        internal const string Off = "SephiriaEnhancements.MapEnhancements.Off";
        internal const string On = "SephiriaEnhancements.MapEnhancements.On";

        private static readonly Dictionary<string, string[]> Texts = new()
        {
            ["en-US"] = new[] { "Show hidden rooms", "Show undiscovered hidden rooms on supported maps and the current-floor overlay. Disabled by default; enabling this reveals secret locations early.", "Off", "On", "Map enhancements", "Enable the map tools, current-floor overlay, NPC tracking and hidden-room option. Turn off to use the original map. Your hidden-room preference is kept." },
            ["zh-CN"] = new[] { "显示隐藏房间", "在支持的地图及本层地图叠加层中显示尚未发现的隐藏房间。默认关闭；开启会提前揭示秘密位置。", "关闭", "开启", "地图增强", "启用地图工具、本层地图叠层、NPC 跟踪和隐藏房间选项。关闭后使用游戏原有地图，并保留隐藏房间偏好。" },
            ["zh-TW"] = new[] { "顯示隱藏房間", "在支援的地圖及本層地圖疊加層中顯示尚未發現的隱藏房間。預設關閉；開啟會提前揭示秘密位置。", "關閉", "開啟", "地圖增強", "啟用地圖工具、本層地圖疊加層、NPC 追蹤與隱藏房間選項。關閉後使用遊戲原有地圖，並保留隱藏房間偏好。" },
            ["ko-KR"] = new[] { "숨겨진 방 표시", "지원되는 지도와 현재 층 지도 오버레이에 아직 발견하지 못한 숨겨진 방을 표시합니다. 기본적으로 꺼져 있으며, 켜면 비밀 위치가 미리 드러납니다.", "끄기", "켜기", "지도 개선", "지도 도구, 현재 층 지도 오버레이, NPC 추적 및 숨겨진 방 옵션을 활성화합니다. 끄면 기존 지도를 사용하며 숨겨진 방 설정은 유지됩니다." },
            ["ja-JP"] = new[] { "隠し部屋を表示", "対応するマップと現在のフロアマップに未発見の隠し部屋を表示します。初期設定はオフです。有効にすると秘密の場所が先に分かります。", "オフ", "オン", "マップ拡張", "マップツール、現在のフロアマップ、NPC追跡、隠し部屋の設定を有効にします。オフにすると元のマップに戻ります。隠し部屋の設定は保持されます。" },
            ["de-DE"] = new[] { "Geheime Räume anzeigen", "Zeigt unentdeckte Geheimräume auf unterstützten Karten und im Karten-Overlay der aktuellen Ebene. Standardmäßig aus; verrät geheime Orte vorzeitig.", "Aus", "Ein", "Kartenerweiterungen", "Aktiviert Kartenwerkzeuge, Ebenen-Overlay, NPC-Verfolgung und die Geheimraumoption. Ausschalten stellt die ursprüngliche Karte wieder her. Die Geheimraumeinstellung bleibt gespeichert." },
            ["es-ES"] = new[] { "Mostrar salas ocultas",
                    "Muestra salas ocultas aún sin descubrir en los mapas compatibles y en la superposición de la planta actual. Desactivado por defecto; revela lugares secretos antes de tiempo.",
                    "Desactivado",
                    "Activado", "Mejoras del mapa", "Activa las herramientas del mapa, la superposición de la planta, el seguimiento de personajes y la opción de salas ocultas. Desactívalo para usar el mapa original. Se conserva la preferencia de salas ocultas." },
            ["fr-FR"] = new[] { "Afficher les salles cachées",
                    "Affiche les salles cachées non découvertes sur les cartes compatibles et la carte superposée de l’étage actuel. Désactivé par défaut ; révèle les lieux secrets à l’avance.",
                    "Désactivé",
                    "Activé", "Améliorations de la carte", "Active les outils de carte, la carte superposée de l’étage, le suivi des PNJ et l’option des salles cachées. Désactivez pour retrouver la carte d’origine. Le choix des salles cachées est conservé." },
            ["it-IT"] = new[] { "Mostra stanze nascoste",
                    "Mostra le stanze nascoste non ancora scoperte sulle mappe supportate e sulla mappa sovrapposta del piano attuale. Disattivato per impostazione predefinita; rivela in anticipo i luoghi segreti.",
                    "Disattivato",
                    "Attivato", "Migliorie della mappa", "Attiva gli strumenti della mappa, la mappa sovrapposta del piano, il tracciamento dei PNG e l’opzione delle stanze nascoste. Disattiva per usare la mappa originale. La preferenza delle stanze nascoste viene mantenuta." },
            ["pl-PL"] = new[] { "Pokaż ukryte pomieszczenia", "Pokazuje nieodkryte ukryte pomieszczenia na obsługiwanych mapach i nakładce bieżącego piętra. Domyślnie wyłączone; ujawnia sekretne miejsca z wyprzedzeniem.", "Wył.", "Wł.", "Ulepszenia mapy", "Włącza narzędzia mapy, nakładkę piętra, śledzenie postaci niezależnych i opcję ukrytych pomieszczeń. Wyłącz, aby używać oryginalnej mapy. Ustawienie ukrytych pomieszczeń zostanie zachowane." },
            ["pt-BR"] = new[] { "Mostrar salas ocultas",
                    "Mostra salas ocultas ainda não descobertas nos mapas compatíveis e na sobreposição do andar atual. Desativado por padrão; revela locais secretos antecipadamente.",
                    "Desativado",
                    "Ativado", "Melhorias do mapa", "Ativa as ferramentas do mapa, a sobreposição do andar, o rastreamento de personagens e a opção de salas ocultas. Desative para usar o mapa original. A preferência de salas ocultas é mantida." },
            ["ru-RU"] = new[] { "Показывать тайные комнаты",
                    "Показывает ещё не найденные тайные комнаты на поддерживаемых картах и наложении карты текущего этажа. По умолчанию отключено; заранее раскрывает секретные места.",
                    "Выкл.",
                    "Вкл.", "Улучшения карты", "Включает инструменты карты, наложение текущего этажа, отслеживание персонажей и настройку тайных комнат. Отключите для возврата к исходной карте. Настройка тайных комнат сохраняется." },
            ["sv-SE"] = new[] { "Visa dolda rum", "Visar oupptäckta dolda rum på kartor som stöds och i kartöverlägget för aktuell våning. Av som standard; avslöjar hemliga platser i förväg.", "Av", "På", "Kartförbättringar", "Aktiverar kartverktyg, våningens kartöverlägg, spårning av figurer och alternativet för dolda rum. Stäng av för att använda originalkartan. Inställningen för dolda rum sparas." },
            ["th-TH"] = new[] { "แสดงห้องลับ", "แสดงห้องลับที่ยังไม่ค้นพบบนแผนที่ที่รองรับและแผนที่ซ้อนทับของชั้นปัจจุบัน ปิดไว้ตามค่าเริ่มต้น การเปิดจะเผยตำแหน่งลับล่วงหน้า", "ปิด", "เปิด", "ปรับปรุงแผนที่", "เปิดเครื่องมือแผนที่ แผนที่ซ้อนทับของชั้นปัจจุบัน การติดตามตัวละคร และตัวเลือกห้องลับ ปิดเพื่อใช้แผนที่เดิม โดยจะเก็บการตั้งค่าห้องลับไว้" },
            ["tr-TR"] = new[] { "Gizli odaları göster",
                    "Desteklenen haritalarda ve mevcut kat haritası kaplamasında keşfedilmemiş gizli odaları gösterir. Varsayılan olarak kapalıdır; gizli yerleri önceden açığa çıkarır.",
                    "Kapalı",
                    "Açık", "Harita iyileştirmeleri", "Harita araçlarını, mevcut kat kaplamasını, karakter takibini ve gizli oda seçeneğini etkinleştirir. Özgün haritayı kullanmak için kapatın. Gizli oda tercihi korunur." }
        };

        internal static void Register(Action<string, string, string> addText,
            IEnumerable<string> languages)
        {
            Configuration.LocalizationGroup.Register(addText, languages,
                new[] { SettingShowHiddenRooms, HelpShowHiddenRooms, Off, On, SettingEnabled, HelpEnabled }, Texts);
        }
    }
}
