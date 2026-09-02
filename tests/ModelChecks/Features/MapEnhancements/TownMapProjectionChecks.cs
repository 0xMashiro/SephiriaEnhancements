using SephiriaEnhancements.MapEnhancements.Core;

namespace SephiriaEnhancements.ModelChecks.Features.MapEnhancements;

internal static class TownMapProjectionChecks
{
    internal static void Run()
    {
        TownMapPoint townMapPoint = TownMapProjection.Project(
            worldX: 42f, worldY: 18f,
            floorOriginX: 10f, floorOriginY: -2f,
            mapScale: 3f, mapOffsetX: -8f, mapOffsetY: 5f);
        if (Math.Abs(townMapPoint.X - 88f) > 0.001f ||
            Math.Abs(townMapPoint.Y - 65f) > 0.001f)
        {
            throw new InvalidOperationException(
                "town NPC map projection must preserve native floor origin, scale and offset");
        }
        Console.WriteLine("TownMapProjection: native town-map coordinate mapping passed");
    }
}
