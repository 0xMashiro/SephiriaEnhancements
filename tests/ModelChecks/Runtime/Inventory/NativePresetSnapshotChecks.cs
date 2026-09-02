using SephiriaEnhancements.Runtime.Inventory;

namespace SephiriaEnhancements.ModelChecks.Runtime.Inventory;

internal static class NativePresetSnapshotChecks
{
    internal static void Run()
    {
        var presetSnapshot = new NativePresetSnapshot(2, true, "Fire", 7, "Scholar",
            new[] { 101 }, new[] { "EMBER" });
        var matchingPreset = new NativePresetSnapshot(2, true, "Fire", 7, "Scholar",
            new[] { 101 }, new[] { "EMBER" });
        var changedPreset = new NativePresetSnapshot(2, true, "Fire", 7, "Scholar",
            new[] { 102 }, new[] { "EMBER" });
        if (!presetSnapshot.ContentEquals(matchingPreset) ||
            presetSnapshot.ContentEquals(changedPreset) || presetSnapshot.ContentEquals(null))
            throw new InvalidOperationException("native preset semantic equality failed");
        Console.WriteLine("BuildIntentSnapshot: native preset remains a soft preference projection");
    }
}
