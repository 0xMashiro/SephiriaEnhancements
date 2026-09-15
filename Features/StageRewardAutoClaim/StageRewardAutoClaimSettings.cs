namespace SephiriaEnhancements.StageRewardAutoClaim
{
    internal static class StageRewardAutoClaimSettings
    {
        internal const string EnabledKey = "SephiriaEnhancements.StageRewardAutoClaim.Enabled";
        internal static bool Enabled
        {
            get => OptionsBinding.Instance?.DeviceOptions?.GetBool(EnabledKey, false) ?? false;
            set => OptionsBinding.Instance?.DeviceOptions?.SetBool(EnabledKey, value);
        }
        internal static void Save() => OptionsBinding.Instance?.DeviceOptions?.Save();
    }
}
