using SephiriaEnhancements.Integration;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System;
using SephiriaEnhancements.MapEnhancements.Integration;

namespace SephiriaEnhancements.MapEnhancements
{
    internal sealed class MapLocationMarkerView : MonoBehaviour, ISelectHandler
    {
        private RectTransform rectTransform;
        private TextMeshProUGUI textTemplate;
        private TextMeshProUGUI glyph;
        private TextMeshProUGUI nameText;
        private string label;
        internal string Label => label;
        internal bool IsPerson { get; private set; }
        internal bool HasQuest { get; private set; }
        internal Transform Target { get; private set; }
        internal Vector3 WorldPosition { get; private set; }
        internal NativeMapDestination Destination { get; private set; }
        internal Action Selected;
        internal Action Activated;
        internal Vector2 NameSize => new Vector2(Mathf.Clamp(nameText.GetPreferredValues(label).x + 4, 18, 80), 13);

        internal static MapLocationMarkerView Create(RectTransform parent,
            TextMeshProUGUI detailText)
        {
            GameObject markerObject = new GameObject("Map Location",
                typeof(RectTransform), typeof(Image), typeof(UI_HorayButton),
                typeof(MapLocationMarkerView));
            var marker = markerObject.GetComponent<MapLocationMarkerView>();
            marker.rectTransform = markerObject.GetComponent<RectTransform>();
            marker.rectTransform.SetParent(parent, false);
            marker.rectTransform.sizeDelta = new Vector2(10f, 10f);
            marker.textTemplate = detailText;
            markerObject.GetComponent<Image>().color = Color.clear;

            var glyphObject = new GameObject("Symbol", typeof(RectTransform),
                typeof(TextMeshProUGUI));
            marker.glyph = glyphObject.GetComponent<TextMeshProUGUI>();
            marker.glyph.rectTransform.SetParent(marker.rectTransform, false);
            marker.glyph.rectTransform.sizeDelta = new Vector2(10f, 12f);
            marker.glyph.alignment = TextAlignmentOptions.Center;
            marker.glyph.raycastTarget = false;
            marker.glyph.color = detailText.color;
            marker.glyph.text = "•";
            NativeLocalizedText.BindFont(marker.glyph, detailText);
            NativeLocalizedText.MatchFontSize(marker.glyph, detailText);
            var nameObject = new GameObject("Name", typeof(RectTransform), typeof(TextMeshProUGUI));
            marker.nameText = nameObject.GetComponent<TextMeshProUGUI>();
            marker.nameText.rectTransform.SetParent(marker.rectTransform, false);
            marker.nameText.alignment = TextAlignmentOptions.Center;
            marker.nameText.textWrappingMode = TextWrappingModes.NoWrap;
            marker.nameText.overflowMode = TextOverflowModes.Ellipsis;
            marker.nameText.raycastTarget = false;
            marker.nameText.color = detailText.color;
            NativeLocalizedText.BindFont(marker.nameText, detailText);
            NativeLocalizedText.MatchFontSize(marker.nameText, detailText);
            UI_HorayButton button = markerObject.GetComponent<UI_HorayButton>();
            button.targetGraphic = marker.glyph;
            button.onClick.AddListener(() => marker.Activated?.Invoke());
            ColorBlock colors = button.colors;
            colors.highlightedColor = new Color(1.35f, 1.35f, 1.35f);
            colors.selectedColor = colors.highlightedColor;
            button.colors = colors;
            return marker;
        }

        internal void Set(string text, Vector2 mapPosition, Transform target, bool person,
            bool quest = false, NativeMapDestination destination = null, Vector3? worldPosition = null)
        {
            rectTransform.anchoredPosition = mapPosition;
            label = text;
            Target = target;
            WorldPosition = worldPosition ?? target.position;
            IsPerson = person;
            HasQuest = quest;
            Destination = destination;
            glyph.gameObject.SetActive(destination == null);
            nameText.text = label;
            NativeLocalizedText.MatchFontSize(glyph, textTemplate);
            NativeLocalizedText.MatchFontSize(nameText, textTemplate);
        }

        internal void SetScale(float scale) => rectTransform.localScale = Vector3.one / scale;

        internal void ShowName(bool show, Vector2 offset, bool selected)
        {
            nameText.gameObject.SetActive(show);
            nameText.rectTransform.anchoredPosition = offset;
            nameText.rectTransform.sizeDelta = NameSize;
            glyph.text = HasQuest ? "!" : "•";
            glyph.rectTransform.localScale = Vector3.one * (selected ? 1.35f : 1f);
            glyph.color = textTemplate.color;
        }

        public void OnSelect(BaseEventData eventData) => Selected?.Invoke();
    }
}
