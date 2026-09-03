using System;
using System.Collections.Generic;

namespace SephiriaEnhancements.Configuration
{
    internal static class ControlLocalization
    {
        internal const string SettingTargetingMode =
            "SephiriaEnhancements.Setting.TargetingMode";
        internal const string HelpTargetingMode =
            "SephiriaEnhancements.Help.TargetingMode";
        internal const string SettingMouseAimAssist =
            "SephiriaEnhancements.Setting.MouseAimAssist";
        internal const string HelpMouseAimAssist =
            "SephiriaEnhancements.Help.MouseAimAssist";
        internal const string SettingViewDistance =
            "SephiriaEnhancements.Setting.ViewDistance";
        internal const string HelpViewDistance = "SephiriaEnhancements.Help.ViewDistance";
        internal const string AimVisibleTargets =
            "SephiriaEnhancements.MouseAimAssist.VisibleTargets";
        internal const string SwitchLockedTarget =
            "SephiriaEnhancements.Controls.SwitchLockedTarget";
        internal const string ToggleCurrentFloorMapOverlay =
            "SephiriaEnhancements.Controls.ToggleCurrentFloorMapOverlay";
        internal const string ToggleDamageStatistics =
            "SephiriaEnhancements.Controls.ToggleDamageStatistics";
        internal const string OptimizeInventory =
            "SephiriaEnhancements.Controls.OptimizeInventory";
        internal const string SecondaryUiAction =
            "SephiriaEnhancements.Controls.SecondaryUiAction";
        internal const string RotateItem =
            "SephiriaEnhancements.Controls.RotateItem";
        internal const string EngraveTablet =
            "SephiriaEnhancements.Controls.EngraveTablet";
        internal const string ShortcutsSection =
            "SephiriaEnhancements.Controls.Section";

        internal static readonly string[] TargetingModeKeys =
        {
            ModLocalization.Off,
            "SephiriaEnhancements.TargetingMode.Automatic"
        };

        internal static readonly string[] MouseAimAssistKeys =
        {
            ModLocalization.Off, AimVisibleTargets
        };

        internal static readonly string[] ViewDistanceKeys =
        {
            "SephiriaEnhancements.ViewDistance.75",
            "SephiriaEnhancements.ViewDistance.100",
            "SephiriaEnhancements.ViewDistance.125",
            "SephiriaEnhancements.ViewDistance.150",
            "SephiriaEnhancements.ViewDistance.175",
            "SephiriaEnhancements.ViewDistance.200"
        };

        private static readonly Dictionary<string, Dictionary<string, string>> Texts =
            new Dictionary<string, Dictionary<string, string>>
            {
                ["en-US"] = Create("Automatic targeting & target lock", "Mouse aim assist", "View distance",
                    "Keyboard attacks automatically aim at a visible, unobstructed enemy selected by movement direction and distance. Press the configurable Switch locked target action to cycle visible targets; hold it to clear the manual lock. On gamepad, the Mod controls aim only while a target is manually locked. On first enable, J and K fill only empty, conflict-free official alternate bindings for the basic and secondary weapon actions; mouse buttons and existing bindings are not replaced.",
                    "Snap mouse aim within 96 pixels to visible, unobstructed enemies. This does not change automatic targeting, manual target lock, or native unlocked gamepad aim.",
                    "Scale the final native camera view while preserving scripted zoom and multiplayer framing.",
                    "Enabled", "Visible targets"),
                ["zh-CN"] = Create("自动索敌与目标锁定", "鼠标辅助瞄准", "视野距离",
                    "使用键盘时，攻击会自动瞄准根据移动方向和距离选出的可见、无遮挡敌人。短按可改键的“切换锁定目标”可在可见目标间切换，长按则清除手动锁定；使用手柄时，仅在手动锁定目标期间由 Mod 接管瞄准。首次启用时，只有在官方普通攻击和副攻击的备用键位为空且无冲突时，才会分别填入 J 和 K；不会替换鼠标按键或已有绑定。",
                    "将鼠标瞄准吸附到周围 96 像素内、位于屏幕中且无遮挡的敌人；不改变自动索敌、手动目标锁定或未锁定时的手柄原生瞄准。",
                    "缩放原生镜头的最终视野，同时保留剧情缩放与多人镜头构图。",
                    "开启", "可见目标"),
                ["zh-TW"] = Create("自動索敵與目標鎖定", "滑鼠輔助瞄準", "視野距離",
                    "使用鍵盤時，攻擊會自動瞄準依移動方向與距離選出的可見、無遮擋敵人。短按可改鍵的「切換鎖定目標」可在可見目標間切換，長按則清除手動鎖定；使用手把時，僅在手動鎖定目標期間由 Mod 接管瞄準。首次啟用時，只有在官方普通攻擊與副攻擊的備用按鍵為空且無衝突時，才會分別填入 J 與 K；不會取代滑鼠按鍵或既有綁定。",
                    "將滑鼠瞄準吸附到周圍 96 像素內、位於畫面中且無遮擋的敵人；不改變自動索敵、手動目標鎖定或未鎖定時的手把原生瞄準。",
                    "縮放原生鏡頭的最終視野，同時保留劇情縮放與多人鏡頭構圖。",
                    "開啟", "可見目標"),
                ["ko-KR"] = Create("자동 조준 및 대상 고정", "마우스 조준 보정", "시야 거리",
                    "키보드 공격은 이동 방향과 거리를 기준으로 선택한, 화면에 보이고 가려지지 않은 적을 자동으로 조준합니다. 설정 가능한 대상 고정 전환 동작을 짧게 누르면 보이는 대상을 순환하고 길게 누르면 수동 고정을 해제합니다. 게임패드는 대상을 수동으로 고정한 동안에만 Mod가 조준을 제어합니다. 처음 활성화할 때 공식 기본 및 보조 무기 동작의 비어 있고 충돌 없는 보조 바인딩에만 J와 K를 지정하며, 마우스 버튼이나 기존 바인딩은 바꾸지 않습니다.",
                    "마우스 주변 96픽셀의 화면 안 가려지지 않은 적에게 조준을 보정합니다. 자동 키보드 및 게임패드 조준은 그대로입니다.",
                    "연출 줌과 멀티플레이 구도를 유지하면서 최종 카메라 시야를 조절합니다.",
                    "활성화", "보이는 대상"),
                ["ja-JP"] = Create("自動照準とターゲットロック", "マウス照準アシスト", "表示範囲",
                    "キーボード攻撃は、移動方向と距離から選ばれた画面内の遮られていない敵を自動で狙います。変更可能な「ロック対象切替」を短く押すと表示中の対象を順に切り替え、長押しすると手動ロックを解除します。ゲームパッドでは、手動ロック中のみ Mod が照準を制御します。初回有効化時、公式の通常攻撃と副攻撃に空きがあり競合しない場合だけ、予備バインドへ J と K を設定します。マウスボタンや既存の設定は置き換えません。",
                    "マウスの96ピクセル以内にいる、画面内で遮られていない敵へ照準を補正します。キーボードの自動照準とゲームパッドは変更しません。",
                    "演出ズームとマルチプレイの構図を保ったまま、最終的なカメラ範囲を調整します。",
                    "有効", "見える対象"),
                ["de-DE"] = Create("Automatische Zielwahl & Zielerfassung", "Maus-Zielhilfe", "Sichtweite",
                    "Tastaturangriffe zielen automatisch auf einen sichtbaren, freien Gegner, der nach Bewegungsrichtung und Entfernung gewählt wird. Kurzes Drücken der konfigurierbaren Aktion zum Zielwechsel wechselt zwischen sichtbaren Zielen; langes Drücken löst die manuelle Erfassung. Beim Gamepad steuert der Mod das Zielen nur während einer manuellen Zielerfassung. Bei der ersten Aktivierung werden J und K nur in freie, konfliktlose offizielle Zweitbelegungen für primäre und sekundäre Waffenaktionen eingetragen; Maustasten und vorhandene Belegungen bleiben bestehen.",
                    "Richtet die Maus innerhalb von 96 Pixeln auf sichtbare, freie Gegner aus. Automatische Tastatur- und Gamepad-Zielhilfe bleiben unverändert.",
                    "Skaliert die endgültige Kamerasicht und erhält Skript-Zoom und Mehrspieler-Bildrahmen.",
                    "Aktiviert", "Sichtbare Ziele"),
                ["es-ES"] = Create("Selección automática y fijación", "Ayuda de puntería del ratón", "Distancia de vista",
                    "Los ataques con teclado apuntan automáticamente a un enemigo visible y sin obstáculos, elegido según la dirección de movimiento y la distancia. Pulsa brevemente la acción configurable para cambiar de objetivo fijado; mantenla pulsada para quitar la fijación manual. Con mando, el Mod solo controla la puntería mientras haya un objetivo fijado manualmente. Al activarlo por primera vez, J y K solo se asignan a enlaces oficiales alternativos vacíos y sin conflictos para las acciones de arma principal y secundaria; no se sustituyen los botones del ratón ni los enlaces existentes.",
                    "Ajusta el ratón a enemigos visibles y sin obstáculos en un radio de 96 píxeles. No cambia el objetivo automático ni la ayuda del mando.",
                    "Escala la vista final conservando el zoom de escenas y el encuadre multijugador.",
                    "Activado", "Objetivos visibles"),
                ["fr-FR"] = Create("Ciblage automatique et verrouillage", "Aide à la visée à la souris", "Distance de vue",
                    "Les attaques au clavier visent automatiquement un ennemi visible et dégagé, choisi selon la direction du déplacement et la distance. Appuyez brièvement sur l'action configurable de changement de cible verrouillée pour parcourir les cibles visibles ; maintenez-la pour annuler le verrouillage manuel. À la manette, le Mod ne contrôle la visée que pendant un verrouillage manuel. Lors de la première activation, J et K ne sont ajoutés qu'aux liaisons officielles secondaires libres et sans conflit des actions d'arme principale et secondaire ; les boutons de souris et les liaisons existantes ne sont pas remplacés.",
                    "Ajuste la souris vers les ennemis visibles et dégagés dans un rayon de 96 pixels. Le ciblage automatique et la manette restent inchangés.",
                    "Ajuste la vue finale en conservant les zooms scriptés et le cadrage multijoueur.",
                    "Activé", "Cibles visibles"),
                ["it-IT"] = Create("Mira automatica e blocco bersaglio", "Mira assistita del mouse", "Distanza visuale",
                    "Gli attacchi da tastiera mirano automaticamente a un nemico visibile e non coperto, scelto in base alla direzione di movimento e alla distanza. Premi brevemente l'azione configurabile per cambiare il bersaglio bloccato; tienila premuta per rimuovere il blocco manuale. Con il gamepad, il Mod controlla la mira solo durante un blocco manuale. Alla prima attivazione, J e K vengono assegnati solo ai binding ufficiali alternativi vuoti e senza conflitti per le azioni arma primaria e secondaria; i pulsanti del mouse e i binding esistenti non vengono sostituiti.",
                    "Corregge il mouse verso nemici visibili e non coperti entro 96 pixel. Il bersaglio automatico e il gamepad non cambiano.",
                    "Scala la vista finale mantenendo zoom di scena e inquadratura multigiocatore.",
                    "Attivato", "Bersagli visibili"),
                ["pl-PL"] = Create("Automatyczne celowanie i blokada celu", "Wspomaganie celowania myszą", "Zasięg widoku",
                    "Ataki z klawiatury automatycznie celują w widocznego, niezasłoniętego przeciwnika wybranego według kierunku ruchu i odległości. Krótkie naciśnięcie konfigurowalnej akcji zmiany zablokowanego celu przełącza widoczne cele, a przytrzymanie usuwa ręczną blokadę. Na padzie Mod steruje celowaniem tylko podczas ręcznej blokady. Przy pierwszym włączeniu J i K są przypisywane wyłącznie do pustych, bezkonfliktowych oficjalnych alternatywnych powiązań podstawowej i dodatkowej akcji broni; przyciski myszy i istniejące powiązania nie są zastępowane.",
                    "Koryguje mysz na widocznych, niezasłoniętych wrogów w promieniu 96 pikseli. Automatyczne celowanie i pad pozostają bez zmian.",
                    "Skaluje końcowy widok kamery, zachowując zoom scen i kadr wieloosobowy.",
                    "Włączone", "Widoczne cele"),
                ["pt-BR"] = Create("Mira automática e trava de alvo", "Assistência de mira do mouse", "Distância de visão",
                    "Os ataques pelo teclado miram automaticamente em um inimigo visível e sem obstáculos, escolhido pela direção do movimento e pela distância. Pressione brevemente a ação configurável para alternar o alvo travado; mantenha pressionado para remover a trava manual. No controle, o Mod só controla a mira durante uma trava manual. Na primeira ativação, J e K são atribuídos apenas a vínculos oficiais alternativos vazios e sem conflito para as ações de arma principal e secundária; os botões do mouse e vínculos existentes não são substituídos.",
                    "Ajusta o mouse para inimigos visíveis e desobstruídos em um raio de 96 pixels. O alvo automático e o controle não mudam.",
                    "Escala a visão final preservando zooms de cena e enquadramento multijogador.",
                    "Ativado", "Alvos visíveis"),
                ["ru-RU"] = Create("Автонаведение и захват цели", "Помощь прицеливания мышью", "Дальность обзора",
                    "Атаки с клавиатуры автоматически наводятся на видимого противника без препятствий, выбранного по направлению движения и расстоянию. Короткое нажатие на настраиваемое действие смены захваченной цели перебирает видимые цели, а удержание снимает ручной захват. При игре с геймпадом Mod управляет прицеливанием только во время ручного захвата. При первом включении J и K назначаются только на свободные и неконфликтующие официальные дополнительные привязки основного и дополнительного действий оружия; кнопки мыши и существующие привязки не заменяются.",
                    "Корректирует мышь по видимым целям без препятствий в радиусе 96 пикселей. Автовыбор и геймпад не меняются.",
                    "Масштабирует итоговый вид, сохраняя сюжетный зум и сетевое кадрирование.",
                    "Включено", "Видимые цели"),
                ["sv-SE"] = Create("Automatiskt sikte och mållåsning", "Mushjälp för sikte", "Synavstånd",
                    "Tangentbordsattacker siktar automatiskt på en synlig fiende utan hinder, vald efter rörelseriktning och avstånd. Tryck kort på den konfigurerbara åtgärden för att byta låst mål; håll den nedtryckt för att ta bort den manuella låsningen. Med handkontroll styr Mod siktet endast under en manuell mållåsning. Vid första aktiveringen tilldelas J och K endast till tomma, konfliktfria officiella alternativbindningar för primär och sekundär vapenåtgärd; musknappar och befintliga bindningar ersätts inte.",
                    "Justerar musen mot synliga mål utan hinder inom 96 pixlar. Automatiskt sikte och handkontroll ändras inte.",
                    "Skalar den slutliga kameravyn men behåller scenzoom och flerspelarbild.",
                    "Aktiverat", "Synliga mål"),
                ["th-TH"] = Create("เล็งอัตโนมัติและล็อกเป้าหมาย", "ตัวช่วยเล็งเมาส์", "ระยะการมองเห็น",
                    "การโจมตีด้วยแป้นพิมพ์จะเล็งศัตรูที่มองเห็นและไม่มีสิ่งกีดขวางโดยอัตโนมัติ โดยเลือกตามทิศทางการเคลื่อนที่และระยะทาง กดคำสั่งสลับเป้าหมายที่ตั้งค่าได้แบบสั้นเพื่อวนเป้าหมายที่มองเห็น หรือกดค้างเพื่อล้างการล็อกด้วยตนเอง เมื่อใช้จอย Mod จะควบคุมการเล็งเฉพาะขณะล็อกเป้าหมายด้วยตนเองเท่านั้น เมื่อเปิดใช้ครั้งแรก J และ K จะถูกกำหนดเฉพาะช่องปุ่มสำรองอย่างเป็นทางการที่ว่างและไม่ขัดแย้งสำหรับคำสั่งอาวุธหลักและรอง โดยไม่แทนที่ปุ่มเมาส์หรือการตั้งค่าเดิม",
                    "ปรับเมาส์ไปยังศัตรูที่มองเห็นและไม่มีสิ่งกีดขวางภายใน 96 พิกเซล โดยไม่เปลี่ยนการเล็งอัตโนมัติหรือจอย",
                    "ปรับมุมมองสุดท้ายโดยคงการซูมเนื้อเรื่องและเฟรมผู้เล่นหลายคน",
                    "เปิดใช้งาน", "เป้าหมายที่มองเห็น"),
                ["tr-TR"] = Create("Otomatik hedefleme ve hedef kilidi", "Fare nişan yardımı", "Görüş mesafesi",
                    "Klavye saldırıları, hareket yönü ve mesafeye göre seçilen görünür ve engelsiz bir düşmanı otomatik olarak hedefler. Yapılandırılabilir kilitli hedef değiştirme eylemine kısa basarak görünür hedefler arasında geçiş yapın; basılı tutarak manuel kilidi kaldırın. Oyun kolunda Mod, nişanı yalnızca manuel hedef kilidi sırasında kontrol eder. İlk etkinleştirmede J ve K yalnızca birincil ve ikincil silah eylemlerinin boş ve çakışmasız resmi alternatif bağlarına atanır; fare düğmeleri ve mevcut bağlar değiştirilmez.",
                    "Fareyi 96 piksel içindeki görünür, engelsiz düşmanlara düzeltir. Otomatik klavye hedefi ve oyun kolu değişmez.",
                    "Sahne yakınlaştırmasını ve çok oyunculu kadrajı koruyarak son kamera görünümünü ölçekler.",
                    "Etkin", "Görünür hedefler")
            };

        private static readonly Dictionary<string, string> SwitchLockedTargetTexts =
            new Dictionary<string, string>
            {
                ["en-US"] = "Switch locked target",
                ["zh-CN"] = "切换锁定目标",
                ["zh-TW"] = "切換鎖定目標",
                ["ko-KR"] = "고정 대상 전환",
                ["ja-JP"] = "ロック対象切替",
                ["de-DE"] = "Festes Ziel wechseln",
                ["es-ES"] = "Cambiar objetivo fijado",
                ["fr-FR"] = "Changer de cible verrouillée",
                ["it-IT"] = "Cambia bersaglio bloccato",
                ["pl-PL"] = "Zmień zablokowany cel",
                ["pt-BR"] = "Alternar alvo travado",
                ["ru-RU"] = "Сменить захваченную цель",
                ["sv-SE"] = "Byt låst mål",
                ["th-TH"] = "สลับเป้าหมายที่ล็อก",
                ["tr-TR"] = "Kilitli hedefi değiştir"
            };

        private static readonly Dictionary<string, string>
            ToggleCurrentFloorMapOverlayTexts =
            new Dictionary<string, string>
            {
                ["en-US"] = "Toggle current-floor map overlay",
                ["zh-CN"] = "显示或隐藏本层地图叠加层",
                ["zh-TW"] = "顯示或隱藏本層地圖疊加層",
                ["ko-KR"] = "현재 층 지도 오버레이 표시 전환",
                ["ja-JP"] = "現在のフロアマップオーバーレイ表示切替",
                ["de-DE"] = "Overlay der aktuellen Etagenkarte ein-/ausblenden",
                ["es-ES"] = "Mostrar u ocultar la superposición del mapa de la planta actual",
                ["fr-FR"] = "Afficher ou masquer la superposition de la carte de l’étage actuel",
                ["it-IT"] = "Mostra o nascondi la mappa sovrapposta del piano attuale",
                ["pl-PL"] = "Pokaż lub ukryj nakładkę mapy bieżącego piętra",
                ["pt-BR"] = "Mostrar ou ocultar sobreposição do mapa do andar atual",
                ["ru-RU"] = "Показать или скрыть наложение карты текущего этажа",
                ["sv-SE"] = "Visa eller dölj kartöverlägget för aktuell våning",
                ["th-TH"] = "แสดงหรือซ่อนแผนที่ซ้อนทับของชั้นปัจจุบัน",
                ["tr-TR"] = "Geçerli kat haritası kaplamasını göster veya gizle"
            };

        private static readonly Dictionary<string, string>
            ToggleDamageStatisticsTexts =
            new Dictionary<string, string>
            {
                ["en-US"] = "Combat report (tap) / statistics display (hold)",
                ["zh-CN"] = "战报（短按）/统计显示（长按）",
                ["zh-TW"] = "戰報（短按）/統計顯示（長按）",
                ["ko-KR"] = "전투 보고서(짧게) / 통계 표시(길게)",
                ["ja-JP"] = "戦闘レポート（短押し）／統計表示（長押し）",
                ["de-DE"] = "Kampfbericht (kurz) / Statistik (halten)",
                ["es-ES"] = "Informe de combate (pulsar) / estadísticas (mantener)",
                ["fr-FR"] = "Rapport de combat (appui bref) / statistiques (maintien)",
                ["it-IT"] = "Resoconto (pressione breve) / statistiche (pressione lunga)",
                ["pl-PL"] = "Raport walki (naciśnij) / statystyki (przytrzymaj)",
                ["pt-BR"] = "Relatório de combate (toque) / estatísticas (segure)",
                ["ru-RU"] = "Отчёт о бое (нажать) / статистика (удерживать)",
                ["sv-SE"] = "Stridsrapport (tryck) / statistik (håll)",
                ["th-TH"] = "รายงานการต่อสู้ (กด) / สถิติ (กดค้าง)",
                ["tr-TR"] = "Savaş raporu (bas) / istatistikler (basılı tut)"

            };

        private static readonly Dictionary<string, string> OptimizeInventoryTexts =
            new Dictionary<string, string>
            {
                ["en-US"] = "Optimize inventory",
                ["zh-CN"] = "优化背包",
                ["zh-TW"] = "最佳化背包",
                ["ko-KR"] = "인벤토리 최적화",
                ["ja-JP"] = "インベントリを最適化",
                ["de-DE"] = "Inventar optimieren",
                ["es-ES"] = "Optimizar inventario",
                ["fr-FR"] = "Optimiser l’inventaire",
                ["it-IT"] = "Ottimizza inventario",
                ["pl-PL"] = "Optymalizuj ekwipunek",
                ["pt-BR"] = "Otimizar inventário",
                ["ru-RU"] = "Оптимизировать инвентарь",
                ["sv-SE"] = "Optimera inventariet",
                ["th-TH"] = "ปรับช่องเก็บของให้เหมาะสม",
                ["tr-TR"] = "Envanteri iyileştir"

            };

        private static readonly Dictionary<string, string> SecondaryUiActionTexts =
            new Dictionary<string, string>
            {
                ["en-US"] = "Secondary UI action",
                ["zh-CN"] = "次要界面操作",
                ["zh-TW"] = "次要介面操作",
                ["ko-KR"] = "보조 UI 동작",
                ["ja-JP"] = "UI の副操作",
                ["de-DE"] = "Sekundäre UI-Aktion",
                ["es-ES"] = "Acción secundaria de interfaz",
                ["fr-FR"] = "Action secondaire de l’interface",
                ["it-IT"] = "Azione secondaria dell’interfaccia",
                ["pl-PL"] = "Dodatkowa akcja interfejsu",
                ["pt-BR"] = "Ação secundária da interface",
                ["ru-RU"] = "Дополнительное действие интерфейса",
                ["sv-SE"] = "Sekundär gränssnittsåtgärd",
                ["th-TH"] = "คำสั่งรองของหน้าจอ",
                ["tr-TR"] = "İkincil arayüz eylemi"

            };

        private static readonly Dictionary<string, string> RotateItemTexts =
            new Dictionary<string, string>
            {
                ["en-US"] = "Rotate or favorite item",
                ["zh-CN"] = "旋转物品或切换收藏",
                ["zh-TW"] = "旋轉物品或切換收藏",
                ["ko-KR"] = "아이템 회전 또는 즐겨찾기 전환",
                ["ja-JP"] = "アイテムの回転／お気に入り切替",
                ["de-DE"] = "Gegenstand drehen oder favorisieren",
                ["es-ES"] = "Girar objeto o marcar favorito",
                ["fr-FR"] = "Tourner l’objet ou basculer le favori",
                ["it-IT"] = "Ruota oggetto o cambia preferito",
                ["pl-PL"] = "Obróć przedmiot lub zmień ulubione",
                ["pt-BR"] = "Girar item ou alternar favorito",
                ["ru-RU"] = "Повернуть предмет / изменить избранное",
                ["sv-SE"] = "Rotera föremål eller växla favorit",
                ["th-TH"] = "หมุนไอเทมหรือสลับรายการโปรด",
                ["tr-TR"] = "Eşyayı döndür veya favoriyi değiştir"

            };

        private static readonly Dictionary<string, string> EngraveTabletTexts =
            new Dictionary<string, string>
            {
                ["en-US"] = "Engrave tablet",
                ["zh-CN"] = "刻印石板",
                ["zh-TW"] = "刻印石板",
                ["ko-KR"] = "석판 각인",
                ["ja-JP"] = "石板を刻印",
                ["de-DE"] = "Steintafel gravieren",
                ["es-ES"] = "Grabar tablilla",
                ["fr-FR"] = "Graver une tablette",
                ["it-IT"] = "Incidi tavoletta",
                ["pl-PL"] = "Wyryj tabliczkę",
                ["pt-BR"] = "Gravar tabuleta",
                ["ru-RU"] = "Выгравировать табличку",
                ["sv-SE"] = "Gravera stentavla",
                ["th-TH"] = "สลักแผ่นศิลา",
                ["tr-TR"] = "Tableti kazı"

            };

        private static readonly Dictionary<string, string> ShortcutSectionTexts =
            new Dictionary<string, string>
            {
                ["en-US"] = "SEPHIRIA ENHANCEMENTS SHORTCUTS",
                ["zh-CN"] = "SEPHIRIA ENHANCEMENTS 快捷键",
                ["zh-TW"] = "SEPHIRIA ENHANCEMENTS 快捷鍵",
                ["ko-KR"] = "SEPHIRIA ENHANCEMENTS 단축키",
                ["ja-JP"] = "SEPHIRIA ENHANCEMENTS ショートカット",
                ["de-DE"] = "SEPHIRIA ENHANCEMENTS TASTENKÜRZEL",
                ["es-ES"] = "ATAJOS DE SEPHIRIA ENHANCEMENTS",
                ["fr-FR"] = "RACCOURCIS SEPHIRIA ENHANCEMENTS",
                ["it-IT"] = "SCORCIATOIE SEPHIRIA ENHANCEMENTS",
                ["pl-PL"] = "SKRÓTY SEPHIRIA ENHANCEMENTS",
                ["pt-BR"] = "ATALHOS DO SEPHIRIA ENHANCEMENTS",
                ["ru-RU"] = "ГОРЯЧИЕ КЛАВИШИ SEPHIRIA ENHANCEMENTS",
                ["sv-SE"] = "SEPHIRIA ENHANCEMENTS-GENVÄGAR",
                ["th-TH"] = "ปุ่มลัด SEPHIRIA ENHANCEMENTS",
                ["tr-TR"] = "SEPHIRIA ENHANCEMENTS KISAYOLLARI"
            };

        internal static void Register(Action<string, string, string> addText)
        {
            int[] viewPercentages = { 75, 100, 125, 150, 175, 200 };
            foreach (KeyValuePair<string, Dictionary<string, string>> language in Texts)
            {
                // Shortcut labels are one localization group. OptimizeInventory is
                // the completeness gate so a language never gets a mixed row set.
                string shortcutLanguage = OptimizeInventoryTexts.ContainsKey(language.Key)
                    ? language.Key : "en-US";
                addText(language.Key, SwitchLockedTarget,
                    SwitchLockedTargetTexts[shortcutLanguage]);
                addText(language.Key, ToggleCurrentFloorMapOverlay,
                    ToggleCurrentFloorMapOverlayTexts[shortcutLanguage]);
                addText(language.Key, ToggleDamageStatistics,
                    ToggleDamageStatisticsTexts[shortcutLanguage]);
                addText(language.Key, OptimizeInventory,
                    OptimizeInventoryTexts[shortcutLanguage]);
                addText(language.Key, SecondaryUiAction,
                    SecondaryUiActionTexts[shortcutLanguage]);
                addText(language.Key, RotateItem,
                    RotateItemTexts[shortcutLanguage]);
                addText(language.Key, EngraveTablet,
                    EngraveTabletTexts[shortcutLanguage]);
                addText(language.Key, ShortcutsSection,
                    ShortcutSectionTexts[shortcutLanguage]);
                foreach (KeyValuePair<string, string> text in language.Value)
                {
                    addText(language.Key, text.Key, text.Value);
                }

                for (int index = 0; index < ViewDistanceKeys.Length; index++)
                {
                    addText(language.Key, ViewDistanceKeys[index],
                        viewPercentages[index] + "%");
                }
            }
        }

        private static Dictionary<string, string> Create(string targeting,
            string aimAssist, string viewDistance, string targetingHelp, string assistHelp,
            string viewHelp, string enabled, string visibleTargets)
        {
            return new Dictionary<string, string>
            {
                [SettingTargetingMode] = targeting,
                [SettingMouseAimAssist] = aimAssist,
                [SettingViewDistance] = viewDistance,
                [HelpTargetingMode] = targetingHelp,
                [HelpMouseAimAssist] = assistHelp,
                [HelpViewDistance] = viewHelp,
                [TargetingModeKeys[1]] = enabled,
                [AimVisibleTargets] = visibleTargets
            };
        }
    }
}
