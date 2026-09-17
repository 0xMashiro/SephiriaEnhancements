using System;

namespace SephiriaEnhancements.Inventory
{
    internal static class InventoryRecoveryListLayout
    {
        internal const float RowHeight = 44, ViewportHeight = 176;

        internal static float Reveal(int index, int count, float offset)
        {
            if (count == 0) return 0;
            float top = Math.Max(0, Math.Min(index, count - 1)) * RowHeight;
            float visible = top < offset ? top : top + RowHeight > offset + ViewportHeight
                ? top + RowHeight - ViewportHeight : offset;
            return Math.Max(0, Math.Min(visible, Math.Max(0, count * RowHeight - ViewportHeight)));
        }
    }
}
