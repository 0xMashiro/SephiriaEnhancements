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
                    "Keyboard melee follows movement or the last aim direction. Ranged attacks and abilities acquire nearby visible, unobstructed enemies and retain valid targets while aiming. Tap and release Switch locked target to cycle targets in a stable order; hold to clear the manual lock without switching first. Manual locks also work with melee. Gamepad aim stays native unless manually locked; assign a target-switch binding in controls.",
                    "Snap mouse aim within 96 pixels to visible, unobstructed enemies. This does not change automatic targeting, manual target lock, or native unlocked gamepad aim.",
                    "Scale the final native camera view while preserving scripted zoom and multiplayer framing.",
                    "Enabled", "Visible targets"),
                ["zh-CN"] = Create("自动索敌与目标锁定", "鼠标辅助瞄准", "视野距离",
                    "键盘近战按移动方向或最后瞄准方向攻击。远程攻击与技能自动选择附近可见、无遮挡的敌人，并在瞄准时保持有效目标。短按并松开“切换锁定目标”按稳定顺序切换；长按清除手动锁定，不会先切换目标。近战也可手动锁定。手柄未锁定时保留原生瞄准；请在控制设置中自行绑定切换目标。",
                    "将鼠标瞄准吸附到周围 96 像素内、位于屏幕中且无遮挡的敌人；不改变自动索敌、手动目标锁定或未锁定时的手柄原生瞄准。",
                    "缩放原生镜头的最终视野，同时保留剧情缩放与多人镜头构图。",
                    "开启", "可见目标"),
                ["zh-TW"] = Create("自動索敵與目標鎖定", "滑鼠輔助瞄準", "視野距離",
                    "鍵盤近戰依移動方向或最後瞄準方向攻擊。遠程攻擊與技能自動選擇附近可見、無遮擋的敵人，並在瞄準時保持有效目標。短按並放開「切換鎖定目標」依穩定順序切換；長按清除手動鎖定，不會先切換目標。近戰也可手動鎖定。手把未鎖定時保留原生瞄準；請在控制設定中自行綁定切換目標。",
                    "將滑鼠瞄準吸附到周圍 96 像素內、位於畫面中且無遮擋的敵人；不改變自動索敵、手動目標鎖定或未鎖定時的手把原生瞄準。",
                    "縮放原生鏡頭的最終視野，同時保留劇情縮放與多人鏡頭構圖。",
                    "開啟", "可見目標"),
                ["ko-KR"] = Create("자동 조준 및 대상 고정", "마우스 조준 보정", "시야 거리",
                    "키보드 근접 공격은 이동 방향이나 마지막 조준 방향을 따릅니다. 원거리 공격과 능력은 근처의 보이고 가려지지 않은 적을 선택하고 유효한 대상을 유지합니다. 대상 전환을 짧게 눌렀다 놓으면 일정한 순서로 전환하며, 길게 누르면 먼저 전환하지 않고 수동 고정을 해제합니다. 근접 공격도 수동 고정이 가능합니다. 게임패드는 수동 고정 외에는 기본 조준을 사용합니다. 대상 전환 버튼은 조작 설정에서 지정하세요.",
                    "마우스 주변 96픽셀의 화면 안 가려지지 않은 적에게 조준을 보정합니다. 자동 키보드 및 게임패드 조준은 그대로입니다.",
                    "연출 줌과 멀티플레이 구도를 유지하면서 최종 카메라 시야를 조절합니다.",
                    "활성화", "보이는 대상"),
                ["ja-JP"] = Create("自動照準とターゲットロック", "マウス照準アシスト", "表示範囲",
                    "キーボードの近接攻撃は移動方向か最後の照準方向に従います。遠距離攻撃とアビリティは近くの見える遮られていない敵を選び、有効な対象を維持します。対象切替を短く押して離すと一定の順序で切り替わり、長押しすると先に切り替えず手動ロックを解除します。近接攻撃でも手動ロックが可能です。ゲームパッドは手動ロック中以外は標準の照準を使用します。対象切替は操作設定で割り当ててください。",
                    "マウスの96ピクセル以内にいる、画面内で遮られていない敵へ照準を補正します。キーボードの自動照準とゲームパッドは変更しません。",
                    "演出ズームとマルチプレイの構図を保ったまま、最終的なカメラ範囲を調整します。",
                    "有効", "見える対象"),
                ["de-DE"] = Create("Automatische Zielwahl & Zielerfassung", "Maus-Zielhilfe", "Sichtweite",
                    "Tastatur-Nahkampf folgt der Bewegung oder letzten Zielrichtung. Fernangriffe und Fähigkeiten wählen nahe, sichtbare Gegner ohne Hindernisse und behalten gültige Ziele bei. Zielwechsel kurz drücken und loslassen: Wechsel in stabiler Reihenfolge. Gedrückt halten: manuelle Erfassung ohne vorherigen Wechsel lösen. Auch Nahkampf erlaubt manuelle Erfassung. Gamepad-Zielen bleibt sonst unverändert; Zielwechsel in der Steuerung selbst belegen.",
                    "Richtet die Maus innerhalb von 96 Pixeln auf sichtbare, freie Gegner aus. Automatische Tastatur- und Gamepad-Zielhilfe bleiben unverändert.",
                    "Skaliert die endgültige Kamerasicht und erhält Skript-Zoom und Mehrspieler-Bildrahmen.",
                    "Aktiviert", "Sichtbare Ziele"),
                ["es-ES"] = Create("Selección automática y fijación", "Ayuda de puntería del ratón", "Distancia de vista",
                    "El combate cuerpo a cuerpo con teclado sigue el movimiento o la última dirección de apuntado. Los ataques a distancia y las habilidades eligen enemigos cercanos, visibles y sin obstáculos, y mantienen los objetivos válidos. Pulsa y suelta el cambio de objetivo para recorrerlos en un orden estable; mantén pulsado para soltar la fijación sin cambiar antes. También puedes fijar objetivos cuerpo a cuerpo. El mando conserva la puntería original salvo con fijación manual; asigna el cambio de objetivo en los controles.",
                    "Ajusta el ratón a enemigos visibles y sin obstáculos en un radio de 96 píxeles. No cambia el objetivo automático ni la ayuda del mando.",
                    "Escala la vista final conservando el zoom de escenas y el encuadre multijugador.",
                    "Activado", "Objetivos visibles"),
                ["fr-FR"] = Create("Ciblage automatique et verrouillage", "Aide à la visée à la souris", "Distance de vue",
                    "Au clavier, la mêlée suit le déplacement ou la dernière direction visée. Les attaques à distance et les capacités choisissent des ennemis proches, visibles et dégagés, puis conservent les cibles valides. Appuyez brièvement puis relâchez le changement de cible pour suivre un ordre stable ; maintenez pour déverrouiller sans changer de cible auparavant. Le verrouillage manuel fonctionne aussi en mêlée. La manette conserve la visée native sans verrouillage manuel ; attribuez le changement de cible dans les commandes.",
                    "Ajuste la souris vers les ennemis visibles et dégagés dans un rayon de 96 pixels. Le ciblage automatique et la manette restent inchangés.",
                    "Ajuste la vue finale en conservant les zooms scriptés et le cadrage multijoueur.",
                    "Activé", "Cibles visibles"),
                ["it-IT"] = Create("Mira automatica e blocco bersaglio", "Mira assistita del mouse", "Distanza visuale",
                    "La mischia da tastiera segue il movimento o l’ultima direzione di mira. Gli attacchi a distanza e le abilità scelgono nemici vicini, visibili e senza ostacoli, mantenendo i bersagli validi. Premi e rilascia il cambio bersaglio per scorrere in ordine stabile; tieni premuto per sbloccare senza cambiare prima bersaglio. Il blocco manuale funziona anche in mischia. Il gamepad mantiene la mira originale senza blocco manuale; assegna il cambio bersaglio nei comandi.",
                    "Corregge il mouse verso nemici visibili e non coperti entro 96 pixel. Il bersaglio automatico e il gamepad non cambiano.",
                    "Scala la vista finale mantenendo zoom di scena e inquadratura multigiocatore.",
                    "Attivato", "Bersagli visibili"),
                ["pl-PL"] = Create("Automatyczne celowanie i blokada celu", "Wspomaganie celowania myszą", "Zasięg widoku",
                    "Walka wręcz na klawiaturze podąża za ruchem lub ostatnim kierunkiem celowania. Ataki dystansowe i zdolności wybierają pobliskich, widocznych i nieosłoniętych wrogów oraz utrzymują prawidłowe cele. Naciśnij i zwolnij zmianę celu, aby przełączać w stałej kolejności; przytrzymaj, aby zwolnić blokadę bez wcześniejszej zmiany. Blokada działa też w walce wręcz. Pad zachowuje oryginalne celowanie poza ręczną blokadą; przypisz zmianę celu w ustawieniach sterowania.",
                    "Koryguje mysz na widocznych, niezasłoniętych wrogów w promieniu 96 pikseli. Automatyczne celowanie i pad pozostają bez zmian.",
                    "Skaluje końcowy widok kamery, zachowując zoom scen i kadr wieloosobowy.",
                    "Włączone", "Widoczne cele"),
                ["pt-BR"] = Create("Mira automática e trava de alvo", "Assistência de mira do mouse", "Distância de visão",
                    "Ataques corpo a corpo no teclado seguem o movimento ou a última direção de mira. Ataques à distância e habilidades escolhem inimigos próximos, visíveis e sem obstáculos, mantendo alvos válidos. Pressione e solte a troca de alvo para seguir uma ordem estável; segure para destravar sem trocar antes. A trava manual também funciona corpo a corpo. O controle mantém a mira nativa sem trava manual; atribua a troca de alvo nas configurações de controles.",
                    "Ajusta o mouse para inimigos visíveis e desobstruídos em um raio de 96 pixels. O alvo automático e o controle não mudam.",
                    "Escala a visão final preservando zooms de cena e enquadramento multijogador.",
                    "Ativado", "Alvos visíveis"),
                ["ru-RU"] = Create("Автонаведение и захват цели", "Помощь прицеливания мышью", "Дальность обзора",
                    "Ближний бой с клавиатуры следует направлению движения или последнего прицеливания. Дальние атаки и способности выбирают ближайших видимых врагов без преград и удерживают подходящую цель. Коротко нажмите и отпустите смену цели для переключения в стабильном порядке; удерживайте для снятия захвата без предварительной смены. Ручной захват доступен и в ближнем бою. Геймпад сохраняет штатное прицеливание без ручного захвата; назначьте смену цели в настройках управления.",
                    "Корректирует мышь по видимым целям без препятствий в радиусе 96 пикселей. Автовыбор и геймпад не меняются.",
                    "Масштабирует итоговый вид, сохраняя сюжетный зум и сетевое кадрирование.",
                    "Включено", "Видимые цели"),
                ["sv-SE"] = Create("Automatiskt sikte och mållåsning", "Mushjälp för sikte", "Synavstånd",
                    "Närstrid med tangentbord följer rörelsen eller den senaste siktriktningen. Distansattacker och förmågor väljer närliggande, synliga fiender utan hinder och behåller giltiga mål. Tryck och släpp målbyte för att växla i stabil ordning; håll inne för att släppa låsningen utan att byta först. Manuell låsning fungerar även i närstrid. Handkontrollen behåller spelets vanliga sikte utan manuell låsning; tilldela målbyte i kontrollinställningarna.",
                    "Justerar musen mot synliga mål utan hinder inom 96 pixlar. Automatiskt sikte och handkontroll ändras inte.",
                    "Skalar den slutliga kameravyn men behåller scenzoom och flerspelarbild.",
                    "Aktiverat", "Synliga mål"),
                ["th-TH"] = Create("เล็งอัตโนมัติและล็อกเป้าหมาย", "ตัวช่วยเล็งเมาส์", "ระยะการมองเห็น",
                    "การโจมตีระยะประชิดด้วยแป้นพิมพ์จะตามทิศทางเคลื่อนที่หรือทิศเล็งล่าสุด การโจมตีระยะไกลและความสามารถจะเลือกศัตรูใกล้เคียงที่มองเห็นและไม่มีสิ่งกีดขวาง แล้วคงเป้าหมายที่ยังใช้ได้ กดแล้วปล่อยปุ่มสลับเป้าหมายเพื่อสลับตามลำดับคงที่ กดค้างเพื่อปลดล็อกโดยไม่สลับเป้าหมายก่อน การโจมตีระยะประชิดก็ล็อกเป้าหมายเองได้ จอยจะใช้การเล็งเดิมเมื่อไม่ได้ล็อกเอง โปรดกำหนดปุ่มสลับเป้าหมายในการตั้งค่าการควบคุม",
                    "ปรับเมาส์ไปยังศัตรูที่มองเห็นและไม่มีสิ่งกีดขวางภายใน 96 พิกเซล โดยไม่เปลี่ยนการเล็งอัตโนมัติหรือจอย",
                    "ปรับมุมมองสุดท้ายโดยคงการซูมเนื้อเรื่องและเฟรมผู้เล่นหลายคน",
                    "เปิดใช้งาน", "เป้าหมายที่มองเห็น"),
                ["tr-TR"] = Create("Otomatik hedefleme ve hedef kilidi", "Fare nişan yardımı", "Görüş mesafesi",
                    "Klavyeyle yakın dövüş, hareketi veya son nişan yönünü izler. Menzilli saldırılar ve yetenekler yakındaki görünür, engelsiz düşmanları seçer ve geçerli hedefi korur. Sabit sırayla geçmek için hedef değiştirmeye basıp bırakın; önce hedef değiştirmeden kilidi kaldırmak için basılı tutun. Yakın dövüşte de elle kilitlenebilir. Elle kilit yokken oyun kumandası özgün nişanı kullanır; hedef değiştirmeyi kontrol ayarlarından atayın.",
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
                ["en-US"] = "View statistics (tap) / hide or show (hold)",
                ["zh-CN"] = "查看统计（短按）/隐藏或显示（长按）",
                ["zh-TW"] = "查看統計（短按）/隱藏或顯示（長按）",
                ["ko-KR"] = "통계 보기(짧게) / 표시 전환(길게)",
                ["ja-JP"] = "統計を見る（短押し）／表示切替（長押し）",
                ["de-DE"] = "Statistik öffnen (kurz) / ein- oder ausblenden (halten)",
                ["es-ES"] = "Ver estadísticas (pulsar) / ocultar o mostrar (mantener)",
                ["fr-FR"] = "Voir les statistiques (appui bref) / masquer ou afficher (maintien)",
                ["it-IT"] = "Statistiche (pressione breve) / mostra o nascondi (pressione lunga)",
                ["pl-PL"] = "Statystyki (naciśnij) / ukryj lub pokaż (przytrzymaj)",
                ["pt-BR"] = "Ver estatísticas (toque) / ocultar ou mostrar (segure)",
                ["ru-RU"] = "Статистика (нажать) / скрыть или показать (удерживать)",
                ["sv-SE"] = "Visa statistik (tryck) / dölj eller visa (håll)",
                ["th-TH"] = "ดูสถิติ (กด) / ซ่อนหรือแสดง (กดค้าง)",
                ["tr-TR"] = "İstatistikleri aç (bas) / gizle veya göster (basılı tut)"

            };

        private static readonly Dictionary<string, string> OptimizeInventoryTexts =
            new Dictionary<string, string>
            {
                ["en-US"] = "Smart Arrange",
                ["zh-CN"] = "智能整理",
                ["zh-TW"] = "智慧整理",
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
