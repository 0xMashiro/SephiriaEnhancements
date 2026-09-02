using HarmonyLib;
using UnityEngine;

namespace SephiriaEnhancements.RangedControls
{
    [HarmonyPatch(typeof(PlayerAvatar), nameof(PlayerAvatar.AttackButtonDown))]
    internal static class KeyboardBasicAttackPatch
    {
        private static void Prefix(PlayerAvatar __instance, ref Vector2 attackDirection)
        {
            if (RangedControlsController.TryGetKeyboardAttackDirection(
                __instance, OfficialCombatBindings.FireAction,
                out Vector2 replacement))
            {
                attackDirection = replacement;
            }
        }
    }

    [HarmonyPatch(typeof(PlayerAvatar), nameof(PlayerAvatar.SubAttackButtonDown))]
    internal static class KeyboardSpecialAttackPatch
    {
        private static void Prefix(PlayerAvatar __instance, ref Vector2 attackDirection)
        {
            if (RangedControlsController.TryGetKeyboardAttackDirection(
                __instance, OfficialCombatBindings.SubFireAction,
                out Vector2 replacement))
            {
                attackDirection = replacement;
            }
        }
    }
}
