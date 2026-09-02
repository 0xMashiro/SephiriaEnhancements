using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using UnityEngine.EventSystems;

namespace SephiriaEnhancements.KeyboardUiNavigation
{
    [HarmonyPatch(typeof(UIBase), nameof(UIBase.Open))]
    internal static class MessageBoxKeyboardInitialSelectionPatch
    {
        private static void Postfix(UIBase __instance)
        {
            if (__instance is UI_MessageBox messageBox)
            {
                // Keyboard&Mouse deliberately skips UIBase's native default
                // selection. Seed focus on the next frame so Navigate and Submit
                // can use the message box's existing Selectable wiring.
                KeyboardUiNavigationController.RequestSelection(messageBox,
                    messageBox.defaultSelectable);
            }
        }
    }

    [HarmonyPatch(typeof(UI_MessageBoxHolder),
        nameof(UI_MessageBoxHolder.OnlyActiveLastSibling))]
    internal static class MessageBoxKeyboardRestoredSelectionPatch
    {
        private static void Postfix(List<UI_MessageBox> ___openedBoxes)
        {
            if (___openedBoxes == null || ___openedBoxes.Count == 0)
            {
                return;
            }

            UI_MessageBox top = ___openedBoxes[___openedBoxes.Count - 1];
            if (top != null)
            {
                // Re-establish focus after a nested box closes and the previous
                // box becomes the interactive top sibling again.
                KeyboardUiNavigationController.RequestSelection(top,
                    top.defaultSelectable);
            }
        }
    }

    [HarmonyPatch(typeof(ControlsChangeHandler), "Update")]
    internal static class OptionsKeyboardEmptyFocusPatch
    {
        private static bool Prefix()
        {
            ControlsChangeHandler controls = ControlsChangeHandler.Current;
            if (controls?.PlayerInput == null ||
                controls.PlayerInput.currentControlScheme !=
                    PlayerInputController.KeyboardAndMouseScheme ||
                EventSystem.current?.currentSelectedGameObject != null)
            {
                return true;
            }

            UIManager manager = UIManager.Instance;
            return manager?.CurrentControlStack == null ||
                manager.CurrentControlStack.Count == 0 ||
                !(manager.CurrentControlStack[0] is UI_OptionsPanel);
        }
    }

    [HarmonyPatch(typeof(ControlsChangeHandler),
        nameof(ControlsChangeHandler.HandleOnControlsChanged))]
    internal static class KeyboardControlsChangedPatch
    {
        private static void Postfix(ControlsChangeHandler __instance)
        {
            KeyboardUiNavigationController.ApplyNativeSelectionPolicy(__instance);
        }
    }

    [HarmonyPatch(typeof(UI_ItemIcon), nameof(UI_ItemIcon.ClickButton))]
    internal static class ItemIconKeyboardSubmitPatch
    {
        private static readonly AccessTools.FieldRef<UI_ItemIcon,
            Action<PointerEventData.InputButton, UI_ItemIcon>> ClickHandler =
            AccessTools.FieldRefAccess<UI_ItemIcon,
                Action<PointerEventData.InputButton, UI_ItemIcon>>("OnClick");

        private static void Postfix(UI_ItemIcon __instance, bool ___ignoreOnClick)
        {
            if (___ignoreOnClick || EventSystem.current == null ||
                EventSystem.current.currentSelectedGameObject !=
                    __instance.gameObject || UIInputModule.currentModule == null ||
                !KeyboardUiNavigationController.WasNativeUiActionPressed(
                    UIInputModule.currentModule.submit))
            {
                return;
            }

            Action<PointerEventData.InputButton, UI_ItemIcon> handler =
                ClickHandler(__instance);
            handler?.Invoke(PointerEventData.InputButton.Left, __instance);
        }
    }

    [HarmonyPatch(typeof(UI_ItemBoxPanel), "Update")]
    internal static class ItemBoxKeyboardSecondaryActionPatch
    {
        private static void Postfix(UI_ItemBoxPanel __instance)
        {
            if (UIInputModule.current == null || EventSystem.current == null ||
                !KeyboardUiNavigationController.WasNativeUiActionPressed(
                    UIInputModule.current.rotateItemControlAction))
            {
                return;
            }

            UI_ItemIcon icon = EventSystem.current.currentSelectedGameObject?
                .GetComponent<UI_ItemIcon>();
            if (icon != null && icon.transform.IsChildOf(__instance.transform))
            {
                __instance.SetToggleFavorite(icon);
            }
        }
    }

    [HarmonyPatch(typeof(UI_TreeShopPanel), "Update")]
    internal static class TreeShopKeyboardSecondaryActionPatch
    {
        private static void Postfix(UI_TreeShopPanel __instance,
            UI_TreeShopItem ___currentSelected, ref bool ___isPurchasing)
        {
            if (___currentSelected == null || UIInputModule.current == null ||
                !KeyboardUiNavigationController.WasNativeUiActionPressed(
                    UIInputModule.current.throwItemControlAction) ||
                (UIInputModule.currentModule != null &&
                    UIInputModule.currentModule.middleClick.action.
                        WasPressedThisFrame()) ||
                ___currentSelected.connected.behaviour !=
                    TreeShopItemEntity.EBehaviour.UnlockItem)
            {
                return;
            }

            __instance.defaultSelectable =
                EventSystem.current?.currentSelectedGameObject;
            UIManager.Instance.GetElement<UI_UnlockItemPreviewer>().Open(
                ___currentSelected.connected.items.ToList());
            ___isPurchasing = false;
        }
    }
}
