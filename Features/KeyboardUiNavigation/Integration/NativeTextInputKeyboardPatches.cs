using HarmonyLib;
using HeathenEngineering.SteamworksIntegration.API;
using SephiriaEnhancements.Runtime;
using TMPro;

namespace SephiriaEnhancements.KeyboardUiNavigation.Integration
{
    internal static class NativeTextInputKeyboard
    {
        private static int pendingDismissals;

        internal static void Dismiss()
        {
            // The native defocus method has already released the input field.
            // Reserve the callback before calling Steam, including synchronous delivery.
            pendingDismissals++;
            if (!Steamworks.SteamUtils.DismissFloatingGamepadTextInput()) pendingDismissals--;
        }

        internal static bool ConsumeDismissal()
        {
            if (pendingDismissals == 0) return false;
            pendingDismissals--;
            return true;
        }

        internal static void Reset() => pendingDismissals = 0;
    }

    [HarmonyPatch(typeof(SteamDeckKeyboard), nameof(SteamDeckKeyboard.NotifyFieldDefocused))]
    internal static class NativeTextInputKeyboardDefocusPatch
    {
        private static void Prefix(TMP_InputField field, TMP_InputField ___activeField, out bool __state)
        {
            __state = false;
            if (!FeatureFailure.IsAvailable(FeatureId.KeyboardUiNavigation)) return;
            try { __state = PrefixCore(field, ___activeField); }
            catch (System.Exception exception)
            {
                FeatureFailure.Disable(FeatureId.KeyboardUiNavigation, exception);
            }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static bool PrefixCore(TMP_InputField field, TMP_InputField activeField)
        {
            // Reference identity also handles the owner's OnDisable during destruction.
            return !ReferenceEquals(activeField, null) && ReferenceEquals(field, activeField);
        }

        private static void Postfix(bool __state)
        {
            if (!FeatureFailure.IsAvailable(FeatureId.KeyboardUiNavigation)) return;
            try { PostfixCore(__state); }
            catch (System.Exception exception)
            {
                FeatureFailure.Disable(FeatureId.KeyboardUiNavigation, exception);
            }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static void PostfixCore(bool ownedKeyboard)
        {
            if (ownedKeyboard && App.Initialized) NativeTextInputKeyboard.Dismiss();
        }
    }

    [HarmonyPatch(typeof(SteamDeckKeyboard), "HandleKeyboardClosed")]
    internal static class NativeTextInputKeyboardClosedPatch
    {
        private static bool Prefix()
        {
            if (!FeatureFailure.IsAvailable(FeatureId.KeyboardUiNavigation)) return true;
            try { return PrefixCore(); }
            catch (System.Exception exception)
            {
                FeatureFailure.Disable(FeatureId.KeyboardUiNavigation, exception);
                return true;
            }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static bool PrefixCore()
        {
            // An acknowledgement for an old field must not deactivate a newer field.
            return !NativeTextInputKeyboard.ConsumeDismissal();
        }
    }
}
