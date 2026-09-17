using System;
using System.Collections.Generic;
using SephiriaEnhancements.Configuration;
using SephiriaEnhancements.Integration;
using SephiriaEnhancements.Runtime;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SephiriaEnhancements.CostumeAppearance.Integration
{
    internal sealed class NativeCostumeAppearancePicker : UIBase
    {
        private sealed class Entry
        {
            internal UI_HorayButton Button;
            internal Image Icon, Selection;
        }

        private UI_CostumePanel panel;
        private NativeCostumeAppearanceView owner;
        private PlayerAvatar player;
        private GameObject returnSelection;
        private readonly List<CostumeSkinEntity> skins = new();
        private readonly List<Entry> entries = new();
        private UI_HorayButton thumbnails, list, apply, cancel;
        private Image thumbnailsSelected, listSelected, preview;
        private TextMeshProUGUI title, help, nameText, regionHint;
        private ScrollRect scroll;
        private RectTransform content;
        private AppearanceViewMode mode = AppearanceViewMode.Thumbnails;
        private AppearancePickerLayout layout;
        private readonly float[] scrollOffsets = new float[2];
        private int selected, focused;
        private string language;
        private bool waiting, lastEditable;
        public override bool CanBeSearchedByTypeHash => false;
        public override bool MarksPlayerAsPreparing => true;

        internal void Build(UI_CostumePanel source, NativeCostumeAppearanceView view)
        {
            panel = source; owner = view;
            hasControl = true; SetRoot(source.ParentRoot);
            rectTransform.anchorMin = rectTransform.anchorMax = new Vector2(.5f, .5f);
            rectTransform.sizeDelta = new Vector2(510, 310);
            rectTransform.anchoredPosition = Vector2.zero;
            var background = gameObject.AddComponent<Image>();
            var window = NativeAppearanceControls.DialogTemplate.GetComponent<Image>();
            background.sprite = window.sprite; background.type = window.type;
            background.color = window.color; background.material = window.material;
            background.pixelsPerUnitMultiplier = window.pixelsPerUnitMultiplier;
            title = Label(12, 10, 486, 22);
            thumbnails = Button("Thumbnails", 12, 40, 152, 26, () => SetMode(AppearanceViewMode.Thumbnails));
            list = Button("List", 170, 40, 152, 26, () => SetMode(AppearanceViewMode.List));
            thumbnailsSelected = SelectionFrame(thumbnails);
            listSelected = SelectionFrame(list);
            BuildScroll();
            var imageRect = NativeAppearanceControls.Rect("Preview", transform);
            Position(imageRect, 362, 78, 108, 86);
            preview = imageRect.gameObject.AddComponent<Image>();
            preview.preserveAspect = true; preview.raycastTarget = false;
            nameText = Label(334, 168, 164, 32);
            help = Label(334, 204, 164, 58);
            help.alignment = TextAlignmentOptions.TopLeft;
            apply = Button("Apply", 262, 276, 236, 24, Apply);
            cancel = Button("Cancel", 12, 276, 236, 24, Close);
            regionHint = Label(12, 262, 310, 12);
        }

        private void BuildScroll()
        {
            var zone = NativeAppearanceControls.Rect("Appearances", transform);
            Position(zone, 12, 74, AppearancePickerLayout.ContentWidth, AppearancePickerLayout.ViewportHeight);
            var raycast = zone.gameObject.AddComponent<Image>(); raycast.color = Color.clear;
            zone.gameObject.AddComponent<RectMask2D>();
            content = NativeAppearanceControls.Rect("Content", zone);
            Position(content, 0, 0, AppearancePickerLayout.ContentWidth, AppearancePickerLayout.ViewportHeight);
            scroll = zone.gameObject.AddComponent<ScrollRect>();
            scroll.viewport = zone; scroll.content = content;
            scroll.horizontal = false; scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.inertia = false; scroll.scrollSensitivity = 34;
            var native = panel.contentsZone.GetComponentInParent<ScrollRect>();
            var bar = Instantiate(native.verticalScrollbar, transform);
            bar.name = "AppearanceScrollbar";
            bar.transform.localScale = Vector3.one;
            Position((RectTransform)bar.transform, 318, 74, 6, AppearancePickerLayout.ViewportHeight);
            bar.onValueChanged = new Scrollbar.ScrollEvent();
            bar.navigation = new Navigation { mode = Navigation.Mode.None };
            scroll.verticalScrollbar = bar;
            scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;
        }

        internal void Show(PlayerAvatar avatar, GameObject returnTo)
        {
            player = avatar; returnSelection = returnTo; waiting = false;
            foreach (var entry in entries)
            {
                entry.Button.gameObject.SetActive(false);
                Destroy(entry.Button.gameObject);
            }
            entries.Clear(); skins.Clear();
            foreach (var costume in CostumeDatabase.GetAll())
                foreach (var skin in CostumeDatabase.GetAllRelatedSkins(costume.id))
                    if (NativeCostumeAppearance.Owned(skin)) skins.Add(skin);
            selected = Math.Max(0, skins.FindIndex(s => s.skinID == player.currentCostumeSkin));
            focused = selected;
            for (int i = 0; i < skins.Count; i++)
            {
                int index = i;
                var button = NativeAppearanceControls.Button(content, "Appearance", () => Select(index));
                // Native pointer hover also selects the EventSystem object; only activation changes the appearance.
                button.gameObject.AddComponent<NativeAppearanceFocus>().Focused = () => Reveal(index);
                var frame = SelectionFrame(button);
                var iconRect = NativeAppearanceControls.Rect("Icon", button.transform);
                var icon = iconRect.gameObject.AddComponent<Image>();
                icon.sprite = skins[i].icon; icon.preserveAspect = true; icon.raycastTarget = false;
                entries.Add(new Entry { Button = button, Icon = icon, Selection = frame });
            }
            RefreshTexts(); Arrange(); RefreshAvailability();
            transform.SetAsLastSibling();
            defaultSelectable = skins.Count == 0 ? cancel.gameObject : entries[selected].Button.gameObject;
            Open();
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(defaultSelectable);
            Reveal(selected);
        }

        private void SetMode(AppearanceViewMode value)
        {
            if (waiting || mode == value) return;
            scrollOffsets[(int)mode] = content.anchoredPosition.y;
            mode = value;
            Arrange();
            // Keep focus on the activated tab; Down returns to the same selected appearance.
            if (EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject((mode == AppearanceViewMode.Thumbnails ? thumbnails : list).gameObject);
        }

        private void Arrange()
        {
            layout = new AppearancePickerLayout(mode, skins.Count);
            content.sizeDelta = new Vector2(AppearancePickerLayout.ContentWidth, layout.Height);
            bool compact = mode == AppearanceViewMode.Thumbnails;
            thumbnailsSelected.gameObject.SetActive(compact);
            listSelected.gameObject.SetActive(!compact);
            for (int i = 0; i < entries.Count; i++)
            {
                var entry = entries[i];
                Position((RectTransform)entry.Button.transform, layout.X(i), layout.Y(i), layout.CellWidth, layout.CellHeight);
                entry.Button.text.gameObject.SetActive(!compact);
                if (compact)
                    NativeAppearanceControls.Place(entry.Icon.transform, 0, 0, 1, 1, 5, 4, -5, -4);
                else
                {
                    NativeAppearanceControls.Place(entry.Icon.transform, 0, .5f, 0, .5f, 4, -12, 28, 12);
                    NativeAppearanceControls.Place(entry.Button.text.transform, 0, 0, 1, 1, 32, 2, -4, -2);
                }
            }
            SetScrollOffset(layout.Reveal(selected, scrollOffsets[(int)mode]));
            RefreshNavigation(); UpdatePreview();
        }

        private void RefreshNavigation()
        {
            UI_HorayButton action = apply.interactable ? apply : cancel;
            UI_HorayButton tab = mode == AppearanceViewMode.Thumbnails ? thumbnails : list;
            UI_HorayButton selectedButton = entries.Count == 0 ? cancel : entries[selected].Button;
            for (int i = 0; i < entries.Count; i++)
            {
                var button = entries[i].Button;
                button.SetForceNavUp(layout.Up(i) < 0 ? tab : entries[layout.Up(i)].Button);
                button.SetForceNavDown(layout.Down(i) < 0 ? action : entries[layout.Down(i)].Button);
                button.SetForceNavLeft(entries[layout.Left(i)].Button);
                button.SetForceNavRight(layout.Right(i) < 0 ? action : entries[layout.Right(i)].Button);
            }
            thumbnails.SetForceNavLeft(thumbnails); thumbnails.SetForceNavRight(list);
            list.SetForceNavLeft(thumbnails); list.SetForceNavRight(list);
            thumbnails.SetForceNavUp(thumbnails); list.SetForceNavUp(list);
            thumbnails.SetForceNavDown(selectedButton); list.SetForceNavDown(selectedButton);
            apply.SetForceNavLeft(cancel); apply.SetForceNavRight(apply); apply.SetForceNavDown(apply);
            cancel.SetForceNavRight(action); cancel.SetForceNavLeft(cancel); cancel.SetForceNavDown(cancel);
            apply.SetForceNavUp(selectedButton); cancel.SetForceNavUp(entries.Count == 0 ? tab : selectedButton);
        }

        private void Select(int index)
        {
            if (waiting || index < 0 || index >= skins.Count) return;
            selected = index;
            UpdatePreview(); RefreshNavigation(); Reveal(selected);
        }

        private void Reveal(int index)
        {
            if (waiting) return;
            focused = index;
            SetScrollOffset(layout.Reveal(index, content.anchoredPosition.y));
        }

        internal void SwitchRegion()
        {
            if (waiting || EventSystem.current == null) return;
            var current = EventSystem.current.currentSelectedGameObject;
            GameObject target = current == apply.gameObject || current == cancel.gameObject
                ? (entries.Count == 0 ? thumbnails.gameObject : entries[focused].Button.gameObject)
                : (apply.interactable ? apply.gameObject : cancel.gameObject);
            EventSystem.current.SetSelectedGameObject(target);
        }
        private void SetScrollOffset(float value)
        {
            scroll.StopMovement();
            content.anchoredPosition = new Vector2(0, value);
        }

        private void RefreshTexts()
        {
            language = LocalizationManager.Instance?.CurrentLanguage;
            title.text = ModLocalization.Get(CostumeAppearanceLocalization.Title);
            thumbnails.text.text = ModLocalization.Get(CostumeAppearanceLocalization.Thumbnails);
            list.text.text = ModLocalization.Get(CostumeAppearanceLocalization.List);
            apply.text.text = ModLocalization.Get(CostumeAppearanceLocalization.Apply);
            cancel.text.text = ModLocalization.Get(CostumeAppearanceLocalization.Cancel);
            for (int i = 0; i < entries.Count; i++) entries[i].Button.text.text = skins[i].aName.ToString();
            UpdatePreview();
        }

        private void UpdatePreview()
        {
            bool valid = selected < skins.Count;
            preview.enabled = valid;
            if (valid)
            {
                preview.sprite = skins[selected].icon;
                nameText.text = string.Format(ModLocalization.Get(CostumeAppearanceLocalization.Preview), skins[selected].aName.ToString());
            }
            else nameText.text = ModLocalization.Get(CostumeAppearanceLocalization.Empty);
            for (int i = 0; i < entries.Count; i++) entries[i].Selection.gameObject.SetActive(i == selected);
        }

        private void Apply()
        {
            if (owner.CanEdit && selected < skins.Count)
                waiting = NativeCostumeAppearance.Instance.Request(player, skins[selected].skinID, true);
            RefreshAvailability();
        }

        private void RefreshAvailability()
        {
            apply.interactable = owner.CanEdit && skins.Count > 0;
            cancel.interactable = !waiting;
            thumbnails.interactable = list.interactable = !waiting;
            scroll.enabled = !waiting;
            scroll.verticalScrollbar.interactable = !waiting;
            foreach (var entry in entries) entry.Button.interactable = !waiting;
            lastEditable = apply.interactable;
            RefreshNavigation();
        }

        private void LateUpdate()
        {
            if (!FeatureFailure.IsAvailable(FeatureId.CostumeAppearance)) return;
            try { LateUpdateCore(); }
            catch (Exception exception) { FeatureFailure.Disable(FeatureId.CostumeAppearance, exception); }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private void LateUpdateCore()
        {
            if (!panel.IsOpened || !NativeCostumeAppearance.Available || !LocalPlayerResolver.IsLocal(player) || player.loadingScreenType != -1)
            { Close(); return; }
            if (waiting && !NativeCostumeAppearance.Instance.Pending) { Close(); return; }
            if (language != LocalizationManager.Instance?.CurrentLanguage) RefreshTexts();
            if (lastEditable != (owner.CanEdit && skins.Count > 0)) RefreshAvailability();
            regionHint.text = waiting ? string.Empty : NativeAppearanceRegionInput.Hint();
            if (NativeAppearanceRegionInput.WasPressed(this)) SwitchRegion();
            help.text = ModLocalization.Get(waiting ? CostumeAppearanceLocalization.Waiting : CostumeAppearanceLocalization.Help);
            var buttonText = NativeAppearanceControls.ButtonTextTemplate;
            NativeLocalizedText.MatchFontSize(regionHint, buttonText);
            if (mode == AppearanceViewMode.List)
                foreach (var entry in entries) NativeLocalizedText.MatchFontSize(entry.Button.text, buttonText);
            NativeLocalizedText.MatchFontSize(thumbnails.text, buttonText);
            NativeLocalizedText.MatchFontSize(list.text, buttonText);
            NativeLocalizedText.MatchFontSize(apply.text, buttonText);
            NativeLocalizedText.MatchFontSize(cancel.text, buttonText);
            var bodyText = NativeAppearanceControls.DialogTemplate.text;
            NativeLocalizedText.MatchFontSize(title, bodyText);
            NativeLocalizedText.MatchFontSize(help, bodyText);
            NativeLocalizedText.MatchFontSize(nameText, bodyText);
        }

        public override void CloseFromEsc() { if (!waiting) Close(); }

        public override void OnClosed()
        {
            base.OnClosed();
            scrollOffsets[(int)mode] = content.anchoredPosition.y;
            // ParentRoot.RemoveControl runs after OnClosed; restore the default it will use.
            if (panel != null && returnSelection != null) panel.defaultSelectable = returnSelection;
            if (returnSelection != null && returnSelection.activeInHierarchy && EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(returnSelection);
        }

        private TextMeshProUGUI Label(float x, float y, float w, float h)
        {
            var text = NativeAppearanceControls.Text(NativeAppearanceControls.DialogTemplate.text, transform);
            Position(text.rectTransform, x, y, w, h);
            return text;
        }

        private UI_HorayButton Button(string label, float x, float y, float w, float h, Action action)
        {
            var button = NativeAppearanceControls.Button(transform, label, action);
            Position((RectTransform)button.transform, x, y, w, h);
            return button;
        }

        private static Image SelectionFrame(UI_HorayButton button)
        {
            var rect = NativeAppearanceControls.Rect("Selection", button.transform);
            rect.SetAsFirstSibling();
            NativeAppearanceControls.Place(rect, 0, 0, 1, 1, 0, 0, 0, 0);
            var image = rect.gameObject.AddComponent<Image>();
            var source = (Image)NativeAppearanceControls.ButtonTemplate.targetGraphic;
            image.sprite = NativeAppearanceControls.ButtonTemplate.spriteState.selectedSprite;
            image.type = source.type; image.material = source.material;
            image.pixelsPerUnitMultiplier = source.pixelsPerUnitMultiplier;
            image.raycastTarget = false;
            rect.gameObject.SetActive(false);
            return image;
        }

        private static void Position(RectTransform rect, float x, float y, float width, float height)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(0, 1); rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = new Vector2(x, -y); rect.sizeDelta = new Vector2(width, height);
        }
    }

    internal sealed class NativeAppearanceFocus : MonoBehaviour, ISelectHandler
    {
        internal Action Focused;
        public void OnSelect(BaseEventData eventData)
        {
            if (!FeatureFailure.IsAvailable(FeatureId.CostumeAppearance)) return;
            try { Focused?.Invoke(); }
            catch (Exception exception) { FeatureFailure.Disable(FeatureId.CostumeAppearance, exception); }
        }
    }
}
