using System.Collections.Generic;

namespace SephiriaEnhancements.MapEnhancements.Core
{
    internal readonly struct MapLabelBounds
    {
        internal readonly float X, Y, Width, Height;
        internal MapLabelBounds(float x, float y, float width, float height)
        { X = x; Y = y; Width = width; Height = height; }
        internal bool Overlaps(MapLabelBounds other) =>
            X < other.X + other.Width && X + Width > other.X &&
            Y < other.Y + other.Height && Y + Height > other.Y;
    }

    internal static class MapLabelLayout
    {
        internal static MapLabelBounds PlaceFocused(float x, float y, float width, float height,
            MapLabelBounds viewport, float clearance = 10)
        {
            width = System.Math.Min(width, viewport.Width);
            height = System.Math.Min(height, viewport.Height);
            float left = System.Math.Max(viewport.X, System.Math.Min(x - width / 2, viewport.X + viewport.Width - width));
            float bottom = y + clearance;
            if (bottom + height > viewport.Y + viewport.Height) bottom = y - clearance - height;
            bottom = System.Math.Max(viewport.Y, System.Math.Min(bottom, viewport.Y + viewport.Height - height));
            return new MapLabelBounds(left, bottom, width, height);
        }

        internal static bool TryPlace(float x, float y, float width, float height,
            MapLabelBounds viewport, List<MapLabelBounds> occupied, out MapLabelBounds result, float clearance = 7)
        {
            // Stable alternatives avoid squeezing text or moving its actual map point.
            for (int side = 0; side < 4; side++)
            {
                float left = side == 2 ? x + clearance : side == 3 ? x - clearance - width : x - width / 2;
                float bottom = side == 0 ? y + clearance : side == 1 ? y - clearance - height : y - height / 2;
                var candidate = new MapLabelBounds(left, bottom, width, height);
                if (left < viewport.X || bottom < viewport.Y ||
                    left + width > viewport.X + viewport.Width ||
                    bottom + height > viewport.Y + viewport.Height) continue;
                bool overlaps = false;
                foreach (var bounds in occupied)
                    if (candidate.Overlaps(bounds)) { overlaps = true; break; }
                if (overlaps) continue;
                occupied.Add(candidate);
                result = candidate;
                return true;
            }
            result = default;
            return false;
        }
    }
}
