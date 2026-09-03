using SephiriaEnhancements.Integration;
using System.Text.Json;

namespace SephiriaEnhancements.ModelChecks.Integration;

internal static class ModShortcutsChecks
{
    internal static void Run()
    {
        using (JsonDocument shortcutDocument = JsonDocument.Parse(ModShortcuts.ActionMapJson))
        {
            JsonElement map = shortcutDocument.RootElement.GetProperty("maps")[0];
            JsonElement actions = map.GetProperty("actions");
            JsonElement bindings = map.GetProperty("bindings");
            if (map.GetProperty("name").GetString() != ModShortcuts.MapName ||
                actions.GetArrayLength() != ModShortcuts.ActionNames.Length ||
                bindings.GetArrayLength() != ModShortcuts.ActionNames.Length * 3)
                throw new InvalidOperationException("shortcut action map shape failed");

            var actionNames = actions.EnumerateArray()
                .Select(action => action.GetProperty("name").GetString())
                .ToHashSet(StringComparer.Ordinal);
            if (ModShortcuts.ActionNames.Any(action => !actionNames.Contains(action)))
                throw new InvalidOperationException("shortcut action catalog mismatch");

            var bindingIds = bindings.EnumerateArray()
                .Select(binding => binding.GetProperty("id").GetString())
                .ToArray();
            if (bindingIds.Distinct(StringComparer.OrdinalIgnoreCase).Count() != bindingIds.Length)
                throw new InvalidOperationException("shortcut binding IDs must be unique");
#if SEPHIRIA_ENHANCEMENTS_DEVTOOLS
            var captures = bindings.EnumerateArray().Where(binding =>
                binding.GetProperty("action").GetString() == ModShortcuts.CaptureInventoryReproduction).ToArray();
            if (captures.Length != 3 || captures.Any(binding => binding.GetProperty("path").GetString() != string.Empty) ||
                captures.Count(binding => binding.GetProperty("groups").GetString() == ModShortcuts.KeyboardScheme) != 2 ||
                captures.Count(binding => binding.GetProperty("groups").GetString() == ModShortcuts.GamepadScheme) != 1)
                throw new InvalidOperationException("manual inventory capture must provide unassigned keyboard and gamepad bindings");
#endif

            JsonElement targetGamepadBinding = bindings.EnumerateArray().Single(binding =>
                binding.GetProperty("action").GetString() == ModShortcuts.SwitchLockedTarget &&
                binding.GetProperty("groups").GetString() == ModShortcuts.GamepadScheme);
            if (targetGamepadBinding.GetProperty("path").GetString() != string.Empty)
                throw new InvalidOperationException("target switching must not occupy the native status-panel button");

            JsonElement mapOverlayBinding = bindings.EnumerateArray().Single(binding =>
                binding.GetProperty("action").GetString() ==
                    ModShortcuts.ToggleCurrentFloorMapOverlay &&
                binding.GetProperty("groups").GetString() ==
                    ModShortcuts.KeyboardScheme &&
                binding.GetProperty("path").GetString() != string.Empty);
            if (mapOverlayBinding.GetProperty("path").GetString() != "<Keyboard>/m")
                throw new InvalidOperationException(
                    "current-floor map overlay default binding failed");

            JsonElement optimizeBinding = bindings.EnumerateArray().Single(binding =>
                binding.GetProperty("action").GetString() ==
                    ModShortcuts.OptimizeInventory &&
                binding.GetProperty("groups").GetString() ==
                    ModShortcuts.KeyboardScheme &&
                binding.GetProperty("path").GetString() != string.Empty);
            if (optimizeBinding.GetProperty("path").GetString() != "<Keyboard>/f8")
                throw new InvalidOperationException("inventory shortcut default binding failed");
        }
        Console.WriteLine("ModShortcuts: action catalog, binding shape and stable IDs passed");
    }
}
