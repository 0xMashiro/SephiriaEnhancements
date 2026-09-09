using System;
using System.Collections.Generic;
using System.Globalization;

namespace SephiriaEnhancements.MultiplayerRules.Presentation
{
    internal static partial class MultiplayerRulesLocalization
    {
        internal const string Section = "SephiriaEnhancements.MultiplayerRules.Section";
        internal const string PresetSetting = "SephiriaEnhancements.MultiplayerRules.PresetSetting";
        internal const string PresetHelp = "SephiriaEnhancements.MultiplayerRules.PresetHelp";
        internal const string ExternalRuleStackingSetting =
            "SephiriaEnhancements.MultiplayerRules.ExternalRuleStacking";
        internal const string ExternalRuleStackingHelp =
            "SephiriaEnhancements.MultiplayerRules.ExternalRuleStacking.Help";
        internal const string ParticipantCountSetting = "SephiriaEnhancements.MultiplayerRules.ParticipantCount";
        internal const string ParticipantCountHelp = "SephiriaEnhancements.MultiplayerRules.ParticipantCount.Help";
        internal const string CopyParticipantValuesSetting = "SephiriaEnhancements.MultiplayerRules.CopyParticipantValues";
        internal const string CopyParticipantValuesHelp = "SephiriaEnhancements.MultiplayerRules.CopyParticipantValues.Help";
        internal const string SelectCopyTarget = "SephiriaEnhancements.MultiplayerRules.CopyParticipantValues.SelectTarget";
        internal const string HealthCombinationSetting = "SephiriaEnhancements.MultiplayerRules.HealthCombination";
        internal const string HealthCombinationHelp = "SephiriaEnhancements.MultiplayerRules.HealthCombination.Help";
        internal const string OriginalPreset = "SephiriaEnhancements.MultiplayerRules.Preset.Original";
        internal const string OptimizedPreset = "SephiriaEnhancements.MultiplayerRules.Preset.Optimized";
        internal const string CustomPreset = "SephiriaEnhancements.MultiplayerRules.Preset.Custom";
        internal const string UseGameBehavior = "SephiriaEnhancements.MultiplayerRules.Value.UseGameBehavior";
        internal const string ToggleDisabled = "SephiriaEnhancements.MultiplayerRules.Value.Disabled";
        internal const string ToggleEnabled = "SephiriaEnhancements.MultiplayerRules.Value.Enabled";
        internal const string GroupSpawnAndDifficulty = "SephiriaEnhancements.MultiplayerRules.Group.SpawnAndDifficulty";
        internal const string GroupEnemyStats = "SephiriaEnhancements.MultiplayerRules.Group.EnemyStats";
        internal const string GroupEncountersAndBosses = "SephiriaEnhancements.MultiplayerRules.Group.EncountersAndBosses";
        internal const string GroupRewardsAndSupplies = "SephiriaEnhancements.MultiplayerRules.Group.RewardsAndSupplies";
        internal const string GroupMerchants = "SephiriaEnhancements.MultiplayerRules.Group.Merchants";
        internal const string GroupQliphoth = "SephiriaEnhancements.MultiplayerRules.Group.Qliphoth";
        internal const string RuleGroupSetting =
            "SephiriaEnhancements.MultiplayerRules.RuleGroup";
        internal const string RuleGroupHelp =
            "SephiriaEnhancements.MultiplayerRules.RuleGroup.Help";

        internal static readonly string[] PresetKeys =
        {
            OriginalPreset, OptimizedPreset, CustomPreset
        };

        internal static readonly string[] HealthCombinationKeys =
        {
            "SephiriaEnhancements.MultiplayerRules.HealthCombination.ParticipantRuleOnly",
            "SephiriaEnhancements.MultiplayerRules.HealthCombination.Additive",
            "SephiriaEnhancements.MultiplayerRules.HealthCombination.Multiplicative"
        };

        private static readonly string[] CommonTextKeys =
        {
            Section, PresetSetting, PresetHelp, ExternalRuleStackingSetting,
            ExternalRuleStackingHelp, ParticipantCountSetting,
            ParticipantCountHelp, CopyParticipantValuesSetting,
            CopyParticipantValuesHelp, SelectCopyTarget,
            HealthCombinationSetting, HealthCombinationHelp,
            OriginalPreset, OptimizedPreset, CustomPreset, UseGameBehavior,
            HealthCombinationKeys[0], HealthCombinationKeys[1],
            HealthCombinationKeys[2], ToggleDisabled, ToggleEnabled,
            GroupSpawnAndDifficulty, GroupEnemyStats, GroupEncountersAndBosses,
            GroupRewardsAndSupplies, GroupMerchants, GroupQliphoth,
            RuleGroupSetting, RuleGroupHelp
        };

        private static readonly Dictionary<string, string[]> CommonTexts = new()
        {
            ["en-US"] = new[]
            {
                "Multiplayer", "Rule Preset",
                "The host's selection is frozen when exploration starts. Original delegates every value to the current game. Optimized fixes only confirmed health-scaling anomalies. Custom enables the rules below.",
                "Stack Rules with Multiplayer Extensions", "Advanced compatibility option. Disabled lets detected multiplayer extensions own scaling and other rules. Enable only when you intentionally want both rule systems to apply; parties above four always use external or game behavior.",
                "Settings for player count", "Choose a team size from 1–4. The settings below apply when the team has that many players.",
                "Copy settings to player count", "Copy every custom rule from the currently edited participant count to the selected participant count. The target values are overwritten immediately.", "Select target",
                "Enemy health scaling", "Choose how floor and Hard Mode bonuses affect custom enemy health. With 2× health and a +50% bonus: ignore gives 2×, add gives 2.5×, multiply gives 3×.",
                "Original", "Optimized", "Custom", "Use game behavior",
                "Ignore other health bonuses", "Add other health bonuses", "Multiply by other health bonuses", "Disabled", "Enabled",
                "Spawning and Difficulty", "Enemy Stats", "Encounters and Bosses",
                "Rewards and Supplies", "Merchants", "Qliphoth",
                "Rule Group", "Choose which custom multiplayer-rule group is shown below."
            },
            ["zh-CN"] = new[]
            {
                "多人游戏", "规则预设",
                "开始探索时固定房主的选择。原版将每项数值交给当前游戏处理；优化仅修正确认的生命缩放异常；自定义启用下方规则。",
                "与联机扩展叠加规则", "高级兼容选项。禁用时，由检测到的联机扩展负责缩放与其他规则。仅在明确希望两套规则同时生效时启用；超过四人的队伍始终使用外部扩展或游戏行为。",
                "设置几人队伍", "选择要调整的队伍人数（1–4 人）。队伍达到该人数时，使用下方对应设置。",
                "复制设置到其他人数", "将当前正在编辑人数的全部自定义规则复制到所选人数。目标人数的参数会立即被覆盖。", "选择目标人数",
                "敌人生命加成", "选择楼层和困难模式加成如何影响自定义生命。例如生命设为 2 倍，另有 50% 加成：忽略后仍为 2 倍，相加为 2.5 倍，相乘为 3 倍。",
                "原版", "优化", "自定义", "使用游戏行为",
                "忽略其他生命加成", "加上其他生命加成", "再乘其他生命加成", "禁用", "启用",
                "生成与难度", "敌人属性", "遭遇与 Boss", "奖励与补给", "商人", "克里弗",
                "规则分组", "选择下方显示的自定义多人游戏规则组。"
            },
            ["zh-TW"] = new[]
            {
                "多人遊戲", "規則預設",
                "開始探索時固定房主的選擇。原版將每項數值交給目前遊戲處理；最佳化僅修正已確認的生命縮放異常；自訂啟用下方規則。",
                "與連線擴充套件疊加規則", "進階相容選項。停用時，由偵測到的連線擴充套件負責縮放與其他規則。僅在明確希望兩套規則同時生效時啟用；超過四人的隊伍一律使用外部擴充套件或遊戲行為。",
                "設定幾人隊伍", "選擇要調整的隊伍人數（1–4 人）。隊伍達到該人數時，使用下方對應設定。",
                "複製設定到其他人數", "將目前正在編輯人數的全部自訂規則複製到所選人數。目標人數的參數會立即被覆寫。", "選擇目標人數",
                "敵人生命加成", "選擇樓層和困難模式加成如何影響自訂生命。例如生命設為 2 倍，另有 50% 加成：忽略後仍為 2 倍，相加為 2.5 倍，相乘為 3 倍。",
                "原版", "最佳化", "自訂", "使用遊戲行為",
                "忽略其他生命加成", "加上其他生命加成", "再乘其他生命加成", "停用", "啟用",
                "產生與難度", "敵人屬性", "遭遇與 Boss", "獎勵與補給", "商人", "克里弗",
                "規則分組", "選擇下方顯示的自訂多人遊戲規則群組。"
            },
            ["ja-JP"] = new[] { "マルチプレイ",
                    "ルールのプリセット",
                    "ホストの設定は探索開始時に固定されます。元の設定はすべてゲームに任せます。最適化は確認済みのHP倍率の不具合のみを修正します。カスタムでは下のルールを使用します。",
                    "マルチプレイ拡張とルールを併用",
                    "上級者向けの互換設定です。オフでは検出した拡張に倍率などのルールを任せます。両方のルールを適用したい場合だけ有効にしてください。4人を超える場合は常に拡張またはゲームのルールを使います。",
                    "設定するチーム人数",
                    "1～4人から選びます。チームがその人数のとき、下の設定が使われます。",
                    "別の人数へ設定をコピー",
                    "現在編集中の人数の全カスタムルールを選択した人数へコピーします。コピー先の設定は即座に上書きされます。",
                    "コピー先を選択",
                    "敵のHPボーナス",
                    "フロアとハードモードの補正を自分で設定したHPにどう適用するか選びます。HPが2倍、補正が+50%なら、無視で2倍、加算で2.5倍、乗算で3倍になります。",
                    "元の設定",
                    "最適化",
                    "カスタム",
                    "ゲームに任せる",
                    "他のHP補正を無視",
                    "他のHP補正を加算",
                    "他のHP補正を乗算",
                    "無効",
                    "有効",
                    "出現と難易度",
                    "敵の能力",
                    "遭遇とボス",
                    "報酬と補給",
                    "商人",
                    "クリフォト",
                    "ルールの分類",
                    "下に表示するカスタムルールの分類を選びます。" },
            ["ko-KR"] = new[] { "멀티플레이",
                    "규칙 프리셋",
                    "호스트의 설정은 탐험 시작 시 고정됩니다. 원본은 모든 값을 게임에 맡깁니다. 최적화는 확인된 체력 배율 이상만 수정합니다. 사용자 설정은 아래 규칙을 사용합니다.",
                    "멀티플레이 확장과 규칙 중첩",
                    "고급 호환 설정입니다. 끄면 감지된 확장이 배율과 기타 규칙을 관리합니다. 두 규칙을 함께 적용하려는 경우에만 켜세요. 4명을 넘는 파티는 항상 확장이나 게임의 동작을 따릅니다.",
                    "설정할 팀 인원",
                    "1~4명 중 선택합니다. 팀이 해당 인원일 때 아래 설정이 적용됩니다.",
                    "다른 인원에 설정 복사",
                    "현재 편집 중인 인원의 모든 사용자 규칙을 선택한 인원으로 복사합니다. 대상 설정은 즉시 덮어씁니다.",
                    "대상 선택",
                    "적 체력 보너스",
                    "층과 하드모드 보너스를 사용자 설정 체력에 적용하는 방식입니다. 체력 2배에 +50% 보너스라면 무시는 2배, 더하기는 2.5배, 곱하기는 3배입니다.",
                    "원본",
                    "최적화",
                    "사용자 설정",
                    "게임 동작 사용",
                    "다른 체력 보너스 무시",
                    "다른 체력 보너스 더하기",
                    "다른 체력 보너스 곱하기",
                    "비활성화",
                    "활성화",
                    "생성과 난이도",
                    "적 능력치",
                    "조우와 보스",
                    "보상과 보급",
                    "상인",
                    "클리포트",
                    "규칙 분류",
                    "아래에 표시할 사용자 멀티플레이 규칙 분류를 선택합니다." },
            ["de-DE"] = new[] { "Mehrspieler",
                    "Regelprofil",
                    "Die Auswahl des Hosts wird beim Start der Erkundung festgelegt. Original überlässt alle Werte dem Spiel. Optimiert korrigiert nur bestätigte Fehler der Lebensskalierung. Benutzerdefiniert aktiviert die folgenden Regeln.",
                    "Regeln mit Mehrspieler-Erweiterungen kombinieren",
                    "Erweiterte Kompatibilitätsoption. Aus überlässt Skalierung und Regeln erkannten Erweiterungen. Nur aktivieren, wenn beide Regelsysteme gelten sollen. Gruppen über vier nutzen immer Erweiterungs- oder Spielverhalten.",
                    "Einstellungen für Spielerzahl",
                    "Wähle eine Teamgröße von 1–4. Bei dieser Spielerzahl gelten die folgenden Einstellungen.",
                    "Einstellungen für andere Spielerzahl kopieren",
                    "Kopiert alle benutzerdefinierten Regeln der bearbeiteten Teilnehmerzahl auf die ausgewählte Zahl. Zielwerte werden sofort überschrieben.",
                    "Ziel auswählen",
                    "Lebensboni der Gegner",
                    "Bestimmt den Einfluss von Ebene und Schwerem Modus auf das eingestellte Leben. Bei 2× Leben und +50% Bonus: ignorieren ergibt 2×, addieren 2,5×, multiplizieren 3×.",
                    "Original",
                    "Optimiert",
                    "Benutzerdefiniert",
                    "Spielverhalten nutzen",
                    "Andere Lebensboni ignorieren",
                    "Andere Lebensboni addieren",
                    "Mit anderen Lebensboni multiplizieren",
                    "Deaktiviert",
                    "Aktiviert",
                    "Spawns und Schwierigkeit",
                    "Gegnerwerte",
                    "Begegnungen und Bosse",
                    "Belohnungen und Vorräte",
                    "Händler",
                    "Qliphoth",
                    "Regelgruppe",
                    "Wählt die unten angezeigte Gruppe benutzerdefinierter Mehrspielerregeln." },
            ["es-ES"] = new[] { "Multijugador",
                    "Preajuste de reglas",
                    "La selección del anfitrión queda fijada al iniciar la expedición. Original deja todos los valores al juego. Optimizado solo corrige anomalías confirmadas del escalado de vida. Personalizado activa las reglas de abajo.",
                    "Combinar reglas con extensiones multijugador",
                    "Opción avanzada de compatibilidad. Desactivada deja el escalado y las reglas a las extensiones detectadas. Actívala solo para aplicar ambos sistemas. Los grupos de más de cuatro siempre usan las reglas de las extensiones o del juego.",
                    "Ajustes por número de jugadores",
                    "Elige un tamaño de equipo de 1 a 4. Los ajustes de abajo se aplican cuando hay ese número de jugadores.",
                    "Copiar ajustes a otro número de jugadores",
                    "Copia todas las reglas personalizadas del número de participantes actual al seleccionado. Los valores de destino se sobrescriben inmediatamente.",
                    "Seleccionar destino",
                    "Bonificaciones de vida enemiga",
                    "Elige cómo afectan el piso y el modo difícil a la vida personalizada. Con 2× de vida y +50%: ignorar da 2×, sumar da 2,5× y multiplicar da 3×.",
                    "Original",
                    "Optimizado",
                    "Personalizado",
                    "Usar reglas del juego",
                    "Ignorar otras bonificaciones de vida",
                    "Sumar otras bonificaciones de vida",
                    "Multiplicar por otras bonificaciones de vida",
                    "Desactivado",
                    "Activado",
                    "Apariciones y dificultad",
                    "Atributos de enemigos",
                    "Encuentros y jefes",
                    "Recompensas y suministros",
                    "Comerciantes",
                    "Qliphoth",
                    "Grupo de reglas",
                    "Elige el grupo de reglas multijugador personalizadas que se muestra debajo." },
            ["fr-FR"] = new[] { "Multijoueur",
                    "Préréglage des règles",
                    "Le choix de l’hôte est fixé au départ de l’exploration. Original délègue toutes les valeurs au jeu. Optimisé corrige uniquement les anomalies confirmées de mise à l’échelle des PV. Personnalisé active les règles ci-dessous.",
                    "Cumuler les règles avec les extensions multijoueurs",
                    "Option avancée de compatibilité. Désactivée, elle laisse les extensions détectées gérer les règles et multiplicateurs. Activez-la uniquement pour appliquer les deux systèmes. Au-delà de quatre joueurs, les règles des extensions ou du jeu s’appliquent toujours.",
                    "Réglages par nombre de joueurs",
                    "Choisissez une équipe de 1 à 4 joueurs. Les réglages ci-dessous s'appliquent lorsque l'équipe compte ce nombre de joueurs.",
                    "Copier vers un autre nombre de joueurs",
                    "Copie toutes les règles personnalisées du nombre de participants actuel vers celui choisi. Les valeurs cibles sont immédiatement écrasées.",
                    "Choisir la cible",
                    "Bonus de vie des ennemis",
                    "Choisissez l'effet des bonus d'étage et du mode difficile sur la vie personnalisée. Avec 2× et +50% : ignorer donne 2×, additionner 2,5×, multiplier 3×.",
                    "Original",
                    "Optimisé",
                    "Personnalisé",
                    "Suivre le jeu",
                    "Ignorer les autres bonus de vie",
                    "Ajouter les autres bonus de vie",
                    "Multiplier par les autres bonus de vie",
                    "Désactivé",
                    "Activé",
                    "Apparitions et difficulté",
                    "Attributs ennemis",
                    "Rencontres et boss",
                    "Récompenses et provisions",
                    "Marchands",
                    "Qliphoth",
                    "Groupe de règles",
                    "Choisissez le groupe de règles multijoueurs personnalisées à afficher ci-dessous." },
            ["it-IT"] = new[] { "Multigiocatore",
                    "Profilo regole",
                    "La scelta dell’host viene fissata all’inizio dell’esplorazione. Originale affida tutti i valori al gioco. Ottimizzato corregge solo anomalie confermate del ridimensionamento della salute. Personalizzato attiva le regole sottostanti.",
                    "Combina regole con estensioni multigiocatore",
                    "Opzione avanzata di compatibilità. Disattivata lascia regole e moltiplicatori alle estensioni rilevate. Attivala solo per applicare entrambi i sistemi. I gruppi oltre quattro usano sempre le regole delle estensioni o del gioco.",
                    "Impostazioni per numero di giocatori",
                    "Scegli una squadra da 1 a 4 giocatori. Le impostazioni sotto si applicano quando la squadra ha quel numero di giocatori.",
                    "Copia impostazioni per un altro numero",
                    "Copia tutte le regole personalizzate del numero di partecipanti attuale su quello scelto. I valori di destinazione vengono sovrascritti subito.",
                    "Scegli destinazione",
                    "Bonus alla vita dei nemici",
                    "Scegli come i bonus del piano e della modalità difficile modificano la vita personalizzata. Con 2× e +50%: ignorare dà 2×, sommare 2,5×, moltiplicare 3×.",
                    "Originale",
                    "Ottimizzato",
                    "Personalizzato",
                    "Usa regole del gioco",
                    "Ignora gli altri bonus alla vita",
                    "Somma gli altri bonus alla vita",
                    "Moltiplica per gli altri bonus alla vita",
                    "Disattivato",
                    "Attivato",
                    "Generazione e difficoltà",
                    "Attributi nemici",
                    "Incontri e boss",
                    "Ricompense e scorte",
                    "Mercanti",
                    "Qliphoth",
                    "Gruppo di regole",
                    "Scegli il gruppo di regole multigiocatore personalizzate da mostrare qui sotto." },
            ["pl-PL"] = new[] { "Tryb wieloosobowy",
                    "Zestaw reguł",
                    "Wybór gospodarza zostaje ustalony na początku wyprawy. Oryginalny pozostawia wszystkie wartości grze. Zoptymalizowany poprawia tylko potwierdzone błędy skalowania zdrowia. Własny włącza reguły poniżej.",
                    "Łącz reguły z rozszerzeniami wieloosobowymi",
                    "Zaawansowana opcja zgodności. Wyłączona pozostawia skalowanie i reguły wykrytym rozszerzeniom. Włącz tylko, gdy oba systemy mają działać jednocześnie. Grupy powyżej czterech zawsze korzystają z reguł rozszerzeń lub gry.",
                    "Ustawienia liczby graczy",
                    "Wybierz drużynę od 1 do 4 graczy. Poniższe ustawienia działają, gdy drużyna liczy tyle osób.",
                    "Kopiuj ustawienia dla innej liczby graczy",
                    "Kopiuje wszystkie własne reguły bieżącej liczby uczestników do wybranej liczby. Wartości docelowe zostaną natychmiast nadpisane.",
                    "Wybierz cel",
                    "Premie zdrowia przeciwników",
                    "Wybierz wpływ premii piętra i trybu trudnego na własne ustawienie zdrowia. Przy 2× i +50%: pominięcie daje 2×, dodanie 2,5×, mnożenie 3×.",
                    "Oryginalny",
                    "Zoptymalizowany",
                    "Własny",
                    "Użyj reguł gry",
                    "Pomiń inne premie zdrowia",
                    "Dodaj inne premie zdrowia",
                    "Pomnóż przez inne premie zdrowia",
                    "Wyłączone",
                    "Włączone",
                    "Pojawianie i trudność",
                    "Atrybuty wrogów",
                    "Spotkania i bossowie",
                    "Nagrody i zaopatrzenie",
                    "Kupcy",
                    "Qliphoth",
                    "Grupa reguł",
                    "Wybierz grupę własnych reguł wieloosobowych wyświetlaną poniżej." },
            ["pt-BR"] = new[] { "Multijogador",
                    "Predefinição de regras",
                    "A escolha do anfitrião é fixada no início da exploração. Original deixa todos os valores com o jogo. Otimizada corrige apenas anomalias confirmadas do ajuste de vida. Personalizada ativa as regras abaixo.",
                    "Combinar regras com extensões multijogador",
                    "Opção avançada de compatibilidade. Desativada deixa regras e multiplicadores com as extensões detectadas. Ative apenas para aplicar os dois sistemas. Grupos acima de quatro sempre usam as regras das extensões ou do jogo.",
                    "Ajustes por número de jogadores",
                    "Escolha uma equipe de 1 a 4 jogadores. Os ajustes abaixo valem quando a equipe tem esse número de jogadores.",
                    "Copiar ajustes para outro número",
                    "Copia todas as regras personalizadas do número de participantes atual para o escolhido. Os valores de destino são substituídos imediatamente.",
                    "Selecionar destino",
                    "Bônus de vida dos inimigos",
                    "Escolha como os bônus do andar e do modo difícil afetam a vida personalizada. Com 2× de vida e +50%: ignorar dá 2×, somar dá 2,5× e multiplicar dá 3×.",
                    "Original",
                    "Otimizada",
                    "Personalizada",
                    "Usar regras do jogo",
                    "Ignorar outros bônus de vida",
                    "Somar outros bônus de vida",
                    "Multiplicar por outros bônus de vida",
                    "Desativado",
                    "Ativado",
                    "Surgimento e dificuldade",
                    "Atributos dos inimigos",
                    "Encontros e chefes",
                    "Recompensas e suprimentos",
                    "Comerciantes",
                    "Qlipoth",
                    "Grupo de regras",
                    "Escolha o grupo de regras multijogador personalizadas exibido abaixo." },
            ["ru-RU"] = new[] { "Сетевая игра",
                    "Набор правил",
                    "Выбор хоста фиксируется в начале забега. Исходный передаёт все значения игре. Улучшенный исправляет только подтверждённые ошибки масштабирования здоровья. Свой включает правила ниже.",
                    "Совмещать правила с сетевыми расширениями",
                    "Дополнительная настройка совместимости. В выключенном состоянии правилами и масштабированием управляют обнаруженные расширения. Включайте только для применения обеих систем. Группы свыше четырёх всегда используют правила расширений или игры.",
                    "Настройки по числу игроков",
                    "Выберите размер команды от 1 до 4. Настройки ниже действуют при таком числе игроков.",
                    "Копировать для другого числа игроков",
                    "Копирует все свои правила текущего числа участников на выбранное число. Целевые значения сразу перезаписываются.",
                    "Выбрать цель",
                    "Бонусы здоровья врагов",
                    "Выберите влияние этажа и сложного режима на заданное здоровье. При 2× и бонусе +50%: игнорирование даёт 2×, сложение — 2,5×, умножение — 3×.",
                    "Исходный",
                    "Улучшенный",
                    "Свой",
                    "Использовать правила игры",
                    "Игнорировать другие бонусы здоровья",
                    "Прибавлять другие бонусы здоровья",
                    "Умножать на другие бонусы здоровья",
                    "Отключено",
                    "Включено",
                    "Появление и сложность",
                    "Параметры врагов",
                    "Встречи и боссы",
                    "Награды и припасы",
                    "Торговцы",
                    "Клиппот",
                    "Группа правил",
                    "Выберите группу своих правил сетевой игры для отображения ниже." },
            ["sv-SE"] = new[] { "Flerspelarläge",
                    "Regelförval",
                    "Värdens val låses när utforskningen börjar. Original överlåter alla värden till spelet. Optimerat rättar endast bekräftade fel i hälsoskalningen. Anpassat aktiverar reglerna nedan.",
                    "Kombinera regler med flerspelartillägg",
                    "Avancerad kompatibilitetsinställning. Av låter upptäckta tillägg styra skalning och regler. Aktivera endast om båda systemen ska gälla. Grupper över fyra använder alltid tilläggets eller spelets regler.",
                    "Inställningar för spelarantal",
                    "Välj en lagstorlek på 1–4. Inställningarna nedan gäller när laget har så många spelare.",
                    "Kopiera till annat spelarantal",
                    "Kopierar alla anpassade regler för aktuellt deltagarantal till det valda antalet. Målvärdena skrivs över direkt.",
                    "Välj mål",
                    "Fiendernas hälsobonusar",
                    "Välj hur våningens och det svåra lägets bonusar påverkar anpassad hälsa. Med 2× och +50%: ignorera ger 2×, addera 2,5×, multiplicera 3×.",
                    "Original",
                    "Optimerat",
                    "Anpassat",
                    "Använd spelets regler",
                    "Ignorera andra hälsobonusar",
                    "Addera andra hälsobonusar",
                    "Multiplicera med andra hälsobonusar",
                    "Av",
                    "På",
                    "Spawning och svårighet",
                    "Fiendevärden",
                    "Möten och bossar",
                    "Belöningar och förråd",
                    "Handlare",
                    "Qliphoth",
                    "Regelgrupp",
                    "Välj vilken grupp anpassade flerspelarregler som visas nedan." },
            ["th-TH"] = new[] { "ผู้เล่นหลายคน",
                    "ชุดกฎ",
                    "ตัวเลือกของโฮสต์จะถูกยึดไว้เมื่อเริ่มสำรวจ ดั้งเดิมให้เกมจัดการทุกค่า ปรับปรุงแก้เฉพาะความผิดปกติของการปรับพลังชีวิตที่ยืนยันแล้ว กำหนดเองใช้กฎด้านล่าง",
                    "ใช้กฎร่วมกับส่วนเสริมผู้เล่นหลายคน",
                    "ตัวเลือกความเข้ากันได้ขั้นสูง เมื่อปิด ส่วนเสริมที่ตรวจพบจะจัดการตัวคูณและกฎ เปิดเฉพาะเมื่อต้องการใช้ทั้งสองระบบ ทีมเกินสี่คนใช้กฎส่วนเสริมหรือเกมเสมอ",
                    "ตั้งค่าสำหรับทีมกี่คน",
                    "เลือกขนาดทีม 1–4 คน การตั้งค่าด้านล่างจะใช้เมื่อทีมมีผู้เล่นตามจำนวนนี้",
                    "คัดลอกการตั้งค่าไปยังจำนวนอื่น",
                    "คัดลอกกฎกำหนดเองทั้งหมดของจำนวนผู้เข้าร่วมปัจจุบันไปยังจำนวนที่เลือก ค่าเป้าหมายจะถูกเขียนทับทันที",
                    "เลือกเป้าหมาย",
                    "โบนัสพลังชีวิตศัตรู",
                    "เลือกผลของโบนัสจากชั้นและโหมดยากต่อพลังชีวิตที่กำหนดเอง เช่น 2 เท่ากับโบนัส +50%: ไม่ใช้โบนัสได้ 2 เท่า บวกได้ 2.5 เท่า คูณได้ 3 เท่า",
                    "ดั้งเดิม",
                    "ปรับปรุง",
                    "กำหนดเอง",
                    "ใช้กฎของเกม",
                    "ไม่ใช้โบนัสพลังชีวิตอื่น",
                    "บวกโบนัสพลังชีวิตอื่น",
                    "คูณด้วยโบนัสพลังชีวิตอื่น",
                    "ปิด",
                    "เปิด",
                    "การเกิดและความยาก",
                    "ค่าสถานะศัตรู",
                    "การเผชิญหน้าและบอส",
                    "รางวัลและเสบียง",
                    "พ่อค้า",
                    "Qliphoth",
                    "หมวดกฎ",
                    "เลือกหมวดกฎผู้เล่นหลายคนแบบกำหนดเองที่แสดงด้านล่าง" },
            ["tr-TR"] = new[] { "Çok oyunculu",
                    "Kural ön ayarı",
                    "Sunucu sahibinin seçimi keşif başlarken sabitlenir. Özgün, tüm değerleri oyuna bırakır. İyileştirilmiş, yalnızca doğrulanmış sağlık ölçekleme hatalarını düzeltir. Özel, aşağıdaki kuralları açar.",
                    "Kuralları çok oyunculu eklentilerle birleştir",
                    "Gelişmiş uyumluluk ayarı. Kapalıyken algılanan eklentiler ölçeklemeyi ve kuralları yönetir. Yalnızca iki sistemin de uygulanmasını istiyorsanız açın. Dörtten büyük gruplar daima eklenti veya oyun kurallarını kullanır.",
                    "Oyuncu sayısına göre ayarlar",
                    "1–4 kişilik takım seçin. Takımda bu kadar oyuncu olduğunda aşağıdaki ayarlar uygulanır.",
                    "Başka oyuncu sayısına kopyala",
                    "Mevcut katılımcı sayısının tüm özel kurallarını seçilen sayıya kopyalar. Hedef değerlerin üzerine hemen yazılır.",
                    "Hedef seç",
                    "Düşman sağlık bonusları",
                    "Kat ve zor mod bonuslarının özel sağlığa etkisini seçin. 2× sağlık ve +%50 bonus için: yok sayma 2×, toplama 2,5×, çarpma 3× verir.",
                    "Özgün",
                    "İyileştirilmiş",
                    "Özel",
                    "Oyun kurallarını kullan",
                    "Diğer sağlık bonuslarını yok say",
                    "Diğer sağlık bonuslarını ekle",
                    "Diğer sağlık bonuslarıyla çarp",
                    "Kapalı",
                    "Açık",
                    "Doğma ve zorluk",
                    "Düşman özellikleri",
                    "Karşılaşmalar ve bosslar",
                    "Ödüller ve ikmal",
                    "Tüccarlar",
                    "Qliphoth",
                    "Kural grubu",
                    "Aşağıda gösterilecek özel çok oyunculu kural grubunu seçin." }
        };

        internal static string RuleLabelKey(MultiplayerRuleId id) =>
            "SephiriaEnhancements.MultiplayerRules.Rule." + id;

        internal static string RuleHelpKey(MultiplayerRuleId id) =>
            RuleLabelKey(id) + ".Help";

        internal static string ParticipantCountValueKey(int participantCount) =>
            "SephiriaEnhancements.MultiplayerRules.ParticipantCount.Participants" +
            participantCount;

        internal static string NumericValueKey(MultiplayerRuleDefinition definition,
            int stepIndex)
        {
            float value = definition.Minimum + definition.Step * stepIndex;
            if (definition.Unit == MultiplayerRuleUnit.Toggle)
                return value <= 0f ? ToggleDisabled : ToggleEnabled;
            return "SephiriaEnhancements.MultiplayerRules.Value." + definition.Unit + "." +
                value.ToString("0.####", CultureInfo.InvariantCulture);
        }

        internal static int NumericValueCount(MultiplayerRuleDefinition definition) =>
            (int)Math.Round((definition.Maximum - definition.Minimum) /
                definition.Step) + 1;

        internal static void Register(Action<string, string, string> addText,
            IEnumerable<string> languages)
        {
            var translations = new Dictionary<string, Dictionary<string, string>>();
            foreach (var language in CommonTexts)
            {
                var texts = new Dictionary<string, string>();
                for (int index = 0; index < language.Value.Length; index++)
                    texts.Add(CommonTextKeys[index], language.Value[index]);
                if (RuleTexts.TryGetValue(language.Key, out var rules))
                    foreach (MultiplayerRuleDefinition definition in MultiplayerRuleCatalog.All)
                        if (rules.TryGetValue(definition.Id, out var ruleText) && ruleText.Length == 2)
                        {
                            texts.Add(RuleLabelKey(definition.Id), ruleText[0]);
                            texts.Add(RuleHelpKey(definition.Id), ruleText[1]);
                        }
                translations.Add(language.Key, texts);
            }
            Configuration.LocalizationGroup.Register(addText, languages, translations);
            foreach (string language in languages)
            {
                var registeredNumericKeys = new HashSet<string>(StringComparer.Ordinal);
                for (int participantCount = 1; participantCount <= 4; participantCount++)
                    addText(language, ParticipantCountValueKey(participantCount),
                        participantCount.ToString(CultureInfo.InvariantCulture));

                foreach (MultiplayerRuleDefinition definition in MultiplayerRuleCatalog.All)
                {
                    if (definition.Unit == MultiplayerRuleUnit.Toggle) continue;
                    int count = NumericValueCount(definition);
                    for (int stepIndex = 0; stepIndex < count; stepIndex++)
                    {
                        string key = NumericValueKey(definition, stepIndex);
                        if (!registeredNumericKeys.Add(key)) continue;
                        float number = definition.Minimum + definition.Step * stepIndex;
                        addText(language, key, FormatValue(number, definition.Unit));
                    }
                }
            }
        }

        private static string FormatValue(float value, MultiplayerRuleUnit unit)
        {
            string number = value.ToString(value % 1f == 0f ? "0" : "0.##",
                CultureInfo.InvariantCulture);
            return unit switch
            {
                MultiplayerRuleUnit.Multiplier => number + "×",
                MultiplayerRuleUnit.PercentagePoints => "+" + number,
                MultiplayerRuleUnit.DifficultyOffset => "+" + number,
                _ => number
            };
        }

    }
}
