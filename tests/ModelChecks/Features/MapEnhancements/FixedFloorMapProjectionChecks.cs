using SephiriaEnhancements.MapEnhancements.Core;

namespace SephiriaEnhancements.ModelChecks.Features.MapEnhancements;

internal static class FixedFloorMapProjectionChecks
{
    internal static void Run()
    {
        FixedFloorMapPoint point = FixedFloorMapProjection.Project(
            worldX: 42f, worldY: 18f,
            floorOriginX: 10f, floorOriginY: -2f,
            mapScale: 3f, mapOffsetX: -8f, mapOffsetY: 5f);
        if (Math.Abs(point.X - 88f) > 0.001f ||
            Math.Abs(point.Y - 65f) > 0.001f)
        {
            throw new InvalidOperationException(
                "fixed-floor map projection must preserve native floor origin, scale and offset");
        }
        foreach (float origin in new[] { -1000f, 0f, 1000f })
        {
            if (!FixedFloorMapProjection.Contains(origin - 28, origin - 45,
                    origin, origin, -28, -45, 29, 35) ||
                !FixedFloorMapProjection.Contains(origin + 29, origin + 35,
                    origin, origin, -28, -45, 29, 35) ||
                FixedFloorMapProjection.Contains(origin + 30, origin,
                    origin, origin, -28, -45, 29, 35))
                throw new InvalidOperationException("Fixed-floor markers must stay inside the displayed floor's world bounds.");
        }
        Console.WriteLine("FixedFloorMapProjection: native origin, scale, offset and floor isolation passed");
        bool[] empty = new bool[9];
        bool[] filled = Enumerable.Repeat(true, 9).ToArray();
        for (int y = 0; y < 3; y++)
            for (int x = 0; x < 3; x++)
                if (FixedFloorMapOutline.IsEdge(empty, 3, 3, x, y) ||
                    FixedFloorMapOutline.IsEdge(filled, 3, 3, x, y))
                    throw new InvalidOperationException("Map sampling bounds must not create a rectangular frame.");
        empty[4] = true;
        if (!FixedFloorMapOutline.IsEdge(empty, 3, 3, 1, 1) ||
            FixedFloorMapOutline.IsEdge(empty, 3, 3, 0, 1))
            throw new InvalidOperationException("Only the occupied side of an obstacle boundary receives ink.");
        filled[0] = false;
        if (FixedFloorMapOutline.IsEdge(filled, 3, 3, 1, 1) ||
            !FixedFloorMapOutline.IsEdge(filled, 3, 3, 1, 0))
            throw new InvalidOperationException("Obstacle interiors and cardinal boundaries must stay distinct.");
    }
}
