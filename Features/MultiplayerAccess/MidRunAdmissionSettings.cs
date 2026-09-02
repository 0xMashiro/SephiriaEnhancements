using SephiriaEnhancements.Configuration;

namespace SephiriaEnhancements.MultiplayerAccess
{
    internal static class MidRunAdmissionSettings
    {
        internal const string AllowJoinAndReconnectKey =
            "SephiriaEnhancements.MultiplayerAccess.AllowMidRunJoinAndReconnect";

        internal static bool AllowJoinAndReconnect
        {
            get => OptionsBinding.Instance?.DeviceOptions?.GetBool(
                AllowJoinAndReconnectKey, MidRunAdmissionPolicy.DefaultEnabled) ??
                MidRunAdmissionPolicy.DefaultEnabled;
            set => OptionsBinding.Instance?.DeviceOptions?.SetBool(
                AllowJoinAndReconnectKey, value);
        }

        internal static void Save() => EnhancementsSettings.Save();
    }
}
