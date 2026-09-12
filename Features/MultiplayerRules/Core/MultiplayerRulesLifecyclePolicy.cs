namespace SephiriaEnhancements.MultiplayerRules
{
    internal static class MultiplayerRulesLifecyclePolicy
    {
        internal static bool ShouldBeginNewExploration(bool serverActive,
            bool explorationStarted)
        {
            return serverActive && !explorationStarted;
        }

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
            return serverActive && explorationActive && ResolveAvailability(true, integrationAvailable,
                participantCount, multiplayerExtensionPresent, allowExternalRuleStacking) == MultiplayerRulesAvailability.Available;
        }

        internal static MultiplayerRulesAvailability ResolveAvailability(bool enabled, bool integrationAvailable,
            int participantCount, bool multiplayerExtensionPresent, bool allowExternalRuleStacking)
        {
            if (!enabled) return MultiplayerRulesAvailability.Disabled;
            if (!integrationAvailable) return MultiplayerRulesAvailability.Unavailable;
            if (participantCount < 1 || participantCount > 4) return MultiplayerRulesAvailability.UnsupportedTeam;
            if (multiplayerExtensionPresent && !allowExternalRuleStacking) return MultiplayerRulesAvailability.ExternalExtension;
            return MultiplayerRulesAvailability.Available;
        }
    }
}
