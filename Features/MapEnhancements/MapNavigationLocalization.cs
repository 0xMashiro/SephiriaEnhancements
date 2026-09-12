using System;
using System.Collections.Generic;

namespace SephiriaEnhancements.MapEnhancements
{
    internal static class MapNavigationLocalization
    {
        private const string Prefix = "SephiriaEnhancements.MapNavigation.";
        internal const string FloorEntrance = Prefix + "FloorEntrance";
        internal const string FloorExit = Prefix + "FloorExit";
        internal const string Home = Prefix + "Home";
        internal const string Weapons = Prefix + "Weapons";
        internal const string Departure = Prefix + "Departure";
        internal const string MultiplayerGate = Prefix + "MultiplayerGate";
        internal const string TownReturnPortal = Prefix + "TownReturnPortal";
        internal const string Tree = Prefix + "Tree";
        internal const string DestinationTravel = Prefix + "DestinationTravel";
        internal const string TravelUnavailable = Prefix + "TravelUnavailable";
        internal const string MapNotReady = Prefix + "MapNotReady";
        internal const string NoLanding = Prefix + "NoLanding";
        internal const string Rooms = Prefix + "Rooms";
        internal const string SelectRoomGuide = Prefix + "SelectRoomGuide";
        internal const string People = Prefix + "People";
        internal const string Places = Prefix + "Places";
        internal const string Fit = Prefix + "Fit";
        internal const string Travel = Prefix + "Travel";
        internal const string RoomTravel = Prefix + "RoomTravel";
        internal const string Empty = Prefix + "Empty";
        internal const string Browse = Prefix + "Browse";
        internal const string Guide = Prefix + "Guide";
        internal const string PanGuide = Prefix + "PanGuide";

        internal const string Track = Prefix + "Track";
        internal const string Untrack = Prefix + "Untrack";

        internal const string ChooseTarget = Prefix + "ChooseTarget";
        internal const string ChooseTargetGuide = Prefix + "ChooseTargetGuide";
        internal const string PointerGuide = Prefix + "PointerGuide";

        private static readonly Dictionary<string, string[]> Texts = new()
        {
            ["en-US"] = new[] { "People", "Places", "Fit map", "Travel near {0}", "No visible destinations", "Browse map", "Navigate to locate · {0}: travel", "Navigate to pan · {0}: return to list", "Track NPC", "Stop tracking", "Travel to room: {0}", "Rooms", "Navigate to select a room · {0}: travel", "Home", "Weapon selection", "Departure point", "Multiplayer gate", "Tree", "Travel to {0}", "Cannot travel right now", "Waiting for the area to load", "No nearby landing spot found", "Portal to your town", "Choose a target", "Choose a target to preview its location and travel action", "Hover or navigate to locate · Click or {0}: travel", "Floor entrance", "Floor exit" },
            ["zh-CN"] = new[] { "人物", "地点", "全图", "传送到「{0}」附近", "暂无可显示的目标", "浏览地图", "方向导航定位 · {0}：传送", "方向导航平移 · {0}：返回列表", "跟踪人物", "取消跟踪", "移动至「{0}」所在房间", "房间", "方向键选择房间 · {0}：传送", "家", "武器选择", "出发点", "联机入口", "大树", "传送到「{0}」", "暂时无法传送", "等待区域加载完成", "未找到附近可站立的位置", "返乡传送门", "请选择目标", "选择目标，查看位置与传送方式", "悬停或方向导航定位 · 单击或 {0}：传送", "楼层入口", "楼层出口" },
            ["zh-TW"] = new[] { "人物", "地點", "全圖", "傳送到「{0}」附近", "暫無可顯示的目標", "瀏覽地圖", "方向導覽定位 · {0}：傳送", "方向導覽平移 · {0}：返回清單", "追蹤人物", "取消追蹤", "移動至「{0}」所在房間", "房間", "方向鍵選擇房間 · {0}：傳送", "家", "武器選擇", "出發點", "連線入口", "大樹", "傳送到「{0}」", "暫時無法傳送", "等待區域載入完成", "未找到附近可站立的位置", "返鄉傳送門", "請選擇目標", "選擇目標，查看位置與傳送方式", "懸停或方向導覽定位 · 點擊或 {0}：傳送", "樓層入口", "樓層出口" },
            ["ko-KR"] = new[] { "인물", "장소", "전체 지도", "{0} 근처로 이동", "표시할 대상이 없습니다", "지도 탐색", "방향 입력으로 위치 확인 · {0}: 이동", "방향 입력으로 지도 이동 · {0}: 목록으로", "인물 추적", "추적 해제", "{0}이 있는 방으로 이동", "방", "방향 입력으로 방 선택 · {0}: 이동", "집", "무기 선택", "출발 지점", "멀티플레이 입구", "나무", "{0}(으)로 이동", "지금은 이동할 수 없습니다", "지역을 불러오는 중", "주변에서 설 수 있는 곳을 찾지 못했습니다", "내 마을로 가는 포털", "대상을 선택하세요", "대상을 선택해 위치와 이동 방식을 확인하세요", "마우스 또는 방향 입력으로 위치 확인 · 클릭 또는 {0}: 이동", "층 입구", "층 출구" },
            ["ja-JP"] = new[] { "人物", "場所", "全体表示", "「{0}」の近くへ転送", "表示できる対象がありません", "地図を閲覧", "方向入力で位置を表示 · {0}: 転送", "方向入力で地図移動 · {0}: 一覧に戻る", "人物を追跡", "追跡解除", "「{0}」のいる部屋へ移動", "部屋", "方向入力で部屋を選択 · {0}: 移動", "家", "武器選択", "出発地点", "マルチプレイの入口", "大樹", "「{0}」へ転送", "今は転送できません", "エリアの読み込み待ち", "近くに立てる場所が見つかりません", "自分の町へのポータル", "対象を選択", "対象を選ぶと位置と転送方法を表示", "カーソルか方向入力で位置を表示 · クリックか{0}: 転送", "フロア入口", "フロア出口" },
            ["de-DE"] = new[] { "Personen", "Orte", "Gesamtkarte", "In die Nähe von {0} reisen", "Keine sichtbaren Ziele", "Karte bewegen", "Navigation zeigt Position · {0}: reisen", "Navigation verschiebt Karte · {0}: zurück zur Liste", "Person folgen", "Nicht folgen", "Zum Raum reisen: {0}", "Räume", "Raum mit Richtungseingabe wählen · {0}: reisen", "Zuhause", "Waffenauswahl", "Aufbruchsort", "Mehrspieler-Tor", "Baum", "Zu {0} reisen", "Reisen derzeit nicht möglich", "Warten, bis das Gebiet geladen ist", "Kein freier Platz in der Nähe gefunden", "Portal zur eigenen Stadt", "Ziel wählen", "Ziel wählen, um Position und Reiseart zu sehen", "Zeigen oder navigieren für Position · Klick oder {0}: reisen", "Etageneingang", "Etagenausgang" },
            ["es-ES"] = new[] { "Personajes", "Lugares", "Ver mapa", "Viajar cerca de {0}", "No hay destinos visibles", "Explorar mapa", "Navega para localizar · {0}: viajar", "Navega para desplazar · {0}: volver a la lista", "Seguir", "Dejar de seguir", "Viajar a la sala de {0}", "Salas", "Usa la dirección para elegir sala · {0}: viajar", "Casa", "Selección de armas", "Punto de partida", "Acceso multijugador", "Árbol", "Viajar a {0}", "No puedes viajar ahora", "Esperando a que cargue la zona", "No se encontró un lugar libre cerca", "Portal a tu pueblo", "Elige un destino", "Elige un destino para ver su ubicación y cómo viajar", "Señala o navega para localizar · Clic o {0}: viajar", "Entrada de la planta", "Salida de la planta" },
            ["fr-FR"] = new[] { "Personnages", "Lieux", "Vue globale", "Rejoindre les environs de {0}", "Aucune destination visible", "Parcourir", "Navigation pour localiser · {0} : voyager", "Navigation pour déplacer · {0} : retour à la liste", "Suivre", "Ne plus suivre", "Rejoindre la salle de {0}", "Salles", "Navigation pour choisir une salle · {0} : voyager", "Maison", "Choix des armes", "Point de départ", "Accès multijoueur", "Arbre", "Rejoindre {0}", "Déplacement impossible pour le moment", "En attente du chargement de la zone", "Aucun emplacement libre trouvé à proximité", "Portail vers votre village", "Choisir une cible", "Choisissez une cible pour voir sa position et le déplacement proposé", "Survol ou navigation pour localiser · Clic ou {0} : voyager", "Entrée de l’étage", "Sortie de l’étage" },
            ["it-IT"] = new[] { "Personaggi", "Luoghi", "Mappa intera", "Vai vicino a {0}", "Nessuna destinazione visibile", "Esplora mappa", "Naviga per localizzare · {0}: viaggia", "Naviga per spostare · {0}: torna alla lista", "Segui", "Smetti di seguire", "Vai alla stanza di {0}", "Stanze", "Usa le direzioni per scegliere · {0}: viaggia", "Casa", "Scelta delle armi", "Punto di partenza", "Accesso multigiocatore", "Albero", "Vai a {0}", "Non puoi viaggiare ora", "In attesa del caricamento della zona", "Nessun punto libero trovato nelle vicinanze", "Portale per il tuo villaggio", "Scegli una destinazione", "Scegli una destinazione per vederne la posizione e lo spostamento", "Punta o naviga per localizzare · Clic o {0}: viaggia", "Ingresso del piano", "Uscita del piano" },
            ["pl-PL"] = new[] { "Postacie", "Miejsca", "Cała mapa", "Przenieś się w pobliże: {0}", "Brak widocznych celów", "Przeglądaj", "Nawiguj, aby wskazać cel · {0}: podróż", "Nawigacja przesuwa mapę · {0}: wróć do listy", "Śledź postać", "Przestań śledzić", "Przenieś się do pokoju: {0}", "Pokoje", "Wybierz pokój kierunkiem · {0}: podróż", "Dom", "Wybór broni", "Punkt wyruszenia", "Brama gry wieloosobowej", "Drzewo", "Przenieś się do: {0}", "Nie można teraz się przenieść", "Oczekiwanie na wczytanie obszaru", "Nie znaleziono wolnego miejsca w pobliżu", "Portal do własnej wioski", "Wybierz cel", "Wybierz cel, aby zobaczyć jego pozycję i sposób podróży", "Wskaż lub nawiguj, aby znaleźć cel · Kliknij lub {0}: podróż", "Wejście na piętro", "Wyjście z piętra" },
            ["pt-BR"] = new[] { "Personagens", "Locais", "Mapa inteiro", "Viajar para perto de {0}", "Nenhum destino visível", "Explorar mapa", "Navegue para localizar · {0}: viajar", "Navegue para mover · {0}: voltar à lista", "Rastrear", "Parar de rastrear", "Viajar à sala de {0}", "Salas", "Use as direções para selecionar · {0}: viajar", "Casa", "Seleção de armas", "Ponto de partida", "Entrada multijogador", "Árvore", "Viajar para {0}", "Não é possível viajar agora", "Aguardando o carregamento da área", "Nenhum espaço livre encontrado por perto", "Portal para sua vila", "Escolha um destino", "Escolha um destino para ver sua localização e como viajar", "Aponte ou navegue para localizar · Clique ou {0}: viajar", "Entrada do andar", "Saída do andar" },
            ["ru-RU"] = new[] { "Персонажи", "Места", "Вся карта", "Переместиться поближе к {0}", "Нет видимых целей", "Обзор карты", "Навигация указывает цель · {0}: переместиться", "Навигация сдвигает карту · {0}: к списку", "Отслеживать", "Не отслеживать", "Переместиться в комнату: {0}", "Комнаты", "Выберите комнату направлением · {0}: переместиться", "Дом", "Выбор оружия", "Место отправления", "Вход в сетевую игру", "Дерево", "Переместиться к {0}", "Сейчас перемещение недоступно", "Ожидание загрузки области", "Рядом не найдено свободного места", "Портал в свой город", "Выберите цель", "Выберите цель, чтобы увидеть её положение и способ перемещения", "Наведение или навигация указывает цель · Щелчок или {0}: переместиться", "Вход на этаж", "Выход с этажа" },
            ["sv-SE"] = new[] { "Personer", "Platser", "Hela kartan", "Res nära {0}", "Inga synliga mål", "Utforska karta", "Navigera för att hitta · {0}: res", "Navigera för att flytta kartan · {0}: tillbaka till listan", "Spåra person", "Sluta spåra", "Res till rummet med {0}", "Rum", "Välj rum med riktning · {0}: res", "Hem", "Vapenval", "Avreseplats", "Flerspelarport", "Träd", "Res till {0}", "Det går inte att resa just nu", "Väntar på att området ska laddas", "Ingen ledig plats hittades i närheten", "Portal till din by", "Välj ett mål", "Välj ett mål för att se dess plats och resesätt", "Peka eller navigera för att hitta · Klicka eller {0}: res", "Våningsingång", "Våningsutgång" },
            ["th-TH"] = new[] { "ตัวละคร", "สถานที่", "แผนที่ทั้งหมด", "เคลื่อนย้ายไปใกล้ {0}", "ไม่มีเป้าหมายที่แสดงได้", "ดูแผนที่", "ใช้ทิศทางระบุตำแหน่ง · {0}: เคลื่อนย้าย", "ใช้ทิศทางเลื่อนแผนที่ · {0}: กลับรายการ", "ติดตามตัวละคร", "เลิกติดตาม", "ไปยังห้องของ {0}", "ห้อง", "ใช้ทิศทางเพื่อเลือกห้อง · {0}: เดินทาง", "บ้าน", "เลือกอาวุธ", "จุดออกเดินทาง", "ประตูเล่นหลายคน", "ต้นไม้", "เคลื่อนย้ายไปยัง {0}", "ยังไม่สามารถเคลื่อนย้ายได้", "รอให้โหลดพื้นที่เสร็จ", "ไม่พบจุดยืนที่ว่างในบริเวณใกล้เคียง", "ประตูกลับเมืองของคุณ", "เลือกเป้าหมาย", "เลือกเป้าหมายเพื่อดูตำแหน่งและวิธีเคลื่อนย้าย", "ชี้เมาส์หรือใช้ทิศทางระบุตำแหน่ง · คลิกหรือ {0}: เคลื่อนย้าย", "ทางเข้าชั้น", "ทางออกชั้น" },
            ["tr-TR"] = new[] { "Kişiler", "Yerler", "Tüm harita", "{0} yakınına ışınlan", "Görünür hedef yok", "Haritayı gez", "Yönlerle konumu bul · {0}: ışınlan", "Yönlerle haritayı kaydır · {0}: listeye dön", "Kişiyi takip et", "Takibi bırak", "{0} odasına git", "Odalar", "Yönlerle oda seç · {0}: seyahat et", "Ev", "Silah seçimi", "Yola çıkış noktası", "Çok oyunculu giriş", "Ağaç", "{0} konumuna ışınlan", "Şu anda ışınlanamazsın", "Bölgenin yüklenmesi bekleniyor", "Yakında durulabilecek boş yer bulunamadı", "Kendi kasabana açılan portal", "Bir hedef seç", "Konumunu ve yolculuk biçimini görmek için hedef seç", "İşaretçi veya yönlerle konumu bul · Tıkla veya {0}: ışınlan", "Kat girişi", "Kat çıkışı" }
        };

        internal static void Register(Action<string, string, string> add,
            IEnumerable<string> languages)
        {
            Configuration.LocalizationGroup.Register(add, languages,
                new[] { People, Places, Fit, Travel, Empty, Browse, Guide, PanGuide, Track, Untrack, RoomTravel, Rooms, SelectRoomGuide, Home, Weapons, Departure, MultiplayerGate, Tree, DestinationTravel, TravelUnavailable, MapNotReady, NoLanding, TownReturnPortal, ChooseTarget, ChooseTargetGuide, PointerGuide, FloorEntrance, FloorExit }, Texts);
        }
    }
}
