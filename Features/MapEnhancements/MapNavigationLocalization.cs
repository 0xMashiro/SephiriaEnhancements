using System;
using System.Collections.Generic;

namespace SephiriaEnhancements.MapEnhancements
{
    internal static class MapNavigationLocalization
    {
        private const string Prefix = "SephiriaEnhancements.MapNavigation.";
        internal const string Rooms = Prefix + "Rooms";
        internal const string SelectRoomGuide = Prefix + "SelectRoomGuide";
        internal const string People = Prefix + "People";
        internal const string Places = Prefix + "Places";
        internal const string Fit = Prefix + "Fit";
        internal const string Travel = Prefix + "Travel";
        internal const string RoomTravel = Prefix + "RoomTravel";
        internal const string RoomGuide = Prefix + "RoomGuide";
        internal const string Empty = Prefix + "Empty";
        internal const string Browse = Prefix + "Browse";
        internal const string Guide = Prefix + "Guide";
        internal const string PanGuide = Prefix + "PanGuide";

        internal const string Track = Prefix + "Track";
        internal const string Untrack = Prefix + "Untrack";

        private static readonly Dictionary<string, string[]> Texts = new()
        {
            ["en-US"] = new[] { "People", "Places", "Fit map", "Travel to point nearest target", "No visible destinations", "Browse map", "Select to locate · {0}: travel to a nearby teleport point", "Navigate to pan · {0}: return to list", "Track NPC", "Stop tracking", "Travel to target room", "Select to locate · {0}: travel to target room", "Rooms", "Navigate to select a room · {0}: travel" },
            ["zh-CN"] = new[] { "人物", "地点", "全图", "移动至离目标最近的传送点", "暂无可显示的目标", "浏览地图", "选择目标定位 · {0}：传送至附近传送点", "方向导航平移 · {0}：返回列表", "跟踪人物", "取消跟踪", "移动至目标所在房间", "选择目标定位 · {0}：移动至目标所在房间", "房间", "方向键选择房间 · {0}：传送" },
            ["zh-TW"] = new[] { "人物", "地點", "全圖", "移動至離目標最近的傳送點", "暫無可顯示的目標", "瀏覽地圖", "選擇目標定位 · {0}：傳送至附近傳送點", "方向導覽平移 · {0}：返回清單", "追蹤人物", "取消追蹤", "移動至目標所在房間", "選擇目標定位 · {0}：移動至目標所在房間", "房間", "方向鍵選擇房間 · {0}：傳送" },
            ["ko-KR"] = new[] { "인물", "장소", "전체 지도", "대상에서 가장 가까운 이동 지점으로 이동", "표시할 대상이 없습니다", "지도 탐색", "선택하여 위치 확인 · {0}: 가까운 이동 지점으로 이동", "방향 입력으로 지도 이동 · {0}: 목록으로", "인물 추적", "추적 해제", "대상이 있는 방으로 이동", "선택하여 위치 확인 · {0}: 대상의 방으로 이동", "방", "방향 입력으로 방 선택 · {0}: 이동" },
            ["ja-JP"] = new[] { "人物", "場所", "全体表示", "対象に最も近い転送地点へ移動", "表示できる対象がありません", "地図を閲覧", "選択で位置を表示 · {0}: 近くの転送地点へ移動", "方向入力で地図移動 · {0}: 一覧に戻る", "人物を追跡", "追跡解除", "対象のいる部屋へ移動", "選択で位置を表示 · {0}: 対象の部屋へ移動", "部屋", "方向入力で部屋を選択 · {0}: 移動" },
            ["de-DE"] = new[] { "Personen", "Orte", "Gesamtkarte", "Zum Reisepunkt nächst dem Ziel", "Keine sichtbaren Ziele", "Karte bewegen", "Auswahl zeigt Position · {0}: zum nahen Reisepunkt", "Navigation verschiebt Karte · {0}: zurück zur Liste", "Person folgen", "Nicht folgen", "Zum Raum des Ziels reisen", "Auswahl zeigt Position · {0}: zum Zielraum reisen", "Räume", "Raum mit Richtungseingabe wählen · {0}: reisen" },
            ["es-ES"] = new[] { "Personajes", "Lugares", "Ver mapa", "Viajar al punto más cercano al objetivo", "No hay destinos visibles", "Explorar mapa", "Selecciona para localizar · {0}: viajar al punto cercano", "Navega para desplazar · {0}: volver a la lista", "Seguir", "Dejar de seguir", "Viajar a la sala del objetivo", "Selecciona para localizar · {0}: viajar a su sala", "Salas", "Usa la dirección para elegir sala · {0}: viajar" },
            ["fr-FR"] = new[] { "Personnages", "Lieux", "Vue globale", "Rejoindre le point le plus proche de la cible", "Aucune destination visible", "Parcourir", "Sélectionner pour localiser · {0} : point de voyage proche", "Navigation pour déplacer · {0} : retour à la liste", "Suivre", "Ne plus suivre", "Rejoindre la salle de la cible", "Sélectionner pour localiser · {0} : rejoindre sa salle", "Salles", "Navigation pour choisir une salle · {0} : voyager" },
            ["it-IT"] = new[] { "Personaggi", "Luoghi", "Mappa intera", "Vai al punto più vicino alla destinazione", "Nessuna destinazione visibile", "Esplora mappa", "Seleziona per localizzare · {0}: punto di viaggio vicino", "Naviga per spostare · {0}: torna alla lista", "Segui", "Smetti di seguire", "Vai alla stanza della destinazione", "Seleziona per localizzare · {0}: vai alla sua stanza", "Stanze", "Usa le direzioni per scegliere · {0}: viaggia" },
            ["pl-PL"] = new[] { "Postacie", "Miejsca", "Cała mapa", "Przenieś się do punktu najbliżej celu", "Brak widocznych celów", "Przeglądaj", "Wybór wskazuje cel · {0}: pobliski punkt podróży", "Nawigacja przesuwa mapę · {0}: wróć do listy", "Śledź postać", "Przestań śledzić", "Przenieś się do pokoju celu", "Wybierz, aby zlokalizować · {0}: przenieś się do pokoju celu", "Pokoje", "Wybierz pokój kierunkiem · {0}: podróż" },
            ["pt-BR"] = new[] { "Personagens", "Locais", "Mapa inteiro", "Viajar ao ponto mais próximo do alvo", "Nenhum destino visível", "Explorar mapa", "Selecione para localizar · {0}: ponto de viagem próximo", "Navegue para mover · {0}: voltar à lista", "Rastrear", "Parar de rastrear", "Viajar à sala do alvo", "Selecione para localizar · {0}: viajar à sala do alvo", "Salas", "Use as direções para selecionar · {0}: viajar" },
            ["ru-RU"] = new[] { "Персонажи", "Места", "Вся карта", "К ближайшей к цели точке перемещения", "Нет видимых целей", "Обзор карты", "Выбор показывает цель · {0}: к ближайшей точке перемещения", "Навигация сдвигает карту · {0}: к списку", "Отслеживать", "Не отслеживать", "Переместиться в комнату цели", "Выбор показывает цель · {0}: в комнату цели", "Комнаты", "Выберите комнату направлением · {0}: переместиться" },
            ["sv-SE"] = new[] { "Personer", "Platser", "Hela kartan", "Res till punkten närmast målet", "Inga synliga mål", "Utforska karta", "Välj för att hitta · {0}: res till en närliggande punkt", "Navigera för att flytta kartan · {0}: tillbaka till listan", "Spåra person", "Sluta spåra", "Res till målets rum", "Välj för att hitta · {0}: res till målets rum", "Rum", "Välj rum med riktning · {0}: res" },
            ["th-TH"] = new[] { "ตัวละคร", "สถานที่", "แผนที่ทั้งหมด", "ไปยังจุดเคลื่อนย้ายที่ใกล้เป้าหมายที่สุด", "ไม่มีเป้าหมายที่แสดงได้", "ดูแผนที่", "เลือกเพื่อระบุตำแหน่ง · {0}: ไปยังจุดเคลื่อนย้ายใกล้เคียง", "ใช้ทิศทางเลื่อนแผนที่ · {0}: กลับรายการ", "ติดตามตัวละคร", "เลิกติดตาม", "ไปยังห้องของเป้าหมาย", "เลือกเพื่อระบุตำแหน่ง · {0}: ไปยังห้องของเป้าหมาย", "ห้อง", "ใช้ทิศทางเพื่อเลือกห้อง · {0}: เดินทาง" },
            ["tr-TR"] = new[] { "Kişiler", "Yerler", "Tüm harita", "Hedefe en yakın ışınlanma noktasına git", "Görünür hedef yok", "Haritayı gez", "Seçim konumu gösterir · {0}: yakındaki seyahat noktasına git", "Yönlerle haritayı kaydır · {0}: listeye dön", "Kişiyi takip et", "Takibi bırak", "Hedefin odasına git", "Konumu bulmak için seç · {0}: hedefin odasına git", "Odalar", "Yönlerle oda seç · {0}: seyahat et" }
        };

        internal static void Register(Action<string, string, string> add,
            IEnumerable<string> languages)
        {
            Configuration.LocalizationGroup.Register(add, languages,
                new[] { People, Places, Fit, Travel, Empty, Browse, Guide, PanGuide, Track, Untrack, RoomTravel, RoomGuide, Rooms, SelectRoomGuide }, Texts);
        }
    }
}
