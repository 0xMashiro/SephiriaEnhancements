using System;
using System.Collections.Generic;
using UnityEngine.InputSystem;

namespace SephiriaEnhancements.Integration
{
    internal static class NativeInputActions
    {
        internal static InputAction EnsureShortcutMap(InputActionAsset asset)
        {
            if (asset == null)
            {
                return null;
            }

            InputAction targetAction = FindShortcut(asset,
                ModShortcuts.SwitchLockedTarget);
            bool complete = targetAction != null &&
                FindShortcut(asset, ModShortcuts.ToggleCurrentFloorMapOverlay) != null &&
                FindShortcut(asset,
                    ModShortcuts.ToggleDamageStatistics) != null &&
                FindShortcut(asset, ModShortcuts.OptimizeInventory) != null;
#if SEPHIRIA_ENHANCEMENTS_DEVTOOLS
            complete &= FindShortcut(asset, ModShortcuts.CaptureInventoryReproduction) != null;
#endif
            if (complete)
            {
                return targetAction;
            }

            List<InputActionMap> enabledMaps = DisableEnabledMaps(asset);
            try
            {
                InputActionMap incompleteMap = asset.FindActionMap(
                    ModShortcuts.MapName, throwIfNotFound: false);
                if (incompleteMap != null)
                {
                    asset.RemoveActionMap(incompleteMap);
                }

                InputActionMap[] maps = InputActionMap.FromJson(
                    ModShortcuts.ActionMapJson);
                if (maps.Length == 0)
                {
                    throw new InvalidOperationException(
                        "Mod shortcut action map is empty.");
                }

                asset.AddActionMap(maps[0]);
                targetAction = maps[0].FindAction(
                    ModShortcuts.SwitchLockedTarget, throwIfNotFound: true);
            }
            finally
            {
                RestoreEnabledMaps(enabledMaps);
            }

            return targetAction;
        }

        internal static void EnableShortcuts(InputActionAsset asset)
        {
            EnsureShortcutMap(asset);
            for (int index = 0; index < ModShortcuts.ActionNames.Length; index++)
            {
                SetEnabled(asset, ModShortcuts.ActionNames[index], enabled: true);
            }
        }

        internal static bool WasPressed(InputActionAsset asset, string actionName,
            bool rejectKeyboardModifiers = false) =>
            (!rejectKeyboardModifiers || !InputDeviceState.HasKeyboardModifierPressed) &&
            (FindShortcut(asset, actionName)?.WasPressedThisFrame() ?? false);

        internal static bool IsPressed(InputActionAsset asset, string actionName,
            bool rejectKeyboardModifiers = false) =>
            (!rejectKeyboardModifiers || !InputDeviceState.HasKeyboardModifierPressed) &&
            (FindShortcut(asset, actionName)?.IsPressed() ?? false);

        internal static InputAction FindShortcut(InputActionAsset asset,
            string actionName) =>
            asset?.FindAction(ModShortcuts.MapName + "/" + actionName,
                throwIfNotFound: false);

        // Game-owned actions must be resolved through a map-qualified ID. This
        // intentionally has no unqualified string overload.
        internal static InputAction FindAction(InputActionAsset asset,
            NativeActionId actionId) =>
            asset?.FindAction(actionId.QualifiedName, throwIfNotFound: false);

        internal static List<int> FindBindingIndices(InputAction action, string group)
        {
            List<int> indices = new List<int>();
            if (action == null)
            {
                return indices;
            }

            for (int index = 0; index < action.bindings.Count; index++)
            {
                InputBinding binding = action.bindings[index];
                if (!binding.isComposite && !binding.isPartOfComposite &&
                    HasGroup(binding, group))
                {
                    indices.Add(index);
                }
            }

            return indices;
        }

        internal static bool HasGroup(InputBinding binding, string group)
        {
            if (string.IsNullOrEmpty(binding.groups))
            {
                return false;
            }

            string[] groups = binding.groups.Split(';');
            for (int index = 0; index < groups.Length; index++)
            {
                if (string.Equals(groups[index], group,
                    StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        internal static void ReloadSavedOverrides(InputActionAsset asset)
        {
            string json = OptionsBinding.Instance?.Options?.GetString("KeyRebinds", "");
            if (!string.IsNullOrEmpty(json))
            {
                asset.LoadBindingOverridesFromJson(json, removeExisting: false);
            }
        }

        internal static void SaveOfficialOverrides(InputActionAsset asset)
        {
            OptionsBinding options = OptionsBinding.Instance;
            if (options?.Options == null || asset == null)
            {
                return;
            }

            options.Options.SetString("KeyRebinds", asset.SaveBindingOverridesAsJson());
            options.Options.Save();
        }

        internal static void SynchronizeOverrides(InputActionAsset destination)
        {
            InputActionAsset source = OptionsBinding.Instance?.actionAsset;
            if (destination == null || source == null || destination == source)
            {
                return;
            }

            EnsureShortcutMap(destination);
            destination.LoadBindingOverridesFromJson(
                source.SaveBindingOverridesAsJson(), removeExisting: false);
        }

        private static void SetEnabled(InputActionAsset asset,
            string actionName, bool enabled)
        {
            InputAction action = FindShortcut(asset, actionName);
            if (action == null)
            {
                return;
            }

            if (enabled && !action.enabled)
            {
                action.Enable();
            }
            else if (!enabled && action.enabled)
            {
                action.Disable();
            }
        }

        private static List<InputActionMap> DisableEnabledMaps(InputActionAsset asset)
        {
            List<InputActionMap> enabledMaps = new List<InputActionMap>();
            foreach (InputActionMap map in asset.actionMaps)
            {
                if (map.enabled)
                {
                    enabledMaps.Add(map);
                    map.Disable();
                }
            }

            return enabledMaps;
        }

        private static void RestoreEnabledMaps(List<InputActionMap> maps)
        {
            for (int index = 0; index < maps.Count; index++)
            {
                maps[index].Enable();
            }
        }
    }
}
