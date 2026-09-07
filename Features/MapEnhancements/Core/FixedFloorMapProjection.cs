namespace SephiriaEnhancements.MapEnhancements.Core
{
    internal readonly struct FixedFloorMapPoint
    {
        internal FixedFloorMapPoint(float x, float y)
        {
            X = x;
            Y = y;
        }

        internal float X { get; }

        internal float Y { get; }
    }

    internal static class FixedFloorMapProjection
    {
        internal static bool Contains(float worldX, float worldY,
            float floorOriginX, float floorOriginY, float left, float bottom,
            float right, float top) =>
            worldX - floorOriginX >= left && worldX - floorOriginX <= right &&
            worldY - floorOriginY >= bottom && worldY - floorOriginY <= top;

        internal static FixedFloorMapPoint Project(float worldX, float worldY,
            float floorOriginX, float floorOriginY, float mapScale,
            float mapOffsetX, float mapOffsetY) =>
            new FixedFloorMapPoint(
                (worldX - floorOriginX) * mapScale + mapOffsetX,
                (worldY - floorOriginY) * mapScale + mapOffsetY);
    }
}
