using System;
using System.Collections.Generic;
using SephiriaEnhancements.Configuration;

namespace SephiriaEnhancements.ModInformation
{
    internal static class ModInformationLocalization
    {
        private const string Prefix = "SephiriaEnhancements.ModInformation.";
        internal const string Version = Prefix + "Version";
        internal const string VersionHelp = Prefix + "VersionHelp";
        internal const string WelcomeSetting = Prefix + "WelcomeSetting";
        internal const string WelcomeHelp = Prefix + "WelcomeHelp";
        internal const string AutomaticSetting = Prefix + "AutomaticSetting";
        internal const string AutomaticHelp = Prefix + "AutomaticHelp";
        internal const string Check = Prefix + "Check";
        internal const string CheckHelp = Prefix + "CheckHelp";
        internal const string Nexus = Prefix + "Nexus";
        internal const string GitHub = Prefix + "GitHub";
        internal const string LinkHelp = Prefix + "LinkHelp";
        internal const string On = Prefix + "On";
        internal const string Off = Prefix + "Off";
        internal const string Open = Prefix + "Open";
        internal const string NotChecked = Prefix + "NotChecked";
        internal const string Checking = Prefix + "Checking";
        internal const string Available = Prefix + "Available";
        internal const string UpToDate = Prefix + "UpToDate";
        internal const string Failed = Prefix + "Failed";
        internal const string NoPublishedVersion = Prefix + "NoPublishedVersion";
        internal const string Welcome = Prefix + "Welcome";
        internal const string Update = Prefix + "Update";
        internal const string GameVersion = Prefix + "GameVersion";
        internal const string GameVersionHelp = Prefix + "GameVersionHelp";
        internal const string LastChecked = Prefix + "LastChecked";
        internal const string LastCheckedHelp = Prefix + "LastCheckedHelp";
        internal const string ConnectionFailed = Prefix + "ConnectionFailed";
        internal const string TimedOut = Prefix + "TimedOut";
        internal const string RateLimited = Prefix + "RateLimited";
        internal const string ServiceError = Prefix + "ServiceError";
        internal const string InvalidResponse = Prefix + "InvalidResponse";
        internal const string LogFolder = Prefix + "LogFolder";
        internal const string LogFolderHelp = Prefix + "LogFolderHelp";
        internal const string ReportIssue = Prefix + "ReportIssue";
        internal const string ReportIssueHelp = Prefix + "ReportIssueHelp";
        internal const string OpenFolder = Prefix + "OpenFolder";
        internal const string CopyPath = Prefix + "CopyPath";
        internal const string CopyLink = Prefix + "CopyLink";
        internal const string Copied = Prefix + "Copied";
        internal const string CopyFailed = Prefix + "CopyFailed";
        internal const string OpenFailed = Prefix + "OpenFailed";
        internal const string LogOpenFailedHelp = Prefix + "LogOpenFailedHelp";
        internal const string ReportOpenFailedHelp = Prefix + "ReportOpenFailedHelp";
        internal const string CopyReport = Prefix + "CopyReport";
        internal const string CopyReportHelp = Prefix + "CopyReportHelp";
        internal const string Copy = Prefix + "Copy";
        internal const string ReportDetails = Prefix + "ReportDetails";
        private static readonly string[] Keys =
        {
            Version, VersionHelp, WelcomeSetting, WelcomeHelp, AutomaticSetting, AutomaticHelp,
            Check, CheckHelp, Nexus, GitHub, LinkHelp, On, Off, Open, NotChecked, Checking,
            Available, UpToDate, Failed, NoPublishedVersion, Welcome, Update,
            GameVersion, GameVersionHelp, LastChecked, LastCheckedHelp, ConnectionFailed, TimedOut, RateLimited, ServiceError, InvalidResponse,
            LogFolder, LogFolderHelp, ReportIssue, ReportIssueHelp, OpenFolder, CopyPath, CopyLink, Copied, CopyFailed, OpenFailed, LogOpenFailedHelp, ReportOpenFailedHelp,
            CopyReport, CopyReportHelp, Copy, ReportDetails
        };

        private static readonly Dictionary<string, string[]> Texts = new Dictionary<string, string[]>
        {
            ["en-US"] = new[] {
                "Mod version", "Installed version of Sephiria Enhancements. Official download pages are listed below.",
                "Show welcome message", "Show the version and official links on first entering a game after launch. Only visible to you while the Mod is enabled.",
                "Check for updates automatically", "Check GitHub once after entering a game while the Mod is enabled. Runs in the background; never installs updates.",
                "Check for updates", "Check GitHub now. Stable versions check stable releases; test versions also check newer test releases. Newer does not guarantee compatibility with your game version.",
                "Open Nexus Mods", "Open GitHub releases", "Open the official download page in your browser.",
                "Enabled", "Disabled", "Open page", "Not checked", "Checking…", "Available: {0}", "No newer version found", "Check failed; try again", "No downloadable release found",
                "{0}\nVersion {1} loaded.\nSephiria {2}", "Sephiria Enhancements {0} is available (installed: {1}). Official downloads:\nNexus Mods: {2}\nGitHub: {3}",
                "Game version", "Current Sephiria version. Include game and Mod versions in problem reports.", "Last check", "Local time of the last completed check since launch, including failures. Cancelled checks are excluded.", "Cannot connect; check your network", "Request timed out; try again", "GitHub request limit; try later", "GitHub request failed; try later", "Invalid update data; try later",
                "Open Mod log folder", "Open the folder containing support*.log. Attach these files to your report. Copy them before restarting the game.", "Report on GitHub", "Open the GitHub report form with game and Mod versions filled in. A GitHub account is required. Describe what happened and attach logs if available.", "Open folder", "Copy path", "Copy link", "Copied", "Copy failed", "Could not open", "Could not open the log folder. Activate again to copy its path. You can still report the problem without logs.", "Could not open the browser. Activate again to copy the report link.",
                "Copy report details", "Copy game and Mod versions and a short report outline. Add what happened, then paste it on Nexus Mods or where you already contact the author. Copying sends nothing.", "Copy", "Sephiria: {0}\nSephiria Enhancements: {1} ({2})\n\nWhat happened:\nWhat I did:\nSolo / co-op host / co-op client:" },
            ["zh-CN"] = new[] {
                "Mod 版本", "当前安装的 Sephiria 增强版本。下方提供官方下载入口。",
                "显示欢迎信息", "每次启动后首次进入游戏时显示版本和官方地址。仅自己可见，Mod 关闭时不显示。",
                "自动检查更新", "Mod 启用时，进入游戏后向 GitHub 检查一次更新。在后台进行，不会自动安装。",
                "检查更新", "立即向 GitHub 检查更新。正式版只检查正式更新；测试版也检查更新的测试版。新版本不代表一定兼容当前游戏版本。",
                "打开 Nexus Mods", "打开 GitHub 发布页", "在浏览器中打开官方下载页面。",
                "开启", "关闭", "打开页面", "尚未检查", "正在检查…", "有新版本：{0}", "未发现更新版本", "检查失败，请重试", "未找到可下载版本",
                "{0}\n已加载版本 {1}。\nSephiria {2}", "Sephiria 增强 {0} 已发布（当前安装：{1}）。官方下载：\nNexus Mods：{2}\nGitHub：{3}",
                "游戏版本", "当前 Sephiria 版本。反馈问题时请提供游戏和 Mod 版本。", "上次检查", "本次启动后最近一次完成检查的本机时间，包含失败的检查，不包含已取消的检查。", "无法连接，请检查网络", "请求超时，请重试", "GitHub 请求受限，请稍后重试", "GitHub 请求失败，请稍后重试", "更新数据异常，请稍后重试",
                "打开 Mod 日志文件夹", "打开存放 support*.log 的文件夹，将这些文件附到问题反馈中即可。重启游戏前请先复制日志。", "在 GitHub 反馈", "打开 GitHub 反馈表单，自动填入游戏和 Mod 版本。需要 GitHub 账号。描述遇到的情况，有日志时请一并附上。", "打开文件夹", "复制路径", "复制链接", "已复制", "复制失败", "无法打开", "无法打开日志文件夹。再次点击或确认可复制路径。没有日志也可以反馈。", "无法打开浏览器。再次点击或确认可复制反馈链接。",
                "复制反馈信息", "复制游戏和 Mod 版本及简短的反馈提纲。补充遇到的问题后，可粘贴到 Nexus Mods 或原来联系作者的地方。复制不会发送任何内容。", "复制", "Sephiria：{0}\nSephiria Enhancements：{1}（{2}）\n\n遇到的问题：\n具体操作：\n单人／联机房主／联机客户端：" },
            ["zh-TW"] = new[] {
                "Mod 版本", "目前安裝的 Sephiria 增強版本。下方提供官方下載入口。",
                "顯示歡迎訊息", "每次啟動後首次進入遊戲時顯示版本和官方網址。僅自己可見，Mod 關閉時不顯示。",
                "自動檢查更新", "Mod 啟用時，進入遊戲後向 GitHub 檢查一次更新。在背景執行，不會自動安裝。",
                "檢查更新", "立即向 GitHub 檢查更新。正式版只檢查正式更新；測試版也檢查較新的測試版。新版本不代表一定相容目前遊戲版本。",
                "開啟 Nexus Mods", "開啟 GitHub 發布頁", "在瀏覽器中開啟官方下載頁面。",
                "開啟", "關閉", "開啟頁面", "尚未檢查", "正在檢查…", "有新版本：{0}", "未發現更新版本", "檢查失敗，請重試", "未找到可下載版本",
                "{0}\n已載入版本 {1}。\nSephiria {2}", "Sephiria 增強 {0} 已發布（目前安裝：{1}）。官方下載：\nNexus Mods：{2}\nGitHub：{3}",
                "遊戲版本", "目前的 Sephiria 版本。回報問題時請提供遊戲與 Mod 版本。", "上次檢查", "本次啟動後最近一次完成檢查的本機時間，包含失敗的檢查，不包含已取消的檢查。", "無法連線，請檢查網路", "請求逾時，請重試", "GitHub 請求受限，請稍後重試", "GitHub 請求失敗，請稍後重試", "更新資料異常，請稍後重試",
                "開啟 Mod 日誌資料夾", "開啟存放 support*.log 的資料夾，將這些檔案附在問題回報中即可。重新啟動遊戲前請先複製日誌。", "在 GitHub 回報", "開啟 GitHub 回報表單，自動填入遊戲與 Mod 版本。需要 GitHub 帳號。描述遇到的情況，有日誌時請一併附上。", "開啟資料夾", "複製路徑", "複製連結", "已複製", "複製失敗", "無法開啟", "無法開啟日誌資料夾。再次點擊或確認可複製路徑。沒有日誌也可以回報。", "無法開啟瀏覽器。再次點擊或確認可複製回報連結。",
                "複製回報資訊", "複製遊戲與 Mod 版本及簡短的回報提綱。補充遇到的問題後，可貼到 Nexus Mods 或原本聯絡作者的地方。複製不會傳送任何內容。", "複製", "Sephiria：{0}\nSephiria Enhancements：{1}（{2}）\n\n遇到的問題：\n具體操作：\n單人／連線房主／連線用戶端：" },
            ["ko-KR"] = new[] {
                "모드 버전", "설치된 Sephiria Enhancements 버전입니다. 공식 다운로드 페이지는 아래에 있습니다.",
                "환영 메시지 표시", "게임 실행 후 처음 입장할 때 버전과 공식 링크를 표시합니다. 모드가 켜져 있을 때 자신에게만 표시됩니다.",
                "업데이트 자동 확인", "모드가 켜져 있으면 게임 입장 후 GitHub에서 한 번 확인합니다. 백그라운드에서 실행되며 자동 설치하지 않습니다.",
                "업데이트 확인", "지금 GitHub에서 확인합니다. 정식 버전은 정식 업데이트만, 테스트 버전은 새 테스트 버전도 확인합니다. 새 버전이 현재 게임과 호환된다는 보장은 없습니다.",
                "Nexus Mods 열기", "GitHub 릴리스 열기", "브라우저에서 공식 다운로드 페이지를 엽니다.",
                "켜짐", "꺼짐", "페이지 열기", "확인 전", "확인 중…", "새 버전: {0}", "더 새 버전 없음", "확인 실패, 다시 시도", "다운로드 가능한 버전 없음",
                "{0}\n버전 {1} 로드됨.\nSephiria {2}", "Sephiria Enhancements {0} 버전이 출시되었습니다(설치됨: {1}). 공식 다운로드:\nNexus Mods: {2}\nGitHub: {3}",
                "게임 버전", "현재 Sephiria 버전입니다. 문제 제보 시 게임과 모드 버전을 알려 주세요.", "마지막 확인", "게임 실행 후 마지막으로 완료된 확인의 현지 시각입니다. 실패는 포함하고 취소는 제외합니다.", "연결할 수 없음: 네트워크 확인", "요청 시간 초과: 다시 시도", "GitHub 요청 제한: 나중에 시도", "GitHub 요청 실패: 나중에 시도", "업데이트 데이터 오류: 나중에 시도",
                "Mod 로그 폴더 열기", "support*.log 파일이 있는 폴더를 엽니다. 이 파일들을 제보에 첨부하세요. 게임을 다시 시작하기 전에 로그를 복사하세요.", "GitHub에 제보", "게임과 Mod 버전이 입력된 GitHub 제보 양식을 엽니다. GitHub 계정이 필요합니다. 발생한 상황을 설명하고 로그가 있으면 첨부하세요.", "폴더 열기", "경로 복사", "링크 복사", "복사됨", "복사 실패", "열기 실패", "로그 폴더를 열 수 없습니다. 다시 선택하여 경로를 복사하세요. 로그 없이도 제보할 수 있습니다.", "브라우저를 열 수 없습니다. 다시 선택하여 제보 링크를 복사하세요.",
                "제보 정보 복사", "게임과 Mod 버전, 간단한 제보 양식을 복사합니다. 문제를 적은 뒤 Nexus Mods나 평소 제작자에게 연락하는 곳에 붙여넣으세요. 복사만으로 전송되지는 않습니다.", "복사", "Sephiria: {0}\nSephiria Enhancements: {1} ({2})\n\n발생한 문제:\n수행한 조작:\n싱글플레이 / 협동 호스트 / 협동 클라이언트:" },
            ["ja-JP"] = new[] {
                "Mod のバージョン", "インストール済みの Sephiria Enhancements のバージョンです。公式ダウンロード先は下にあります。",
                "開始メッセージを表示", "起動後、最初にゲームへ入った際にバージョンと公式リンクを表示します。Mod が有効な場合に自分だけに表示されます。",
                "更新を自動確認", "Mod が有効な場合、ゲームへ入った後に GitHub で一度確認します。バックグラウンドで実行し、自動インストールはしません。",
                "更新を確認", "今すぐ GitHub で確認します。正式版は正式な更新のみ、テスト版は新しいテスト版も確認します。新しいバージョンが現在のゲームに対応しているとは限りません。",
                "Nexus Mods を開く", "GitHub のリリースを開く", "ブラウザーで公式ダウンロードページを開きます。",
                "有効", "無効", "ページを開く", "未確認", "確認中…", "新バージョン：{0}", "新しいバージョンなし", "確認失敗・再試行可能", "ダウンロード可能な版なし",
                "{0}\nバージョン {1} を読み込みました。\nSephiria {2}", "Sephiria Enhancements {0} が公開されました（インストール済み：{1}）。公式ダウンロード：\nNexus Mods：{2}\nGitHub：{3}",
                "ゲームバージョン", "現在の Sephiria のバージョンです。報告にはゲームと Mod のバージョンを添えてください。", "最終確認", "今回の起動後、最後に完了した確認の現地時刻です。失敗を含み、キャンセルは含みません。", "接続できません。通信環境を確認", "タイムアウトしました。再試行", "GitHub の要求制限。後で再試行", "GitHub への要求失敗。後で再試行", "更新データが不正です。後で再試行",
                "Mod ログフォルダーを開く", "support*.log の保存先を開きます。これらのファイルを報告に添付してください。ゲームを再起動する前にログをコピーしてください。", "GitHub で報告", "ゲームと Mod のバージョンを入力済みの GitHub 報告フォームを開きます。GitHub アカウントが必要です。状況を説明し、ログがあれば添付してください。", "フォルダーを開く", "パスをコピー", "リンクをコピー", "コピー済み", "コピー失敗", "開けません", "ログフォルダーを開けません。もう一度実行するとパスをコピーします。ログがなくても報告できます。", "ブラウザーを開けません。もう一度実行すると報告リンクをコピーします。",
                "報告情報をコピー", "ゲームと Mod のバージョン、簡単な報告用のひな形をコピーします。状況を記入し、Nexus Mods や普段作者に連絡する場所に貼り付けてください。コピーだけでは送信されません。", "コピー", "Sephiria: {0}\nSephiria Enhancements: {1} ({2})\n\n発生した問題：\n行った操作：\nソロ／協力プレイのホスト／クライアント：" },
            ["de-DE"] = new[] {
                "Mod-Version", "Installierte Version von Sephiria Enhancements. Offizielle Downloadseiten stehen unten.",
                "Begrüßung anzeigen", "Zeigt Version und offizielle Links beim ersten Spielbeitritt nach dem Start. Nur für dich sichtbar, wenn die Mod aktiv ist.",
                "Automatisch nach Updates suchen", "Prüft GitHub einmal nach dem Spielbeitritt, wenn die Mod aktiv ist. Läuft im Hintergrund und installiert nichts automatisch.",
                "Nach Updates suchen", "Prüft jetzt GitHub. Stabile Versionen suchen stabile Updates; Testversionen auch neuere Testversionen. Eine neuere Version garantiert keine Kompatibilität mit deiner Spielversion.",
                "Nexus Mods öffnen", "GitHub-Veröffentlichungen öffnen", "Öffnet die offizielle Downloadseite im Browser.",
                "Aktiviert", "Deaktiviert", "Seite öffnen", "Noch nicht geprüft", "Prüfung läuft…", "Verfügbar: {0}", "Keine neuere Version gefunden", "Prüfung fehlgeschlagen; erneut versuchen", "Keine herunterladbare Version gefunden",
                "{0}\nVersion {1} geladen.\nSephiria {2}", "Sephiria Enhancements {0} ist verfügbar (installiert: {1}). Offizielle Downloads:\nNexus Mods: {2}\nGitHub: {3}",
                "Spielversion", "Aktuelle Sephiria-Version. Bei Problemen Spiel- und Mod-Version angeben.", "Letzte Prüfung", "Ortszeit der letzten abgeschlossenen Prüfung seit Spielstart, auch bei Fehlern. Abbrüche zählen nicht.", "Keine Verbindung; Netzwerk prüfen", "Zeitüberschreitung; erneut versuchen", "GitHub-Anfragelimit; später versuchen", "GitHub-Anfrage fehlgeschlagen; später versuchen", "Ungültige Updatedaten; später versuchen",
                "Mod-Protokollordner öffnen", "Öffnet den Ordner mit support*.log. Hänge diese Dateien an deinen Bericht an. Kopiere sie vor dem nächsten Spielstart.", "Auf GitHub melden", "Öffnet das GitHub-Formular mit ausgefüllter Spiel- und Mod-Version. Ein GitHub-Konto ist nötig. Beschreibe das Problem und füge vorhandene Protokolle hinzu.", "Ordner öffnen", "Pfad kopieren", "Link kopieren", "Kopiert", "Kopieren fehlgeschlagen", "Öffnen fehlgeschlagen", "Der Protokollordner konnte nicht geöffnet werden. Erneut betätigen, um den Pfad zu kopieren. Eine Meldung ist auch ohne Protokolle möglich.", "Der Browser konnte nicht geöffnet werden. Erneut betätigen, um den Meldelink zu kopieren.",
                "Berichtsdaten kopieren", "Kopiert Spiel- und Mod-Version sowie eine kurze Vorlage. Ergänze das Problem und füge den Text auf Nexus Mods oder bei deinem bisherigen Kontakt zum Autor ein. Kopieren sendet nichts.", "Kopieren", "Sephiria: {0}\nSephiria Enhancements: {1} ({2})\n\nWas passiert ist:\nMeine Schritte:\nSolo / Koop-Host / Koop-Client:" },
            ["es-ES"] = new[] {
                "Versión del mod", "Versión instalada de Sephiria Enhancements. Las páginas oficiales de descarga aparecen debajo.",
                "Mostrar bienvenida", "Muestra la versión y los enlaces oficiales al entrar por primera vez tras iniciar el juego. Solo los ves tú y con el mod activado.",
                "Buscar actualizaciones automáticamente", "Consulta GitHub una vez al entrar con el mod activado. Se ejecuta en segundo plano y no instala actualizaciones.",
                "Buscar actualizaciones", "Consulta GitHub ahora. Las versiones estables buscan versiones estables; las de prueba también buscan nuevas versiones de prueba. Una versión más reciente no garantiza compatibilidad con tu juego.",
                "Abrir Nexus Mods", "Abrir versiones de GitHub", "Abre la página oficial de descarga en el navegador.",
                "Activado", "Desactivado", "Abrir página", "Sin comprobar", "Comprobando…", "Disponible: {0}", "No se encontró una versión más reciente", "Error al comprobar; reintenta", "No hay versiones descargables",
                "{0}\nVersión {1} cargada.\nSephiria {2}", "Sephiria Enhancements {0} está disponible (instalada: {1}). Descargas oficiales:\nNexus Mods: {2}\nGitHub: {3}",
                "Versión del juego", "Versión actual de Sephiria. Indica las versiones del juego y del mod al informar de problemas.", "Última comprobación", "Hora local de la última comprobación terminada desde el inicio, incluidos los errores. No cuenta las canceladas.", "Sin conexión; revisa la red", "Tiempo agotado; vuelve a intentarlo", "Límite de GitHub; inténtalo más tarde", "Petición a GitHub fallida; prueba más tarde", "Datos de actualización no válidos; prueba más tarde",
                "Abrir carpeta de registros del Mod", "Abre la carpeta de support*.log. Adjunta estos archivos al informe. Cópialos antes de reiniciar el juego.", "Informar en GitHub", "Abre el formulario de GitHub con las versiones del juego y del Mod ya incluidas. Requiere una cuenta de GitHub. Describe lo ocurrido y adjunta registros si los tienes.", "Abrir carpeta", "Copiar ruta", "Copiar enlace", "Copiado", "Error al copiar", "No se pudo abrir", "No se pudo abrir la carpeta de registros. Activa de nuevo para copiar la ruta. Puedes informar sin registros.", "No se pudo abrir el navegador. Activa de nuevo para copiar el enlace del informe.",
                "Copiar datos del informe", "Copia las versiones del juego y del Mod y una breve plantilla. Añade el problema y pégalo en Nexus Mods o donde ya contactes con el autor. Copiar no envía nada.", "Copiar", "Sephiria: {0}\nSephiria Enhancements: {1} ({2})\n\nQué ocurrió:\nQué hice:\nSolo / anfitrión cooperativo / cliente cooperativo:" },
            ["fr-FR"] = new[] {
                "Version du mod", "Version installée de Sephiria Enhancements. Les pages de téléchargement officielles figurent ci-dessous.",
                "Afficher le message d’accueil", "Affiche la version et les liens officiels à la première entrée en jeu après le lancement. Visible uniquement par vous, si le mod est activé.",
                "Rechercher les mises à jour automatiquement", "Consulte GitHub une fois après l’entrée en jeu si le mod est activé. Fonctionne en arrière-plan, sans installation automatique.",
                "Rechercher les mises à jour", "Consulte GitHub maintenant. Les versions stables recherchent les versions stables ; les versions de test recherchent aussi les nouveaux tests. Une version plus récente ne garantit pas la compatibilité avec votre jeu.",
                "Ouvrir Nexus Mods", "Ouvrir les versions GitHub", "Ouvre la page officielle de téléchargement dans le navigateur.",
                "Activé", "Désactivé", "Ouvrir la page", "Non vérifié", "Vérification…", "Disponible : {0}", "Aucune version plus récente trouvée", "Échec de vérification ; réessayez", "Aucune version téléchargeable trouvée",
                "{0}\nVersion {1} chargée.\nSephiria {2}", "Sephiria Enhancements {0} est disponible (version installée : {1}). Téléchargements officiels :\nNexus Mods : {2}\nGitHub : {3}",
                "Version du jeu", "Version actuelle de Sephiria. Indiquez les versions du jeu et du mod en signalant un problème.", "Dernière vérification", "Heure locale de la dernière vérification terminée depuis le lancement, y compris les échecs. Annulations exclues.", "Connexion impossible ; vérifiez le réseau", "Délai dépassé ; réessayez", "Limite GitHub atteinte ; réessayez plus tard", "Requête GitHub échouée ; réessayez plus tard", "Données de mise à jour invalides ; réessayez plus tard",
                "Ouvrir le dossier des journaux du Mod", "Ouvre le dossier contenant support*.log. Joignez ces fichiers au signalement. Copiez-les avant de redémarrer le jeu.", "Signaler sur GitHub", "Ouvre le formulaire GitHub avec les versions du jeu et du Mod préremplies. Un compte GitHub est requis. Décrivez le problème et joignez les journaux disponibles.", "Ouvrir le dossier", "Copier le chemin", "Copier le lien", "Copié", "Échec de la copie", "Ouverture impossible", "Impossible d’ouvrir le dossier des journaux. Activez à nouveau pour copier son chemin. Vous pouvez signaler le problème sans journaux.", "Impossible d’ouvrir le navigateur. Activez à nouveau pour copier le lien du formulaire.",
                "Copier les infos du signalement", "Copie les versions du jeu et du Mod et un court modèle. Décrivez le problème, puis collez le texte sur Nexus Mods ou là où vous contactez déjà l’auteur. Rien n’est envoyé lors de la copie.", "Copier", "Sephiria : {0}\nSephiria Enhancements : {1} ({2})\n\nProblème rencontré :\nActions effectuées :\nSolo / hôte en coop / client en coop :" },
            ["it-IT"] = new[] {
                "Versione della mod", "Versione installata di Sephiria Enhancements. Le pagine ufficiali di download sono elencate sotto.",
                "Mostra messaggio di benvenuto", "Mostra versione e link ufficiali al primo ingresso dopo l’avvio del gioco. Visibile solo a te, con la mod attiva.",
                "Cerca aggiornamenti automaticamente", "Controlla GitHub una volta dopo l’ingresso in gioco con la mod attiva. Opera in background e non installa aggiornamenti.",
                "Cerca aggiornamenti", "Controlla GitHub ora. Le versioni stabili cercano aggiornamenti stabili; quelle di prova anche nuove versioni di prova. Una versione più recente non garantisce la compatibilità con il gioco.",
                "Apri Nexus Mods", "Apri versioni su GitHub", "Apre la pagina ufficiale di download nel browser.",
                "Attivo", "Disattivo", "Apri pagina", "Non controllato", "Controllo…", "Disponibile: {0}", "Nessuna versione più recente trovata", "Controllo fallito; riprova", "Nessuna versione scaricabile trovata",
                "{0}\nVersione {1} caricata.\nSephiria {2}", "Sephiria Enhancements {0} è disponibile (installata: {1}). Download ufficiali:\nNexus Mods: {2}\nGitHub: {3}",
                "Versione del gioco", "Versione attuale di Sephiria. Indica le versioni del gioco e della mod nelle segnalazioni.", "Ultimo controllo", "Ora locale dell’ultimo controllo completato dall’avvio, inclusi quelli falliti. Gli annullamenti non contano.", "Connessione impossibile; controlla la rete", "Tempo scaduto; riprova", "Limite GitHub raggiunto; riprova più tardi", "Richiesta GitHub fallita; riprova più tardi", "Dati aggiornamento non validi; riprova più tardi",
                "Apri cartella dei registri del Mod", "Apre la cartella con support*.log. Allega questi file alla segnalazione. Copiali prima di riavviare il gioco.", "Segnala su GitHub", "Apre il modulo GitHub con le versioni del gioco e del Mod già inserite. Serve un account GitHub. Descrivi il problema e allega i registri disponibili.", "Apri cartella", "Copia percorso", "Copia collegamento", "Copiato", "Copia non riuscita", "Apertura non riuscita", "Impossibile aprire la cartella dei registri. Attiva di nuovo per copiarne il percorso. Puoi segnalare il problema anche senza registri.", "Impossibile aprire il browser. Attiva di nuovo per copiare il collegamento alla segnalazione.",
                "Copia dati della segnalazione", "Copia le versioni del gioco e del Mod e un breve modello. Aggiungi il problema e incolla su Nexus Mods o dove contatti già l’autore. La copia non invia nulla.", "Copia", "Sephiria: {0}\nSephiria Enhancements: {1} ({2})\n\nProblema riscontrato:\nAzioni eseguite:\nSingolo / host cooperativo / client cooperativo:" },
            ["pl-PL"] = new[] {
                "Wersja moda", "Zainstalowana wersja Sephiria Enhancements. Oficjalne strony pobierania znajdują się poniżej.",
                "Pokaż powitanie", "Pokazuje wersję i oficjalne odnośniki przy pierwszym wejściu do gry po uruchomieniu. Widoczne tylko dla ciebie, gdy mod jest włączony.",
                "Automatycznie sprawdzaj aktualizacje", "Sprawdza GitHub raz po wejściu do gry, gdy mod jest włączony. Działa w tle i nie instaluje aktualizacji.",
                "Sprawdź aktualizacje", "Sprawdza teraz GitHub. Wersje stabilne sprawdzają wydania stabilne, a testowe również nowsze wydania testowe. Nowsza wersja nie gwarantuje zgodności z twoją grą.",
                "Otwórz Nexus Mods", "Otwórz wydania GitHub", "Otwiera oficjalną stronę pobierania w przeglądarce.",
                "Włączone", "Wyłączone", "Otwórz stronę", "Nie sprawdzono", "Sprawdzanie…", "Dostępna: {0}", "Nie znaleziono nowszej wersji", "Sprawdzanie nieudane; spróbuj ponownie", "Brak wersji do pobrania",
                "{0}\nWczytano wersję {1}.\nSephiria {2}", "Dostępna jest wersja Sephiria Enhancements {0} (zainstalowana: {1}). Oficjalne pliki do pobrania:\nNexus Mods: {2}\nGitHub: {3}",
                "Wersja gry", "Bieżąca wersja Sephiria. Zgłaszając problem, podaj wersję gry i moda.", "Ostatnie sprawdzenie", "Lokalny czas ostatniego zakończonego sprawdzenia od uruchomienia gry, także nieudanego. Anulowane nie są liczone.", "Brak połączenia; sprawdź sieć", "Przekroczono czas; spróbuj ponownie", "Limit GitHub; spróbuj później", "Żądanie GitHub nieudane; spróbuj później", "Błędne dane aktualizacji; spróbuj później",
                "Otwórz folder dzienników Modu", "Otwiera folder z plikami support*.log. Dołącz te pliki do zgłoszenia. Skopiuj je przed ponownym uruchomieniem gry.", "Zgłoś na GitHub", "Otwiera formularz GitHub z wpisanymi wersjami gry i Modu. Wymaga konta GitHub. Opisz problem i dołącz dzienniki, jeśli je masz.", "Otwórz folder", "Kopiuj ścieżkę", "Kopiuj link", "Skopiowano", "Błąd kopiowania", "Nie można otworzyć", "Nie można otworzyć folderu dzienników. Aktywuj ponownie, aby skopiować ścieżkę. Możesz zgłosić problem bez dzienników.", "Nie można otworzyć przeglądarki. Aktywuj ponownie, aby skopiować link do zgłoszenia.",
                "Kopiuj dane zgłoszenia", "Kopiuje wersje gry i Modu oraz krótki wzór zgłoszenia. Opisz problem i wklej na Nexus Mods lub tam, gdzie kontaktujesz się z autorem. Kopiowanie niczego nie wysyła.", "Kopiuj", "Sephiria: {0}\nSephiria Enhancements: {1} ({2})\n\nCo się stało:\nWykonane czynności:\nSolo / host współpracy / klient współpracy:" },
            ["pt-BR"] = new[] {
                "Versão do mod", "Versão instalada do Sephiria Enhancements. As páginas oficiais de download estão abaixo.",
                "Mostrar boas-vindas", "Mostra a versão e os links oficiais na primeira entrada após iniciar o jogo. Visível apenas para você, com o mod ativado.",
                "Buscar atualizações automaticamente", "Consulta o GitHub uma vez após entrar no jogo com o mod ativado. Executa em segundo plano e não instala atualizações.",
                "Buscar atualizações", "Consulta o GitHub agora. Versões estáveis buscam versões estáveis; versões de teste também buscam novos testes. Uma versão mais recente não garante compatibilidade com seu jogo.",
                "Abrir Nexus Mods", "Abrir versões no GitHub", "Abre a página oficial de download no navegador.",
                "Ativado", "Desativado", "Abrir página", "Não verificado", "Verificando…", "Disponível: {0}", "Nenhuma versão mais recente encontrada", "Falha na verificação; tente novamente", "Nenhuma versão para download encontrada",
                "{0}\nVersão {1} carregada.\nSephiria {2}", "Sephiria Enhancements {0} está disponível (instalada: {1}). Downloads oficiais:\nNexus Mods: {2}\nGitHub: {3}",
                "Versão do jogo", "Versão atual de Sephiria. Informe as versões do jogo e do mod ao relatar problemas.", "Última verificação", "Hora local da última verificação concluída desde o início, incluindo falhas. Verificações canceladas não contam.", "Sem conexão; verifique a rede", "Tempo esgotado; tente novamente", "Limite do GitHub; tente mais tarde", "Falha na solicitação ao GitHub; tente mais tarde", "Dados de atualização inválidos; tente mais tarde",
                "Abrir pasta de registros do Mod", "Abre a pasta com support*.log. Anexe esses arquivos ao relato. Copie-os antes de reiniciar o jogo.", "Relatar no GitHub", "Abre o formulário do GitHub com as versões do jogo e do Mod preenchidas. Requer uma conta do GitHub. Descreva o problema e anexe registros se disponíveis.", "Abrir pasta", "Copiar caminho", "Copiar link", "Copiado", "Falha ao copiar", "Não foi possível abrir", "Não foi possível abrir a pasta de registros. Ative novamente para copiar o caminho. Você pode relatar o problema sem registros.", "Não foi possível abrir o navegador. Ative novamente para copiar o link do relato.",
                "Copiar dados do relato", "Copia as versões do jogo e do Mod e um breve modelo. Descreva o problema e cole no Nexus Mods ou onde já fala com o autor. Copiar não envia nada.", "Copiar", "Sephiria: {0}\nSephiria Enhancements: {1} ({2})\n\nO que aconteceu:\nO que fiz:\nSolo / anfitrião cooperativo / cliente cooperativo:" },
            ["ru-RU"] = new[] {
                "Версия мода", "Установленная версия Sephiria Enhancements. Официальные страницы загрузки указаны ниже.",
                "Показывать приветствие", "Показывает версию и официальные ссылки при первом входе после запуска игры. Видно только вам, если мод включён.",
                "Автоматически проверять обновления", "Проверяет GitHub один раз после входа в игру, если мод включён. Работает в фоне и не устанавливает обновления.",
                "Проверить обновления", "Проверяет GitHub сейчас. Стабильные версии ищут стабильные обновления, тестовые — также новые тестовые. Новая версия не гарантирует совместимость с вашей игрой.",
                "Открыть Nexus Mods", "Открыть релизы GitHub", "Открывает официальную страницу загрузки в браузере.",
                "Включено", "Выключено", "Открыть страницу", "Не проверено", "Проверка…", "Доступна: {0}", "Более новая версия не найдена", "Ошибка проверки; повторите", "Версии для загрузки не найдены",
                "{0}\nЗагружена версия {1}.\nSephiria {2}", "Доступна Sephiria Enhancements {0} (установлена: {1}). Официальные загрузки:\nNexus Mods: {2}\nGitHub: {3}",
                "Версия игры", "Текущая версия Sephiria. В сообщении о проблеме укажите версии игры и мода.", "Последняя проверка", "Местное время последней завершённой проверки с запуска игры, включая неудачные. Отменённые не учитываются.", "Нет соединения; проверьте сеть", "Время ожидания истекло; повторите", "Лимит GitHub; повторите позже", "Ошибка запроса GitHub; повторите позже", "Неверные данные обновления; повторите позже",
                "Открыть папку журналов мода", "Открывает папку с support*.log. Приложите эти файлы к сообщению. Скопируйте их до перезапуска игры.", "Сообщить на GitHub", "Открывает форму GitHub с заполненными версиями игры и мода. Нужна учётная запись GitHub. Опишите проблему и приложите журналы, если они есть.", "Открыть папку", "Копировать путь", "Копировать ссылку", "Скопировано", "Ошибка копирования", "Не удалось открыть", "Не удалось открыть папку журналов. Нажмите ещё раз, чтобы скопировать путь. О проблеме можно сообщить и без журналов.", "Не удалось открыть браузер. Нажмите ещё раз, чтобы скопировать ссылку на форму.",
                "Копировать данные для сообщения", "Копирует версии игры и мода и краткий шаблон. Опишите проблему и вставьте текст на Nexus Mods или там, где уже общаетесь с автором. Копирование ничего не отправляет.", "Копировать", "Sephiria: {0}\nSephiria Enhancements: {1} ({2})\n\nЧто произошло:\nМои действия:\nСоло / хост совместной игры / клиент совместной игры:" },
            ["sv-SE"] = new[] {
                "Modversion", "Installerad version av Sephiria Enhancements. Officiella nedladdningssidor finns nedan.",
                "Visa välkomstmeddelande", "Visar version och officiella länkar första gången du går in i spelet efter start. Syns bara för dig när modden är aktiverad.",
                "Sök uppdateringar automatiskt", "Kontrollerar GitHub en gång efter att du gått in i spelet med modden aktiverad. Körs i bakgrunden och installerar inget.",
                "Sök efter uppdateringar", "Kontrollerar GitHub nu. Stabila versioner söker stabila utgåvor; testversioner söker även nyare testutgåvor. En nyare version garanterar inte kompatibilitet med ditt spel.",
                "Öppna Nexus Mods", "Öppna GitHub-utgåvor", "Öppnar den officiella nedladdningssidan i webbläsaren.",
                "Aktiverad", "Inaktiverad", "Öppna sida", "Inte kontrollerat", "Kontrollerar…", "Tillgänglig: {0}", "Ingen nyare version hittades", "Kontrollen misslyckades; försök igen", "Ingen nedladdningsbar utgåva hittades",
                "{0}\nVersion {1} har lästs in.\nSephiria {2}", "Sephiria Enhancements {0} finns tillgänglig (installerad: {1}). Officiella nedladdningar:\nNexus Mods: {2}\nGitHub: {3}",
                "Spelversion", "Aktuell Sephiria-version. Ange spel- och modversion när du rapporterar problem.", "Senaste kontroll", "Lokal tid för senaste slutförda kontrollen sedan spelstart, även misslyckade. Avbrutna kontroller räknas inte.", "Kan inte ansluta; kontrollera nätverket", "Tidsgränsen nådd; försök igen", "GitHubs anropsgräns nådd; försök senare", "GitHub-anrop misslyckades; försök senare", "Ogiltiga uppdateringsdata; försök senare",
                "Öppna moddens loggmapp", "Öppnar mappen med support*.log. Bifoga filerna till rapporten. Kopiera dem innan du startar om spelet.", "Rapportera på GitHub", "Öppnar GitHubs formulär med spel- och modversion ifyllda. Ett GitHub-konto krävs. Beskriv problemet och bifoga loggar om du har dem.", "Öppna mapp", "Kopiera sökväg", "Kopiera länk", "Kopierat", "Kopiering misslyckades", "Kunde inte öppna", "Kunde inte öppna loggmappen. Aktivera igen för att kopiera sökvägen. Du kan rapportera problemet utan loggar.", "Kunde inte öppna webbläsaren. Aktivera igen för att kopiera rapportlänken.",
                "Kopiera rapportuppgifter", "Kopierar spel- och modversion samt en kort mall. Beskriv problemet och klistra in på Nexus Mods eller där du redan kontaktar skaparen. Kopiering skickar inget.", "Kopiera", "Sephiria: {0}\nSephiria Enhancements: {1} ({2})\n\nVad som hände:\nVad jag gjorde:\nSolo / samarbetsvärd / samarbetsklient:" },
            ["th-TH"] = new[] {
                "เวอร์ชันม็อด", "เวอร์ชัน Sephiria Enhancements ที่ติดตั้งอยู่ ลิงก์ดาวน์โหลดทางการอยู่ด้านล่าง",
                "แสดงข้อความต้อนรับ", "แสดงเวอร์ชันและลิงก์ทางการเมื่อเข้าเล่นครั้งแรกหลังเปิดเกม เห็นเฉพาะคุณเมื่อเปิดใช้งานม็อด",
                "ตรวจสอบอัปเดตอัตโนมัติ", "ตรวจสอบ GitHub หนึ่งครั้งหลังเข้าเล่นขณะเปิดใช้งานม็อด ทำงานเบื้องหลังและไม่ติดตั้งอัปเดตอัตโนมัติ",
                "ตรวจสอบอัปเดต", "ตรวจสอบ GitHub ตอนนี้ รุ่นเสถียรตรวจสอบเฉพาะรุ่นเสถียร รุ่นทดสอบตรวจสอบรุ่นทดสอบใหม่ด้วย รุ่นใหม่ไม่ได้รับประกันว่าจะเข้ากับเวอร์ชันเกมของคุณ",
                "เปิด Nexus Mods", "เปิดหน้ารุ่นบน GitHub", "เปิดหน้าดาวน์โหลดทางการในเบราว์เซอร์",
                "เปิดใช้งาน", "ปิดใช้งาน", "เปิดหน้าเว็บ", "ยังไม่ตรวจสอบ", "กำลังตรวจสอบ…", "มีรุ่นใหม่: {0}", "ไม่พบรุ่นที่ใหม่กว่า", "ตรวจสอบไม่สำเร็จ โปรดลองอีกครั้ง", "ไม่พบรุ่นที่ดาวน์โหลดได้",
                "{0}\nโหลดเวอร์ชัน {1} แล้ว\nSephiria {2}", "มี Sephiria Enhancements {0} แล้ว (ติดตั้งอยู่: {1}) ดาวน์โหลดทางการ:\nNexus Mods: {2}\nGitHub: {3}",
                "เวอร์ชันเกม", "เวอร์ชัน Sephiria ปัจจุบัน โปรดระบุเวอร์ชันเกมและม็อดเมื่อแจ้งปัญหา", "ตรวจสอบล่าสุด", "เวลาท้องถิ่นของการตรวจสอบที่เสร็จสิ้นล่าสุดตั้งแต่เปิดเกม รวมครั้งที่ล้มเหลว แต่ไม่นับครั้งที่ยกเลิก", "เชื่อมต่อไม่ได้ โปรดตรวจสอบเครือข่าย", "คำขอหมดเวลา โปรดลองใหม่", "คำขอ GitHub ถึงขีดจำกัด โปรดลองภายหลัง", "คำขอ GitHub ล้มเหลว โปรดลองภายหลัง", "ข้อมูลอัปเดตไม่ถูกต้อง โปรดลองภายหลัง",
                "เปิดโฟลเดอร์บันทึก Mod", "เปิดโฟลเดอร์ที่มี support*.log แนบไฟล์เหล่านี้ในรายงาน โปรดคัดลอกก่อนเริ่มเกมใหม่", "รายงานบน GitHub", "เปิดแบบฟอร์ม GitHub พร้อมกรอกเวอร์ชันเกมและ Mod ต้องมีบัญชี GitHub อธิบายปัญหาและแนบบันทึกหากมี", "เปิดโฟลเดอร์", "คัดลอกเส้นทาง", "คัดลอกลิงก์", "คัดลอกแล้ว", "คัดลอกไม่สำเร็จ", "เปิดไม่ได้", "เปิดโฟลเดอร์บันทึกไม่ได้ เลือกอีกครั้งเพื่อคัดลอกเส้นทาง คุณรายงานปัญหาได้แม้ไม่มีบันทึก", "เปิดเบราว์เซอร์ไม่ได้ เลือกอีกครั้งเพื่อคัดลอกลิงก์รายงาน",
                "คัดลอกข้อมูลรายงาน", "คัดลอกเวอร์ชันเกมและ Mod พร้อมแบบร่างสั้น ๆ เติมรายละเอียดปัญหาแล้ววางบน Nexus Mods หรือช่องทางที่ใช้ติดต่อผู้สร้างอยู่แล้ว การคัดลอกจะไม่ส่งข้อมูล", "คัดลอก", "Sephiria: {0}\nSephiria Enhancements: {1} ({2})\n\nปัญหาที่พบ:\nสิ่งที่ทำ:\nเล่นคนเดียว / โฮสต์ร่วมเล่น / ไคลเอนต์ร่วมเล่น:" },
            ["tr-TR"] = new[] {
                "Mod sürümü", "Yüklü Sephiria Enhancements sürümü. Resmî indirme sayfaları aşağıdadır.",
                "Karşılama mesajını göster", "Oyun açıldıktan sonra ilk girişte sürümü ve resmî bağlantıları gösterir. Mod etkinken yalnızca siz görürsünüz.",
                "Güncellemeleri otomatik denetle", "Mod etkinken oyuna girdikten sonra GitHub’ı bir kez denetler. Arka planda çalışır, güncelleme yüklemez.",
                "Güncellemeleri denetle", "GitHub’ı şimdi denetler. Kararlı sürümler kararlı güncellemeleri, test sürümleri yeni test sürümlerini de arar. Yeni sürüm, oyun sürümünüzle uyumluluğu garanti etmez.",
                "Nexus Mods’u aç", "GitHub sürümlerini aç", "Resmî indirme sayfasını tarayıcıda açar.",
                "Etkin", "Devre dışı", "Sayfayı aç", "Denetlenmedi", "Denetleniyor…", "Yeni sürüm: {0}", "Daha yeni sürüm bulunamadı", "Denetleme başarısız; tekrar deneyin", "İndirilebilir sürüm bulunamadı",
                "{0}\n{1} sürümü yüklendi.\nSephiria {2}", "Sephiria Enhancements {0} yayımlandı (yüklü: {1}). Resmî indirmeler:\nNexus Mods: {2}\nGitHub: {3}",
                "Oyun sürümü", "Geçerli Sephiria sürümü. Sorun bildirirken oyun ve mod sürümlerini belirtin.", "Son denetleme", "Oyun açıldığından beri tamamlanan son denetlemenin yerel saati; başarısız olanlar dahil, iptaller hariç.", "Bağlanılamadı; ağı denetleyin", "İstek zaman aşımına uğradı; tekrar deneyin", "GitHub istek sınırı; sonra deneyin", "GitHub isteği başarısız; sonra deneyin", "Güncelleme verisi geçersiz; sonra deneyin",
                "Mod günlük klasörünü aç", "support*.log dosyalarının klasörünü açar. Bu dosyaları bildirime ekleyin. Oyunu yeniden başlatmadan önce kopyalayın.", "GitHub’da bildir", "Oyun ve Mod sürümleri doldurulmuş GitHub formunu açar. GitHub hesabı gerekir. Sorunu açıklayın ve varsa günlükleri ekleyin.", "Klasörü aç", "Yolu kopyala", "Bağlantıyı kopyala", "Kopyalandı", "Kopyalama başarısız", "Açılamadı", "Günlük klasörü açılamadı. Yolu kopyalamak için tekrar etkinleştirin. Günlük olmadan da sorun bildirebilirsiniz.", "Tarayıcı açılamadı. Bildirim bağlantısını kopyalamak için tekrar etkinleştirin.",
                "Bildirim bilgilerini kopyala", "Oyun ve Mod sürümlerini ve kısa bir taslağı kopyalar. Sorunu ekleyip Nexus Mods’a veya yapımcıyla zaten iletişim kurduğunuz yere yapıştırın. Kopyalama hiçbir şey göndermez.", "Kopyala", "Sephiria: {0}\nSephiria Enhancements: {1} ({2})\n\nKarşılaşılan sorun:\nYaptığım işlemler:\nTek oyunculu / ortak oyun sunucusu / ortak oyun istemcisi:" }
        };

        internal static void Register(Action<string, string, string> addText, IEnumerable<string> languages) =>
            LocalizationGroup.Register(addText, languages, Keys, Texts);

        internal static string StatusKey(ModUpdateStatus status)
        {
            switch (status)
            {
                case ModUpdateStatus.Checking: return Checking;
                case ModUpdateStatus.UpdateAvailable: return Available;
                case ModUpdateStatus.UpToDate: return UpToDate;
                case ModUpdateStatus.Failed: return Failed;
                case ModUpdateStatus.ConnectionFailed: return ConnectionFailed;
                case ModUpdateStatus.TimedOut: return TimedOut;
                case ModUpdateStatus.RateLimited: return RateLimited;
                case ModUpdateStatus.ServiceError: return ServiceError;
                case ModUpdateStatus.InvalidResponse: return InvalidResponse;
                case ModUpdateStatus.NoPublishedVersion: return NoPublishedVersion;
                default: return NotChecked;
            }
        }
    }
}
