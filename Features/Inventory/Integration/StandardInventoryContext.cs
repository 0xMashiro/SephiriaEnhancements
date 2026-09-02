#nullable disable
using SephiriaEnhancements.Runtime.Inventory;

using TMPro;
using SephiriaEnhancements.Integration;
using UnityEngine;

namespace SephiriaEnhancements.Inventory
{
    internal sealed class StandardInventoryViewContext
    {
        internal RectTransform InventoryZone;
        internal TextMeshProUGUI TextTemplate;
        internal Canvas Canvas;
        internal NativeInventoryOptimizationViewTemplates ViewTemplates;
        internal UI_CharacterStatusPanel Panel;
    }

    internal static class StandardInventoryContext
    {
        internal static bool TryGetOpenInventory(out GridInventory inventory)
        {
            return TryGetOpenInventory(out inventory,
                out UI_CharacterStatusPanel _);
        }

        internal static bool TryGetOpenInventory(out GridInventory inventory,
            out UI_CharacterStatusPanel panel)
        {
            bool open = TryGetOpenPanel(out panel);
            PlayerAvatar player = panel?.PlayerAvatar;
            inventory = player?.Inventory;
            return open && player != null &&
                LocalPlayerResolver.IsLocal(player) && inventory != null &&
                inventory.CurrentInventoryStorage > 1;
        }

        internal static bool TryGetOpenView(
            out StandardInventoryViewContext context)
        {
            context = null;
            if (!TryGetOpenPanel(out UI_CharacterStatusPanel panel) ||
                !NativeInventoryOptimizationViewTemplateResolver.TryResolve(
                    panel, out NativeInventoryOptimizationViewTemplates
                        templates))
            {
                return false;
            }
            RectTransform inventoryZone = panel.inventoryZone;
            TextMeshProUGUI textTemplate = panel.selectItemScreenText ??
                panel.GetComponentInChildren<TextMeshProUGUI>(true);
            Canvas canvas = inventoryZone?.GetComponentInParent<Canvas>();
            if (inventoryZone == null || textTemplate?.font == null ||
                canvas?.rootCanvas?.transform is not RectTransform)
            {
                return false;
            }
            context = new StandardInventoryViewContext
            {
                InventoryZone = inventoryZone,
                TextTemplate = textTemplate,
                Canvas = canvas,
                ViewTemplates = templates,
                Panel = panel
            };
            return true;
        }

        internal static bool TryGetOpenPanel(
            out UI_CharacterStatusPanel panel)
        {
            UIManager manager = UIManager.Instance;
            panel = manager?.GetElement<UI_CharacterStatusPanel>();
            if (panel == null || !panel.IsOpened ||
                panel.InventoryMode !=
                    UI_CharacterStatusPanel.EInventoryMode.None)
            {
                return false;
            }

            // Native integration boundary: these companion panels open the
            // character inventory in a shifted contextual layout while leaving
            // EInventoryMode at None. That state is not the ordinary inventory
            // and must never expose or execute inventory optimization.
            return manager.GetElement<UI_SephiriteRewardPanel>()?.IsOpened !=
                    true &&
                manager.GetElement<UI_ShopPanel>()?.IsOpened != true &&
                manager.GetElement<UI_MysticPotPanel>()?.IsOpened != true &&
                manager.GetElement<UI_TabletMixPanel>()?.IsOpened != true &&
                manager.GetElement<UI_ItemBoxPanel>()?.IsOpened != true;
        }
    }
}
