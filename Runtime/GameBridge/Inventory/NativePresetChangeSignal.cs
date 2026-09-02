#nullable disable
using SephiriaEnhancements.Runtime.Inventory;

using System.Threading;
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
    }
}
