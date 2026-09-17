using static SephiriaEnhancements.Inventory.NativeInventoryHudControls;
#nullable disable
using SephiriaEnhancements.Runtime.Inventory;
using SephiriaEnhancements.Integration;
using SephiriaEnhancements.Runtime;
using SephiriaEnhancements.KeyboardUiNavigation;
using UnityEngine.InputSystem;

using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SephiriaEnhancements.Inventory
{
    internal enum InventoryOptimizationHudPhase
    {
        Ready,
        Searching,
        CheckingItems,
        Applying
    }

    internal sealed partial class InventoryOptimizationHud : IDisposable
    {
        private const int IntentSlots = InventoryOptimizationHudLayout.IntentSlotsPerPage;
        private const float LauncherWidth = 30f;
        private const float LauncherHeight = 30f;
        private const float PanelWidth = InventoryOptimizationHudLayout.Width;
        private const float PanelGap = 10f;
        private const float PanelHeight = InventoryOptimizationHudLayout.Height;
        private const float ProjectionInterval = 0.15f;
        private static readonly Color Background =
            new(0.055f, 0.05f, 0.075f, 0.96f);
        private readonly Vector3[] inventoryWorldCorners = new Vector3[4];
        private NativeInventoryComboGoalEditor comboEditor;
        private NativeInventoryArtifactIntentCommands artifactCommands;
        private readonly List<IntentSlot> prioritySlots = new();
        private readonly List<IntentSlot> avoidSlots = new();
        private readonly InventoryOptimizationNavigationBridge navigationBridge =
            new InventoryOptimizationNavigationBridge();
        private NativeInventoryHudControls controls;
        private NativeInventoryArtifactGoalEditor goalEditor;
        private GameObject root;
        private RectTransform attachedInventoryZone;
        private UI_CharacterStatusPanel attachedPanel;
        private Image panelBackground;
        private TextMeshProUGUI title;
        private TextMeshProUGUI status;
        private Button editGoals;
        private TextMeshProUGUI editGoalsText;
        private InventoryItemKey? previewItemKey;
        private Button launcher;
        private Image launcherIcon;
        private Button close;
        private Button previousPage;
        private Button nextPage;
        private Button markPriorities;
        private Button optimize;
        private TextMeshProUGUI previousPageText;
        private TextMeshProUGUI nextPageText;
        private TextMeshProUGUI markPrioritiesText;
        private TextMeshProUGUI optimizeText;
        private TextMeshProUGUI closeText;
        private TextMeshProUGUI priorityQueueTitle;
        private TextMeshProUGUI avoidZoneTitle;
        private TextMeshProUGUI boardHint;
        private TextMeshProUGUI comboTargetsTitle;

        private GameObject lastCustomSelection;

        private InventorySnapshot currentSnapshot;
        private InventoryIntentResultFeedback resultFeedback;
        private int intentPage;
        private bool panelOpen;
        private bool detailsExpanded;
        private float nextAttachAt;
        private float nextProjectionAt;
        private Action requestOptimization;
        private Action<InventoryOptimizationPreferences> replacePreferences;
        private Action togglePriorityMarking;
        private Action endPriorityMarking;
        private bool priorityMarking;
        private readonly InventoryIntentInteractionState interaction = new();
        private NativeInventoryIntentPickupView pickupView;
        private NativeInventoryIntentDropFilter nativeDropFilter;

        private GameObject lastInventorySelection;

        internal bool HasArtifactPickup => interaction.HasPickup;

        internal void PreviewArtifact(InventoryItemKey itemKey)
        {
            previewItemKey = itemKey;
            nextProjectionAt = 0f;
        }

        private InventoryOptimizationHudPhase currentPhase;
        private NativeInventoryOptimizationViewTemplates nativeTemplates;

        internal void Update(bool allowed, InventoryOptimizationHudPhase phase,
            InventorySnapshot snapshot, Action optimizeAction,
            Action<InventoryOptimizationPreferences> replaceAction,
            bool markingPriorities,
            Action toggleMarkingAction, Action endMarkingAction,
            InventoryIntentResultFeedback feedback = null)
        {
            if (!ReferenceEquals(resultFeedback, feedback)) nextProjectionAt = 0f;
            resultFeedback = feedback;
            currentPhase = phase;
            currentSnapshot = snapshot;
            UI_CharacterStatusPanel openPanel = null;
            bool visible = allowed && StandardInventoryContext.TryGetOpenInventory(
                out GridInventory _, out openPanel);
            if (!visible)
            {
                lastInventorySelection = null;
                lastCustomSelection = null;
                SuspendEditing();
                panelOpen = false;
                preferencesExpanded = false;
                detailsExpanded = false;
                intentPage = 0;
                endPriorityMarking?.Invoke();
                ApplyDisclosureLayout();
                SetVisible(false);
                return;
            }

            float now = Time.unscaledTime;
            if ((root == null || attachedPanel != openPanel ||
                    attachedInventoryZone != openPanel.inventoryZone) &&
                now >= nextAttachAt)
            {
                nextAttachAt = now + 1f;
                if (StandardInventoryContext.TryGetOpenView(
                    out StandardInventoryViewContext viewContext))
                {
                    Attach(viewContext);
                }
            }
            if (root == null || attachedPanel != openPanel ||
                attachedInventoryZone != openPanel.inventoryZone)
            {
                SuspendEditing();
                SetVisible(false);
                return;
            }

            requestOptimization = optimizeAction;
            replacePreferences = replaceAction;
            togglePriorityMarking = toggleMarkingAction;
            endPriorityMarking = endMarkingAction;
            priorityMarking = markingPriorities;
            TrackInventorySelection();
            interaction.SetEditable(panelOpen && phase ==
                InventoryOptimizationHudPhase.Ready);
            UpdateArtifactPickup();
            PositionBesideInventory();
            SetVisible(true);
            RefreshNavigation();
            root.transform.SetAsLastSibling();
            HandleIntentRemoval();
            HandleLevelEditShortcut();
            HandlePageShortcut();
            if (now < nextProjectionAt)
            {
                return;
            }
            nextProjectionAt = now + ProjectionInterval;
            Project(phase, snapshot);
            RefreshPageNavigation();
        }

        internal void Reset()
        {
            DestroyRoot();
            intentPage = 0;
            panelOpen = false;
            preferencesExpanded = false;
            detailsExpanded = false;
            nextAttachAt = 0f;
            nextProjectionAt = 0f;
            requestOptimization = null;
            replacePreferences = null;
            togglePriorityMarking = null;
            endPriorityMarking = null;
            priorityMarking = false;
            lastInventorySelection = null;
            lastCustomSelection = null;
            SuspendEditing();
        }

        public void Dispose() => Reset();

        private void Attach(StandardInventoryViewContext context)
        {
            RectTransform inventoryZone = context?.InventoryZone;
            TextMeshProUGUI template = context?.TextTemplate;
            Canvas canvas = context?.Canvas;
            RectTransform canvasRoot = canvas?.rootCanvas?.transform as
                RectTransform;
            if (inventoryZone == null || canvasRoot == null ||
                template?.font == null || context?.ViewTemplates == null)
            {
                return;
            }

            DestroyRoot();
            SuspendEditing();
            panelOpen = false;
            preferencesExpanded = false;
            detailsExpanded = false;
            intentPage = 0;
            nativeTemplates = context.ViewTemplates;
            controls = new NativeInventoryHudControls(nativeTemplates, ClearArtifactPickup, ChangePage);
            attachedInventoryZone = inventoryZone;
            attachedPanel = context.Panel;
            artifactCommands = new NativeInventoryArtifactIntentCommands(attachedPanel, interaction, ReplacePreferences);
            lastInventorySelection = null;
            lastCustomSelection = null;
            root = new GameObject(
                "Sephiria Enhancements — Smart Inventory",
                typeof(RectTransform), typeof(CanvasGroup), typeof(Image),
                typeof(Canvas), typeof(GraphicRaycaster),
                typeof(InventoryIntentPanelDropTarget));
            RectTransform rect = root.GetComponent<RectTransform>();
            rect.SetParent(canvasRoot, false);
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0f, 0.5f);
            rect.sizeDelta = new Vector2(PanelWidth, PanelHeight);

            // Share the native ordering: inventory < board < item picker.
            // Raising above every character-panel canvas also covers the picker.
            Canvas overlay = root.GetComponent<Canvas>();
            overlay.overrideSorting = true;
            overlay.sortingLayerID = nativeTemplates.DragCanvas.sortingLayerID;
            overlay.sortingOrder = nativeTemplates.DragCanvas.sortingOrder - 1;

            CanvasGroup group = root.GetComponent<CanvasGroup>();
            group.interactable = true;
            group.blocksRaycasts = true;
            panelBackground = root.GetComponent<Image>();
            ApplyImageStyle(panelBackground,
                nativeTemplates.WindowBackground,
                Background);
            panelBackground.raycastTarget = true;
            root.GetComponent<InventoryIntentPanelDropTarget>().Configure(
                ClearArtifactPickup, ChangePage);
            pickupView = new NativeInventoryIntentPickupView(overlay,
                nativeTemplates.DragCanvas, ClearArtifactPickup, ChangePage);
            nativeDropFilter = attachedPanel.itemDropZone.gameObject
                .AddComponent<NativeInventoryIntentDropFilter>();
            nativeDropFilter.Bind(rect);

            title = CreateText("Title", rect, template,
                new Vector2(24f, -20f), new Vector2(268f, 30f),
                TextAlignmentOptions.MidlineLeft);
            title.color = PrimaryText;

            priorityQueueTitle = CreateText("PriorityQueueTitle", rect,
                template, new Vector2(24f, -102f), new Vector2(312f, 22f),
                TextAlignmentOptions.MidlineLeft);
            priorityQueueTitle.color = PrimaryText;
            avoidZoneTitle = CreateText("AvoidZoneTitle", rect, template,
                new Vector2(24f, -190f), new Vector2(312f, 22f),
                TextAlignmentOptions.MidlineLeft);
            avoidZoneTitle.color = PrimaryText;
            for (int index = 0; index < IntentSlots; index++)
            {
                prioritySlots.Add(CreateIntentSlot(rect, template, index,
                    new Vector2(20f + index * 54f,
                        -InventoryOptimizationHudLayout.PrioritySlotsTop),
                    placeInPriorityQueue: true));
                avoidSlots.Add(CreateIntentSlot(rect, template, index,
                    new Vector2(20f + index * 54f,
                        -InventoryOptimizationHudLayout.AvoidSlotsTop),
                    placeInPriorityQueue: false));
            }
            boardHint = CreateText("BoardHint", rect, template,
                new Vector2(24f, -InventoryOptimizationHudLayout.HintTop),
                new Vector2(312f, InventoryOptimizationHudLayout.HintHeight),
                TextAlignmentOptions.TopLeft);
            boardHint.color = SecondaryText;
            boardHint.textWrappingMode = TextWrappingModes.Normal;
            boardHint.fontSize *= 0.75f;

            launcher = CreateNativeLauncher(rect, template, OpenPanel,
                out TextMeshProUGUI launcherText, out launcherIcon);
            launcherText.gameObject.SetActive(false);
            editGoals = controls.CreateButton("EditGoals", rect, template,
                new Vector2(24f, -InventoryOptimizationHudLayout.DetailsTop),
                new Vector2(148f, InventoryOptimizationHudLayout.DetailsHeight),
                EditPreviewedGoals, out editGoalsText);
            editGoals.interactable = false;
            close = controls.CreateButton("Close", rect, template,
                new Vector2(308f, -20f), new Vector2(28f, 28f),
                ClosePanel, out closeText);
            closeText.text = "×";

            comboTargetsTitle = CreateText("ComboTargetsTitle", rect, template,
                new Vector2(24f, -102f), new Vector2(144f, 32f), TextAlignmentOptions.MidlineLeft);
            NativeLocalizedText.SetShrinkOnlySize(comboTargetsTitle,
                comboTargetsTitle.fontSize, comboTargetsTitle.fontSize * 0.75f);
            goalEditor = new NativeInventoryArtifactGoalEditor(rect, template, nativeTemplates, controls, EditArtifactGoal, CloseLevelEditor);

            comboEditor = new NativeInventoryComboGoalEditor(rect, template, controls,
                interaction, EditComboGoal, () => nextProjectionAt = 0f);

            previousPage = controls.CreateButton("PreviousPage", rect, template,
                new Vector2(24f, -InventoryOptimizationHudLayout.BoardPagingTop),
                new Vector2(48f, InventoryOptimizationHudLayout.PagingHeight),
                () => ChangePage(-1), out previousPageText);
            nextPage = controls.CreateButton("NextPage", rect, template,
                new Vector2(288f, -InventoryOptimizationHudLayout.BoardPagingTop),
                new Vector2(48f, InventoryOptimizationHudLayout.PagingHeight),
                () => ChangePage(1), out nextPageText);
            status = CreateText("Status", rect, template,
                new Vector2(66f, -InventoryOptimizationHudLayout.BoardPagingTop),
                new Vector2(228f, InventoryOptimizationHudLayout.PagingHeight),
                TextAlignmentOptions.Center);
            status.color = SecondaryText;
            NativeLocalizedText.SetShrinkOnlySize(status, status.fontSize, status.fontSize * 0.75f);

            markPriorities = controls.CreateButton("MarkPriorities", rect, template,
                new Vector2(24f, -InventoryOptimizationHudLayout.ActionsTop),
                new Vector2(148f, InventoryOptimizationHudLayout.ActionsHeight),
                () => togglePriorityMarking?.Invoke(),
                out markPrioritiesText);
            optimize = controls.CreateButton("Optimize", rect, template,
                new Vector2(188f, -InventoryOptimizationHudLayout.ActionsTop),
                new Vector2(148f, InventoryOptimizationHudLayout.ActionsHeight),
                () => requestOptimization?.Invoke(), out optimizeText);

            CreateArrangementActions(rect, template);

            ApplyDisclosureLayout();
            root.transform.SetAsLastSibling();
            PositionBesideInventory();
            nextProjectionAt = 0f;
        }

        private IntentSlot CreateIntentSlot(RectTransform parent,
            TextMeshProUGUI template, int index, Vector2 position,
            bool placeInPriorityQueue)
        {
            GameObject slotObject = new("IntentSlot" + index,
                typeof(RectTransform), typeof(Image),
                typeof(InventoryIntentDropTarget));
            RectTransform rect = slotObject.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            SetTopRect(rect, position, new Vector2(InventoryOptimizationHudLayout.SlotSize, InventoryOptimizationHudLayout.SlotSize));
            Image background = slotObject.GetComponent<Image>();
            UI_SubBagIcon nativeIcon = nativeTemplates.Slot;
            ApplyImageStyle(background, nativeIcon?.bgImage,
                new Color(0.18f, 0.19f, 0.24f, 0.98f));
            // Inventory background materials depend on the native hierarchy's
            // stencil state. Reusing them here can render an opaque white tile.
            background.material = null;
            background.sprite = nativeIcon?.defaultBGSprite;
            background.color = Color.white;
            background.raycastTarget = true;

            GameObject iconObject = new("ItemIcon", typeof(RectTransform),
                typeof(Image));
            RectTransform iconRect = iconObject.GetComponent<RectTransform>();
            iconRect.SetParent(rect, false);
            iconRect.anchorMin = new Vector2(0.08f, 0.08f);
            iconRect.anchorMax = new Vector2(0.92f, 0.92f);
            iconRect.offsetMin = iconRect.offsetMax = Vector2.zero;
            Image itemIcon = iconObject.GetComponent<Image>();
            itemIcon.preserveAspect = true;
            itemIcon.raycastTarget = false;
            itemIcon.enabled = false;

            TextMeshProUGUI marker = CreateText("Marker", rect, nativeIcon.quantityText,
                Vector2.zero, new Vector2(InventoryOptimizationHudLayout.SlotSize, InventoryOptimizationHudLayout.SlotSize),
                TextAlignmentOptions.TopLeft,
                childCoordinates: true);
            marker.color = placeInPriorityQueue ? TitleColor : SecondaryText;
            marker.margin = new Vector4(8f, 8f, 8f, 8f);

            var slot = new IntentSlot
            {
                Root = slotObject,
                Tooltip = slotObject.AddComponent<NativeInventoryArtifactTooltip>(),
                Background = background,
                Icon = itemIcon,
                Marker = marker,
                Index = index,
                PriorityQueue = placeInPriorityQueue
            };
            slot.Button = NativeInventoryOptimizationControls.AddButton(
                slotObject, nativeIcon?.button);
            Image nativeHighlight = nativeIcon?.button?.targetGraphic as Image;
            if (nativeHighlight?.sprite != null && nativeHighlight != nativeIcon.bgImage)
            {
                GameObject highlightObject = new("Selection", typeof(RectTransform),
                    typeof(Image));
                RectTransform highlightRect = highlightObject.GetComponent<RectTransform>();
                highlightRect.SetParent(rect, false);
                highlightRect.anchorMin = Vector2.zero;
                highlightRect.anchorMax = Vector2.one;
                highlightRect.offsetMin = highlightRect.offsetMax = Vector2.zero;
                Image highlight = highlightObject.GetComponent<Image>();
                ApplyImageStyle(highlight, nativeHighlight, Color.clear);
                highlight.raycastTarget = false;
                slot.Button.targetGraphic = highlight;
            }
            else
            {
                slot.Button.targetGraphic = background;
            }
            // Native inventory labels render above the selection frame.
            marker.transform.SetAsLastSibling();
            GameObject resultObject = new("ResultStatus", typeof(RectTransform), typeof(Image));
            var resultRect = resultObject.GetComponent<RectTransform>();
            resultRect.SetParent(rect, false);
            resultRect.anchorMin = new Vector2(0.08f, 0.02f);
            resultRect.anchorMax = new Vector2(0.92f, 0.09f);
            resultRect.offsetMin = resultRect.offsetMax = Vector2.zero;
            slot.ResultStatus = resultObject.GetComponent<Image>();
            slot.ResultStatus.raycastTarget = false;
            slot.Button.onClick.AddListener(() => ActivateIntentSlot(slot));
            slot.Tooltip.Configure(() => interaction.HasPickup);
            slotObject.GetComponent<InventoryIntentDropTarget>().Configure(
                interaction, icon => DropIntoIntentSlot(slot, icon),
                () => PlaceHeldArtifact(slot), () => BeginArtifactPickup(slot, dragging: true),
                EndArtifactDrag, () => RemoveIntentSlot(slot));
            slotObject.GetComponent<InventoryIntentDropTarget>().ConfigureSelection(
                () => PreviewIntentSlot(slot));
            return slot;
        }

        private void HandleLevelEditShortcut()
        {
            if (!panelOpen || !preferencesExpanded || detailsExpanded || !interaction.Editable || interaction.HasPickup ||
                NativeInventoryIntentDrop.HasHeldItem)
            {
                return;
            }
            var action = NativeInventoryLevelEditShortcut.PressedAction(attachedPanel);
            if (action == null) return;
            IntentSlot slot = null;
            if (action.activeControl?.device is not Mouse)
            {
                var selected = EventSystem.current?.currentSelectedGameObject;
                slot = prioritySlots.Concat(avoidSlots).FirstOrDefault(candidate => candidate.Root == selected);
            }
            if (slot == null && !NativeInventoryIntentPickupView.UsesSelection && action.activeControl?.device is not Gamepad &&
                InputDeviceState.TryGetPointerPosition(out var position))
            {
                var canvas = root.GetComponentInParent<Canvas>();
                var camera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
                slot = prioritySlots.Concat(avoidSlots).FirstOrDefault(candidate => candidate.Root.activeInHierarchy &&
                    RectTransformUtility.RectangleContainsScreenPoint(
                        candidate.Root.transform as RectTransform, position, camera));
            }
            EditArtifactGoals(slot, action.activeControl?.device is not Mouse);
        }

        private void EditPreviewedGoals()
        {
            var slot = prioritySlots.Concat(avoidSlots).FirstOrDefault(candidate =>
                previewItemKey.HasValue && candidate.Preference?.ItemKey == previewItemKey);
            EditArtifactGoals(slot, true);
        }

        private void EditArtifactGoals(IntentSlot slot, bool selectEditor)
        {
            if (!panelOpen || !preferencesExpanded || detailsExpanded || !interaction.Editable || interaction.HasPickup ||
                NativeInventoryIntentDrop.HasHeldItem || slot?.Root.activeInHierarchy != true ||
                slot.Preference == null) return;
            if (!artifactCommands.TryOpenGoal(slot.Preference.ItemKey, out var preferences)) return;
            previewItemKey = slot.Preference.ItemKey;
            endPriorityMarking?.Invoke();
            slot.Tooltip.Hide();
            if (selectEditor && interaction.LevelTarget.HasValue)
            {
                ProjectLevelEditor(preferences);
                if (goalEditor.Visible)
                    goalEditor.SelectEntry();
            }
            nextProjectionAt = 0f;
        }

        private void ProjectLevelEditor(InventoryOptimizationPreferences preferences)
        {
            var rule = preferences.ArtifactPreferences.FirstOrDefault(candidate =>
                candidate.ItemKey == interaction.LevelTarget);
            var item = currentSnapshot?.Items.FirstOrDefault(candidate => candidate.ItemKey == interaction.LevelTarget);
            bool show = interaction.Editable && !interaction.HasPickup && !NativeInventoryIntentDrop.HasHeldItem &&
                rule != null && item?.Artifact != null && artifactCommands.HasArtifact(rule.ItemKey);
            if (!show) interaction.CancelLevelEdit();
            goalEditor.SetVisible(show);
            foreach (var slot in prioritySlots.Concat(avoidSlots)) slot.Root.SetActive(!show);
            priorityQueueTitle.gameObject.SetActive(!show);
            avoidZoneTitle.gameObject.SetActive(!show);
            previousPage.gameObject.SetActive(!show);
            nextPage.gameObject.SetActive(!show);
            status.gameObject.SetActive(!show);
            markPriorities.gameObject.SetActive(!show);
            boardHint.gameObject.SetActive(!show);
            editGoals.gameObject.SetActive(!show && editGoals.interactable);
            clearArtifactPriorities.gameObject.SetActive(panelOpen && preferencesExpanded && !detailsExpanded && !show);
            if (!show) return;
            goalEditor.Render(rule, item, interaction.Editable, preferences.AllowAdditionalMagicCost, resultFeedback?.Find(rule.ItemKey));
        }

        private void EditArtifactGoal(InventoryArtifactGoalEdit edit)
        {
            if (artifactCommands == null || !artifactCommands.TryEditGoal(currentSnapshot, edit, out var preferences)) return;
            ProjectLevelEditor(preferences);
            nextProjectionAt = 0f;
        }

        private void Project(InventoryOptimizationHudPhase phase,
            InventorySnapshot snapshot)
        {
            RefreshArrangementActions();
            title.text = Loc._(!preferencesExpanded ? InventoryOptimizationLocalization.HudTitle
                : detailsExpanded ? InventoryArrangementLocalization.ComboPriorities : InventoryArrangementLocalization.ArtifactPriorities);
            InventoryOptimizationPreferences preferences =
                WorldSessionInventoryIntentStore.Capture();
            if (panelOpen && preferencesExpanded && !detailsExpanded)
            {
                ProjectIntentBoard(preferences);
            }
            editGoalsText.text = Loc._(InventoryOptimizationLocalization.HudEditGoals);
            if (!panelOpen)
            {
                return;
            }
            optimizeText.text = Loc._(phase == InventoryOptimizationHudPhase.CheckingItems
                ? InventoryItemRecoveryLocalization.Checking : phase == InventoryOptimizationHudPhase.Searching
                ? InventoryOptimizationLocalization.HudSearching
                : phase == InventoryOptimizationHudPhase.Applying
                    ? InventoryOptimizationLocalization.HudApplying : InventoryOptimizationLocalization.HudOptimize);
            bool editable = phase == InventoryOptimizationHudPhase.Ready;
            markPrioritiesText.text = Loc._(priorityMarking
                ? InventoryOptimizationLocalization.HudFinishMarking
                : InventoryOptimizationLocalization.HudMarkArtifacts);
            markPriorities.interactable = editable && !interaction.HasPickup &&
                !NativeInventoryIntentDrop.HasHeldItem;
            SetSelected(markPriorities, priorityMarking);
            // Keep unavailable/empty inventory requests reachable so the controller can explain why.
            optimize.interactable = editable && !interaction.HasPickup &&
                !NativeInventoryIntentDrop.HasHeldItem;
            previousPageText.text = "‹";
            nextPageText.text = "›";
            if (!preferencesExpanded || !detailsExpanded)
            {
                return;
            }

            comboTargetsTitle.text = Loc._(InventoryOptimizationLocalization.HudComboTargets);
            comboEditor.Render(snapshot, preferences, resultFeedback);
            previousPage.interactable = editable && comboEditor.Page > 0;
            nextPage.interactable = editable && comboEditor.Page + 1 < comboEditor.PageCount;
            status.text = phase switch
            {
                InventoryOptimizationHudPhase.CheckingItems => Loc._(InventoryItemRecoveryLocalization.Checking),
                InventoryOptimizationHudPhase.Searching =>
                    Loc._(InventoryOptimizationLocalization.HudSearching),
                InventoryOptimizationHudPhase.Applying =>
                    Loc._(InventoryOptimizationLocalization.HudApplying),
                _ when comboEditor.TargetCount == 0 =>
                    Loc._(InventoryOptimizationLocalization.HudNoTargets),
                _ => string.Format(Loc._(
                    InventoryOptimizationLocalization.HudPage), comboEditor.Page + 1,
                    comboEditor.PageCount)
            };
        }

        private void ProjectIntentBoard(
            InventoryOptimizationPreferences preferences)
        {
            priorityQueueTitle.text = Loc._(
                InventoryOptimizationLocalization.HudPriorityQueue);
            avoidZoneTitle.text = Loc._(
                InventoryOptimizationLocalization.HudAvoidZone);
            string binding = NativeInventoryLevelEditShortcut.BindingLabel;
            string removeBinding = NativeInventoryIntentDrop.RemoveBindingLabel;
            boardHint.text = NativeInventoryIntentPickupView.UsesSelection
                ? string.Format(Loc._(interaction.HasPickup
                    ? InventoryOptimizationLocalization.HudNavigationChooseIntentSlot
                    : InventoryOptimizationLocalization.HudNavigationBoardHint), removeBinding)
                : interaction.HasPickup ? Loc._(InventoryOptimizationLocalization.HudChooseIntentSlot)
                : string.Format(Loc._(InventoryOptimizationLocalization.HudIntentBoardHint),
                    string.IsNullOrEmpty(binding)
                        ? Loc._(InventoryOptimizationLocalization.HudLevelEditUnbound)
                        : string.Format(Loc._(InventoryOptimizationLocalization.HudEditGoalsShortcut), binding));
            ProjectLevelEditor(preferences);

            var sourceIcons = new Dictionary<InventoryItemKey, UI_NewInventoryIcon>();
            if (attachedPanel != null)
            {
                foreach (UI_NewInventoryIcon icon in attachedPanel.
                    GetComponentsInChildren<UI_NewInventoryIcon>(true))
                {
                    if (icon?.Item?.Charm != null &&
                        icon.Inventory == attachedPanel.PlayerAvatar?.Inventory)
                    {
                        sourceIcons[new InventoryItemKey(icon.Item.EntityID, icon.Item.InstanceID)] = icon;
                    }
                }
            }
            ArtifactOptimizationPreference[] priorities =
                InventoryArtifactIntentEditor.OrderedPriorities(preferences);
            ArtifactOptimizationPreference[] avoided =
                InventoryArtifactIntentEditor.AvoidedInstances(preferences);
            int pageCount = InventoryOptimizationHudLayout.IntentPageCount(
                InventoryArtifactIntentEditor.SlotCount(priorities),
                InventoryArtifactIntentEditor.SlotCount(avoided));
            intentPage = Mathf.Clamp(intentPage, 0, pageCount - 1);
            previousPage.interactable = interaction.Editable && intentPage > 0;
            nextPage.interactable = interaction.Editable && intentPage + 1 < pageCount;
            status.text = string.Format(Loc._(
                InventoryOptimizationLocalization.HudPage), intentPage + 1,
                pageCount);
            for (int index = 0; index < IntentSlots; index++)
            {
                int targetIndex = intentPage * IntentSlots + index;
                prioritySlots[index].Index = targetIndex;
                avoidSlots[index].Index = targetIndex;
                ProjectIntentSlot(prioritySlots[index],
                    priorities.FirstOrDefault(rule => rule.IntentSlotIndex == targetIndex),
                    sourceIcons);
                ProjectIntentSlot(avoidSlots[index],
                    avoided.FirstOrDefault(rule => rule.IntentSlotIndex == targetIndex),
                    sourceIcons);
            }
            ProjectHoveredGoal();
        }

        private void ProjectHoveredGoal()
        {
            bool canEdit = false;
            string hint = null;
            if (!interaction.HasPickup && !NativeInventoryIntentDrop.HasHeldItem &&
                !goalEditor.Visible)
            {
                var slots = prioritySlots.Concat(avoidSlots);
                IntentSlot hovered = null;
                if (!NativeInventoryIntentPickupView.UsesSelection && InputDeviceState.TryGetPointerPosition(out var pointer))
                {
                    var canvas = root.GetComponentInParent<Canvas>();
                    var camera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
                    hovered = slots.FirstOrDefault(slot => slot.Root.activeInHierarchy &&
                        RectTransformUtility.RectangleContainsScreenPoint(slot.Root.transform as RectTransform, pointer, camera));
                }
                hovered ??= slots.FirstOrDefault(slot =>
                    EventSystem.current?.currentSelectedGameObject == slot.Root);
                if (hovered?.Preference != null) previewItemKey = hovered.Preference.ItemKey;
                // Retain the preview while moving from the item to its Edit goals button.
                var rule = slots.FirstOrDefault(slot => slot.Root.activeInHierarchy &&
                    previewItemKey.HasValue && slot.Preference?.ItemKey == previewItemKey)?.Preference;
                var item = currentSnapshot?.Items.FirstOrDefault(candidate => candidate.ItemKey == rule?.ItemKey);
                if (rule == null || item?.Artifact == null)
                {
                    previewItemKey = null;
                }
                else
                {
                    canEdit = interaction.Editable && artifactCommands.HasArtifact(rule.ItemKey);
                    hint = item.Name + "\n" + InventoryOptimizationLocalization.FormatArtifactFeedback(rule, item.Artifact,
                        resultFeedback?.Find(rule.ItemKey), key => Loc._(key), WorldSessionInventoryIntentStore.Capture().AllowAdditionalMagicCost);
                }
            }

            // Assign the final state once. Toggling interactable off and on in the
            // same projection can make the controller treat the button as disabled
            // and recover to the native inventory default selection.
            editGoals.interactable = canEdit;
            editGoals.gameObject.SetActive(panelOpen && preferencesExpanded && !detailsExpanded && canEdit);
            if (hint != null) boardHint.text = hint;
            ConfigureIntentNavigation();
        }

        private void PreviewIntentSlot(IntentSlot slot)
        {
            if (slot?.Preference == null) return;
            previewItemKey = slot.Preference.ItemKey;
            nextProjectionAt = 0f;
            ProjectHoveredGoal();
        }

        private void ConfigureIntentNavigation()
        {
            if (editGoals == null) return;
            UI_HorayButton edit = editGoals as UI_HorayButton;
            if (edit == null) return;

            UI_HorayButton markButton = markPriorities as UI_HorayButton;
            UI_HorayButton optimizeButton = optimize as UI_HorayButton;
            for (int index = 0; index < IntentSlots; index++)
            {
                UI_HorayButton priority = prioritySlots[index].Button as UI_HorayButton;
                UI_HorayButton avoid = avoidSlots[index].Button as UI_HorayButton;
                priority?.SetForceNavDown(avoid);
                priority?.SetForceNavUp(index < IntentSlots / 2 ? preferencesToggle : markPriorities);
                avoid?.SetForceNavDown(edit.interactable ? edit : null);
            }

            IntentSlot preview = prioritySlots.Concat(avoidSlots).FirstOrDefault(slot =>
                slot.Root.activeInHierarchy && previewItemKey.HasValue &&
                slot.Preference?.ItemKey == previewItemKey);
            edit.SetForceNavUp(preview?.Button);
            edit.SetForceNavRight(clearArtifactPriorities.IsInteractable() ? clearArtifactPriorities : null);
            edit.SetForceNavDown(undoArrangement.interactable ? undoArrangement : optimize);
            var clear = (UI_HorayButton)clearArtifactPriorities;
            clear.SetForceNavLeft(edit.interactable ? edit : null);
            clear.SetForceNavUp(preview?.Button ?? avoidSlots[IntentSlots - 1].Button);
            clear.SetForceNavDown(optimize);
            markButton?.SetForceNavLeft(preferencesToggle);
            markButton?.SetForceNavDown(prioritySlots[0].Button);
            optimizeButton?.SetForceNavLeft(undoArrangement.interactable ? undoArrangement : null);
            optimizeButton?.SetForceNavUp(clear.IsInteractable() ? clear : edit.interactable ? edit : preferencesToggle);
        }

        private void ActivateIntentSlot(IntentSlot slot)
        {
            if (slot == null || !interaction.Editable)
            {
                return;
            }
            UI_NewInventoryIcon held = NativeInventoryIntentDrop.ConfirmedPickup;
            if (held != null)
            {
                if (artifactCommands.OwnsArtifact(held))
                {
                    DropIntoIntentSlot(slot, held);
                    NativeInventoryIntentDrop.ConsumeConfirmedPickup(held);
                }
                return;
            }
            if (interaction.HasPickup)
            {
                PlaceHeldArtifact(slot);
            }
            else
            {
                BeginArtifactPickup(slot, dragging: false);
            }
        }

        private void BeginArtifactPickup(IntentSlot slot, bool dragging)
        {
            if (artifactCommands == null || slot?.Icon.sprite == null || !artifactCommands.TryPickup(slot.Preference, dragging))
            {
                return;
            }
            endPriorityMarking?.Invoke();
            foreach (IntentSlot candidate in prioritySlots.Concat(avoidSlots))
            {
                candidate.Tooltip.Hide();
            }
            pickupView.Show(slot.Icon.sprite, slot.Root);
            if (NativeInventoryIntentPickupView.UsesSelection)
                EventSystem.current?.SetSelectedGameObject(slot.Root);
            RefreshPickupControls();
        }

        private void PlaceHeldArtifact(IntentSlot slot)
        {
            if (slot == null || !interaction.HasPickup)
            {
                return;
            }
            ArtifactOptimizationPreference held = interaction.Pickup;
            Sprite displacedSprite = slot.Icon.sprite;
            if (artifactCommands.TryPlacePickup(
                slot.PriorityQueue ? InventoryPreferenceLevel.Priority : InventoryPreferenceLevel.Avoid, slot.Index))
            {
                PreviewArtifact(held.ItemKey);
            }
            if (interaction.HasPickup && interaction.ItemKey != held.ItemKey)
            {
                pickupView.Show(displacedSprite, slot.Root);
                RefreshPickupControls();
                return;
            }
            ClearArtifactPickup();
        }

        private void EndArtifactDrag()
        {
            interaction.EndDrag();
            if (!interaction.HasPickup)
            {
                ClearArtifactPickup();
            }
        }

        private void UpdateArtifactPickup()
        {
            if (!interaction.HasPickup)
            {
                pickupView?.Hide();
                return;
            }
            if (!artifactCommands.ValidatePickup())
            {
                ClearArtifactPickup();
                return;
            }
            pickupView?.UpdateWithinPanel(root.transform);
        }

        internal void PrepareNativeInventoryInput(UI_CharacterStatusPanel panel)
        {
            if (panel == attachedPanel && interaction.HasPickup)
            {
                UpdateArtifactPickup();
            }
        }

        internal void SuspendForInventoryViewChange(UI_CharacterStatusPanel panel)
        {
            if (panel == attachedPanel)
            {
                SuspendEditing();
            }
        }

        private void RefreshPickupControls()
        {
            if (root == null) return;
            foreach (IntentSlot slot in prioritySlots.Concat(avoidSlots))
            {
                slot.Icon.enabled = slot.Icon.sprite != null;
                slot.Icon.color = interaction.HasPickup && interaction.ItemKey == slot.Preference?.ItemKey
                    ? new Color(1f, 1f, 1f, 0.25f) : Color.white;
            }
            bool canRun = interaction.Editable && !interaction.HasPickup &&
                !NativeInventoryIntentDrop.HasHeldItem;
            if (markPriorities != null)
            {
                markPriorities.interactable = canRun;
            }
            if (optimize != null && !canRun)
            {
                optimize.interactable = false;
            }
            if (editGoals != null && !canRun) editGoals.interactable = false;
            nextProjectionAt = 0f;
        }

        private void HandleIntentRemoval()
        {
            if (!interaction.Editable || NativeInventoryIntentDrop.HasHeldItem ||
                !NativeInventoryIntentDrop.WasRemovePressed)
            {
                return;
            }
            if (interaction.HasPickup)
            {
                CancelPickedUpMark();
                return;
            }
            GameObject selected = EventSystem.current?.currentSelectedGameObject;
            IntentSlot slot = prioritySlots.Concat(avoidSlots).FirstOrDefault(
                candidate => candidate.Root == selected);
            if (slot?.Preference != null)
            {
                RemoveIntentSlot(slot);
                ClearArtifactPickup();
            }
        }

        private void CancelPickedUpMark()
        {
            var key = interaction.ItemKey;
            ClearArtifactPickup();
            if (!NativeInventoryIntentPickupView.UsesSelection || !panelOpen ||
                !preferencesExpanded || detailsExpanded) return;
            var preferences = WorldSessionInventoryIntentStore.Capture();
            var rule = preferences.ArtifactPreferences.FirstOrDefault(candidate => candidate.ItemKey == key);
            if (rule == null) return;
            intentPage = rule.IntentSlotIndex / IntentSlots;
            previewItemKey = key;
            ProjectIntentBoard(preferences);
            var source = prioritySlots.Concat(avoidSlots).FirstOrDefault(slot =>
                slot.Preference?.ItemKey == key && slot.Root.activeInHierarchy);
            if (source != null) EventSystem.current?.SetSelectedGameObject(source.Root);
        }

        private void ClearArtifactPickup()
        {
            if (interaction.LevelTarget.HasValue && goalEditor?.Visible == true) CloseLevelEditor();
            interaction.CancelPickup();
            interaction.CancelLevelEdit();
            pickupView?.Hide();
            RefreshPickupControls();
        }

        internal void SuspendEditing()
        {
            previewItemKey = null;
            interaction.SetEditable(false);
            ClearArtifactPickup();
        }

        internal void CancelArtifactPickup() => ClearArtifactPickup();

        private void ProjectIntentSlot(IntentSlot slot,
            ArtifactOptimizationPreference preference,
            IReadOnlyDictionary<InventoryItemKey, UI_NewInventoryIcon> sourceIcons)
        {
            slot.Preference = preference;
            slot.Button.interactable = interaction.Editable;
            slot.Marker.text = slot.PriorityQueue
                ? (slot.Index + 1).ToString()
                : "×";
            if (preference?.Strength == InventoryConstraintStrength.Hard) slot.Marker.text += "!";
            if (interaction.HasPickup && interaction.ItemKey == preference?.ItemKey)
            {
                slot.Marker.text = "›" + slot.Marker.text;
            }
            UI_NewInventoryIcon source = null;
            if (preference != null)
            {
                sourceIcons.TryGetValue(preference.ItemKey, out source);
            }
            slot.Tooltip.SetItem(source?.Item);
            slot.Icon.sprite = source?.Item?.Entity?.Icon;
            slot.Icon.enabled = slot.Icon.sprite != null;
            slot.Icon.material = null;
            slot.Icon.color = interaction.HasPickup && interaction.ItemKey == preference?.ItemKey
                ? new Color(1f, 1f, 1f, 0.25f) : Color.white;
            slot.Background.overrideSprite = null;
            slot.Background.sprite = source?.bgImage?.sprite ??
                nativeTemplates.Slot.defaultBGSprite;
            slot.Background.material = null;
            slot.Background.color = Color.white;
            slot.ResultStatus.enabled = preference != null;
            slot.ResultStatus.color = SatisfactionColor(preference == null
                ? InventoryIntentSatisfaction.NotEvaluated
                : resultFeedback?.Find(preference.ItemKey)?.State ?? InventoryIntentSatisfaction.NotEvaluated);
        }

        private void DropIntoIntentSlot(IntentSlot slot, UI_NewInventoryIcon icon)
        {
            if (artifactCommands?.OwnsArtifact(icon) != true) return;
            if (slot != null && artifactCommands.TryPlaceInventoryArtifact(icon,
                    slot.PriorityQueue ? InventoryPreferenceLevel.Priority : InventoryPreferenceLevel.Avoid, slot.Index))
                PreviewArtifact(new InventoryItemKey(icon.Item.EntityID, icon.Item.InstanceID));
            ClearArtifactPickup();
        }

        private void RemoveIntentSlot(IntentSlot slot)
        {
            if (interaction.HasPickup)
            {
                ClearArtifactPickup();
                return;
            }
            if (artifactCommands?.TryRemove(slot?.Preference) != true) return;
            ClearArtifactPickup();
            nextProjectionAt = 0f;
        }

        private void EditComboGoal(string categoryId, InventoryComboGoalEdit edit)
        {
            if (!interaction.TryEditComboGoal(WorldSessionInventoryIntentStore.Capture(),
                    currentSnapshot, categoryId, edit, out var preferences)) return;
            ReplacePreferences(preferences);
            if (edit == InventoryComboGoalEdit.CycleChoice)
                comboEditor.ChoiceEdited(categoryId, preferences);
            nextProjectionAt = 0f;
        }

        private void ReplacePreferences(InventoryOptimizationPreferences preferences) =>
            replacePreferences?.Invoke(preferences);

        private void ChangePage(int delta)
        {
            if (!interaction.Editable || !preferencesExpanded || goalEditor.Visible)
            {
                return;
            }
            interaction.CancelLevelEdit();
            previewItemKey = null;
            if (detailsExpanded)
            {
                comboEditor.ChangePage(delta);
            }
            else
            {
                intentPage = Math.Max(0, intentPage + delta);
                ProjectIntentBoard(WorldSessionInventoryIntentStore.Capture());
            }
            nextProjectionAt = 0f;
        }

        private void OpenPanel()
        {
            bool launcherWasSelected = EventSystem.current?.currentSelectedGameObject ==
                launcher?.gameObject;
            previewItemKey = null;
            panelOpen = true;
            preferencesExpanded = false;
            detailsExpanded = false;
            ClearArtifactPickup();
            endPriorityMarking?.Invoke();
            interaction.SetEditable(currentPhase == InventoryOptimizationHudPhase.Ready);
            comboEditor?.ResetPage();
            ApplyDisclosureLayout();
            PositionBesideInventory();
            RefreshNavigation();
            if (launcherWasSelected && panelOpen)
            {
                SelectFirstCustomEntry();
            }
            nextProjectionAt = 0f;
        }

        private void ClosePanel()
        {
            bool customWasSelected = IsCustomSelection(
                EventSystem.current?.currentSelectedGameObject);
            endPriorityMarking?.Invoke();
            SuspendEditing();
            ClearPanelSelection();
            panelOpen = false;
            preferencesExpanded = false;
            detailsExpanded = false;
            comboEditor?.ResetPage();
            ApplyDisclosureLayout();
            PositionBesideInventory();
            RefreshNavigation();
            if (customWasSelected)
            {
                SelectFirstCustomEntry();
            }
            nextProjectionAt = 0f;
        }

        private void ApplyDisclosureLayout()
        {
            if (root == null)
            {
                return;
            }

            float width = panelOpen ? PanelWidth : LauncherWidth;
            float height = panelOpen ? OpenPanelHeight : LauncherHeight;
            (root.transform as RectTransform).sizeDelta =
                new Vector2(width, height);
            if (panelBackground != null)
            {
                panelBackground.enabled = panelOpen;
                panelBackground.raycastTarget = panelOpen;
            }
            title?.gameObject.SetActive(panelOpen);
            launcher?.gameObject.SetActive(!panelOpen);
            RefreshArrangementActions();
            bool showBoard = panelOpen && preferencesExpanded && !detailsExpanded;
            editGoals?.gameObject.SetActive(showBoard && editGoals.interactable);
            priorityQueueTitle?.gameObject.SetActive(showBoard);
            avoidZoneTitle?.gameObject.SetActive(showBoard);
            boardHint?.gameObject.SetActive(showBoard);
            goalEditor?.SetVisible(false);
            foreach (IntentSlot slot in prioritySlots)
            {
                slot.Root.SetActive(showBoard);
            }
            foreach (IntentSlot slot in avoidSlots)
            {
                slot.Root.SetActive(showBoard);
            }
            close?.gameObject.SetActive(panelOpen);
            markPriorities?.gameObject.SetActive(panelOpen && preferencesExpanded && !detailsExpanded);
            optimize?.gameObject.SetActive(panelOpen);
            bool showTargets = panelOpen && preferencesExpanded && detailsExpanded;
            comboTargetsTitle?.gameObject.SetActive(showTargets);
            previousPage?.gameObject.SetActive(panelOpen && preferencesExpanded);
            nextPage?.gameObject.SetActive(panelOpen && preferencesExpanded);
            status?.gameObject.SetActive(panelOpen && preferencesExpanded);
            float pagingY = -(showTargets
                ? InventoryOptimizationHudLayout.TargetPagingTop
                : InventoryOptimizationHudLayout.BoardPagingTop);
            if (previousPage != null)
            {
                SetTopRect((RectTransform)previousPage.transform, new Vector2(showTargets ? 172f : 24f, pagingY),
                    new Vector2(showTargets ? 28f : 48f, InventoryOptimizationHudLayout.PagingHeight));
                SetTopRect((RectTransform)nextPage.transform, new Vector2(showTargets ? 308f : 288f, pagingY),
                    new Vector2(showTargets ? 28f : 48f, InventoryOptimizationHudLayout.PagingHeight));
                SetTopRect(status.rectTransform, new Vector2(showTargets ? 200f : 72f, pagingY),
                    new Vector2(showTargets ? 108f : 216f, InventoryOptimizationHudLayout.PagingHeight));
            }
            if (!showTargets)
            {
                comboEditor?.Hide();
            }

            if (markPriorities != null && optimize != null)
            {
                RectTransform markRect =
                    markPriorities.transform as RectTransform;
                RectTransform optimizeRect =
                    optimize.transform as RectTransform;
                float y = -ActionsTop;
                markRect.anchoredPosition = new Vector2(188f, -56f);
                markRect.sizeDelta = new Vector2(148f, 32f);
                optimizeRect.anchoredPosition = new Vector2(188f,
                    y);
            }
        }

        private void PositionBesideInventory()
        {
            if (root == null || attachedInventoryZone == null ||
                !(root.transform.parent is RectTransform canvasRoot))
            {
                return;
            }
            RectTransform rootRect = root.transform as RectTransform;
            attachedInventoryZone.GetWorldCorners(inventoryWorldCorners);
            Vector3 rightCenter = (inventoryWorldCorners[2] +
                inventoryWorldCorners[3]) * 0.5f;
            Vector3 bottomRight = inventoryWorldCorners[3];
            Vector3 leftCenter = (inventoryWorldCorners[0] +
                inventoryWorldCorners[1]) * 0.5f;
            Vector3 rightLocal = canvasRoot.InverseTransformPoint(rightCenter);
            Vector3 bottomRightLocal =
                canvasRoot.InverseTransformPoint(bottomRight);
            Vector3 leftLocal = canvasRoot.InverseTransformPoint(leftCenter);
            float layoutWidth = panelOpen ? PanelWidth : LauncherWidth;
            float panelHeight = panelOpen ? OpenPanelHeight : LauncherHeight;
            float leftAvailable = Mathf.Max(0f,
                leftLocal.x - canvasRoot.rect.xMin - PanelGap);
            float rightAvailable = Mathf.Max(0f,
                canvasRoot.rect.xMax - rightLocal.x - PanelGap);
            // Prefer the free right side over the native combo and skill panels.
            bool placeRight = rightAvailable >= layoutWidth ||
                rightAvailable >= leftAvailable;
            float sideAvailable = placeRight
                ? rightAvailable
                : leftAvailable;
            float heightAvailable = Mathf.Max(LauncherHeight,
                canvasRoot.rect.height - 24f);
            float canvasUnitScale = Mathf.Min(panelOpen
                    ? 1f / InventoryOptimizationHudLayout.NativeUnitScale : 1f,
                Mathf.Min(sideAvailable / layoutWidth,
                    heightAvailable / panelHeight));
            rootRect.localScale = Vector3.one * canvasUnitScale;
            float renderedWidth = layoutWidth * canvasUnitScale;
            float renderedHeight = panelHeight * canvasUnitScale;
            float x = placeRight
                ? rightLocal.x + PanelGap
                : leftLocal.x - renderedWidth - PanelGap;
            x = Mathf.Clamp(x, canvasRoot.rect.xMin,
                canvasRoot.rect.xMax - renderedWidth);
            float preferredY = panelOpen
                ? rightLocal.y
                : bottomRightLocal.y + renderedHeight * 0.5f;
            float y = Mathf.Clamp(preferredY,
                canvasRoot.rect.yMin + renderedHeight * 0.5f,
                canvasRoot.rect.yMax - renderedHeight * 0.5f);
            rootRect.anchoredPosition = new Vector2(x, y);
        }

        private Button CreateNativeLauncher(RectTransform parent,
            TextMeshProUGUI textTemplate, Action onClick,
            out TextMeshProUGUI label, out Image icon)
        {
            GameObject buttonObject = new("SmartInventoryLauncher",
                typeof(RectTransform), typeof(Image));
            buttonObject.name = "SmartInventoryLauncher";
            RectTransform rect = buttonObject.transform as RectTransform;
            rect.SetParent(parent, false);
            SetTopRect(rect, Vector2.zero,
                new Vector2(LauncherWidth, LauncherHeight));
            Button nativeButton =
                nativeTemplates.LauncherButton.GetComponent<Button>();
            Button button = NativeInventoryOptimizationControls.AddButton(
                buttonObject, nativeButton);
            Image background = buttonObject.GetComponent<Image>();
            ApplyImageStyle(background,
                nativeButton?.targetGraphic as Image, ButtonColor);
            button.targetGraphic = background;
            button.onClick.AddListener(() => FeatureFailure.Run(FeatureId.Inventory, () => onClick?.Invoke()));
            buttonObject.AddComponent<InventoryIntentPanelDropTarget>().Configure(
                ClearArtifactPickup, ChangePage, cancelOnLeft: false);

            GameObject iconObject = new("Icon", typeof(RectTransform),
                typeof(Image));
            RectTransform iconRect = iconObject.GetComponent<RectTransform>();
            iconRect.SetParent(rect, false);
            iconRect.anchorMin = new Vector2(0.18f, 0.18f);
            iconRect.anchorMax = new Vector2(0.82f, 0.82f);
            iconRect.offsetMin = iconRect.offsetMax = Vector2.zero;
            icon = iconObject.GetComponent<Image>();
            if (nativeTemplates.PreferencesIcon != null)
            {
                icon.sprite = nativeTemplates.PreferencesIcon;
            }
            icon.preserveAspect = true;
            icon.raycastTarget = false;

            label = CreateText("Label", rect, textTemplate, Vector2.zero,
                new Vector2(LauncherWidth, LauncherHeight),
                TextAlignmentOptions.Center,
                childCoordinates: true);
            label.color = PrimaryText;
            NativeInventoryOptimizationControls.SetLabel(button, label);
            return button;
        }

        private void SetSelected(Button button, bool selected)
        {
            if (button?.targetGraphic is Image image)
            {
                image.color = selected ? button.colors.selectedColor :
                    nativeTemplates.ContentButton.targetGraphic.color;
            }
        }

        private void SetVisible(bool visible)
        {
            if (root != null && root.activeSelf != visible)
            {
                if (!visible)
                {
                    navigationBridge.Clear();
                    ClearPanelSelection();
                }
                root.SetActive(visible);
            }
        }

        private void DestroyRoot()
        {
            SuspendEditing();
            navigationBridge.Clear();
            pickupView?.Dispose();
            pickupView = null;
            if (nativeDropFilter != null)
            {
                nativeDropFilter.Bind(null);
                UnityEngine.Object.Destroy(nativeDropFilter);
                nativeDropFilter = null;
            }
            if (root != null)
            {
                ClearPanelSelection();
                root.SetActive(false);
                UnityEngine.Object.Destroy(root);
            }
            root = null;
            attachedInventoryZone = null;
            attachedPanel = null;
            artifactCommands = null;
            panelBackground = null;
            title = null;
            status = null;
            editGoals = null;
            editGoalsText = null;
            clearArtifactPriorities = null;
            clearArtifactPrioritiesText = null;
            previewItemKey = null;
            launcher = null;
            launcherIcon = null;
            close = null;
            previousPage = null;
            nextPage = null;
            markPriorities = null;
            optimize = null;
            preferencesToggle = null;
            comboPreferences = null;
            undoArrangement = null;
            preferencesToggleText = null;
            comboPreferencesText = null;
            undoArrangementText = null;
            previousPageText = null;
            nextPageText = null;
            markPrioritiesText = null;
            optimizeText = null;
            closeText = null;
            priorityQueueTitle = null;
            avoidZoneTitle = null;
            boardHint = null;
            comboTargetsTitle = null;
            lastCustomSelection = null;
            lastInventorySelection = null;
            requestOptimization = null;
            replacePreferences = null;
            togglePriorityMarking = null;
            endPriorityMarking = null;
            currentSnapshot = null;
            goalEditor?.Dispose();
            goalEditor = null;
            controls = null;
            nativeTemplates = null;
            comboEditor = null;
            prioritySlots.Clear();
            avoidSlots.Clear();
        }

        private void ClearPanelSelection()
        {
            GameObject selected = EventSystem.current?.currentSelectedGameObject;
            if (selected != null && root != null &&
                selected.transform.IsChildOf(root.transform))
            {
                EventSystem.current.SetSelectedGameObject(null);
            }
        }

        private sealed class IntentSlot
        {
            internal GameObject Root;
            internal Image Background;
            internal Image ResultStatus;
            internal NativeInventoryArtifactTooltip Tooltip;
            internal Image Icon;
            internal TextMeshProUGUI Marker;
            internal int Index;
            internal bool PriorityQueue;
            internal Button Button;
            internal ArtifactOptimizationPreference Preference;
        }
    }
}
