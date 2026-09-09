using System.Collections.Generic;
using HarmonyLib;
using SephiriaEnhancements.Configuration;
using SephiriaEnhancements.Integration;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace SephiriaEnhancements.AutoCasting.Integration
{
    internal sealed class NativeAutoCastingUi : MonoBehaviour
    {
        private UI_CharacterStatusPanel panel;
        private TextMeshProUGUI hint;
        private TextMeshProUGUI textTemplate;
        private Image focus;
        private readonly List<RaycastResult> hits = new List<RaycastResult>();
        private readonly List<AutoCastingMarker> markers = new List<AutoCastingMarker>();
        private float refreshAt;
        private static int lastToggleFrame = -1;
        internal static bool IsEditable(UI_CharacterStatusPanel panel) =>
            EnhancementsSettings.Enabled && panel != null && panel.IsOpened && panel.IsControlEnabled &&
            UIManager.Instance?.CurrentControlStack != null &&
            UIManager.Instance.CurrentControlStack[0] == panel;

        private void Awake() => panel = GetComponent<UI_CharacterStatusPanel>();

        private void LateUpdate()
        {
            if (textTemplate == null)
            {
                UI_NewInventoryIcon reference = panel.GetComponentInChildren<UI_NewInventoryIcon>(true);
                if (reference == null || reference.quantityText == null) return;
                textTemplate = reference.quantityText;
            }
            if (Time.unscaledTime >= refreshAt)
            {
                refreshAt = Time.unscaledTime + 0.2f;
                foreach (UI_PlayerSkillIcon icon in panel.skillIconZone.GetComponentsInChildren<UI_PlayerSkillIcon>())
                    AddMarker(icon.gameObject, icon, null);
                UI_SkillQuickSlotBar bar = UIManager.Instance.GetElement<UI_SkillQuickSlotBar>();
                if (bar != null)
                    foreach (UI_SkillQuickSlotElement slot in bar.GetComponentsInChildren<UI_SkillQuickSlotElement>(true))
                        AddMarker(slot.gameObject, null, slot);
            }
            if (!IsEditable(panel))
            {
                if (hint != null) hint.gameObject.SetActive(false);
                if (focus != null) focus.gameObject.SetActive(false);
                return;
            }
            UpdateFocus();
            UI_PlayerSkillIcon selected = EventSystem.current?.currentSelectedGameObject?
                .GetComponent<UI_PlayerSkillIcon>();
            UpdateHint(selected);
            InputAction shortcut = NativeInputActions.FindShortcut(
                PlayerInputController.Instance?.playerInput?.actions, ModShortcuts.SwitchLockedTarget);
            if (shortcut?.WasPressedThisFrame() != true) return;
            if (shortcut.activeControl?.device is Mouse)
            {
                selected = null;
                if (Mouse.current != null && EventSystem.current != null)
                {
                    hits.Clear();
                    EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current)
                        { position = Mouse.current.position.ReadValue() }, hits);
                    if (hits.Count > 0)
                        selected = hits[0].gameObject.GetComponentInParent<UI_PlayerSkillIcon>();
                }
            }
            Toggle(selected);
        }

        private void AddMarker(GameObject target, UI_PlayerSkillIcon icon, UI_SkillQuickSlotElement slot)
        {
            if (target.GetComponent<AutoCastingMarker>() != null) return;
            AutoCastingMarker marker = target.AddComponent<AutoCastingMarker>();
            marker.Initialize(textTemplate, icon, slot);
            markers.Add(marker);
        }

        internal static void Toggle(UI_PlayerSkillIcon icon)
        {
            if (lastToggleFrame == Time.frameCount || icon == null ||
                !IsEditable(icon.GetComponentInParent<UI_CharacterStatusPanel>())) return;
            if (NativeAutoCasting.Current?.Toggle(icon.Magic) == true) lastToggleFrame = Time.frameCount;
        }

        internal static void DisposeAll()
        {
            foreach (NativeAutoCastingUi view in Object.FindObjectsByType<NativeAutoCastingUi>(
                FindObjectsInactive.Include, FindObjectsSortMode.None)) Destroy(view);
            foreach (NativeAutoCastingOptions option in Object.FindObjectsByType<NativeAutoCastingOptions>(
                FindObjectsInactive.Include, FindObjectsSortMode.None)) Destroy(option.gameObject);
        }

        private void UpdateHint(UI_PlayerSkillIcon selected)
        {
            if (hint == null)
            {
                hint = CreateText(panel.skillPanel, textTemplate, "AutoCastingHint");
                RectTransform rect = hint.rectTransform;
                rect.anchorMin = new Vector2(0f, 0f);
                rect.anchorMax = new Vector2(1f, 0f);
                rect.pivot = new Vector2(0.5f, 0f);
                rect.offsetMin = new Vector2(7f, 23f);
                rect.offsetMax = new Vector2(-7f, 47f);
                hint.alignment = TextAlignmentOptions.BottomLeft;
                UI_CommonTooltipOpener help = hint.gameObject.AddComponent<UI_CommonTooltipOpener>();
                help.tooltipName = new LocalizedString(AutoCastingLocalization.Title);
                help.tooltipContext = new LocalizedString(AutoCastingLocalization.Help);
                help.UpdateTooltipData();
                hint.raycastTarget = true;
            }
            bool supported = selected != null && NativeAutoCasting.Current?.CanSelect(selected.Magic) == true;
            hint.gameObject.SetActive(supported);
            if (!supported) return;
            bool on = NativeAutoCasting.Current.IsSelected(selected.Magic);
            string binding = BindingLabel();
            hint.text = ModLocalization.Get(AutoCastingLocalization.Title) + ": " +
                ModLocalization.Get(on ? AutoCastingLocalization.On : AutoCastingLocalization.Off) +
                "\n" + string.Format(ModLocalization.Get(AutoCastingLocalization.Hint), binding);
            NativeLocalizedText.SetShrinkOnlySize(hint, textTemplate.fontSize, textTemplate.fontSize * 0.75f);
        }

        private static string BindingLabel()
        {
            bool gamepad = PlayerInputController.Instance?.playerInput?.currentControlScheme == ModShortcuts.GamepadScheme;
            InputAction action = gamepad ? UIInputModule.currentModule?.submit?.action :
                NativeInputActions.FindShortcut(PlayerInputController.Instance?.playerInput?.actions,
                    ModShortcuts.SwitchLockedTarget);
            if (action == null) return string.Empty;
            var labels = new List<string>();
            foreach (InputControl control in action.controls)
            {
                if (gamepad != (control.device is Gamepad)) continue;
                int index = action.GetBindingIndexForControl(control);
                if (index >= 0)
                {
                    string label = action.GetBindingDisplayString(index);
                    if (!labels.Contains(label)) labels.Add(label);
                }
            }
            return string.Join(" / ", labels);
        }

        private void UpdateFocus()
        {
            GameObject selected = EventSystem.current?.currentSelectedGameObject;
            bool sideControl = selected != null &&
                ((selected.GetComponent<UI_PlayerSkillIcon>() != null && selected.transform.IsChildOf(panel.skillIconZone)) ||
                 (selected.GetComponent<UI_SetEffectElement>() != null && selected.transform.IsChildOf(panel.setEffectZone)) ||
                 selected == panel.showWeaponToggle.gameObject);
            if (!sideControl) { if (focus != null) focus.gameObject.SetActive(false); return; }
            if (focus == null)
            {
                UI_NewInventoryIcon reference = panel.GetComponentInChildren<UI_NewInventoryIcon>();
                Animator2D_UI template = reference?.selectedImage;
                if (template == null) return;
                Animator2D_UI selection = Instantiate(template, panel.transform);
                selection.name = "InventorySideSelection";
                selection.frameMoveType = EAnimator2DFrameMoveType.UNSCALED;
                focus = selection.image;
                focus.color = Color.white;
                focus.raycastTarget = false;
                // Side controls lay out their children; the focus frame is an overlay.
                focus.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            }
            focus.transform.SetParent(selected.transform, false);
            focus.transform.SetAsLastSibling();
            focus.rectTransform.anchorMin = Vector2.zero;
            focus.rectTransform.anchorMax = Vector2.one;
            focus.rectTransform.offsetMin = new Vector2(-1f, -1f);
            focus.rectTransform.offsetMax = new Vector2(1f, 1f);
            focus.gameObject.SetActive(true);
        }

        internal static TextMeshProUGUI CreateText(Transform parent, TextMeshProUGUI template, string name)
        {
            var text = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI))
                .GetComponent<TextMeshProUGUI>();
            text.transform.SetParent(parent, false);
            text.font = template.font;
            text.fontSharedMaterial = template.fontSharedMaterial;
            text.color = template.color;
            text.raycastTarget = false;
            text.margin = Vector4.zero;
            NativeLocalizedText.BindFont(text, template);
            NativeLocalizedText.MatchFontSize(text, template);
            return text;
        }

        private void OnDestroy()
        {
            if (focus != null) Destroy(focus.gameObject);
            if (hint != null) Destroy(hint.gameObject);
            foreach (AutoCastingMarker marker in markers) if (marker != null) Destroy(marker);
        }
    }

    internal sealed class AutoCastingMarker : MonoBehaviour
    {
        private TextMeshProUGUI text;
        private TextMeshProUGUI template;
        private UI_PlayerSkillIcon icon;
        private UI_SkillQuickSlotElement slot;
        internal void Initialize(TextMeshProUGUI template, UI_PlayerSkillIcon icon, UI_SkillQuickSlotElement slot)
        {
            this.icon = icon;
            this.slot = slot;
            this.template = template;
            text = NativeAutoCastingUi.CreateText(slot != null ? slot.iconImage.transform.parent : transform,
                template, "AutoCastingMark");
            text.text = "A";
            text.alignment = TextAlignmentOptions.BottomLeft;
            text.rectTransform.anchorMin = Vector2.zero;
            text.rectTransform.anchorMax = new Vector2(0.45f, 0.45f);
            text.rectTransform.offsetMin = Vector2.one;
            text.rectTransform.offsetMax = Vector2.zero;
        }
        private void LateUpdate()
        {
            if (text == null) return;
            if (template != null) NativeLocalizedText.MatchFontSize(text, template);
            if (slot != null)
            {
                RectTransform iconRect = slot.iconImage.rectTransform;
                RectTransform markRect = text.rectTransform;
                markRect.anchorMin = iconRect.anchorMin;
                markRect.anchorMax = iconRect.anchorMax;
                markRect.pivot = iconRect.pivot;
                markRect.anchoredPosition = iconRect.anchoredPosition;
                markRect.sizeDelta = iconRect.sizeDelta;
                text.margin = Vector4.one;
                text.transform.SetAsLastSibling();
            }
            NativeAutoCasting controller = NativeAutoCasting.Current;
            int index = slot == null ? -1 : slot.buttonIndex >= 100 ? slot.buttonIndex - 100 + 8 : slot.buttonIndex;
            Charm_Magic magic = icon != null ? icon.Magic : controller?.MagicAt(index);
            text.gameObject.SetActive(EnhancementsSettings.Enabled && controller?.IsSelected(magic) == true);
        }
        private void OnDestroy() { if (text != null) Destroy(text.gameObject); }
    }

    [HarmonyPatch(typeof(UI_CharacterStatusPanel), "Update")]
    internal static class AutoCastingPanelPatch
    {
        private static void Postfix(UI_CharacterStatusPanel __instance)
        {
            if (__instance.GetComponent<NativeAutoCastingUi>() == null)
                __instance.gameObject.AddComponent<NativeAutoCastingUi>();
        }
    }

    [HarmonyPatch(typeof(UI_HorayButton), nameof(UI_HorayButton.OnSubmit))]
    internal static class AutoCastingSubmitPatch
    {
        private static bool Prefix(UI_HorayButton __instance, BaseEventData eventData)
        {
            if (PlayerInputController.Instance?.playerInput?.currentControlScheme != ModShortcuts.GamepadScheme)
                return true;
            UI_PlayerSkillIcon icon = __instance.GetComponent<UI_PlayerSkillIcon>();
            if (icon == null || NativeAutoCasting.Current?.CanSelect(icon.Magic) != true ||
                !NativeAutoCastingUi.IsEditable(icon.GetComponentInParent<UI_CharacterStatusPanel>())) return true;
            NativeAutoCastingUi.Toggle(icon);
            eventData.Use();
            return false;
        }
    }
}
