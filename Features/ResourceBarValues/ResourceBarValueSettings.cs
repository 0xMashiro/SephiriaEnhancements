namespace SephiriaEnhancements.ResourceBarValues
{
    internal static class ResourceBarValueSettings
    {
        internal static event System.Action Changed;
        internal static bool AnyEnabled
        {
            get
            {
                foreach (ResourceBarValueSetting setting in System.Enum.GetValues(typeof(ResourceBarValueSetting)))
                    if (Get(setting)) return true;
                return false;
            }
        }

        internal static void DisableAll()
        {
            foreach (ResourceBarValueSetting setting in System.Enum.GetValues(typeof(ResourceBarValueSetting)))
                Set(setting, false);
            Save();
        }
        internal static bool Get(ResourceBarValueSetting setting)
        {
            bool fallback = setting == ResourceBarValueSetting.MiniBossHealthNumbers ||
                setting == ResourceBarValueSetting.MiniBossSuperArmorNumbers ||
                setting == ResourceBarValueSetting.BossHealthNumbers;
            return OptionsBinding.Instance?.DeviceOptions?.GetBool(Key(setting), fallback) ?? fallback;
        }

        internal static void Set(ResourceBarValueSetting setting, bool value) =>
            OptionsBinding.Instance?.DeviceOptions?.SetBool(Key(setting), value);

        private static string Key(ResourceBarValueSetting setting) =>
            "SephiriaEnhancements.ResourceBarValues." + setting;

        internal static void Save()
        {
            OptionsBinding.Instance?.DeviceOptions?.Save();
            Changed?.Invoke();
        }
    }
}
