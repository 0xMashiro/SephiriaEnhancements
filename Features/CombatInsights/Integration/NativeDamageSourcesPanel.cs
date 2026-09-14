using System.Collections.Generic;
using System.Linq;
using Mirror;
using SephiriaEnhancements.Combat;
using SephiriaEnhancements.Configuration;
using SephiriaEnhancements.Runtime;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SephiriaEnhancements.Integration
{
    internal sealed class NativeDamageSourcesPanel : MonoBehaviour
    {
        private UI_DamageStatsUI panel;
        private GameObject controls;
        private RectTransform scrollRect;
        private Vector2 originalScrollTop;
        private UI_HorayButton previous, next, trainingButton, clearButton;
        private TextMeshProUGUI playerLabel, statusLabel;
        private TextMeshProUGUI buttonTemplate;
        private PlayerAvatar selected, local;
        private string selectedName;
        private readonly List<PlayerAvatar> players = new List<PlayerAvatar>();
        private Dictionary<DamageKey, float> displayed;
        private float displayedReceived = -1f, nextRefresh;
        private bool training, allAreas, disposed;
        private int sessionSerial;
        private string localFloor;
        private ScrollRect scroll;
        private string language;
        private bool hasPresentation;
        private bool ownsPause;
        private int openedFrame;
        private bool needsInitialFocus;
        private UI_HorayButton currentAreaButton, allAreasButton;
        private Navigation currentAreaNavigation, allAreasNavigation, scrollbarNavigation;
        private bool currentAreaAabb, allAreasAabb;

        internal void Show(UI_DamageStatsUI owner)
        {
            if (controls == null) Build(owner);
            enabled = true;
            local = selected = LocalPlayerResolver.Resolve();
            selectedName = selected != null ? selected.Name : string.Empty;
            localFloor = local != null ? local.NetworkcurrentFloorGuid : null;
            sessionSerial = DungeonManager.Instance != null ? DungeonManager.Instance.sessionSerial : 0;
            allAreas = owner.allLocationSelected.activeSelf;
            training = false;
            displayed = null;
            hasPresentation = false;
            TrainingStatisticsBridge.ResetView();
            Refresh();
            openedFrame = Time.frameCount;
            needsInitialFocus = true;
        }

        private void Build(UI_DamageStatsUI owner)
        {
            panel = owner;
            UI_HorayButton template = owner.currentLocationSelected.GetComponentInParent<UI_HorayButton>();
            currentAreaButton = template;
            allAreasButton = owner.allLocationSelected.GetComponentInParent<UI_HorayButton>();
            currentAreaNavigation = currentAreaButton.navigation;
            allAreasNavigation = allAreasButton.navigation;
            currentAreaAabb = currentAreaButton.useAABBNav;
            allAreasAabb = allAreasButton.useAABBNav;
            currentAreaButton.useAABBNav = allAreasButton.useAABBNav = false;
            buttonTemplate = template.GetComponentInChildren<TextMeshProUGUI>(true);
            scroll = panel.dealZone.GetComponentInParent<ScrollRect>();
            if (scroll.verticalScrollbar != null) scrollbarNavigation = scroll.verticalScrollbar.navigation;
            scrollRect = (RectTransform)scroll.transform;
            originalScrollTop = scrollRect.offsetMax;
            // Native tabs and totals stay in place; reserve two control rows and
            // a two-line status above the existing scrollable source list.
            scrollRect.offsetMax = new Vector2(originalScrollTop.x, originalScrollTop.y - 110f);
            controls = new GameObject("Sephiria Enhancements — Damage Sources", typeof(RectTransform));
            var root = (RectTransform)controls.transform;
            root.SetParent(scrollRect.parent, false);
            root.anchorMin = new Vector2(0f, 1f); root.anchorMax = Vector2.one;
            root.pivot = new Vector2(0.5f, 1f);
            root.anchoredPosition = new Vector2(0f, -110f); root.sizeDelta = new Vector2(-28f, 74f);
            previous = MakeButton(template, root, "Previous Player", 0f, 0f, 64f, () => CyclePlayer(-1));
            next = MakeButton(template, root, "Next Player", 268f, 0f, 64f, () => CyclePlayer(1));
            playerLabel = MakeLabel(root, "Player", buttonTemplate, 68f, 0f, 196f, 22f);
            trainingButton = MakeButton(template, root, "Training", 0f, -26f, 162f, () =>
            {
                training = true; hasPresentation = false;
                TrainingStatisticsBridge.ResetView();
                Refresh();
            });
            clearButton = MakeButton(template, root, "Clear Own Training", 170f, -26f, 162f, () =>
            {
                if (training && selected == LocalPlayerResolver.Resolve() && TrainingStatisticsBridge.Query(selected, true))
                {
                    hasPresentation = false;
                    nextRefresh = 0f;
                }
            });
            statusLabel = MakeLabel(root, "Status", panel.receivedDamageTitleText, 0f, -52f, 332f, 30f);
            foreach (var button in new[] { previous, next, trainingButton, clearButton })
                button.navigation = new Navigation { mode = Navigation.Mode.Automatic };
        }

        internal void SelectNative(bool all)
        {
            if (panel == null || disposed || !TrainingDamageStatistics.Available) return;
            allAreas = all; training = false; hasPresentation = false;
            TrainingStatisticsBridge.ResetView();
            Refresh();
            if (panel.IsControlEnabled)
                panel.DoControlSelection((all ? panel.allLocationSelected : panel.currentLocationSelected)
                    .GetComponentInParent<UI_HorayButton>().gameObject);
        }

        private void Update()
        {
            if (!TrainingDamageStatistics.Available) { Dispose(); return; }
            FeatureFailure.Run(FeatureId.CombatInsights, () =>
            {
                if (local != LocalPlayerResolver.Resolve() || !TrainingDamageStatistics.Ready(local) ||
                    localFloor != local.NetworkcurrentFloorGuid || DungeonManager.Instance == null ||
                    sessionSerial != DungeonManager.Instance.sessionSerial)
                {
                    panel.Close();
                    return;
                }
                if (Time.unscaledTime >= nextRefresh) Refresh();
                UpdateControls();
            });
        }

        private void UpdateControls()
        {
            if (!panel.IsControlEnabled || Time.frameCount == openedFrame) return;
            var asset = PlayerInputController.Instance?.playerInput?.actions;
            var previousAction = NativeInputActions.FindAction(asset, NativeUiActions.PrevTab);
            var nextAction = NativeInputActions.FindAction(asset, NativeUiActions.NextTab);
            previous.text.text = PlayerButtonLabel("‹", NativeReportDismissal.BindingLabel(previousAction));
            next.text.text = PlayerButtonLabel("›", NativeReportDismissal.BindingLabel(nextAction));
            if (players.Count > 1)
            {
                if (previousAction?.WasPressedThisFrame() == true) CyclePlayer(-1);
                else if (nextAction?.WasPressedThisFrame() == true) CyclePlayer(1);
            }

            // Skip unavailable player/clear controls. The native scrollbar keeps
            // up/down for scrolling; left/right returns to the control rows.
            bool multiple = players.Count > 1;
            Selectable clear = clearButton.interactable ? clearButton : trainingButton;
            Scrollbar bar = scroll.verticalScrollbar;
            Selectable list = bar != null && bar.IsActive() && bar.IsInteractable() ? bar : null;
            SetNavigation(currentAreaButton, null, multiple ? previous : trainingButton, null, allAreasButton);
            SetNavigation(allAreasButton, null, multiple ? next : clear, currentAreaButton, null);
            SetNavigation(previous, currentAreaButton, trainingButton, null, next);
            SetNavigation(next, allAreasButton, clear, previous, null);
            SetNavigation(trainingButton, multiple ? previous : currentAreaButton, list, null, clearButton.interactable ? clearButton : list);
            SetNavigation(clearButton, multiple ? next : allAreasButton, list, trainingButton, null);
            if (bar != null) SetNavigation(bar, null, null, trainingButton, clear);

            var events = EventSystem.current;
            if (events == null) return;
            GameObject focused = events.currentSelectedGameObject;
            Selectable selectable = focused != null ? focused.GetComponent<Selectable>() : null;
            bool usable = selectable != null && selectable.IsActive() && selectable.IsInteractable() &&
                focused.transform.IsChildOf(panel.transform);
            bool navigating = NativeInputActions.FindAction(asset, NativeUiActions.Navigate)?.WasPressedThisFrame() == true ||
                NativeInputActions.FindAction(asset, NativeUiActions.Submit)?.WasPressedThisFrame() == true;
            if (!usable && (needsInitialFocus || navigating || focused != null))
                events.SetSelectedGameObject((training ? trainingButton : allAreas ? allAreasButton : currentAreaButton).gameObject);
            needsInitialFocus = false;
        }

        private static string PlayerButtonLabel(string arrow, string binding) =>
            string.IsNullOrEmpty(binding) ? arrow : arrow + " " + binding;

        private static void SetNavigation(Selectable button, Selectable up, Selectable down, Selectable left, Selectable right)
        {
            button.navigation = new Navigation { mode = Navigation.Mode.Explicit,
                selectOnUp = up, selectOnDown = down, selectOnLeft = left, selectOnRight = right };
        }

        private void CyclePlayer(int direction)
        {
            if (players.Count == 0) return;
            int index = players.IndexOf(selected);
            selected = players[(index + direction + players.Count) % players.Count];
            selectedName = selected.Name;
            hasPresentation = false;
            scroll.verticalNormalizedPosition = 1f;
            TrainingStatisticsBridge.ResetView();
            Refresh();
            // A selection change can disable the clear button, so retain focus
            // on the player control that performed the change.
            panel.DoControlSelection(direction < 0 ? previous.gameObject : next.gameObject);
        }

        private void Refresh()
        {
            nextRefresh = Time.unscaledTime + 0.5f;
            string currentLanguage = LocalizationManager.Instance?.CurrentLanguage;
            if (language != currentLanguage) { language = currentLanguage; hasPresentation = false; }
            players.Clear();
            foreach (var identity in NetworkClient.spawned.Values)
            {
                PlayerAvatar avatar = identity != null ? identity.GetComponent<PlayerAvatar>() : null;
                if (avatar != null) players.Add(avatar);
            }
            players.Sort((a, b) => a == local ? b == local ? 0 : -1 : b == local ? 1 : a.netId.CompareTo(b.netId));
            previous.interactable = next.interactable = players.Count > 1;
            foreach (var button in new[] { previous, next, trainingButton, clearButton })
                NativeLocalizedText.MatchFontSize(button.text, buttonTemplate);
            NativeLocalizedText.MatchFontSize(playerLabel, buttonTemplate);
            NativeLocalizedText.MatchFontSize(statusLabel, panel.receivedDamageTitleText);
            if (selected != null) selectedName = selected.Name;
            playerLabel.text = string.Format(ModLocalization.Get(DamageSourcesLocalization.Player), selectedName);
            trainingButton.text.text = (training ? "● " : string.Empty) + ModLocalization.Get(DamageSourcesLocalization.Training);
            clearButton.text.text = ModLocalization.Get(DamageSourcesLocalization.ClearMine);
            clearButton.interactable = training && TrainingStatisticsBridge.CanClear(selected);
            panel.currentLocationSelected.SetActive(!training && !allAreas);
            panel.allLocationSelected.SetActive(!training && allAreas);
            panel.receivedDamageTitleText.text = (allAreas ? panel.receivedDamageTitle : panel.receivedDamageTitle_lastLocation).ToString();
            panel.totalReceivedDamageTitleText.text = (allAreas ? panel.totalReceivedDamageTitle : panel.totalReceivedDamageTitle_lastLocation).ToString();

            if (selected == null || !players.Contains(selected) || !TrainingDamageStatistics.Ready(selected))
            {
                Present(null, 0f, DamageSourcesLocalization.PlayerUnavailable);
                return;
            }
            if (!training)
            {
                // Snapshot the synchronized dictionary. Never combine it with
                // local damage feedback, or bind the global UI to a teammate.
                var damage = (allAreas ? selected.dealsStatistics : selected.dealsStatistics_LastLocation)
                    .ToDictionary(pair => pair.Key, pair => pair.Value);
                Present(damage, allAreas ? selected.receivedDamage : selected.receivedDamage_LastLocation,
                    damage.Count == 0 ? DamageSourcesLocalization.Empty : DamageSourcesLocalization.NativeScope);
                return;
            }

            panel.totalReceivedDamageTitleText.text = ModLocalization.Get(DamageSourcesLocalization.Training);
            if (selected.NetworkcurrentFloorGuid != localFloor)
            {
                Present(null, 0f, DamageSourcesLocalization.SameArea);
                return;
            }
            if (!TrainingStatisticsBridge.Supported)
            {
                Present(null, 0f, DamageSourcesLocalization.HostRequired);
                return;
            }
            TrainingStatisticsBridge.Query(selected);
            clearButton.interactable = TrainingStatisticsBridge.CanClear(selected);
            var snapshot = TrainingStatisticsBridge.Latest;
            if (!TrainingStatisticsBridge.HasRecentReply || !snapshot.HasValue || snapshot.Value.Player != selected.netId || snapshot.Value.World != sessionSerial || snapshot.Value.Floor != localFloor)
                Present(null, 0f, DamageSourcesLocalization.Waiting);
            else if (snapshot.Value.Status != TrainingStatisticsStatus.Ready)
                Present(null, 0f, snapshot.Value.Status == TrainingStatisticsStatus.TooLarge
                    ? DamageSourcesLocalization.TooLarge : DamageSourcesLocalization.PlayerUnavailable);
            else Present(snapshot.Value.Damage, 0f, DamageSourcesLocalization.TrainingHelp);
            panel.receivedDamageValueText.text = "—";
        }

        private void Present(Dictionary<DamageKey, float> damage, float received, string status)
        {
            statusLabel.text = ModLocalization.Get(status);
            bool equal = hasPresentation && displayedReceived == received &&
                (displayed == null && damage == null || displayed != null && damage != null &&
                displayed.Count == damage.Count && displayed.All(pair => damage.TryGetValue(pair.Key, out float value) && value == pair.Value));
            if (equal) return;
            float position = scroll.verticalNormalizedPosition;
            panel.SetDealState(damage, received);
            displayed = damage; displayedReceived = received;
            hasPresentation = true;
            Canvas.ForceUpdateCanvases();
            scroll.verticalNormalizedPosition = position;
        }

        private UI_HorayButton MakeButton(UI_HorayButton template, RectTransform root, string name,
            float x, float y, float width, UnityEngine.Events.UnityAction action)
        {
            var button = Instantiate(template, root, false);
            button.name = name;
            button.onClick = new Button.ButtonClickedEvent();
            button.onClick.AddListener(() => FeatureFailure.Run(FeatureId.CombatInsights, () => action()));
            button.text = button.GetComponentInChildren<TextMeshProUGUI>(true);
            foreach (var translation in button.GetComponentsInChildren<UI_LocalizationStringText>(true)) translation.enabled = false;
            foreach (var fitter in button.GetComponentsInChildren<ContentSizeFitter>(true)) fitter.enabled = false;
            foreach (var layout in button.GetComponentsInChildren<LayoutGroup>(true)) layout.enabled = false;
            Transform selection = button.transform.Find("Select");
            if (selection != null) selection.gameObject.SetActive(false);
            Position((RectTransform)button.transform, x, y, width, 22f);
            button.text.rectTransform.anchorMin = Vector2.zero; button.text.rectTransform.anchorMax = Vector2.one;
            button.text.rectTransform.offsetMin = new Vector2(4f, 1f); button.text.rectTransform.offsetMax = new Vector2(-4f, -1f);
            button.text.alignment = TextAlignmentOptions.Center;
            button.SetForceNavUp(null); button.SetForceNavDown(null); button.SetForceNavLeft(null); button.SetForceNavRight(null);
            button.useAABBNav = false;
            return button;
        }

        private static TextMeshProUGUI MakeLabel(RectTransform root, string name, TextMeshProUGUI template,
            float x, float y, float width, float height)
        {
            var obj = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            obj.transform.SetParent(root, false);
            var label = obj.GetComponent<TextMeshProUGUI>();
            label.font = template.font; label.fontSharedMaterial = template.fontSharedMaterial;
            label.richText = false; label.raycastTarget = false; label.alignment = TextAlignmentOptions.Center;
            NativeLocalizedText.BindFont(label, template);
            NativeLocalizedText.MatchFontSize(label, template);
            Position(label.rectTransform, x, y, width, height);
            return label;
        }

        private static void Position(RectTransform rect, float x, float y, float width, float height)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f); rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(x, y); rect.sizeDelta = new Vector2(width, height);
        }

        internal void TakePause()
        {
            ownsPause = true;
            GameTimeManager.Instance?.Pause();
        }

        private void OnDisable()
        {
            TrainingStatisticsBridge.ResetView();
            if (ownsPause) GameTimeManager.Instance?.ResetTimeScaleTo1();
            ownsPause = false;
        }

        internal void Dispose()
        {
            Restore(true);
            if (this != null) DestroyImmediate(this);
        }

        private void Restore(bool refreshNative)
        {
            if (disposed) return;
            disposed = true; enabled = false;
            if (scrollRect != null) scrollRect.offsetMax = originalScrollTop;
            if (currentAreaButton != null) { currentAreaButton.navigation = currentAreaNavigation; currentAreaButton.useAABBNav = currentAreaAabb; }
            if (allAreasButton != null) { allAreasButton.navigation = allAreasNavigation; allAreasButton.useAABBNav = allAreasAabb; }
            if (scroll != null && scroll.verticalScrollbar != null) scroll.verticalScrollbar.navigation = scrollbarNavigation;
            GameObject focused = EventSystem.current?.currentSelectedGameObject;
            if (refreshNative && panel != null && panel.IsControlEnabled && controls != null && focused != null &&
                focused.transform.IsChildOf(controls.transform)) panel.DoControlSelection(panel.defaultSelectable);
            if (controls != null) { controls.SetActive(false); DestroyImmediate(controls); }
            if (refreshNative && panel != null && panel.IsOpened)
            {
                if (allAreas) panel.AllLocationDamageButtonClick(); else panel.CurrentLocationDamageButtonClick();
            }
            TrainingStatisticsBridge.ResetView();
        }

        private void OnDestroy() => Restore(false);
    }
}
