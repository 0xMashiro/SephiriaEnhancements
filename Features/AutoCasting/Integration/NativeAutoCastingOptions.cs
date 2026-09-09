using HarmonyLib;
using SephiriaEnhancements.Configuration;
using SephiriaEnhancements.Integration;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace SephiriaEnhancements.AutoCasting.Integration
{
    internal sealed class NativeAutoCastingOptions : MonoBehaviour
    {
        private int slot;
        private Button button;
        private TextMeshProUGUI text;
        private TextMeshProUGUI template;
        private Vector4 previousMargin;

        internal static void AddToRows(UI_OptionsPanel panel)
        {
            foreach (RebindActionUI binding in panel.GetComponentsInChildren<RebindActionUI>(true))
            {
                InputAction action = binding.actionReference?.action;
                int slot = NativeAutoCastingBindings.SlotFor(action?.actionMap?.name, action?.name);
                if (slot < 0) continue;
                Transform row = NativeRebindSectionBuilder.FindRowRoot(binding, panel.transform);
                if (row == null || row.GetComponentInChildren<NativeAutoCastingOptions>(true) != null) continue;
                UI_LocalizationStringText label = row.GetComponentInChildren<UI_LocalizationStringText>(true);
                if (label == null) continue;
                TextMeshProUGUI labelText = label.GetComponent<TextMeshProUGUI>();
                Button reset = null;
                foreach (Button candidate in row.GetComponentsInChildren<Button>(true))
                    if (candidate.GetComponent<RebindActionUI>() == null &&
                        candidate.GetComponentInChildren<RebindActionUI>(true) == null)
                    { reset = candidate; break; }
                if (labelText == null || reset == null) continue;
                GameObject root = Object.Instantiate(reset.gameObject, label.transform);
                root.name = "AutoCastingOption";
                root.SetActive(false);
                var option = root.AddComponent<NativeAutoCastingOptions>();
                option.slot = slot;
                option.template = labelText;
                option.previousMargin = labelText.margin;
                option.button = root.GetComponent<Button>();
                // RESET is drawn into its sprite. Use the native binding button's blank frame.
                root.GetComponent<Image>().sprite = binding.GetComponent<Image>().sprite;
                option.text = NativeAutoCastingUi.CreateText(root.transform, labelText, "AutoCastingOptionText");
                option.text.alignment = TextAlignmentOptions.Center;
                option.text.rectTransform.anchorMin = Vector2.zero;
                option.text.rectTransform.anchorMax = Vector2.one;
                option.text.rectTransform.offsetMin = new Vector2(2f, 0f);
                option.text.rectTransform.offsetMax = new Vector2(-2f, 0f);
                option.button.onClick = new Button.ButtonClickedEvent();
                option.button.onClick.AddListener(() =>
                {
                    NativeAutoCasting controller = NativeAutoCasting.Current;
                    controller?.Toggle(controller.MagicAt(option.slot));
                });
                UI_CommonTooltipOpener tooltip = root.GetComponent<UI_CommonTooltipOpener>() ??
                    root.AddComponent<UI_CommonTooltipOpener>();
                tooltip.tooltipName = new LocalizedString(AutoCastingLocalization.Title);
                tooltip.tooltipContext = new LocalizedString(AutoCastingLocalization.Help);
                tooltip.UpdateTooltipData();
                root.SetActive(true);
            }
        }

        private void Update()
        {
            NativeAutoCasting controller = NativeAutoCasting.Current;
            Charm_Magic magic = controller?.MagicAt(slot);
            bool supported = EnhancementsSettings.Enabled && controller?.CanSelect(magic) == true;
            button.interactable = supported;
            text.text = supported ? "A: " + ModLocalization.Get(controller.IsSelected(magic) ?
                AutoCastingLocalization.On : AutoCastingLocalization.Off) : "—";
            NativeLocalizedText.MatchFontSize(text, template);
            RectTransform rect = (RectTransform)transform;
            float width = Mathf.Min(64f, template.rectTransform.rect.width * 0.4f);
            rect.anchorMin = rect.anchorMax = new Vector2(1f, 0.5f);
            rect.pivot = new Vector2(1f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(width, template.rectTransform.rect.height);
            template.margin = new Vector4(previousMargin.x, previousMargin.y, previousMargin.z + width + 3f, previousMargin.w);
        }

        private void OnDestroy() { if (template != null) template.margin = previousMargin; }
    }

    [HarmonyPatch(typeof(UI_OptionsPanel), nameof(UI_OptionsPanel.SelectTab))]
    internal static class AutoCastingOptionsPatch
    {
        private static void Postfix(UI_OptionsPanel __instance) => NativeAutoCastingOptions.AddToRows(__instance);
    }
}
