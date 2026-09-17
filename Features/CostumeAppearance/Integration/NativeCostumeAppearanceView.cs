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
    internal sealed class NativeCostumeAppearanceView : MonoBehaviour
    {
        private UI_CostumePanel panel;
        private PlayerAvatar player;
        private RectTransform scroll, footer;
        private Vector2 originalOffset;
        private UI_HorayButton choose, follow;
        private TextMeshProUGUI regionHint;
        private NativeCostumeAppearancePicker picker;
        private GameObject regionReturn;
        private Vector2 regionScrollPosition;
        private string language, preference;

        internal void Bind(UI_CostumePanel owner, PlayerAvatar avatar)
        {
            panel = owner; player = avatar; regionReturn = null;
            if (footer == null)
            {
                scroll = panel.contentsZone.GetComponentInParent<ScrollRect>().transform as RectTransform;
                originalOffset = scroll.offsetMin;
                footer = NativeAppearanceControls.Rect("CostumeAppearance", scroll.parent);
                footer.anchorMin = new Vector2(scroll.anchorMin.x, scroll.anchorMin.y);
                footer.anchorMax = new Vector2(scroll.anchorMax.x, scroll.anchorMin.y);
                footer.pivot = new Vector2(.5f, 0);
                footer.offsetMin = new Vector2(scroll.offsetMin.x, originalOffset.y);
                float buttonHeight = ((RectTransform)NativeAppearanceControls.ButtonTemplate.transform).rect.height;
                footer.offsetMax = new Vector2(scroll.offsetMax.x, originalOffset.y + buttonHeight + 14);
                scroll.offsetMin = originalOffset + new Vector2(0, buttonHeight + 18);
                regionHint = NativeAppearanceControls.Text(NativeAppearanceControls.ButtonTextTemplate, footer);
                NativeAppearanceControls.Place(regionHint.transform, 0, 1, 1, 1, 0, -14, 0, 0);
                choose = NativeAppearanceControls.Button(footer, "Choose", OpenPicker);
                follow = NativeAppearanceControls.Button(footer, "Follow", Restore);
                NativeAppearanceControls.Place(choose.transform, 0, 0, .64f, 1, 0, 0, -2, -14);
                NativeAppearanceControls.Place(follow.transform, .64f, 0, 1, 1, 2, 0, 0, -14);
                choose.SetForceNavUpGroup(panel.contentsZone);
                follow.SetForceNavUpGroup(panel.contentsZone);
                choose.SetForceNavRight(follow); follow.SetForceNavLeft(choose);
                // Only add a route from the last native row; do not replace its own handlers.
                var rows = panel.contentsZone.GetComponentsInChildren<UI_CostumeListElement>();
                if (rows.Length > 0) rows[rows.Length - 1].button.SetForceNavDown(choose);
            }
            footer.gameObject.SetActive(true);
            Refresh();
        }

        private void OpenPicker() => Guard(() =>
        {
            if (!CanEdit) return;
            if (picker == null)
            {
                var go = new GameObject("CostumeAppearancePicker", typeof(RectTransform), typeof(CanvasGroup));
                go.SetActive(false);
                go.transform.SetParent(panel.transform.parent, false);
                picker = go.AddComponent<NativeCostumeAppearancePicker>();
                picker.Build(panel, this);
            }
            picker.Show(player, choose.gameObject);
        });

        private void Restore() => Guard(() =>
        {
            if (CanEdit) NativeCostumeAppearance.Instance.Request(player, "", true);
        });

        internal void SwitchRegion()
        {
            if (!CanEdit || footer == null || !footer.gameObject.activeInHierarchy || EventSystem.current == null) return;
            var current = EventSystem.current.currentSelectedGameObject;
            if (current != null && current.transform.IsChildOf(footer))
            {
                var target = KeyboardUiNavigation.KeyboardUiSelection.FindPanelEntry(panel, regionReturn);
                if (target == null || target.transform.IsChildOf(footer)) return;
                EventSystem.current.SetSelectedGameObject(target);
                if (target == regionReturn)
                    scroll.GetComponent<ScrollRect>().content.anchoredPosition = regionScrollPosition;
            }
            else
            {
                regionReturn = KeyboardUiNavigation.KeyboardUiSelection.FindPanelEntry(panel, current);
                regionScrollPosition = scroll.GetComponent<ScrollRect>().content.anchoredPosition;
                EventSystem.current.SetSelectedGameObject(choose.gameObject);
            }
        }

        internal bool CanEdit => NativeCostumeAppearance.HostSupports && NativeCostumeAppearance.Instance != null &&
            !NativeCostumeAppearance.Instance.Pending && panel != null && !panel.ignoreSave &&
            LocalPlayerResolver.IsLocal(player) && player.loadingScreenType == -1;

        private void LateUpdate() => Guard(() =>
        {
            if (footer == null) return;
            footer.gameObject.SetActive(NativeCostumeAppearance.Available && !panel.ignoreSave);
            var current = EventSystem.current?.currentSelectedGameObject;
            if (current != null && !current.transform.IsChildOf(footer) &&
                KeyboardUiNavigation.KeyboardUiSelection.IsInPanel(panel, current))
            {
                regionReturn = current;
                regionScrollPosition = scroll.GetComponent<ScrollRect>().content.anchoredPosition;
            }
            if (language != LocalizationManager.Instance?.CurrentLanguage || preference != NativeCostumeAppearance.Preference) Refresh();
            choose.interactable = CanEdit;
            follow.interactable = CanEdit && !string.IsNullOrEmpty(NativeCostumeAppearance.Preference);
            regionHint.text = CanEdit ? NativeAppearanceRegionInput.Hint() : string.Empty;
            if (NativeAppearanceRegionInput.WasPressed(panel)) SwitchRegion();
            var buttonText = NativeAppearanceControls.ButtonTextTemplate;
            NativeLocalizedText.MatchFontSize(regionHint, buttonText);
            NativeLocalizedText.MatchFontSize(choose.text, buttonText);
            NativeLocalizedText.MatchFontSize(follow.text, buttonText);
            if (!NativeCostumeAppearance.HostSupports)
                choose.text.text = ModLocalization.Get(CostumeAppearanceLocalization.HostRequired);
            else RefreshLabel();
        });

        private void Refresh()
        {
            language = LocalizationManager.Instance?.CurrentLanguage;
            preference = NativeCostumeAppearance.Preference;
            follow.text.text = ModLocalization.Get(CostumeAppearanceLocalization.Follow);
            RefreshLabel();
        }

        private void RefreshLabel()
        {
            var skin = CostumeDatabase.GetCostumeSkinByID(NativeCostumeAppearance.Preference);
            choose.text.text = string.Format(ModLocalization.Get(CostumeAppearanceLocalization.Appearance),
                skin == null ? ModLocalization.Get(CostumeAppearanceLocalization.Following) : skin.aName.ToString());
        }

        private static void Guard(Action action)
        {
            if (!FeatureFailure.IsAvailable(FeatureId.CostumeAppearance)) return;
            try { action(); }
            catch (Exception exception) { FeatureFailure.Disable(FeatureId.CostumeAppearance, exception); }
        }

        private void OnDisable()
        {
            if (picker != null && picker.IsOpened) picker.Close();
        }

        private void OnDestroy()
        {
            if (scroll != null) scroll.offsetMin = originalOffset;
            if (footer != null) Destroy(footer.gameObject);
            if (picker != null) { picker.Close(); Destroy(picker.gameObject); }
        }

        internal static void DisposeAll()
        {
            foreach (var view in Resources.FindObjectsOfTypeAll<NativeCostumeAppearanceView>()) DestroyImmediate(view);
        }
    }

    internal static class NativeAppearanceControls
    {
        internal static UI_MessageBox_YesNo DialogTemplate => UIManager.Instance.GetElement<UI_MessageBoxHolder>().yesNoPrefab;
        internal static UI_HorayButton ButtonTemplate => (UI_HorayButton)DialogTemplate.yesButton;
        // Native buttons have a child label; UI_HorayButton.text is not assigned on this prefab.
        internal static TextMeshProUGUI ButtonTextTemplate => ButtonTemplate.GetComponentInChildren<TextMeshProUGUI>(true);

        internal static RectTransform Rect(string name, Transform parent)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            return rect;
        }

        internal static void Place(Transform target, float x0, float y0, float x1, float y1,
            float left, float bottom, float right, float top)
        {
            var r = (RectTransform)target;
            r.anchorMin = new Vector2(x0, y0); r.anchorMax = new Vector2(x1, y1);
            r.offsetMin = new Vector2(left, bottom); r.offsetMax = new Vector2(right, top);
        }

        internal static TextMeshProUGUI Text(TextMeshProUGUI template, Transform parent)
        {
            var r = Rect("Label", parent);
            var t = r.gameObject.AddComponent<TextMeshProUGUI>();
            t.font = template.font; t.fontSharedMaterial = template.fontSharedMaterial;
            t.color = template.color; t.alignment = TextAlignmentOptions.Center;
            t.raycastTarget = false;
            NativeLocalizedText.BindFont(t, template);
            NativeLocalizedText.MatchFontSize(t, template);
            return t;
        }

        internal static UI_HorayButton Button(Transform parent, string name, Action click)
        {
            var r = Rect(name, parent);
            var image = r.gameObject.AddComponent<Image>();
            var template = ButtonTemplate;
            var source = (Image)template.targetGraphic;
            image.sprite = source.sprite;
            image.type = source.type;
            image.color = source.color;
            image.material = source.material;
            image.pixelsPerUnitMultiplier = source.pixelsPerUnitMultiplier;
            var b = r.gameObject.AddComponent<UI_HorayButton>();
            b.targetGraphic = image; b.transition = template.transition; b.colors = template.colors;
            b.spriteState = template.spriteState;
            b.disabledColor = template.disabledColor;
            b.text = Text(ButtonTextTemplate, r);
            Place(b.text.transform, 0, 0, 1, 1, 6, 2, -6, -2);
            b.onClick.AddListener(() =>
            {
                if (!FeatureFailure.IsAvailable(FeatureId.CostumeAppearance)) return;
                try { click(); }
                catch (Exception exception) { FeatureFailure.Disable(FeatureId.CostumeAppearance, exception); }
            });
            return b;
        }
    }
}
