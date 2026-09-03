using TMPro;

namespace SephiriaEnhancements.Integration
{
    internal static class NativeLocalizedText
    {
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
