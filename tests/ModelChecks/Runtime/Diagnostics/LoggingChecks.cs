using System.Text;
using System.Text.Json;
using SephiriaEnhancements.Diagnostics;

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
