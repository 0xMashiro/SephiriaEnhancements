namespace SephiriaEnhancements.MultiplayerAccess
{
    internal static class MidRunAdmissionPolicy
    {
        internal const bool DefaultEnabled = true;

        internal static bool CanOwnAdmission(bool suiteEnabled,
            bool settingEnabled, bool integrationAvailable, bool serverActive,
            bool runInProgress, bool hasPerPlayerRunSave,
            bool multiplayerExtensionPresent)
        {
            return suiteEnabled && settingEnabled && integrationAvailable &&
                serverActive && runInProgress && hasPerPlayerRunSave &&
                !multiplayerExtensionPresent;
        }

        internal static bool CanEnableNativeReconnect(bool suiteEnabled,
            bool settingEnabled, bool integrationAvailable,
            bool multiplayerExtensionPresent)
        {
            return suiteEnabled && settingEnabled && integrationAvailable &&
                !multiplayerExtensionPresent;
        }
    }
}
