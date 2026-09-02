using System;
using UnityEngine.InputSystem;

namespace SephiriaEnhancements.Integration
{
    internal static class OfficialKeyboardBindingFallbacks
    {
        internal static bool ApplyDefaultBindingIfGroupMissing(
            InputActionAsset asset, NativeActionId actionId, string controlPath,
            Guid fallbackBindingId) =>
            ApplyDefaultBindingIfGroupMissing(asset, actionId.MapName,
                actionId.ActionName, controlPath, fallbackBindingId);

        internal static bool EnsureEmptyBindingIfGroupMissing(
            InputActionAsset asset, NativeActionId actionId, Guid bindingId) =>
            EnsureEmptyBindingIfGroupMissing(asset, actionId.MapName,
                actionId.ActionName, bindingId);

        internal static void RestoreFallbackIfGroupMissingAndReferenced(
            InputActionAsset asset, string savedOverrides,
            NativeActionId actionId, Guid bindingId) =>
            RestoreFallbackIfGroupMissingAndReferenced(asset, savedOverrides,
                actionId.MapName, actionId.ActionName, bindingId);

        internal static void EnsureFallbackBindingMatches(
            InputActionAsset source, InputActionAsset destination,
            NativeActionId actionId, Guid bindingId) =>
            EnsureFallbackBindingMatches(source, destination, actionId.MapName,
                actionId.ActionName, bindingId);

        internal static bool ApplyDefaultBinding(InputActionAsset asset,
            NativeActionId actionId, string controlPath,
            Guid fallbackBindingId) =>
            ApplyDefaultBinding(asset, actionId.MapName, actionId.ActionName,
                controlPath, fallbackBindingId);

        internal static void RestoreFallbackIfReferenced(
            InputActionAsset asset, string savedOverrides,
            NativeActionId actionId, Guid bindingId) =>
            RestoreFallbackIfReferenced(asset, savedOverrides, actionId.MapName,
                actionId.ActionName, bindingId);

        private static bool ApplyDefaultBindingIfGroupMissing(
            InputActionAsset asset, string actionMapName, string actionName,
            string controlPath, Guid fallbackBindingId)
        {
            InputAction action = NativeInputActions.FindAction(asset,
                new NativeActionId(actionMapName, actionName));
            if (HasBindingForGroup(action, ModShortcuts.KeyboardScheme))
            {
                return true;
            }

            return ApplyDefaultBinding(asset, actionMapName, actionName, controlPath,
                fallbackBindingId);
        }

        private static bool EnsureEmptyBindingIfGroupMissing(
            InputActionAsset asset, string actionMapName, string actionName,
            Guid bindingId)
        {
            InputAction action = NativeInputActions.FindAction(asset,
                new NativeActionId(actionMapName, actionName));
            if (HasBindingForGroup(action, ModShortcuts.KeyboardScheme))
            {
                return true;
            }

            return AddFallbackBinding(action, bindingId) >= 0;
        }

        private static void RestoreFallbackIfGroupMissingAndReferenced(
            InputActionAsset asset, string savedOverrides, string actionMapName,
            string actionName, Guid bindingId)
        {
            InputAction action = NativeInputActions.FindAction(asset,
                new NativeActionId(actionMapName, actionName));
            if (HasBindingForGroup(action, ModShortcuts.KeyboardScheme))
            {
                return;
            }

            RestoreFallbackIfReferenced(asset, savedOverrides, actionMapName,
                actionName, bindingId);
        }

        private static bool ApplyDefaultBinding(InputActionAsset asset,
            string actionMapName, string actionName, string controlPath,
            Guid fallbackBindingId)
        {
            InputAction action = NativeInputActions.FindAction(asset,
                new NativeActionId(actionMapName, actionName));
            if (action == null)
            {
                return false;
            }

            if (HasEffectivePath(action, controlPath))
            {
                return true;
            }

            int bindingIndex = FindEmptyKeyboardBinding(action);
            if (bindingIndex < 0)
            {
                bindingIndex = AddFallbackBinding(action, fallbackBindingId);
            }

            if (bindingIndex < 0 || IsControlUsedByAnotherGameplayAction(
                asset, action, controlPath))
            {
                return false;
            }

            action.ApplyBindingOverride(bindingIndex, controlPath);
            return true;
        }

        private static void RestoreFallbackIfReferenced(InputActionAsset asset,
            string savedOverrides, string actionMapName, string actionName,
            Guid bindingId)
        {
            if (string.IsNullOrEmpty(savedOverrides) ||
                savedOverrides.IndexOf(bindingId.ToString(),
                    StringComparison.OrdinalIgnoreCase) < 0)
            {
                return;
            }

            InputAction action = NativeInputActions.FindAction(asset,
                new NativeActionId(actionMapName, actionName));
            if (!HasBinding(action, bindingId))
            {
                AddFallbackBinding(action, bindingId);
            }
        }

        private static void EnsureFallbackBindingMatches(InputActionAsset source,
            InputActionAsset destination, string actionMapName,
            string actionName, Guid bindingId)
        {
            InputAction sourceAction =
                NativeInputActions.FindAction(source,
                    new NativeActionId(actionMapName, actionName));
            InputAction destinationAction =
                NativeInputActions.FindAction(destination,
                    new NativeActionId(actionMapName, actionName));
            if (HasBinding(sourceAction, bindingId) &&
                !HasBinding(destinationAction, bindingId))
            {
                AddFallbackBinding(destinationAction, bindingId);
            }
        }

        private static int FindEmptyKeyboardBinding(InputAction action)
        {
            for (int index = 0; index < action.bindings.Count; index++)
            {
                InputBinding binding = action.bindings[index];
                if (!binding.isComposite && !binding.isPartOfComposite &&
                    NativeInputActions.HasGroup(binding, ModShortcuts.KeyboardScheme) &&
                    string.IsNullOrEmpty(binding.effectivePath))
                {
                    return index;
                }
            }

            return -1;
        }

        private static bool HasBindingForGroup(InputAction action,
            string group)
        {
            if (action == null)
            {
                return false;
            }

            for (int index = 0; index < action.bindings.Count; index++)
            {
                InputBinding binding = action.bindings[index];
                if (!binding.isComposite && !binding.isPartOfComposite &&
                    NativeInputActions.HasGroup(binding, group))
                {
                    return true;
                }
            }

            return false;
        }

        private static int AddFallbackBinding(InputAction action, Guid bindingId)
        {
            if (action?.actionMap == null)
            {
                return -1;
            }

            bool wasEnabled = action.actionMap.enabled;
            if (wasEnabled)
            {
                action.actionMap.Disable();
            }

            try
            {
                InputBinding binding = new InputBinding
                {
                    id = bindingId,
                    path = string.Empty,
                    groups = ModShortcuts.KeyboardScheme,
                    action = action.name
                };
                action.AddBinding(binding);
                for (int index = 0; index < action.bindings.Count; index++)
                {
                    if (action.bindings[index].id == bindingId)
                    {
                        return index;
                    }
                }
            }
            finally
            {
                if (wasEnabled)
                {
                    action.actionMap.Enable();
                }
            }

            return -1;
        }

        private static bool IsControlUsedByAnotherGameplayAction(
            InputActionAsset asset, InputAction destination, string controlPath)
        {
            foreach (InputActionMap map in asset.actionMaps)
            {
                if (map.name == "UI" || map.name == "Magic_Joystick" ||
                    map.name == ModShortcuts.MapName)
                {
                    continue;
                }

                foreach (InputAction action in map.actions)
                {
                    if (action != destination &&
                        HasEffectivePath(action, controlPath))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static bool HasEffectivePath(InputAction action,
            string controlPath)
        {
            for (int index = 0; index < action.bindings.Count; index++)
            {
                if (string.Equals(NormalizeControlPath(
                        action.bindings[index].effectivePath),
                    NormalizeControlPath(controlPath),
                    StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private static string NormalizeControlPath(string path) =>
            string.IsNullOrEmpty(path)
                ? string.Empty
                : path.Replace("<", string.Empty).Replace(">", string.Empty)
                    .TrimStart('/');

        private static bool HasBinding(InputAction action, Guid bindingId)
        {
            if (action == null)
            {
                return false;
            }

            for (int index = 0; index < action.bindings.Count; index++)
            {
                if (action.bindings[index].id == bindingId)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
