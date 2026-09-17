using SephiriaEnhancements.Configuration;
using SephiriaEnhancements.Integration;
using SephiriaEnhancements.KeyboardUiNavigation;
using UnityEngine.InputSystem;

namespace SephiriaEnhancements.CostumeAppearance.Integration
{
    internal static class NativeAppearanceRegionInput
    {
        internal static bool WasPressed(UIBase panel)
        {
            var input = PlayerInputController.Instance?.playerInput;
            var stack = UIManager.Instance?.CurrentControlStack;
            if (input?.currentControlScheme != ModShortcuts.GamepadScheme ||
                stack == null || stack.Count == 0 || stack[0] != panel || !KeyboardUiSelection.IsPanelReady(panel))
                return false;
            return NativeInputActions.FindAction(input.actions, NativeUiActions.PrevTab)?.WasPressedThisFrame() == true ||
                NativeInputActions.FindAction(input.actions, NativeUiActions.NextTab)?.WasPressedThisFrame() == true;
        }

        internal static string Hint()
        {
            var input = PlayerInputController.Instance?.playerInput;
            // Tab belongs to the existing keyboard navigation extension, not a rebindable game action.
            string binding = "Tab";
            if (input?.currentControlScheme == ModShortcuts.GamepadScheme)
            {
                string previous = BindingLabel(NativeInputActions.FindAction(input.actions, NativeUiActions.PrevTab));
                string next = BindingLabel(NativeInputActions.FindAction(input.actions, NativeUiActions.NextTab));
                binding = string.IsNullOrEmpty(previous) ? next :
                    string.IsNullOrEmpty(next) || previous == next ? previous : previous + " / " + next;
            }
            return string.IsNullOrEmpty(binding) ? string.Empty :
                string.Format(ModLocalization.Get(CostumeAppearanceLocalization.SwitchRegion), binding);
        }

        private static string BindingLabel(InputAction action)
        {
            if (action == null || !action.enabled) return string.Empty;
            foreach (var control in action.controls)
            {
                if (!(control.device is Gamepad)) continue;
                int index = action.GetBindingIndexForControl(control);
                if (index >= 0) return action.GetBindingDisplayString(index);
            }
            return string.Empty;
        }
    }
}
