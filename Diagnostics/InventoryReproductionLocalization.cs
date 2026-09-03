using System;
using System.Collections.Generic;
using SephiriaEnhancements.Configuration;

namespace SephiriaEnhancements.Diagnostics
{
    internal static class InventoryReproductionLocalization
    {
        private const string Prefix = "SephiriaEnhancements.InventoryReproduction.";
        internal const string Setting = Prefix + "Setting";
        internal const string Help = Prefix + "Help";
        internal const string Off = Prefix + "Off";
        internal const string On = Prefix + "On";
        internal const string Capture = Prefix + "Capture";
        internal const string Queued = Prefix + "Queued";
        internal const string Unavailable = Prefix + "Unavailable";
        internal const string WriteFailed = Prefix + "WriteFailed";

        internal static void Register(Action<string, string, string> addText)
        {
            foreach (string language in LocalizationLanguages.All)
                foreach (var entry in ForLanguage(language)) addText(language, entry.Key, entry.Value);
        }

        private static Dictionary<string, string> ForLanguage(string language) => language switch
        {
            "zh-CN" => new Dictionary<string, string>
            {
                [Setting] = "背包案例采集（开发版）",
                [Help] = "开启后记录每次完成的求解及应用校验，包括成功结果；从下一次求解生效。关闭时仍自动记录异常与未达成目标。可在控制设置绑定“保存当前背包案例”，不整理背包也能采集。日志只保存在本机，会轮换覆盖。",
                [Off] = "仅异常与困难案例",
                [On] = "全部完成结果",
                [Capture] = "保存当前背包案例（开发版）",
                [Queued] = "背包案例已排队记录。",
                [Unavailable] = "请打开背包，完成物品移动或整理，并等待结算后再采集。",
                [WriteFailed] = "背包案例记录失败或部分记录丢失，请检查反馈日志。"
            },
            "zh-TW" => Create("背包案例採集（開發版）", "開啟後從下一次求解起記錄全部完成結果及套用驗證，包括成功結果。關閉時仍記錄異常與未達成目標。可在控制設定綁定手動儲存案例，無須整理背包。日誌僅存於本機，會輪替覆寫。",
                "僅異常與困難案例", "全部完成結果", "儲存目前背包案例（開發版）", "背包案例已排入記錄佇列。",
                "請開啟背包，完成物品移動或整理，並等待結算後再採集。", "背包案例記錄失敗或部分記錄遺失，請檢查回饋日誌。"),
            "ja-JP" => Create("インベントリ事例収集（開発版）", "有効にすると、次の探索から成功を含む完了結果と配置後の検証を記録します。無効時も異常と未達成目標は記録します。操作設定で事例保存キーを割り当てると、整理せずに保存できます。ログは端末内に保存され、順次上書きされます。",
                "異常と難しい事例のみ", "すべての完了結果", "現在のインベントリ事例を保存（開発版）", "インベントリ事例を記録待ちに追加しました。",
                "インベントリを開き、移動や整理を終え、計算の完了を待ってから収集してください。", "事例の記録に失敗したか、一部の記録が失われました。サポートログを確認してください。"),
            "ko-KR" => Create("인벤토리 사례 수집 (개발판)", "켜면 다음 탐색부터 성공을 포함한 모든 완료 결과와 배치 적용 검증을 기록합니다. 꺼도 오류와 미달성 목표는 기록합니다. 조작 설정에서 사례 저장 키를 지정하면 정리 없이 수집할 수 있습니다. 로그는 이 기기에 저장되며 순환 덮어쓰기됩니다.",
                "오류와 어려운 사례만", "모든 완료 결과", "현재 인벤토리 사례 저장 (개발판)", "인벤토리 사례가 기록 대기열에 추가되었습니다.",
                "인벤토리를 열고 아이템 이동이나 정리를 마친 뒤 계산이 완료되면 수집하세요.", "사례 기록에 실패했거나 일부 기록이 누락되었습니다. 지원 로그를 확인하세요."),
            "de-DE" => Create("Inventarfälle erfassen (Entwicklung)", "Ab der nächsten Suche alle abgeschlossenen Ergebnisse und Anwendungsprüfungen einschließlich Erfolgen aufzeichnen. Ausgeschaltet werden weiterhin Fehler und unerfüllte Ziele erfasst. In der Steuerung eine Taste zum Speichern ohne Sortieren zuweisen. Protokolle bleiben lokal und werden rotierend überschrieben.",
                "Nur Fehler und schwierige Fälle", "Alle abgeschlossenen Ergebnisse", "Aktuellen Inventarfall speichern (Entwicklung)", "Inventarfall zur Aufzeichnung eingereiht.",
                "Inventar öffnen, Verschieben oder Sortieren abschließen und die Berechnung abwarten.", "Aufzeichnung fehlgeschlagen oder Einträge verloren. Bitte das Supportprotokoll prüfen."),
            "es-ES" => Create("Captura de casos de inventario (desarrollo)", "Desde la próxima búsqueda, registra todos los resultados completados y las comprobaciones de aplicación, incluidos los éxitos. Desactivado, sigue registrando anomalías y objetivos incumplidos. Asigna un control para guardar casos sin ordenar. Los registros son locales y se sobrescriben por rotación.",
                "Solo anomalías y casos difíciles", "Todos los resultados completados", "Guardar caso de inventario actual (desarrollo)", "Caso de inventario en cola para registrarse.",
                "Abre el inventario, termina de mover u ordenar objetos y espera a que se complete el cálculo.", "Falló el registro o se perdieron entradas. Consulta el registro de soporte."),
            "fr-FR" => Create("Capture de cas d’inventaire (développement)", "Dès la prochaine recherche, enregistre tous les résultats terminés et les vérifications d’application, réussites comprises. Désactivé, conserve les anomalies et les objectifs non atteints. Attribuez une commande pour capturer sans ranger. Les journaux restent locaux et sont remplacés par rotation.",
                "Anomalies et cas difficiles", "Tous les résultats terminés", "Enregistrer le cas d’inventaire actuel (développement)", "Cas d’inventaire en attente d’enregistrement.",
                "Ouvrez l’inventaire, terminez le déplacement ou le rangement et attendez la fin du calcul.", "Échec de l’enregistrement ou perte d’entrées. Consultez le journal d’assistance."),
            "it-IT" => Create("Acquisizione casi inventario (sviluppo)", "Dalla prossima ricerca registra tutti i risultati completati e le verifiche di applicazione, inclusi i successi. Se disattivato, registra comunque anomalie e obiettivi non raggiunti. Assegna un comando per salvare senza riordinare. I registri restano locali e vengono sovrascritti a rotazione.",
                "Solo anomalie e casi difficili", "Tutti i risultati completati", "Salva il caso inventario attuale (sviluppo)", "Caso inventario in coda per la registrazione.",
                "Apri l’inventario, termina gli spostamenti o il riordino e attendi il completamento del calcolo.", "Registrazione fallita o alcune voci perse. Controlla il registro di supporto."),
            "pl-PL" => Create("Zbieranie przypadków ekwipunku (wersja deweloperska)", "Od następnego wyszukiwania zapisuje wszystkie ukończone wyniki i weryfikacje zastosowania, także udane. Po wyłączeniu nadal zapisuje anomalie i niespełnione cele. Przypisz w sterowaniu zapis przypadku bez porządkowania. Lokalne dzienniki są cyklicznie nadpisywane.",
                "Tylko anomalie i trudne przypadki", "Wszystkie ukończone wyniki", "Zapisz bieżący przypadek ekwipunku (wersja deweloperska)", "Przypadek ekwipunku dodano do kolejki zapisu.",
                "Otwórz ekwipunek, zakończ przenoszenie lub porządkowanie i poczekaj na obliczenia.", "Zapis nie powiódł się lub utracono wpisy. Sprawdź dziennik pomocy."),
            "pt-BR" => Create("Captura de casos de inventário (desenvolvimento)", "A partir da próxima busca, registra todos os resultados concluídos e verificações de aplicação, incluindo sucessos. Desativado, ainda registra anomalias e metas não atingidas. Atribua um comando para salvar sem organizar. Os registros ficam locais e são sobrescritos por rotação.",
                "Apenas anomalias e casos difíceis", "Todos os resultados concluídos", "Salvar caso do inventário atual (desenvolvimento)", "Caso do inventário adicionado à fila de registro.",
                "Abra o inventário, termine de mover ou organizar itens e aguarde o cálculo.", "Falha no registro ou perda de entradas. Consulte o registro de suporte."),
            "ru-RU" => Create("Запись примеров инвентаря (для разработки)", "Со следующего поиска записывает все завершённые результаты и проверки применения, включая успешные. При отключении ошибки и недостигнутые цели всё равно записываются. Назначьте в управлении сохранение примера без сортировки. Журналы хранятся локально и циклически перезаписываются.",
                "Только ошибки и сложные примеры", "Все завершённые результаты", "Сохранить текущий пример инвентаря (для разработки)", "Пример инвентаря добавлен в очередь записи.",
                "Откройте инвентарь, завершите перемещение или сортировку и дождитесь расчёта.", "Запись не удалась или часть записей потеряна. Проверьте журнал поддержки."),
            "sv-SE" => Create("Samla inventariefall (utveckling)", "Från nästa sökning sparas alla slutförda resultat och tillämpningskontroller, även lyckade. Avstängt sparas fortfarande avvikelser och ouppnådda mål. Tilldela en knapp för att spara utan att sortera. Loggarna lagras lokalt och skrivs över i rotation.",
                "Endast avvikelser och svåra fall", "Alla slutförda resultat", "Spara aktuellt inventariefall (utveckling)", "Inventariefallet har köats för loggning.",
                "Öppna inventariet, avsluta flyttning eller sortering och vänta tills beräkningen är klar.", "Loggningen misslyckades eller poster gick förlorade. Kontrollera supportloggen."),
            "th-TH" => Create("เก็บกรณีช่องเก็บของ (รุ่นพัฒนา)", "ตั้งแต่การค้นหาครั้งถัดไป จะบันทึกผลที่เสร็จสิ้นและการตรวจสอบการจัดวางทั้งหมด รวมถึงผลสำเร็จ เมื่อปิดยังบันทึกความผิดปกติและเป้าหมายที่ไม่สำเร็จ ตั้งปุ่มบันทึกกรณีในหน้าควบคุมเพื่อเก็บข้อมูลโดยไม่จัดของ บันทึกอยู่ในเครื่องและจะถูกเขียนทับแบบหมุนเวียน",
                "เฉพาะความผิดปกติและกรณียาก", "ผลที่เสร็จสิ้นทั้งหมด", "บันทึกกรณีช่องเก็บของปัจจุบัน (รุ่นพัฒนา)", "เพิ่มกรณีช่องเก็บของลงในคิวบันทึกแล้ว",
                "เปิดช่องเก็บของ ย้ายหรือจัดของให้เสร็จ และรอการคำนวณก่อนเก็บข้อมูล", "บันทึกไม่สำเร็จหรือข้อมูลบางส่วนสูญหาย โปรดตรวจสอบบันทึกการสนับสนุน"),
            "tr-TR" => Create("Envanter örneği toplama (geliştirme)", "Sonraki aramadan itibaren başarılı olanlar dahil tüm tamamlanan sonuçları ve uygulama kontrollerini kaydeder. Kapalıyken de hatalar ve karşılanmayan hedefler kaydedilir. Düzenlemeden kaydetmek için kontrollerden bir tuş atayın. Günlükler yerelde kalır ve dönüşümlü olarak üzerlerine yazılır.",
                "Yalnızca hatalar ve zor örnekler", "Tüm tamamlanan sonuçlar", "Mevcut envanter örneğini kaydet (geliştirme)", "Envanter örneği kayıt kuyruğuna eklendi.",
                "Envanteri açın, taşıma veya düzenlemeyi bitirin ve hesaplamanın tamamlanmasını bekleyin.", "Kayıt başarısız oldu veya bazı kayıtlar kayboldu. Destek günlüğünü kontrol edin."),
            _ => new Dictionary<string, string>
            {
                [Setting] = "Inventory case capture (development)",
                [Help] = "Record every completed search and application check, including successes, starting with the next search. When off, anomalies and unmet goals are still recorded. Bind Save current inventory case in controls to capture without arranging. Logs stay local and rotate.",
                [Off] = "Anomalies and difficult cases",
                [On] = "All completed results",
                [Capture] = "Save current inventory case (development)",
                [Queued] = "Inventory case queued for recording.",
                [Unavailable] = "Open the inventory, finish moving or arranging items, and wait for settlement before capturing.",
                [WriteFailed] = "Inventory recording failed or some records were dropped. Check the support log."
            }
        };

        private static Dictionary<string, string> Create(string setting, string help, string off, string on,
            string capture, string queued, string unavailable, string writeFailed) => new Dictionary<string, string>
            {
                [Setting] = setting,
                [Help] = help,
                [Off] = off,
                [On] = on,
                [Capture] = capture,
                [Queued] = queued,
                [Unavailable] = unavailable,
                [WriteFailed] = writeFailed
            };
    }
}
