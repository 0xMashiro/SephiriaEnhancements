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
        private static readonly string[] Keys =
        {
            Version, VersionHelp, WelcomeSetting, WelcomeHelp, AutomaticSetting, AutomaticHelp,
            Check, CheckHelp, Nexus, GitHub, LinkHelp, On, Off, Open, NotChecked, Checking,
            Available, UpToDate, Failed, NoPublishedVersion, Welcome, Update,
            GameVersion, GameVersionHelp, LastChecked, LastCheckedHelp, ConnectionFailed, TimedOut, RateLimited, ServiceError, InvalidResponse
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
                "Game version", "Current Sephiria version. Include game and Mod versions in problem reports.", "Last check", "Local time of the last completed check since launch, including failures. Cancelled checks are excluded.", "Cannot connect; check your network", "Request timed out; try again", "GitHub request limit; try later", "GitHub request failed; try later", "Invalid update data; try later" },
            ["zh-CN"] = new[] {
                "Mod 版本", "当前安装的 Sephiria 增强版本。下方提供官方下载入口。",
                "显示欢迎信息", "每次启动后首次进入游戏时显示版本和官方地址。仅自己可见，Mod 关闭时不显示。",
                "自动检查更新", "Mod 启用时，进入游戏后向 GitHub 检查一次更新。在后台进行，不会自动安装。",
                "检查更新", "立即向 GitHub 检查更新。正式版只检查正式更新；测试版也检查更新的测试版。新版本不代表一定兼容当前游戏版本。",
                "打开 Nexus Mods", "打开 GitHub 发布页", "在浏览器中打开官方下载页面。",
                "开启", "关闭", "打开页面", "尚未检查", "正在检查…", "有新版本：{0}", "未发现更新版本", "检查失败，请重试", "未找到可下载版本",
                "{0}\n已加载版本 {1}。\nSephiria {2}", "Sephiria 增强 {0} 已发布（当前安装：{1}）。官方下载：\nNexus Mods：{2}\nGitHub：{3}",
                "游戏版本", "当前 Sephiria 版本。反馈问题时请提供游戏和 Mod 版本。", "上次检查", "本次启动后最近一次完成检查的本机时间，包含失败的检查，不包含已取消的检查。", "无法连接，请检查网络", "请求超时，请重试", "GitHub 请求受限，请稍后重试", "GitHub 请求失败，请稍后重试", "更新数据异常，请稍后重试" },
            ["zh-TW"] = new[] {
                "Mod 版本", "目前安裝的 Sephiria 增強版本。下方提供官方下載入口。",
                "顯示歡迎訊息", "每次啟動後首次進入遊戲時顯示版本和官方網址。僅自己可見，Mod 關閉時不顯示。",
                "自動檢查更新", "Mod 啟用時，進入遊戲後向 GitHub 檢查一次更新。在背景執行，不會自動安裝。",
                "檢查更新", "立即向 GitHub 檢查更新。正式版只檢查正式更新；測試版也檢查較新的測試版。新版本不代表一定相容目前遊戲版本。",
                "開啟 Nexus Mods", "開啟 GitHub 發布頁", "在瀏覽器中開啟官方下載頁面。",
                "開啟", "關閉", "開啟頁面", "尚未檢查", "正在檢查…", "有新版本：{0}", "未發現更新版本", "檢查失敗，請重試", "未找到可下載版本",
                "{0}\n已載入版本 {1}。\nSephiria {2}", "Sephiria 增強 {0} 已發布（目前安裝：{1}）。官方下載：\nNexus Mods：{2}\nGitHub：{3}",
                "遊戲版本", "目前的 Sephiria 版本。回報問題時請提供遊戲與 Mod 版本。", "上次檢查", "本次啟動後最近一次完成檢查的本機時間，包含失敗的檢查，不包含已取消的檢查。", "無法連線，請檢查網路", "請求逾時，請重試", "GitHub 請求受限，請稍後重試", "GitHub 請求失敗，請稍後重試", "更新資料異常，請稍後重試" },
            ["ko-KR"] = new[] {
                "모드 버전", "설치된 Sephiria Enhancements 버전입니다. 공식 다운로드 페이지는 아래에 있습니다.",
                "환영 메시지 표시", "게임 실행 후 처음 입장할 때 버전과 공식 링크를 표시합니다. 모드가 켜져 있을 때 자신에게만 표시됩니다.",
                "업데이트 자동 확인", "모드가 켜져 있으면 게임 입장 후 GitHub에서 한 번 확인합니다. 백그라운드에서 실행되며 자동 설치하지 않습니다.",
                "업데이트 확인", "지금 GitHub에서 확인합니다. 정식 버전은 정식 업데이트만, 테스트 버전은 새 테스트 버전도 확인합니다. 새 버전이 현재 게임과 호환된다는 보장은 없습니다.",
                "Nexus Mods 열기", "GitHub 릴리스 열기", "브라우저에서 공식 다운로드 페이지를 엽니다.",
                "켜짐", "꺼짐", "페이지 열기", "확인 전", "확인 중…", "새 버전: {0}", "더 새 버전 없음", "확인 실패, 다시 시도", "다운로드 가능한 버전 없음",
                "{0}\n버전 {1} 로드됨.\nSephiria {2}", "Sephiria Enhancements {0} 버전이 출시되었습니다(설치됨: {1}). 공식 다운로드:\nNexus Mods: {2}\nGitHub: {3}",
                "게임 버전", "현재 Sephiria 버전입니다. 문제 제보 시 게임과 모드 버전을 알려 주세요.", "마지막 확인", "게임 실행 후 마지막으로 완료된 확인의 현지 시각입니다. 실패는 포함하고 취소는 제외합니다.", "연결할 수 없음: 네트워크 확인", "요청 시간 초과: 다시 시도", "GitHub 요청 제한: 나중에 시도", "GitHub 요청 실패: 나중에 시도", "업데이트 데이터 오류: 나중에 시도" },
            ["ja-JP"] = new[] {
                "Mod のバージョン", "インストール済みの Sephiria Enhancements のバージョンです。公式ダウンロード先は下にあります。",
                "開始メッセージを表示", "起動後、最初にゲームへ入った際にバージョンと公式リンクを表示します。Mod が有効な場合に自分だけに表示されます。",
                "更新を自動確認", "Mod が有効な場合、ゲームへ入った後に GitHub で一度確認します。バックグラウンドで実行し、自動インストールはしません。",
                "更新を確認", "今すぐ GitHub で確認します。正式版は正式な更新のみ、テスト版は新しいテスト版も確認します。新しいバージョンが現在のゲームに対応しているとは限りません。",
                "Nexus Mods を開く", "GitHub のリリースを開く", "ブラウザーで公式ダウンロードページを開きます。",
                "有効", "無効", "ページを開く", "未確認", "確認中…", "新バージョン：{0}", "新しいバージョンなし", "確認失敗・再試行可能", "ダウンロード可能な版なし",
                "{0}\nバージョン {1} を読み込みました。\nSephiria {2}", "Sephiria Enhancements {0} が公開されました（インストール済み：{1}）。公式ダウンロード：\nNexus Mods：{2}\nGitHub：{3}",
                "ゲームバージョン", "現在の Sephiria のバージョンです。報告にはゲームと Mod のバージョンを添えてください。", "最終確認", "今回の起動後、最後に完了した確認の現地時刻です。失敗を含み、キャンセルは含みません。", "接続できません。通信環境を確認", "タイムアウトしました。再試行", "GitHub の要求制限。後で再試行", "GitHub への要求失敗。後で再試行", "更新データが不正です。後で再試行" },
            ["de-DE"] = new[] {
                "Mod-Version", "Installierte Version von Sephiria Enhancements. Offizielle Downloadseiten stehen unten.",
                "Begrüßung anzeigen", "Zeigt Version und offizielle Links beim ersten Spielbeitritt nach dem Start. Nur für dich sichtbar, wenn die Mod aktiv ist.",
                "Automatisch nach Updates suchen", "Prüft GitHub einmal nach dem Spielbeitritt, wenn die Mod aktiv ist. Läuft im Hintergrund und installiert nichts automatisch.",
                "Nach Updates suchen", "Prüft jetzt GitHub. Stabile Versionen suchen stabile Updates; Testversionen auch neuere Testversionen. Eine neuere Version garantiert keine Kompatibilität mit deiner Spielversion.",
                "Nexus Mods öffnen", "GitHub-Veröffentlichungen öffnen", "Öffnet die offizielle Downloadseite im Browser.",
                "Aktiviert", "Deaktiviert", "Seite öffnen", "Noch nicht geprüft", "Prüfung läuft…", "Verfügbar: {0}", "Keine neuere Version gefunden", "Prüfung fehlgeschlagen; erneut versuchen", "Keine herunterladbare Version gefunden",
                "{0}\nVersion {1} geladen.\nSephiria {2}", "Sephiria Enhancements {0} ist verfügbar (installiert: {1}). Offizielle Downloads:\nNexus Mods: {2}\nGitHub: {3}",
                "Spielversion", "Aktuelle Sephiria-Version. Bei Problemen Spiel- und Mod-Version angeben.", "Letzte Prüfung", "Ortszeit der letzten abgeschlossenen Prüfung seit Spielstart, auch bei Fehlern. Abbrüche zählen nicht.", "Keine Verbindung; Netzwerk prüfen", "Zeitüberschreitung; erneut versuchen", "GitHub-Anfragelimit; später versuchen", "GitHub-Anfrage fehlgeschlagen; später versuchen", "Ungültige Updatedaten; später versuchen" },
            ["es-ES"] = new[] {
                "Versión del mod", "Versión instalada de Sephiria Enhancements. Las páginas oficiales de descarga aparecen debajo.",
                "Mostrar bienvenida", "Muestra la versión y los enlaces oficiales al entrar por primera vez tras iniciar el juego. Solo los ves tú y con el mod activado.",
                "Buscar actualizaciones automáticamente", "Consulta GitHub una vez al entrar con el mod activado. Se ejecuta en segundo plano y no instala actualizaciones.",
                "Buscar actualizaciones", "Consulta GitHub ahora. Las versiones estables buscan versiones estables; las de prueba también buscan nuevas versiones de prueba. Una versión más reciente no garantiza compatibilidad con tu juego.",
                "Abrir Nexus Mods", "Abrir versiones de GitHub", "Abre la página oficial de descarga en el navegador.",
                "Activado", "Desactivado", "Abrir página", "Sin comprobar", "Comprobando…", "Disponible: {0}", "No se encontró una versión más reciente", "Error al comprobar; reintenta", "No hay versiones descargables",
                "{0}\nVersión {1} cargada.\nSephiria {2}", "Sephiria Enhancements {0} está disponible (instalada: {1}). Descargas oficiales:\nNexus Mods: {2}\nGitHub: {3}",
                "Versión del juego", "Versión actual de Sephiria. Indica las versiones del juego y del mod al informar de problemas.", "Última comprobación", "Hora local de la última comprobación terminada desde el inicio, incluidos los errores. No cuenta las canceladas.", "Sin conexión; revisa la red", "Tiempo agotado; vuelve a intentarlo", "Límite de GitHub; inténtalo más tarde", "Petición a GitHub fallida; prueba más tarde", "Datos de actualización no válidos; prueba más tarde" },
            ["fr-FR"] = new[] {
                "Version du mod", "Version installée de Sephiria Enhancements. Les pages de téléchargement officielles figurent ci-dessous.",
                "Afficher le message d’accueil", "Affiche la version et les liens officiels à la première entrée en jeu après le lancement. Visible uniquement par vous, si le mod est activé.",
                "Rechercher les mises à jour automatiquement", "Consulte GitHub une fois après l’entrée en jeu si le mod est activé. Fonctionne en arrière-plan, sans installation automatique.",
                "Rechercher les mises à jour", "Consulte GitHub maintenant. Les versions stables recherchent les versions stables ; les versions de test recherchent aussi les nouveaux tests. Une version plus récente ne garantit pas la compatibilité avec votre jeu.",
                "Ouvrir Nexus Mods", "Ouvrir les versions GitHub", "Ouvre la page officielle de téléchargement dans le navigateur.",
                "Activé", "Désactivé", "Ouvrir la page", "Non vérifié", "Vérification…", "Disponible : {0}", "Aucune version plus récente trouvée", "Échec de vérification ; réessayez", "Aucune version téléchargeable trouvée",
                "{0}\nVersion {1} chargée.\nSephiria {2}", "Sephiria Enhancements {0} est disponible (version installée : {1}). Téléchargements officiels :\nNexus Mods : {2}\nGitHub : {3}",
                "Version du jeu", "Version actuelle de Sephiria. Indiquez les versions du jeu et du mod en signalant un problème.", "Dernière vérification", "Heure locale de la dernière vérification terminée depuis le lancement, y compris les échecs. Annulations exclues.", "Connexion impossible ; vérifiez le réseau", "Délai dépassé ; réessayez", "Limite GitHub atteinte ; réessayez plus tard", "Requête GitHub échouée ; réessayez plus tard", "Données de mise à jour invalides ; réessayez plus tard" },
            ["it-IT"] = new[] {
                "Versione della mod", "Versione installata di Sephiria Enhancements. Le pagine ufficiali di download sono elencate sotto.",
                "Mostra messaggio di benvenuto", "Mostra versione e link ufficiali al primo ingresso dopo l’avvio del gioco. Visibile solo a te, con la mod attiva.",
                "Cerca aggiornamenti automaticamente", "Controlla GitHub una volta dopo l’ingresso in gioco con la mod attiva. Opera in background e non installa aggiornamenti.",
                "Cerca aggiornamenti", "Controlla GitHub ora. Le versioni stabili cercano aggiornamenti stabili; quelle di prova anche nuove versioni di prova. Una versione più recente non garantisce la compatibilità con il gioco.",
                "Apri Nexus Mods", "Apri versioni su GitHub", "Apre la pagina ufficiale di download nel browser.",
                "Attivo", "Disattivo", "Apri pagina", "Non controllato", "Controllo…", "Disponibile: {0}", "Nessuna versione più recente trovata", "Controllo fallito; riprova", "Nessuna versione scaricabile trovata",
                "{0}\nVersione {1} caricata.\nSephiria {2}", "Sephiria Enhancements {0} è disponibile (installata: {1}). Download ufficiali:\nNexus Mods: {2}\nGitHub: {3}",
                "Versione del gioco", "Versione attuale di Sephiria. Indica le versioni del gioco e della mod nelle segnalazioni.", "Ultimo controllo", "Ora locale dell’ultimo controllo completato dall’avvio, inclusi quelli falliti. Gli annullamenti non contano.", "Connessione impossibile; controlla la rete", "Tempo scaduto; riprova", "Limite GitHub raggiunto; riprova più tardi", "Richiesta GitHub fallita; riprova più tardi", "Dati aggiornamento non validi; riprova più tardi" },
            ["pl-PL"] = new[] {
                "Wersja moda", "Zainstalowana wersja Sephiria Enhancements. Oficjalne strony pobierania znajdują się poniżej.",
                "Pokaż powitanie", "Pokazuje wersję i oficjalne odnośniki przy pierwszym wejściu do gry po uruchomieniu. Widoczne tylko dla ciebie, gdy mod jest włączony.",
                "Automatycznie sprawdzaj aktualizacje", "Sprawdza GitHub raz po wejściu do gry, gdy mod jest włączony. Działa w tle i nie instaluje aktualizacji.",
                "Sprawdź aktualizacje", "Sprawdza teraz GitHub. Wersje stabilne sprawdzają wydania stabilne, a testowe również nowsze wydania testowe. Nowsza wersja nie gwarantuje zgodności z twoją grą.",
                "Otwórz Nexus Mods", "Otwórz wydania GitHub", "Otwiera oficjalną stronę pobierania w przeglądarce.",
                "Włączone", "Wyłączone", "Otwórz stronę", "Nie sprawdzono", "Sprawdzanie…", "Dostępna: {0}", "Nie znaleziono nowszej wersji", "Sprawdzanie nieudane; spróbuj ponownie", "Brak wersji do pobrania",
                "{0}\nWczytano wersję {1}.\nSephiria {2}", "Dostępna jest wersja Sephiria Enhancements {0} (zainstalowana: {1}). Oficjalne pliki do pobrania:\nNexus Mods: {2}\nGitHub: {3}",
                "Wersja gry", "Bieżąca wersja Sephiria. Zgłaszając problem, podaj wersję gry i moda.", "Ostatnie sprawdzenie", "Lokalny czas ostatniego zakończonego sprawdzenia od uruchomienia gry, także nieudanego. Anulowane nie są liczone.", "Brak połączenia; sprawdź sieć", "Przekroczono czas; spróbuj ponownie", "Limit GitHub; spróbuj później", "Żądanie GitHub nieudane; spróbuj później", "Błędne dane aktualizacji; spróbuj później" },
            ["pt-BR"] = new[] {
                "Versão do mod", "Versão instalada do Sephiria Enhancements. As páginas oficiais de download estão abaixo.",
                "Mostrar boas-vindas", "Mostra a versão e os links oficiais na primeira entrada após iniciar o jogo. Visível apenas para você, com o mod ativado.",
                "Buscar atualizações automaticamente", "Consulta o GitHub uma vez após entrar no jogo com o mod ativado. Executa em segundo plano e não instala atualizações.",
                "Buscar atualizações", "Consulta o GitHub agora. Versões estáveis buscam versões estáveis; versões de teste também buscam novos testes. Uma versão mais recente não garante compatibilidade com seu jogo.",
                "Abrir Nexus Mods", "Abrir versões no GitHub", "Abre a página oficial de download no navegador.",
                "Ativado", "Desativado", "Abrir página", "Não verificado", "Verificando…", "Disponível: {0}", "Nenhuma versão mais recente encontrada", "Falha na verificação; tente novamente", "Nenhuma versão para download encontrada",
                "{0}\nVersão {1} carregada.\nSephiria {2}", "Sephiria Enhancements {0} está disponível (instalada: {1}). Downloads oficiais:\nNexus Mods: {2}\nGitHub: {3}",
                "Versão do jogo", "Versão atual de Sephiria. Informe as versões do jogo e do mod ao relatar problemas.", "Última verificação", "Hora local da última verificação concluída desde o início, incluindo falhas. Verificações canceladas não contam.", "Sem conexão; verifique a rede", "Tempo esgotado; tente novamente", "Limite do GitHub; tente mais tarde", "Falha na solicitação ao GitHub; tente mais tarde", "Dados de atualização inválidos; tente mais tarde" },
            ["ru-RU"] = new[] {
                "Версия мода", "Установленная версия Sephiria Enhancements. Официальные страницы загрузки указаны ниже.",
                "Показывать приветствие", "Показывает версию и официальные ссылки при первом входе после запуска игры. Видно только вам, если мод включён.",
                "Автоматически проверять обновления", "Проверяет GitHub один раз после входа в игру, если мод включён. Работает в фоне и не устанавливает обновления.",
                "Проверить обновления", "Проверяет GitHub сейчас. Стабильные версии ищут стабильные обновления, тестовые — также новые тестовые. Новая версия не гарантирует совместимость с вашей игрой.",
                "Открыть Nexus Mods", "Открыть релизы GitHub", "Открывает официальную страницу загрузки в браузере.",
                "Включено", "Выключено", "Открыть страницу", "Не проверено", "Проверка…", "Доступна: {0}", "Более новая версия не найдена", "Ошибка проверки; повторите", "Версии для загрузки не найдены",
                "{0}\nЗагружена версия {1}.\nSephiria {2}", "Доступна Sephiria Enhancements {0} (установлена: {1}). Официальные загрузки:\nNexus Mods: {2}\nGitHub: {3}",
                "Версия игры", "Текущая версия Sephiria. В сообщении о проблеме укажите версии игры и мода.", "Последняя проверка", "Местное время последней завершённой проверки с запуска игры, включая неудачные. Отменённые не учитываются.", "Нет соединения; проверьте сеть", "Время ожидания истекло; повторите", "Лимит GitHub; повторите позже", "Ошибка запроса GitHub; повторите позже", "Неверные данные обновления; повторите позже" },
            ["sv-SE"] = new[] {
                "Modversion", "Installerad version av Sephiria Enhancements. Officiella nedladdningssidor finns nedan.",
                "Visa välkomstmeddelande", "Visar version och officiella länkar första gången du går in i spelet efter start. Syns bara för dig när modden är aktiverad.",
                "Sök uppdateringar automatiskt", "Kontrollerar GitHub en gång efter att du gått in i spelet med modden aktiverad. Körs i bakgrunden och installerar inget.",
                "Sök efter uppdateringar", "Kontrollerar GitHub nu. Stabila versioner söker stabila utgåvor; testversioner söker även nyare testutgåvor. En nyare version garanterar inte kompatibilitet med ditt spel.",
                "Öppna Nexus Mods", "Öppna GitHub-utgåvor", "Öppnar den officiella nedladdningssidan i webbläsaren.",
                "Aktiverad", "Inaktiverad", "Öppna sida", "Inte kontrollerat", "Kontrollerar…", "Tillgänglig: {0}", "Ingen nyare version hittades", "Kontrollen misslyckades; försök igen", "Ingen nedladdningsbar utgåva hittades",
                "{0}\nVersion {1} har lästs in.\nSephiria {2}", "Sephiria Enhancements {0} finns tillgänglig (installerad: {1}). Officiella nedladdningar:\nNexus Mods: {2}\nGitHub: {3}",
                "Spelversion", "Aktuell Sephiria-version. Ange spel- och modversion när du rapporterar problem.", "Senaste kontroll", "Lokal tid för senaste slutförda kontrollen sedan spelstart, även misslyckade. Avbrutna kontroller räknas inte.", "Kan inte ansluta; kontrollera nätverket", "Tidsgränsen nådd; försök igen", "GitHubs anropsgräns nådd; försök senare", "GitHub-anrop misslyckades; försök senare", "Ogiltiga uppdateringsdata; försök senare" },
            ["th-TH"] = new[] {
                "เวอร์ชันม็อด", "เวอร์ชัน Sephiria Enhancements ที่ติดตั้งอยู่ ลิงก์ดาวน์โหลดทางการอยู่ด้านล่าง",
                "แสดงข้อความต้อนรับ", "แสดงเวอร์ชันและลิงก์ทางการเมื่อเข้าเล่นครั้งแรกหลังเปิดเกม เห็นเฉพาะคุณเมื่อเปิดใช้งานม็อด",
                "ตรวจสอบอัปเดตอัตโนมัติ", "ตรวจสอบ GitHub หนึ่งครั้งหลังเข้าเล่นขณะเปิดใช้งานม็อด ทำงานเบื้องหลังและไม่ติดตั้งอัปเดตอัตโนมัติ",
                "ตรวจสอบอัปเดต", "ตรวจสอบ GitHub ตอนนี้ รุ่นเสถียรตรวจสอบเฉพาะรุ่นเสถียร รุ่นทดสอบตรวจสอบรุ่นทดสอบใหม่ด้วย รุ่นใหม่ไม่ได้รับประกันว่าจะเข้ากับเวอร์ชันเกมของคุณ",
                "เปิด Nexus Mods", "เปิดหน้ารุ่นบน GitHub", "เปิดหน้าดาวน์โหลดทางการในเบราว์เซอร์",
                "เปิดใช้งาน", "ปิดใช้งาน", "เปิดหน้าเว็บ", "ยังไม่ตรวจสอบ", "กำลังตรวจสอบ…", "มีรุ่นใหม่: {0}", "ไม่พบรุ่นที่ใหม่กว่า", "ตรวจสอบไม่สำเร็จ โปรดลองอีกครั้ง", "ไม่พบรุ่นที่ดาวน์โหลดได้",
                "{0}\nโหลดเวอร์ชัน {1} แล้ว\nSephiria {2}", "มี Sephiria Enhancements {0} แล้ว (ติดตั้งอยู่: {1}) ดาวน์โหลดทางการ:\nNexus Mods: {2}\nGitHub: {3}",
                "เวอร์ชันเกม", "เวอร์ชัน Sephiria ปัจจุบัน โปรดระบุเวอร์ชันเกมและม็อดเมื่อแจ้งปัญหา", "ตรวจสอบล่าสุด", "เวลาท้องถิ่นของการตรวจสอบที่เสร็จสิ้นล่าสุดตั้งแต่เปิดเกม รวมครั้งที่ล้มเหลว แต่ไม่นับครั้งที่ยกเลิก", "เชื่อมต่อไม่ได้ โปรดตรวจสอบเครือข่าย", "คำขอหมดเวลา โปรดลองใหม่", "คำขอ GitHub ถึงขีดจำกัด โปรดลองภายหลัง", "คำขอ GitHub ล้มเหลว โปรดลองภายหลัง", "ข้อมูลอัปเดตไม่ถูกต้อง โปรดลองภายหลัง" },
            ["tr-TR"] = new[] {
                "Mod sürümü", "Yüklü Sephiria Enhancements sürümü. Resmî indirme sayfaları aşağıdadır.",
                "Karşılama mesajını göster", "Oyun açıldıktan sonra ilk girişte sürümü ve resmî bağlantıları gösterir. Mod etkinken yalnızca siz görürsünüz.",
                "Güncellemeleri otomatik denetle", "Mod etkinken oyuna girdikten sonra GitHub’ı bir kez denetler. Arka planda çalışır, güncelleme yüklemez.",
                "Güncellemeleri denetle", "GitHub’ı şimdi denetler. Kararlı sürümler kararlı güncellemeleri, test sürümleri yeni test sürümlerini de arar. Yeni sürüm, oyun sürümünüzle uyumluluğu garanti etmez.",
                "Nexus Mods’u aç", "GitHub sürümlerini aç", "Resmî indirme sayfasını tarayıcıda açar.",
                "Etkin", "Devre dışı", "Sayfayı aç", "Denetlenmedi", "Denetleniyor…", "Yeni sürüm: {0}", "Daha yeni sürüm bulunamadı", "Denetleme başarısız; tekrar deneyin", "İndirilebilir sürüm bulunamadı",
                "{0}\n{1} sürümü yüklendi.\nSephiria {2}", "Sephiria Enhancements {0} yayımlandı (yüklü: {1}). Resmî indirmeler:\nNexus Mods: {2}\nGitHub: {3}",
                "Oyun sürümü", "Geçerli Sephiria sürümü. Sorun bildirirken oyun ve mod sürümlerini belirtin.", "Son denetleme", "Oyun açıldığından beri tamamlanan son denetlemenin yerel saati; başarısız olanlar dahil, iptaller hariç.", "Bağlanılamadı; ağı denetleyin", "İstek zaman aşımına uğradı; tekrar deneyin", "GitHub istek sınırı; sonra deneyin", "GitHub isteği başarısız; sonra deneyin", "Güncelleme verisi geçersiz; sonra deneyin" }
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
