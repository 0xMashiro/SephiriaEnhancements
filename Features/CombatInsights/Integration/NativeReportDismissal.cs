using HarmonyLib;
using SephiriaEnhancements.Combat;
using UnityEngine.InputSystem;

namespace SephiriaEnhancements.Integration
{
    [HarmonyPatch(typeof(UIInputModule), "Update")]
    internal static class NativeReportDismissal
    {
        private static CombatInsightsController controller;
        internal static bool IsAvailable { get; set; }

        internal static void SetController(CombatInsightsController value)
        {
            controller = value;
            IsAvailable = false;
        }

        private static bool Prefix(UIInputModule __instance)
        {
            // CloseControl is the game's menu command, distinct from UI/Cancel.
            // Consume it before native menu opening, only when it dismisses a report.
            return __instance != UIInputModule.current ||
                __instance.closeControlAction?.action?.WasPressedThisFrame() != true ||
                controller == null || !controller.TryDismissPresentedReport();
        }

        internal static string BindingLabel()
        {
            if (!IsAvailable) return string.Empty;
            InputAction action = UIInputModule.current?.closeControlAction?.action;
            if (action == null || !action.enabled) return string.Empty;
            bool gamepad = PlayerInputController.Instance?.playerInput?
                .currentControlScheme == ModShortcuts.GamepadScheme;
            // Native menu bindings can have no binding group. Resolve the actual
            // device controls instead of filtering out those ungrouped bindings.
            foreach (InputControl control in action.controls)
            {
                if (gamepad ? !(control.device is Gamepad)
                    : !(control.device is Keyboard)) continue;
                int index = action.GetBindingIndexForControl(control);
                if (index >= 0) return action.GetBindingDisplayString(index);
            }
            return string.Empty;
        }
    }
}
