namespace SephiriaEnhancements.Configuration
{
    internal static class EnhancementsSettings
    {
        internal const string EnabledKey = "SephiriaEnhancements.Enabled";

        internal static bool Enabled
        {
            get => OptionsBinding.Instance?.DeviceOptions?.GetBool(EnabledKey, true) ?? true;
            set => OptionsBinding.Instance?.DeviceOptions?.SetBool(EnabledKey, value);
        }

        internal static void Save()
        {
            OptionsBinding.Instance?.DeviceOptions?.Save();
        }
    }
}
