using System;
using System.Collections.Generic;
using SephiriaEnhancements.Configuration;

namespace SephiriaEnhancements.AutoCasting
{
    internal static class AutoCastingLocalization
    {
        internal const string Title = "SephiriaEnhancements.AutoCasting.Title";
        internal const string On = "SephiriaEnhancements.AutoCasting.On";
        internal const string Off = "SephiriaEnhancements.AutoCasting.Off";
        internal const string Hint = "SephiriaEnhancements.AutoCasting.Hint";
        internal const string Help = "SephiriaEnhancements.AutoCasting.Help";
        internal const string Unavailable = "SephiriaEnhancements.AutoCasting.Unavailable";
        private static readonly string[] Keys = { Title, On, Off, Hint, Help, Unavailable };
        private static readonly Dictionary<string, string[]> Texts = new Dictionary<string, string[]>
        {
            ["en-US"] = new[] { "Auto casting", "On", "Off", "{0}: toggle auto casting", "Automatically casts this magic while exploring, even without enemies. Uses normal mana and charges, and your current aim. Pauses in menus. Follows this artifact when rebound; clears when it is lost, the character is replaced, or the world is reloaded.", "No supported magic" },
            ["zh-CN"] = new[] { "自动施法", "开启", "关闭", "{0}：切换自动施法", "探索中自动释放此魔法，没有敌人时也会释放。正常消耗魔力与次数，沿用当前瞄准。菜单中暂停。更换快捷键时跟随这件神器；失去神器、更换角色或重新加载世界后清除。", "无可用魔法" },
            ["zh-TW"] = new[] { "自動施法", "開啟", "關閉", "{0}：切換自動施法", "探索中自動施放此魔法，沒有敵人時也會施放。正常消耗魔力與次數，沿用目前瞄準。選單中暫停。更換快捷鍵時跟隨這件神器；失去神器、更換角色或重新載入世界後清除。", "無可用魔法" },
            ["ko-KR"] = new[] { "자동 시전", "켜짐", "꺼짐", "{0}: 자동 시전 전환", "탐험 중 적이 없어도 이 마법을 자동으로 시전합니다. 현재 조준을 사용하며 마나와 횟수를 정상적으로 소모합니다. 메뉴에서는 멈춥니다. 단축키를 바꿔도 이 유물을 따르며, 유물을 잃거나 캐릭터를 바꾸거나 세계를 다시 불러오면 해제됩니다.", "사용 가능한 마법 없음" },
            ["ja-JP"] = new[] { "自動詠唱", "オン", "オフ", "{0}：自動詠唱を切り替え", "探索中、敵がいなくてもこの魔法を自動で使います。現在の照準を使い、魔力と回数を通常どおり消費します。メニュー中は停止します。キーを変更してもこのアーティファクトに設定が残り、失った時・キャラクター変更時・世界の再読み込み時に解除されます。", "対応する魔法なし" },
            ["de-DE"] = new[] { "Automatisches Zaubern", "An", "Aus", "{0}: automatisches Zaubern umschalten", "Wirkt diesen Zauber beim Erkunden auch ohne Gegner. Verbraucht normal Mana und Ladungen und nutzt das aktuelle Ziel. Pausiert in Menüs. Bleibt beim Umbelegen am Artefakt; wird bei Verlust, Charakterwechsel oder erneutem Laden der Welt gelöscht.", "Kein geeigneter Zauber" },
            ["es-ES"] = new[] { "Lanzamiento automático", "Activado", "Desactivado", "{0}: alternar lanzamiento automático", "Lanza esta magia al explorar, incluso sin enemigos. Consume maná y cargas normales y usa la puntería actual. Se pausa en los menús. Sigue al artefacto al reasignarlo; se borra al perderlo, cambiar de personaje o recargar el mundo.", "Sin magia compatible" },
            ["fr-FR"] = new[] { "Lancement automatique", "Activé", "Désactivé", "{0} : basculer le lancement automatique", "Lance cette magie en exploration, même sans ennemis. Consomme normalement mana et charges et utilise la visée actuelle. Pause dans les menus. Suit cet artefact après réattribution ; effacé en cas de perte, de changement de personnage ou de rechargement du monde.", "Aucune magie compatible" },
            ["it-IT"] = new[] { "Lancio automatico", "Attivo", "Disattivo", "{0}: alterna il lancio automatico", "Lancia questa magia durante l'esplorazione, anche senza nemici. Consuma normalmente mana e cariche e usa la mira attuale. Si ferma nei menu. Segue l'artefatto se riassegnato; si azzera alla perdita, al cambio di personaggio o al ricaricamento del mondo.", "Nessuna magia compatibile" },
            ["pl-PL"] = new[] { "Automatyczne rzucanie", "Włączone", "Wyłączone", "{0}: przełącz automatyczne rzucanie", "Rzuca tę magię podczas eksploracji, nawet bez wrogów. Zużywa normalnie manę i ładunki oraz używa bieżącego celowania. Pauzuje w menu. Podąża za artefaktem po zmianie przypisania; znika po jego utracie, zmianie postaci lub ponownym wczytaniu świata.", "Brak obsługiwanej magii" },
            ["pt-BR"] = new[] { "Conjuração automática", "Ativada", "Desativada", "{0}: alternar conjuração automática", "Conjura esta magia ao explorar, mesmo sem inimigos. Consome mana e cargas normalmente e usa a mira atual. Pausa nos menus. Acompanha o artefato ao remapear; é apagada ao perdê-lo, trocar de personagem ou recarregar o mundo.", "Nenhuma magia compatível" },
            ["ru-RU"] = new[] { "Автоприменение магии", "Включено", "Выключено", "{0}: переключить автоприменение", "Применяет эту магию при исследовании даже без врагов. Обычно расходует ману и заряды, использует текущее прицеливание. В меню приостанавливается. Следует за артефактом при смене привязки; сбрасывается при его потере, смене персонажа или перезагрузке мира.", "Нет подходящей магии" },
            ["sv-SE"] = new[] { "Automatisk magi", "På", "Av", "{0}: växla automatisk magi", "Använder denna magi under utforskning, även utan fiender. Förbrukar mana och laddningar normalt och använder nuvarande sikte. Pausar i menyer. Följer artefakten vid ombindning; rensas när den förloras, karaktären byts eller världen laddas om.", "Ingen kompatibel magi" },
            ["th-TH"] = new[] { "ร่ายเวทอัตโนมัติ", "เปิด", "ปิด", "{0}: สลับการร่ายเวทอัตโนมัติ", "ร่ายเวทนี้อัตโนมัติระหว่างสำรวจแม้ไม่มีศัตรู ใช้มานาและจำนวนครั้งตามปกติและเล็งตามทิศปัจจุบัน หยุดในเมนู การตั้งค่าจะตามสิ่งประดิษฐ์นี้เมื่อเปลี่ยนปุ่ม และจะล้างเมื่อเสียสิ่งประดิษฐ์ เปลี่ยนตัวละคร หรือโหลดโลกใหม่", "ไม่มีเวทที่รองรับ" },
            ["tr-TR"] = new[] { "Otomatik büyü", "Açık", "Kapalı", "{0}: otomatik büyüyü değiştir", "Keşif sırasında düşman olmasa da bu büyüyü kullanır. Normal mana ve kullanım hakkı harcar, mevcut nişanı kullanır. Menülerde durur. Tuş değişince eseri takip eder; eser kaybolduğunda, karakter değiştiğinde veya dünya yeniden yüklendiğinde temizlenir.", "Uygun büyü yok" }
        };
        internal static void Register(Action<string, string, string> addText, IEnumerable<string> languages) =>
            LocalizationGroup.Register(addText, languages, Keys, Texts);
    }
}
