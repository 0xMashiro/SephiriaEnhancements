using System.Linq;
using HarmonyLib;
using Mirror;
using SephiriaEnhancements.Integration;
using SephiriaEnhancements.KeyboardUiNavigation;
using UnityEngine;

namespace SephiriaEnhancements.ItemCommunication.Integration
{
    internal sealed class NativeItemCommunicationTarget
    {
        private static readonly AccessTools.FieldRef<UI_SephiriteRewardPanel, PlayerAvatar> RewardOwner =
            AccessTools.FieldRefAccess<UI_SephiriteRewardPanel, PlayerAvatar>("openedAvatar");
        private static readonly AccessTools.FieldRef<UI_SephiriteRewardPanel, System.Collections.Generic.List<UI_SephiriteRewardElement>> Rewards =
            AccessTools.FieldRefAccess<UI_SephiriteRewardPanel, System.Collections.Generic.List<UI_SephiriteRewardElement>>("rewardElements");

        internal UIBase Panel;
        internal GameObject Source;
        internal ItemEntity Item;
        internal UI_ShopPanel Shop;
        private object identity;
        private NewItemOwnInstance inventoryItem;

        internal static bool Ready(PlayerAvatar player)
        {
            var manager = UIManager.Instance;
            if (!Application.isFocused || !NetworkClient.active || !NetworkClient.ready ||
                player == null || player.loadingScreenType != -1 || manager == null || manager.IsHidden ||
                ScreenFader.Instance?.IsFading == true ||
                manager.GetElement<UI_ChatInput>()?.IsOpened == true ||
                manager.GetElement<UI_NewItemPicker>()?.CurrentAny == true ||
                manager.GetElement<UI_NewItemPicker_Controller>()?.CurrentAny == true) return false;
            var stack = manager.CurrentControlStack;
            return stack != null && stack.Count > 0 && stack.All(panel =>
                panel is UI_SephiriteRewardPanel || panel is UI_CharacterStatusPanel || panel is UI_ShopPanel);
        }

        internal static NativeItemCommunicationTarget Read(GameObject source)
        {
            PlayerAvatar player = LocalPlayerResolver.Resolve();
            if (!Ready(player) || source == null || !source.activeInHierarchy) return null;
            var reward = source.GetComponentInParent<UI_SephiriteRewardElement>();
            if (reward != null)
            {
                var panel = reward.parentPanel;
                if (!KeyboardUiSelection.IsInPanel(panel, reward.gameObject) ||
                    !UIManager.Instance.CurrentControlStack.Contains(panel) ||
                    RewardOwner(panel) != player || !Rewards(panel).Contains(reward)) return null;
                var item = ItemDatabase.FindItemById(reward.reward.entityID);
                return item == null ? null : new NativeItemCommunicationTarget
                { Panel = panel, Source = reward.gameObject, Item = item, identity = reward.reward };
            }
            var shop = UIManager.Instance.GetElement<UI_ShopPanel>();
            if (shop == null || shop.BuyerCharacter != player || shop.ShopCharacter == null || shop.Buyer == null || shop.Shop == null ||
                !KeyboardUiSelection.IsPanelReady(shop) || !UIManager.Instance.CurrentControlStack.Contains(shop)) return null;
            var icon = source.GetComponentInParent<UI_NewInventoryIcon>();
            if (icon != null && KeyboardUiSelection.IsInPanel(shop, icon.gameObject) &&
                icon.Inventory == shop.Shop && shop.shopInventoryIconList.Contains(icon) && icon.Item != null)
                return new NativeItemCommunicationTarget
                { Panel = shop, Shop = shop, Source = icon.gameObject, Item = icon.Item.Entity, identity = icon.Item, inventoryItem = icon.Item };
            var replenishment = source.GetComponentInParent<UI_ReplenishmentIcon>();
            if (replenishment == null || !KeyboardUiSelection.IsInPanel(shop, replenishment.gameObject) ||
                !shop.replenishmentIcons.Contains(replenishment) ||
                replenishment.Shop == null || replenishment.Shop.gameObject != shop.ShopCharacter.gameObject ||
                replenishment.ReplenishmentIdx < 0 || replenishment.ReplenishmentIdx >= replenishment.Shop.replenishments.Count)
                return null;
            var stock = replenishment.Shop.replenishments[replenishment.ReplenishmentIdx];
            if (stock == null || stock.purchased || replenishment.Entity == null || stock.entityID != replenishment.Entity.id)
                return null;
            return new NativeItemCommunicationTarget
            { Panel = shop, Shop = shop, Source = replenishment.gameObject, Item = replenishment.Entity, identity = stock };
        }

        internal bool IsCurrent()
        {
            var current = Read(Source);
            return current != null && current.Panel == Panel && current.Item == Item && Equals(current.identity, identity);
        }

        internal string Quote()
        {
            if (Shop.Buyer.TryGetTradeVoucher(out _))
                return Configuration.ModLocalization.Get(ItemCommunicationLocalization.Voucher);
            int price = ItemDatabase.GetItemBuyPrice(Item, Shop.ShopCharacter.GetCustomStat(ECustomStat.Negotiation),
                Shop.BuyerCharacter.GetCustomStat(ECustomStat.Negotiation));
            return price + " " + Shop.goldNameText.ToString();
        }

        internal string Categories
        {
            get
            {
                if (Item.type != EItemType.Charm) return "";
                var categories = inventoryItem?.Charm?.GetItemCategory();
                if (categories == null)
                {
                    // A prototype with computed categories needs a live instance; do not guess.
                    if (Item.resourcePrefab == null || !Item.resourcePrefab.TryGetComponent<Charm_Basic>(out var artifact) ||
                        artifact.GetType().GetMethod(nameof(Charm_Basic.GetItemCategory)).DeclaringType != typeof(Charm_Basic)) return "";
                    categories = Item.categories;
                }
                return string.Join(", ", categories.Select(ItemDatabase.FindItemCategory)
                    .Where(category => category != null && category.isEnabled).Select(category => category.Name));
            }
        }
    }
}
