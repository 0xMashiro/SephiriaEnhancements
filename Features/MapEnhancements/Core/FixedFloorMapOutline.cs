namespace SephiriaEnhancements.MapEnhancements.Core
{
    internal static class FixedFloorMapOutline
    {
        // Only draw observed obstacle boundaries; the sampled rectangle is not a wall.
        internal static bool IsEdge(bool[] blocked, int width, int height, int x, int y) =>
            blocked[y * width + x] &&
            ((x > 0 && !blocked[y * width + x - 1]) ||
             (x + 1 < width && !blocked[y * width + x + 1]) ||
             (y > 0 && !blocked[(y - 1) * width + x]) ||
             (y + 1 < height && !blocked[(y + 1) * width + x]));
    }
}
