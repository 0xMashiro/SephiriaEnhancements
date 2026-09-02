using SephiriaEnhancements.Integration;
using System.Linq;
using UnityEngine.InputSystem;

namespace SephiriaEnhancements.Inventory
{
    internal static class NativeInventoryLevelEditShortcut
    {
        private static InputAction Action => NativeInputActions.FindShortcut(
            PlayerInputController.Instance?.playerInput?.actions, ModShortcuts.SwitchLockedTarget);

        internal static InputAction PressedAction(UI_CharacterStatusPanel panel)
        {
            // Combat target switching yields whenever a menu owns the controls.
            if (UIManager.Instance?.CurrentControlStack?.Contains(panel) != true) return null;
            var action = Action;
            return action?.WasPressedThisFrame() == true ? action : null;
        }

        internal static string BindingLabel => Action?.GetBindingDisplayString(
            group: PlayerInputController.Instance?.playerInput?.currentControlScheme) ?? string.Empty;
    }
}
