#nullable disable
using SephiriaEnhancements.Runtime.Inventory;
using SephiriaEnhancements.KeyboardUiNavigation;

using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
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
        private const int RowsPerPage = 5;
        private const int IntentSlots = 6;
        private const float LauncherWidth = 30f;
        private const float LauncherHeight = 30f;
        private const float PanelWidth = 360f;
        private const float PanelGap = 10f;
        private const float CollapsedPanelHeight = 326f;
        private const float ExpandedPanelHeight = 630f;
        private const float ProjectionInterval = 0.15f;
        private static readonly Color Background =
            new(0.055f, 0.05f, 0.075f, 0.96f);
        private static readonly Color Border =
            new(0.45f, 0.5f, 0.62f, 0.9f);
        private static readonly Color TitleColor =
            new(0.98f, 0.78f, 0.18f, 1f);
        private static readonly Color PrimaryText =
            new(0.92f, 0.94f, 0.98f, 1f);
        private static readonly Color SecondaryText =
            new(0.58f, 0.76f, 0.78f, 1f);
        private static readonly Color ButtonColor =
            new(0.16f, 0.17f, 0.24f, 0.98f);
        private static readonly Color SelectedButtonColor =
            new(0.25f, 0.3f, 0.4f, 1f);

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
        private Image launcherIcon;
        private Button close;
        private Button artifactTab;
        private Button comboTab;
        private Button previousPage;
        private Button nextPage;
        private Button markPriorities;
        private Button optimize;
        private TextMeshProUGUI artifactTabText;
        private TextMeshProUGUI comboTabText;
        private TextMeshProUGUI previousPageText;
        private TextMeshProUGUI nextPageText;
        private TextMeshProUGUI markPrioritiesText;
        private TextMeshProUGUI optimizeText;
        private TextMeshProUGUI detailsText;
        private TextMeshProUGUI closeText;
        private TextMeshProUGUI priorityQueueTitle;
        private TextMeshProUGUI avoidZoneTitle;
        private TextMeshProUGUI boardHint;
        private InventoryOptimizationTargetKind activeKind =
            InventoryOptimizationTargetKind.Artifact;
        private int page;
        private bool panelOpen;
        private bool detailsExpanded;
        private float nextAttachAt;
        private float nextProjectionAt;
        private Action requestOptimization;
        private Action<InventoryOptimizationPreferences> replacePreferences;
        private Action togglePriorityMarking;
        private Action endPriorityMarking;
        private bool priorityMarking;
        private int priorityMarkCount;
        private int pendingArtifactInstanceId = -1;
        private int pendingArtifactEntityId = -1;
        private NativeInventoryOptimizationViewTemplates nativeTemplates;

        internal void Update(bool allowed, InventoryOptimizationHudPhase phase,
            InventorySnapshot snapshot, Action optimizeAction,
            Action<InventoryOptimizationPreferences> replaceAction,
            bool markingPriorities, int markedPriorityCount,
            Action toggleMarkingAction, Action endMarkingAction)
        {
            StandardInventoryViewContext viewContext = null;
            bool visible = allowed && StandardInventoryContext.TryGetOpenView(
                out viewContext);
            if (!visible)
            {
                panelOpen = false;
                detailsExpanded = false;
                ApplyDisclosureLayout();
                SetVisible(false);
                return;
            }

            float now = Time.unscaledTime;
            if ((root == null || attachedInventoryZone !=
                    viewContext.InventoryZone) &&
                now >= nextAttachAt)
            {
                nextAttachAt = now + 1f;
                Attach(viewContext);
            }
            if (root == null)
            {
                return;
            }

            requestOptimization = optimizeAction;
            replacePreferences = replaceAction;
            togglePriorityMarking = toggleMarkingAction;
            endPriorityMarking = endMarkingAction;
            priorityMarking = markingPriorities;
            priorityMarkCount = Math.Max(0, markedPriorityCount);
            PositionBesideInventory();
            root.transform.SetAsLastSibling();
            SetVisible(true);
            HandleKeyboardIntentRemoval();
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
            page = 0;
            panelOpen = false;
            detailsExpanded = false;
            activeKind = InventoryOptimizationTargetKind.Artifact;
            nextAttachAt = 0f;
            nextProjectionAt = 0f;
            requestOptimization = null;
            replacePreferences = null;
            togglePriorityMarking = null;
            endPriorityMarking = null;
            priorityMarking = false;
            priorityMarkCount = 0;
            ClearPendingArtifact();
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
            nativeTemplates = context.ViewTemplates;
            attachedInventoryZone = inventoryZone;
            attachedPanel = context.Panel;
            root = new GameObject(
                "Sephiria Enhancements — Smart Inventory",
                typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
            RectTransform rect = root.GetComponent<RectTransform>();
            rect.SetParent(canvasRoot, false);
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0f, 0.5f);
            rect.sizeDelta = new Vector2(PanelWidth, CollapsedPanelHeight);

            CanvasGroup group = root.GetComponent<CanvasGroup>();
            group.interactable = true;
            group.blocksRaycasts = true;
            panelBackground = root.GetComponent<Image>();
            ApplyImageStyle(panelBackground,
                nativeTemplates.WindowBackground,
                Background);
            panelBackground.raycastTarget = true;

            title = CreateText("Title", rect, template,
                new Vector2(12f, -8f), new Vector2(336f, 30f),
                TextAlignmentOptions.MidlineLeft, 0.68f, FontStyles.Bold);
            title.color = TitleColor;

            summary = CreateText("Summary", rect, template,
                new Vector2(12f, -42f), new Vector2(336f, 28f),
                TextAlignmentOptions.MidlineLeft, 0.48f, FontStyles.Normal);
            summary.color = SecondaryText;

            priorityQueueTitle = CreateText("PriorityQueueTitle", rect,
                template, new Vector2(12f, -76f), new Vector2(336f, 22f),
                TextAlignmentOptions.MidlineLeft, 0.45f, FontStyles.Bold);
            priorityQueueTitle.color = PrimaryText;
            avoidZoneTitle = CreateText("AvoidZoneTitle", rect, template,
                new Vector2(12f, -158f), new Vector2(336f, 22f),
                TextAlignmentOptions.MidlineLeft, 0.45f, FontStyles.Bold);
            avoidZoneTitle.color = PrimaryText;
            for (int index = 0; index < IntentSlots; index++)
            {
                prioritySlots.Add(CreateIntentSlot(rect, template, index,
                    new Vector2(12f + index * 56f, -100f),
                    placeInPriorityQueue: true));
                avoidSlots.Add(CreateIntentSlot(rect, template, index,
                    new Vector2(12f + index * 56f, -182f),
                    placeInPriorityQueue: false));
            }
            ConfigureIntentSlotNavigation();
            boardHint = CreateText("BoardHint", rect, template,
                new Vector2(12f, -238f), new Vector2(336f, 24f),
                TextAlignmentOptions.MidlineLeft, 0.38f, FontStyles.Normal);
            boardHint.color = SecondaryText;

            details = CreateNativeLauncher(rect, template, ToggleDetails,
                out detailsText, out launcherIcon);
            close = CreateButton("Close", rect, template,
                new Vector2(320f, -8f), new Vector2(28f, 28f),
                ClosePanel, out closeText);
            closeText.text = "×";

            artifactTab = CreateButton("ArtifactTab", rect, template,
                new Vector2(12f, -306f), new Vector2(163f, 30f),
                () => SelectKind(InventoryOptimizationTargetKind.Artifact),
                out artifactTabText);
            comboTab = CreateButton("ComboTab", rect, template,
                new Vector2(185f, -306f), new Vector2(163f, 30f),
                () => SelectKind(
                    InventoryOptimizationTargetKind.ComboCategory),
                out comboTabText);

            for (int index = 0; index < RowsPerPage; index++)
            {
                rows.Add(CreateTargetRow(rect, template, index));
            }

            previousPage = CreateButton("PreviousPage", rect, template,
                new Vector2(12f, -556f), new Vector2(48f, 28f),
                () => ChangePage(-1), out previousPageText);
            nextPage = CreateButton("NextPage", rect, template,
                new Vector2(300f, -556f), new Vector2(48f, 28f),
                () => ChangePage(1), out nextPageText);
            status = CreateText("Status", rect, template,
                new Vector2(66f, -556f), new Vector2(228f, 28f),
                TextAlignmentOptions.Center, 0.48f, FontStyles.Normal);
            status.color = SecondaryText;

            markPriorities = CreateButton("MarkPriorities", rect, template,
                new Vector2(12f, -116f), new Vector2(160f, 36f),
                () => togglePriorityMarking?.Invoke(),
                out markPrioritiesText);
            optimize = CreateButton("Optimize", rect, template,
                new Vector2(180f, -116f), new Vector2(168f, 36f),
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
                typeof(Button), typeof(InventoryIntentDropTarget));
            RectTransform rect = slotObject.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            SetTopRect(rect, position, new Vector2(48f, 48f));
            Image background = slotObject.GetComponent<Image>();
            UI_NewInventoryIcon nativeIcon = nativeTemplates.InventoryIcon;
            ApplyImageStyle(background, nativeIcon?.bgImage,
                new Color(0.18f, 0.19f, 0.24f, 0.98f));
            // Inventory background materials depend on the native hierarchy's
            // stencil state. Reusing them here can render an opaque white tile.
            background.material = null;
            background.color = new Color(0.16f, 0.10f, 0.15f, 1f);
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

            TextMeshProUGUI marker = CreateText("Marker", rect, template,
                Vector2.zero, new Vector2(48f, 48f),
                TextAlignmentOptions.TopLeft, 0.42f, FontStyles.Bold,
                childCoordinates: true);
            marker.color = placeInPriorityQueue ? TitleColor :
                new Color(1f, 0.42f, 0.36f, 1f);

            var slot = new IntentSlot
            {
                Root = slotObject,
                Background = background,
                Icon = itemIcon,
                Marker = marker,
                Index = index,
                PriorityQueue = placeInPriorityQueue
            };
            slot.Button = slotObject.GetComponent<Button>();
            slot.Button.targetGraphic = background;
            slot.Button.onClick.AddListener(() => ActivateIntentSlot(slot));
            slotObject.GetComponent<InventoryIntentDropTarget>().Configure(
                background,
                icon => DropIntoIntentSlot(slot, icon),
                () => RemoveIntentSlot(slot));
            AddBorders(rect);
            return slot;
        }

        private void ConfigureIntentSlotNavigation()
        {
            for (int index = 0; index < IntentSlots; index++)
            {
                ConfigureIntentSlotNavigation(prioritySlots, avoidSlots,
                    index);
                ConfigureIntentSlotNavigation(avoidSlots, prioritySlots,
                    index);
            }
        }

        private static void ConfigureIntentSlotNavigation(
            IReadOnlyList<IntentSlot> row,
            IReadOnlyList<IntentSlot> otherRow, int index)
        {
            IntentSlot slot = row[index];
            Navigation navigation = new()
            {
                mode = Navigation.Mode.Explicit,
                selectOnLeft = index > 0 ? row[index - 1].Button : null,
                selectOnRight = index + 1 < row.Count
                    ? row[index + 1].Button
                    : null,
                selectOnUp = slot.PriorityQueue
                    ? null
                    : otherRow[index].Button,
                selectOnDown = slot.PriorityQueue
                    ? otherRow[index].Button
                    : null
            };
            slot.Button.navigation = navigation;
        }

        private TargetRow CreateTargetRow(RectTransform parent,
            TextMeshProUGUI template, int index)
        {
            float y = -344f - index * 41f;
            var row = new TargetRow
            {
                Root = new GameObject("TargetRow" + index,
                    typeof(RectTransform))
            };
            RectTransform rect = row.Root.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            SetTopRect(rect, new Vector2(8f, y), new Vector2(344f, 36f));
            row.Name = CreateText("Name", rect, template,
                new Vector2(4f, 0f), new Vector2(126f, 36f),
                TextAlignmentOptions.MidlineLeft, 0.48f, FontStyles.Normal);
            row.Name.color = PrimaryText;
            row.Choice = CreateButton("Choice", rect, template,
                new Vector2(132f, 3f), new Vector2(88f, 30f),
                () => CycleChoice(row), out row.ChoiceText);
            row.Decrease = CreateButton("Decrease", rect, template,
                new Vector2(226f, 3f), new Vector2(28f, 30f),
                () => AdjustRequiredValue(row, -1), out row.DecreaseText);
            row.Value = CreateText("Value", rect, template,
                new Vector2(258f, 0f), new Vector2(48f, 36f),
                TextAlignmentOptions.Center, 0.52f, FontStyles.Bold);
            row.Value.color = PrimaryText;
            row.Increase = CreateButton("Increase", rect, template,
                new Vector2(310f, 3f), new Vector2(28f, 30f),
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
            ProjectIntentBoard(preferences);
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
            markPriorities.interactable = editable;
            SetSelected(markPriorities, priorityMarking);
            optimize.interactable = editable && snapshot?.Items.Count > 0;
            if (!detailsExpanded)
            {
                return;
            }

            artifactTabText.text = Loc._(
                InventoryOptimizationLocalization.HudArtifactsTab);
            comboTabText.text = Loc._(
                InventoryOptimizationLocalization.HudCombosTab);
            previousPageText.text = "‹";
            nextPageText.text = "›";
            SetSelected(artifactTab,
                activeKind == InventoryOptimizationTargetKind.Artifact);
            SetSelected(comboTab,
                activeKind == InventoryOptimizationTargetKind.ComboCategory);

            IReadOnlyList<InventoryPreferenceEditorTarget> targets =
                InventoryPreferenceEditor.BuildTargets(snapshot, preferences,
                    activeKind);
            int pageCount = Math.Max(1,
                (targets.Count + RowsPerPage - 1) / RowsPerPage);
            page = Mathf.Clamp(page, 0, pageCount - 1);
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

                InventoryPreferenceEditorTarget target = targets[targetIndex];
                row.Target = target;
                row.Root.SetActive(true);
                row.Name.text = DisplayName(target);
                row.ChoiceText.text = Loc._(
                    InventoryOptimizationLocalization.PreferenceChoiceKeys[
                        (int)target.Choice]);
                row.Value.text = target.Choice ==
                        InventoryPreferenceChoice.Avoid && target.Kind ==
                            InventoryOptimizationTargetKind.Artifact
                    ? "—"
                    : target.Kind == InventoryOptimizationTargetKind.Artifact &&
                        target.RequiredValue == 0
                        ? Loc._(InventoryOptimizationLocalization.HudEnabled)
                    : target.RequiredValue.ToString();
                row.Choice.interactable = editable;
                row.Decrease.interactable = editable &&
                    target.CanAdjustRequiredValue && target.RequiredValue >
                        (target.Kind ==
                            InventoryOptimizationTargetKind.ComboCategory
                            ? 1
                            : 0);
                row.Increase.interactable = editable &&
                    target.CanAdjustRequiredValue &&
                    target.RequiredValue < target.MaximumValue;
            }

            previousPage.interactable = editable && page > 0;
            nextPage.interactable = editable && page + 1 < pageCount;
            artifactTab.interactable = editable;
            comboTab.interactable = editable;
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
            boardHint.text = Loc._(pendingArtifactInstanceId >= 0
                ? InventoryOptimizationLocalization.HudChooseIntentSlot
                : InventoryOptimizationLocalization.HudIntentBoardHint);

            var sourceIcons = new Dictionary<int, UI_NewInventoryIcon>();
            if (attachedPanel != null)
            {
                foreach (UI_NewInventoryIcon icon in attachedPanel.
                    GetComponentsInChildren<UI_NewInventoryIcon>(true))
                {
                    if (icon?.Item != null)
                    {
                        sourceIcons[icon.Item.InstanceID] = icon;
                    }
                }
            }
            ArtifactOptimizationPreference[] priorities =
                InventoryArtifactIntentEditor.OrderedPriorities(preferences);
            ArtifactOptimizationPreference[] avoided =
                InventoryArtifactIntentEditor.AvoidedInstances(preferences);
            for (int index = 0; index < IntentSlots; index++)
            {
                ProjectIntentSlot(prioritySlots[index],
                    index < priorities.Length ? priorities[index] : null,
                    sourceIcons);
                ProjectIntentSlot(avoidSlots[index],
                    index < avoided.Length ? avoided[index] : null,
                    sourceIcons);
            }
        }

        internal bool TryStageKeyboardArtifact(UI_NewInventoryIcon icon)
        {
            if (!panelOpen || root?.activeInHierarchy != true ||
                icon?.Item?.Charm == null ||
                !KeyboardUiNavigationController.IsKeyboardModeActive() ||
                EventSystem.current?.currentSelectedGameObject !=
                    icon.gameObject || UIInputModule.currentModule == null ||
                !KeyboardUiNavigationController.WasNativeUiActionPressed(
                    UIInputModule.currentModule.submit))
            {
                return false;
            }

            pendingArtifactInstanceId = icon.Item.InstanceID;
            pendingArtifactEntityId = icon.Item.EntityID;
            IntentSlot destination = prioritySlots.FirstOrDefault(slot =>
                    slot.Preference == null) ?? prioritySlots[0];
            EventSystem.current.SetSelectedGameObject(destination.Root);
            nextProjectionAt = 0f;
            return true;
        }

        private void ActivateIntentSlot(IntentSlot slot)
        {
            if (slot == null)
            {
                return;
            }
            if (pendingArtifactInstanceId >= 0 &&
                pendingArtifactEntityId >= 0)
            {
                InventoryOptimizationPreferences current =
                    ExplorationInventoryIntentStore.Capture();
                InventoryOptimizationPreferences updated = slot.PriorityQueue
                    ? InventoryArtifactIntentEditor.PlacePriority(current,
                        pendingArtifactInstanceId, pendingArtifactEntityId,
                        slot.Index)
                    : InventoryArtifactIntentEditor.PlaceAvoid(current,
                        pendingArtifactInstanceId, pendingArtifactEntityId);
                ReplacePreferences(updated);
                ClearPendingArtifact();
                nextProjectionAt = 0f;
                return;
            }
            if (slot.Preference != null)
            {
                pendingArtifactInstanceId = slot.Preference.InstanceId;
                pendingArtifactEntityId = slot.Preference.EntityId;
                nextProjectionAt = 0f;
            }
        }

        private void HandleKeyboardIntentRemoval()
        {
            Keyboard keyboard = Keyboard.current;
            if (!panelOpen || keyboard == null ||
                (!keyboard.deleteKey.wasPressedThisFrame &&
                    !keyboard.backspaceKey.wasPressedThisFrame))
            {
                return;
            }
            GameObject selected = EventSystem.current?.currentSelectedGameObject;
            IntentSlot slot = prioritySlots.Concat(avoidSlots).FirstOrDefault(
                candidate => candidate.Root == selected);
            if (slot?.Preference != null)
            {
                RemoveIntentSlot(slot);
                ClearPendingArtifact();
            }
        }

        private void ClearPendingArtifact()
        {
            pendingArtifactInstanceId = -1;
            pendingArtifactEntityId = -1;
        }

        private void ProjectIntentSlot(IntentSlot slot,
            ArtifactOptimizationPreference preference,
            IReadOnlyDictionary<int, UI_NewInventoryIcon> sourceIcons)
        {
            slot.Preference = preference;
            slot.Marker.text = slot.PriorityQueue
                ? (slot.Index + 1).ToString()
                : "×";
            UI_NewInventoryIcon source = null;
            if (preference != null)
            {
                sourceIcons.TryGetValue(preference.InstanceId, out source);
            }
            slot.Icon.sprite = source?.iconImage?.sprite;
            slot.Icon.material = source?.iconImage?.material;
            slot.Icon.color = source?.iconImage?.color ?? Color.white;
            slot.Background.sprite = source?.bgImage?.sprite ??
                nativeTemplates.InventoryIcon?.bgImage?.sprite;
            slot.Background.material = null;
            slot.Background.color = new Color(0.16f, 0.10f, 0.15f, 1f);
        }

        private void DropIntoIntentSlot(IntentSlot slot,
            UI_NewInventoryIcon icon)
        {
            NewItemOwnInstance item = icon?.Item;
            if (item?.Charm == null)
            {
                return;
            }
            InventoryOptimizationPreferences current =
                ExplorationInventoryIntentStore.Capture();
            InventoryOptimizationPreferences updated = slot.PriorityQueue
                ? InventoryArtifactIntentEditor.PlacePriority(current,
                    item.InstanceID, item.EntityID, slot.Index)
                : InventoryArtifactIntentEditor.PlaceAvoid(current,
                    item.InstanceID, item.EntityID);
            ReplacePreferences(updated);
            ClearPendingArtifact();
            nextProjectionAt = 0f;
        }

        private void RemoveIntentSlot(IntentSlot slot)
        {
            if (slot?.Preference == null)
            {
                return;
            }
            ReplacePreferences(InventoryArtifactIntentEditor.Remove(
                ExplorationInventoryIntentStore.Capture(),
                slot.Preference.InstanceId));
            ClearPendingArtifact();
            nextProjectionAt = 0f;
        }

        private static string DisplayName(InventoryPreferenceEditorTarget target)
        {
            if (target.Kind == InventoryOptimizationTargetKind.Artifact)
            {
                return target.DisplayName;
            }

            // Native integration boundary: categoryName is the game's
            // localized ItemCategoryEntity label, while CategoryId remains
            // the stable optimizer identifier.
            try
            {
                ItemCategoryEntity category =
                    ItemDatabase.FindItemCategory(target.CategoryId);
                string name = category?.categoryName?.ToString();
                return string.IsNullOrEmpty(name) ? target.DisplayName : name;
            }
            catch
            {
                return target.DisplayName;
            }
        }

        private void CycleChoice(TargetRow row)
        {
            if (row?.Target == null)
            {
                return;
            }
            InventoryOptimizationPreferences updated =
                InventoryPreferenceEditor.SetChoice(
                    ExplorationInventoryIntentStore.Capture(), row.Target,
                    InventoryPreferenceEditor.NextChoice(row.Target.Choice));
            ReplacePreferences(updated);
            nextProjectionAt = 0f;
        }

        private void AdjustRequiredValue(TargetRow row, int delta)
        {
            if (row?.Target?.CanAdjustRequiredValue != true)
            {
                return;
            }
            InventoryOptimizationPreferences updated =
                InventoryPreferenceEditor.SetRequiredValue(
                    ExplorationInventoryIntentStore.Capture(), row.Target,
                    row.Target.RequiredValue + delta);
            ReplacePreferences(updated);
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

        private void SelectKind(InventoryOptimizationTargetKind kind)
        {
            activeKind = kind;
            page = 0;
            nextProjectionAt = 0f;
        }

        private void ChangePage(int delta)
        {
            page = Math.Max(0, page + delta);
            nextProjectionAt = 0f;
        }

        private void ToggleDetails()
        {
            if (!panelOpen)
            {
                panelOpen = true;
                detailsExpanded = false;
            }
            else
            {
                detailsExpanded = !detailsExpanded;
            }
            page = 0;
            ApplyDisclosureLayout();
            PositionBesideInventory();
            nextProjectionAt = 0f;
        }

        private void ClosePanel()
        {
            endPriorityMarking?.Invoke();
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
            float height = !panelOpen
                ? LauncherHeight
                : detailsExpanded
                    ? ExpandedPanelHeight
                    : CollapsedPanelHeight;
            (root.transform as RectTransform).sizeDelta =
                new Vector2(width, height);
            if (panelBackground != null)
            {
                panelBackground.enabled = panelOpen;
                panelBackground.raycastTarget = panelOpen;
            }
            title?.gameObject.SetActive(panelOpen);
            summary?.gameObject.SetActive(panelOpen);
            priorityQueueTitle?.gameObject.SetActive(panelOpen);
            avoidZoneTitle?.gameObject.SetActive(panelOpen);
            boardHint?.gameObject.SetActive(panelOpen);
            foreach (IntentSlot slot in prioritySlots)
            {
                slot.Root.SetActive(panelOpen);
            }
            foreach (IntentSlot slot in avoidSlots)
            {
                slot.Root.SetActive(panelOpen);
            }
            close?.gameObject.SetActive(panelOpen);
            markPriorities?.gameObject.SetActive(panelOpen);
            optimize?.gameObject.SetActive(panelOpen);
            bool showTargets = panelOpen && detailsExpanded;
            artifactTab?.gameObject.SetActive(showTargets);
            comboTab?.gameObject.SetActive(showTargets);
            previousPage?.gameObject.SetActive(showTargets);
            nextPage?.gameObject.SetActive(showTargets);
            status?.gameObject.SetActive(showTargets);
            if (!showTargets)
            {
                foreach (TargetRow row in rows)
                {
                    row.Target = null;
                    row.Root.SetActive(false);
                }
            }

            if (details != null)
            {
                RectTransform detailsRect = details.transform as RectTransform;
                detailsRect.anchoredPosition = panelOpen
                    ? new Vector2(12f, -268f)
                    : Vector2.zero;
                detailsRect.sizeDelta = panelOpen
                    ? new Vector2(336f, 28f)
                    : new Vector2(LauncherWidth, LauncherHeight);
                detailsText?.gameObject.SetActive(panelOpen);
                launcherIcon?.gameObject.SetActive(!panelOpen);
            }

            if (markPriorities != null && optimize != null)
            {
                RectTransform markRect =
                    markPriorities.transform as RectTransform;
                RectTransform optimizeRect =
                    optimize.transform as RectTransform;
                float y = detailsExpanded ? -590f : -280f;
                markRect.anchoredPosition = new Vector2(12f, y);
                optimizeRect.anchoredPosition = new Vector2(180f,
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
            float panelHeight = !panelOpen
                ? LauncherHeight
                : detailsExpanded
                    ? ExpandedPanelHeight
                    : CollapsedPanelHeight;
            float leftAvailable = Mathf.Max(0f,
                leftLocal.x - canvasRoot.rect.xMin - PanelGap);
            float rightAvailable = Mathf.Max(0f,
                canvasRoot.rect.xMax - rightLocal.x - PanelGap);
            bool placeRight = rightAvailable > leftAvailable;
            float sideAvailable = placeRight
                ? rightAvailable
                : leftAvailable;
            float heightAvailable = Mathf.Max(LauncherHeight,
                canvasRoot.rect.height - 24f);
            float canvasUnitScale = Mathf.Min(1f,
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
                typeof(RectTransform), typeof(Image), typeof(Button));
            buttonObject.name = "SmartInventoryLauncher";
            RectTransform rect = buttonObject.transform as RectTransform;
            rect.SetParent(parent, false);
            SetTopRect(rect, Vector2.zero,
                new Vector2(LauncherWidth, LauncherHeight));
            Button button = buttonObject.GetComponent<Button>();
            Button nativeButton =
                nativeTemplates.LauncherButton.GetComponent<Button>();
            Image background = buttonObject.GetComponent<Image>();
            ApplyImageStyle(background,
                nativeButton?.targetGraphic as Image, ButtonColor);
            button.targetGraphic = background;
            if (nativeButton != null)
            {
                button.transition = nativeButton.transition;
                button.colors = nativeButton.colors;
                button.spriteState = nativeButton.spriteState;
                button.animationTriggers = nativeButton.animationTriggers;
            }
            button.onClick.AddListener(() => onClick?.Invoke());

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
                TextAlignmentOptions.Center, 0.5f, FontStyles.Bold,
                childCoordinates: true);
            label.color = PrimaryText;
            return button;
        }

        private Button CreateButton(string name, RectTransform parent,
            TextMeshProUGUI template, Vector2 position, Vector2 size,
            Action onClick, out TextMeshProUGUI label)
        {
            GameObject buttonObject = new(name, typeof(RectTransform),
                typeof(Image), typeof(Button));
            RectTransform rect = buttonObject.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            SetTopRect(rect, position, size);
            Image image = buttonObject.GetComponent<Image>();
            Button button = buttonObject.GetComponent<Button>();
            ApplyButtonStyle(image, button);
            button.targetGraphic = image;
            button.onClick.AddListener(() => onClick?.Invoke());
            label = CreateText("Label", rect, template, Vector2.zero, size,
                TextAlignmentOptions.Center, 0.5f, FontStyles.Bold,
                childCoordinates: true);
            label.color = PrimaryText;
            return button;
        }

        private void ApplyButtonStyle(Image image, Button button)
        {
            Button template = nativeTemplates?.ContentButton;
            Image templateImage = template?.targetGraphic as Image;
            ApplyImageStyle(image, templateImage, ButtonColor);
            if (template == null)
            {
                return;
            }
            button.transition = template.transition;
            button.colors = template.colors;
            button.spriteState = template.spriteState;
            button.animationTriggers = template.animationTriggers;
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
            destination.overrideSprite = source.overrideSprite;
            destination.type = source.type;
            destination.preserveAspect = source.preserveAspect;
            destination.fillCenter = source.fillCenter;
            destination.fillMethod = source.fillMethod;
            destination.fillAmount = source.fillAmount;
            destination.fillClockwise = source.fillClockwise;
            destination.fillOrigin = source.fillOrigin;
            destination.pixelsPerUnitMultiplier =
                source.pixelsPerUnitMultiplier;
            destination.material = source.material;
            destination.color = source.color;
        }

        private static TextMeshProUGUI CreateText(string name,
            RectTransform parent, TextMeshProUGUI template, Vector2 position,
            Vector2 size, TextAlignmentOptions alignment, float sizeRatio,
            FontStyles fontStyle, bool childCoordinates = false)
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
            text.fontStyle = fontStyle;
            text.fontSize = Mathf.Max(8f, template.fontSize * sizeRatio);
            text.enableAutoSizing = true;
            text.fontSizeMin = 7f;
            text.fontSizeMax = Mathf.Max(9f,
                template.fontSize * sizeRatio);
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Ellipsis;
            text.alignment = alignment;
            text.raycastTarget = false;
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

        private static void SetSelected(Button button, bool selected)
        {
            if (button?.targetGraphic is Image image)
            {
                image.color = selected ? SelectedButtonColor : ButtonColor;
            }
        }

        private static void AddBorders(RectTransform parent)
        {
            AddBorder(parent, new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(0f, -1f), new Vector2(0f, 2f));
            AddBorder(parent, Vector2.zero, new Vector2(1f, 0f),
                new Vector2(0f, 1f), new Vector2(0f, 2f));
            AddBorder(parent, Vector2.zero, new Vector2(0f, 1f),
                new Vector2(1f, 0f), new Vector2(2f, 0f));
            AddBorder(parent, new Vector2(1f, 0f), Vector2.one,
                new Vector2(-1f, 0f), new Vector2(2f, 0f));
        }

        private static void AddBorder(RectTransform parent, Vector2 anchorMin,
            Vector2 anchorMax, Vector2 position, Vector2 size)
        {
            GameObject borderObject = new("Border", typeof(RectTransform),
                typeof(Image));
            RectTransform rect = borderObject.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            Image image = borderObject.GetComponent<Image>();
            image.color = Border;
            image.raycastTarget = false;
        }

        private void SetVisible(bool visible)
        {
            if (root != null && root.activeSelf != visible)
            {
                root.SetActive(visible);
            }
        }

        private void DestroyRoot()
        {
            if (root != null)
            {
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
            launcherIcon = null;
            close = null;
            artifactTab = null;
            comboTab = null;
            previousPage = null;
            nextPage = null;
            markPriorities = null;
            optimize = null;
            artifactTabText = null;
            comboTabText = null;
            previousPageText = null;
            nextPageText = null;
            markPrioritiesText = null;
            optimizeText = null;
            detailsText = null;
            closeText = null;
            priorityQueueTitle = null;
            avoidZoneTitle = null;
            boardHint = null;
            nativeTemplates = null;
            rows.Clear();
            prioritySlots.Clear();
            avoidSlots.Clear();
        }

        private sealed class TargetRow
        {
            internal GameObject Root;
            internal TextMeshProUGUI Name;
            internal Button Choice;
            internal TextMeshProUGUI ChoiceText;
            internal Button Decrease;
            internal TextMeshProUGUI DecreaseText;
            internal TextMeshProUGUI Value;
            internal Button Increase;
            internal TextMeshProUGUI IncreaseText;
            internal InventoryPreferenceEditorTarget Target;
        }

        private sealed class IntentSlot
        {
            internal GameObject Root;
            internal Image Background;
            internal Image Icon;
            internal TextMeshProUGUI Marker;
            internal int Index;
            internal bool PriorityQueue;
            internal Button Button;
            internal ArtifactOptimizationPreference Preference;
        }
    }
}
