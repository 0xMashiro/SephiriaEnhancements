using TMPro;
using UnityEngine;

namespace SephiriaEnhancements.Integration
{
    internal static class NativeLocalizedText
    {
        // Refresh with the native peer so language and font-option changes remain authoritative.
        // Use this for labels with the same role, such as adjacent action buttons.
        internal static void MatchFontSize(TextMeshProUGUI text, TextMeshProUGUI template)
        {
            SetShrinkOnlySize(text, template.fontSize,
                Mathf.Min(template.fontSizeMin, template.fontSize * 0.75f));
        }

        // referenceSize is the intended size in the target canvas's units, before fitting.
        // Distinct HUD roles may supply their own template-relative size and readability floor.
        internal static void SetShrinkOnlySize(TextMeshProUGUI text, float referenceSize,
            float minimumSize)
        {
            text.fontSizeMax = referenceSize;
            text.fontSizeMin = Mathf.Min(minimumSize, referenceSize);
            text.enableAutoSizing = true;
        }

        internal static void BindFont(TextMeshProUGUI text, TextMeshProUGUI template)
        {
            // OnEnable reads text immediately; configure the native component while inactive.
            bool active = text.gameObject.activeSelf;
            text.gameObject.SetActive(false);
            var changer = text.gameObject.AddComponent<UI_LocalizationFontChanger>();
            changer.text = text;
            changer.style = template.GetComponent<UI_LocalizationFontChanger>()?.style ?? "Default";
            text.gameObject.SetActive(active);
        }
    }
}
