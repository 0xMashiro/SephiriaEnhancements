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
                    "Press the target-switch control on the keyboard, or attack with a keyboard binding, to enter keyboard combat and hide the pointer, even without enemies. Tap and release to lock or cycle targets; hold to clear the manual lock while staying in keyboard combat. Locked melee and ranged attacks aim at the target. Unlocked melee follows movement; ranged attacks and abilities retain nearby visible, unobstructed enemies. Without a target, movement sets aim; stopping retains it. Menus and travel suspend targeting. Move, click or scroll the mouse to resume mouse control. Unlocked gamepad aim stays native.",
                    "Snap mouse aim within 96 pixels to visible, unobstructed enemies. This does not change automatic targeting, manual target lock, or native unlocked gamepad aim.",
                    "Scale the final native camera view while preserving scripted zoom and multiplayer framing.",
                    "Enabled", "Visible targets"),
                ["zh-CN"] = Create("自动索敌与目标锁定", "鼠标辅助瞄准", "视野距离",
                    "按下键盘上的切换目标键，或使用键盘攻击，即进入键盘战斗并隐藏鼠标，无敌人时也可启用。短按松开锁定或切换目标；长按解除手动锁定，但保持键盘战斗。近战与远程锁定后都瞄准目标。未锁定时近战跟随移动，远程与技能保持附近可见、无遮挡的敌人。无目标时跟随移动方向，停下保留朝向。菜单和旅行期间暂停索敌。移动、点击或滚动鼠标恢复鼠标操作。手柄未锁定时保留原生瞄准。",
                    "将鼠标瞄准吸附到周围 96 像素内、位于屏幕中且无遮挡的敌人；不改变自动索敌、手动目标锁定或未锁定时的手柄原生瞄准。",
                    "缩放原生镜头的最终视野，同时保留剧情缩放与多人镜头构图。",
                    "开启", "可见目标"),
                ["zh-TW"] = Create("自動索敵與目標鎖定", "滑鼠輔助瞄準", "視野距離",
                    "按下鍵盤上的切換目標鍵，或使用鍵盤攻擊，即進入鍵盤戰鬥並隱藏滑鼠，無敵人時也可啟用。短按放開鎖定或切換目標；長按解除手動鎖定，但保持鍵盤戰鬥。近戰與遠程鎖定後都瞄準目標。未鎖定時近戰跟隨移動，遠程與技能保持附近可見、無遮擋的敵人。無目標時跟隨移動方向，停下保留朝向。選單和旅行期間暫停索敵。移動、點擊或捲動滑鼠恢復滑鼠操作。手把未鎖定時保留原生瞄準。",
                    "將滑鼠瞄準吸附到周圍 96 像素內、位於畫面中且無遮擋的敵人；不改變自動索敵、手動目標鎖定或未鎖定時的手把原生瞄準。",
                    "縮放原生鏡頭的最終視野，同時保留劇情縮放與多人鏡頭構圖。",
                    "開啟", "可見目標"),
                ["ko-KR"] = Create("자동 조준 및 대상 고정", "마우스 조준 보정", "시야 거리",
                    "키보드의 대상 전환 키나 공격 키를 누르면 적이 없어도 키보드 전투로 전환되고 포인터가 숨겨집니다. 짧게 눌렀다 놓으면 대상을 고정하거나 전환하고, 길게 누르면 키보드 전투를 유지하며 수동 고정을 해제합니다. 고정 중에는 근접 및 원거리 공격 모두 대상을 조준합니다. 고정하지 않은 근접 공격은 이동 방향을 따르고 원거리 공격과 능력은 가까운 시야 내의 가려지지 않은 적을 유지합니다. 대상이 없으면 이동 방향으로 조준하며 멈추면 방향을 유지합니다. 메뉴와 이동 로딩 중에는 조준을 중단합니다. 마우스 이동, 클릭 또는 스크롤로 마우스 조작을 재개합니다. 고정하지 않은 게임패드 조준은 기본 동작을 유지합니다.",
                    "마우스 주변 96픽셀의 화면 안 가려지지 않은 적에게 조준을 보정합니다. 자동 키보드 및 게임패드 조준은 그대로입니다.",
                    "연출 줌과 멀티플레이 구도를 유지하면서 최종 카메라 시야를 조절합니다.",
                    "활성화", "보이는 대상"),
                ["ja-JP"] = Create("自動照準とターゲットロック", "マウス照準アシスト", "表示範囲",
                    "キーボードの対象切替キーか攻撃キーを押すと、敵がいなくてもキーボード戦闘に入り、ポインターが隠れます。短く押して離すとロックまたは対象切替、長押しするとキーボード戦闘を保ったまま手動ロックを解除します。ロック中は近接・遠距離とも対象を狙います。未ロックの近接は移動方向に従い、遠距離とアビリティは近くの見える遮られていない敵を維持します。対象がいないと移動方向を向き、停止時は向きを保ちます。メニューと移動ロード中は照準を中断します。マウス移動・クリック・スクロールでマウス操作に戻ります。未ロックのゲームパッド照準は標準動作のままです。",
                    "マウスの96ピクセル以内にいる、画面内で遮られていない敵へ照準を補正します。キーボードの自動照準とゲームパッドは変更しません。",
                    "演出ズームとマルチプレイの構図を保ったまま、最終的なカメラ範囲を調整します。",
                    "有効", "見える対象"),
                ["de-DE"] = Create("Automatische Zielwahl & Zielerfassung", "Maus-Zielhilfe", "Sichtweite",
                    "Zielwechseltaste oder Angriff auf der Tastatur drücken: Tastaturkampf mit verborgenem Zeiger, auch ohne Gegner. Kurz drücken und loslassen erfasst oder wechselt Ziele; halten löst nur die manuelle Erfassung. Erfasste Nah- und Fernangriffe zielen auf den Gegner. Ohne Erfassung folgt Nahkampf der Bewegung; Fernangriffe und Fähigkeiten behalten nahe, sichtbare Gegner ohne Hindernisse. Ohne Ziel bestimmt Bewegung die Richtung, Stillstand behält sie bei. Menüs und Reisen unterbrechen das Zielen. Mausbewegung, Klick oder Scrollen aktiviert die Maussteuerung. Gamepad-Zielen ohne Erfassung bleibt unverändert.",
                    "Richtet die Maus innerhalb von 96 Pixeln auf sichtbare, freie Gegner aus. Automatische Tastatur- und Gamepad-Zielhilfe bleiben unverändert.",
                    "Skaliert die endgültige Kamerasicht und erhält Skript-Zoom und Mehrspieler-Bildrahmen.",
                    "Aktiviert", "Sichtbare Ziele"),
                ["es-ES"] = Create("Selección automática y fijación", "Ayuda de puntería del ratón", "Distancia de vista",
                    "Pulsa el cambio de objetivo o un ataque con el teclado para entrar en combate con teclado y ocultar el puntero, incluso sin enemigos. Pulsa y suelta para fijar o cambiar de objetivo; mantén pulsado para liberar la fijación sin salir del combate con teclado. Los ataques cuerpo a cuerpo y a distancia apuntan al objetivo fijado. Sin fijación, el cuerpo a cuerpo sigue el movimiento; los ataques a distancia y habilidades mantienen enemigos cercanos, visibles y sin obstáculos. Sin objetivo, el movimiento dirige la mira y al parar se conserva la dirección. Los menús y viajes suspenden la selección. Mueve, pulsa o desplaza la rueda del ratón para recuperar su control. La mira del mando sin fijación conserva su comportamiento original.",
                    "Ajusta el ratón a enemigos visibles y sin obstáculos en un radio de 96 píxeles. No cambia el objetivo automático ni la ayuda del mando.",
                    "Escala la vista final conservando el zoom de escenas y el encuadre multijugador.",
                    "Activado", "Objetivos visibles"),
                ["fr-FR"] = Create("Ciblage automatique et verrouillage", "Aide à la visée à la souris", "Distance de vue",
                    "Appuyez sur le changement de cible ou une attaque au clavier pour passer au combat au clavier et masquer le pointeur, même sans ennemi. Appuyez puis relâchez pour verrouiller ou changer de cible ; maintenez pour libérer le verrouillage sans quitter ce mode. Les attaques de mêlée et à distance visent la cible verrouillée. Sans verrouillage, la mêlée suit le mouvement ; les attaques à distance et capacités gardent les ennemis proches, visibles et sans obstacle. Sans cible, le déplacement oriente la visée, conservée à l’arrêt. Les menus et voyages suspendent le ciblage. Bougez, cliquez ou utilisez la molette pour reprendre la souris. La visée à la manette sans verrouillage reste inchangée.",
                    "Ajuste la souris vers les ennemis visibles et dégagés dans un rayon de 96 pixels. Le ciblage automatique et la manette restent inchangés.",
                    "Ajuste la vue finale en conservant les zooms scriptés et le cadrage multijoueur.",
                    "Activé", "Cibles visibles"),
                ["it-IT"] = Create("Mira automatica e blocco bersaglio", "Mira assistita del mouse", "Distanza visuale",
                    "Premi il cambio bersaglio o un attacco sulla tastiera per usare il combattimento da tastiera e nascondere il puntatore, anche senza nemici. Premi e rilascia per agganciare o cambiare bersaglio; tieni premuto per sganciarlo restando in questa modalità. Gli attacchi ravvicinati e a distanza mirano al bersaglio agganciato. Senza aggancio, la mischia segue il movimento; attacchi a distanza e abilità mantengono nemici vicini, visibili e senza ostacoli. Senza bersaglio, il movimento orienta la mira, che resta ferma quando ti fermi. Menu e viaggi sospendono il puntamento. Muovi, clicca o scorri il mouse per riprenderne il controllo. La mira del controller senza aggancio resta invariata.",
                    "Corregge il mouse verso nemici visibili e non coperti entro 96 pixel. Il bersaglio automatico e il gamepad non cambiano.",
                    "Scala la vista finale mantenendo zoom di scena e inquadratura multigiocatore.",
                    "Attivato", "Bersagli visibili"),
                ["pl-PL"] = Create("Automatyczne celowanie i blokada celu", "Wspomaganie celowania myszą", "Zasięg widoku",
                    "Naciśnij klawisz zmiany celu lub ataku, aby włączyć walkę klawiaturą i ukryć wskaźnik, także bez wrogów. Naciśnij i zwolnij, aby zablokować lub zmienić cel; przytrzymaj, aby usunąć blokadę bez opuszczania tego trybu. Ataki wręcz i dystansowe celują w zablokowanego wroga. Bez blokady walka wręcz podąża za ruchem, a ataki dystansowe i zdolności utrzymują bliskich, widocznych i niezasłoniętych wrogów. Bez celu ruch wyznacza kierunek, zachowany po zatrzymaniu. Menu i podróż zawieszają celowanie. Ruch, kliknięcie lub przewijanie myszą przywraca jej sterowanie. Celowanie padem bez blokady pozostaje bez zmian.",
                    "Koryguje mysz na widocznych, niezasłoniętych wrogów w promieniu 96 pikseli. Automatyczne celowanie i pad pozostają bez zmian.",
                    "Skaluje końcowy widok kamery, zachowując zoom scen i kadr wieloosobowy.",
                    "Włączone", "Widoczne cele"),
                ["pt-BR"] = Create("Mira automática e trava de alvo", "Assistência de mira do mouse", "Distância de visão",
                    "Pressione a troca de alvo ou um ataque no teclado para entrar no combate por teclado e ocultar o ponteiro, mesmo sem inimigos. Pressione e solte para travar ou trocar o alvo; segure para liberar a trava sem sair desse modo. Ataques corpo a corpo e à distância miram no alvo travado. Sem trava, o corpo a corpo segue o movimento; ataques à distância e habilidades mantêm inimigos próximos, visíveis e sem obstáculos. Sem alvo, o movimento define a mira e parar preserva a direção. Menus e viagens suspendem a seleção. Mova, clique ou role o mouse para retomar seu controle. A mira do controle sem trava permanece original.",
                    "Ajusta o mouse para inimigos visíveis e desobstruídos em um raio de 96 pixels. O alvo automático e o controle não mudam.",
                    "Escala a visão final preservando zooms de cena e enquadramento multijogador.",
                    "Ativado", "Alvos visíveis"),
                ["ru-RU"] = Create("Автонаведение и захват цели", "Помощь прицеливания мышью", "Дальность обзора",
                    "Нажмите клавишу смены цели или атаки, чтобы включить бой с клавиатуры и скрыть указатель даже без врагов. Короткое нажатие с отпусканием захватывает или меняет цель; удержание снимает ручной захват, сохраняя этот режим. Ближние и дальние атаки направлены на захваченную цель. Без захвата ближний бой следует движению, а дальние атаки и способности удерживают близких видимых врагов без преград. Без цели движение задаёт направление, которое сохраняется при остановке. Меню и переходы приостанавливают наведение. Движение, щелчок или прокрутка мыши возвращают управление мышью. Прицеливание геймпадом без захвата не меняется.",
                    "Корректирует мышь по видимым целям без препятствий в радиусе 96 пикселей. Автовыбор и геймпад не меняются.",
                    "Масштабирует итоговый вид, сохраняя сюжетный зум и сетевое кадрирование.",
                    "Включено", "Видимые цели"),
                ["sv-SE"] = Create("Automatiskt sikte och mållåsning", "Mushjälp för sikte", "Synavstånd",
                    "Tryck på målbyte eller en attack på tangentbordet för tangentbordsstrid med dold pekare, även utan fiender. Tryck och släpp för att låsa eller byta mål; håll inne för att släppa det manuella låset men behålla läget. Låsta när- och distansattacker siktar på målet. Utan lås följer närstrid rörelsen; distansattacker och förmågor behåller närliggande, synliga fiender utan hinder. Utan mål styr rörelsen siktet, som behålls när du stannar. Menyer och resor pausar målsökningen. Flytta, klicka eller rulla musen för att återgå till musstyrning. Handkontrollens sikte utan lås är oförändrat.",
                    "Justerar musen mot synliga mål utan hinder inom 96 pixlar. Automatiskt sikte och handkontroll ändras inte.",
                    "Skalar den slutliga kameravyn men behåller scenzoom och flerspelarbild.",
                    "Aktiverat", "Synliga mål"),
                ["th-TH"] = Create("เล็งอัตโนมัติและล็อกเป้าหมาย", "ตัวช่วยเล็งเมาส์", "ระยะการมองเห็น",
                    "กดปุ่มสลับเป้าหมายหรือโจมตีบนแป้นพิมพ์เพื่อเข้าสู่การต่อสู้ด้วยแป้นพิมพ์และซ่อนตัวชี้ แม้ไม่มีศัตรู กดแล้วปล่อยเพื่อล็อกหรือสลับเป้าหมาย กดค้างเพื่อปลดล็อกเองโดยยังใช้แป้นพิมพ์ การโจมตีประชิดและระยะไกลจะเล็งเป้าหมายที่ล็อกไว้ เมื่อไม่ล็อก การโจมตีประชิดตามทิศเคลื่อนที่ ส่วนระยะไกลและความสามารถคงศัตรูใกล้เคียงที่มองเห็นและไม่มีสิ่งกีดขวาง หากไม่มีเป้าหมายจะเล็งตามการเคลื่อนที่และคงทิศเมื่อหยุด เมนูและการเดินทางจะพักการเล็ง ขยับ คลิก หรือเลื่อนเมาส์เพื่อกลับไปใช้เมาส์ การเล็งด้วยจอยเมื่อไม่ล็อกยังเป็นแบบเดิม",
                    "ปรับเมาส์ไปยังศัตรูที่มองเห็นและไม่มีสิ่งกีดขวางภายใน 96 พิกเซล โดยไม่เปลี่ยนการเล็งอัตโนมัติหรือจอย",
                    "ปรับมุมมองสุดท้ายโดยคงการซูมเนื้อเรื่องและเฟรมผู้เล่นหลายคน",
                    "เปิดใช้งาน", "เป้าหมายที่มองเห็น"),
                ["tr-TR"] = Create("Otomatik hedefleme ve hedef kilidi", "Fare nişan yardımı", "Görüş mesafesi",
                    "Düşman olmasa bile klavyeyle hedef değiştirme veya saldırı tuşuna basarak klavye savaşına geçin ve işaretçiyi gizleyin. Basıp bırakmak hedefi kilitler veya değiştirir; basılı tutmak bu moddan çıkmadan elle kilidi kaldırır. Kilitli yakın ve uzak saldırılar hedefe yönelir. Kilitsiz yakın dövüş hareketi izler; menzilli saldırılar ve yetenekler yakındaki görünür, engelsiz düşmanları korur. Hedef yoksa hareket nişanı belirler, durunca yön korunur. Menüler ve yolculuk hedeflemeyi duraklatır. Fareyi hareket ettirmek, tıklamak veya kaydırmak fare kontrolünü geri getirir. Kilitsiz oyun kumandası nişanı değişmez.",
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
