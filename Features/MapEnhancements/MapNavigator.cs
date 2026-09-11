using System;
using System.Collections.Generic;
using SephiriaEnhancements.Configuration;
using SephiriaEnhancements.Integration;
using SephiriaEnhancements.MapEnhancements.Core;
using SephiriaEnhancements.MapEnhancements.Integration;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.InputSystem;

namespace SephiriaEnhancements.MapEnhancements
{
    internal sealed class MapNavigator
    {
        private readonly UI_MapPanel panel;
        private readonly UI_Map map;
        private readonly NativeMapGeometry geometry;
        private readonly GameObject nativeSelection;
        private readonly NativeRoomNavigation roomNavigation;
        private readonly UI_HorayButton roomsButton;
        private readonly TextMeshProUGUI template, details;
        private readonly RectTransform uiRoot, listContent, viewport, scrollFrame;
        private readonly ScrollRect nativeScroll;
        private readonly Vector2 originalMin, originalMax, originalContentSize;
        private readonly Vector2 originalContentPosition;
        private readonly Vector3 originalScale;
        private readonly float originalSensitivity;
        private readonly ScrollRect.MovementType originalMovementType;
        private Vector2 fittedViewportSize;
        private GameObject centeredRoomSelection;
        private readonly MapZoomWheel wheel;
        private readonly NativeMapTravel travel;
        private readonly UI_HorayButton peopleButton, placesButton, fitButton, travelButton, minusButton, plusButton, browseButton, trackButton;
        private readonly TextMeshProUGUI helpText;
        private readonly TextMeshProUGUI focusedName;
        private readonly RectTransform focusedLabel, focusedLeader;
        private readonly MapPanSurface panSurface;
        private readonly TextMeshProUGUI emptyText;
        private readonly List<MapLocationMarkerView> entries = new();
        private readonly List<MapLocationMarkerView> rowEntries = new();
        private readonly List<UI_HorayButton> rows = new();
        private readonly List<MapLabelBounds> occupied = new();
        private readonly Dictionary<Transform, Vector3> nativeSymbolScales = new();
        private GameObject localRoomSelection;
        private MapLocationMarkerView selected;
        private MapNavigationMode mode;
        private readonly TextMeshProUGUI modeBindings;
        private bool initialRoomFocusPending;
        private float fitScale = 1f, zoom = 1f;
        internal float Scale => fitScale * zoom;
        internal GameObject DefaultSelection => mode == MapNavigationMode.Rooms
            ? RoomSelection() ?? roomsButton.gameObject
            : rows.Count > 0 ? rows[0].gameObject : mode == MapNavigationMode.People ? peopleButton.gameObject : placesButton.gameObject;

        private GameObject RoomSelection() => roomNavigation?.FirstSelection(
            LocalPlayerResolver.Resolve()?.transform.position ?? Vector3.zero) ??
            (nativeSelection != null && nativeSelection.activeInHierarchy ? nativeSelection : null);

        private void FocusRooms()
        {
            var target = RoomSelection();
            if (target != null) EventSystem.current?.SetSelectedGameObject(target);
        }

        internal void FocusCurrentRoom()
        {
            if (roomNavigation == null) return;
            roomNavigation.Refresh();
            GameObject target = RoomSelection();
            if (target == null) return;
            localRoomSelection = target;
            initialRoomFocusPending = false;
            panel.defaultSelectable = target;
            var stack = UIManager.Instance?.CurrentControlStack;
            if (stack != null && stack.Count > 0 && stack[stack.Count - 1] == panel)
                EventSystem.current?.SetSelectedGameObject(target);
        }

        internal MapNavigator(UI_MapPanel owner, UI_Map shownMap,
            NativeMapGeometry geometry, TextMeshProUGUI bodyTemplate, TextMeshProUGUI detailText)
        {
            panel = owner; map = shownMap; this.geometry = geometry; template = bodyTemplate; details = detailText;
            nativeSelection = geometry.Designed == null ? panel.defaultSelectable : null;
            if (geometry.Designed == null) roomNavigation = new NativeRoomNavigation(map);
            initialRoomFocusPending = roomNavigation != null;
            mode = MapNavigationModes.Initial(roomNavigation != null);
            viewport = (RectTransform)panel.contentsParent.parent;
            nativeScroll = viewport.GetComponentInParent<ScrollRect>();
            scrollFrame = (RectTransform)nativeScroll.transform;
            originalMin = scrollFrame.offsetMin; originalMax = scrollFrame.offsetMax;
            originalContentSize = panel.contentsParent.sizeDelta;
            originalContentPosition = panel.contentsParent.anchoredPosition;
            originalScale = map.rectTransform.localScale;
            originalSensitivity = nativeScroll.scrollSensitivity;
            originalMovementType = nativeScroll.movementType;
            nativeScroll.movementType = ScrollRect.MovementType.Unrestricted;
            nativeScroll.StopMovement();
            if (geometry.Designed != null)
            foreach (var connection in geometry.Designed.mapTeleportConnections)
                if (connection.icon != null && connection.icon.transform.IsChildOf(map.contentsChild))
                    nativeSymbolScales[connection.icon.transform] = connection.icon.transform.localScale;
            scrollFrame.offsetMin = new Vector2(154f, 73f);
            scrollFrame.offsetMax = new Vector2(-24f, -32f);
            nativeScroll.scrollSensitivity = 0;
            wheel = viewport.gameObject.AddComponent<MapZoomWheel>();
            wheel.Zoom = ScrollZoom;
            uiRoot = Rect("Map Navigation", scrollFrame.parent, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            uiRoot.offsetMin = uiRoot.offsetMax = Vector2.zero;
            travel = new NativeMapTravel(geometry, panel);
            if (roomNavigation != null)
                roomsButton = Button(uiRoot, MapNavigationLocalization.Rooms, new Vector2(24, -31), new Vector2(57, 17), () => SetMode(MapNavigationMode.Rooms));
            peopleButton = Button(uiRoot, MapNavigationLocalization.People, new Vector2(roomNavigation != null ? 85 : 24, -31), new Vector2(57, 17), () => SetMode(MapNavigationMode.People));
            placesButton = Button(uiRoot, MapNavigationLocalization.Places, new Vector2(roomNavigation != null ? 146 : 85, -31), new Vector2(57, 17), () => SetMode(MapNavigationMode.Places));
            modeBindings = Text(uiRoot, string.Empty);
            modeBindings.rectTransform.anchorMin = modeBindings.rectTransform.anchorMax = new Vector2(0, 1);
            modeBindings.rectTransform.pivot = new Vector2(0, 1);
            modeBindings.rectTransform.anchoredPosition = new Vector2(213, -31);
            modeBindings.rectTransform.sizeDelta = new Vector2(120, 17);
            RectTransform listView = Rect("Destinations", uiRoot, new Vector2(0, 1), new Vector2(0, 1), new Vector2(24, -52), new Vector2(118, 142));
            listView.pivot = new Vector2(0, 1);
            listView.gameObject.AddComponent<RectMask2D>();
            listView.gameObject.AddComponent<Image>().color = Color.clear;
            listContent = Rect("Entries", listView, new Vector2(0, 1), new Vector2(1, 1), Vector2.zero, Vector2.zero);
            listContent.pivot = new Vector2(0.5f, 1);
            ScrollRect listScroll = listView.gameObject.AddComponent<ScrollRect>();
            listScroll.viewport = listView; listScroll.content = listContent;
            listScroll.horizontal = false; listScroll.vertical = true;
            listScroll.movementType = ScrollRect.MovementType.Clamped;
            listScroll.scrollSensitivity = 18;
            listView.gameObject.AddComponent<UI_ScrollToSelection>();
            emptyText = Text(listView, ModLocalization.Get(MapNavigationLocalization.Empty));
            emptyText.rectTransform.sizeDelta = new Vector2(114, 35);
            emptyText.rectTransform.anchorMin = emptyText.rectTransform.anchorMax = new Vector2(.5f, 1);
            emptyText.rectTransform.anchoredPosition = new Vector2(0, -20);
            minusButton = Button(uiRoot, null, new Vector2(154, -182), new Vector2(36, 17), () => ZoomBy(1f / 1.25f));
            minusButton.text.text = "−";
            fitButton = Button(uiRoot, MapNavigationLocalization.Fit, new Vector2(194, -182), new Vector2(48, 17), Fit);
            plusButton = Button(uiRoot, null, new Vector2(246, -182), new Vector2(36, 17), () => ZoomBy(1.25f));
            plusButton.text.text = "+";
            travelButton = Button(uiRoot, MapNavigationLocalization.Travel, new Vector2(154, -201), new Vector2(314, 17), Travel);
            browseButton = Button(uiRoot, MapNavigationLocalization.Browse, new Vector2(286, -182), new Vector2(86, 17),
                () => EventSystem.current?.SetSelectedGameObject(panSurface.gameObject));
            var panObject = Rect("Map Panning", viewport, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            panObject.offsetMin = panObject.offsetMax = Vector2.zero;
            panSurface = panObject.gameObject.AddComponent<MapPanSurface>();
            panSurface.Pan = direction => { nativeScroll.StopMovement(); panel.contentsParent.anchoredPosition -= direction * 24; };
            panSurface.Return = FocusSelectedRow;
            focusedLeader = Ink("Selected Target Link", viewport, Vector2.one);
            focusedLabel = Ink("Selected Target Name", viewport, Vector2.zero);
            focusedLabel.GetComponent<Image>().color = new Color(.92f, .76f, .56f, .98f);
            focusedName = Text(focusedLabel, string.Empty);
            focusedName.rectTransform.anchorMin = Vector2.zero; focusedName.rectTransform.anchorMax = Vector2.one;
            focusedName.rectTransform.offsetMin = new Vector2(4, 2); focusedName.rectTransform.offsetMax = new Vector2(-4, -2);
            focusedName.alignment = TextAlignmentOptions.Center;
            focusedName.textWrappingMode = TextWrappingModes.Normal;
            trackButton = Button(uiRoot, MapNavigationLocalization.Track, new Vector2(376, -182), new Vector2(92, 17), ToggleTracking);
            helpText = Text(uiRoot, string.Empty);
            helpText.rectTransform.anchorMin = helpText.rectTransform.anchorMax = new Vector2(.5f, 0);
            helpText.rectTransform.anchoredPosition = new Vector2(0, 20);
            helpText.rectTransform.sizeDelta = new Vector2(445, 14);
            details.rectTransform.SetParent(uiRoot, false);
            details.rectTransform.anchorMin = details.rectTransform.anchorMax = new Vector2(0, 0);
            details.rectTransform.pivot = new Vector2(0, 0);
            details.rectTransform.anchoredPosition = new Vector2(24, 30);
            details.rectTransform.sizeDelta = new Vector2(118, 18);
            Fit();
        }

        private static RectTransform Rect(string name, Transform parent, Vector2 min, Vector2 max, Vector2 position, Vector2 size)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false); rect.anchorMin = min; rect.anchorMax = max;
            rect.anchoredPosition = position; rect.sizeDelta = size; return rect;
        }

        private RectTransform Ink(string name, Transform parent, Vector2 size)
        {
            var rect = Rect(name, parent, new Vector2(.5f, .5f), new Vector2(.5f, .5f), Vector2.zero, size);
            var image = rect.gameObject.AddComponent<Image>();
            image.color = template.color; image.raycastTarget = false;
            return rect;
        }

        private TextMeshProUGUI Text(Transform parent, string value)
        {
            var text = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI)).GetComponent<TextMeshProUGUI>();
            text.rectTransform.SetParent(parent, false); text.text = value; text.color = template.color;
            text.alignment = TextAlignmentOptions.MidlineLeft; text.raycastTarget = false;
            text.textWrappingMode = TextWrappingModes.NoWrap; text.overflowMode = TextOverflowModes.Ellipsis;
            NativeLocalizedText.BindFont(text, template); NativeLocalizedText.MatchFontSize(text, template);
            return text;
        }

        private UI_HorayButton Button(Transform parent, string key, Vector2 position, Vector2 size, Action click)
        {
            RectTransform rect = Rect("Map Button", parent, new Vector2(0, 1), new Vector2(0, 1), position, size);
            rect.gameObject.SetActive(false);
            rect.pivot = new Vector2(0, 1);
            Image background = rect.gameObject.AddComponent<Image>();
            Color tint = template.color; tint.a = .08f; background.color = tint;
            var button = rect.gameObject.AddComponent<UI_HorayButton>();
            button.targetGraphic = background;
            // Focus is rendered explicitly below; do not mix a native color tween
            // with the persistent target/category background.
            button.transition = Selectable.Transition.None;
            button.text = Text(rect, key == null ? string.Empty : ModLocalization.Get(key));
            button.text.rectTransform.anchorMin = Vector2.zero; button.text.rectTransform.anchorMax = Vector2.one;
            button.text.rectTransform.offsetMin = new Vector2(4, 0); button.text.rectTransform.offsetMax = new Vector2(-4, 0);
            button.onClick.AddListener(() => click());
            rect.gameObject.SetActive(true);
            return button;
        }

        internal void Sync(IEnumerable<MapLocationMarkerView> markers)
        {
            entries.Clear(); entries.AddRange(markers);
            entries.Sort((a, b) => { int c = string.Compare(a.Label, b.Label, StringComparison.CurrentCulture); return c != 0 ? c : a.GetInstanceID().CompareTo(b.GetInstanceID()); });
            var filtered = entries.FindAll(m => mode != MapNavigationMode.Rooms && m.IsPerson == (mode == MapNavigationMode.People));
            bool changed = filtered.Count != rowEntries.Count;
            for (int i = 0; !changed && i < filtered.Count; i++) changed = filtered[i] != rowEntries[i];
            if (changed)
            {
                int focusedIndex = rows.FindIndex(row => EventSystem.current?.currentSelectedGameObject == row.gameObject);
                MapLocationMarkerView focused = focusedIndex >= 0 ? rowEntries[focusedIndex] : null;
                foreach (var row in rows) { row.gameObject.SetActive(false); UnityEngine.Object.Destroy(row.gameObject); }
                rows.Clear(); rowEntries.Clear(); rowEntries.AddRange(filtered);
                for (int i = 0; i < filtered.Count; i++)
                {
                    MapLocationMarkerView marker = filtered[i];
                    var row = Button(listContent, null, new Vector2(0, -i * 18), new Vector2(118, 17), () => { Select(marker, true); Travel(); });
                    row.gameObject.AddComponent<MapNavigationSelection>().Selected = () => Select(marker, true);
                    rows.Add(row);
                }
                listContent.sizeDelta = new Vector2(0, rows.Count * 18);
                for (int i = 0; i < rows.Count; i++)
                {
                    rows[i].SetForceNavUp(i > 0 ? rows[i - 1] : rows[i]);
                    rows[i].SetForceNavDown(i + 1 < rows.Count ? rows[i + 1] : rows[i]);
                    rows[i].SetForceNavLeft(rows[i]);
                }
                if (selected == null || !filtered.Contains(selected)) selected = filtered.Count > 0 ? filtered[0] : null;
                if (focused != null && rows.Count > 0)
                {
                    int index = rowEntries.IndexOf(focused);
                    EventSystem.current?.SetSelectedGameObject(rows[Mathf.Max(0, index)].gameObject);
                }
            }
            UpdateNavigation();
            RefreshRowText();
            emptyText.gameObject.SetActive(mode != MapNavigationMode.Rooms && rows.Count == 0);
            listContent.parent.gameObject.SetActive(mode != MapNavigationMode.Rooms);
            foreach (var marker in entries)
            {
                bool interactive = mode != MapNavigationMode.Rooms && marker.IsPerson == (mode == MapNavigationMode.People);
                marker.GetComponent<UI_HorayButton>().enabled = interactive;
                marker.GetComponent<Image>().raycastTarget = interactive;
                marker.Selected = () => Select(marker, false);
                marker.Activated = () => { if (!interactive) return; Select(marker, false); Travel(); };
            }
            panel.defaultSelectable = DefaultSelection;
        }

        private void SetMode(MapNavigationMode value)
        {
            mode = value;
            initialRoomFocusPending = false;
            var currentEntries = entries.ToArray();
            Sync(currentEntries);
            if (mode == MapNavigationMode.Rooms) FocusRooms();
            else FocusSelectedRow();
        }

        private void Select(MapLocationMarkerView marker, bool center)
        {
            if (mode == MapNavigationMode.Rooms || marker.IsPerson != (mode == MapNavigationMode.People)) return;
            selected = marker;
            if (!center)
            {
                int index = rowEntries.IndexOf(marker);
                if (index >= 0) listContent.anchoredPosition = new Vector2(0,
                    Mathf.Clamp(index * 18 - 62, 0, Mathf.Max(0, rows.Count * 18 - 142)));
            }
            if (center)
            {
                Vector2 position = viewport.InverseTransformPoint(marker.transform.position);
                Rect safe = viewport.rect;
                safe.xMin += 24; safe.xMax -= 24; safe.yMin += 24; safe.yMax -= 24;
                if (!safe.Contains(position)) Center(marker.transform.position);
            }
        }

        private void Center(Vector3 worldPoint)
        {
            nativeScroll.StopMovement();
            // The native viewport pivots at its top-left, not its center.
            panel.contentsParent.anchoredPosition += viewport.rect.center - (Vector2)viewport.InverseTransformPoint(worldPoint);
        }

        internal void Fit()
        {
            Canvas.ForceUpdateCanvases();
            Vector2 size = map.rectTransform.rect.size;
            fittedViewportSize = viewport.rect.size;
            fitScale = Mathf.Min(1f, Mathf.Min(viewport.rect.width / Mathf.Max(1, size.x), viewport.rect.height / Mathf.Max(1, size.y)) * .9f);
            zoom = 1; ApplyScale();
            Center(map.rectTransform.TransformPoint(map.rectTransform.rect.center));
        }

        private void ApplyScale()
        {
            map.rectTransform.localScale = Vector3.one * Scale;
            foreach (var symbol in nativeSymbolScales)
                if (symbol.Key != null) symbol.Key.localScale = symbol.Value / Scale;
            panel.contentsParent.sizeDelta = map.rectTransform.sizeDelta * Scale;
        }

        private void ZoomBy(float factor)
        {
            Vector3 anchor = selected != null ? selected.transform.position : viewport.TransformPoint(viewport.rect.center);
            Vector2 before = viewport.InverseTransformPoint(anchor);
            Vector3 local = map.rectTransform.InverseTransformPoint(anchor);
            zoom = Mathf.Clamp(zoom * factor, 1, 4); ApplyScale();
            panel.contentsParent.anchoredPosition += before - (Vector2)viewport.InverseTransformPoint(map.rectTransform.TransformPoint(local));
            nativeScroll.StopMovement();
        }

        private void ScrollZoom(PointerEventData data)
        {
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(viewport, data.position, data.enterEventCamera, out Vector2 point)) return;
            Vector3 local = map.rectTransform.InverseTransformPoint(viewport.TransformPoint(point));
            zoom = Mathf.Clamp(zoom * (data.scrollDelta.y > 0 ? 1.25f : 1 / 1.25f), 1, 4); ApplyScale();
            panel.contentsParent.anchoredPosition += point - (Vector2)viewport.InverseTransformPoint(map.rectTransform.TransformPoint(local));
            nativeScroll.StopMovement();
        }

        internal void Tick()
        {
            var stack = UIManager.Instance?.CurrentControlStack;
            if (stack == null || stack.Count == 0 || stack[stack.Count - 1] != panel) return;
            if (viewport.rect.size != fittedViewportSize) Fit();
            roomNavigation?.Refresh();
            if (roomNavigation != null)
            {
                GameObject currentRoom = RoomSelection();
                if (currentRoom != null && currentRoom != localRoomSelection)
                {
                    localRoomSelection = currentRoom;
                    if (mode == MapNavigationMode.Rooms)
                    {
                        panel.defaultSelectable = currentRoom;
                        EventSystem.current?.SetSelectedGameObject(currentRoom);
                    }
                }
            }
            if (initialRoomFocusPending && RoomSelection() != null)
            {
                initialRoomFocusPending = false;
                FocusRooms();
            }
            // Center on navigation changes, allowing subsequent panning and Fit to persist.
            GameObject roomSelection = EventSystem.current?.currentSelectedGameObject;
            if (geometry.Designed == null && roomSelection != centeredRoomSelection &&
                ControlsChangeHandler.Current != null && !ControlsChangeHandler.Current.IsUsingKeyboardAndMouse)
                foreach (var room in map.rooms)
                    if (room != null && room.GetSelectable() == roomSelection)
                    {
                        Center(map.contentsChild.TransformPoint(room.GetIconCenterAnchoredPosition()));
                        break;
                    }
            centeredRoomSelection = roomSelection;
            UIInputModule input = UIInputModule.current;
            InputAction trackAction = NativeInputActions.FindShortcut(PlayerInputController.Instance?.playerInput?.actions, ModShortcuts.SwitchLockedTarget);
            if (trackAction?.WasPressedThisFrame() == true) ToggleTracking();
            bool tracked = selected != null && MapEnhancementsController.TrackedNpc == selected.Target;
            trackButton.interactable = selected != null && selected.IsPerson;
            trackButton.text.text = ModLocalization.Get(tracked ? MapNavigationLocalization.Untrack : MapNavigationLocalization.Track) + " " + Binding(trackAction);
            if (input != null)
            {
                if (input.prevTabUIAction.action.WasPressedThisFrame()) SetMode(MapNavigationModes.Cycle(mode, -1, roomNavigation != null));
                if (input.nextTabUIAction.action.WasPressedThisFrame()) SetMode(MapNavigationModes.Cycle(mode, 1, roomNavigation != null));
                if (input.prevTab2Action.action.WasPressedThisFrame()) ZoomBy(1 / 1.25f);
                if (input.nextTab2Action.action.WasPressedThisFrame()) ZoomBy(1.25f);
            }
            Vector2 pan = input != null ? input.rightStickScrollAction.action.ReadValue<Vector2>() : Vector2.zero;
            if (pan.sqrMagnitude > .01f)
            {
                nativeScroll.StopMovement();
                panel.contentsParent.anchoredPosition -= pan * (100 * Time.unscaledDeltaTime);
            }
            foreach (var text in uiRoot.GetComponentsInChildren<TextMeshProUGUI>()) NativeLocalizedText.MatchFontSize(text, template);
            details.text = mode != MapNavigationMode.Rooms && selected != null ? selected.Label : string.Empty;
            travelButton.gameObject.SetActive(mode != MapNavigationMode.Rooms);
            trackButton.gameObject.SetActive(mode == MapNavigationMode.People);
            MapTravelState travelState = mode == MapNavigationMode.Rooms
                ? MapTravelState.Unavailable : travel.State(selected);
            travelButton.interactable = travelState == MapTravelState.Ready;
            UpdateNavigation();
            peopleButton.text.text = ModLocalization.Get(MapNavigationLocalization.People);
            modeBindings.text = Binding(input?.prevTabUIAction?.action) + " / " + Binding(input?.nextTabUIAction?.action) + "  ↔";
            placesButton.text.text = ModLocalization.Get(MapNavigationLocalization.Places);
            fitButton.text.text = ModLocalization.Get(MapNavigationLocalization.Fit);
            travelButton.text.text = ModLocalization.Get(selected?.Destination != null
                ? MapNavigationLocalization.DestinationTravel
                : geometry.Designed != null ? MapNavigationLocalization.Travel : MapNavigationLocalization.RoomTravel);
            if (selected != null && travelState != MapTravelState.Ready)
                travelButton.text.text = ModLocalization.Get(travelState switch
                {
                    MapTravelState.MapNotReady => MapNavigationLocalization.MapNotReady,
                    MapTravelState.NoNearbyLanding => MapNavigationLocalization.NoLanding,
                    _ => MapNavigationLocalization.TravelUnavailable
                });
            emptyText.text = ModLocalization.Get(MapNavigationLocalization.Empty);
            minusButton.text.text = "− " + Binding(input?.prevTab2Action?.action);
            plusButton.text.text = "+ " + Binding(input?.nextTab2Action?.action);
            browseButton.text.text = ModLocalization.Get(MapNavigationLocalization.Browse);
            if (roomsButton != null) roomsButton.text.text = ModLocalization.Get(MapNavigationLocalization.Rooms);
            helpText.text = EventSystem.current?.currentSelectedGameObject == panSurface.gameObject
                ? string.Format(ModLocalization.Get(MapNavigationLocalization.PanGuide), Binding(UIInputModule.currentModule?.submit?.action))
                : IsRoomFocused()
                    ? string.Format(ModLocalization.Get(MapNavigationLocalization.SelectRoomGuide), Binding(UIInputModule.currentModule?.submit?.action))
                    : string.Format(ModLocalization.Get(selected?.Destination != null
                        ? MapNavigationLocalization.DestinationGuide
                        : geometry.Designed != null ? MapNavigationLocalization.Guide : MapNavigationLocalization.RoomGuide), Binding(UIInputModule.currentModule?.submit?.action));
            RefreshRowText();
            LayoutLabels();
        }

        private void ToggleTracking()
        {
            if (mode == MapNavigationMode.People && selected != null && selected.IsPerson) MapEnhancementsController.ToggleNpcTracking(selected.Target);
        }

        private bool IsRoomFocused()
        {
            if (roomNavigation == null) return false;
            foreach (var room in map.rooms)
                if (room != null && room.GetSelectable() == EventSystem.current?.currentSelectedGameObject) return true;
            return false;
        }

        private void FocusSelectedRow()
        {
            if (mode == MapNavigationMode.Rooms) { FocusRooms(); return; }
            int index = rowEntries.IndexOf(selected);
            EventSystem.current?.SetSelectedGameObject(index >= 0 ? rows[index].gameObject : peopleButton.gameObject);
        }

        private void UpdateNavigation()
        {
            UI_HorayButton row = rows.Count > 0 ? rows[Mathf.Max(0, rowEntries.IndexOf(selected))] : peopleButton;
            foreach (var entry in rows)
                entry.SetForceNavRight(entry);
            peopleButton.SetForceNavRight(placesButton); peopleButton.SetForceNavLeft(placesButton);
            placesButton.SetForceNavLeft(peopleButton); placesButton.SetForceNavRight(minusButton);
            peopleButton.SetForceNavUp(fitButton); placesButton.SetForceNavUp(fitButton);
            peopleButton.SetForceNavDown(rows.Count > 0 ? row : minusButton);
            placesButton.SetForceNavDown(rows.Count > 0 ? row : minusButton);
            var toolbar = new[] { minusButton, fitButton, plusButton, browseButton, trackButton };
            for (int i = 0; i < toolbar.Length; i++)
            {
                toolbar[i].SetForceNavLeft(i > 0 ? toolbar[i - 1] : row);
                toolbar[i].SetForceNavRight(i + 1 < toolbar.Length ? toolbar[i + 1] : row);
                toolbar[i].SetForceNavUp(row);
                toolbar[i].SetForceNavDown(travelButton.interactable ? travelButton : row);
            }
            travelButton.SetForceNavUp(browseButton); travelButton.SetForceNavLeft(row); travelButton.SetForceNavRight(row);
            if (roomsButton != null)
            {
                placesButton.SetForceNavRight(roomsButton);
                roomsButton.SetForceNavLeft(row); roomsButton.SetForceNavRight(browseButton);
                roomsButton.SetForceNavUp(peopleButton); roomsButton.SetForceNavDown(browseButton);
            }
        }

        private static string Binding(InputAction action)
        {
            if (action == null) return string.Empty;
            bool gamepad = ControlsChangeHandler.Current != null && !ControlsChangeHandler.Current.IsUsingKeyboardAndMouse;
            foreach (var control in action.controls)
            {
                if (gamepad ? !(control.device is Gamepad) : !(control.device is Keyboard)) continue;
                int index = action.GetBindingIndexForControl(control);
                if (index >= 0) return action.GetBindingDisplayString(index);
            }
            return string.Empty;
        }

        private void RefreshRowText()
        {
            for (int i = 0; i < rows.Count; i++)
            {
                rows[i].text.text = rowEntries[i].Label;
                SetSelectedTint(rows[i], !IsRoomFocused() && rowEntries[i] == selected);
            }
            SetSelectedTint(peopleButton, mode == MapNavigationMode.People);
            SetSelectedTint(placesButton, mode == MapNavigationMode.Places);
            SetSelectedTint(minusButton, false);
            SetSelectedTint(fitButton, false);
            SetSelectedTint(plusButton, false);
            SetSelectedTint(browseButton, EventSystem.current?.currentSelectedGameObject == panSurface.gameObject);
            SetSelectedTint(travelButton, false);
            SetSelectedTint(trackButton, selected != null && MapEnhancementsController.TrackedNpc == selected.Target);
            if (roomsButton != null) SetSelectedTint(roomsButton, mode == MapNavigationMode.Rooms);
        }

        private void SetSelectedTint(UI_HorayButton button, bool isSelected)
        {
            Color tint = template.color;
            bool focused = EventSystem.current?.currentSelectedGameObject == button.gameObject;
            tint.a = focused ? .4f : isSelected ? .18f : .06f;
            button.GetComponent<Image>().color = tint;
        }

        private void LayoutLabels()
        {
            var selected = mode == MapNavigationMode.Rooms ? null : this.selected;
            occupied.Clear();
            var bounds = new MapLabelBounds(viewport.rect.xMin + 4, viewport.rect.yMin + 4, viewport.rect.width - 8, viewport.rect.height - 8);
            foreach (var marker in entries)
            {
                marker.SetScale(Scale);
                Vector2 p = viewport.InverseTransformPoint(marker.transform.position);
                occupied.Add(new MapLabelBounds(p.x - 5, p.y - 5, 10, 10));
            }
            LayoutFocusedName(bounds);
            // Selection and quest labels get first choice; all names remain in the list.
            for (int priority = 0; priority < 4; priority++)
                foreach (var marker in entries)
                {
                    int rank = marker == selected ? 0 : marker.HasQuest ? 1 : marker.IsPerson ? 2 : 3;
                    if (rank != priority) continue;
                    if (marker == selected)
                    {
                        marker.ShowName(false, Vector2.zero, true);
                        continue;
                    }
                    Vector2 p = viewport.InverseTransformPoint(marker.transform.position);
                    Vector2 size = marker.NameSize;
                    bool visible = MapLabelLayout.TryPlace(p.x, p.y, size.x, size.y, bounds, occupied, out var place);
                    marker.ShowName(visible, new Vector2(place.X + size.x / 2 - p.x, place.Y + size.y / 2 - p.y), marker == selected);
                }
        }

        private void LayoutFocusedName(MapLabelBounds bounds)
        {
            bool visible = mode != MapNavigationMode.Rooms && selected != null;
            focusedLabel.gameObject.SetActive(visible);

            focusedLeader.gameObject.SetActive(visible);
            if (!visible) return;
            NativeLocalizedText.MatchFontSize(focusedName, template);
            focusedName.text = selected.Label;
            float width = Mathf.Min(bounds.Width, focusedName.GetPreferredValues(selected.Label).x + 8);
            float height = Mathf.Min(bounds.Height, focusedName.GetPreferredValues(selected.Label, Mathf.Max(1, width - 8), float.PositiveInfinity).y + 4);
            Vector2 p = viewport.InverseTransformPoint(selected.transform.position);
            if (!MapLabelLayout.TryPlace(p.x, p.y, width, height, bounds, occupied, out var place))
                place = MapLabelLayout.PlaceFocused(p.x, p.y, width, height, bounds);
            occupied.Add(new MapLabelBounds(place.X - 2, place.Y - 2, place.Width + 4, place.Height + 4));
            focusedLabel.localPosition = new Vector3(place.X + place.Width / 2, place.Y + place.Height / 2, 0);
            focusedLabel.sizeDelta = new Vector2(place.Width, place.Height);

            Vector2 end = new Vector2(Mathf.Clamp(p.x, place.X, place.X + place.Width), Mathf.Clamp(p.y, place.Y, place.Y + place.Height));
            Vector2 delta = end - p;
            focusedLeader.localPosition = (p + end) / 2;
            focusedLeader.sizeDelta = new Vector2(delta.magnitude, 1);
            focusedLeader.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
            focusedLeader.SetAsLastSibling(); focusedLabel.SetAsLastSibling();
        }

        private void Travel()
        {
            if (mode != MapNavigationMode.Rooms) travel.Travel(selected);
        }

        internal void Clear()
        {
            travel.Clear();
            roomNavigation?.Clear();
            if (nativeScroll != null) { nativeScroll.StopMovement(); nativeScroll.scrollSensitivity = originalSensitivity; nativeScroll.movementType = originalMovementType; }
            if (scrollFrame != null) { scrollFrame.offsetMin = originalMin; scrollFrame.offsetMax = originalMax; }
            if (map != null) map.rectTransform.localScale = originalScale;
            foreach (var symbol in nativeSymbolScales)
                if (symbol.Key != null) symbol.Key.localScale = symbol.Value;
            if (panel != null)
            {
                panel.contentsParent.sizeDelta = originalContentSize;
                panel.contentsParent.anchoredPosition = originalContentPosition;
            }
            if (wheel != null) { wheel.Zoom = null; UnityEngine.Object.Destroy(wheel); }
            if (panSurface != null) UnityEngine.Object.Destroy(panSurface.gameObject);
            if (focusedLabel != null) UnityEngine.Object.Destroy(focusedLabel.gameObject);

            if (focusedLeader != null) UnityEngine.Object.Destroy(focusedLeader.gameObject);
            if (uiRoot != null) { uiRoot.gameObject.SetActive(false); UnityEngine.Object.Destroy(uiRoot.gameObject); }
        }
    }

    internal sealed class MapPanSurface : Selectable, ISubmitHandler
    {
        internal Action<Vector2> Pan;
        internal Action Return;
        public override void OnMove(AxisEventData data) { Pan?.Invoke(data.moveVector); data.Use(); }
        public void OnSubmit(BaseEventData data) { Return?.Invoke(); data.Use(); }
    }

    internal sealed class MapNavigationSelection : MonoBehaviour, ISelectHandler
    {
        internal Action Selected;
        public void OnSelect(BaseEventData eventData) => Selected?.Invoke();
    }

    internal sealed class MapZoomWheel : MonoBehaviour, IScrollHandler
    {
        internal Action<PointerEventData> Zoom;
        public void OnScroll(PointerEventData eventData) { if (eventData.scrollDelta.y != 0) Zoom?.Invoke(eventData); }
    }
}
