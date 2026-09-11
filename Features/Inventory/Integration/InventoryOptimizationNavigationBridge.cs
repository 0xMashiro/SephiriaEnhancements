using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace SephiriaEnhancements.Inventory
{
    // Native API boundary: only override the horizontal edge leading to the
    // optimizer. Preserve every other native link, including locked links.
    internal sealed class InventoryOptimizationNavigationBridge
    {
        private static readonly AccessTools.FieldRef<UI_HorayButton, Selectable> Left =
            AccessTools.FieldRefAccess<UI_HorayButton, Selectable>("forceNavLeft");
        private static readonly AccessTools.FieldRef<UI_HorayButton, Selectable> Right =
            AccessTools.FieldRefAccess<UI_HorayButton, Selectable>("forceNavRight");
        private readonly List<(UI_HorayButton Button, bool Right, Selectable Original, Selectable Installed)> links = new();

        internal void Refresh(UI_CharacterStatusPanel panel, RectTransform root,
            UI_HorayButton entry, UI_HorayButton returnTarget, bool allowReturnFromEntry = true)
        {
            Clear();
            if (entry == null || !entry.isActiveAndEnabled || !entry.IsInteractable()) return;
            GridInventory inventory = panel.PlayerAvatar.Inventory;
            bool right = root.TransformPoint(root.rect.center).x >=
                panel.inventoryZone.TransformPoint(panel.inventoryZone.rect.center).x;
            // Use native storage coordinates, including a partially filled last
            // row. Rect sizes and world positions use different units under UI scaling.
            for (int start = 0; start < inventory.CurrentInventoryStorage; start += inventory.Width)
            {
                int index = right ? System.Math.Min(start + inventory.Width,
                    inventory.CurrentInventoryStorage) - 1 : start;
                UI_HorayButton cell = panel.GetItemIcon(inventory.IdxToPos(index))?.button;
                if (cell == null || !cell.isActiveAndEnabled || !cell.IsInteractable()) continue;
                Link(cell, right, entry);
                if (returnTarget == null) returnTarget = cell;
            }
            if (allowReturnFromEntry && returnTarget != null) Link(entry, !right, returnTarget);
        }

        private void Link(UI_HorayButton button, bool right, Selectable target)
        {
            var field = right ? Right : Left;
            links.Add((button, right, field(button), target));
            field(button) = target;
        }

        internal void Clear()
        {
            foreach (var link in links)
                if (link.Button != null)
                {
                    var field = link.Right ? Right : Left;
                    if (ReferenceEquals(field(link.Button), link.Installed))
                        field(link.Button) = link.Original != null ? link.Original : null;
                }
            links.Clear();
        }
    }
}
