namespace SephiriaEnhancements.CombatRelationOutlines
{
    internal static class CombatRelationOutlinePolicy
    {
        internal static bool ShouldShow(bool suiteEnabled, bool featureEnabled,
            bool hasReferencePlayer, bool isReferencePlayer, bool relationAllowed,
            bool isAlive, bool isTargetable, bool isActive)
        {
            return suiteEnabled && featureEnabled && hasReferencePlayer &&
                !isReferencePlayer && relationAllowed && isAlive &&
                isTargetable && isActive;
        }
    }
}
