using System.Collections.Generic;
using HarmonyLib;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SephiriaEnhancements.KeyboardUiNavigation
{
    internal static class RewardKeyboardNavigation
    {
        // Use the native reward order, including dynamically generated entries.
        private static readonly AccessTools.FieldRef<UI_SephiriteRewardPanel,
            List<UI_SephiriteRewardElement>> RewardElements =
                AccessTools.FieldRefAccess<UI_SephiriteRewardPanel,
                    List<UI_SephiriteRewardElement>>("rewardElements");

        internal static void RequestFirstReward(UI_SephiriteRewardPanel panel)
        {
            if (!KeyboardUiNavigationController.IsKeyboardModeActive() ||
                UIManager.Instance?.GetElement<UI_NewItemPicker_Controller>()?.CurrentAny == true)
                return;
            List<UI_SephiriteRewardElement> rewards = RewardElements(panel);
            if (rewards.Count > 0 && rewards[0] != null)
                KeyboardUiNavigationController.RequestSelection(panel, rewards[0].gameObject);
        }

        internal static void SelectFirstInventorySlot(UI_SephiriteRewardElement reward)
        {
            if (!KeyboardUiNavigationController.IsKeyboardModeActive() ||
                reward?.parentPanel == null || !reward.parentPanel.IsControlEnabled ||
                !reward.parentPanel.IsOpened || EventSystem.current == null)
                return;
            KeyboardUiNavigationController.CancelSelection(reward.parentPanel);
            UI_CharacterStatusPanel inventory =
                UIManager.Instance?.GetElement<UI_CharacterStatusPanel>();
            if (inventory == null || !inventory.IsOpened || !inventory.IsControlEnabled)
                return;
            UI_NewInventoryIcon slot = inventory.GetItemIcon(new ItemPosition(0, 0));
            if (slot == null || !slot.gameObject.activeInHierarchy) return;
            Selectable selectable = slot.GetComponent<Selectable>();
            if (selectable != null && !selectable.IsInteractable()) return;
            // Picking has already succeeded. Move focus only; placement still
            // requires a separate native submit on the chosen inventory slot.
            EventSystem.current.SetSelectedGameObject(slot.gameObject);
        }
    }

    [HarmonyPatch(typeof(UI_SephiriteRewardPanel), "GenerateIcon")]
    internal static class RewardKeyboardGeneratedSelectionPatch
    {
        private static void Postfix(UI_SephiriteRewardPanel __instance) =>
            RewardKeyboardNavigation.RequestFirstReward(__instance);
    }

    [HarmonyPatch(typeof(UIBase), nameof(UIBase.Enable))]
    internal static class RewardKeyboardControlSelectionPatch
    {
        private static void Postfix(UIBase __instance)
        {
            if (__instance is UI_SephiriteRewardPanel reward)
                RewardKeyboardNavigation.RequestFirstReward(reward);
        }
    }

    [HarmonyPatch(typeof(UI_NewItemPicker_Controller),
        nameof(UI_NewItemPicker_Controller.PickSephiriteReward))]
    internal static class RewardKeyboardPlacementSelectionPatch
    {
        private static void Postfix(UI_NewItemPicker_Controller __instance,
            UI_SephiriteRewardElement instance)
        {
            if (instance != null && __instance.CurrentSephiriteReward == instance)
                RewardKeyboardNavigation.SelectFirstInventorySlot(instance);
        }
    }
}
