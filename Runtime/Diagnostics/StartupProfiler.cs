using SephiriaEnhancements.Runtime.Inventory;
using System.Diagnostics;

namespace SephiriaEnhancements.Diagnostics
{
    internal static class StartupProfiler
    {
#if SEPHIRIA_ENHANCEMENTS_DEVTOOLS
        private static long startupStartedAt;
        private static bool firstFrameObserved;

        internal static void Begin()
        {
            startupStartedAt = Stopwatch.GetTimestamp();
            firstFrameObserved = false;
            DeveloperLogger.Initialize();
            RecordMilestone("mod_entry");
        }

        internal static void RecordMilestone(string milestone)
        {
            if (startupStartedAt != 0)
            {
                DeveloperLogger.RecordStartupMilestone(milestone,
                    ElapsedMilliseconds(startupStartedAt));
            }
        }

        internal static void ObserveFirstFrame()
        {
            if (firstFrameObserved)
            {
                return;
            }

            firstFrameObserved = true;
            RecordMilestone("first_mod_frame");
        }

        private static float ElapsedMilliseconds(long startedAt)
        {
            return (float)((Stopwatch.GetTimestamp() - startedAt) * 1000d /
                Stopwatch.Frequency);
        }
#else
        internal static void Begin() { }
        internal static void RecordMilestone(string milestone) { }
        internal static void ObserveFirstFrame() { }
#endif
    }
}
