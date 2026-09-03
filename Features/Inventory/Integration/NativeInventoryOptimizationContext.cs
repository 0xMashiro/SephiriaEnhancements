#nullable disable
using SephiriaEnhancements.Integration;

namespace SephiriaEnhancements.Inventory
{
    internal static class NativeInventoryOptimizationContext
    {
        internal static bool TryGetOpenInventory(out GridInventory inventory)
        {
            bool open = TryGetOpenPanel(out UI_CharacterStatusPanel panel);
            PlayerAvatar player = panel?.PlayerAvatar;
            inventory = player?.Inventory;
            return open && player != null &&
                LocalPlayerResolver.IsLocal(player) && inventory != null &&
                inventory.CurrentInventoryStorage > 1 && inventory.IsPickable;
        }

        internal static bool TryGetOpenPanel(out UI_CharacterStatusPanel panel)
        {
            panel = UIManager.Instance?.GetElement<UI_CharacterStatusPanel>();
            // Reward and shop layouts still allow ordinary inventory moves.
            // Native selection modes reserve the inventory for another action.
            return panel != null && panel.IsOpened &&
                (panel.CanvasGroup == null || panel.CanvasGroup.interactable) &&
                panel.InventoryMode == UI_CharacterStatusPanel.EInventoryMode.None;
        }
    }
}
