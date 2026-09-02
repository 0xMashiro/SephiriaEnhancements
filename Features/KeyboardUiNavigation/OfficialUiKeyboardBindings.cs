using SephiriaEnhancements.Diagnostics;
using System;
using SephiriaEnhancements.Integration;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SephiriaEnhancements.KeyboardUiNavigation
{
    internal static class OfficialUiKeyboardBindings
    {
        private const string EngraveTabletDefaultPath = "<Keyboard>/y";

        private static readonly Guid ThrowItemExtensionBindingId =
            new Guid("3d72d996-6307-4a2b-a6de-8764479a7c36");
        private static readonly Guid RotateItemExtensionBindingId =
            new Guid("9ec85e8c-c21f-4a3c-a58f-b77872c28849");
        private static readonly Guid EngraveTabletFallbackBindingId =
            new Guid("531c79b8-da09-4f3f-a096-04819967af15");
        private static bool fallbackInitializationChecked;

        internal static void RestoreSavedFallbackBindings(InputActionAsset asset)
        {
            string savedOverrides =
                OptionsBinding.Instance?.Options?.GetString("KeyRebinds", "");
            OfficialKeyboardBindingFallbacks.
                RestoreFallbackIfGroupMissingAndReferenced(asset,
                    savedOverrides, NativeUiActions.ThrowItem,
                    ThrowItemExtensionBindingId);
            OfficialKeyboardBindingFallbacks.
                RestoreFallbackIfGroupMissingAndReferenced(asset,
                    savedOverrides, NativeUiActions.RotateItem,
                    RotateItemExtensionBindingId);
            OfficialKeyboardBindingFallbacks.
                RestoreFallbackIfGroupMissingAndReferenced(asset,
                    savedOverrides, NativeUiActions.EngraveTablet,
                    EngraveTabletFallbackBindingId);
        }

        internal static void EnsureFallbackBindingInitialization()
        {
            if (fallbackInitializationChecked)
            {
                return;
            }

            OptionsBinding options = OptionsBinding.Instance;
            if (options?.actionAsset == null)
            {
                return;
            }

            InputActionAsset asset = options.actionAsset;
            RestoreSavedFallbackBindings(asset);
            NativeInputActions.ReloadSavedOverrides(asset);
            // Re-evaluate the live action map every process start. Native slots
            // always win; blank extension slots keep actions rebindable if a game
            // version omits them, without freezing today's physical defaults.
            bool throwItemReady = OfficialKeyboardBindingFallbacks.
                EnsureEmptyBindingIfGroupMissing(asset,
                    NativeUiActions.ThrowItem,
                    ThrowItemExtensionBindingId);
            bool rotateItemReady = OfficialKeyboardBindingFallbacks.
                EnsureEmptyBindingIfGroupMissing(asset,
                    NativeUiActions.RotateItem,
                    RotateItemExtensionBindingId);
            bool engraveReady = OfficialKeyboardBindingFallbacks.
                ApplyDefaultBindingIfGroupMissing(asset,
                    NativeUiActions.EngraveTablet,
                    EngraveTabletDefaultPath, EngraveTabletFallbackBindingId);

            fallbackInitializationChecked = true;
            NativeInputActions.SaveOfficialOverrides(asset);

            InputActionAsset runtime =
                PlayerInputController.Instance?.playerInput?.actions;
            EnsureRuntimeBindings(asset, runtime);
            NativeInputActions.SynchronizeOverrides(runtime);

            if (!throwItemReady || !rotateItemReady || !engraveReady)
            {
                SupportLogger.Warning("keyboard_actions_unavailable", "[SephiriaEnhancements] Some native UI keyboard " +
                    "actions could not be exposed, or the engraving fallback key " +
                    "conflicts with another gameplay action.");
            }
        }

        internal static void EnsureRuntimeBindings(InputActionAsset source,
            InputActionAsset destination)
        {
            if (source == null || destination == null || source == destination)
            {
                return;
            }

            OfficialKeyboardBindingFallbacks.EnsureFallbackBindingMatches(source,
                destination, NativeUiActions.ThrowItem,
                ThrowItemExtensionBindingId);
            OfficialKeyboardBindingFallbacks.EnsureFallbackBindingMatches(source,
                destination, NativeUiActions.RotateItem,
                RotateItemExtensionBindingId);
            OfficialKeyboardBindingFallbacks.EnsureFallbackBindingMatches(source,
                destination, NativeUiActions.EngraveTablet,
                EngraveTabletFallbackBindingId);
        }
    }
}
