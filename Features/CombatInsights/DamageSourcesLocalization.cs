using System;
using System.Collections.Generic;
using SephiriaEnhancements.Configuration;

namespace SephiriaEnhancements.Combat
{
    internal static class DamageSourcesLocalization
    {
        private const string Prefix = "SephiriaEnhancements.DamageSources.";
        internal const string Sources = Prefix + "Sources", Player = Prefix + "Player", Training = Prefix + "Training",
            ClearMine = Prefix + "ClearMine", Empty = Prefix + "Empty", NativeScope = Prefix + "NativeScope",
            TrainingHelp = Prefix + "TrainingHelp", PlayerUnavailable = Prefix + "PlayerUnavailable",
            SameArea = Prefix + "SameArea", HostRequired = Prefix + "HostRequired", Waiting = Prefix + "Waiting", TooLarge = Prefix + "TooLarge";
        private static readonly string[] Keys = { Sources, Player, Training, ClearMine, Empty, NativeScope,
            TrainingHelp, PlayerUnavailable, SameArea, HostRequired, Waiting, TooLarge };
        private static readonly Dictionary<string, string[]> Texts = new Dictionary<string, string[]>
        {
            ["en-US"] = new[] { "Damage sources", "Player: {0}", "Training damage", "Clear my training damage",
                "No recorded damage in this range.", "Uses the selected player's in-game damage statistics.",
                "Training dummies only. Resets when cleared, travelling or reloading; excluded from exploration statistics.",
                "This player's statistics are not available right now.", "Join this player's area to view training damage.",
                "Training statistics require a supporting version of the Mod on the host.", "Waiting for the host's training statistics…",
                "There are too many damage sources to transfer in this connection." },
            ["zh-CN"] = new[] { "伤害来源", "玩家：{0}", "训练伤害", "清空我的训练伤害",
                "此范围内暂无伤害记录。", "显示所选玩家的游戏内伤害统计。",
                "只统计训练假人；清空、旅行或重新加载时重置，不计入探索统计。", "暂时无法查看此玩家的统计。",
                "请进入此玩家所在区域查看训练伤害。", "房主需要安装支持训练统计的 Mod 版本。", "正在等待房主的训练统计……",
                "伤害来源过多，当前连接无法传输完整统计。" },
            ["zh-TW"] = new[] { "傷害來源", "玩家：{0}", "訓練傷害", "清空我的訓練傷害",
                "此範圍內尚無傷害紀錄。", "顯示所選玩家的遊戲內傷害統計。",
                "只統計訓練假人；清空、旅行或重新載入時重置，不計入探索統計。", "暫時無法查看此玩家的統計。",
                "請進入此玩家所在區域查看訓練傷害。", "房主需要安裝支援訓練統計的 Mod 版本。", "正在等待房主的訓練統計……",
                "傷害來源過多，目前連線無法傳輸完整統計。" },
            ["ko-KR"] = new[] { "피해 출처", "플레이어: {0}", "훈련 피해", "내 훈련 피해 초기화",
                "이 범위에 기록된 피해가 없습니다.", "선택한 플레이어의 게임 내 피해 통계를 표시합니다.",
                "훈련 허수아비만 집계합니다. 초기화, 이동 또는 다시 불러오기 시 지워지며 탐험 통계에는 포함하지 않습니다.", "지금은 이 플레이어의 통계를 볼 수 없습니다.",
                "훈련 피해를 보려면 해당 플레이어의 지역으로 이동하세요.", "호스트에게 훈련 통계를 지원하는 Mod 버전이 필요합니다.", "호스트의 훈련 통계를 기다리는 중…",
                "피해 출처가 너무 많아 현재 연결로 전체 통계를 전송할 수 없습니다." },
            ["ja-JP"] = new[] { "ダメージの発生源", "プレイヤー：{0}", "訓練ダメージ", "自分の訓練ダメージを消去",
                "この範囲のダメージ記録はありません。", "選択したプレイヤーのゲーム内ダメージ統計を表示します。",
                "訓練用の人形のみを集計します。消去、移動、再読み込み時にリセットし、探索の統計には含めません。", "現在、このプレイヤーの統計は確認できません。",
                "訓練ダメージを見るには、そのプレイヤーのエリアへ移動してください。", "ホストに訓練統計対応のModが必要です。", "ホストの訓練統計を待っています…",
                "ダメージの発生源が多すぎるため、現在の接続では全統計を転送できません。" },
            ["de-DE"] = new[] { "Schadensquellen", "Spieler: {0}", "Trainingsschaden", "Meinen Trainingsschaden löschen",
                "In diesem Bereich wurde kein Schaden aufgezeichnet.", "Zeigt die Spielstatistik des gewählten Spielers.",
                "Nur Trainingspuppen. Wird beim Löschen, Reisen oder Neuladen zurückgesetzt und zählt nicht zur Erkundungsstatistik.", "Die Statistik dieses Spielers ist derzeit nicht verfügbar.",
                "Betritt das Gebiet dieses Spielers, um den Trainingsschaden zu sehen.", "Der Host benötigt eine Mod-Version mit Trainingsstatistik.", "Warte auf die Trainingsstatistik des Hosts…",
                "Zu viele Schadensquellen für eine vollständige Übertragung über diese Verbindung." },
            ["es-ES"] = new[] { "Fuentes de daño", "Jugador: {0}", "Daño de entrenamiento", "Borrar mi daño de entrenamiento",
                "No hay daño registrado en este ámbito.", "Muestra las estadísticas del juego del jugador seleccionado.",
                "Solo muñecos de entrenamiento. Se reinicia al borrar, viajar o recargar; no cuenta para la exploración.", "Las estadísticas de este jugador no están disponibles ahora.",
                "Ve a la zona de este jugador para ver su daño de entrenamiento.", "El anfitrión necesita una versión del Mod compatible con las estadísticas de entrenamiento.", "Esperando las estadísticas de entrenamiento del anfitrión…",
                "Hay demasiadas fuentes de daño para transferirlas por esta conexión." },
            ["fr-FR"] = new[] { "Sources de dégâts", "Joueur : {0}", "Dégâts d’entraînement", "Effacer mes dégâts d’entraînement",
                "Aucun dégât enregistré pour cette sélection.", "Affiche les statistiques du jeu pour le joueur sélectionné.",
                "Mannequins uniquement. Réinitialisé à l’effacement, au voyage ou au rechargement ; exclu des statistiques d’exploration.", "Les statistiques de ce joueur sont indisponibles pour le moment.",
                "Rejoignez la zone de ce joueur pour voir ses dégâts d’entraînement.", "L’hôte doit avoir une version du Mod prenant en charge les statistiques d’entraînement.", "En attente des statistiques d’entraînement de l’hôte…",
                "Trop de sources de dégâts pour un transfert complet sur cette connexion." },
            ["it-IT"] = new[] { "Fonti di danno", "Giocatore: {0}", "Danni di allenamento", "Cancella i miei danni di allenamento",
                "Nessun danno registrato in questo ambito.", "Mostra le statistiche di gioco del giocatore selezionato.",
                "Solo manichini. Si azzerano quando cancellati, durante un viaggio o al ricaricamento; esclusi dall’esplorazione.", "Le statistiche di questo giocatore non sono disponibili al momento.",
                "Raggiungi l’area del giocatore per vedere i danni di allenamento.", "L’host deve avere una versione del Mod che supporti le statistiche di allenamento.", "In attesa delle statistiche di allenamento dell’host…",
                "Troppe fonti di danno per trasferire tutte le statistiche con questa connessione." },
            ["pl-PL"] = new[] { "Źródła obrażeń", "Gracz: {0}", "Obrażenia treningowe", "Wyczyść moje obrażenia treningowe",
                "Brak zapisanych obrażeń w tym zakresie.", "Pokazuje statystyki gry wybranego gracza.",
                "Tylko manekiny treningowe. Reset przy czyszczeniu, podróży lub ponownym wczytaniu; bez wpływu na statystyki eksploracji.", "Statystyki tego gracza są teraz niedostępne.",
                "Przejdź do obszaru tego gracza, aby zobaczyć obrażenia treningowe.", "Host potrzebuje wersji moda obsługującej statystyki treningowe.", "Oczekiwanie na statystyki treningowe hosta…",
                "Zbyt wiele źródeł obrażeń, aby przesłać pełne statystyki przez to połączenie." },
            ["pt-BR"] = new[] { "Fontes de dano", "Jogador: {0}", "Dano de treino", "Limpar meu dano de treino",
                "Nenhum dano registrado neste intervalo.", "Exibe as estatísticas do jogo do jogador selecionado.",
                "Apenas bonecos de treino. Reinicia ao limpar, viajar ou recarregar; não entra nas estatísticas de exploração.", "As estatísticas deste jogador estão indisponíveis no momento.",
                "Entre na área deste jogador para ver o dano de treino.", "O anfitrião precisa de uma versão do Mod com suporte a estatísticas de treino.", "Aguardando as estatísticas de treino do anfitrião…",
                "Há fontes de dano demais para transferir as estatísticas completas nesta conexão." },
            ["ru-RU"] = new[] { "Источники урона", "Игрок: {0}", "Тренировочный урон", "Сбросить мой тренировочный урон",
                "В этом диапазоне нет записей об уроне.", "Показана игровая статистика выбранного игрока.",
                "Только тренировочные манекены. Сброс при очистке, перемещении или перезагрузке; не входит в статистику исследования.", "Статистика этого игрока сейчас недоступна.",
                "Перейдите в область этого игрока, чтобы увидеть тренировочный урон.", "Хосту нужна версия мода с поддержкой тренировочной статистики.", "Ожидание тренировочной статистики от хоста…",
                "Слишком много источников урона для передачи полной статистики через это соединение." },
            ["sv-SE"] = new[] { "Skadekällor", "Spelare: {0}", "Träningsskada", "Rensa min träningsskada",
                "Ingen skada har registrerats inom detta urval.", "Visar den valda spelarens skadestatistik från spelet.",
                "Endast träningsdockor. Återställs vid rensning, resa eller omladdning; räknas inte i utforskningen.", "Den här spelarens statistik är inte tillgänglig just nu.",
                "Gå till spelarens område för att se träningsskadan.", "Värden behöver en Mod-version med stöd för träningsstatistik.", "Väntar på värdens träningsstatistik…",
                "För många skadekällor för att överföra fullständig statistik via den här anslutningen." },
            ["th-TH"] = new[] { "แหล่งความเสียหาย", "ผู้เล่น: {0}", "ความเสียหายฝึกซ้อม", "ล้างความเสียหายฝึกซ้อมของฉัน",
                "ยังไม่มีบันทึกความเสียหายในขอบเขตนี้", "แสดงสถิติความเสียหายในเกมของผู้เล่นที่เลือก",
                "นับเฉพาะหุ่นฝึกซ้อม รีเซ็ตเมื่อล้าง เดินทาง หรือโหลดใหม่ และไม่นับรวมในสถิติการสำรวจ", "ยังไม่สามารถดูสถิติของผู้เล่นนี้ได้",
                "ไปยังพื้นที่ของผู้เล่นนี้เพื่อดูความเสียหายฝึกซ้อม", "โฮสต์ต้องติดตั้ง Mod เวอร์ชันที่รองรับสถิติฝึกซ้อม", "กำลังรอสถิติฝึกซ้อมจากโฮสต์…",
                "มีแหล่งความเสียหายมากเกินไปที่จะส่งสถิติทั้งหมดผ่านการเชื่อมต่อนี้" },
            ["tr-TR"] = new[] { "Hasar kaynakları", "Oyuncu: {0}", "Antrenman hasarı", "Antrenman hasarımı temizle",
                "Bu kapsamda kayıtlı hasar yok.", "Seçilen oyuncunun oyun içi hasar istatistiklerini gösterir.",
                "Yalnızca antrenman kuklaları. Temizleme, yolculuk veya yeniden yüklemede sıfırlanır; keşif istatistiklerine katılmaz.", "Bu oyuncunun istatistikleri şu anda kullanılamıyor.",
                "Antrenman hasarını görmek için bu oyuncunun bölgesine gidin.", "Ev sahibinde antrenman istatistiklerini destekleyen bir Mod sürümü olmalıdır.", "Ev sahibinin antrenman istatistikleri bekleniyor…",
                "Bu bağlantı üzerinden tüm istatistikleri aktarmak için çok fazla hasar kaynağı var." }
        };

        internal static void Register(Action<string, string, string> addText, IEnumerable<string> languages) =>
            LocalizationGroup.Register(addText, languages, Keys, Texts);
    }
}
