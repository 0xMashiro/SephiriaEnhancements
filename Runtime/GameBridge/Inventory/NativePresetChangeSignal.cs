#nullable disable
using SephiriaEnhancements.Runtime.Inventory;

using System.Threading;
using System;
namespace SephiriaEnhancements.Runtime.GameBridge.Inventory
{
    internal static class NativePresetChangeSignal
    {
        private static long revision;

        internal static long Revision => Interlocked.Read(ref revision);

        internal static void MarkChanged()
        {
            Interlocked.Increment(ref revision);
        }

        internal static void ObserveCurrentSetupWrite(string key)
        {
            if (key != null && (key.StartsWith("Item_Favorite_", StringComparison.Ordinal) ||
                key.StartsWith("FruitSkewer_", StringComparison.Ordinal) || key == "Preset_SelectedSlot"))
                MarkChanged();
        }
    }
}
