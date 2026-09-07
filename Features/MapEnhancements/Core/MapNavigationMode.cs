namespace SephiriaEnhancements.MapEnhancements.Core
{
    internal enum MapNavigationMode { Rooms, People, Places }

    internal static class MapNavigationModes
    {
        internal static MapNavigationMode Initial(bool hasRooms) =>
            hasRooms ? MapNavigationMode.Rooms : MapNavigationMode.People;

        internal static MapNavigationMode Cycle(MapNavigationMode mode, int direction, bool hasRooms)
        {
            int first = hasRooms ? 0 : 1;
            int count = hasRooms ? 3 : 2;
            return (MapNavigationMode)(first + ((int)mode - first + direction + count) % count);
        }
    }
}
