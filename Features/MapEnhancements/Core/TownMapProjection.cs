namespace SephiriaEnhancements.MapEnhancements.Core
{
    internal readonly struct TownMapPoint
    {
        internal TownMapPoint(float x, float y)
        {
            X = x;
            Y = y;
        }

        internal float X { get; }

        internal float Y { get; }
    }

    internal static class TownMapProjection
    {
        internal static TownMapPoint Project(float worldX, float worldY,
            float floorOriginX, float floorOriginY, float mapScale,
            float mapOffsetX, float mapOffsetY) =>
            new TownMapPoint(
                (worldX - floorOriginX) * mapScale + mapOffsetX,
                (worldY - floorOriginY) * mapScale + mapOffsetY);
    }
}
