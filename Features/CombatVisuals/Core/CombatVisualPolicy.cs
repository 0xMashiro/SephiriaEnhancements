namespace SephiriaEnhancements.CombatVisuals
{
    internal enum CombatVisualPreset
    {
        FollowGame,
        Balanced,
        Minimal,
        Custom
    }

    internal enum EffectTransparencyLevel
    {
        Normal,
        SlightlyTransparent,
        VeryTransparent,
        CompletelyTransparent
    }

    internal enum CombatVisualSourceRelation
    {
        Unknown,
        LocalPlayer,
        LocalCompanion,
        RemotePlayer,
        RemoteCompanion,
        Other
    }

    internal enum CombatVisualSurface
    {
        Body,
        Effect
    }

    internal enum CombatOutlineScope
    {
        Off,
        HostileOnly,
        HostileAndFriendly
    }

    internal static class CombatVisualPolicy
    {
        internal const CombatVisualPreset DefaultPreset =
            CombatVisualPreset.Balanced;

        internal static bool TryGetTransparencyLevel(CombatVisualPreset preset,
            CombatVisualSourceRelation relation, CombatVisualSurface surface,
            EffectTransparencyLevel customBody,
            EffectTransparencyLevel customEffect,
            out EffectTransparencyLevel level)
        {
            level = EffectTransparencyLevel.Normal;
            if (relation != CombatVisualSourceRelation.LocalCompanion ||
                preset == CombatVisualPreset.FollowGame)
            {
                return false;
            }

            switch (preset)
            {
                case CombatVisualPreset.Balanced:
                    level = surface == CombatVisualSurface.Body
                        ? EffectTransparencyLevel.SlightlyTransparent
                        : EffectTransparencyLevel.VeryTransparent;
                    return true;
                case CombatVisualPreset.Minimal:
                    level = surface == CombatVisualSurface.Body
                        ? EffectTransparencyLevel.VeryTransparent
                        : EffectTransparencyLevel.CompletelyTransparent;
                    return true;
                case CombatVisualPreset.Custom:
                    level = surface == CombatVisualSurface.Body
                        ? customBody : customEffect;
                    return true;
                default:
                    return false;
            }
        }

        internal static CombatOutlineScope GetOutlineScope(
            CombatVisualPreset preset, CombatOutlineScope customScope)
        {
            switch (preset)
            {
                case CombatVisualPreset.Balanced:
                case CombatVisualPreset.Minimal:
                    return CombatOutlineScope.HostileAndFriendly;
                case CombatVisualPreset.Custom:
                    return customScope;
                default:
                    return CombatOutlineScope.HostileAndFriendly;
            }
        }

        internal static bool AllowsOutline(CombatVisualPreset preset,
            CombatOutlineScope customScope, int multiplayerCount,
            bool isFriendly, bool isHostile)
        {
            if (preset == CombatVisualPreset.FollowGame)
            {
                return multiplayerCount > 1 && (isFriendly || isHostile);
            }

            CombatOutlineScope scope = GetOutlineScope(preset, customScope);
            return isHostile && scope != CombatOutlineScope.Off ||
                isFriendly && scope == CombatOutlineScope.HostileAndFriendly;
        }
    }
}
