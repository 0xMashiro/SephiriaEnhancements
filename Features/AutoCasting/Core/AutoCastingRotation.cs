using System;

namespace SephiriaEnhancements.AutoCasting
{
    internal sealed class AutoCastingRotation
    {
        internal bool IsPaused { get; private set; }
        internal void TogglePause() => IsPaused = !IsPaused;
        private int nextSlot;
        private double nextAttempt;

        internal int Take(double now, int count, Func<int, bool> isReady,
            Func<int, bool> prepareRequest, double interval)
        {
            if (IsPaused || now < nextAttempt) return -1;
            for (int offset = 0; offset < count; offset++)
            {
                int slot = (nextSlot + offset) % count;
                if (!isReady(slot)) continue;
                // One bounded pass shares an interval, including when no target is available.
                // A rejected slot does not postpone a later ready spell in this pass.
                nextAttempt = now + interval;
                if (!prepareRequest(slot)) continue;
                nextSlot = (slot + 1) % count;
                return slot;
            }
            return -1;
        }

        internal void YieldToManualInput(double now) => nextAttempt = Math.Max(nextAttempt, now + 0.35);
        internal void Reset() { nextSlot = 0; nextAttempt = 0; IsPaused = false; }
    }
}
