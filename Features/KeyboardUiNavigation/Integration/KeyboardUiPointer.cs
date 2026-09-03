using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace SephiriaEnhancements.KeyboardUiNavigation
{
    // Native UI API boundary: keep device ownership local to UI navigation.
    // The native Keyboard&Mouse
    // control scheme still owns gameplay, bindings, and the actual mouse position.
    internal static class KeyboardUiPointer
    {
        private static readonly KeyboardPointerOwnership Ownership = new KeyboardPointerOwnership();
        private static int inputFrame = -1;
        private static bool cursorHidden;
        private static bool savedSystemCursorVisible;
        private static UI_Cursor hiddenCursor;
        private static float savedCursorAlpha;
        private static readonly List<RaycastResult> PointerHits = new List<RaycastResult>();

        internal static bool OwnsFocus
        {
            get
            {
                RefreshInput();
                return Ownership.KeyboardOwnsFocus;
            }
        }

        internal static void RefreshInput()
        {
            var stack = UIManager.Instance?.CurrentControlStack;
            bool available = KeyboardUiNavigationController.IsKeyboardModeActive() &&
                Application.isFocused && stack != null && stack.Count > 0;
            if (!available)
            {
                Reset();
                return;
            }
            if (inputFrame == Time.frameCount) return;
            inputFrame = Time.frameCount;
            Mouse mouse = Mouse.current;
            Vector2 position = mouse != null ? mouse.position.ReadValue() : Vector2.zero;
            bool pointerAction = mouse != null &&
                (mouse.leftButton.isPressed || mouse.leftButton.wasReleasedThisFrame ||
                 mouse.rightButton.isPressed || mouse.rightButton.wasReleasedThisFrame ||
                 mouse.middleButton.isPressed || mouse.middleButton.wasReleasedThisFrame ||
                 mouse.scroll.ReadValue().sqrMagnitude > 0f);
            bool keyboardOwnedFocus = Ownership.KeyboardOwnsFocus;
            Ownership.Update(available, Keyboard.current?.anyKey.wasPressedThisFrame == true,
                pointerAction, mouse != null, position.x, position.y);
            if (keyboardOwnedFocus && !Ownership.KeyboardOwnsFocus)
                RestorePointerHover(position);
        }

        private static void RestorePointerHover(Vector2 position)
        {
            EventSystem events = EventSystem.current;
            if (events == null) return;
            // A stationary pointer may already be inside a control whose enter
            // event was ignored during keyboard navigation. Re-enter on takeover
            // so moving within that same control also restores mouse focus.
            var pointer = new PointerEventData(events) { position = position };
            events.RaycastAll(pointer, PointerHits);
            events.SetSelectedGameObject(null);
            if (PointerHits.Count > 0)
                ExecuteEvents.ExecuteHierarchy(PointerHits[0].gameObject, pointer,
                    ExecuteEvents.pointerEnterHandler);
            PointerHits.Clear();
        }

        internal static GameObject SelectedTarget()
        {
            if (!OwnsFocus) return null;
            GameObject selected = EventSystem.current?.currentSelectedGameObject;
            return KeyboardUiSelection.IsInControlStack(selected) ? selected : null;
        }

        internal static void UpdateCursor()
        {
            UI_Cursor cursor = UI_Cursor.Current;
            bool hide = SelectedTarget() != null;
            if (!hide || hiddenCursor != cursor)
                RestoreCursor();
            if (!hide) return;
            if (!cursorHidden)
            {
                savedSystemCursorVisible = Cursor.visible;
                hiddenCursor = cursor;
                savedCursorAlpha = cursor != null && cursor.group != null ? cursor.group.alpha : 0f;
                cursorHidden = true;
            }
            Cursor.visible = false;
            if (cursor != null && cursor.group != null) cursor.group.alpha = 0f;
        }

        private static void RestoreCursor()
        {
            if (!cursorHidden) return;
            Cursor.visible = Application.isFocused ? savedSystemCursorVisible : true;
            // A control-scheme change already gave visibility back to the game.
            if (hiddenCursor != null && hiddenCursor.group != null &&
                ControlsChangeHandler.Current?.IsUsingKeyboardAndMouse == true)
                hiddenCursor.group.alpha = savedCursorAlpha;
            hiddenCursor = null;
            cursorHidden = false;
        }

        internal static void Reset()
        {
            Ownership.Reset();
            inputFrame = -1;
            RestoreCursor();
        }

        internal static void PositionCarriedItem(UI_NewItemPicker_Controller picker)
        {
            GameObject selected = SelectedTarget();
            if (selected == null || !picker.CurrentAny) return;
            RectTransform target = selected.transform as RectTransform;
            if (target == null) return;
            picker.transform.position = target.TransformPoint(target.rect.center);
            // Same canvas-space offset as native focus navigation, keeping the
            // destination cell visible below the carried item.
            picker.rectTransform.anchoredPosition += new Vector2(0f, 20f);
        }

        internal static void CenterMapSelection(UI_MapPanel panel, UI_Map map)
        {
            GameObject selected = SelectedTarget();
            if (selected == null || !panel.IsControlEnabled || map == null) return;
            foreach (UI_Map_Room room in map.rooms)
            {
                if (room == null || room.GetSelectable() != selected) continue;
                panel.contentsParent.anchoredPosition =
                    -(map.contentsChild.anchoredPosition + room.GetIconCenterAnchoredPosition());
                return;
            }
        }
    }

    [HarmonyPatch(typeof(InputSystemUIInputModule), nameof(InputSystemUIInputModule.Process))]
    internal static class KeyboardPointerInputPatch
    {
        private static void Prefix() => KeyboardUiPointer.RefreshInput();
    }

    [HarmonyPatch]
    internal static class KeyboardPointerHoverPatch
    {
        private static IEnumerable<MethodBase> TargetMethods()
        {
            foreach (Type type in new[] { typeof(UI_HorayButton), typeof(UI_HorizontalSelectionBox),
                typeof(UI_HoraySelectable), typeof(UI_ArcSlider), typeof(UI_ArcSliderHandle) })
            {
                yield return AccessTools.Method(type, "OnPointerEnter");
                yield return AccessTools.Method(type, "OnPointerExit");
            }
        }

        private static bool Prefix() => !KeyboardUiPointer.OwnsFocus;
    }

    [HarmonyPatch(typeof(UI_Cursor), "LateUpdate")]
    internal static class KeyboardCursorVisibilityPatch
    {
        private static void Postfix() => KeyboardUiPointer.UpdateCursor();
    }

    [HarmonyPatch(typeof(UI_NewItemPicker_Controller), "Update")]
    internal static class KeyboardCarriedItemPositionPatch
    {
        private static void Postfix(UI_NewItemPicker_Controller __instance) =>
            KeyboardUiPointer.PositionCarriedItem(__instance);
    }

    [HarmonyPatch(typeof(UI_MapPanel), "Update")]
    internal static class KeyboardMapSelectionPositionPatch
    {
        private static void Postfix(UI_MapPanel __instance, UI_Map ___currentMap) =>
            KeyboardUiPointer.CenterMapSelection(__instance, ___currentMap);
    }
}
