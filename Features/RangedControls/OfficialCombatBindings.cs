using SephiriaEnhancements.Diagnostics;
using System;
using SephiriaEnhancements.Integration;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SephiriaEnhancements.RangedControls
{
    internal static class OfficialCombatBindings
    {
        private const string DefaultBindingInitializationAttemptedKey =
            "SephiriaEnhancements.Controls." +
            "OfficialDefaultBindingInitializationAttempted";
        internal static NativeActionId FireAction => NativePlayerActions.Fire;
        internal static NativeActionId SubFireAction => NativePlayerActions.SubFire;
        private const string FireDefaultPath = "<Keyboard>/j";
        private const string SubFireDefaultPath = "<Keyboard>/k";

        private static readonly Guid FireFallbackBindingId =
            new Guid("a50ff66f-9097-4f53-a0b1-e26d248df974");
        private static readonly Guid SubFireFallbackBindingId =
            new Guid("647bd57d-6421-409e-85c2-49151df3b71a");
        private static bool defaultBindingInitializationChecked;

        internal static void RestoreSavedFallbackBindings(InputActionAsset asset)
        {
            if (asset == null)
            {
                return;
            }

            string savedOverrides =
                OptionsBinding.Instance?.Options?.GetString("KeyRebinds", "");
            OfficialKeyboardBindingFallbacks.RestoreFallbackIfReferenced(
                asset, savedOverrides, NativePlayerActions.Fire,
                FireFallbackBindingId);
            OfficialKeyboardBindingFallbacks.RestoreFallbackIfReferenced(
                asset, savedOverrides, NativePlayerActions.SubFire,
                SubFireFallbackBindingId);
        }

        internal static void EnsureDefaultBindingInitializationIfEnabled()
        {
            if (!defaultBindingInitializationChecked &&
                RangedControlsSettings.TargetingMode != TargetingMode.Disabled)
            {
                InitializeDefaultBindingsOnce();
            }
        }

        internal static void OnTargetingSettingChanged(bool enabled)
        {
            if (enabled && !defaultBindingInitializationChecked)
            {
                InitializeDefaultBindingsOnce();
            }
        }

        internal static void EnsureRuntimeBindings(InputActionAsset source,
            InputActionAsset destination)
        {
            if (source == null || destination == null || source == destination)
            {
                return;
            }

            OfficialKeyboardBindingFallbacks.EnsureFallbackBindingMatches(
                source, destination, NativePlayerActions.Fire,
                FireFallbackBindingId);
            OfficialKeyboardBindingFallbacks.EnsureFallbackBindingMatches(
                source, destination, NativePlayerActions.SubFire,
                SubFireFallbackBindingId);
        }

        internal static bool WasKeyboardCombatPressed(InputActionAsset asset) =>
            WasPressedByKeyboard(asset, NativePlayerActions.Fire) ||
            WasPressedByKeyboard(asset, NativePlayerActions.SubFire);

        internal static bool IsActionControlledByKeyboard(InputActionAsset asset,
            NativeActionId actionId) =>
            NativeInputActions.FindAction(asset,
                actionId)?.activeControl?.device is Keyboard;

        internal static bool IsActionControlledByMouse(InputActionAsset asset,
            NativeActionId actionId) =>
            NativeInputActions.FindAction(asset,
                actionId)?.activeControl?.device is Mouse;

        private static void InitializeDefaultBindingsOnce()
        {
            OptionsBinding options = OptionsBinding.Instance;
            if (options?.actionAsset == null || options.DeviceOptions == null)
            {
                return;
            }

            InputActionAsset asset = options.actionAsset;
            RestoreSavedFallbackBindings(asset);
            NativeInputActions.ReloadSavedOverrides(asset);
            if (options.DeviceOptions.GetBool(
                    DefaultBindingInitializationAttemptedKey, false))
            {
                defaultBindingInitializationChecked = true;
                return;
            }

            bool fireReady = OfficialKeyboardBindingFallbacks.ApplyDefaultBinding(
                asset, NativePlayerActions.Fire,
                FireDefaultPath, FireFallbackBindingId);
            bool subFireReady = OfficialKeyboardBindingFallbacks.ApplyDefaultBinding(
                asset, NativePlayerActions.SubFire,
                SubFireDefaultPath, SubFireFallbackBindingId);

            // This records that initialization was attempted, not that both
            // bindings were applied; conflicts intentionally leave them empty.
            options.DeviceOptions.SetBool(
                DefaultBindingInitializationAttemptedKey, true);
            options.DeviceOptions.Save();
            defaultBindingInitializationChecked = true;
            NativeInputActions.SaveOfficialOverrides(asset);

            InputActionAsset runtime =
                PlayerInputController.Instance?.playerInput?.actions;
            EnsureRuntimeBindings(asset, runtime);
            NativeInputActions.SynchronizeOverrides(runtime);

            if (!fireReady || !subFireReady)
            {
                SupportLogger.Warning("alternate_bindings_conflict", "[SephiriaEnhancements] J/K defaults were not applied " +
                    "because an official secondary binding is already occupied or the " +
                    "requested key is used by another gameplay action.");
            }
            else
            {
                SupportLogger.Info("alternate_bindings_initialized", "[SephiriaEnhancements] Official alternate combat bindings " +
                    "initialized with J/K.");
            }
        }

        private static bool WasPressedByKeyboard(InputActionAsset asset,
            NativeActionId actionId)
        {
            InputAction action = NativeInputActions.FindAction(asset, actionId);
            return action != null && action.WasPressedThisFrame() &&
                action.activeControl?.device is Keyboard;
        }

    }
}
