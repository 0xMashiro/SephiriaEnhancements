using System.Text;
using System.Text.Json;
using SephiriaEnhancements.Diagnostics;
using SephiriaEnhancements.Integration;
using SephiriaEnhancements.Runtime.GameBridge.Inventory;

namespace SephiriaEnhancements.ModelChecks.Runtime.Diagnostics;

internal static class LoggingChecks
{
    internal static void Run()
    {
        string directory = Path.Combine(Path.GetTempPath(), "sephiria-log-check-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            VerifyRotation(directory);
            VerifyOversizedRecords(directory);
            VerifyRepeatedSupportEvents(directory);
            VerifyNativeReadFailureDetails(directory);
            VerifyLoaderFailureDetails();
            VerifyNativeBindings();
            VerifyIoFailure(directory);
        }
        finally { Directory.Delete(directory, recursive: true); }
        Console.WriteLine("Logging: byte bounds, retention, continued recording, repeated failures and I/O boundaries passed");
    }

    private static void VerifyRotation(string directory)
    {
        string path = Path.Combine(directory, "diagnostics.jsonl");
        const string header = "{\"event\":\"log_start\",\"build\":\"Development\"}";
        using (var log = new RollingLogFile(path, 4096, 4, header))
        {
            for (int index = 0; index <= 50100; index++)
                log.WriteLine("{\"event\":\"sample\",\"index\":" + index + ",\"text\":\"火\"}");
        }
        string[] files = Directory.GetFiles(directory, "diagnostics*.jsonl");
        Require(files.Length == 4, "retention includes the active file");
        foreach (string file in files)
        {
            Require(new FileInfo(file).Length <= 4096, "UTF-8 bytes must remain bounded");
            string[] lines = File.ReadAllLines(file);
            Require(lines[0] == header, "each rotated file retains build context");
            foreach (string line in lines) using (JsonDocument.Parse(line)) { }
        }
        Require(File.ReadAllText(path).Contains("\"index\":50100"), "recording must continue beyond 50000 events");
        File.WriteAllText(Path.Combine(directory, "unrelated.jsonl"), "keep");
        using (var restarted = new RollingLogFile(path, 4096, 4, header))
            restarted.WriteLine("{\"event\":\"restarted\"}");
        Require(File.ReadAllText(Path.Combine(directory, "diagnostics.1.jsonl")).Contains("\"index\":50100"),
            "opening a new log must retain the previous run");
        Require(File.ReadAllText(Path.Combine(directory, "unrelated.jsonl")) == "keep", "rotation owns only its named files");
    }

    private static void VerifyOversizedRecords(string directory)
    {
        string path = Path.Combine(directory, "oversized.jsonl");
        using (var log = new RollingLogFile(path, 256, 2, "{\"event\":\"log_start\"}"))
        {
            log.WriteLine("{\"data\":\"" + new string('火', 1000) + "\"}");
            log.WriteLine("{\"event\":\"after_oversized\"}");
        }
        string[] lines = File.ReadAllLines(path);
        Require(lines.Any(line => line.Contains("log_record_omitted")), "oversized payloads must leave an explicit marker");
        Require(lines.Last().Contains("after_oversized"), "oversized records must not stop later events");
        Require(Encoding.UTF8.GetByteCount(string.Join("\n", lines) + "\n") <= 256, "record omission respects byte bounds");
        foreach (string line in lines) using (JsonDocument.Parse(line)) { }
    }

    private static void VerifyRepeatedSupportEvents(string directory)
    {
        string path = Path.Combine(directory, "support.log");
        DateTime now = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        using (var log = new SupportLog(path, "build=Release"))
        {
            log.Record("inventory_capture_failed", "exception=InvalidOperationException", "WARN", now);
            for (int index = 0; index < 100; index++)
                log.Record("inventory_capture_failed", "exception=InvalidOperationException", "WARN", now.AddSeconds(1));
            log.Record("inventory_capture_failed", "exception=InvalidOperationException", "WARN", now.AddSeconds(31));
            log.Record("inventory_capture_failed", "exception=InvalidOperationException", "WARN", now.AddSeconds(32));
            log.Record("inventory_capture_failed", "exception=IOException", "WARN", now.AddSeconds(33));
            log.Record("inventory_message", "code=Completed\nextra", "INFO", now.AddSeconds(34));
            log.Record("inventory_message", "code=Completed\nextra", "INFO", now.AddSeconds(35));
        }
        string[] lines = File.ReadAllLines(path);
        Require(lines.Length == 7, "repeated events must be summarized on interval, change and close");
        Require(lines[2].EndsWith("repeated=101"), "continuous repeats remain observable every 30 seconds");
        Require(lines[3].EndsWith("repeated=1"), "changing failure details flushes the previous count");
        Require(lines[4].Contains("exception=IOException"), "different failure details must not be merged");
        Require(lines[5].EndsWith("code=Completed extra"), "each support event occupies one line");
        Require(lines[6].EndsWith("repeated=1"), "shutdown flushes the final repeated event");
    }

    private static void VerifyNativeReadFailureDetails(string directory)
    {
        string path = Path.Combine(directory, "native-reads.log");
        DateTime now = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        using (var log = new SupportLog(path, "build=Development"))
        {
            foreach (string operation in new[] { "unique_pair_combo", "unique_pair_combo", "tablet_queries" })
            {
                try
                {
                    NativeInventoryRead.Required<int>(operation, () => throw new InvalidOperationException("PRIVATE_DETAIL"));
                }
                catch (Exception exception)
                {
                    log.Record("inventory_capture_failed", NativeInventoryRead.FailureDetails(exception), "ERROR", now);
                }
            }
        }
        string[] lines = File.ReadAllLines(path);
        Require(lines.Length == 4 && lines[2].EndsWith("repeated=1"),
            "repeated read failures must be summarized without merging different operations");
        Require(lines[1].Contains("operation=unique_pair_combo") && lines[3].Contains("operation=tablet_queries") &&
            lines[3].Contains("exception=System.InvalidOperationException"), "support logs lost the failed native operation");
        Require(!string.Join("\n", lines).Contains("PRIVATE_DETAIL"), "support logs copied exception messages");
    }

    private static void VerifyLoaderFailureDetails()
    {
        try
        {
            throw new TypeInitializationException("PRIVATE_DETAIL", new MissingMethodException("PRIVATE_DETAIL"));
        }
        catch (Exception exception)
        {
            string details = SupportFailureDetails.Format(exception);
            Require(details.Contains("cause=System.MissingMethodException"), "retain the loader failure cause");
            Require(details.Contains("System.TypeInitializationException > System.MissingMethodException"),
                "retain the exception chain");
            Require(details.Contains(nameof(VerifyLoaderFailureDetails)), "retain wrapper frames when the cause has no stack");
            Require(!details.Contains("PRIVATE_DETAIL"), "do not copy exception messages or unverified type names");
        }
        Require(SupportFailureDetails.Format(null!).Contains("cause=unknown"), "absent failures have no invented cause");
    }

    private static void VerifyNativeBindings()
    {
        var instance = new BindingFixture();
        var read = NativeBinding.Method<Func<BindingFixture, int>>(typeof(BindingFixture), "Read");
        var write = NativeBinding.Method<Action<BindingFixture, int>>(typeof(BindingFixture), "set_Value");
        write(instance, 7);
        Require(read(instance) == 7, "private method and property bindings preserve their receiver");
        var field = NativeBinding.Field<BindingFixture, int>("value");
        field(instance) = 9;
        Require(read(instance) == 9, "field bindings access the specified instance");

        foreach (Action bind in new Action[] {
            () => NativeBinding.Method<Func<BindingFixture, int>>(typeof(BindingFixture), "Missing"),
            () => NativeBinding.Field<BindingFixture, int>("Missing"),
            () => NativeBinding.Method<Action<BindingFixture, string>>(typeof(BindingFixture), "Read") })
        {
            try { bind(); throw new InvalidOperationException("A broken binding must fail."); }
            catch (NativeBindingException exception)
            {
                var wrapped = new TypeInitializationException("PRIVATE_DETAIL", exception);
                string details = SupportFailureDetails.Format(wrapped);
                Require(exception.Owner == typeof(BindingFixture) && exception.InnerException != null,
                    "binding failure retains owner and cause");
                Require(details.Contains("binding=" + typeof(BindingFixture).FullName + "." + exception.MemberName),
                    "the exact failed member survives type initialization wrapping");
                Require(!details.Contains("PRIVATE_DETAIL"), "binding details exclude arbitrary wrapper text");
            }
        }
    }

    private sealed class BindingFixture
    {
        private int value;
        private int Value { set => this.value = value; }
        private int Read() => value;
    }

    private static void VerifyIoFailure(string directory)
    {
        string blocked = Path.Combine(directory, "blocked");
        File.WriteAllText(blocked, "file");
        try
        {
            using var log = new RollingLogFile(Path.Combine(blocked, "support.log"), 256, 2, "header");
            throw new InvalidOperationException("An unwritable directory must report failure to the logging boundary.");
        }
        catch (IOException) { }
    }

    private static void Require(bool condition, string reason)
    {
        if (!condition) throw new InvalidOperationException(reason);
    }
}
