using SephiriaEnhancements.Configuration;

namespace SephiriaEnhancements.DefeatRetry
{
    internal static class DefeatRetrySettings
    {
        internal const string EnabledKey =
            "SephiriaEnhancements.DefeatRetry.Enabled";

        internal static bool Enabled
        {
            get => OptionsBinding.Instance?.DeviceOptions?.GetBool(EnabledKey, false) ?? false;
            set => OptionsBinding.Instance?.DeviceOptions?.SetBool(EnabledKey, value);
        }

        internal static void Save()
        {
            OptionsBinding.Instance?.DeviceOptions?.Save();
        }
    }
}
