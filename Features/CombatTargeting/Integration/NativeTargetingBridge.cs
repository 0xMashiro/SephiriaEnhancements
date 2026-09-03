using System;
using HarmonyLib;
using SephiriaEnhancements.Integration;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SephiriaEnhancements.CombatTargeting
{
    internal static class NativeTargetingBridge
    {
        private static readonly AccessTools.FieldRef<PlayerInputController, Vector2> Movement =
            AccessTools.FieldRefAccess<PlayerInputController, Vector2>("moveInput");
        private static readonly Func<PlayerInputController, bool> ValidateMovement =
            AccessTools.MethodDelegate<Func<PlayerInputController, bool>>(
                AccessTools.Method(typeof(PlayerInputController), "ValidateScreenFader_PlayerMove"));
        private static readonly Action<UnitAvatar, Vector2> SetAimedPosition =
            AccessTools.MethodDelegate<Action<UnitAvatar, Vector2>>(
                AccessTools.PropertySetter(typeof(UnitAvatar), nameof(UnitAvatar.AimedPosition)));

        internal static Vector2 ReadMovement(PlayerInputController input) => Movement(input);
        internal static bool IsReady(PlayerInputController input) => ValidateMovement(input);

        // These indices and slot types belong to the native Cast API.
        internal static InputAction FindAction(InputActionAsset actions, int slot)
        {
            if (slot == 100) return NativeInputActions.FindAction(actions, NativePlayerActions.Fire);
            if (slot == 101) return NativeInputActions.FindAction(actions, NativePlayerActions.SubFire);
            if (slot == 102) return actions.FindAction("Player/Reload", false);
            return slot >= 0 && slot < 8
                ? actions.FindAction("Magic_Keyboard/QuickCast" + (slot + 1), false) : null;
        }

        internal static bool IsAbility(IntegratedActionController source, int slot)
        {
            QuickSlotData data = slot >= 100 && slot <= 102 ? source.quickSlotsWeapon[slot - 100]
                : slot >= 0 && slot < 8 ? source.quickSlots[slot] : null;
            return data != null && (data.Type == QuickSlotType.Magic || data.Type == QuickSlotType.Active);
        }

        internal static void RestoreMouseAim(PlayerInputController input, PlayerAvatar player,
            WeaponControllerSimple weapon)
        {
            Camera camera = GameCamera.Instance?.Camera;
            if (camera == null || !InputDeviceState.TryGetPointerPosition(out Vector2 pointer)) return;
            Vector2 position = camera.ScreenToWorldPoint(pointer);
            UnitAvatar target = OptionsBinding.Instance.Options.GetInt("KeyboardAimSupport", 0) == 1
                ? PlayerInputController.SearchTargetNearestPoint(player, position, 4f) : null;
            input.autoAimedTarget = target;
            player.autoAimedTarget = target;
            UIManager.Instance.GetElement<UI_TargetingCursor>()?.SetTarget(target);
            if (target != null && (player.activeMagicCastModeClientside || weapon.HasRangedWeapon()))
                position = target.transform.position;
            ApplyAim(input, player, weapon, position);
        }

        internal static void ApplyAim(PlayerInputController input, PlayerAvatar player,
            WeaponControllerSimple weapon, Vector2 position)
        {
            input.ForceAimToPosition(position);
            // Continuous attacks consume this copy before the next player update.
            // CurrentLookingPosition preserves the native movement/action gate.
            weapon.aimedPositionClientside = player.CurrentLookingPosition;
            SetAimedPosition(player, player.CurrentLookingPosition);
        }
    }
}
