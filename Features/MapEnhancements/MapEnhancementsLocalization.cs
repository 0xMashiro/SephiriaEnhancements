using System;
using System.Collections.Generic;

namespace SephiriaEnhancements.MapEnhancements
{
    internal static class MapEnhancementsLocalization
    {
        internal const string SettingShowHiddenRooms =
            "SephiriaEnhancements.MapEnhancements.Setting.ShowHiddenRooms";
        internal const string HelpShowHiddenRooms =
            "SephiriaEnhancements.MapEnhancements.Help.ShowHiddenRooms";
        internal const string Off = "SephiriaEnhancements.MapEnhancements.Off";
        internal const string On = "SephiriaEnhancements.MapEnhancements.On";

        private static readonly Dictionary<string, string[]> Texts = new()
        {
            ["en-US"] = new[] { "Show hidden rooms", "Show undiscovered hidden rooms on supported maps and the current-floor overlay. Disabled by default; enabling this reveals secret locations early.", "Off", "On" },
            ["zh-CN"] = new[] { "显示隐藏房间", "在支持的地图及本层地图叠加层中显示尚未发现的隐藏房间。默认关闭；开启会提前揭示秘密位置。", "关闭", "开启" },
            ["zh-TW"] = new[] { "顯示隱藏房間", "在支援的地圖及本層地圖疊加層中顯示尚未發現的隱藏房間。預設關閉；開啟會提前揭示秘密位置。", "關閉", "開啟" },
            ["ko-KR"] = new[] { "숨겨진 방 표시", "지원되는 지도와 현재 층 지도 오버레이에 아직 발견하지 못한 숨겨진 방을 표시합니다. 기본적으로 꺼져 있으며, 켜면 비밀 위치가 미리 드러납니다.", "끄기", "켜기" },
            ["ja-JP"] = new[] { "隠し部屋を表示", "対応するマップと現在のフロアマップに未発見の隠し部屋を表示します。初期設定はオフです。有効にすると秘密の場所が先に分かります。", "オフ", "オン" },
            ["de-DE"] = new[] { "Geheime Räume anzeigen", "Zeigt unentdeckte Geheimräume auf unterstützten Karten und im Karten-Overlay der aktuellen Ebene. Standardmäßig aus; verrät geheime Orte vorzeitig.", "Aus", "Ein" },
            ["es-ES"] = new[] { "Mostrar salas ocultas",
                    "Muestra salas ocultas aún sin descubrir en los mapas compatibles y en la superposición de la planta actual. Desactivado por defecto; revela lugares secretos antes de tiempo.",
                    "Desactivado",
                    "Activado" },
            ["fr-FR"] = new[] { "Afficher les salles cachées",
                    "Affiche les salles cachées non découvertes sur les cartes compatibles et la carte superposée de l’étage actuel. Désactivé par défaut ; révèle les lieux secrets à l’avance.",
                    "Désactivé",
                    "Activé" },
            ["it-IT"] = new[] { "Mostra stanze nascoste",
                    "Mostra le stanze nascoste non ancora scoperte sulle mappe supportate e sulla mappa sovrapposta del piano attuale. Disattivato per impostazione predefinita; rivela in anticipo i luoghi segreti.",
                    "Disattivato",
                    "Attivato" },
            ["pl-PL"] = new[] { "Pokaż ukryte pomieszczenia", "Pokazuje nieodkryte ukryte pomieszczenia na obsługiwanych mapach i nakładce bieżącego piętra. Domyślnie wyłączone; ujawnia sekretne miejsca z wyprzedzeniem.", "Wył.", "Wł." },
            ["pt-BR"] = new[] { "Mostrar salas ocultas",
                    "Mostra salas ocultas ainda não descobertas nos mapas compatíveis e na sobreposição do andar atual. Desativado por padrão; revela locais secretos antecipadamente.",
                    "Desativado",
                    "Ativado" },
            ["ru-RU"] = new[] { "Показывать тайные комнаты",
                    "Показывает ещё не найденные тайные комнаты на поддерживаемых картах и наложении карты текущего этажа. По умолчанию отключено; заранее раскрывает секретные места.",
                    "Выкл.",
                    "Вкл." },
            ["sv-SE"] = new[] { "Visa dolda rum", "Visar oupptäckta dolda rum på kartor som stöds och i kartöverlägget för aktuell våning. Av som standard; avslöjar hemliga platser i förväg.", "Av", "På" },
            ["th-TH"] = new[] { "แสดงห้องลับ", "แสดงห้องลับที่ยังไม่ค้นพบบนแผนที่ที่รองรับและแผนที่ซ้อนทับของชั้นปัจจุบัน ปิดไว้ตามค่าเริ่มต้น การเปิดจะเผยตำแหน่งลับล่วงหน้า", "ปิด", "เปิด" },
            ["tr-TR"] = new[] { "Gizli odaları göster",
                    "Desteklenen haritalarda ve mevcut kat haritası kaplamasında keşfedilmemiş gizli odaları gösterir. Varsayılan olarak kapalıdır; gizli yerleri önceden açığa çıkarır.",
                    "Kapalı",
                    "Açık" }
        };

        internal static void Register(Action<string, string, string> addText,
            IEnumerable<string> languages)
        {
            foreach (string language in languages)
            {
                string[] texts = Texts.TryGetValue(language, out var translated)
                    ? translated : Texts["en-US"];
                addText(language, SettingShowHiddenRooms, texts[0]);
                addText(language, HelpShowHiddenRooms, texts[1]);
                addText(language, Off, texts[2]);
                addText(language, On, texts[3]);
            }
        }
    }
}
