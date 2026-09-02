namespace SephiriaEnhancements.NativeCompanion
{
    internal static class NativeCompanionSettings
    {
        internal const string ModeKey = "SephiriaEnhancements.NativeCompanion.Mode";
        internal const int ModeCount = 4;

        internal static NativeCompanionMode Mode
        {
            get
            {
                int stored = OptionsBinding.Instance?.DeviceOptions?.GetInt(ModeKey, 0) ?? 0;
                if (stored < 0 || stored >= ModeCount)
                {
                    stored = 0;
                }

                return (NativeCompanionMode)stored;
            }
            set
            {
                int stored = (int)value;
                if (stored < 0 || stored >= ModeCount) stored = 0;
                OptionsBinding.Instance?.DeviceOptions?.SetInt(ModeKey, stored);
            }
        }

        internal static void Save()
        {
            OptionsBinding.Instance?.DeviceOptions?.Save();
        }
    }
}
