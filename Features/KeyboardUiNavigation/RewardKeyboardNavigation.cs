using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using UnityEngine.EventSystems;

namespace SephiriaEnhancements.KeyboardUiNavigation
{
    internal static class RewardKeyboardNavigation
    {
        // Use the native reward order, including dynamically generated entries.
        private static readonly AccessTools.FieldRef<UI_SephiriteRewardPanel,
            List<UI_SephiriteRewardElement>> RewardElements =
                AccessTools.FieldRefAccess<UI_SephiriteRewardPanel,
                    List<UI_SephiriteRewardElement>>("rewardElements");

        private static UI_SephiriteRewardElement lastBrowsedReward;

        internal static void Reset() => lastBrowsedReward = null;

        internal static void RememberReward(GameObject selected)
        {
            UI_SephiriteRewardElement reward = selected?.GetComponent<UI_SephiriteRewardElement>();
            if (reward != null && reward.parentPanel != null &&
                RewardElements(reward.parentPanel).Contains(reward))
                lastBrowsedReward = reward;
        }

        internal static GameObject FindRememberedReward(UI_SephiriteRewardPanel panel)
        {
            if (lastBrowsedReward == null || lastBrowsedReward.parentPanel != panel ||
                !RewardElements(panel).Contains(lastBrowsedReward) ||
                !KeyboardUiSelection.IsInPanel(panel, lastBrowsedReward.gameObject))
                return null;
            return lastBrowsedReward.gameObject;
        }

        internal static bool TryCancelCarriedReward()
        {
            if (!KeyboardUiNavigationController.WasNativeUiActionPressed(
                    UIInputModule.currentModule?.cancel))
                return false;
            UI_NewItemPicker_Controller picker =
                UIManager.Instance?.GetElement<UI_NewItemPicker_Controller>();
            UI_SephiriteRewardElement reward = picker?.CurrentSephiriteReward;
            if (reward == null || reward.parentPanel == null ||
                !RewardElements(reward.parentPanel).Contains(reward) ||
                !KeyboardUiSelection.IsInControlStack(reward.gameObject))
                return false;

            // A dialog or other menu must keep ownership of its cancel action.
            foreach (UIBase panel in UIManager.Instance.CurrentControlStack)
                if (panel != reward.parentPanel && !(panel is UI_CharacterStatusPanel))
                    return false;
            if (EventSystem.current == null) return false;

            KeyboardUiNavigationController.CancelSelection(reward.parentPanel);
            picker.PickSephiriteReward(null);
            lastBrowsedReward = reward;
            EventSystem.current.SetSelectedGameObject(reward.gameObject);
            return true;
        }

        internal static void RequestFirstReward(UI_SephiriteRewardPanel panel)
        {
            if (panel == null || !panel.IsOpened ||
                !KeyboardUiNavigationController.IsKeyboardModeActive() ||
                UIManager.Instance?.GetElement<UI_NewItemPicker_Controller>()?.CurrentAny == true)
                return;
            List<UI_SephiriteRewardElement> rewards = RewardElements(panel);
            if (rewards.Count > 0)
                KeyboardUiNavigationController.RequestSelection(panel,
                    rewards[0] != null ? rewards[0].gameObject : null);
        }

        internal static GameObject FindFirstEmptyInventorySlot(UI_CharacterStatusPanel inventory)
        {
            if (!KeyboardUiSelection.IsPanelReady(inventory)) return null;
            GridInventory items = inventory.GetItemIcon(new ItemPosition(0, 0))?.Inventory;
            if (items == null) return null;
            // Native storage indices run left to right, then top to bottom.
            // Only unlocked main-backpack slots are placement destinations.
            for (int index = 0; index < items.CurrentInventoryStorage; index++)
            {
                UI_NewInventoryIcon slot = inventory.GetItemIcon(items.IdxToPos(index));
                if (slot != null && slot.Item == null &&
                    KeyboardUiSelection.IsInPanel(inventory, slot.gameObject))
                    return slot.gameObject;
            }
            return null;
        }
    }

    [HarmonyPatch(typeof(UIBase), nameof(UIBase.Open))]
    internal static class RewardKeyboardInventoryOpenedSelectionPatch
    {
        private static void Postfix(UIBase __instance)
        {
            // The backpack opens after reward generation and combines control.
            // Seed entry again once that transition has finished.
            if (__instance is UI_CharacterStatusPanel)
                RewardKeyboardNavigation.RequestFirstReward(
                    UIManager.Instance?.GetElement<UI_SephiriteRewardPanel>());
        }
    }

    [HarmonyPatch(typeof(UI_SephiriteRewardPanel), "GenerateIcon")]
    internal static class RewardKeyboardGeneratedSelectionPatch
    {
        private static void Postfix(UI_SephiriteRewardPanel __instance)
        {
            RewardKeyboardNavigation.Reset();
            RewardKeyboardNavigation.RequestFirstReward(__instance);
        }
    }

    [HarmonyPatch(typeof(UI_SephiriteRewardPanel), nameof(UI_SephiriteRewardPanel.OnClosed))]
    internal static class RewardKeyboardClosedSelectionPatch
    {
        private static void Postfix() => RewardKeyboardNavigation.Reset();
    }

    [HarmonyPatch(typeof(UIInputModule), "Update")]
    internal static class RewardKeyboardCancelSelectionPatch
    {
        private static bool Prefix(UIInputModule __instance) =>
            __instance != UIInputModule.current || !RewardKeyboardNavigation.TryCancelCarriedReward();
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
    internal static class RewardKeyboardToggleSelectionPatch
    {
        private static void Prefix(UI_NewItemPicker_Controller __instance,
            ref UI_SephiriteRewardElement instance)
        {
            if (instance == null ||
                EventSystem.current?.currentSelectedGameObject != instance.gameObject ||
                !KeyboardUiSelection.IsInControlStack(instance.gameObject) ||
                !KeyboardUiNavigationController.WasNativeUiActionPressed(
                    UIInputModule.currentModule?.submit))
                return;

            KeyboardUiNavigationController.CancelSelection(instance.parentPanel);
            // Re-submit at the carried reward's source uses native cancellation,
            // including clearing its icon and rotation. Other rewards still pick.
            if (__instance.CurrentSephiriteReward == instance)
                instance = null;
        }
    }
}
