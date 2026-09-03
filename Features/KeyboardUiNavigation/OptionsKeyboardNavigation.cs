using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace SephiriaEnhancements.KeyboardUiNavigation
{
    internal static class OptionsKeyboardNavigation
    {
        internal static UI_OptionsPanel ActivePanel()
        {
            if (!KeyboardUiNavigationController.IsKeyboardModeActive()) return null;
            var stack = UIManager.Instance?.CurrentControlStack;
            if (stack == null) return null;
            foreach (UIBase panel in stack)
                if (panel is UI_OptionsPanel options && options.IsControlEnabled)
                    return options;
            return null;
        }

        private static List<Selectable> Entries(UI_OptionsPanel panel)
        {
            var entries = new List<Selectable>();
            UI_Tab tab = panel.tab;
            if (tab == null || tab.CurrentSelectedTab < 0 ||
                tab.CurrentSelectedTab >= tab.tabContents.Length) return entries;
            UI_TabContent content = tab.tabContents[tab.CurrentSelectedTab];
            if (content == null) return entries;
            foreach (Selectable candidate in content.GetComponentsInChildren<Selectable>())
            {
                if (!candidate.IsActive() || !candidate.IsInteractable() ||
                    candidate is Scrollbar || candidate.navigation.mode == Navigation.Mode.None)
                    continue;
                // A value control owns its nested arrow buttons as one setting.
                Selectable owner = candidate.transform.parent?
                    .GetComponentInParent<Selectable>();
                if (owner != null && owner.transform.IsChildOf(content.transform))
                    continue;
                entries.Add(candidate);
            }
            return entries;
        }

        internal static void RequestEntry(UI_OptionsPanel panel)
        {
            List<Selectable> entries = Entries(panel);
            if (entries.Count > 0)
                KeyboardUiNavigationController.RequestSelection(panel, entries[0].gameObject);
        }

        internal static bool SwitchTab()
        {
            UI_OptionsPanel panel = ActivePanel();
            if (panel == null) return false;
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null && keyboard.tabKey.wasPressedThisFrame && panel.tab != null)
            {
                int direction = keyboard.leftShiftKey.isPressed ||
                    keyboard.rightShiftKey.isPressed ? -1 : 1;
                panel.SelectTab(panel.tab.CurrentSelectedTab + direction);
            }
            return true;
        }

        internal static bool Move(Selectable source, AxisEventData eventData)
        {
            UI_OptionsPanel panel = ActivePanel();
            if (panel == null || !source.transform.IsChildOf(panel.transform) ||
                (eventData.moveDir != MoveDirection.Up &&
                 eventData.moveDir != MoveDirection.Down)) return true;
            List<Selectable> entries = Entries(panel);
            if (entries.Count == 0) return false;
            int index = entries.IndexOf(source);
            int direction = eventData.moveDir == MoveDirection.Up ? -1 : 1;
            int next = index < 0 ? 0 : Mathf.Clamp(index + direction, 0, entries.Count - 1);
            eventData.selectedObject = entries[next].gameObject;
            eventData.Use();
            return false;
        }

        internal static bool Owns(Selectable source)
        {
            UI_OptionsPanel panel = ActivePanel();
            return panel != null && source.transform.IsChildOf(panel.transform);
        }
    }

    [HarmonyPatch(typeof(UI_OptionsPanel), nameof(UI_OptionsPanel.SelectTab))]
    internal static class OptionsKeyboardTabSelectionPatch
    {
        private static void Postfix(UI_OptionsPanel __instance)
        {
            if (OptionsKeyboardNavigation.ActivePanel() == __instance)
                OptionsKeyboardNavigation.RequestEntry(__instance);
        }
    }

    [HarmonyPatch(typeof(Selectable), nameof(Selectable.OnMove))]
    internal static class OptionsKeyboardMovePatch
    {
        private static bool Prefix(Selectable __instance, AxisEventData eventData) =>
            OptionsKeyboardNavigation.Move(__instance, eventData);
    }

    [HarmonyPatch]
    internal static class OptionsPointerExitSelectionPatch
    {
        private static IEnumerable<System.Reflection.MethodBase> TargetMethods()
        {
            yield return AccessTools.Method(typeof(UI_HorizontalSelectionBox), "OnPointerExit");
            yield return AccessTools.Method(typeof(UI_HorayButton), "OnPointerExit");
        }

        private static bool Prefix(Selectable __instance) =>
            !OptionsKeyboardNavigation.Owns(__instance);
    }

    [HarmonyPatch]
    internal static class OptionsPointerEnterSelectionPatch
    {
        private static IEnumerable<System.Reflection.MethodBase> TargetMethods()
        {
            yield return AccessTools.Method(typeof(UI_HorizontalSelectionBox), "OnPointerEnter");
            yield return AccessTools.Method(typeof(UI_HorayButton), "OnPointerEnter");
        }

        private static bool Prefix(Selectable __instance)
        {
            if (!OptionsKeyboardNavigation.Owns(__instance)) return true;
            Mouse mouse = Mouse.current;
            return mouse != null && (mouse.delta.ReadValue().sqrMagnitude > 0f ||
                mouse.leftButton.wasPressedThisFrame);
        }
    }
}
