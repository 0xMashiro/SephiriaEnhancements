using System;
using System.Collections.Generic;
using SephiriaEnhancements.Configuration;

namespace SephiriaEnhancements.DefeatRetry
{
    internal static class DefeatRetryLocalization
    {
        internal const string SettingDefeatRetry =
            "SephiriaEnhancements.Setting.DefeatRetry";
        internal const string HelpDefeatRetry =
            "SephiriaEnhancements.Help.DefeatRetry";
        internal const string DefeatRetryOff =
            "SephiriaEnhancements.DefeatRetry.Off";
        internal const string DefeatRetryOn =
            "SephiriaEnhancements.DefeatRetry.On";
        internal const string RetryFloor = "SephiriaEnhancements.RetryFloor";
        internal const string RetryBossEncounter =
            "SephiriaEnhancements.RetryBossEncounter";
        internal const string RetryBossUnavailable =
            "SephiriaEnhancements.RetryBossUnavailable";

        private static readonly Dictionary<string, Dictionary<string, string>>
            DefeatRetryTexts =
                new Dictionary<string, Dictionary<string, string>>
                {
                    ["en-US"] = CreateTexts("Retry after defeat",
                        "Off by default. After a party wipe, retry the floor from entry or a supported boss from before its first phase. Items revert to the selected checkpoint. Boss retry is unavailable if the boss is already defeated, the encounter cannot be recreated, surrounding objects changed, or players changed or left the floor. In co-op, retry requires every player to be connected with a compatible Mod version.",
                        "Off", "On",
                        "Retry floor", "Retry BOSS fight", "Boss retry unavailable"),
                    ["zh-CN"] = CreateTexts("失败后重试",
                        "默认关闭。全队死亡后，可选择从入层检查点重试整层，或从第一阶段开战前重试受支持的 Boss。道具恢复至所选检查点。Boss 已被击败、无法重建、周边对象发生变化，或玩家变更、离开本层时，Boss 重试不可用。 联机重试需要所有玩家均已连接，并安装兼容版本的 Mod。",
                        "关闭", "开启",
                        "重试本层", "重试 BOSS 战", "Boss 重试不可用"),
                    ["zh-TW"] = CreateTexts("失敗後重試",
                        "預設關閉。全隊死亡後，可選擇從入層檢查點重試整層，或從第一階段開戰前重試受支援的 Boss。道具恢復至所選檢查點。Boss 已被擊敗、無法重建、周邊物件發生變化，或玩家變更、離開本層時，Boss 重試不可用。 連線重試需要所有玩家均已連線，並安裝相容版本的 Mod。",
                        "關閉", "開啟",
                        "重試本層", "重試 BOSS 戰", "Boss 重試不可用"),
                    ["ko-KR"] = CreateTexts("패배 후 재시도", "기본적으로 꺼져 있습니다. 전멸 후 층 입장 시점 또는 지원되는 보스의 첫 단계 시작 전부터 재시도합니다. 아이템은 선택한 체크포인트로 돌아갑니다. 보스를 이미 처치했거나 재생성할 수 없거나 주변 오브젝트가 바뀌거나 플레이어 구성 또는 층이 바뀌면 보스 재시도는 사용할 수 없습니다. 멀티플레이 재시도에는 모든 플레이어가 호환되는 모드 버전으로 연결되어 있어야 합니다.", "끄기", "켜기",
                        "현재 층 재시작", "보스전 재시작", "보스 재시도 불가"),
                    ["ja-JP"] = CreateTexts("敗北後に再挑戦", "初期設定はオフです。全滅後、フロア入口または対応ボスの第1段階開始前から再挑戦できます。アイテムは選択したチェックポイントに戻ります。ボスを撃破済みの場合、再生成できない場合、周囲のオブジェクトが変化した場合、参加者が変わった場合やフロアを離れた場合はボス再挑戦を利用できません。 マルチプレイでの再挑戦には、全員が対応バージョンのModを導入して接続している必要があります。", "オフ", "オン",
                        "この階をやり直す", "ボス戦をやり直す", "ボス再挑戦不可"),
                    ["de-DE"] = CreateTexts("Nach Niederlage erneut versuchen",
                    "Standardmäßig aus. Nach einem Gruppen-Tod die Ebene ab Eingang oder einen unterstützten Boss vor Phase eins neu starten. Gegenstände werden auf den gewählten Kontrollpunkt zurückgesetzt. Boss-Neustart ist bei bereits besiegtem oder nicht rekonstruierbarem Boss, veränderten Umgebungsobjekten, Spielerwechsel oder Verlassen der Ebene nicht verfügbar. Im Koop müssen alle Spieler mit einer kompatiblen Mod-Version verbunden sein.",
                    "Aus",
                    "Ein",
                        "Ebene wiederholen", "Bosskampf wiederholen", "Boss-Neustart nicht verfügbar"),
                    ["es-ES"] = CreateTexts("Reintentar tras la derrota",
                    "Desactivado por defecto. Tras morir todo el grupo, reinicia la planta desde la entrada o un jefe compatible desde antes de su primera fase. Los objetos vuelven al punto elegido. El jefe no se puede reintentar si ya fue derrotado, no puede recrearse, cambia el entorno, cambian los jugadores o salen de la planta. En cooperativo, todos deben estar conectados con una versión compatible del mod para reintentar.",
                    "Desactivado",
                    "Activado",
                        "Reintentar piso", "Reintentar jefe", "Reintento de jefe no disponible"),
                    ["fr-FR"] = CreateTexts("Réessayer après une défaite",
                    "Désactivé par défaut. Après la mort du groupe, reprenez à l’entrée de l’étage ou avant la première phase d’un boss compatible. Les objets reviennent au point choisi. La reprise du boss est indisponible si le boss est déjà vaincu, si sa recréation échoue, si les objets environnants changent ou si des joueurs changent ou quittent l’étage. En coopération, tous les joueurs doivent être connectés avec une version compatible du mod pour réessayer.",
                    "Désactivé",
                    "Activé",
                        "Recommencer l'étage", "Recommencer le boss", "Reprise du boss indisponible"),
                    ["it-IT"] = CreateTexts("Riprova dopo la sconfitta",
                    "Disattivato per impostazione predefinita. Dopo la sconfitta del gruppo, riparti dall’ingresso del piano o da prima della prima fase di un boss supportato. Gli oggetti tornano al checkpoint scelto. Il boss non è ripetibile se è già stato sconfitto, non può essere ricreato, cambiano gli oggetti circostanti, cambiano i giocatori o lasciano il piano. In cooperativa, tutti devono essere connessi con una versione compatibile della mod per riprovare.",
                    "Disattivato",
                    "Attivato",
                        "Riprova piano", "Riprova il boss", "Riprova boss non disponibile"),
                    ["pl-PL"] = CreateTexts("Ponów po porażce",
                    "Domyślnie wyłączone. Po śmierci drużyny ponów piętro od wejścia lub obsługiwanego bossa sprzed pierwszej fazy. Przedmioty wracają do wybranego punktu. Powtórka bossa jest niedostępna, gdy boss został już pokonany, nie można go odtworzyć, zmieniły się obiekty otoczenia, skład graczy lub gracze opuścili piętro. W kooperacji wszyscy gracze muszą być połączeni i mieć zgodną wersję moda, aby ponowić próbę.",
                    "Wył.",
                    "Wł.",
                        "Powtórz piętro", "Powtórz walkę z bossem", "Ponowienie bossa niedostępne"),
                    ["pt-BR"] = CreateTexts("Tentar novamente após derrota",
                    "Desativado por padrão. Após a derrota da equipe, reinicie o andar pela entrada ou um chefe compatível antes da primeira fase. Os itens voltam ao ponto escolhido. Repetir o chefe fica indisponível se ele já foi derrotado, não puder ser recriado, se objetos ao redor mudarem, se os jogadores mudarem ou saírem do andar. No cooperativo, todos devem estar conectados com uma versão compatível do mod para tentar novamente.",
                    "Desativado",
                    "Ativado",
                        "Repetir andar", "Repetir chefe", "Repetir chefe indisponível"),
                    ["ru-RU"] = CreateTexts("Повтор после поражения",
                    "По умолчанию отключено. После гибели группы повторите этаж со входа или поддерживаемого босса до начала первой фазы. Предметы возвращаются к выбранной точке. Повтор босса недоступен, если босс уже побеждён, его нельзя воссоздать, изменились окружающие объекты, состав игроков или игроки покинули этаж. Для повтора в кооперативе все игроки должны быть подключены и использовать совместимую версию мода.",
                    "Выкл.",
                    "Вкл.",
                        "Переиграть этаж", "Повторить бой с боссом", "Повтор босса недоступен"),
                    ["sv-SE"] = CreateTexts("Försök igen efter nederlag",
                    "Av som standard. Efter gruppens nederlag kan våningen startas om från ingången eller en boss som stöds från före första fasen. Föremål återställs till vald kontrollpunkt. Bossförsök saknas om bossen redan besegrats, inte kan återskapas, omgivningen ändrats eller spelare bytts ut eller lämnat våningen. I samarbete måste alla spelare vara anslutna med en kompatibel modversion för att försöka igen.",
                    "Av",
                    "På",
                        "Försök våningen igen", "Försök bossen igen", "Bossförsök inte tillgängligt"),
                    ["th-TH"] = CreateTexts("ลองใหม่หลังพ่ายแพ้",
                    "ปิดไว้ตามค่าเริ่มต้น เมื่อทั้งทีมตาย ให้เริ่มชั้นใหม่จากทางเข้าหรือเริ่มบอสที่รองรับใหม่ก่อนเฟสแรก ไอเทมจะกลับสู่จุดบันทึกที่เลือก ไม่สามารถสู้บอสใหม่ได้หากบอสถูกกำจัดแล้ว สร้างบอสใหม่ไม่ได้ วัตถุรอบข้างเปลี่ยนไป ผู้เล่นเปลี่ยน หรือออกจากชั้น ในโหมดร่วมมือ ผู้เล่นทุกคนต้องเชื่อมต่อและใช้ม็อดเวอร์ชันที่รองรับจึงจะลองใหม่ได้",
                    "ปิด",
                    "เปิด",
                        "ลองชั้นนี้ใหม่", "สู้บอสใหม่", "ไม่สามารถสู้บอสใหม่ได้"),
                    ["tr-TR"] = CreateTexts("Yenilgiden sonra yeniden dene",
                    "Varsayılan olarak kapalıdır. Grup yenilince kat girişinden veya desteklenen bossun ilk aşamasından önce yeniden başlatır. Eşyalar seçilen kontrol noktasına döner. Boss zaten yenildiyse, yeniden oluşturulamıyorsa, çevredeki nesneler değişmişse veya oyuncular değişmiş ya da kattan ayrılmışsa boss tekrarı kullanılamaz. Eşli oyunda tekrar denemek için tüm oyuncuların uyumlu bir mod sürümüyle bağlı olması gerekir.",
                    "Kapalı",
                    "Açık",
                        "Katı yeniden dene", "Boss savaşını yeniden dene", "Boss tekrarı kullanılamıyor")
                };

        private static Dictionary<string, string> CreateTexts(string label, string help,
            string off, string on, string retryFloor, string retryBossEncounter,
            string retryBossUnavailable)
        {
            return new Dictionary<string, string>
            {
                [SettingDefeatRetry] = label,
                [HelpDefeatRetry] = help,
                [DefeatRetryOff] = off,
                [DefeatRetryOn] = on,
                [RetryFloor] = retryFloor,
                [RetryBossEncounter] = retryBossEncounter,
                [RetryBossUnavailable] = retryBossUnavailable
            };
        }

        internal static void Register(Action<string, string, string> addText, IEnumerable<string> languages)
        {
            LocalizationGroup.Register(addText, languages, DefeatRetryTexts);
        }
    }
}
