using SephiriaEnhancements.MapEnhancements.Core;

namespace SephiriaEnhancements.ModelChecks.Features.MapEnhancements;

internal static class RoomMapIconLayoutChecks
{
    internal static void Run()
    {
        foreach (var (width, height) in new[] { (32f, 32f), (64f, 32f), (32f, 64f), (6f, 40f) })
            foreach (int count in Enumerable.Range(1, 16))
            {
                var icons = Enumerable.Range(0, count)
                    .Select(i => RoomMapIconLayout.Place(width, height, count, i)).ToArray();
                for (int i = 0; i < icons.Length; i++)
                {
                    var a = icons[i];
                    if (a.Size <= 0 || Math.Abs(a.X) + a.Size / 2 > width * .4f + .001f ||
                        Math.Abs(a.Y) + a.Size / 2 > height * .4f + .001f)
                        throw new Exception("Map icons must stay inside room interiors, away from connections.");
                    for (int j = 0; j < i; j++)
                    {
                        var b = icons[j];
                        if (Math.Abs(a.X - b.X) < (a.Size + b.Size) / 2 &&
                            Math.Abs(a.Y - b.Y) < (a.Size + b.Size) / 2)
                            throw new Exception("Room map icons must remain distinct, including more than six icons.");
                    }
                }
            }
        Console.WriteLine("RoomMapIconLayout: 64 room/count combinations stay inside rooms without overlap.");
    }
}
