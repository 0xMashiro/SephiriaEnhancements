using SephiriaEnhancements.Combat;
using SephiriaEnhancements.Configuration;
using SephiriaEnhancements.Core;
using SephiriaEnhancements.Presentation;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace SephiriaEnhancements.Integration
{
    // A native control-stack entry keeps UI navigation separate from gameplay input.
    internal sealed class NativeStatisticsBrowser : UIBase
    {
        private readonly CombatInsightsHud report = new CombatInsightsHud();
        private CombatInsightsController controller;
        private RectTransform content;
        private Button recentTab, floorTab, closeButton;
        private TextMeshProUGUI recentLabel, floorLabel, closeLabel, emptyLabel;
        private bool ownsPause;
        private int openedFrame;
        private float nextProjection, reportHeight;

        internal static NativeStatisticsBrowser Create(UI_PausePanel pause,
            CombatInsightsController controller)
        {
            TextMeshProUGUI template = pause.GetComponentInChildren<TextMeshProUGUI>(true);
            if (template == null) return null;
            var root = new GameObject("Sephiria Enhancements — Statistics Browser",
                typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
            root.SetActive(false);
            var rect = (RectTransform)root.transform;
            rect.SetParent(pause.ParentRoot.transform, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            root.GetComponent<Image>().color = new Color(0.025f, 0.025f, 0.035f, 0.96f);
            var browser = root.AddComponent<NativeStatisticsBrowser>();
            browser.SetRoot(pause.ParentRoot);
            browser.hasControl = true;
            browser.isPlayerUITHing = true;
            browser.controller = controller;
            browser.Build(template);
            return browser;
        }

        private void Build(TextMeshProUGUI template)
        {
            content = new GameObject("Content", typeof(RectTransform)).GetComponent<RectTransform>();
            content.SetParent(transform, false);
            content.anchorMin = content.anchorMax = new Vector2(0.5f, 0.5f);
            content.sizeDelta = new Vector2(640f, 360f);
            report.AttachBrowser(content,
                UIManager.Instance?.GetElement<UI_PlayerMP>()?.mpBar?.valueText ?? template);
            recentTab = MakeButton("Recent Encounter", template, out recentLabel);
            floorTab = MakeButton("Current Floor", template, out floorLabel);
            closeButton = MakeButton("Close", template, out closeLabel);
            recentTab.onClick.AddListener(() => SelectPage(false));
            floorTab.onClick.AddListener(() => SelectPage(true));
            closeButton.onClick.AddListener(Close);
            recentTab.navigation = new Navigation
            {
                mode = Navigation.Mode.Explicit,
                selectOnRight = floorTab,
                selectOnDown = closeButton
            };
            floorTab.navigation = new Navigation
            {
                mode = Navigation.Mode.Explicit,
                selectOnLeft = recentTab,
                selectOnDown = closeButton
            };
            closeButton.navigation = new Navigation
            {
                mode = Navigation.Mode.Explicit,
                selectOnUp = recentTab
            };
            emptyLabel = MakeLabel("Empty", template, content);
            emptyLabel.rectTransform.sizeDelta = new Vector2(288f, 48f);
            defaultSelectable = recentTab.gameObject;
        }

        internal void Show(bool pauseGame)
        {
            ownsPause = pauseGame;
            if (ownsPause) GameTimeManager.Instance?.Pause();
            openedFrame = Time.frameCount;
            nextProjection = 0f;
            defaultSelectable = controller.PreferFloorStatistics
                ? floorTab.gameObject : recentTab.gameObject;
            transform.SetAsLastSibling();
            Project(null, null);
            Open();
        }

        public override void OnClosed()
        {
            if (ownsPause) GameTimeManager.Instance?.ResetTimeScaleTo1();
            ownsPause = false;
            report.Hide();
        }

        private void Update()
        {
            if (!controller.CanBrowseStatistics)
            {
                Close();
                return;
            }
            if (!IsControlEnabled || Time.frameCount == openedFrame) return;
            var asset = PlayerInputController.Instance?.playerInput?.actions;
            InputAction previous = NativeInputActions.FindAction(asset, NativeUiActions.PrevTab);
            InputAction next = NativeInputActions.FindAction(asset, NativeUiActions.NextTab);
            if (previous?.WasPressedThisFrame() == true) SelectPage(false);
            else if (next?.WasPressedThisFrame() == true) SelectPage(true);
            if (Time.unscaledTime >= nextProjection)
            {
                nextProjection = Time.unscaledTime + 0.2f;
                Project(previous, next);
            }
            Rect bounds = rectTransform.rect;
            float scale = EncounterReportLayout.FitBrowserScale(bounds.width, bounds.height,
                reportHeight, ModSettings.DamageStatisticsScale);
            content.localScale = new Vector3(scale, scale, 1f);
        }

        private void SelectPage(bool floor)
        {
            controller.PreferFloorStatistics = floor;
            nextProjection = 0f;
            DoControlSelection(floor ? floorTab.gameObject : recentTab.gameObject);
        }

        private void Project(InputAction previous, InputAction next)
        {
            bool floor = controller.PreferFloorStatistics;
            CombatStatisticsSnapshot snapshot = floor ? controller.FloorStatistics
                : controller.EncounterReport;
            bool empty = snapshot == null || (snapshot.TotalDamage <= 0f && snapshot.DefeatedCount == 0);
            emptyLabel.gameObject.SetActive(empty);
            if (empty)
            {
                report.Hide();
                reportHeight = 110f;
                emptyLabel.text = ModLocalization.Get(floor ? ModLocalization.FloorStatisticsEmpty
                    : ModLocalization.EncounterReportUnavailable);
            }
            else reportHeight = report.DrawBrowser(snapshot, floor);
            recentLabel.text = PageLabel(NativeReportDismissal.BindingLabel(previous),
                ModLocalization.RecentEncounterStatistics, !floor);
            floorLabel.text = PageLabel(NativeReportDismissal.BindingLabel(next),
                ModLocalization.CurrentFloorStatistics, floor);
            closeLabel.text = PageLabel(NativeReportDismissal.BindingLabel(),
                ModLocalization.CloseStatistics, false);
            Position(recentTab, -78f, reportHeight / 2f + 18f, 148f);
            Position(floorTab, 78f, reportHeight / 2f + 18f, 148f);
            Position(closeButton, 0f, -reportHeight / 2f - 20f, 210f);
        }

        private static string PageLabel(string binding, string key, bool selected) =>
            (selected ? "●  " : string.Empty) + ModLocalization.Get(key) +
            (string.IsNullOrEmpty(binding) ? string.Empty : "  [" + binding + "]");

        private Button MakeButton(string name, TextMeshProUGUI template,
            out TextMeshProUGUI label)
        {
            var obj = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            obj.transform.SetParent(content, false);
            var background = obj.GetComponent<Image>();
            background.color = Color.white;
            var button = obj.GetComponent<Button>();
            button.targetGraphic = background;
            ColorBlock colors = button.colors;
            colors.normalColor = new Color(0.22f, 0.25f, 0.28f, 1f);
            colors.selectedColor = colors.highlightedColor = new Color(0.58f, 0.76f, 0.38f);
            button.colors = colors;
            label = MakeLabel("Label", template, (RectTransform)obj.transform);
            label.rectTransform.anchorMin = Vector2.zero;
            label.rectTransform.anchorMax = Vector2.one;
            label.rectTransform.offsetMin = new Vector2(4f, 0f);
            label.rectTransform.offsetMax = new Vector2(-4f, 0f);
            return button;
        }

        private static TextMeshProUGUI MakeLabel(string name, TextMeshProUGUI template,
            RectTransform parent)
        {
            var obj = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            obj.transform.SetParent(parent, false);
            var label = obj.GetComponent<TextMeshProUGUI>();
            label.font = template.font;
            label.fontSharedMaterial = template.fontSharedMaterial;
            label.fontSize = 10f;
            label.fontSizeMin = 8f;
            label.fontSizeMax = 10f;
            label.enableAutoSizing = true;
            label.alignment = TextAlignmentOptions.Center;
            label.richText = false;
            label.raycastTarget = false;
            NativeLocalizedText.BindFont(label, template);
            return label;
        }

        private static void Position(Button button, float x, float y, float width)
        {
            var rect = (RectTransform)button.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(x, y);
            rect.sizeDelta = new Vector2(width, 24f);
        }

        private void OnDestroy() => report.Dispose();
    }
}
