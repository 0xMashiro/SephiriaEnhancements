using System;

namespace SephiriaEnhancements.AutoCasting
{
    internal sealed class AutoCastingRotation
    {
        private int nextSlot;
        private double nextRequest;

        internal int Take(double now, int count, Func<int, bool> canRequest, double interval)
        {
            if (now < nextRequest) return -1;
            for (int offset = 0; offset < count; offset++)
            {
                int slot = (nextSlot + offset) % count;
                if (!canRequest(slot)) continue;
                nextSlot = (slot + 1) % count;
                nextRequest = now + interval;
                return slot;
            }
            return -1;
        }

        internal void YieldToManualInput(double now) => nextRequest = now + 0.35;
        internal void Reset() { nextSlot = 0; nextRequest = 0; }
    }
}
