using SephiriaEnhancements.Integration;
using SephiriaEnhancements.DeveloperConsole;

namespace SephiriaEnhancements.Configuration
{
    internal static class NativeControlOptionsIntegration
    {
        private static readonly NativeRebindDefinition[] KeyboardDefinitions =
        {
            new NativeRebindDefinition(
                NativeUiActions.ThrowItem,
                ControlLocalization.SecondaryUiAction),
            new NativeRebindDefinition(
                NativeUiActions.RotateItem,
                ControlLocalization.RotateItem),
            new NativeRebindDefinition(
                NativeUiActions.EngraveTablet,
                ControlLocalization.EngraveTablet),
            new NativeRebindDefinition(
                ModShortcuts.SwitchLockedTarget,
                ControlLocalization.SwitchLockedTarget),
            new NativeRebindDefinition(
                ModShortcuts.ToggleCurrentFloorMapOverlay,
                ControlLocalization.ToggleCurrentFloorMapOverlay),
            new NativeRebindDefinition(
                ModShortcuts.ToggleDamageStatistics,
                ControlLocalization.ToggleDamageStatistics),
            new NativeRebindDefinition(
                ModShortcuts.OptimizeInventory,
                ControlLocalization.OptimizeInventory),
            new NativeRebindDefinition(
                DeveloperConsoleContract.ActionMapName,
                DeveloperConsoleContract.ActionName,
                ModLocalization.DeveloperConsoleShortcut)
        };

        private static readonly NativeRebindDefinition[] GamepadDefinitions =
        {
            new NativeRebindDefinition(
                ModShortcuts.SwitchLockedTarget,
                ControlLocalization.SwitchLockedTarget),
            new NativeRebindDefinition(
                ModShortcuts.ToggleCurrentFloorMapOverlay,
                ControlLocalization.ToggleCurrentFloorMapOverlay),
            new NativeRebindDefinition(
                ModShortcuts.ToggleDamageStatistics,
                ControlLocalization.ToggleDamageStatistics),
            new NativeRebindDefinition(
                ModShortcuts.OptimizeInventory,
                ControlLocalization.OptimizeInventory)
        };

        internal static void Inject(UI_OptionsPanel panel)
        {
            if (panel == null ||
                NativeInputActions.EnsureShortcutMap(panel.actions) == null)
            {
                return;
            }

            NativeRebindSectionBuilder.Inject(panel, KeyboardDefinitions,
                ModShortcuts.KeyboardScheme, ControlLocalization.ShortcutsSection,
                ModShortcuts.MapName);
            NativeRebindSectionBuilder.Inject(panel, GamepadDefinitions,
                ModShortcuts.GamepadScheme, ControlLocalization.ShortcutsSection,
                ModShortcuts.MapName);
        }
    }
}
