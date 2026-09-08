using System;

namespace SephiriaEnhancements.Inventory
{
    internal static class InventoryPickupPlacement
    {
        internal static (float X, float Y) BesideSelection(float left, float right, float centerY,
            float width, float height, float screenLeft, float screenRight, float screenBottom, float screenTop)
        {
            const float gap = 6f;
            float x = right + gap + width * 0.5f;
            if (x + width * 0.5f > screenRight) x = left - gap - width * 0.5f;
            return (Math.Clamp(x, screenLeft + width * 0.5f, screenRight - width * 0.5f),
                Math.Clamp(centerY, screenBottom + height * 0.5f, screenTop - height * 0.5f));
        }
    }
}
