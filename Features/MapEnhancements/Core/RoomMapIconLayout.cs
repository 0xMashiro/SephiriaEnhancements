using System;

namespace SephiriaEnhancements.MapEnhancements.Core
{
    internal static class RoomMapIconLayout
    {
        internal static (float X, float Y, float Size) Place(float width, float height,
            int count, int index)
        {
            if (width <= 0 || height <= 0 || count <= 0 || index < 0 || index >= count)
                return (0, 0, 0);
            int columns = Math.Min(count, Math.Max(1,
                (int)Math.Ceiling(Math.Sqrt(count * width / height))));
            int rows = (count + columns - 1) / columns;
            float cellWidth = Math.Min(14f, width * .8f / columns);
            float cellHeight = Math.Min(14f, height * .8f / rows);
            int row = index / columns;
            int rowCount = Math.Min(columns, count - row * columns);
            return ((index % columns - (rowCount - 1) * .5f) * cellWidth,
                ((rows - 1) * .5f - row) * cellHeight,
                Math.Min(12f, Math.Min(cellWidth, cellHeight) * .85f));
        }
    }
}
