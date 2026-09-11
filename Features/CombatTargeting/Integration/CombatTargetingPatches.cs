using SephiriaEnhancements.Runtime;
using HarmonyLib;
using UnityEngine;

namespace SephiriaEnhancements.CombatTargeting
{
    [HarmonyPatch(typeof(PlayerInputController), "Update")]
    internal static class CombatTargetingInputPatch
    {
        private static void Postfix(PlayerInputController __instance)
        {
            if (!FeatureFailure.IsAvailable(FeatureId.CombatTargeting))
            {
                return;
            }

            try
            {
                PostfixCore(__instance);
            }
            catch (System.Exception exception)
            {
                FeatureFailure.Disable(FeatureId.CombatTargeting, exception);
                return;
            }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static void PostfixCore(PlayerInputController __instance) => CombatTargetingController.UpdateInput(__instance);
    }

    [HarmonyPatch(typeof(IntegratedActionController), nameof(IntegratedActionController.Cast))]
    internal static class CombatTargetingCastPatch
    {
        private static void Prefix(IntegratedActionController __instance, int idx, ref Vector3 aimedPosition, ref UnitAvatar aimedTarget)
        {
            if (!FeatureFailure.IsAvailable(FeatureId.CombatTargeting))
            {
                return;
            }

            var original_aimedPosition = aimedPosition;
            var original_aimedTarget = aimedTarget;
            try
            {
                PrefixCore(__instance, idx, ref aimedPosition, ref aimedTarget);
            }
            catch (System.Exception exception)
            {
                aimedPosition = original_aimedPosition;
                aimedTarget = original_aimedTarget;
                FeatureFailure.Disable(FeatureId.CombatTargeting, exception);
                return;
            }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static void PrefixCore(IntegratedActionController __instance, int idx, ref Vector3 aimedPosition, ref UnitAvatar aimedTarget) => CombatTargetingController.PrepareCast(__instance, idx, ref aimedPosition, ref aimedTarget);
    }

    [HarmonyPatch(typeof(IntegratedActionController), nameof(IntegratedActionController.CastStop))]
    internal static class CombatTargetingReleasePatch
    {
        private static void Prefix(IntegratedActionController __instance)
        {
            if (!FeatureFailure.IsAvailable(FeatureId.CombatTargeting))
            {
                return;
            }

            try
            {
                PrefixCore(__instance);
            }
            catch (System.Exception exception)
            {
                FeatureFailure.Disable(FeatureId.CombatTargeting, exception);
                return;
            }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static void PrefixCore(IntegratedActionController __instance) => CombatTargetingController.PrepareRelease(__instance);
    }

    [HarmonyPatch(typeof(PlayerInputController), nameof(PlayerInputController.HandleOnDash))]
    internal static class CombatTargetingDashPatch
    {
        private static void Prefix(PlayerInputController __instance)
        {
            if (!FeatureFailure.IsAvailable(FeatureId.CombatTargeting))
            {
                return;
            }

            try
            {
                PrefixCore(__instance);
            }
            catch (System.Exception exception)
            {
                FeatureFailure.Disable(FeatureId.CombatTargeting, exception);
                return;
            }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static void PrefixCore(PlayerInputController __instance) => CombatTargetingController.UpdateInput(__instance);
    }
}
