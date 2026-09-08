namespace SephiriaEnhancements.MapEnhancements
{
    internal static class MapEnhancementsSettings
    {
        internal const string EnabledKey = "SephiriaEnhancements.MapEnhancements.Enabled";
        internal static bool Enabled
        {
            get => OptionsBinding.Instance?.DeviceOptions?.GetBool(EnabledKey, true) ?? true;
            set => OptionsBinding.Instance?.DeviceOptions?.SetBool(EnabledKey, value);
        }

        internal static bool IsActive => Configuration.EnhancementsSettings.Enabled && Enabled;

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
