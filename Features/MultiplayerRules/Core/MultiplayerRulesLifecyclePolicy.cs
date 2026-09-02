namespace SephiriaEnhancements.MultiplayerRules
{
    internal static class MultiplayerRulesLifecyclePolicy
    {
        internal static bool RequiresNativeBehaviorHooks(
            MultiplayerRulesPreset preset)
        {
            return preset != MultiplayerRulesPreset.Original;
        }

        internal static bool CanEditHostPreferences(bool localPeerIsHost,
            bool explorationActive)
        {
            return localPeerIsHost && !explorationActive;
        }

        internal static bool CanApplyAuthoritativeRules(bool serverActive,
            bool explorationActive, bool integrationAvailable,
            int participantCount, bool multiplayerExtensionPresent,
            bool allowExternalRuleStacking)
        {
            return serverActive && explorationActive && integrationAvailable &&
                participantCount >= 1 && participantCount <= 4 &&
                (!multiplayerExtensionPresent || allowExternalRuleStacking);
        }
    }
}
