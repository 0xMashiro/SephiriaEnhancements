using System;
using System.Globalization;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace SephiriaEnhancements.Diagnostics
{
    internal static class SupportLogger
    {
        private static SupportLog file;

        internal static void Initialize()
        {
            Shutdown();
            try
            {
                string version = typeof(SupportLogger).Assembly
                    .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
                string header = DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture) +
                    " INFO mod_started version=" + version + " game=" + Application.version +
                    " build=" + BuildIdentity.Flavor;
                file = new SupportLog(Path.Combine(SaveData.CommonPath, "Mods",
                    "SephiriaEnhancements", "Logs", "Support", "support.log"),
                    header);
            }
            catch (Exception ex) { Disable(ex); }
        }

        // Codes and details are deliberately supplied by the Mod: never copy arbitrary
        // Unity messages, exception messages, player names, paths or inventory contents here.
        internal static void Record(string code, string details = "", string level = "INFO")
        {
            if (file == null) return;
            try
            {
                file.Record(code, details, level, DateTime.UtcNow);
            }
            catch (Exception ex) { Disable(ex); }
        }

        internal static void Failure(string code, Exception exception)
        {
            Record(code, "exception=" + (exception?.GetType().FullName ?? "unknown"), "ERROR");
        }

        internal static void Info(string code, object message)
        {
            Record(code);
            Debug.Log(message);
        }

        internal static void Warning(string code, object message)
        {
            Record(code, level: "WARN");
            Debug.LogWarning(message);
        }

        internal static void Error(string code, object message)
        {
            Record(code, level: "ERROR");
            Debug.LogError(message);
        }

        internal static void Shutdown()
        {
            if (file == null) return;
            Record("mod_stopped");
            SupportLog previous = file;
            file = null;
            try { previous?.Dispose(); }
            catch (Exception ex) { Debug.LogWarning("[SephiriaEnhancements] Support log close failed: " + ex.GetType().Name); }
        }

        private static void Disable(Exception exception)
        {
            SupportLog previous = file;
            file = null;
            try { previous?.Dispose(); }
            catch (Exception) { /* Logging failure must not interrupt gameplay. */ }
            Debug.LogWarning("[SephiriaEnhancements] Support log unavailable: " + exception.GetType().Name);
        }
    }
}
