using System;
using SephiriaEnhancements.Configuration;
using SephiriaEnhancements.Integration;
using SephiriaEnhancements.Runtime;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SephiriaEnhancements.ItemCommunication.Integration
{
    // This strip belongs to the currently controlled reward/shop panel, not a tooltip.
    internal sealed class NativeItemCommunicationView : IDisposable
    {
        internal UIBase Panel { get; }
        internal Selectable Entry => primary;
        private readonly RectTransform root;
        private readonly RectTransform details;
        private readonly UI_HorayButton primary, share;
        private readonly TextMeshProUGUI itemName, help, template;
        private GameObject returnSelection;
        internal Selectable FollowingControl;
        private readonly bool shop;

        internal NativeItemCommunicationView(UIBase panel, Action<ItemCommunicationIntent> send)
        {
            Panel = panel; shop = panel is UI_ShopPanel;
            var dialog = UIManager.Instance.GetElement<UI_MessageBoxHolder>().yesNoPrefab;
            var buttonTemplate = (UI_HorayButton)dialog.yesButton;
            template = buttonTemplate.GetComponentInChildren<TextMeshProUGUI>(true);
            root = Rect("ItemCommunication", panel.transform);
            root.anchorMin = root.anchorMax = new Vector2(shop ? .25f : .5f, 0);
            root.pivot = new Vector2(.5f, 0);
            // The shop's native negotiation bar begins above this bottom row.
            root.sizeDelta = new Vector2(shop ? 240 : 200, 24);
            root.anchoredPosition = new Vector2(0, 4);
            var background = root.gameObject.AddComponent<Image>();
            var source = dialog.GetComponent<Image>();
            background.sprite = source.sprite; background.type = source.type;
            background.color = source.color; background.material = source.material;
            background.pixelsPerUnitMultiplier = source.pixelsPerUnitMultiplier;
            details = Rect("Details", root);
            Place(details, 0, 28, 240, 70);
            var detailsBackground = details.gameObject.AddComponent<Image>();
            detailsBackground.sprite = source.sprite; detailsBackground.type = source.type;
            detailsBackground.color = source.color; detailsBackground.material = source.material;
            detailsBackground.pixelsPerUnitMultiplier = source.pixelsPerUnitMultiplier;
            detailsBackground.raycastTarget = false;
            itemName = Text(details, template); Place(itemName.rectTransform, 6, 44, 228, 22);
            primary = Button(buttonTemplate, "Primary", () => send(shop ? ItemCommunicationIntent.RequestPurchase : ItemCommunicationIntent.OfferReward));
            Place(primary.transform as RectTransform, 2, 1, shop ? 142 : 196, 22);
            if (shop)
            {
                share = Button(buttonTemplate, "ShareProduct", () => send(ItemCommunicationIntent.ShareProduct));
                Place(share.transform as RectTransform, 146, 1, 92, 22);
            }
            help = Text(details, template); Place(help.rectTransform, 6, 4, 228, 36);
            help.textWrappingMode = TextWrappingModes.Normal;
        }

        internal bool Owns(GameObject selected) => selected != null &&
            (selected.transform.IsChildOf(primary.transform) || share != null && selected.transform.IsChildOf(share.transform));
        internal ItemCommunicationIntent IntentFor(GameObject selected) => !shop ? ItemCommunicationIntent.OfferReward :
            share != null && selected.transform.IsChildOf(share.transform) ? ItemCommunicationIntent.ShareProduct : ItemCommunicationIntent.RequestPurchase;

        internal void Refresh(NativeItemCommunicationTarget target, string binding)
        {
            bool available = target != null;
            if (!available && Owns(EventSystem.current?.currentSelectedGameObject)) RestoreSelection();
            returnSelection = target?.Source;
            details.gameObject.SetActive(Owns(EventSystem.current?.currentSelectedGameObject));
            primary.interactable = available;
            if (share != null) share.interactable = available;
            itemName.text = available ? target.Item.Name : ModLocalization.Get(ItemCommunicationLocalization.SelectItem);
            primary.text.text = ModLocalization.Get(shop ? ItemCommunicationLocalization.BuyAction : ItemCommunicationLocalization.RewardAction) +
                (string.IsNullOrWhiteSpace(binding) ? "" : " [" + binding + "]");
            if (share != null) share.text.text = ModLocalization.Get(ItemCommunicationLocalization.ShareAction);
            help.text = ModLocalization.Get(ItemCommunicationLocalization.Help);
            foreach (var text in new[] { itemName, primary.text, share?.text, help })
                if (text != null) NativeLocalizedText.MatchFontSize(text, template);
            var back = returnSelection?.GetComponent<Selectable>();
            primary.navigation = new Navigation { mode = Navigation.Mode.Explicit, selectOnUp = back, selectOnRight = share, selectOnDown = FollowingControl };
            if (share != null) share.navigation = new Navigation { mode = Navigation.Mode.Explicit, selectOnUp = back, selectOnLeft = primary, selectOnDown = FollowingControl };
        }

        private UI_HorayButton Button(UI_HorayButton source, string name, Action send)
        {
            var rect = Rect(name, root);
            var image = rect.gameObject.AddComponent<Image>();
            var original = (Image)source.targetGraphic;
            image.sprite = original.sprite; image.type = original.type; image.color = original.color;
            image.material = original.material; image.pixelsPerUnitMultiplier = original.pixelsPerUnitMultiplier;
            var button = rect.gameObject.AddComponent<UI_HorayButton>();
            button.targetGraphic = image; button.transition = source.transition;
            button.colors = source.colors; button.spriteState = source.spriteState; button.disabledColor = source.disabledColor;
            button.text = Text(rect, template);
            var label = button.text.rectTransform;
            label.anchorMin = Vector2.zero; label.anchorMax = Vector2.one;
            label.offsetMin = new Vector2(4, 2); label.offsetMax = new Vector2(-4, -2);
            button.onClick.AddListener(() => FeatureFailure.Run(FeatureId.ItemCommunication, send));
            return button;
        }

        private static TextMeshProUGUI Text(Transform parent, TextMeshProUGUI source)
        {
            var rect = Rect("Label", parent);
            var text = rect.gameObject.AddComponent<TextMeshProUGUI>();
            text.font = source.font; text.fontSharedMaterial = source.fontSharedMaterial;
            text.color = source.color; text.alignment = TextAlignmentOptions.Center; text.raycastTarget = false;
            text.textWrappingMode = TextWrappingModes.Normal;
            text.overflowMode = TextOverflowModes.Ellipsis;
            NativeLocalizedText.BindFont(text, source); NativeLocalizedText.MatchFontSize(text, source);
            return text;
        }

        private static RectTransform Rect(string name, Transform parent)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false); return rect;
        }

        private static void Place(RectTransform rect, float x, float y, float width, float height)
        {
            rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.zero;
            rect.anchoredPosition = new Vector2(x, y); rect.sizeDelta = new Vector2(width, height);
        }

        private void RestoreSelection()
        {
            if (EventSystem.current != null && Owns(EventSystem.current.currentSelectedGameObject))
                EventSystem.current.SetSelectedGameObject(returnSelection != null && returnSelection.activeInHierarchy ?
                    returnSelection : Panel.defaultSelectable);
        }

        public void Dispose()
        {
            if (root == null) return;
            RestoreSelection(); UnityEngine.Object.DestroyImmediate(root.gameObject);
        }
    }
}
