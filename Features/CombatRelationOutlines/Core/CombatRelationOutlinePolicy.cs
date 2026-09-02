namespace SephiriaEnhancements.CombatRelationOutlines
{
    internal static class CombatRelationOutlinePolicy
    {
        internal static bool ShouldShow(bool suiteEnabled, bool featureEnabled,
            bool hasLocalPlayer, bool isLocalPlayer, bool relationAllowed,
            bool isAlive, bool isTargetable, bool isActive)
        {
            return suiteEnabled && featureEnabled && hasLocalPlayer &&
                !isLocalPlayer && relationAllowed && isAlive &&
                isTargetable && isActive;
        }
    }
}
