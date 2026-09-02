#nullable disable
using HarmonyLib;

using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace SephiriaEnhancements.Runtime.GameBridge.Inventory
{
    internal sealed class NativeInventoryItemSelectionView : IDisposable
    {
        private static readonly FieldInfo ItemIconMergedDatasField =
            AccessTools.Field(typeof(UI_CharacterStatusPanel),
                "itemIconMergedDatas");
        private static NativeInventoryItemSelectionView active;

        private readonly Dictionary<Canvas, bool> canvasStates = new();
        private UI_CharacterStatusPanel panel;
        private GridInventory inventory;
        private GameObject cover;
        private Func<NewItemOwnInstance, bool> isSelectable;
        private bool inventoryWasPickable;

        internal bool IsVisible => active == this && panel != null &&
            cover != null;

        internal bool TryShow(UI_CharacterStatusPanel target,
            Func<NewItemOwnInstance, bool> selectable)
        {
            if (target == null || selectable == null || !target.IsOpened ||
                target.InventoryMode !=
                    UI_CharacterStatusPanel.EInventoryMode.None ||
                target.inventoryCover == null || ItemIconMergedDatasField == null ||
                active != null && active != this)
            {
                return false;
            }

            if (panel != target)
            {
                Hide();
            }
            panel = target;
            inventory = target.PlayerAvatar?.Inventory;
            if (inventory == null)
            {
                Hide();
                return false;
            }

            isSelectable = selectable;
            if (cover == null)
            {
                cover = UnityEngine.Object.Instantiate(
                    target.inventoryCover.gameObject,
                    target.inventoryCover.parent, false);
                cover.name = "Sephiria Enhancements — Inventory Item Selection";
            }
            cover.transform.SetAsLastSibling();
            cover.SetActive(true);

            inventoryWasPickable = inventory.IsPickable;
            inventory.IsPickable = false;
            active = this;
            Refresh();
            return IsVisible;
        }

        internal bool Refresh()
        {
            if (!IsVisible)
            {
                return false;
            }
            if (!panel.IsOpened || panel.InventoryMode !=
                    UI_CharacterStatusPanel.EInventoryMode.None ||
                panel.PlayerAvatar?.Inventory != inventory)
            {
                Hide(panel.InventoryMode ==
                    UI_CharacterStatusPanel.EInventoryMode.None);
                return false;
            }

            if (!(ItemIconMergedDatasField.GetValue(panel) is
                    List<UI_InventoryIconMergedData> mergedDatas))
            {
                Hide();
                return false;
            }

            foreach (UI_InventoryIconMergedData data in mergedDatas)
            {
                Canvas itemCanvas = data?.itemIconCanvas;
                UI_NewInventoryIcon icon = data?.icon;
                if (itemCanvas == null || icon == null)
                {
                    continue;
                }
                if (!canvasStates.ContainsKey(itemCanvas))
                {
                    canvasStates.Add(itemCanvas, itemCanvas.overrideSorting);
                }
                NewItemOwnInstance item = icon.Showing ? icon.Item : null;
                itemCanvas.overrideSorting = item != null && isSelectable(item);
            }
            cover.transform.SetAsLastSibling();
            return true;
        }

        internal void Hide()
        {
            Hide(restoreNativeState: true);
        }

        private void Hide(bool restoreNativeState)
        {
            if (restoreNativeState)
            {
                foreach ((Canvas itemCanvas, bool overrideSorting) in
                    canvasStates)
                {
                    if (itemCanvas != null)
                    {
                        itemCanvas.overrideSorting = overrideSorting;
                    }
                }
                if (inventory != null)
                {
                    inventory.IsPickable = inventoryWasPickable;
                }
            }
            canvasStates.Clear();
            if (cover != null)
            {
                UnityEngine.Object.Destroy(cover);
            }
            cover = null;
            panel = null;
            inventory = null;
            isSelectable = null;
            if (active == this)
            {
                active = null;
            }
        }

        internal static void EndBeforeNativeModeChange(
            UI_CharacterStatusPanel target)
        {
            if (active?.panel == target)
            {
                active.Hide();
            }
        }

        public void Dispose()
        {
            Hide();
        }
    }

    [HarmonyPatch(typeof(UI_CharacterStatusPanel),
        nameof(UI_CharacterStatusPanel.SetInventoryMode))]
    internal static class NativeInventoryItemSelectionModePatch
    {
        private static void Prefix(UI_CharacterStatusPanel __instance)
        {
            NativeInventoryItemSelectionView.EndBeforeNativeModeChange(
                __instance);
        }
    }
}
