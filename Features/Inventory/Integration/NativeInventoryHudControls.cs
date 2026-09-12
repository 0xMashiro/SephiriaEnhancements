using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using SephiriaEnhancements.Integration;
using SephiriaEnhancements.Runtime;

namespace SephiriaEnhancements.Inventory
{
    internal sealed class NativeInventoryHudControls
    {
        private readonly NativeInventoryOptimizationViewTemplates nativeTemplates;
        private readonly Action cancelPickup;
        private readonly Action<int> changePage;

        internal NativeInventoryHudControls(NativeInventoryOptimizationViewTemplates templates,
            Action cancelPickup, Action<int> changePage)
        {
            nativeTemplates = templates;
            this.cancelPickup = cancelPickup;
            this.changePage = changePage;
        }
        internal static readonly Color ButtonColor =
            new(0.16f, 0.17f, 0.24f, 0.98f);
        internal static readonly Color PrimaryText =
            new(0.92f, 0.94f, 0.98f, 1f);

        internal Button CreateButton(string name, RectTransform parent,
            TextMeshProUGUI template, Vector2 position, Vector2 size,
            Action onClick, out TextMeshProUGUI label)
        {
            GameObject buttonObject = new(name, typeof(RectTransform),
                typeof(Image));
            RectTransform rect = buttonObject.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            SetTopRect(rect, position, size);
            Image image = buttonObject.GetComponent<Image>();
            Button button = NativeInventoryOptimizationControls.AddButton(
                buttonObject, nativeTemplates.ContentButton);
            ApplyImageStyle(image, nativeTemplates.Slot.bgImage, ButtonColor);
            image.sprite = nativeTemplates.Slot.defaultBGSprite;
            image.color = Color.white;
            button.targetGraphic = image;
            button.onClick.AddListener(() => FeatureFailure.Run(FeatureId.Inventory, () => onClick?.Invoke()));
            buttonObject.AddComponent<InventoryIntentPanelDropTarget>().Configure(
                cancelPickup, changePage, cancelOnLeft: false);
            label = CreateText("Label", rect, template, Vector2.zero, size,
                TextAlignmentOptions.Center,
                childCoordinates: true);
            label.color = PrimaryText;
            NativeLocalizedText.SetShrinkOnlySize(label, label.fontSize, label.fontSize * 0.75f);
            NativeInventoryOptimizationControls.SetLabel(button, label);
            return button;
        }

        internal static void ApplyImageStyle(Image destination,
            Image source, Color fallbackColor)
        {
            if (source == null)
            {
                destination.color = fallbackColor;
                return;
            }
            destination.sprite = source.sprite;
            destination.type = source.type;
            destination.preserveAspect = source.preserveAspect;
            destination.fillCenter = source.fillCenter;
            destination.fillMethod = source.fillMethod;
            destination.fillAmount = source.fillAmount;
            destination.fillClockwise = source.fillClockwise;
            destination.fillOrigin = source.fillOrigin;
            destination.pixelsPerUnitMultiplier =
                source.pixelsPerUnitMultiplier / InventoryOptimizationHudLayout.NativeUnitScale;
            destination.material = null;
            destination.color = source.color;
        }

        internal static TextMeshProUGUI CreateText(string name,
            RectTransform parent, TextMeshProUGUI template, Vector2 position,
            Vector2 size, TextAlignmentOptions alignment,
            bool childCoordinates = false)
        {
            GameObject textObject = new(name, typeof(RectTransform),
                typeof(TextMeshProUGUI));
            RectTransform rect = textObject.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            if (childCoordinates)
            {
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
            }
            else
            {
                SetTopRect(rect, position, size);
            }
            TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
            text.font = template.font;
            text.fontSharedMaterial = template.fontSharedMaterial;
            text.fontStyle = template.fontStyle;
            text.fontSize = template.fontSize * InventoryOptimizationHudLayout.NativeUnitScale;
            text.enableAutoSizing = false;
            text.characterSpacing = template.characterSpacing;
            text.wordSpacing = template.wordSpacing;
            text.lineSpacing = template.lineSpacing;
            text.isOrthographic = template.isOrthographic;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Ellipsis;
            text.alignment = alignment;
            text.raycastTarget = false;
            SephiriaEnhancements.Integration.NativeLocalizedText.BindFont(text, template);
            return text;
        }

        internal static void SetTopRect(RectTransform rect, Vector2 position,
            Vector2 size)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }
    }
}
