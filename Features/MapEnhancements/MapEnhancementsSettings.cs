namespace SephiriaEnhancements.MapEnhancements
{
    internal static class MapEnhancementsSettings
    {
        internal const string ShowHiddenRoomsKey =
            "SephiriaEnhancements.MapEnhancements.ShowHiddenRooms";

        internal static bool ShowHiddenRooms
        {
            get => OptionsBinding.Instance?.DeviceOptions?.GetBool(
                ShowHiddenRoomsKey, false) ?? false;
            set => OptionsBinding.Instance?.DeviceOptions?.SetBool(
                ShowHiddenRoomsKey, value);
        }
    }
}
