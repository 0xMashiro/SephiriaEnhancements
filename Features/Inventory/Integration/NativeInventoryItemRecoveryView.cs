using System;
using System.Collections.Generic;
using System.Linq;
using SephiriaEnhancements.Configuration;
using SephiriaEnhancements.Integration;
using SephiriaEnhancements.KeyboardUiNavigation;
using SephiriaEnhancements.Runtime;
using SephiriaEnhancements.Runtime.Inventory;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SephiriaEnhancements.Inventory.Integration
{
    internal sealed class NativeInventoryItemRecoveryView : UIBase
    {
        private sealed class Row
        {
            internal InventoryItemSnapshot Item;
            internal UI_HorayButton Button;
            internal TextMeshProUGUI Mark;
        }
        private const float RowHeight = InventoryRecoveryListLayout.RowHeight,
            ViewportHeight = InventoryRecoveryListLayout.ViewportHeight, ContentWidth = 508;
        private UI_CharacterStatusPanel panel;
        private GridInventory inventory;
        private UI_MessageBox_YesNo template;
        private TextMeshProUGUI title, help, summary;
        private UI_HorayButton apply, cancel, group;
        private UI_MessageBox groupingDialog;
        private ScrollRect scroll;
        private RectTransform content;
        private readonly List<Row> rows = new();
        private InventoryRecoverySelection selection = new();
        private Action<InventoryItemKey[]> confirmed;
        private Action cancelled;
        private Action grouping;
        private GameObject returnSelection;
        private bool moving;
        private bool checking;
        private float nextRefreshAt;
        private Row focusedRow;
        public override bool CanBeSearchedByTypeHash => false;
        public override bool MarksPlayerAsPreparing => true;

        internal static NativeInventoryItemRecoveryView Create(UI_CharacterStatusPanel panel,
            Action<InventoryItemKey[]> confirm, Action cancel, Action group)
        {
            var go = new GameObject("InventoryItemRecovery", typeof(RectTransform), typeof(CanvasGroup));
            go.SetActive(false);
            go.transform.SetParent(panel.transform.parent, false);
            var view = go.AddComponent<NativeInventoryItemRecoveryView>();
            view.Build(panel, confirm, cancel, group);
            return view;
        }

        private void Build(UI_CharacterStatusPanel owner, Action<InventoryItemKey[]> confirm, Action cancelAction, Action groupAction)
        {
            panel = owner; inventory = owner.PlayerAvatar.Inventory;
            confirmed = confirm; cancelled = cancelAction;
            grouping = groupAction;
            returnSelection = EventSystem.current?.currentSelectedGameObject;
            template = UIManager.Instance.GetElement<UI_MessageBoxHolder>().yesNoPrefab;
            hasControl = true; SetRoot(panel.ParentRoot);
            rectTransform.anchorMin = rectTransform.anchorMax = new Vector2(.5f, .5f);
            rectTransform.sizeDelta = new Vector2(540, 390);
            rectTransform.anchoredPosition = Vector2.zero;
            var background = gameObject.AddComponent<Image>();
            CopyImage(background, template.GetComponent<Image>());
            title = Text(transform, template.text, 12, 10, 516, 24);
            help = Text(transform, template.text, 12, 38, 516, 48);
            var viewport = Rect("Items", transform, 12, 92, ContentWidth, ViewportHeight);
            viewport.gameObject.AddComponent<Image>().color = Color.clear;
            viewport.gameObject.AddComponent<RectMask2D>();
            content = Rect("Content", viewport, 0, 0, ContentWidth, ViewportHeight);
            scroll = viewport.gameObject.AddComponent<ScrollRect>();
            scroll.viewport = viewport; scroll.content = content;
            scroll.horizontal = false; scroll.vertical = true; scroll.inertia = false;
            scroll.movementType = ScrollRect.MovementType.Clamped; scroll.scrollSensitivity = RowHeight;
            var rail = Rect("Scrollbar", transform, 522, 92, 6, ViewportHeight);
            var handle = Rect("Handle", rail, 0, 0, 6, 24);
            var handleImage = handle.gameObject.AddComponent<Image>();
            CopyImage(handleImage, (Image)template.yesButton.targetGraphic);
            var bar = rail.gameObject.AddComponent<Scrollbar>();
            bar.handleRect = handle; bar.targetGraphic = handleImage;
            bar.direction = Scrollbar.Direction.BottomToTop;
            bar.navigation = new Navigation { mode = Navigation.Mode.None };
            scroll.verticalScrollbar = bar;
            scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;
            summary = Text(transform, template.text, 12, 274, 516, 24);
            group = Button(transform, "GroupItems", 12, 310, 516, 30, ConfirmGrouping);
            cancel = Button(transform, "Cancel", 12, 348, 176, 30, Close);
            apply = Button(transform, "MoveAndContinue", 194, 348, 334, 30, () =>
            {
                if (!moving && selection.Count > 0) confirmed(selection.Selected(rows.Select(row => row.Item)));
            });
        }

        private void ConfirmGrouping()
        {
            if (moving || groupingDialog != null) return;
            var holder = UIManager.Instance.GetElement<UI_MessageBoxHolder>();
            if (holder.HasOpenedBox) return;
            defaultSelectable = group.gameObject;
            groupingDialog = holder.OpenYesNo(ModLocalization.Get(InventoryItemRecoveryLocalization.GroupingPrompt),
                () => FeatureFailure.Run(FeatureId.Inventory, grouping), null, negative: true);
            var dialog = (UI_MessageBox_YesNo)groupingDialog;
            dialog.defaultSelectable = dialog.noButton.gameObject;
            EventSystem.current?.SetSelectedGameObject(dialog.defaultSelectable);
            groupingDialog.onCloseMessageBox += _ =>
            {
                if (this != null && IsOpened && !moving)
                    KeyboardUiNavigationController.RequestSelection(this, group.gameObject);
            };
        }

        internal void ShowItems(InventoryItemSnapshot[] items)
        {
            moving = false; checking = false; selection = new InventoryRecoverySelection();
            foreach (var row in rows) { row.Button.gameObject.SetActive(false); Destroy(row.Button.gameObject); }
            rows.Clear(); focusedRow = null;
            for (int i = 0; i < items.Length; i++)
            {
                var row = new Row { Item = items[i] };
                row.Button = Button(content, "Item", 0, i * RowHeight, ContentWidth, RowHeight - 2,
                    () => { if (!moving) selection.Toggle(row.Item.ItemKey, Eligible(row.Item), FreeSlots()); Refresh(); });
                Place(row.Button.text.rectTransform, 68, 2, ContentWidth - 76, RowHeight - 6);
                row.Button.text.alignment = TextAlignmentOptions.MidlineLeft;
                row.Mark = Text(row.Button.transform, template.text, 4, 2, 28, RowHeight - 6);
                row.Mark.alignment = TextAlignmentOptions.Center;
                var icon = Rect("Icon", row.Button.transform, 34, 7, 28, 28).gameObject.AddComponent<Image>();
                icon.sprite = ItemDatabase.FindItemById(row.Item.EntityId)?.Icon;
                icon.preserveAspect = true; icon.raycastTarget = false;
                row.Button.gameObject.AddComponent<NativeInventoryRecoveryRowSelection>().Selected = () => Reveal(row);
                rows.Add(row);
            }
            content.sizeDelta = new Vector2(ContentWidth, Math.Max(ViewportHeight, rows.Count * RowHeight));
            content.anchoredPosition = Vector2.zero;
            bool hasItems = rows.Count > 0;
            rectTransform.sizeDelta = new Vector2(540, hasItems ? 390 : 220);
            scroll.gameObject.SetActive(hasItems);
            scroll.verticalScrollbar.gameObject.SetActive(hasItems);
            summary.gameObject.SetActive(hasItems);
            apply.gameObject.SetActive(hasItems);
            Place(help.rectTransform, 12, 38, 516, hasItems ? 48 : 90);
            Place((RectTransform)group.transform, 12, hasItems ? 310 : 140, 516, 30);
            Place((RectTransform)cancel.transform, 12, hasItems ? 348 : 178, hasItems ? 176 : 516, 30);
            Refresh();
            defaultSelectable = rows.Count == 0 ? group.gameObject : rows[0].Button.gameObject;
            if (!IsOpened) { transform.SetAsLastSibling(); Open(); }
            EventSystem.current?.SetSelectedGameObject(defaultSelectable);
        }

        internal void SetMoving()
        {
            moving = true; checking = false; Refresh();
            defaultSelectable = cancel.gameObject;
            EventSystem.current?.SetSelectedGameObject(cancel.gameObject);
        }

        internal void SetChecking()
        {
            moving = true; checking = true; Refresh();
            defaultSelectable = cancel.gameObject;
            EventSystem.current?.SetSelectedGameObject(cancel.gameObject);
        }

        internal void Dismiss()
        {
            cancelled = null;
            Close();
        }

        private bool Eligible(InventoryItemSnapshot item) =>
            NativeInventorySubBagTransfer.UnavailableReason(inventory, item, out _) == null;
        private int FreeSlots() => Enumerable.Range(0, Math.Min(inventory.numberOfSubBagStorage, sbyte.MaxValue + 1))
            .Count(index => !inventory.subBagMatrix.ContainsKey((sbyte)index));

        private void Refresh()
        {
            int free = FreeSlots();
            if (!moving) selection.Retain(rows.Where(row => Eligible(row.Item)).Select(row => row.Item.ItemKey), free);
            title.text = ModLocalization.Get(InventoryItemRecoveryLocalization.Title);
            help.text = ModLocalization.Get(rows.Count == 0 ? InventoryItemRecoveryLocalization.Unverified : InventoryItemRecoveryLocalization.Help);
            cancel.text.text = ModLocalization.Get(InventoryItemRecoveryLocalization.Cancel);
            apply.text.text = ModLocalization.Get(InventoryItemRecoveryLocalization.Confirm);
            group.text.text = ModLocalization.Get(InventoryItemRecoveryLocalization.Grouping);
            group.interactable = !moving;
            summary.text = moving ? ModLocalization.Get(checking ? InventoryItemRecoveryLocalization.Checking : InventoryOptimizationLocalization.Applying) :
                string.Format(ModLocalization.Get(InventoryItemRecoveryLocalization.Selection), selection.Count, rows.Count, free);
            apply.interactable = !moving && selection.Count > 0;
            if (!apply.interactable && EventSystem.current?.currentSelectedGameObject == apply.gameObject)
                EventSystem.current.SetSelectedGameObject(cancel.gameObject);
            foreach (var row in rows)
            {
                var item = row.Item;
                string reason = NativeInventorySubBagTransfer.UnavailableReason(inventory, item, out _);
                bool stored = inventory.subBagMatrix.Values.Any(value => value.entityID == item.EntityId && value.instanceID == item.InstanceId);
                string detail = moving && stored ? InventoryItemRecoveryLocalization.Stored : checking ? InventoryItemRecoveryLocalization.Checking :
                    moving && selection.Contains(item.ItemKey) ? InventoryOptimizationLocalization.Applying :
                    reason ?? InventoryItemRecoveryLocalization.ItemUnverified;
                string name = (ItemDatabase.FindItemById(item.EntityId)?.Name ?? item.Name) + " (" + (item.X + 1) + ", " + (item.Y + 1) + ")";
                row.Button.text.text = name + "\n" + ModLocalization.Get(detail);
                row.Mark.text = selection.Contains(item.ItemKey) ? "[×]" : reason != null ? "—" : "[ ]";
                row.Button.interactable = !moving;
                NativeLocalizedText.MatchFontSize(row.Button.text, template.yesButton.GetComponentInChildren<TextMeshProUGUI>(true));
                NativeLocalizedText.MatchFontSize(row.Mark, template.text);
            }
            foreach (var label in new[] { title, help, summary }) NativeLocalizedText.MatchFontSize(label, template.text);
            foreach (var button in new[] { apply, cancel, group })
                NativeLocalizedText.MatchFontSize(button.text, template.yesButton.GetComponentInChildren<TextMeshProUGUI>(true));
            RefreshNavigation();
        }

        private void RefreshNavigation()
        {
            var last = !moving && rows.Count > 0 ? (focusedRow ?? rows[0]).Button : cancel;
            var action = group.interactable ? group : cancel;
            for (int i = 0; i < rows.Count; i++)
            {
                rows[i].Button.SetForceNavUp(i == 0 ? rows[i].Button : rows[i - 1].Button);
                rows[i].Button.SetForceNavDown(i + 1 == rows.Count ? action : rows[i + 1].Button);
                rows[i].Button.SetForceNavLeft(rows[i].Button); rows[i].Button.SetForceNavRight(action);
            }
            group.SetForceNavUp(last); group.SetForceNavDown(apply.interactable ? apply : cancel);
            group.SetForceNavLeft(group); group.SetForceNavRight(group);
            cancel.SetForceNavUp(action); cancel.SetForceNavRight(apply.interactable ? apply : cancel); cancel.SetForceNavLeft(cancel); cancel.SetForceNavDown(cancel);
            apply.SetForceNavUp(action); apply.SetForceNavLeft(cancel); apply.SetForceNavRight(apply); apply.SetForceNavDown(apply);
        }

        private void Reveal(Row row)
        {
            focusedRow = row;
            int index = rows.IndexOf(row);
            float offset = InventoryRecoveryListLayout.Reveal(index, rows.Count, content.anchoredPosition.y);
            scroll.StopMovement(); content.anchoredPosition = new Vector2(0, offset);
            RefreshNavigation();
        }

        private void LateUpdate() => FeatureFailure.Run(FeatureId.Inventory, () =>
        {
            if (IsOpened && Time.unscaledTime >= nextRefreshAt)
            {
                nextRefreshAt = Time.unscaledTime + 0.15f;
                Refresh();
            }
        });
        public override void CloseFromEsc() => Close();
        public override void OnClosed()
        {
            if (groupingDialog != null) groupingDialog.ForceClose();
            groupingDialog = null;
            base.OnClosed();
            var action = cancelled; cancelled = null; action?.Invoke();
            if (panel != null && returnSelection != null) panel.defaultSelectable = returnSelection;
            if (returnSelection != null && returnSelection.activeInHierarchy)
            {
                EventSystem.current?.SetSelectedGameObject(returnSelection);
                // UIBase removes modal control after OnClosed. Keyboard entry must
                // also be restored after the native parent becomes interactable again.
                KeyboardUiNavigationController.RequestSelection(panel, returnSelection);
            }
            Destroy(gameObject);
        }

        private UI_HorayButton Button(Transform parent, string name, float x, float y, float width, float height, Action click)
        {
            var rect = Rect(name, parent, x, y, width, height);
            var image = rect.gameObject.AddComponent<Image>(); CopyImage(image, (Image)template.yesButton.targetGraphic);
            var button = (UI_HorayButton)NativeInventoryOptimizationControls.AddButton(rect.gameObject, template.yesButton);
            button.targetGraphic = image;
            button.text = Text(rect, template.yesButton.GetComponentInChildren<TextMeshProUGUI>(true), 6, 2, width - 12, height - 4);
            button.text.alignment = TextAlignmentOptions.Center;
            button.onClick.AddListener(() => FeatureFailure.Run(FeatureId.Inventory, click));
            return button;
        }
        private static TextMeshProUGUI Text(Transform parent, TextMeshProUGUI source, float x, float y, float width, float height)
        {
            var text = Rect("Text", parent, x, y, width, height).gameObject.AddComponent<TextMeshProUGUI>();
            text.font = source.font; text.fontSharedMaterial = source.fontSharedMaterial; text.color = source.color;
            text.raycastTarget = false; text.alignment = TextAlignmentOptions.TopLeft;
            text.textWrappingMode = TextWrappingModes.Normal; text.overflowMode = TextOverflowModes.Overflow;
            NativeLocalizedText.BindFont(text, source); NativeLocalizedText.MatchFontSize(text, source);
            return text;
        }
        private static RectTransform Rect(string name, Transform parent, float x, float y, float width, float height)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false); Place(rect, x, y, width, height); return rect;
        }
        private static void Place(RectTransform rect, float x, float y, float width, float height)
        {
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = new Vector2(x, -y); rect.sizeDelta = new Vector2(width, height);
        }
        private static void CopyImage(Image target, Image source)
        {
            target.sprite = source.sprite; target.type = source.type; target.color = source.color;
            target.material = source.material; target.pixelsPerUnitMultiplier = source.pixelsPerUnitMultiplier;
        }
    }

    internal sealed class NativeInventoryRecoveryRowSelection : MonoBehaviour, ISelectHandler
    {
        internal Action Selected;
        public void OnSelect(BaseEventData eventData) => FeatureFailure.Run(FeatureId.Inventory, () => Selected?.Invoke());
    }
}
