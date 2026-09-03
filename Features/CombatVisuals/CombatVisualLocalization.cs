using System;
using System.Collections.Generic;

namespace SephiriaEnhancements.CombatVisuals
{
    internal static class CombatVisualLocalization
    {
        internal const string SettingPreset =
            "SephiriaEnhancements.CombatVisuals.Setting.Preset";
        internal const string HelpPreset =
            "SephiriaEnhancements.CombatVisuals.Help.Preset";
        internal const string SettingCompanionBody =
            "SephiriaEnhancements.CombatVisuals.Setting.CompanionBody";
        internal const string HelpCompanionBody =
            "SephiriaEnhancements.CombatVisuals.Help.CompanionBody";
        internal const string SettingCompanionEffects =
            "SephiriaEnhancements.CombatVisuals.Setting.CompanionEffects";
        internal const string HelpCompanionEffects =
            "SephiriaEnhancements.CombatVisuals.Help.CompanionEffects";
        internal const string SettingOutlineScope =
            "SephiriaEnhancements.CombatVisuals.Setting.OutlineScope";
        internal const string HelpOutlineScope =
            "SephiriaEnhancements.CombatVisuals.Help.OutlineScope";

        internal static readonly string[] PresetKeys =
        {
            "SephiriaEnhancements.CombatVisuals.Preset.FollowGame",
            "SephiriaEnhancements.CombatVisuals.Preset.Balanced",
            "SephiriaEnhancements.CombatVisuals.Preset.Minimal",
            "SephiriaEnhancements.CombatVisuals.Preset.Custom"
        };

        internal static readonly string[] TransparencyKeys =
        {
            "SephiriaEnhancements.CombatVisuals.Transparency.Normal",
            "SephiriaEnhancements.CombatVisuals.Transparency.Slight",
            "SephiriaEnhancements.CombatVisuals.Transparency.Very",
            "SephiriaEnhancements.CombatVisuals.Transparency.Complete"
        };

        internal static readonly string[] OutlineScopeKeys =
        {
            "SephiriaEnhancements.CombatVisuals.Outline.Off",
            "SephiriaEnhancements.CombatVisuals.Outline.HostileOnly",
            "SephiriaEnhancements.CombatVisuals.Outline.HostileAndFriendly"
        };

        private static readonly Dictionary<string, string> English = Create(
            "Combat visual preset",
            "Follow Game preserves the official behavior. Balanced keeps companions readable while reducing their effects. Minimal hides companion effects but keeps their body faintly visible.",
            "Companion body",
            "Custom preset transparency for companions led by the local player.",
            "Companion projectiles and effects",
            "Custom preset transparency for projectiles, melee swings, and supported effects created by local companions.",
            "Combat outline scope",
            "Custom preset outline scope. The existing outline switch remains the master control.",
            "Follow Game", "Balanced (Recommended)", "Minimal", "Custom",
            "Normal", "Slightly Transparent", "Very Transparent",
            "Completely Transparent", "Off", "Hostile Only",
            "Hostile and Friendly");

        private static readonly Dictionary<string, string> SimplifiedChinese = Create(
            "战斗视觉预设",
            "“跟随游戏”保留官方行为；“均衡清晰”在保持同伴可辨识的同时降低其特效；“极简战斗”隐藏同伴特效，但仍让同伴本体保持微弱可见。",
            "同伴本体",
            "自定义本机玩家所带同伴的本体透明度。",
            "同伴弹道与特效",
            "自定义本机同伴产生的弹道、近战挥砍及已支持特效的透明度。",
            "战斗描边范围",
            "自定义描边范围；现有的敌我描边开关仍是总开关。",
            "跟随游戏", "均衡清晰（推荐）", "极简战斗", "自定义",
            "普通", "稍微透明", "非常透明", "完全透明", "关闭",
            "仅敌方", "敌方与友方");

        private static readonly Dictionary<string, string> TraditionalChinese = Create(
            "戰鬥視覺預設",
            "「跟隨遊戲」保留官方行為；「均衡清晰」在保持同伴可辨識的同時降低其特效；「極簡戰鬥」隱藏同伴特效，但仍讓同伴本體保持微弱可見。",
            "同伴本體",
            "自訂本機玩家所帶同伴的本體透明度。",
            "同伴彈道與特效",
            "自訂本機同伴產生的彈道、近戰揮砍及已支援特效的透明度。",
            "戰鬥描邊範圍",
            "自訂描邊範圍；現有的敵我描邊開關仍是總開關。",
            "跟隨遊戲", "均衡清晰（推薦）", "極簡戰鬥", "自訂",
            "普通", "稍微透明", "非常透明", "完全透明", "關閉",
            "僅敵方", "敵方與友方");

        private static readonly Dictionary<string, Dictionary<string, string>> Texts = new()
        {
            ["en-US"] = English,
            ["zh-CN"] = SimplifiedChinese,
            ["zh-TW"] = TraditionalChinese,
            ["ko-KR"] = Create(
                "전투 시각 프리셋",
                "게임 설정 따르기는 원래 동작을 유지합니다. 균형은 동료를 알아볼 수 있게 유지하면서 효과를 줄입니다. 최소는 효과를 숨기고 본체만 희미하게 표시합니다.",
                "동료 본체",
                "사용자 설정 프리셋에서 로컬 플레이어가 이끄는 동료의 본체 투명도를 조절합니다.",
                "동료 투사체 및 효과",
                "사용자 설정 프리셋에서 로컬 동료가 만든 투사체, 근접 휘두르기 및 지원되는 효과의 투명도를 조절합니다.",
                "전투 윤곽선 범위",
                "사용자 설정 프리셋의 윤곽선 범위입니다. 아군 및 적 윤곽선 설정이 전체 사용 여부를 결정합니다.",
                "게임 설정 따르기",
                "균형 (권장)",
                "최소",
                "사용자 설정",
                "보통",
                "약간 투명",
                "매우 투명",
                "완전히 투명",
                "끄기",
                "적만",
                "적과 아군"),
            ["ja-JP"] = Create(
                "戦闘表示プリセット",
                "ゲームに従うは元の動作を維持します。バランスは仲間の姿を見やすく保ちながらエフェクトを抑えます。最小はエフェクトを隠し、姿だけを薄く表示します。",
                "仲間の姿",
                "カスタム設定で、自分が率いる仲間の姿の透明度を変更します。",
                "仲間の弾とエフェクト",
                "カスタム設定で、自分の仲間が生成する弾、近接攻撃の軌跡、対応するエフェクトの透明度を変更します。",
                "戦闘の輪郭表示範囲",
                "カスタム設定の輪郭表示範囲です。「味方と敵の輪郭」が全体のオン／オフを制御します。",
                "ゲームに従う",
                "バランス（推奨）",
                "最小",
                "カスタム",
                "通常",
                "やや透明",
                "かなり透明",
                "完全に透明",
                "オフ",
                "敵のみ",
                "敵と味方"),
            ["de-DE"] = Create(
                "Kampfansicht",
                "Spielvorgabe behält das Spielverhalten bei. Ausgewogen hält Begleiter erkennbar und reduziert ihre Effekte. Minimal verbirgt Effekte, lässt die Körper aber schwach sichtbar.",
                "Begleiterkörper",
                "Transparenz der vom lokalen Spieler geführten Begleiter im benutzerdefinierten Profil.",
                "Begleitergeschosse und Effekte",
                "Transparenz von Geschossen, Nahkampfschwüngen und unterstützten Effekten lokaler Begleiter im benutzerdefinierten Profil.",
                "Umfang der Kampfumrisse",
                "Umrissumfang im benutzerdefinierten Profil. Umrisse für Freund und Feind bleibt der Hauptschalter.",
                "Spielvorgabe",
                "Ausgewogen (empfohlen)",
                "Minimal",
                "Benutzerdefiniert",
                "Normal",
                "Leicht transparent",
                "Stark transparent",
                "Vollständig transparent",
                "Aus",
                "Nur Feinde",
                "Feinde und Verbündete"),
            ["es-ES"] = Create(
                "Preajuste visual de combate",
                "Seguir el juego conserva el comportamiento original. Equilibrado mantiene visibles a los compañeros y reduce sus efectos. Mínimo oculta los efectos y deja sus cuerpos apenas visibles.",
                "Cuerpo de los compañeros",
                "Transparencia de los compañeros del jugador local en el preajuste personalizado.",
                "Proyectiles y efectos de compañeros",
                "Transparencia de proyectiles, ataques cuerpo a cuerpo y efectos compatibles de los compañeros locales en el preajuste personalizado.",
                "Alcance de los contornos",
                "Alcance de los contornos en el preajuste personalizado. Contornos de aliados y enemigos sigue siendo el interruptor principal.",
                "Seguir el juego",
                "Equilibrado (recomendado)",
                "Mínimo",
                "Personalizado",
                "Normal",
                "Algo transparente",
                "Muy transparente",
                "Totalmente transparente",
                "Desactivado",
                "Solo enemigos",
                "Enemigos y aliados"),
            ["fr-FR"] = Create(
                "Préréglage visuel du combat",
                "Suivre le jeu conserve le comportement d’origine. Équilibré garde les compagnons identifiables en réduisant leurs effets. Minimal masque les effets mais laisse les corps légèrement visibles.",
                "Corps des compagnons",
                "Transparence des compagnons dirigés par le joueur local dans le préréglage personnalisé.",
                "Projectiles et effets des compagnons",
                "Transparence des projectiles, frappes de mêlée et effets pris en charge des compagnons locaux dans le préréglage personnalisé.",
                "Portée des contours",
                "Portée des contours du préréglage personnalisé. Contours des alliés et ennemis reste l’interrupteur principal.",
                "Suivre le jeu",
                "Équilibré (recommandé)",
                "Minimal",
                "Personnalisé",
                "Normal",
                "Légèrement transparent",
                "Très transparent",
                "Entièrement transparent",
                "Désactivé",
                "Ennemis seuls",
                "Ennemis et alliés"),
            ["it-IT"] = Create(
                "Profilo visivo del combattimento",
                "Segui il gioco mantiene il comportamento originale. Bilanciato mantiene riconoscibili i compagni riducendone gli effetti. Minimo nasconde gli effetti, lasciando i corpi appena visibili.",
                "Corpo dei compagni",
                "Trasparenza dei compagni guidati dal giocatore locale nel profilo personalizzato.",
                "Proiettili ed effetti dei compagni",
                "Trasparenza di proiettili, colpi in mischia ed effetti supportati dei compagni locali nel profilo personalizzato.",
                "Ambito dei contorni",
                "Ambito dei contorni nel profilo personalizzato. Contorni di alleati e nemici resta l’interruttore principale.",
                "Segui il gioco",
                "Bilanciato (consigliato)",
                "Minimo",
                "Personalizzato",
                "Normale",
                "Leggermente trasparente",
                "Molto trasparente",
                "Completamente trasparente",
                "Disattivato",
                "Solo nemici",
                "Nemici e alleati"),
            ["pl-PL"] = Create(
                "Profil efektów walki",
                "Zgodnie z grą zachowuje oryginalne działanie. Zrównoważony ogranicza efekty, zachowując widoczność towarzyszy. Minimalny ukrywa efekty, pozostawiając ciała lekko widoczne.",
                "Ciała towarzyszy",
                "Przezroczystość towarzyszy lokalnego gracza w profilu własnym.",
                "Pociski i efekty towarzyszy",
                "Przezroczystość pocisków, zamachów wręcz i obsługiwanych efektów lokalnych towarzyszy w profilu własnym.",
                "Zakres obrysów",
                "Zakres obrysów w profilu własnym. Obrysy sojuszników i wrogów pozostają przełącznikiem głównym.",
                "Zgodnie z grą",
                "Zrównoważony (zalecany)",
                "Minimalny",
                "Własny",
                "Normalne",
                "Lekko przezroczyste",
                "Bardzo przezroczyste",
                "Całkowicie przezroczyste",
                "Wył.",
                "Tylko wrogowie",
                "Wrogowie i sojusznicy"),
            ["pt-BR"] = Create(
                "Predefinição visual de combate",
                "Seguir o jogo mantém o comportamento original. Equilibrado mantém os companheiros visíveis e reduz seus efeitos. Mínimo oculta os efeitos, deixando os corpos levemente visíveis.",
                "Corpo dos companheiros",
                "Transparência dos companheiros do jogador local na predefinição personalizada.",
                "Projéteis e efeitos dos companheiros",
                "Transparência dos projéteis, golpes corpo a corpo e efeitos compatíveis dos companheiros locais na predefinição personalizada.",
                "Alcance dos contornos",
                "Alcance dos contornos na predefinição personalizada. Contornos de aliados e inimigos continua sendo o controle principal.",
                "Seguir o jogo",
                "Equilibrado (recomendado)",
                "Mínimo",
                "Personalizado",
                "Normal",
                "Levemente transparente",
                "Muito transparente",
                "Totalmente transparente",
                "Desativado",
                "Só inimigos",
                "Inimigos e aliados"),
            ["ru-RU"] = Create(
                "Профиль отображения боя",
                "Как в игре сохраняет исходное поведение. Сбалансированный оставляет спутников различимыми, ослабляя эффекты. Минимальный скрывает эффекты, но оставляет тела слабо видимыми.",
                "Тела спутников",
                "Прозрачность спутников локального игрока в пользовательском профиле.",
                "Снаряды и эффекты спутников",
                "Прозрачность снарядов, взмахов в ближнем бою и поддерживаемых эффектов локальных спутников в пользовательском профиле.",
                "Охват контуров",
                "Охват контуров в пользовательском профиле. Контуры союзников и врагов остаётся главным переключателем.",
                "Как в игре",
                "Сбалансированный (рекомендуется)",
                "Минимальный",
                "Пользовательский",
                "Обычная",
                "Слегка прозрачные",
                "Очень прозрачные",
                "Полностью прозрачные",
                "Выкл.",
                "Только враги",
                "Враги и союзники"),
            ["sv-SE"] = Create(
                "Förval för stridsvisning",
                "Följ spelet behåller originalbeteendet. Balanserat håller följeslagare tydliga och minskar deras effekter. Minimalt döljer effekterna men lämnar kropparna svagt synliga.",
                "Följeslagarnas kroppar",
                "Transparens för den lokala spelarens följeslagare i det anpassade förvalet.",
                "Följeslagarnas projektiler och effekter",
                "Transparens för projektiler, närstridssvingar och effekter som stöds från lokala följeslagare i det anpassade förvalet.",
                "Konturernas omfattning",
                "Konturernas omfattning i det anpassade förvalet. Konturer för vän och fiende är fortfarande huvudreglaget.",
                "Följ spelet",
                "Balanserat (rekommenderat)",
                "Minimalt",
                "Anpassat",
                "Normal",
                "Lätt genomskinlig",
                "Mycket genomskinlig",
                "Helt genomskinlig",
                "Av",
                "Endast fiender",
                "Fiender och allierade"),
            ["th-TH"] = Create(
                "ชุดการแสดงผลการต่อสู้",
                "ตามเกมคงการแสดงผลเดิม สมดุลลดเอฟเฟกต์แต่ยังมองเห็นเพื่อนร่วมรบชัดเจน ขั้นต่ำซ่อนเอฟเฟกต์และแสดงตัวเพื่อนร่วมรบจาง ๆ",
                "ตัวเพื่อนร่วมรบ",
                "ความโปร่งใสของเพื่อนร่วมรบที่ผู้เล่นในเครื่องนำอยู่ เมื่อใช้ชุดกำหนดเอง",
                "กระสุนและเอฟเฟกต์เพื่อนร่วมรบ",
                "ความโปร่งใสของกระสุน การฟันระยะประชิด และเอฟเฟกต์ที่รองรับจากเพื่อนร่วมรบในเครื่อง เมื่อใช้ชุดกำหนดเอง",
                "ขอบเขตเส้นขอบ",
                "ขอบเขตเส้นขอบของชุดกำหนดเอง โดยตัวเลือกเส้นขอบฝ่ายเดียวกันและศัตรูยังเป็นสวิตช์หลัก",
                "ตามเกม",
                "สมดุล (แนะนำ)",
                "ขั้นต่ำ",
                "กำหนดเอง",
                "ปกติ",
                "โปร่งใสเล็กน้อย",
                "โปร่งใสมาก",
                "โปร่งใสทั้งหมด",
                "ปิด",
                "ศัตรูเท่านั้น",
                "ศัตรูและฝ่ายเดียวกัน"),
            ["tr-TR"] = Create(
                "Savaş görünümü ön ayarı",
                "Oyunu izle, özgün davranışı korur. Dengeli, yoldaşları seçilebilir tutarken etkilerini azaltır. Asgari, etkileri gizler ama gövdeleri hafifçe görünür bırakır.",
                "Yoldaş gövdeleri",
                "Özel ön ayarda yerel oyuncunun yönettiği yoldaşların gövde saydamlığı.",
                "Yoldaş mermileri ve etkileri",
                "Özel ön ayarda yerel yoldaşların mermileri, yakın dövüş savuruşları ve desteklenen etkilerinin saydamlığı.",
                "Savaş hatlarının kapsamı",
                "Özel ön ayarın hat kapsamı. Dost ve düşman hatları ana açma/kapama ayarı olmaya devam eder.",
                "Oyunu izle",
                "Dengeli (önerilen)",
                "Asgari",
                "Özel",
                "Normal",
                "Biraz saydam",
                "Çok saydam",
                "Tamamen saydam",
                "Kapalı",
                "Yalnızca düşmanlar",
                "Düşmanlar ve dostlar"),
        };

        internal static void Register(Action<string, string, string> addText,
            IEnumerable<string> languages)
        {
            foreach (string language in languages)
            {
                Dictionary<string, string> texts = Texts.TryGetValue(language, out var translated)
                    ? translated : English;
                foreach (KeyValuePair<string, string> text in texts)
                {
                    addText(language, text.Key, text.Value);
                }
            }
        }

        private static Dictionary<string, string> Create(string preset,
            string presetHelp, string body, string bodyHelp, string effects,
            string effectsHelp, string outline, string outlineHelp,
            string followGame, string balanced, string minimal, string custom,
            string normal, string slight, string very, string complete,
            string off, string hostileOnly, string hostileAndFriendly)
        {
            return new Dictionary<string, string>
            {
                [SettingPreset] = preset,
                [HelpPreset] = presetHelp,
                [SettingCompanionBody] = body,
                [HelpCompanionBody] = bodyHelp,
                [SettingCompanionEffects] = effects,
                [HelpCompanionEffects] = effectsHelp,
                [SettingOutlineScope] = outline,
                [HelpOutlineScope] = outlineHelp,
                [PresetKeys[0]] = followGame,
                [PresetKeys[1]] = balanced,
                [PresetKeys[2]] = minimal,
                [PresetKeys[3]] = custom,
                [TransparencyKeys[0]] = normal,
                [TransparencyKeys[1]] = slight,
                [TransparencyKeys[2]] = very,
                [TransparencyKeys[3]] = complete,
                [OutlineScopeKeys[0]] = off,
                [OutlineScopeKeys[1]] = hostileOnly,
                [OutlineScopeKeys[2]] = hostileAndFriendly
            };
        }
    }
}
