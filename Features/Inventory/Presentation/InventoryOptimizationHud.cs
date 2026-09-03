#nullable disable
using SephiriaEnhancements.Runtime.Inventory;
using SephiriaEnhancements.Integration;
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
        Applying
    }

    internal sealed class InventoryOptimizationHud : IDisposable
    {
        private const int RowsPerPage = InventoryOptimizationHudLayout.TargetRowsPerPage;
        private const int IntentSlots = InventoryOptimizationHudLayout.IntentSlotsPerPage;
        private const float LauncherWidth = 30f;
        private const float LauncherHeight = 30f;
        private const float PanelWidth = InventoryOptimizationHudLayout.Width;
        private const float PanelGap = 10f;
        private const float PanelHeight = InventoryOptimizationHudLayout.Height;
        private const float ProjectionInterval = 0.15f;
        private static readonly Color Background =
            new(0.055f, 0.05f, 0.075f, 0.96f);
        private static readonly Color TitleColor =
            new(0.98f, 0.78f, 0.18f, 1f);
        private static readonly Color PrimaryText =
            new(0.92f, 0.94f, 0.98f, 1f);
        private static readonly Color SecondaryText =
            new(0.58f, 0.76f, 0.78f, 1f);
        private static readonly Color ButtonColor =
            new(0.16f, 0.17f, 0.24f, 0.98f);

        private readonly Vector3[] inventoryWorldCorners = new Vector3[4];
        private readonly List<TargetRow> rows = new();
        private readonly List<IntentSlot> prioritySlots = new();
        private readonly List<IntentSlot> avoidSlots = new();
        private GameObject root;
        private RectTransform attachedInventoryZone;
        private UI_CharacterStatusPanel attachedPanel;
        private Image panelBackground;
        private TextMeshProUGUI title;
        private TextMeshProUGUI summary;
        private TextMeshProUGUI status;
        private Button details;
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
        private TextMeshProUGUI detailsText;
        private TextMeshProUGUI closeText;
        private TextMeshProUGUI priorityQueueTitle;
        private TextMeshProUGUI avoidZoneTitle;
        private TextMeshProUGUI boardHint;
        private TextMeshProUGUI comboTargetsTitle;
        private GameObject levelEditor;
        private TextMeshProUGUI levelTargetName;
        private TextMeshProUGUI levelCondition;
        private Button levelMode;
        private Button constraintStrength;
        private TextMeshProUGUI constraintStrengthText;
        private Button decreaseLevel;
        private Button increaseLevel;
        private InventorySnapshot currentSnapshot;
        private InventoryIntentResultFeedback resultFeedback;
        private int page;
        private int intentPage;
        private bool panelOpen;
        private bool detailsExpanded;
        private string expandedComboCategoryId;
        private float nextAttachAt;
        private float nextProjectionAt;
        private Action requestOptimization;
        private Action<InventoryOptimizationPreferences> replacePreferences;
        private Action togglePriorityMarking;
        private Action endPriorityMarking;
        private bool priorityMarking;
        private int priorityMarkCount;
        private readonly InventoryIntentInteractionState interaction = new();
        private NativeInventoryIntentPickupView pickupView;
        private NativeInventoryIntentDropFilter nativeDropFilter;
        private GameObject pickupFocus;

        internal bool HasArtifactPickup => interaction.HasPickup;
        private InventoryOptimizationHudPhase currentPhase;
        private NativeInventoryOptimizationViewTemplates nativeTemplates;

        internal void Update(bool allowed, InventoryOptimizationHudPhase phase,
            InventorySnapshot snapshot, Action optimizeAction,
            Action<InventoryOptimizationPreferences> replaceAction,
            bool markingPriorities, int markedPriorityCount,
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
                SuspendEditing();
                panelOpen = false;
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
            priorityMarkCount = Math.Max(0, markedPriorityCount);
            interaction.SetEditable(panelOpen && phase ==
                InventoryOptimizationHudPhase.Ready);
            UpdateArtifactPickup();
            PositionBesideInventory();
            root.transform.SetAsLastSibling();
            SetVisible(true);
            HandleIntentRemoval();
            HandleLevelEditShortcut();
            if (now < nextProjectionAt)
            {
                return;
            }
            nextProjectionAt = now + ProjectionInterval;
            Project(phase, snapshot);
        }

        internal void Reset()
        {
            DestroyRoot();
            expandedComboCategoryId = null;
            page = 0;
            intentPage = 0;
            panelOpen = false;
            detailsExpanded = false;
            nextAttachAt = 0f;
            nextProjectionAt = 0f;
            requestOptimization = null;
            replacePreferences = null;
            togglePriorityMarking = null;
            endPriorityMarking = null;
            priorityMarking = false;
            priorityMarkCount = 0;
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
            detailsExpanded = false;
            intentPage = 0;
            nativeTemplates = context.ViewTemplates;
            attachedInventoryZone = inventoryZone;
            attachedPanel = context.Panel;
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

            summary = CreateText("Summary", rect, template,
                new Vector2(24f, -56f), new Vector2(312f, 40f),
                TextAlignmentOptions.MidlineLeft);
            summary.color = SecondaryText;
            summary.textWrappingMode = TextWrappingModes.Normal;
            NativeLocalizedText.SetShrinkOnlySize(summary, summary.fontSize, summary.fontSize * 0.75f);

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

            launcher = CreateNativeLauncher(rect, template, ToggleDetails,
                out TextMeshProUGUI launcherText, out launcherIcon);
            launcherText.gameObject.SetActive(false);
            details = CreateButton("Details", rect, template,
                new Vector2(188f, -InventoryOptimizationHudLayout.DetailsTop),
                new Vector2(148f, InventoryOptimizationHudLayout.DetailsHeight),
                ToggleDetails, out detailsText);
            editGoals = CreateButton("EditGoals", rect, template,
                new Vector2(24f, -InventoryOptimizationHudLayout.DetailsTop),
                new Vector2(148f, InventoryOptimizationHudLayout.DetailsHeight),
                EditPreviewedGoals, out editGoalsText);
            editGoals.interactable = false;
            close = CreateButton("Close", rect, template,
                new Vector2(308f, -20f), new Vector2(28f, 28f),
                ClosePanel, out closeText);
            closeText.text = "×";

            comboTargetsTitle = CreateText("ComboTargetsTitle", rect, template,
                new Vector2(24f, -102f), new Vector2(144f, 32f), TextAlignmentOptions.MidlineLeft);
            NativeLocalizedText.SetShrinkOnlySize(comboTargetsTitle,
                comboTargetsTitle.fontSize, comboTargetsTitle.fontSize * 0.75f);
            CreateLevelEditor(rect, template);

            for (int index = 0; index < RowsPerPage; index++)
            {
                rows.Add(CreateTargetRow(rect, template, index));
            }

            previousPage = CreateButton("PreviousPage", rect, template,
                new Vector2(24f, -InventoryOptimizationHudLayout.BoardPagingTop),
                new Vector2(48f, InventoryOptimizationHudLayout.PagingHeight),
                () => ChangePage(-1), out previousPageText);
            nextPage = CreateButton("NextPage", rect, template,
                new Vector2(288f, -InventoryOptimizationHudLayout.BoardPagingTop),
                new Vector2(48f, InventoryOptimizationHudLayout.PagingHeight),
                () => ChangePage(1), out nextPageText);
            status = CreateText("Status", rect, template,
                new Vector2(66f, -InventoryOptimizationHudLayout.BoardPagingTop),
                new Vector2(228f, InventoryOptimizationHudLayout.PagingHeight),
                TextAlignmentOptions.Center);
            status.color = SecondaryText;
            NativeLocalizedText.SetShrinkOnlySize(status, status.fontSize, status.fontSize * 0.75f);

            markPriorities = CreateButton("MarkPriorities", rect, template,
                new Vector2(24f, -InventoryOptimizationHudLayout.ActionsTop),
                new Vector2(148f, InventoryOptimizationHudLayout.ActionsHeight),
                () => togglePriorityMarking?.Invoke(),
                out markPrioritiesText);
            optimize = CreateButton("Optimize", rect, template,
                new Vector2(188f, -InventoryOptimizationHudLayout.ActionsTop),
                new Vector2(148f, InventoryOptimizationHudLayout.ActionsHeight),
                () => requestOptimization?.Invoke(), out optimizeText);

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
            return slot;
        }

        private void CreateLevelEditor(RectTransform parent, TextMeshProUGUI template)
        {
            levelEditor = new GameObject("ArtifactLevelEditor", typeof(RectTransform));
            var rect = levelEditor.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            SetTopRect(rect, new Vector2(24f, -InventoryOptimizationHudLayout.HintTop),
                new Vector2(312f, InventoryOptimizationHudLayout.HintHeight));
            levelTargetName = CreateText("ArtifactName", rect, template,
                Vector2.zero, new Vector2(164f, 28f), TextAlignmentOptions.MidlineLeft);
            levelTargetName.color = PrimaryText;
            constraintStrength = CreateButton("ConstraintStrength", rect, template,
                new Vector2(170f, 0f), new Vector2(106f, 28f), ToggleArtifactStrength, out constraintStrengthText);
            CreateButton("CloseLevelEditor", rect, template, new Vector2(284f, 0f),
                new Vector2(28f, 28f), ClearArtifactPickup, out var closeLevelText);
            closeLevelText.text = "×";
            levelMode = CreateButton("TargetMode", rect, template,
                new Vector2(0f, -36f), new Vector2(228f, 28f),
                CycleArtifactTargetMode, out levelCondition);
            decreaseLevel = CreateButton("DecreaseLevel", rect, template,
                new Vector2(236f, -36f), new Vector2(30f, 28f),
                () => AdjustArtifactLevel(-1), out var decreaseText);
            increaseLevel = CreateButton("IncreaseLevel", rect, template,
                new Vector2(282f, -36f), new Vector2(30f, 28f),
                () => AdjustArtifactLevel(1), out var increaseText);
            decreaseText.text = "−";
            increaseText.text = "+";
        }

        private void HandleLevelEditShortcut()
        {
            if (!panelOpen || detailsExpanded || !interaction.Editable || interaction.HasPickup ||
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
            if (slot == null && action.activeControl?.device is not Gamepad &&
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
            if (!panelOpen || detailsExpanded || !interaction.Editable || interaction.HasPickup ||
                NativeInventoryIntentDrop.HasHeldItem || slot?.Root.activeInHierarchy != true ||
                slot.Preference == null) return;
            // Resolve the current rule by native item identity, never by a stale page index.
            var preferences = ExplorationInventoryIntentStore.Capture();
            var rule = preferences.ArtifactPreferences.FirstOrDefault(candidate =>
                candidate.ItemKey == slot.Preference.ItemKey);
            if (rule == null || !HasInventoryArtifact(rule.InstanceId, rule.EntityId) ||
                !interaction.TryEditLevel(rule)) return;
            previewItemKey = rule.ItemKey;
            endPriorityMarking?.Invoke();
            slot.Tooltip.Hide();
            if (selectEditor && interaction.LevelTarget.HasValue)
            {
                ProjectLevelEditor(preferences);
                if (levelEditor.activeSelf)
                    EventSystem.current?.SetSelectedGameObject(levelMode.gameObject);
            }
            nextProjectionAt = 0f;
        }

        private void ProjectLevelEditor(InventoryOptimizationPreferences preferences)
        {
            var rule = preferences.ArtifactPreferences.FirstOrDefault(candidate =>
                candidate.ItemKey == interaction.LevelTarget);
            var item = currentSnapshot?.Items.FirstOrDefault(candidate => candidate.ItemKey == interaction.LevelTarget);
            bool show = interaction.Editable && !interaction.HasPickup && !NativeInventoryIntentDrop.HasHeldItem &&
                rule != null && item?.Artifact != null && HasInventoryArtifact(rule.InstanceId, rule.EntityId);
            if (!show) interaction.CancelLevelEdit();
            levelEditor.SetActive(show);
            boardHint.gameObject.SetActive(!show);
            editGoals.gameObject.SetActive(!show);
            (details.transform as RectTransform).anchoredPosition = new Vector2(show ? 24f : 188f,
                -InventoryOptimizationHudLayout.DetailsTop);
            (details.transform as RectTransform).sizeDelta = new Vector2(show ? 312f : 148f,
                InventoryOptimizationHudLayout.DetailsHeight);
            if (!show) return;
            levelTargetName.text = item.Name;
            constraintStrengthText.text = Loc._(rule.Strength == InventoryConstraintStrength.Hard
                ? InventoryOptimizationLocalization.HudHard : InventoryOptimizationLocalization.HudSoft);
            constraintStrength.interactable = interaction.Editable;
            levelCondition.text = rule.Level == InventoryPreferenceLevel.Avoid
                ? Loc._(InventoryOptimizationLocalization.HudAvoidGoal)
                : rule.TargetMode == ArtifactLevelTargetMode.Automatic
                ? Loc._(InventoryOptimizationLocalization.PreferenceChoiceKeys[0])
                : InventoryOptimizationLocalization.FormatArtifactMinimumLevel(rule.ResolveTargetLevel(item.Artifact), key => Loc._(key));
            levelMode.interactable = interaction.Editable && rule.Level == InventoryPreferenceLevel.Priority;
            bool specified = rule.Level == InventoryPreferenceLevel.Priority && rule.TargetMode == ArtifactLevelTargetMode.SpecifiedLevel;
            decreaseLevel.interactable = specified && rule.MinimumEffectiveLevel > 1;
            increaseLevel.interactable = specified && rule.MinimumEffectiveLevel < item.Artifact.MaxLevel;
        }

        private void ToggleArtifactStrength()
        {
            if (!interaction.Editable || interaction.HasPickup || NativeInventoryIntentDrop.HasHeldItem ||
                !interaction.LevelTarget.HasValue) return;
            var preferences = ExplorationInventoryIntentStore.Capture();
            var key = interaction.LevelTarget.Value;
            if (!HasInventoryArtifact(key.NativeInstanceId, key.EntityId)) return;
            var rule = preferences.ArtifactPreferences.FirstOrDefault(candidate => candidate.ItemKey == key);
            if (rule == null) return;
            ReplacePreferences(InventoryArtifactIntentEditor.SetStrength(preferences, key,
                rule.Strength == InventoryConstraintStrength.Hard ? InventoryConstraintStrength.Soft : InventoryConstraintStrength.Hard));
            nextProjectionAt = 0f;
        }

        private void CycleArtifactTargetMode()
        {
            if (!interaction.Editable || interaction.HasPickup || NativeInventoryIntentDrop.HasHeldItem ||
                !interaction.LevelTarget.HasValue) return;
            var key = interaction.LevelTarget.Value;
            if (!HasInventoryArtifact(key.NativeInstanceId, key.EntityId)) return;
            var preferences = ExplorationInventoryIntentStore.Capture();
            var rule = preferences.ArtifactPreferences.FirstOrDefault(candidate => candidate.ItemKey == key);
            var item = currentSnapshot?.Items.FirstOrDefault(candidate => candidate.ItemKey == key);
            if (rule == null || item?.Artifact == null) return;
            ReplacePreferences(rule.TargetMode == ArtifactLevelTargetMode.Automatic
                ? InventoryArtifactIntentEditor.SetMinimumEffectiveLevel(preferences, currentSnapshot, key, 0)
                : rule.TargetMode == ArtifactLevelTargetMode.ActiveOnly && item.Artifact.MaxLevel > 0
                    ? InventoryArtifactIntentEditor.SetMinimumEffectiveLevel(preferences, currentSnapshot, key,
                        Math.Max(1, item.Artifact.LimitedEffectEnabledLevel))
                    : InventoryArtifactIntentEditor.SetAutomatic(preferences, key));
            nextProjectionAt = 0f;
        }

        private void AdjustArtifactLevel(int delta)
        {
            if (!interaction.Editable || interaction.HasPickup || NativeInventoryIntentDrop.HasHeldItem ||
                !interaction.LevelTarget.HasValue) return;
            var key = interaction.LevelTarget.Value;
            if (!HasInventoryArtifact(key.NativeInstanceId, key.EntityId)) return;
            var preferences = ExplorationInventoryIntentStore.Capture();
            var rule = preferences.ArtifactPreferences.FirstOrDefault(candidate => candidate.ItemKey == key);
            if (rule == null) return;
            ReplacePreferences(InventoryArtifactIntentEditor.SetMinimumEffectiveLevel(
                preferences, currentSnapshot, key, rule.MinimumEffectiveLevel + delta));
            nextProjectionAt = 0f;
        }

        private TargetRow CreateTargetRow(RectTransform parent,
            TextMeshProUGUI template, int index)
        {
            var row = new TargetRow
            {
                Root = new GameObject("TargetRow" + index,
                    typeof(RectTransform))
            };
            RectTransform rect = row.Root.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            SetTopRect(rect, new Vector2(8f, -InventoryOptimizationHudLayout.TargetRowsTop),
                new Vector2(344f, InventoryOptimizationHudLayout.TargetRowHeight(false)));
            row.Select = CreateButton("Name", rect, template,
                new Vector2(16f, 0f), new Vector2(190f, 26f),
                () => ToggleComboEditor(row), out row.Name);
            row.Name.alignment = TextAlignmentOptions.MidlineLeft;
            row.Name.color = PrimaryText;
            ColorBlock nameColors = row.Select.colors;
            nameColors.normalColor = nameColors.disabledColor = Color.clear;
            nameColors.highlightedColor = nameColors.selectedColor = new Color(1f, 1f, 1f, 0.12f);
            nameColors.pressedColor = new Color(1f, 1f, 1f, 0.2f);
            row.Select.colors = nameColors;
            row.Choice = CreateButton("Choice", rect, template,
                new Vector2(208f, 0f), new Vector2(120f, 26f),
                () => CycleChoice(row), out row.ChoiceText);
            row.Decrease = CreateButton("Decrease", rect, template,
                new Vector2(260f, -28f), new Vector2(30f, 24f),
                () => AdjustRequiredValue(row, -1), out row.DecreaseText);
            row.Value = CreateText("Value", rect, template,
                new Vector2(128f, -28f), new Vector2(124f, 24f),
                TextAlignmentOptions.MidlineLeft);
            row.Strength = CreateButton("ConstraintStrength", rect, template,
                new Vector2(16f, -28f), new Vector2(106f, 24f), () => ToggleComboStrength(row), out row.StrengthText);
            row.Value.color = SecondaryText;
            row.Value.fontSize *= 0.8f;
            row.Increase = CreateButton("Increase", rect, template,
                new Vector2(298f, -28f), new Vector2(30f, 24f),
                () => AdjustRequiredValue(row, 1), out row.IncreaseText);
            row.DecreaseText.text = "−";
            row.IncreaseText.text = "+";
            return row;
        }

        private void Project(InventoryOptimizationHudPhase phase,
            InventorySnapshot snapshot)
        {
            title.text = Loc._(InventoryOptimizationLocalization.HudTitle);
            InventoryOptimizationPreferences preferences =
                ExplorationInventoryIntentStore.Capture();
            if (panelOpen && !detailsExpanded)
            {
                ProjectIntentBoard(preferences);
            }
            int adjustmentCount = Math.Max(0,
                preferences.ArtifactPreferences.Count +
                preferences.ComboPreferences.Count - priorityMarkCount);
            summary.text = phase switch
            {
                InventoryOptimizationHudPhase.Searching =>
                    Loc._(InventoryOptimizationLocalization.HudSearching),
                InventoryOptimizationHudPhase.Applying =>
                    Loc._(InventoryOptimizationLocalization.HudApplying),
                _ when priorityMarking => string.Format(Loc._(
                    InventoryOptimizationLocalization.HudMarkingHint),
                    priorityMarkCount),
                _ when detailsExpanded => Loc._(InventoryOptimizationLocalization.HudComboPersistence),
                _ when interaction.LevelTarget.HasValue => Loc._(InventoryOptimizationLocalization.HudConstraintHelp),
                _ when priorityMarkCount > 0 && adjustmentCount > 0 =>
                    string.Format(Loc._(InventoryOptimizationLocalization.
                        HudMarkedAndAdjustmentCount), priorityMarkCount,
                        adjustmentCount),
                _ when priorityMarkCount > 0 => string.Format(Loc._(
                    InventoryOptimizationLocalization.HudMarkedCount),
                    priorityMarkCount),
                _ when adjustmentCount > 0 => string.Format(Loc._(
                    InventoryOptimizationLocalization.HudAdjustmentCount),
                    adjustmentCount),
                _ when snapshot?.BuildIntent?.NativePresetEnabled == true =>
                    Loc._(InventoryOptimizationLocalization.
                        HudAutomaticPreset),
                _ => Loc._(InventoryOptimizationLocalization.
                    HudAutomaticInventory)
            };
            detailsText.text = Loc._(!panelOpen
                ? InventoryOptimizationLocalization.HudOpen
                : detailsExpanded
                    ? InventoryOptimizationLocalization.HudHideTargets
                    : InventoryOptimizationLocalization.HudAdjustTargets);
            editGoalsText.text = Loc._(InventoryOptimizationLocalization.HudEditGoals);
            if (!panelOpen)
            {
                return;
            }
            optimizeText.text = Loc._(
                InventoryOptimizationLocalization.HudOptimize);
            bool editable = phase == InventoryOptimizationHudPhase.Ready;
            markPrioritiesText.text = Loc._(priorityMarking
                ? InventoryOptimizationLocalization.HudFinishMarking
                : InventoryOptimizationLocalization.HudMarkArtifacts);
            markPriorities.interactable = editable && !interaction.HasPickup &&
                !NativeInventoryIntentDrop.HasHeldItem;
            SetSelected(markPriorities, priorityMarking);
            optimize.interactable = editable && !interaction.HasPickup && snapshot?.Items.Count > 0 &&
                !NativeInventoryIntentDrop.HasHeldItem;
            previousPageText.text = "‹";
            nextPageText.text = "›";
            if (!detailsExpanded)
            {
                return;
            }

            comboTargetsTitle.text = Loc._(InventoryOptimizationLocalization.HudComboTargets);
            IReadOnlyList<InventoryComboTarget> targets =
                InventoryComboTargetEditor.BuildTargets(snapshot, preferences);
            int pageCount = Math.Max(1,
                (targets.Count + RowsPerPage - 1) / RowsPerPage);
            page = Mathf.Clamp(page, 0, pageCount - 1);
            float rowTop = InventoryOptimizationHudLayout.TargetRowsTop;
            for (int rowIndex = 0; rowIndex < rows.Count; rowIndex++)
            {
                int targetIndex = page * RowsPerPage + rowIndex;
                TargetRow row = rows[rowIndex];
                if (targetIndex >= targets.Count)
                {
                    row.Target = null;
                    row.Root.SetActive(false);
                    continue;
                }

                InventoryComboTarget target = targets[targetIndex];
                row.Target = target;
                row.Root.SetActive(true);
                bool expanded = target.CanAdjustRequiredValue && target.CategoryId == expandedComboCategoryId;
                float rowHeight = InventoryOptimizationHudLayout.TargetRowHeight(expanded);
                SetTopRect((RectTransform)row.Root.transform, new Vector2(8f, -rowTop),
                    new Vector2(344f, rowHeight));
                rowTop += rowHeight + InventoryOptimizationHudLayout.TargetRowGap;
                row.Name.text = DisplayName(target);
                row.Name.color = expanded ? TitleColor : PrimaryText;
                row.Select.interactable = editable && target.CanAdjustRequiredValue;
                string condition = InventoryOptimizationLocalization.FormatTargetCondition(target, key => Loc._(key));
                row.ChoiceText.text = !expanded && target.CanAdjustRequiredValue ? condition : Loc._(
                    InventoryOptimizationLocalization.PreferenceChoiceKeys[
                        (int)target.Choice]);
                row.Value.text = condition;
                row.Value.gameObject.SetActive(expanded);
                row.Value.color = SatisfactionColor(resultFeedback?.FindCombo(target.CategoryId) ?? InventoryIntentSatisfaction.NotEvaluated);
                row.ChoiceText.color = target.CanAdjustRequiredValue ? row.Value.color : PrimaryText;
                row.Strength.gameObject.SetActive(expanded);
                row.Strength.interactable = editable;
                row.StrengthText.text = Loc._(target.Strength == InventoryConstraintStrength.Hard
                    ? InventoryOptimizationLocalization.HudHard : InventoryOptimizationLocalization.HudSoft);
                row.Decrease.gameObject.SetActive(expanded);
                row.Increase.gameObject.SetActive(expanded);
                row.Choice.interactable = editable;
                row.Decrease.interactable = editable &&
                    target.CanAdjustRequiredValue && target.RequiredValue > 0;
                row.Increase.interactable = editable &&
                    target.CanAdjustRequiredValue &&
                    target.RequiredValue < target.MaximumValue;
            }

            previousPage.interactable = editable && page > 0;
            nextPage.interactable = editable && page + 1 < pageCount;
            status.text = phase switch
            {
                InventoryOptimizationHudPhase.Searching =>
                    Loc._(InventoryOptimizationLocalization.HudSearching),
                InventoryOptimizationHudPhase.Applying =>
                    Loc._(InventoryOptimizationLocalization.HudApplying),
                _ when targets.Count == 0 =>
                    Loc._(InventoryOptimizationLocalization.HudNoTargets),
                _ => string.Format(Loc._(
                    InventoryOptimizationLocalization.HudPage), page + 1,
                    pageCount)
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
            boardHint.text = UsingGamepad
                ? string.Format(Loc._(interaction.HasPickup
                    ? InventoryOptimizationLocalization.HudControllerChooseIntentSlot
                    : InventoryOptimizationLocalization.HudControllerBoardHint), removeBinding)
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
            editGoals.interactable = false;
            if (interaction.HasPickup || NativeInventoryIntentDrop.HasHeldItem || levelEditor.activeSelf) return;
            var slots = prioritySlots.Concat(avoidSlots);
            IntentSlot hovered = null;
            if (!UsingGamepad && InputDeviceState.TryGetPointerPosition(out var pointer))
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
                return;
            }
            editGoals.interactable = interaction.Editable && HasInventoryArtifact(rule.InstanceId, rule.EntityId);
            boardHint.text = item.Name + "\n" + InventoryOptimizationLocalization.FormatArtifactFeedback(rule, item.Artifact,
                resultFeedback?.Find(rule.ItemKey), key => Loc._(key));
        }

        private static bool UsingGamepad => PlayerInputController.Instance?.playerInput != null &&
            PlayerInputController.Instance.playerInput.currentControlScheme != PlayerInputController.KeyboardAndMouseScheme;

        private void ActivateIntentSlot(IntentSlot slot)
        {
            if (slot == null || !interaction.Editable)
            {
                return;
            }
            UI_NewInventoryIcon held = NativeInventoryIntentDrop.ConfirmedPickup;
            if (held != null)
            {
                if (held.Item?.Charm != null &&
                    held.Inventory == attachedPanel?.PlayerAvatar?.Inventory)
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
            if (NativeInventoryIntentDrop.HasHeldItem || slot?.Icon.sprite == null ||
                slot.Preference == null ||
                !HasInventoryArtifact(slot.Preference.InstanceId, slot.Preference.EntityId) ||
                !interaction.TryPickup(slot.Preference, dragging))
            {
                return;
            }
            pickupFocus = slot.Root;
            endPriorityMarking?.Invoke();
            foreach (IntentSlot candidate in prioritySlots.Concat(avoidSlots))
            {
                candidate.Tooltip.Hide();
            }
            pickupView.Show(slot.Icon.sprite);
            RefreshPickupControls();
        }

        private void PlaceHeldArtifact(IntentSlot slot)
        {
            if (slot == null || !interaction.HasPickup)
            {
                return;
            }
            ArtifactOptimizationPreference held = interaction.Pickup;
            if (interaction.TryPlace(ExplorationInventoryIntentStore.Capture(),
                slot.PriorityQueue ? InventoryPreferenceLevel.Priority : InventoryPreferenceLevel.Avoid,
                slot.Index, HasInventoryArtifact(held.InstanceId, held.EntityId), out var updated))
            {
                ReplacePreferences(updated);
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
            ArtifactOptimizationPreference held = interaction.Pickup;
            if (NativeInventoryIntentDrop.HasHeldItem ||
                !interaction.ValidatePickup(ExplorationInventoryIntentStore.Capture(),
                    HasInventoryArtifact(held.InstanceId, held.EntityId)))
            {
                ClearArtifactPickup();
                return;
            }
            KeepPickupFocusInPanel();
            pickupView?.UpdatePosition();
        }

        private void KeepPickupFocusInPanel()
        {
            GameObject selected = EventSystem.current?.currentSelectedGameObject;
            if (selected != null && selected.transform.IsChildOf(root.transform))
            {
                pickupFocus = selected;
            }
            else if (selected != null && pickupFocus != null && pickupFocus.activeInHierarchy)
            {
                EventSystem.current.SetSelectedGameObject(pickupFocus);
            }
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
            foreach (IntentSlot slot in prioritySlots.Concat(avoidSlots))
            {
                slot.Icon.enabled = slot.Icon.sprite != null &&
                    interaction.ItemKey != slot.Preference?.ItemKey;
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
                ClearArtifactPickup();
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

        private void ClearArtifactPickup()
        {
            interaction.CancelPickup();
            interaction.CancelLevelEdit();
            pickupView?.Hide();
            pickupFocus = null;
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
            slot.Icon.enabled = slot.Icon.sprite != null &&
                interaction.ItemKey != preference?.ItemKey;
            slot.Icon.material = null;
            slot.Icon.color = Color.white;
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

        private static Color SatisfactionColor(InventoryIntentSatisfaction state) => state switch
        {
            InventoryIntentSatisfaction.Satisfied => new Color(0.24f, 0.88f, 0.42f, 1f),
            InventoryIntentSatisfaction.Partial => new Color(1f, 0.76f, 0.15f, 1f),
            InventoryIntentSatisfaction.Unmet => new Color(0.98f, 0.25f, 0.22f, 1f),
            _ => new Color(0.47f, 0.49f, 0.54f, 1f)
        };

        private void DropIntoIntentSlot(IntentSlot slot,
            UI_NewInventoryIcon icon)
        {
            NewItemOwnInstance item = icon?.Item;
            if (item?.Charm == null ||
                icon.Inventory != attachedPanel?.PlayerAvatar?.Inventory)
            {
                return;
            }
            PlaceInIntentSlot(slot, item.InstanceID, item.EntityID);
        }

        private void PlaceInIntentSlot(IntentSlot slot, int instanceId,
            int entityId)
        {
            if (!interaction.Editable || slot == null ||
                !HasInventoryArtifact(instanceId, entityId))
            {
                ClearArtifactPickup();
                return;
            }
            InventoryOptimizationPreferences current =
                ExplorationInventoryIntentStore.Capture();
            InventoryOptimizationPreferences updated = slot.PriorityQueue
                ? InventoryArtifactIntentEditor.PlacePriority(current,
                    instanceId, entityId, slot.Index)
                : InventoryArtifactIntentEditor.PlaceAvoid(current,
                    instanceId, entityId, slot.Index);
            ReplacePreferences(updated);
            ClearArtifactPickup();
            nextProjectionAt = 0f;
        }

        private bool HasInventoryArtifact(int instanceId, int entityId)
        {
            GridInventory inventory = attachedPanel?.PlayerAvatar?.Inventory;
            if (inventory == null)
            {
                return false;
            }
            for (int index = 0; index < inventory.CurrentInventoryStorage; index++)
            {
                NewItemOwnInstance item = inventory.FindItem(inventory.IdxToPos(index));
                if (item?.InstanceID == instanceId && item.EntityID == entityId &&
                    item.Charm != null)
                {
                    return true;
                }
            }
            return false;
        }

        private void RemoveIntentSlot(IntentSlot slot)
        {
            if (interaction.HasPickup)
            {
                ClearArtifactPickup();
                return;
            }
            if (!interaction.Editable || NativeInventoryIntentDrop.HasHeldItem || slot?.Preference == null)
            {
                return;
            }
            ReplacePreferences(InventoryArtifactIntentEditor.Remove(
                ExplorationInventoryIntentStore.Capture(),
                slot.Preference.ItemKey));
            ClearArtifactPickup();
            nextProjectionAt = 0f;
        }

        private static string DisplayName(InventoryComboTarget target)
        {
            // Native integration boundary: categoryName is the game's
            // localized ItemCategoryEntity label, while CategoryId remains
            // the stable optimizer identifier.
            try
            {
                ItemCategoryEntity category =
                    ItemDatabase.FindItemCategory(target.CategoryId);
                string name = category?.categoryName?.ToString();
                return string.IsNullOrEmpty(name) ? target.CategoryId : name;
            }
            catch
            {
                return target.CategoryId;
            }
        }

        private void ToggleComboEditor(TargetRow row)
        {
            if (!interaction.Editable || row?.Target?.CanAdjustRequiredValue != true) return;
            expandedComboCategoryId = expandedComboCategoryId == row.Target.CategoryId ? null : row.Target.CategoryId;
            nextProjectionAt = 0f;
        }

        private void CycleChoice(TargetRow row)
        {
            if (!interaction.Editable || row?.Target == null)
            {
                return;
            }
            InventoryPreferenceChoice nextChoice = InventoryComboTargetEditor.NextChoice(row.Target.Choice);
            InventoryOptimizationPreferences updated =
                InventoryComboTargetEditor.SetChoice(
                    ExplorationInventoryIntentStore.Capture(), row.Target,
                    nextChoice);
            ReplacePreferences(updated);
            expandedComboCategoryId = nextChoice == InventoryPreferenceChoice.Automatic ? null : row.Target.CategoryId;
            nextProjectionAt = 0f;
        }

        private void AdjustRequiredValue(TargetRow row, int delta)
        {
            if (!interaction.Editable || row?.Target?.CanAdjustRequiredValue != true)
            {
                return;
            }
            InventoryOptimizationPreferences updated =
                InventoryComboTargetEditor.SetRequiredValue(
                    ExplorationInventoryIntentStore.Capture(), row.Target,
                    row.Target.RequiredValue + delta);
            ReplacePreferences(updated);
            nextProjectionAt = 0f;
        }

        private void ToggleComboStrength(TargetRow row)
        {
            if (!interaction.Editable || row?.Target?.CanAdjustRequiredValue != true) return;
            ReplacePreferences(InventoryComboTargetEditor.SetStrength(ExplorationInventoryIntentStore.Capture(), row.Target,
                row.Target.Strength == InventoryConstraintStrength.Hard ? InventoryConstraintStrength.Soft : InventoryConstraintStrength.Hard));
            nextProjectionAt = 0f;
        }

        private void ReplacePreferences(
            InventoryOptimizationPreferences preferences)
        {
            if (replacePreferences != null)
            {
                replacePreferences(preferences);
            }
            else
            {
                ExplorationInventoryIntentStore.Replace(preferences);
            }
        }

        private void ChangePage(int delta)
        {
            if (!interaction.Editable)
            {
                return;
            }
            interaction.CancelLevelEdit();
            previewItemKey = null;
            if (detailsExpanded)
            {
                page = Math.Max(0, page + delta);
                expandedComboCategoryId = null;
            }
            else
            {
                intentPage = Math.Max(0, intentPage + delta);
                ProjectIntentBoard(ExplorationInventoryIntentStore.Capture());
            }
            nextProjectionAt = 0f;
        }

        private void ToggleDetails()
        {
            expandedComboCategoryId = null;
            previewItemKey = null;
            if (!panelOpen)
            {
                panelOpen = true;
                detailsExpanded = false;
            }
            else
            {
                detailsExpanded = !detailsExpanded;
            }
            ClearArtifactPickup();
            endPriorityMarking?.Invoke();
            interaction.SetEditable(currentPhase == InventoryOptimizationHudPhase.Ready);
            page = 0;
            ApplyDisclosureLayout();
            PositionBesideInventory();
            nextProjectionAt = 0f;
        }

        private void ClosePanel()
        {
            endPriorityMarking?.Invoke();
            SuspendEditing();
            ClearPanelSelection();
            panelOpen = false;
            detailsExpanded = false;
            page = 0;
            ApplyDisclosureLayout();
            PositionBesideInventory();
            nextProjectionAt = 0f;
        }

        private void ApplyDisclosureLayout()
        {
            if (root == null)
            {
                return;
            }

            float width = panelOpen ? PanelWidth : LauncherWidth;
            float height = panelOpen ? PanelHeight : LauncherHeight;
            (root.transform as RectTransform).sizeDelta =
                new Vector2(width, height);
            if (panelBackground != null)
            {
                panelBackground.enabled = panelOpen;
                panelBackground.raycastTarget = panelOpen;
            }
            title?.gameObject.SetActive(panelOpen);
            launcher?.gameObject.SetActive(!panelOpen);
            details?.gameObject.SetActive(panelOpen);
            if (details != null)
            {
                (details.transform as RectTransform).anchoredPosition = new Vector2(detailsExpanded ? 24f : 188f,
                    -InventoryOptimizationHudLayout.DetailsTop);
                (details.transform as RectTransform).sizeDelta = new Vector2(detailsExpanded ? 312f : 148f,
                    InventoryOptimizationHudLayout.DetailsHeight);
            }
            summary?.gameObject.SetActive(panelOpen);
            bool showBoard = panelOpen && !detailsExpanded;
            editGoals?.gameObject.SetActive(showBoard);
            priorityQueueTitle?.gameObject.SetActive(showBoard);
            avoidZoneTitle?.gameObject.SetActive(showBoard);
            boardHint?.gameObject.SetActive(showBoard);
            levelEditor?.SetActive(false);
            foreach (IntentSlot slot in prioritySlots)
            {
                slot.Root.SetActive(showBoard);
            }
            foreach (IntentSlot slot in avoidSlots)
            {
                slot.Root.SetActive(showBoard);
            }
            close?.gameObject.SetActive(panelOpen);
            markPriorities?.gameObject.SetActive(panelOpen);
            optimize?.gameObject.SetActive(panelOpen);
            bool showTargets = panelOpen && detailsExpanded;
            comboTargetsTitle?.gameObject.SetActive(showTargets);
            previousPage?.gameObject.SetActive(panelOpen);
            nextPage?.gameObject.SetActive(panelOpen);
            status?.gameObject.SetActive(panelOpen);
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
                foreach (TargetRow row in rows)
                {
                    row.Target = null;
                    row.Root.SetActive(false);
                }
            }

            if (markPriorities != null && optimize != null)
            {
                RectTransform markRect =
                    markPriorities.transform as RectTransform;
                RectTransform optimizeRect =
                    optimize.transform as RectTransform;
                float y = -InventoryOptimizationHudLayout.ActionsTop;
                markRect.anchoredPosition = new Vector2(24f, y);
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
            float panelHeight = panelOpen ? PanelHeight : LauncherHeight;
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
            button.onClick.AddListener(() => onClick?.Invoke());
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

        private Button CreateButton(string name, RectTransform parent,
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
            button.onClick.AddListener(() => onClick?.Invoke());
            buttonObject.AddComponent<InventoryIntentPanelDropTarget>().Configure(
                ClearArtifactPickup, ChangePage, cancelOnLeft: false);
            label = CreateText("Label", rect, template, Vector2.zero, size,
                TextAlignmentOptions.Center,
                childCoordinates: true);
            label.color = PrimaryText;
            NativeLocalizedText.SetShrinkOnlySize(label, label.fontSize, label.fontSize * 0.75f);
            NativeInventoryOptimizationControls.SetLabel(button, label);
            return button;
        }

        private static void ApplyImageStyle(Image destination,
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

        private static TextMeshProUGUI CreateText(string name,
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

        private static void SetTopRect(RectTransform rect, Vector2 position,
            Vector2 size)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
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
                    ClearPanelSelection();
                }
                root.SetActive(visible);
            }
        }

        private void DestroyRoot()
        {
            SuspendEditing();
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
            panelBackground = null;
            title = null;
            summary = null;
            status = null;
            details = null;
            editGoals = null;
            editGoalsText = null;
            previewItemKey = null;
            launcher = null;
            launcherIcon = null;
            close = null;
            previousPage = null;
            nextPage = null;
            markPriorities = null;
            optimize = null;
            previousPageText = null;
            nextPageText = null;
            markPrioritiesText = null;
            optimizeText = null;
            detailsText = null;
            closeText = null;
            priorityQueueTitle = null;
            avoidZoneTitle = null;
            boardHint = null;
            comboTargetsTitle = null;
            levelEditor = null;
            levelTargetName = null;
            levelCondition = null;
            levelMode = null;
            constraintStrength = null;
            constraintStrengthText = null;
            decreaseLevel = null;
            increaseLevel = null;
            currentSnapshot = null;
            nativeTemplates = null;
            rows.Clear();
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

        private sealed class TargetRow
        {
            internal GameObject Root;
            internal Button Select;
            internal TextMeshProUGUI Name;
            internal Button Choice;
            internal Button Strength;
            internal TextMeshProUGUI StrengthText;
            internal TextMeshProUGUI ChoiceText;
            internal Button Decrease;
            internal TextMeshProUGUI DecreaseText;
            internal TextMeshProUGUI Value;
            internal Button Increase;
            internal TextMeshProUGUI IncreaseText;
            internal InventoryComboTarget Target;
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
