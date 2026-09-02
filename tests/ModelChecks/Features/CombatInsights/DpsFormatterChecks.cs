using SephiriaEnhancements.Core;

namespace SephiriaEnhancements.ModelChecks.Features.CombatInsights;

internal static class DpsFormatterChecks
{
    internal static void Run()
    {
        if (DpsFormatter.Compact(0f) != "0" ||
            DpsFormatter.Compact(999f) != "999" ||
            DpsFormatter.Compact(999.6f) != "1K" ||
            DpsFormatter.Compact(1200f) != "1.2K" ||
            DpsFormatter.Compact(12800f) != "13K" ||
            DpsFormatter.Compact(999900f) != "1M" ||
            DpsFormatter.Compact(1400000f) != "1.4M" ||
            DpsFormatter.Rate(52000f, 42.8f) != "1.2K" ||
            DpsFormatter.Percent(25f, 100f) != "25%" ||
            DpsFormatter.Percent(1f, 0f) != "0%" ||
            DpsFormatter.Seconds(42.8f) != "42.8s")
            throw new InvalidOperationException("compact DPS formatting failed");
        Console.WriteLine("DpsFormatter: compact width checks passed");
    }
}
