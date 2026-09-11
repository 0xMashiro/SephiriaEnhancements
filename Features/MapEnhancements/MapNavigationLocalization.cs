using System;
using System.Collections.Generic;

namespace SephiriaEnhancements.MapEnhancements
{
    internal static class MapNavigationLocalization
    {
        private const string Prefix = "SephiriaEnhancements.MapNavigation.";
        internal const string Home = Prefix + "Home";
        internal const string Weapons = Prefix + "Weapons";
        internal const string Departure = Prefix + "Departure";
        internal const string MultiplayerGate = Prefix + "MultiplayerGate";
        internal const string TownReturnPortal = Prefix + "TownReturnPortal";
        internal const string Tree = Prefix + "Tree";
        internal const string DestinationTravel = Prefix + "DestinationTravel";
        internal const string DestinationGuide = Prefix + "DestinationGuide";
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
        internal const string RoomGuide = Prefix + "RoomGuide";
        internal const string Empty = Prefix + "Empty";
        internal const string Browse = Prefix + "Browse";
        internal const string Guide = Prefix + "Guide";
        internal const string PanGuide = Prefix + "PanGuide";

        internal const string Track = Prefix + "Track";
        internal const string Untrack = Prefix + "Untrack";

        private static readonly Dictionary<string, string[]> Texts = new()
        {
            ["en-US"] = new[] { "People", "Places", "Fit map", "Travel near the target", "No visible destinations", "Browse map", "Select to locate · {0}: travel near the target", "Navigate to pan · {0}: return to list", "Track NPC", "Stop tracking", "Travel to target room", "Select to locate · {0}: travel to target room", "Rooms", "Navigate to select a room · {0}: travel", "Home", "Weapon selection", "Departure point", "Multiplayer gate", "Tree", "Travel to this location", "Select to locate · {0}: travel to this location", "Cannot travel right now", "Waiting for the area to load", "No nearby landing spot found", "Portal to your town" },
            ["zh-CN"] = new[] { "人物", "地点", "全图", "传送到目标附近", "暂无可显示的目标", "浏览地图", "选择目标定位 · {0}：传送到目标附近", "方向导航平移 · {0}：返回列表", "跟踪人物", "取消跟踪", "移动至目标所在房间", "选择目标定位 · {0}：移动至目标所在房间", "房间", "方向键选择房间 · {0}：传送", "家", "武器选择", "出发点", "联机入口", "大树", "传送到此地点", "选择目标定位 · {0}：传送到此地点", "暂时无法传送", "等待区域加载完成", "未找到附近可站立的位置", "返乡传送门" },
            ["zh-TW"] = new[] { "人物", "地點", "全圖", "傳送到目標附近", "暫無可顯示的目標", "瀏覽地圖", "選擇目標定位 · {0}：傳送到目標附近", "方向導覽平移 · {0}：返回清單", "追蹤人物", "取消追蹤", "移動至目標所在房間", "選擇目標定位 · {0}：移動至目標所在房間", "房間", "方向鍵選擇房間 · {0}：傳送", "家", "武器選擇", "出發點", "連線入口", "大樹", "傳送到此地點", "選擇目標定位 · {0}：傳送到此地點", "暫時無法傳送", "等待區域載入完成", "未找到附近可站立的位置", "返鄉傳送門" },
            ["ko-KR"] = new[] { "인물", "장소", "전체 지도", "대상 근처로 이동", "표시할 대상이 없습니다", "지도 탐색", "선택하여 위치 확인 · {0}: 대상 근처로 이동", "방향 입력으로 지도 이동 · {0}: 목록으로", "인물 추적", "추적 해제", "대상이 있는 방으로 이동", "선택하여 위치 확인 · {0}: 대상의 방으로 이동", "방", "방향 입력으로 방 선택 · {0}: 이동", "집", "무기 선택", "출발 지점", "멀티플레이 입구", "나무", "이 장소로 이동", "선택하여 위치 확인 · {0}: 이 장소로 이동", "지금은 이동할 수 없습니다", "지역을 불러오는 중", "주변에서 설 수 있는 곳을 찾지 못했습니다", "내 마을로 가는 포털" },
            ["ja-JP"] = new[] { "人物", "場所", "全体表示", "対象の近くへ転送", "表示できる対象がありません", "地図を閲覧", "選択で位置を表示 · {0}: 対象の近くへ転送", "方向入力で地図移動 · {0}: 一覧に戻る", "人物を追跡", "追跡解除", "対象のいる部屋へ移動", "選択で位置を表示 · {0}: 対象の部屋へ移動", "部屋", "方向入力で部屋を選択 · {0}: 移動", "家", "武器選択", "出発地点", "マルチプレイの入口", "大樹", "この場所へ転送", "選択で位置を表示 · {0}: この場所へ転送", "今は転送できません", "エリアの読み込み待ち", "近くに立てる場所が見つかりません", "自分の町へのポータル" },
            ["de-DE"] = new[] { "Personen", "Orte", "Gesamtkarte", "In die Nähe des Ziels reisen", "Keine sichtbaren Ziele", "Karte bewegen", "Auswahl zeigt Position · {0}: in die Nähe des Ziels reisen", "Navigation verschiebt Karte · {0}: zurück zur Liste", "Person folgen", "Nicht folgen", "Zum Raum des Ziels reisen", "Auswahl zeigt Position · {0}: zum Zielraum reisen", "Räume", "Raum mit Richtungseingabe wählen · {0}: reisen", "Zuhause", "Waffenauswahl", "Aufbruchsort", "Mehrspieler-Tor", "Baum", "Zu diesem Ort reisen", "Auswahl zeigt Position · {0}: zu diesem Ort reisen", "Reisen derzeit nicht möglich", "Warten, bis das Gebiet geladen ist", "Kein freier Platz in der Nähe gefunden", "Portal zur eigenen Stadt" },
            ["es-ES"] = new[] { "Personajes", "Lugares", "Ver mapa", "Viajar cerca del objetivo", "No hay destinos visibles", "Explorar mapa", "Selecciona para localizar · {0}: viajar cerca del objetivo", "Navega para desplazar · {0}: volver a la lista", "Seguir", "Dejar de seguir", "Viajar a la sala del objetivo", "Selecciona para localizar · {0}: viajar a su sala", "Salas", "Usa la dirección para elegir sala · {0}: viajar", "Casa", "Selección de armas", "Punto de partida", "Acceso multijugador", "Árbol", "Viajar a este lugar", "Selecciona para localizar · {0}: viajar a este lugar", "No puedes viajar ahora", "Esperando a que cargue la zona", "No se encontró un lugar libre cerca", "Portal a tu pueblo" },
            ["fr-FR"] = new[] { "Personnages", "Lieux", "Vue globale", "Rejoindre les environs de la cible", "Aucune destination visible", "Parcourir", "Sélectionner pour localiser · {0} : se rapprocher de la cible", "Navigation pour déplacer · {0} : retour à la liste", "Suivre", "Ne plus suivre", "Rejoindre la salle de la cible", "Sélectionner pour localiser · {0} : rejoindre sa salle", "Salles", "Navigation pour choisir une salle · {0} : voyager", "Maison", "Choix des armes", "Point de départ", "Accès multijoueur", "Arbre", "Rejoindre ce lieu", "Sélectionner pour localiser · {0} : rejoindre ce lieu", "Déplacement impossible pour le moment", "En attente du chargement de la zone", "Aucun emplacement libre trouvé à proximité", "Portail vers votre village" },
            ["it-IT"] = new[] { "Personaggi", "Luoghi", "Mappa intera", "Vai vicino alla destinazione", "Nessuna destinazione visibile", "Esplora mappa", "Seleziona per localizzare · {0}: avvicinati alla destinazione", "Naviga per spostare · {0}: torna alla lista", "Segui", "Smetti di seguire", "Vai alla stanza della destinazione", "Seleziona per localizzare · {0}: vai alla sua stanza", "Stanze", "Usa le direzioni per scegliere · {0}: viaggia", "Casa", "Scelta delle armi", "Punto di partenza", "Accesso multigiocatore", "Albero", "Vai in questo luogo", "Seleziona per localizzare · {0}: vai in questo luogo", "Non puoi viaggiare ora", "In attesa del caricamento della zona", "Nessun punto libero trovato nelle vicinanze", "Portale per il tuo villaggio" },
            ["pl-PL"] = new[] { "Postacie", "Miejsca", "Cała mapa", "Przenieś się w pobliże celu", "Brak widocznych celów", "Przeglądaj", "Wybór wskazuje cel · {0}: przenieś się w pobliże celu", "Nawigacja przesuwa mapę · {0}: wróć do listy", "Śledź postać", "Przestań śledzić", "Przenieś się do pokoju celu", "Wybierz, aby zlokalizować · {0}: przenieś się do pokoju celu", "Pokoje", "Wybierz pokój kierunkiem · {0}: podróż", "Dom", "Wybór broni", "Punkt wyruszenia", "Brama gry wieloosobowej", "Drzewo", "Przenieś się w to miejsce", "Wybór wskazuje cel · {0}: przenieś się w to miejsce", "Nie można teraz się przenieść", "Oczekiwanie na wczytanie obszaru", "Nie znaleziono wolnego miejsca w pobliżu", "Portal do własnej wioski" },
            ["pt-BR"] = new[] { "Personagens", "Locais", "Mapa inteiro", "Viajar para perto do alvo", "Nenhum destino visível", "Explorar mapa", "Selecione para localizar · {0}: viajar para perto do alvo", "Navegue para mover · {0}: voltar à lista", "Rastrear", "Parar de rastrear", "Viajar à sala do alvo", "Selecione para localizar · {0}: viajar à sala do alvo", "Salas", "Use as direções para selecionar · {0}: viajar", "Casa", "Seleção de armas", "Ponto de partida", "Entrada multijogador", "Árvore", "Viajar para este local", "Selecione para localizar · {0}: viajar para este local", "Não é possível viajar agora", "Aguardando o carregamento da área", "Nenhum espaço livre encontrado por perto", "Portal para sua vila" },
            ["ru-RU"] = new[] { "Персонажи", "Места", "Вся карта", "Переместиться поближе к цели", "Нет видимых целей", "Обзор карты", "Выбор показывает цель · {0}: переместиться поближе к цели", "Навигация сдвигает карту · {0}: к списку", "Отслеживать", "Не отслеживать", "Переместиться в комнату цели", "Выбор показывает цель · {0}: в комнату цели", "Комнаты", "Выберите комнату направлением · {0}: переместиться", "Дом", "Выбор оружия", "Место отправления", "Вход в сетевую игру", "Дерево", "Переместиться в это место", "Выбор показывает цель · {0}: переместиться сюда", "Сейчас перемещение недоступно", "Ожидание загрузки области", "Рядом не найдено свободного места", "Портал в свой город" },
            ["sv-SE"] = new[] { "Personer", "Platser", "Hela kartan", "Res nära målet", "Inga synliga mål", "Utforska karta", "Välj för att hitta · {0}: res nära målet", "Navigera för att flytta kartan · {0}: tillbaka till listan", "Spåra person", "Sluta spåra", "Res till målets rum", "Välj för att hitta · {0}: res till målets rum", "Rum", "Välj rum med riktning · {0}: res", "Hem", "Vapenval", "Avreseplats", "Flerspelarport", "Träd", "Res till denna plats", "Välj för att hitta · {0}: res till denna plats", "Det går inte att resa just nu", "Väntar på att området ska laddas", "Ingen ledig plats hittades i närheten", "Portal till din by" },
            ["th-TH"] = new[] { "ตัวละคร", "สถานที่", "แผนที่ทั้งหมด", "เคลื่อนย้ายไปใกล้เป้าหมาย", "ไม่มีเป้าหมายที่แสดงได้", "ดูแผนที่", "เลือกเพื่อระบุตำแหน่ง · {0}: เคลื่อนย้ายไปใกล้เป้าหมาย", "ใช้ทิศทางเลื่อนแผนที่ · {0}: กลับรายการ", "ติดตามตัวละคร", "เลิกติดตาม", "ไปยังห้องของเป้าหมาย", "เลือกเพื่อระบุตำแหน่ง · {0}: ไปยังห้องของเป้าหมาย", "ห้อง", "ใช้ทิศทางเพื่อเลือกห้อง · {0}: เดินทาง", "บ้าน", "เลือกอาวุธ", "จุดออกเดินทาง", "ประตูเล่นหลายคน", "ต้นไม้", "เคลื่อนย้ายไปยังสถานที่นี้", "เลือกเพื่อระบุตำแหน่ง · {0}: เคลื่อนย้ายไปยังสถานที่นี้", "ยังไม่สามารถเคลื่อนย้ายได้", "รอให้โหลดพื้นที่เสร็จ", "ไม่พบจุดยืนที่ว่างในบริเวณใกล้เคียง", "ประตูกลับเมืองของคุณ" },
            ["tr-TR"] = new[] { "Kişiler", "Yerler", "Tüm harita", "Hedefin yakınına ışınlan", "Görünür hedef yok", "Haritayı gez", "Seçim konumu gösterir · {0}: hedefin yakınına ışınlan", "Yönlerle haritayı kaydır · {0}: listeye dön", "Kişiyi takip et", "Takibi bırak", "Hedefin odasına git", "Konumu bulmak için seç · {0}: hedefin odasına git", "Odalar", "Yönlerle oda seç · {0}: seyahat et", "Ev", "Silah seçimi", "Yola çıkış noktası", "Çok oyunculu giriş", "Ağaç", "Bu konuma ışınlan", "Seçim konumu gösterir · {0}: bu konuma ışınlan", "Şu anda ışınlanamazsın", "Bölgenin yüklenmesi bekleniyor", "Yakında durulabilecek boş yer bulunamadı", "Kendi kasabana açılan portal" }
        };

        internal static void Register(Action<string, string, string> add,
            IEnumerable<string> languages)
        {
            Configuration.LocalizationGroup.Register(add, languages,
                new[] { People, Places, Fit, Travel, Empty, Browse, Guide, PanGuide, Track, Untrack, RoomTravel, RoomGuide, Rooms, SelectRoomGuide, Home, Weapons, Departure, MultiplayerGate, Tree, DestinationTravel, DestinationGuide, TravelUnavailable, MapNotReady, NoLanding, TownReturnPortal }, Texts);
        }
    }
}
