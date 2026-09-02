namespace SephiriaEnhancements.DeveloperConsole
{
    internal static class DeveloperConsoleSettings
    {
        internal static bool Enabled
        {
            get => OptionsBinding.Instance?.DeviceOptions?.GetBool(
                DeveloperConsoleContract.EnabledKey,
                DeveloperConsoleContract.DefaultEnabled) ??
                DeveloperConsoleContract.DefaultEnabled;
            set => OptionsBinding.Instance?.DeviceOptions?.SetBool(
                DeveloperConsoleContract.EnabledKey, value);
        }

        internal static void Save()
        {
            OptionsBinding.Instance?.DeviceOptions?.Save();
        }
    }
}
