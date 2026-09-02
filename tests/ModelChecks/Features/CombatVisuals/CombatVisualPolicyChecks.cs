using SephiriaEnhancements.CombatVisuals;
using SephiriaEnhancements.CombatRelationOutlines;

namespace SephiriaEnhancements.ModelChecks.Features.CombatVisuals;

internal static class CombatVisualPolicyChecks
{
    internal static void Run()
    {
        if (CombatVisualPolicy.DefaultPreset != CombatVisualPreset.Balanced ||
            CombatVisualPolicy.TryGetTransparencyLevel(
                CombatVisualPreset.FollowGame,
                CombatVisualSourceRelation.LocalCompanion,
                CombatVisualSurface.Body,
                EffectTransparencyLevel.Normal,
                EffectTransparencyLevel.Normal, out _) ||
            !CombatVisualPolicy.TryGetTransparencyLevel(
                CombatVisualPreset.Balanced,
                CombatVisualSourceRelation.LocalCompanion,
                CombatVisualSurface.Body,
                EffectTransparencyLevel.Normal,
                EffectTransparencyLevel.Normal, out EffectTransparencyLevel balancedBody) ||
            balancedBody != EffectTransparencyLevel.SlightlyTransparent ||
            !CombatVisualPolicy.TryGetTransparencyLevel(
                CombatVisualPreset.Balanced,
                CombatVisualSourceRelation.LocalCompanion,
                CombatVisualSurface.Effect,
                EffectTransparencyLevel.Normal,
                EffectTransparencyLevel.Normal, out EffectTransparencyLevel balancedEffect) ||
            balancedEffect != EffectTransparencyLevel.VeryTransparent ||
            !CombatVisualPolicy.TryGetTransparencyLevel(
                CombatVisualPreset.Minimal,
                CombatVisualSourceRelation.LocalCompanion,
                CombatVisualSurface.Body,
                EffectTransparencyLevel.Normal,
                EffectTransparencyLevel.Normal, out EffectTransparencyLevel minimalBody) ||
            minimalBody != EffectTransparencyLevel.VeryTransparent ||
            !CombatVisualPolicy.TryGetTransparencyLevel(
                CombatVisualPreset.Minimal,
                CombatVisualSourceRelation.LocalCompanion,
                CombatVisualSurface.Effect,
                EffectTransparencyLevel.Normal,
                EffectTransparencyLevel.Normal, out EffectTransparencyLevel minimalEffect) ||
            minimalEffect != EffectTransparencyLevel.CompletelyTransparent ||
            CombatVisualPolicy.TryGetTransparencyLevel(
                CombatVisualPreset.Minimal,
                CombatVisualSourceRelation.RemoteCompanion,
                CombatVisualSurface.Effect,
                EffectTransparencyLevel.Normal,
                EffectTransparencyLevel.Normal, out _))
            throw new InvalidOperationException(
                "combat visual presets must only override local companion surfaces");

        if (!CombatVisualPolicy.TryGetTransparencyLevel(
                CombatVisualPreset.Custom,
                CombatVisualSourceRelation.LocalCompanion,
                CombatVisualSurface.Body,
                EffectTransparencyLevel.CompletelyTransparent,
                EffectTransparencyLevel.SlightlyTransparent,
                out EffectTransparencyLevel customBody) ||
            customBody != EffectTransparencyLevel.CompletelyTransparent ||
            !CombatVisualPolicy.TryGetTransparencyLevel(
                CombatVisualPreset.Custom,
                CombatVisualSourceRelation.LocalCompanion,
                CombatVisualSurface.Effect,
                EffectTransparencyLevel.CompletelyTransparent,
                EffectTransparencyLevel.SlightlyTransparent,
                out EffectTransparencyLevel customEffect) ||
            customEffect != EffectTransparencyLevel.SlightlyTransparent ||
            CombatVisualPolicy.AllowsOutline(CombatVisualPreset.FollowGame,
                CombatOutlineScope.HostileAndFriendly, 1, isFriendly: false,
                isHostile: true) ||
            !CombatVisualPolicy.AllowsOutline(CombatVisualPreset.FollowGame,
                CombatOutlineScope.Off, 2, isFriendly: true, isHostile: false) ||
            !CombatVisualPolicy.AllowsOutline(CombatVisualPreset.Balanced,
                CombatOutlineScope.Off, 1, isFriendly: false, isHostile: true) ||
            CombatVisualPolicy.AllowsOutline(CombatVisualPreset.Custom,
                CombatOutlineScope.HostileOnly, 1, isFriendly: true, isHostile: false) ||
            !CombatVisualPolicy.AllowsOutline(CombatVisualPreset.Custom,
                CombatOutlineScope.HostileOnly, 1, isFriendly: false, isHostile: true))
            throw new InvalidOperationException(
                "combat visual custom values or outline scope failed");
        Console.WriteLine("CombatVisualPolicy: preset, surface and outline matrix passed");
    }
}
