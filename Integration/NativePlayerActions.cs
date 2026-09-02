namespace SephiriaEnhancements.Integration
{
    internal static class NativePlayerActions
    {
        private const string MapName = "Player";

        internal static readonly NativeActionId Fire =
            new NativeActionId(MapName, "Fire");
        internal static readonly NativeActionId SubFire =
            new NativeActionId(MapName, "SubFire");
    }
}
