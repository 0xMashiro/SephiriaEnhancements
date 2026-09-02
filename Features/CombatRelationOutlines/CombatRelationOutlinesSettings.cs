namespace SephiriaEnhancements.CombatRelationOutlines
{
    internal static class CombatRelationOutlinesSettings
    {
        internal const string EnabledKey =
            "SephiriaEnhancements.CombatRelationOutlines.Enabled";

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
