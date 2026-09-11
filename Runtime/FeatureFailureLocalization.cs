using System;
using System.Collections.Generic;
using SephiriaEnhancements.Configuration;

namespace SephiriaEnhancements.Runtime
{
    internal static class FeatureFailureLocalization
    {
        internal const string Message = "SephiriaEnhancements.FeatureFailure.Message";
        private static readonly string[] Keys = { Message };
        private static readonly Dictionary<string, string[]> Texts = new()
        {
            ["en-US"] = new[] { "Some Mod features were disabled after an error. See the Mod log for details." },
            ["zh-CN"] = new[] { "部分 Mod 功能因错误已停用。详情请查看 Mod 日志。" },
            ["zh-TW"] = new[] { "部分 Mod 功能因錯誤已停用。詳情請查看 Mod 日誌。" },
            ["ja-JP"] = new[] { "エラーのため一部の Mod 機能を無効にしました。詳細は Mod ログをご確認ください。" },
            ["ko-KR"] = new[] { "오류로 일부 Mod 기능이 비활성화되었습니다. 자세한 내용은 Mod 로그를 확인하세요." },
            ["de-DE"] = new[] { "Einige Mod-Funktionen wurden nach einem Fehler deaktiviert. Details stehen im Mod-Protokoll." },
            ["es-ES"] = new[] { "Se desactivaron algunas funciones del Mod tras un error. Consulta el registro del Mod." },
            ["fr-FR"] = new[] { "Certaines fonctions du Mod ont été désactivées après une erreur. Consultez le journal du Mod." },
            ["it-IT"] = new[] { "Alcune funzioni del Mod sono state disattivate dopo un errore. Consulta il registro del Mod." },
            ["pl-PL"] = new[] { "Niektóre funkcje Modu wyłączono po błędzie. Szczegóły znajdują się w dzienniku Modu." },
            ["pt-BR"] = new[] { "Algumas funções do Mod foram desativadas após um erro. Consulte o registro do Mod." },
            ["ru-RU"] = new[] { "Некоторые функции мода отключены из-за ошибки. Подробности — в журнале мода." },
            ["sv-SE"] = new[] { "Vissa modfunktioner har inaktiverats efter ett fel. Se moddens logg för mer information." },
            ["th-TH"] = new[] { "ปิดใช้งานฟังก์ชันบางส่วนของ Mod เนื่องจากข้อผิดพลาด ดูรายละเอียดในบันทึก Mod" },
            ["tr-TR"] = new[] { "Bir hata nedeniyle bazı Mod özellikleri devre dışı bırakıldı. Ayrıntılar için Mod günlüğüne bakın." }
        };

        internal static void Register(Action<string, string, string> addText, IEnumerable<string> languages) =>
            LocalizationGroup.Register(addText, languages, Keys, Texts);
    }
}
