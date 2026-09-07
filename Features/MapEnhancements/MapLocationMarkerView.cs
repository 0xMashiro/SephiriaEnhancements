using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SephiriaEnhancements.MapEnhancements
{
    internal sealed class MapLocationMarkerView : MonoBehaviour
    {
        private RectTransform rectTransform;
        private TextMeshProUGUI nameText;
        private TextMeshProUGUI textTemplate;

        internal static MapLocationMarkerView Create(RectTransform parent,
            TextMeshProUGUI nativeTextTemplate)
        {
            GameObject markerObject = new GameObject("Map Location Marker",
                typeof(RectTransform), typeof(MapLocationMarkerView));
            MapLocationMarkerView marker =
                markerObject.GetComponent<MapLocationMarkerView>();
            marker.rectTransform = markerObject.GetComponent<RectTransform>();
            marker.rectTransform.SetParent(parent, false);
            marker.rectTransform.anchorMin = Vector2.zero;
            marker.rectTransform.anchorMax = Vector2.zero;
            marker.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            marker.rectTransform.sizeDelta = Vector2.zero;

            GameObject dotObject = new GameObject("Position",
                typeof(RectTransform), typeof(Image));
            RectTransform dot = dotObject.GetComponent<RectTransform>();
            dot.SetParent(marker.rectTransform, false);
            dot.anchorMin = new Vector2(0.5f, 0.5f);
            dot.anchorMax = new Vector2(0.5f, 0.5f);
            dot.pivot = new Vector2(0.5f, 0.5f);
            dot.anchoredPosition = Vector2.zero;
            dot.sizeDelta = new Vector2(4f, 4f);
            dotObject.GetComponent<Image>().color = new Color(1f, 0.82f, 0.28f, 1f);

            GameObject textObject = new GameObject("Name",
                typeof(RectTransform), typeof(TextMeshProUGUI));
            RectTransform textRect = textObject.GetComponent<RectTransform>();
            textRect.SetParent(marker.rectTransform, false);
            textRect.anchorMin = new Vector2(0.5f, 0.5f);
            textRect.anchorMax = new Vector2(0.5f, 0.5f);
            textRect.pivot = new Vector2(0.5f, 0f);
            textRect.anchoredPosition = new Vector2(0f, 5f);
            textRect.sizeDelta = new Vector2(100f, 18f);

            marker.nameText = textObject.GetComponent<TextMeshProUGUI>();
            marker.nameText.alignment = TextAlignmentOptions.Bottom;
            marker.nameText.textWrappingMode = TextWrappingModes.NoWrap;
            marker.nameText.overflowMode = TextOverflowModes.Ellipsis;
            marker.nameText.raycastTarget = false;
            marker.nameText.color = Color.white;
            marker.textTemplate = nativeTextTemplate;
            if (nativeTextTemplate != null)
            {
                marker.nameText.font = nativeTextTemplate.font;
                marker.nameText.fontSharedMaterial =
                    nativeTextTemplate.fontSharedMaterial;
                SephiriaEnhancements.Integration.NativeLocalizedText.BindFont(marker.nameText, nativeTextTemplate);
            }

            UnityEngine.UI.Shadow shadow =
                textObject.AddComponent<UnityEngine.UI.Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.92f);
            shadow.effectDistance = new Vector2(1.5f, -1.5f);
            shadow.useGraphicAlpha = true;
            return marker;
        }

        internal void Set(string label, Vector2 mapPosition)
        {
            rectTransform.anchoredPosition = mapPosition;
            if (textTemplate != null)
                SephiriaEnhancements.Integration.NativeLocalizedText.MatchFontSize(nameText, textTemplate);
            if (nameText.text != label)
            {
                nameText.text = label;
            }
        }
    }
}
