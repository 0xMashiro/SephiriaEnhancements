using SephiriaEnhancements.MapEnhancements.Core;
using SephiriaEnhancements.MapEnhancements;

namespace SephiriaEnhancements.ModelChecks.Features.MapEnhancements;

internal static class MapLabelLayoutChecks
{
    internal static void Run()
    {
        foreach (bool hasRooms in new[] { true, false })
        {
            var initial = MapNavigationModes.Initial(hasRooms);
            if (initial != (hasRooms ? MapNavigationMode.Rooms : MapNavigationMode.People))
                throw new InvalidOperationException("Opening a room map must prioritize room travel.");
            var mode = initial;
            for (int i = 0; i < (hasRooms ? 3 : 2); i++)
            {
                var next = MapNavigationModes.Cycle(mode, 1, hasRooms);
                if (!hasRooms && next == MapNavigationMode.Rooms || MapNavigationModes.Cycle(next, -1, hasRooms) != mode)
                    throw new InvalidOperationException("Mode cycling must be reversible and skip unavailable room mode.");
                mode = next;
            }
            if (mode != initial) throw new InvalidOperationException("Mode cycling must wrap.");
        }
        var viewport = new MapLabelBounds(-100, -80, 200, 160);
        var occupied = new List<MapLabelBounds>();
        for (int i = 0; i < 30; i++)
        {
            if (!MapLabelLayout.TryPlace(i % 3 * 10, i / 3 * 3, 40, 13,
                    viewport, occupied, out var label)) continue;
            if (label.X < -100 || label.Y < -80 || label.X + label.Width > 100 || label.Y + label.Height > 80)
                throw new InvalidOperationException("Map labels must stay inside the viewport.");
            for (int j = 0; j < occupied.Count - 1; j++)
                if (label.Overlaps(occupied[j])) throw new InvalidOperationException("Dense map labels must not overlap.");
        }
        if (occupied.Count == 0 || occupied.Count >= 30)
            throw new InvalidOperationException("Dense targets require a readable subset of labels.");
        occupied.Clear();
        if (!MapLabelLayout.TryPlace(0, 70, 40, 13, viewport, occupied, out var edge) || edge.Y >= 70)
            throw new InvalidOperationException("Top-edge labels must use an alternate placement.");
        var snapshot = occupied.ToArray();
        occupied.Clear();
        MapLabelLayout.TryPlace(0, 70, 40, 13, viewport, occupied, out var repeat);
        if (repeat.X != snapshot[0].X || repeat.Y != snapshot[0].Y)
            throw new InvalidOperationException("Unchanged map labels must keep stable positions.");
        Console.WriteLine("MapLabelLayout: density, bounds, alternate placement and stability passed.");
        // The native viewport uses a top-left pivot. Selected names must still
        // fit at every edge, outside the view, and when ordinary placement fails.
        var nativeViewport = new MapLabelBounds(4, -140, 307, 136);
        foreach (float x in new[] { -100f, 4f, 150f, 311f, 500f })
            foreach (float y in new[] { -400f, -140f, -75f, -4f, 100f })
                foreach (float width in new[] { 35f, 140f, 600f })
                {
                    var focus = MapLabelLayout.PlaceFocused(x, y, width, 20, nativeViewport);
                    if (focus.X < 4 || focus.Y < -140 || focus.X + focus.Width > 311 || focus.Y + focus.Height > -4)
                        throw new InvalidOperationException("Focused labels must remain inside a top-left-pivot viewport.");
                    if (focus.Width <= 0 || focus.Height <= 0)
                        throw new InvalidOperationException("Focused labels must never disappear when clamped.");
                }
        occupied.Clear(); occupied.Add(nativeViewport);
        if (MapLabelLayout.TryPlace(150, -75, 60, 20, nativeViewport, occupied, out _))
            throw new InvalidOperationException("Dense fixture must block ordinary label placement.");
        var forced = MapLabelLayout.PlaceFocused(150, -75, 60, 20, nativeViewport);
        occupied.Clear(); occupied.Add(forced);
        if (MapLabelLayout.TryPlace(150, -75, 60, 20, nativeViewport, occupied, out var ordinary) && ordinary.Overlaps(forced))
            throw new InvalidOperationException("Ordinary labels must yield to the focused label.");
        Console.WriteLine("Focused map labels: 75 edge/size/offscreen cases and dense priority passed.");
        if (NpcTrackingDirection.TryPlace(10, 20, 10, 20, 8, 5, out _))
            throw new InvalidOperationException("Visible NPCs must not produce edge arrows.");
        foreach (var target in new[] { (-100f, 20f), (100f, 20f), (10f, -100f), (10f, 100f), (100f, 100f), (-100f, -100f) })
        {
            if (!NpcTrackingDirection.TryPlace(target.Item1, target.Item2, 10, 20, 8, 5, out var arrow))
                throw new InvalidOperationException("Offscreen NPCs need an edge indicator.");
            if (arrow.X < 2.3125f || arrow.X > 17.6875f || arrow.Y < 15.3125f || arrow.Y > 24.6875f)
                throw new InvalidOperationException("NPC arrows must preserve the native camera-edge inset.");
        }
        var english = new Dictionary<string, string>();
        var fallback = new Dictionary<string, string>();
        MapNavigationLocalization.Register((_, key, text) => english.Add(key, text), new[] { "en-US" });
        MapNavigationLocalization.Register((_, key, text) => fallback.Add(key, text), new[] { "unsupported" });
        if (english.Count != 25 || fallback.Count != english.Count ||
            english.Any(pair => fallback[pair.Key] != pair.Value))
            throw new InvalidOperationException("Map navigation must fall back as a complete language group.");
        if (english[MapNavigationLocalization.MultiplayerGate] == english[MapNavigationLocalization.TownReturnPortal])
            throw new InvalidOperationException("Multiplayer access and returning to your town are different destinations.");
        if (english[MapNavigationLocalization.RoomTravel] == english[MapNavigationLocalization.Travel] ||
            !english[MapNavigationLocalization.RoomGuide].Contains("{0}"))
            throw new InvalidOperationException("Room travel must have its own destination semantics and current input binding.");
    }
}
