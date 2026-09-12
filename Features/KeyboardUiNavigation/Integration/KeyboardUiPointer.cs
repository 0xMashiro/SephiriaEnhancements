using SephiriaEnhancements.Runtime;
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
            bool available = FeatureFailure.IsAvailable(FeatureId.KeyboardUiNavigation) &&
                KeyboardUiNavigationController.IsKeyboardModeActive() &&
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
            // Opening menus, changing tabs and typing are keyboard interaction too.
            // The native Keyboard&Mouse scheme alone cannot distinguish them from hover.
            bool navigationPressed = UIInputModule.currentModule?.move?.action?
                .WasPressedThisFrame() == true;
            bool keyboardPressed = Keyboard.current?.anyKey.wasPressedThisFrame == true;
            bool submitPressed = UIInputModule.currentModule?.submit?.action?.WasPressedThisFrame() == true;
            Ownership.Update(available, navigationPressed || submitPressed || keyboardPressed,
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
            bool hide = SelectedTarget() != null || CombatTargeting.CombatTargetingController.HidesPointer;
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
            if (MapEnhancements.MapEnhancementsController.HasMapNavigation) return;
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
        private static void Prefix()
        {
            if (!FeatureFailure.IsAvailable(FeatureId.KeyboardUiNavigation))
            {
                return;
            }

            try
            {
                PrefixCore();
            }
            catch (System.Exception exception)
            {
                FeatureFailure.Disable(FeatureId.KeyboardUiNavigation, exception);
                return;
            }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static void PrefixCore() => KeyboardUiPointer.RefreshInput();
    }

    [HarmonyPatch]
    internal static class KeyboardPointerHoverPatch
    {
        private static IEnumerable<MethodBase> TargetMethods()
        {
            // Intercept native hover dispatch, including controls that do not derive
            // from shared game buttons. Click, drag and scroll keep their native path.
            foreach (Type handler in new[] { typeof(IPointerEnterHandler), typeof(IPointerExitHandler),
                typeof(IPointerMoveHandler) })
            {
                yield return AccessTools.Method(typeof(ExecuteEvents), "Execute", new[] { handler, typeof(BaseEventData) });
            }
        }

        private static bool Prefix(MethodBase __originalMethod, out GameObject __state)
        {
            __state = null;
            if (!FeatureFailure.IsAvailable(FeatureId.KeyboardUiNavigation))
            {
                return true;
            }

            try
            {
                return PrefixCore(__originalMethod, out __state);
            }
            catch (System.Exception exception)
            {
                FeatureFailure.Disable(FeatureId.KeyboardUiNavigation, exception);
                return true;
            }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static bool PrefixCore(MethodBase method, out GameObject selection)
        {
            selection = null;
            if (!KeyboardUiPointer.OwnsFocus) return true;
            if (method.GetParameters()[0].ParameterType != typeof(IPointerExitHandler)) return false;
            // Exit still clears hover visuals and tooltips; it must not clear
            // the keyboard selection when layout moves under a stationary mouse.
            selection = EventSystem.current?.currentSelectedGameObject;
            return true;
        }

        private static void Postfix(GameObject __state)
        {
            if (!FeatureFailure.IsAvailable(FeatureId.KeyboardUiNavigation)) return;
            try
            {
                PostfixCore(__state);
            }
            catch (System.Exception exception)
            {
                FeatureFailure.Disable(FeatureId.KeyboardUiNavigation, exception);
            }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static void PostfixCore(GameObject selection)
        {
            if (selection != null && EventSystem.current != null &&
                EventSystem.current.currentSelectedGameObject != selection)
                EventSystem.current.SetSelectedGameObject(selection);
        }
    }

    [HarmonyPatch(typeof(UI_Cursor), "LateUpdate")]
    internal static class KeyboardCursorVisibilityPatch
    {
        private static void Postfix()
        {
            if (!FeatureFailure.IsAvailable(FeatureId.KeyboardUiNavigation))
            {
                return;
            }

            try
            {
                PostfixCore();
            }
            catch (System.Exception exception)
            {
                FeatureFailure.Disable(FeatureId.KeyboardUiNavigation, exception);
                return;
            }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static void PostfixCore() => KeyboardUiPointer.UpdateCursor();
    }

    [HarmonyPatch(typeof(UI_NewItemPicker_Controller), "Update")]
    internal static class KeyboardCarriedItemPositionPatch
    {
        private static void Postfix(UI_NewItemPicker_Controller __instance)
        {
            if (!FeatureFailure.IsAvailable(FeatureId.CharacterPanelNavigation))
            {
                return;
            }

            try
            {
                PostfixCore(__instance);
            }
            catch (System.Exception exception)
            {
                FeatureFailure.Disable(FeatureId.CharacterPanelNavigation, exception);
                return;
            }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static void PostfixCore(UI_NewItemPicker_Controller __instance) => KeyboardUiPointer.PositionCarriedItem(__instance);
    }

    [HarmonyPatch(typeof(UI_MapPanel), "Update")]
    internal static class KeyboardMapSelectionPositionPatch
    {
        private static void Postfix(UI_MapPanel __instance, UI_Map ___currentMap)
        {
            if (!FeatureFailure.IsAvailable(FeatureId.KeyboardUiNavigation))
            {
                return;
            }

            try
            {
                PostfixCore(__instance, ___currentMap);
            }
            catch (System.Exception exception)
            {
                FeatureFailure.Disable(FeatureId.KeyboardUiNavigation, exception);
                return;
            }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static void PostfixCore(UI_MapPanel __instance, UI_Map ___currentMap) => KeyboardUiPointer.CenterMapSelection(__instance, ___currentMap);
    }
}
