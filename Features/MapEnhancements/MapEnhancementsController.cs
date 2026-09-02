using HarmonyLib;
using SephiriaEnhancements.Configuration;
using SephiriaEnhancements.Integration;
using SephiriaEnhancements.KeyboardUiNavigation;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

namespace SephiriaEnhancements.MapEnhancements
{
    internal sealed class MapEnhancementsController : MonoBehaviour
    {
        private const float OverlayWidthRatio = 0.7f;
        private const float OverlayHeightRatio = 0.72f;
        private const float OverlayAlpha = 0.48f;

        private static readonly FieldInfo EnhancedHiddenRoomsField = AccessTools.Field(
            typeof(EnhancedProceduralFloorGenerator), "hiddenRoomInstances");
        private static readonly FieldInfo LibraryHiddenRoomsField = AccessTools.Field(
            typeof(LibraryFloorGenerator), "hiddenRoomInstances");

        private static MapEnhancementsController current;

        private readonly List<HiddenRoomMapMarker> markers =
            new List<HiddenRoomMapMarker>();
        private readonly TownNpcMapMarkerLayer townNpcMapMarkers =
            new TownNpcMapMarkerLayer();
        private bool currentFloorMapOverlayCompatible = true;
        private bool hiddenRoomMapCompatible = true;
        private bool townNpcMapMarkersCompatible = true;
        private bool currentFloorMapOverlayVisible;
        private RectTransform currentFloorMapOverlayRoot;
        private UI_Map currentFloorMap;
        private UI_MapPanelPlayerIcon currentFloorMapPlayerIcon;
        private Transform currentFloorMapOriginalParent;
        private int currentFloorMapOriginalSiblingIndex;
        private Vector2 currentFloorMapOriginalAnchorMin;
        private Vector2 currentFloorMapOriginalAnchorMax;
        private Vector2 currentFloorMapOriginalPivot;
        private Vector2 currentFloorMapOriginalAnchoredPosition;
        private Vector2 currentFloorMapOriginalSizeDelta;
        private Vector3 currentFloorMapOriginalLocalScale;
        private Quaternion currentFloorMapOriginalLocalRotation;
        private bool currentFloorMapWasActive;
        private bool nativeMapPanelOpen;
        private float nextCurrentFloorMapRefreshAt;
        private bool wasEnabled = true;

        private void Awake()
        {
            current = this;
        }

        private void Update()
        {
            bool enabled = EnhancementsSettings.Enabled;
            if (!enabled)
            {
                if (wasEnabled)
                {
                    ClearMarkers();
                    townNpcMapMarkers.Clear();
                    currentFloorMapOverlayVisible = false;
                    RestoreCurrentFloorMapOverlay();
                }
                wasEnabled = false;
                return;
            }
            wasEnabled = true;

            townNpcMapMarkers.RefreshIfDue();

            if (!currentFloorMapOverlayCompatible)
            {
                return;
            }

            bool nativeMenuOpen = UIManager.Instance?.CurrentControlStack != null;
            if (currentFloorMapOverlayVisible && !nativeMapPanelOpen &&
                !nativeMenuOpen &&
                Time.unscaledTime >= nextCurrentFloorMapRefreshAt)
            {
                nextCurrentFloorMapRefreshAt = Time.unscaledTime + 0.25f;
                TryRefreshCurrentFloorMapOverlay();
            }

            if (currentFloorMapOverlayRoot != null &&
                (nativeMapPanelOpen || nativeMenuOpen))
            {
                RestoreCurrentFloorMapOverlay();
            }
            else if (currentFloorMapOverlayRoot != null)
            {
                SyncCurrentFloorMapOverlayRoot();
            }

            PlayerInputController input = PlayerInputController.Instance;
            NativeControlCoordinator.PreparePlayerInput(input);
            if (!NativeInputActions.WasPressed(input?.playerInput?.actions,
                    ModShortcuts.ToggleCurrentFloorMapOverlay,
                    rejectKeyboardModifiers: true) ||
                !CanUseGameplayShortcut())
            {
                return;
            }

            try
            {
                ToggleCurrentFloorMapOverlay();
            }
            catch (Exception ex)
            {
                currentFloorMapOverlayVisible = false;
                RestoreCurrentFloorMapOverlay();
                currentFloorMapOverlayCompatible = false;
                Debug.LogWarning("[SephiriaEnhancements] Current-floor map overlay " +
                    "shortcut disabled until the Mod is reloaded: " + ex.Message);
            }
        }

        private void OnDestroy()
        {
            townNpcMapMarkers.Clear();
            ClearMarkers();
            RestoreCurrentFloorMapOverlay();

            if (current == this)
            {
                current = null;
            }
        }

        internal void ResetGameplayContext()
        {
            townNpcMapMarkers.Clear();
            RestoreCurrentFloorMapOverlay();
            ClearMarkers();
            wasEnabled = EnhancementsSettings.Enabled;
        }

        private void ClearMarkers()
        {
            for (int index = markers.Count - 1; index >= 0; index--)
            {
                HiddenRoomMapMarker marker = markers[index];
                if (marker != null)
                {
                    marker.RestoreNativeState();
                    Destroy(marker);
                }
            }
            markers.Clear();
        }

        internal static void ShowHiddenRooms(UI_MapPanel panel, string floorGuid)
        {
            if (current == null || !current.hiddenRoomMapCompatible ||
                !EnhancementsSettings.Enabled || panel == null ||
                string.IsNullOrEmpty(floorGuid))
            {
                return;
            }

            try
            {
                current.ShowHiddenRoomsInner(panel, floorGuid);
            }
            catch (Exception ex)
            {
                current.hiddenRoomMapCompatible = false;
                Debug.LogWarning("[SephiriaEnhancements] Hidden-room map display disabled " +
                    "until the Mod is reloaded: " + ex.Message);
            }
        }

        internal static void InitializeKeyboardRoomNavigation(UI_MapPanel panel,
            string floorGuid)
        {
            if (current == null || !EnhancementsSettings.Enabled || panel == null)
            {
                return;
            }

            // UI_MapPanel.Open enables the control stack before Show assigns the
            // current room to defaultSelectable. Let that frame finish before
            // seeding keyboard focus so the native UI lifecycle cannot clear it.
            KeyboardUiNavigationController.RequestSelection(panel,
                FindKeyboardRoomNavigationStart(panel, floorGuid));
        }

        internal static void ShowTownNpcMapMarkers(UI_MapPanel panel,
            string floorGuid)
        {
            if (current == null || !current.townNpcMapMarkersCompatible ||
                !EnhancementsSettings.Enabled || panel == null)
            {
                current?.townNpcMapMarkers.Clear();
                return;
            }

            try
            {
                current.townNpcMapMarkers.Show(panel, floorGuid);
            }
            catch (Exception ex)
            {
                current.townNpcMapMarkers.Clear();
                current.townNpcMapMarkersCompatible = false;
                Debug.LogWarning("[SephiriaEnhancements] Town NPC map markers " +
                    "disabled until the Mod is reloaded: " + ex.Message);
            }
        }

        private static GameObject FindKeyboardRoomNavigationStart(
            UI_MapPanel panel, string floorGuid)
        {
            if (string.IsNullOrEmpty(floorGuid) ||
                !panel.maps.TryGetValue(floorGuid, out UI_Map map) || map == null)
            {
                return panel.defaultSelectable;
            }

            PlayerAvatar player = CombatManager.Instance?.CurrentPlayer ??
                GameCamera.Instance?.Observer;
            if (player == null)
            {
                return panel.defaultSelectable;
            }

            Vector2 playerPosition = player.transform.position;
            UI_Map_Room nearestRoom = null;
            float nearestDistance = float.PositiveInfinity;
            foreach (UI_Map_Room room in map.rooms)
            {
                if (!TryGetActiveRoomSelectable(room, out GameObject selectable))
                {
                    continue;
                }

                if (playerPosition.x >= room.bottomLeft.x &&
                    playerPosition.x <= room.topRight.x &&
                    playerPosition.y >= room.bottomLeft.y &&
                    playerPosition.y <= room.topRight.y)
                {
                    return selectable;
                }

                if (!IsTeleportableMapRoom(room))
                {
                    continue;
                }

                float horizontalDistance = Mathf.Max(room.bottomLeft.x -
                    playerPosition.x, 0f, playerPosition.x - room.topRight.x);
                float verticalDistance = Mathf.Max(room.bottomLeft.y -
                    playerPosition.y, 0f, playerPosition.y - room.topRight.y);
                float distance = horizontalDistance * horizontalDistance +
                    verticalDistance * verticalDistance;
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearestRoom = room;
                }
            }

            return nearestRoom != null
                ? nearestRoom.GetSelectable()
                : panel.defaultSelectable;
        }

        private static bool TryGetActiveRoomSelectable(UI_Map_Room room,
            out GameObject selectable)
        {
            if (room == null)
            {
                selectable = null;
                return false;
            }

            selectable = room.GetSelectable();
            if (selectable == null || !selectable.activeInHierarchy)
            {
                return false;
            }

            Selectable nativeSelectable = selectable.GetComponent<Selectable>();
            return nativeSelectable == null || nativeSelectable.IsInteractable();
        }

        private static bool IsTeleportableMapRoom(UI_Map_Room room) =>
            room is UI_Map_EnhancedProceduralDungeonRoom_Room ||
            room is UI_Map_LibraryProceduralDungeonRoom;

        internal static void BeforeNativeMapOpened()
        {
            if (current == null)
            {
                return;
            }

            current.nativeMapPanelOpen = true;
            current.townNpcMapMarkers.Clear();
            current.RestoreCurrentFloorMapOverlay();
        }

        internal static void AfterNativeMapClosed()
        {
            if (current == null)
            {
                return;
            }

            current.nativeMapPanelOpen = false;
            current.townNpcMapMarkers.Clear();
            if (EnhancementsSettings.Enabled &&
                current.currentFloorMapOverlayVisible)
            {
                current.TryRefreshCurrentFloorMapOverlay();
            }
        }

        internal void RemoveMarker(HiddenRoomMapMarker marker)
        {
            markers.Remove(marker);
        }

        private static bool CanUseGameplayShortcut()
        {
            PlayerInputController input = PlayerInputController.Instance;
            UIManager manager = UIManager.Instance;
            if (input?.playerInput == null || manager == null ||
                (input.playerInput.currentControlScheme !=
                    PlayerInputController.KeyboardAndMouseScheme &&
                 input.playerInput.currentControlScheme !=
                    PlayerInputController.GamepadScheme) ||
                manager.CurrentControlStack != null)
            {
                return false;
            }

            PlayerAvatar player = CombatManager.Instance?.CurrentPlayer ??
                GameCamera.Instance?.Observer;
            if (player == null || !LocalPlayerResolver.IsLocal(player))
            {
                return false;
            }

            ScreenFader fader = ScreenFader.Instance;
            return fader == null ||
                (fader.FadingState == ScreenFader.EFadingState.None &&
                 fader.currentLoadingScreenType == -1);
        }

        private void ToggleCurrentFloorMapOverlay()
        {
            currentFloorMapOverlayVisible = !currentFloorMapOverlayVisible;
            if (currentFloorMapOverlayVisible)
            {
                TryRefreshCurrentFloorMapOverlay();
            }
            else
            {
                RestoreCurrentFloorMapOverlay();
            }
        }

        private void TryRefreshCurrentFloorMapOverlay()
        {
            try
            {
                RefreshCurrentFloorMapOverlay();
            }
            catch (Exception ex)
            {
                currentFloorMapOverlayVisible = false;
                RestoreCurrentFloorMapOverlay();
                Debug.LogWarning("[SephiriaEnhancements] Current-floor map overlay " +
                    "could not be displayed: " + ex.Message);
            }
        }

        private void RefreshCurrentFloorMapOverlay()
        {
            if (nativeMapPanelOpen || UIManager.Instance == null)
            {
                return;
            }

            PlayerAvatar player = CombatManager.Instance?.CurrentPlayer ??
                GameCamera.Instance?.Observer;
            UI_MapPanel panel = UIManager.Instance.GetElement<UI_MapPanel>();
            if (player == null || panel == null ||
                string.IsNullOrEmpty(player.currentFloorGuid) ||
                !panel.maps.TryGetValue(player.currentFloorGuid, out UI_Map map) ||
                map == null)
            {
                return;
            }

            if (currentFloorMap != map)
            {
                RestoreCurrentFloorMapOverlay();
                CreateCurrentFloorMapOverlayRoot(panel);
                AttachCurrentFloorMap(map, panel);
                ShowHiddenRoomsInner(panel, player.currentFloorGuid);
            }

            SyncCurrentFloorMapOverlayRoot();

            RevealDiscoveredRooms(map, player.currentFloorGuid);
            PositionCurrentFloorMapPlayer(map, player);
            FitCurrentFloorMap(map);
        }

        private void CreateCurrentFloorMapOverlayRoot(UI_MapPanel panel)
        {
            if (currentFloorMapOverlayRoot != null)
            {
                return;
            }

            Canvas canvas = panel.GetComponentInParent<Canvas>();
            RectTransform overlayParent = canvas?.transform as RectTransform;
            if (overlayParent == null)
            {
                throw new InvalidOperationException(
                    "The native map panel is not attached to a UI canvas.");
            }

            GameObject root = new GameObject("Sephiria Current Floor Map Overlay",
                typeof(RectTransform), typeof(CanvasGroup));
            currentFloorMapOverlayRoot = root.GetComponent<RectTransform>();
            currentFloorMapOverlayRoot.SetParent(overlayParent, false);
            currentFloorMapOverlayRoot.SetAsFirstSibling();
            CanvasGroup overlayCanvasGroup = root.GetComponent<CanvasGroup>();
            overlayCanvasGroup.alpha = OverlayAlpha;
            overlayCanvasGroup.interactable = false;
            overlayCanvasGroup.blocksRaycasts = false;
            SyncCurrentFloorMapOverlayRoot();
        }

        private void SyncCurrentFloorMapOverlayRoot()
        {
            if (currentFloorMapOverlayRoot == null ||
                !(currentFloorMapOverlayRoot.parent is RectTransform overlayParent))
            {
                return;
            }

            currentFloorMapOverlayRoot.anchorMin = new Vector2(0.5f, 0.5f);
            currentFloorMapOverlayRoot.anchorMax = new Vector2(0.5f, 0.5f);
            currentFloorMapOverlayRoot.pivot = new Vector2(0.5f, 0.5f);
            currentFloorMapOverlayRoot.anchoredPosition = Vector2.zero;
            currentFloorMapOverlayRoot.sizeDelta = new Vector2(
                overlayParent.rect.width * OverlayWidthRatio,
                overlayParent.rect.height * OverlayHeightRatio);
            currentFloorMapOverlayRoot.localScale = Vector3.one;
            currentFloorMapOverlayRoot.localRotation = Quaternion.identity;
        }

        private void AttachCurrentFloorMap(UI_Map map, UI_MapPanel panel)
        {
            RectTransform mapTransform = map.rectTransform;
            currentFloorMap = map;
            currentFloorMapOriginalParent = mapTransform.parent;
            currentFloorMapOriginalSiblingIndex = mapTransform.GetSiblingIndex();
            currentFloorMapOriginalAnchorMin = mapTransform.anchorMin;
            currentFloorMapOriginalAnchorMax = mapTransform.anchorMax;
            currentFloorMapOriginalPivot = mapTransform.pivot;
            currentFloorMapOriginalAnchoredPosition = mapTransform.anchoredPosition;
            currentFloorMapOriginalSizeDelta = mapTransform.sizeDelta;
            currentFloorMapOriginalLocalScale = mapTransform.localScale;
            currentFloorMapOriginalLocalRotation = mapTransform.localRotation;
            currentFloorMapWasActive = map.gameObject.activeSelf;

            mapTransform.SetParent(currentFloorMapOverlayRoot, false);
            mapTransform.anchorMin = new Vector2(0.5f, 0.5f);
            mapTransform.anchorMax = new Vector2(0.5f, 0.5f);
            mapTransform.pivot = new Vector2(0.5f, 0.5f);
            mapTransform.anchoredPosition = Vector2.zero;
            mapTransform.localRotation = Quaternion.identity;
            map.gameObject.SetActive(true);

            if (map.showPlayerCursor && panel.playerIconPrefab != null)
            {
                currentFloorMapPlayerIcon = Instantiate(panel.playerIconPrefab, mapTransform);
                currentFloorMapPlayerIcon.gameObject.SetActive(true);
                currentFloorMapPlayerIcon.rectTransform.SetAsLastSibling();
                CanvasGroup playerIconCanvasGroup =
                    currentFloorMapPlayerIcon.gameObject.GetComponent<CanvasGroup>() ??
                    currentFloorMapPlayerIcon.gameObject.AddComponent<CanvasGroup>();
                playerIconCanvasGroup.alpha = 1f;
                playerIconCanvasGroup.ignoreParentGroups = true;
                playerIconCanvasGroup.interactable = false;
                playerIconCanvasGroup.blocksRaycasts = false;
                if (PlayerSpawner.MultiplayerList.Count > 1)
                {
                    foreach (PlayerSpawner spawner in PlayerSpawner.MultiplayerList)
                    {
                        if (spawner != null && spawner.isOwned)
                        {
                            currentFloorMapPlayerIcon.SetPlayerIdx(spawner.currentPlayerIdx);
                            break;
                        }
                    }
                }
            }
        }

        private static void RevealDiscoveredRooms(UI_Map map, string floorGuid)
        {
            FloorGenerator generator = FindGenerator(floorGuid);
            if (generator == null)
            {
                return;
            }

            foreach (UI_Map_Room room in map.rooms)
            {
                if (room == null || room.gameObject.activeSelf)
                {
                    continue;
                }

                foreach (KeyValuePair<Vector2, bool> area in generator.revealedAreas)
                {
                    if (area.Key.x >= room.bottomLeft.x && area.Key.x <= room.topRight.x &&
                        area.Key.y >= room.bottomLeft.y && area.Key.y <= room.topRight.y)
                    {
                        room.gameObject.SetActive(true);
                        break;
                    }
                }
            }
        }

        private void PositionCurrentFloorMapPlayer(UI_Map map, PlayerAvatar player)
        {
            if (currentFloorMapPlayerIcon == null)
            {
                return;
            }

            currentFloorMapPlayerIcon.gameObject.SetActive(false);
            Vector3 position = player.transform.position;
            foreach (UI_Map_Room room in map.rooms)
            {
                if (room != null && position.x >= room.bottomLeft.x &&
                    position.x <= room.topRight.x && position.y >= room.bottomLeft.y &&
                    position.y <= room.topRight.y)
                {
                    currentFloorMapPlayerIcon.rectTransform.anchoredPosition =
                        map.contentsChild.anchoredPosition +
                        room.GetIconCenterAnchoredPosition();
                    currentFloorMapPlayerIcon.gameObject.SetActive(true);
                    break;
                }
            }
        }

        private void FitCurrentFloorMap(UI_Map map)
        {
            if (currentFloorMapOverlayRoot == null)
            {
                return;
            }

            Vector2 available = currentFloorMapOverlayRoot.rect.size * 0.9f;
            if (!TryGetCurrentFloorMapBounds(map, out Rect mapBounds))
            {
                return;
            }

            Vector2 mapSize = mapBounds.size;
            if (available.x <= 0f || available.y <= 0f ||
                mapSize.x <= 0f || mapSize.y <= 0f)
            {
                return;
            }

            float scale = Mathf.Min(available.x / mapSize.x,
                available.y / mapSize.y);
            RectTransform mapTransform = map.rectTransform;
            mapTransform.localScale = new Vector3(scale, scale, 1f);
            Vector2 contentCenter = map.contentsChild.anchoredPosition +
                mapBounds.center;
            mapTransform.anchoredPosition = -contentCenter * scale;
        }

        private static bool TryGetCurrentFloorMapBounds(UI_Map map,
            out Rect bounds)
        {
            Vector2 minimum = new Vector2(float.PositiveInfinity,
                float.PositiveInfinity);
            Vector2 maximum = new Vector2(float.NegativeInfinity,
                float.NegativeInfinity);
            bool hasRoom = false;

            foreach (UI_Map_Room room in map.rooms)
            {
                if (room == null)
                {
                    continue;
                }

                Vector2 halfSize = room.GetRoomIconSize() * 0.5f;
                Vector2 center = room.GetIconCenterAnchoredPosition();
                minimum = Vector2.Min(minimum, center - halfSize);
                maximum = Vector2.Max(maximum, center + halfSize);
                hasRoom = true;
            }

            bounds = hasRoom
                ? Rect.MinMaxRect(minimum.x, minimum.y, maximum.x, maximum.y)
                : default;
            return hasRoom;
        }

        private void RestoreCurrentFloorMapOverlay()
        {
            if (currentFloorMapPlayerIcon != null)
            {
                Destroy(currentFloorMapPlayerIcon.gameObject);
                currentFloorMapPlayerIcon = null;
            }

            if (currentFloorMap != null && currentFloorMapOriginalParent != null)
            {
                RectTransform mapTransform = currentFloorMap.rectTransform;
                mapTransform.SetParent(currentFloorMapOriginalParent, false);
                mapTransform.SetSiblingIndex(Mathf.Clamp(currentFloorMapOriginalSiblingIndex,
                    0, currentFloorMapOriginalParent.childCount - 1));
                mapTransform.anchorMin = currentFloorMapOriginalAnchorMin;
                mapTransform.anchorMax = currentFloorMapOriginalAnchorMax;
                mapTransform.pivot = currentFloorMapOriginalPivot;
                mapTransform.anchoredPosition = currentFloorMapOriginalAnchoredPosition;
                mapTransform.sizeDelta = currentFloorMapOriginalSizeDelta;
                mapTransform.localScale = currentFloorMapOriginalLocalScale;
                mapTransform.localRotation = currentFloorMapOriginalLocalRotation;
                currentFloorMap.gameObject.SetActive(currentFloorMapWasActive);
            }

            currentFloorMap = null;
            currentFloorMapOriginalParent = null;
            if (currentFloorMapOverlayRoot != null)
            {
                Destroy(currentFloorMapOverlayRoot.gameObject);
                currentFloorMapOverlayRoot = null;
            }
        }

        private void ShowHiddenRoomsInner(UI_MapPanel panel, string floorGuid)
        {
            if (!panel.maps.TryGetValue(floorGuid, out UI_Map map) || map == null)
            {
                return;
            }

            FloorGenerator generator = FindGenerator(floorGuid);
            if (generator == null)
            {
                return;
            }

            IList hiddenRooms = GetHiddenRooms(generator);
            if (hiddenRooms == null || hiddenRooms.Count == 0)
            {
                return;
            }

            foreach (UI_Map_Room mapRoom in map.rooms)
            {
                object roomData = GetRoomData(mapRoom);
                if (roomData == null || !hiddenRooms.Contains(roomData))
                {
                    continue;
                }

                mapRoom.gameObject.SetActive(true);
                HiddenRoomMapMarker marker =
                    mapRoom.GetComponent<HiddenRoomMapMarker>();
                if (marker == null)
                {
                    marker = mapRoom.gameObject.AddComponent<HiddenRoomMapMarker>();
                    markers.Add(marker);
                }
                marker.Configure(this, generator, mapRoom);
            }
        }

        private static FloorGenerator FindGenerator(string floorGuid)
        {
            foreach (FloorGenerator generator in FloorGenerator.FloorGenerators)
            {
                if (generator != null && generator.guid == floorGuid)
                {
                    return generator;
                }
            }
            return null;
        }

        private static IList GetHiddenRooms(FloorGenerator generator)
        {
            FieldInfo field = generator is EnhancedProceduralFloorGenerator
                ? EnhancedHiddenRoomsField
                : generator is LibraryFloorGenerator
                    ? LibraryHiddenRoomsField
                    : null;
            return field?.GetValue(generator) as IList;
        }

        private static object GetRoomData(UI_Map_Room room)
        {
            if (room is UI_Map_EnhancedProceduralDungeonRoom enhancedRoom)
            {
                return enhancedRoom.room;
            }
            if (room is UI_Map_LibraryProceduralDungeonRoom libraryRoom)
            {
                return libraryRoom.Room;
            }
            return null;
        }
    }

    internal sealed class HiddenRoomMapMarker : MonoBehaviour
    {
        private MapEnhancementsController owner;
        private FloorGenerator generator;
        private UI_Map_Room room;
        private CanvasGroup inputGuard;
        private bool ownedInputGuard;
        private bool originalBlocksRaycasts;
        private bool originalInteractable;
        private float nextRevealCheckAt;
        private bool configured;

        internal void Configure(MapEnhancementsController markerOwner,
            FloorGenerator floorGenerator, UI_Map_Room mapRoom)
        {
            owner = markerOwner;
            generator = floorGenerator;
            room = mapRoom;
            if (IsRevealed())
            {
                RestoreInteraction();
                owner?.RemoveMarker(this);
                Destroy(this);
                return;
            }

            if (!configured)
            {
                GameObject selectable = room.GetSelectable();
                if (selectable != null)
                {
                    inputGuard = selectable.GetComponent<CanvasGroup>();
                    if (inputGuard == null)
                    {
                        inputGuard = selectable.AddComponent<CanvasGroup>();
                        ownedInputGuard = true;
                    }
                    originalBlocksRaycasts = inputGuard.blocksRaycasts;
                    originalInteractable = inputGuard.interactable;
                    inputGuard.blocksRaycasts = false;
                    inputGuard.interactable = false;
                }
                configured = true;
            }
        }

        private void Update()
        {
            if (Time.unscaledTime < nextRevealCheckAt)
            {
                return;
            }
            nextRevealCheckAt = Time.unscaledTime + 0.25f;

            if (IsRevealed())
            {
                RestoreInteraction();
                owner?.RemoveMarker(this);
                Destroy(this);
            }
        }

        internal void RestoreNativeState()
        {
            bool revealed = IsRevealed();
            RestoreInteraction();
            if (!revealed && room != null)
            {
                room.gameObject.SetActive(false);
            }
        }

        private bool IsRevealed()
        {
            if (generator == null || room == null)
            {
                return true;
            }

            foreach (KeyValuePair<Vector2, bool> area in generator.revealedAreas)
            {
                if (area.Key.x >= room.bottomLeft.x && area.Key.x <= room.topRight.x &&
                    area.Key.y >= room.bottomLeft.y && area.Key.y <= room.topRight.y)
                {
                    return true;
                }
            }
            return false;
        }

        private void RestoreInteraction()
        {
            if (inputGuard == null)
            {
                return;
            }

            inputGuard.blocksRaycasts = originalBlocksRaycasts;
            inputGuard.interactable = originalInteractable;
            if (ownedInputGuard)
            {
                Destroy(inputGuard);
            }
            inputGuard = null;
        }
    }
}
