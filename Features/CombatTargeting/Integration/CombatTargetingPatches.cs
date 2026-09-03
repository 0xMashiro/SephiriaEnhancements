using HarmonyLib;
using UnityEngine;

namespace SephiriaEnhancements.CombatTargeting
{
    [HarmonyPatch(typeof(PlayerInputController), "Update")]
    internal static class CombatTargetingInputPatch
    {
        private static void Postfix(PlayerInputController __instance) =>
            CombatTargetingController.UpdateInput(__instance);
    }

    [HarmonyPatch(typeof(IntegratedActionController), nameof(IntegratedActionController.Cast))]
    internal static class CombatTargetingCastPatch
    {
        private static void Prefix(IntegratedActionController __instance, int idx,
            ref Vector3 aimedPosition, ref UnitAvatar aimedTarget) =>
            CombatTargetingController.PrepareCast(__instance, idx, ref aimedPosition, ref aimedTarget);
    }

    [HarmonyPatch(typeof(IntegratedActionController), nameof(IntegratedActionController.CastStop))]
    internal static class CombatTargetingReleasePatch
    {
        private static void Prefix(IntegratedActionController __instance) =>
            CombatTargetingController.PrepareRelease(__instance);
    }

    [HarmonyPatch(typeof(PlayerInputController), nameof(PlayerInputController.HandleOnDash))]
    internal static class CombatTargetingDashPatch
    {
        private static void Prefix(PlayerInputController __instance) =>
            CombatTargetingController.UpdateInput(__instance);
    }
}
