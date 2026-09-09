using SephiriaEnhancements.Configuration;

namespace SephiriaEnhancements.MultiplayerAccess
{
    internal static class MidRunAdmissionSettings
    {
        internal const string AllowMidRunJoinKey =
            "SephiriaEnhancements.MultiplayerAccess.AllowMidRunJoin";

        internal static bool AllowMidRunJoin
        {
            get => OptionsBinding.Instance?.DeviceOptions?.GetBool(
                AllowMidRunJoinKey, MidRunAdmissionPolicy.DefaultEnabled) ??
                MidRunAdmissionPolicy.DefaultEnabled;
            set => OptionsBinding.Instance?.DeviceOptions?.SetBool(
                AllowMidRunJoinKey, value);
        }

        internal static void Save() => EnhancementsSettings.Save();

        internal static bool ReconnectSupport
        {
            get => OptionsBinding.Instance?.DeviceOptions?.GetBool(
                "SephiriaEnhancements.MultiplayerAccess.ReconnectSupport", true) ?? true;
            set => OptionsBinding.Instance?.DeviceOptions?.SetBool(
                "SephiriaEnhancements.MultiplayerAccess.ReconnectSupport", value);
        }
    }
}
