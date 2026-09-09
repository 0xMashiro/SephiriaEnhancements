namespace SephiriaEnhancements.Configuration
{
    internal enum SettingInteractionKind { Master, HostRule, Admission, Reconnect, Companion }
    internal enum SettingLockReason { None, HostOnly, Exploration, ReconnectPending, Connecting, Extension, Unavailable, ModDisabled, Connected }

    internal static class SettingsInteractionPolicy
    {
        internal static SettingLockReason Resolve(SettingInteractionKind kind,
            bool clientActive, bool serverActive, bool worldReady, bool explorationActive,
            bool reconnectPending, bool suiteEnabled, bool extensionPresent, bool admissionAvailable)
        {
            if ((kind == SettingInteractionKind.Admission || kind == SettingInteractionKind.Reconnect) && extensionPresent)
                return SettingLockReason.Extension;
            if ((kind == SettingInteractionKind.Admission || kind == SettingInteractionKind.Reconnect) && !admissionAvailable)
                return SettingLockReason.Unavailable;
            if (kind != SettingInteractionKind.Master && !suiteEnabled)
                return SettingLockReason.ModDisabled;
            if (kind == SettingInteractionKind.Companion)
                return clientActive && !serverActive ? SettingLockReason.HostOnly : SettingLockReason.None;
            if ((kind == SettingInteractionKind.Master || kind == SettingInteractionKind.Reconnect) && reconnectPending)
                return SettingLockReason.ReconnectPending;
            if ((clientActive || serverActive) && !worldReady) return SettingLockReason.Connecting;
            if ((kind == SettingInteractionKind.HostRule || kind == SettingInteractionKind.Admission) && clientActive && !serverActive)
                return SettingLockReason.HostOnly;
            if (explorationActive) return SettingLockReason.Exploration;
            if ((kind == SettingInteractionKind.Master || kind == SettingInteractionKind.Reconnect) && clientActive && !serverActive)
                return SettingLockReason.Connected;
            return SettingLockReason.None;
        }
    }
}
