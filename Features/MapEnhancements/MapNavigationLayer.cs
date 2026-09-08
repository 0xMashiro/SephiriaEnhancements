using System.Collections.Generic;
using HarmonyLib;
using SephiriaEnhancements.Configuration;
using SephiriaEnhancements.Integration;
using SephiriaEnhancements.MapEnhancements.Core;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace SephiriaEnhancements.MapEnhancements
{
    // Owns the additions for one open map; native geometry and destinations stay
    // in NativeMapGeometry rather than leaking generator choices into the UI.
    internal sealed class MapNavigationLayer
    {
        private static readonly AccessTools.FieldRef<UI_MapPanel,
            Dictionary<Transform, UI_MinimapElement>> NativeIcons =
            AccessTools.FieldRefAccess<UI_MapPanel,
                Dictionary<Transform, UI_MinimapElement>>("icons");
        private static readonly AccessTools.FieldRef<UnitAI_NewBasic, GameObject> QuestMarker =
            AccessTools.FieldRefAccess<UnitAI_NewBasic, GameObject>("destinyQuestMarker");
        private static readonly AccessTools.FieldRef<UI_MapPanel, Dictionary<PlayerSpawner, RectTransform>> NativePlayers =
            AccessTools.FieldRefAccess<UI_MapPanel, Dictionary<PlayerSpawner, RectTransform>>("viewedPlayerIcons");

        private readonly Dictionary<Transform, MapLocationMarkerView> labels = new();
        private readonly HashSet<Transform> visible = new();
        private readonly List<Transform> removed = new();
        private readonly Dictionary<UI_MinimapElement, (Vector2 Position, Vector3 Scale)> iconPositions = new();
        private readonly Dictionary<RectTransform, Vector2> playerPositions = new();
        private UI_MapPanel panel;
        private UI_Map map;
        private FloorGenerator floor;
        private NativeMapGeometry geometry;
        private NativeRoomTerrain roomTerrain;
        internal bool IsActive => navigator != null;
        private string mapFloorGuid;
        private RectTransform root;
        private Interactable[] interactions;
        private Texture2D terrain;
        private MapNavigator navigator;
        private readonly List<RectTransform> landmarkSymbols = new();
        private TextMeshProUGUI details;
        private TextMeshProUGUI textTemplate;
        private bool originalDefaultGuide;
        private bool originalConsoleGuide;
        private GameObject originalSelectable;
        private bool createdMap;
        private bool generatedContents;
        private bool originalCursor;
        private Vector2 originalSize;
        private Vector2 originalContentsPosition;
        private bool originalGuide;
        private float nextRefreshAt;
        private float nextInteractionScanAt;

        internal void Prepare(UI_MapPanel mapPanel, string floorGuid)
        {
            Clear();
            PlayerAvatar player = LocalPlayerResolver.Resolve();
            if (player == null || player.currentFloorGuid != floorGuid) return;
            foreach (FloorGenerator candidate in FloorGenerator.FloorGenerators)
            {
                if (candidate.guid == floorGuid && candidate.GenerateSuccess &&
                    (candidate is FullyDesignedFloorGenerator || candidate is FixedFloorGenerator ||
                     candidate is EnhancedProceduralFloorGenerator || candidate is LibraryFloorGenerator ||
                     candidate is SingleRoomFloorGenerator))
                {
                    floor = candidate;
                    break;
                }
            }
            if (floor == null) return;
            var designed = floor as FullyDesignedFloorGenerator;
            if (designed != null && (!floor.isSafeFloor || designed.Size.x <= 0 ||
                designed.Size.y <= 0 || designed.mapScale <= 0)) return;
            panel = mapPanel;
            mapFloorGuid = floorGuid;
            if (!panel.maps.TryGetValue(floorGuid, out map) || map == null)
            {
                if (designed == null) { floor = null; panel = null; return; }
                map = new GameObject("Floor Map", typeof(RectTransform),
                    typeof(UI_Map)).GetComponent<UI_Map>();
                panel.AddMap(floorGuid, map);
                createdMap = true;
            }
            geometry = new NativeMapGeometry(floor, map);
            originalSize = map.rectTransform.sizeDelta;
            originalContentsPosition = map.contentsChild.anchoredPosition;
            originalCursor = map.showPlayerCursor;
            originalSelectable = map.defaultSelectable;
            originalDefaultGuide = panel.defaultGuide.activeSelf;
            originalConsoleGuide = panel.consoleGuide.activeSelf;
            originalGuide = panel.clickGuideMessage.gameObject.activeSelf;
            generatedContents = map.rooms.Count == 0 &&
                map.contentsChild.GetComponentsInChildren<Graphic>(true).Length == 0;

            root = new GameObject("Map Locations", typeof(RectTransform),
                typeof(CanvasGroup)).GetComponent<RectTransform>();
            root.SetParent(map.contentsChild, false);
            root.sizeDelta = Vector2.zero;
            CanvasGroup group = root.GetComponent<CanvasGroup>();
            group.interactable = true;
            group.blocksRaycasts = true;
            textTemplate = panel.clickGuideMessage.GetComponent<TextMeshProUGUI>();
            details = new GameObject("Map Location Details", typeof(RectTransform),
                typeof(TextMeshProUGUI)).GetComponent<TextMeshProUGUI>();
            details.rectTransform.SetParent(panel.clickGuideMessage.parent, false);
            details.rectTransform.anchorMin = panel.clickGuideMessage.anchorMin;
            details.rectTransform.anchorMax = panel.clickGuideMessage.anchorMax;
            details.rectTransform.pivot = panel.clickGuideMessage.pivot;
            details.rectTransform.anchoredPosition = panel.clickGuideMessage.anchoredPosition + new Vector2(0f, 14f);
            details.rectTransform.sizeDelta = new Vector2(360f, 24f);
            details.alignment = TextAlignmentOptions.Center;
            details.textWrappingMode = TextWrappingModes.Normal;
            details.overflowMode = TextOverflowModes.Ellipsis;
            details.color = textTemplate.color;
            details.raycastTarget = false;
            NativeLocalizedText.BindFont(details, textTemplate);
            NativeLocalizedText.MatchFontSize(details, textTemplate);
            if (generatedContents && designed != null)
            {
                map.rectTransform.sizeDelta = designed.Size * designed.mapScale;
                map.contentsChild.anchoredPosition =
                    -(designed.Center * designed.mapScale + designed.mapOffset);
                DrawOverview();
            }
            // Position the existing player icons through the same projection as targets.
            map.showPlayerCursor = false;
            roomTerrain = new NativeRoomTerrain(geometry, root, textTemplate.color);
        }

        internal void Show(UI_MapPanel mapPanel, string floorGuid)
        {
            if (panel != mapPanel || floor == null || floor.guid != floorGuid) return;
            HideUnavailableTravelGuide();
            navigator = new MapNavigator(panel, map, geometry, textTemplate, details);
            RefreshIfDue();
            panel.defaultSelectable = navigator.DefaultSelection;
            navigator.FocusCurrentRoom();
        }

        internal void RefreshIfDue()
        {
            if (root == null) return;
            PlayerAvatar player = LocalPlayerResolver.Resolve();
            if (!EnhancementsSettings.Enabled || floor == null || player == null ||
                player.currentFloorGuid != floor.guid)
            {
                Clear();
                return;
            }
            if (panel == null || !panel.IsOpened || map == null ||
                !map.gameObject.activeInHierarchy) return;
            HideUnavailableTravelGuide();
            NativeLocalizedText.MatchFontSize(details, textTemplate);
            roomTerrain?.Refresh();
            if (navigator != null)
            {
                foreach (var symbol in landmarkSymbols) symbol.localScale = Vector3.one / navigator.Scale;
                navigator.Tick();
            }
            PositionNativePlayers();
            PositionNativeIcons();
            if (Time.unscaledTime < nextRefreshAt) return;
            nextRefreshAt = Time.unscaledTime + 0.2f;
            visible.Clear();
            foreach (UnitAI_NewBasic npc in UnitAI_NewBasic.AllInstances)
            {
                if (npc == null || !npc.gameObject.activeInHierarchy || npc.Avatar == null ||
                    npc.Avatar.IsDead || string.IsNullOrEmpty(npc.socialID) ||
                    string.IsNullOrEmpty(npc.Avatar.Name) || !Contains(npc.transform.position)) continue;
                GameObject questMarker = QuestMarker(npc);
                string name = questMarker != null && questMarker.activeInHierarchy
                    ? "! " + npc.Avatar.Name : npc.Avatar.Name;
                Label(npc.transform, name, true, questMarker != null && questMarker.activeInHierarchy);
            }
            if (interactions == null || Time.unscaledTime >= nextInteractionScanAt)
            {
                interactions = Object.FindObjectsByType<Interactable>(FindObjectsSortMode.None);
                nextInteractionScanAt = Time.unscaledTime + 1f;
            }
            foreach (Interactable interaction in interactions)
            {
                if (interaction == null || !interaction.isActiveAndEnabled || interaction.hideGuide ||
                    interaction.interactionType == Interactable.EInteractionType.NoAction ||
                    !Contains(interaction.transform.position) ||
                    interaction.GetComponentInParent<UnitAI_NewBasic>() != null ||
                    !interaction.IsInteractable(player.gameObject)) continue;
                string text = NativeMapFacilities.Name(interaction);
                if (!string.IsNullOrWhiteSpace(text)) Label(interaction.transform, text);
            }
            removed.Clear();
            foreach (var pair in labels)
                if (pair.Key == null || !visible.Contains(pair.Key)) removed.Add(pair.Key);
            foreach (Transform key in removed)
            {
                if (labels[key] != null) Object.Destroy(labels[key].gameObject);
                labels.Remove(key);
            }
            navigator?.Sync(labels.Values);
        }

        private void Label(Transform target, string text, bool person = false, bool quest = false)
        {
            visible.Add(target);
            if (!labels.TryGetValue(target, out MapLocationMarkerView marker))
            {
                marker = MapLocationMarkerView.Create(root, textTemplate);
                labels.Add(target, marker);
            }
            marker.Set(text, Project(target.position), target, person, quest);
            if (generatedContents && map.defaultSelectable == null)
                map.defaultSelectable = marker.gameObject;
        }

        private void PositionNativeIcons()
        {
            foreach (var pair in NativeIcons(panel))
            {
                if (pair.Key == null || pair.Value == null) continue;
                if (!iconPositions.ContainsKey(pair.Value))
                    iconPositions.Add(pair.Value, (pair.Value.rectTransform.anchoredPosition,
                        pair.Value.rectTransform.localScale));
                float size = Mathf.Max(pair.Value.rectTransform.sizeDelta.x,
                    pair.Value.rectTransform.sizeDelta.y);
                pair.Value.rectTransform.localScale = Vector3.one * (size > 0 ? Mathf.Min(1f, 12f / size) : 1f);
                Vector2 point = new Vector2(-9999f, -9999f);
                if (pair.Key.gameObject.activeInHierarchy && Contains(pair.Key.position))
                {
                    Vector3 worldPoint = map.contentsChild.TransformPoint(Project(pair.Key.position));
                    point = panel.contentsParent.InverseTransformPoint(worldPoint);
                }
                pair.Value.rectTransform.anchoredPosition = point;
                pair.Value.rectTransform.SetAsLastSibling();
            }
        }

        private void PositionNativePlayers()
        {
            foreach (var pair in NativePlayers(panel))
            {
                if (pair.Key == null || pair.Value == null) continue;
                if (!playerPositions.ContainsKey(pair.Value))
                    playerPositions.Add(pair.Value, pair.Value.anchoredPosition);
                Vector3 position = pair.Key.transform.position;
                pair.Value.anchoredPosition = Contains(position)
                    ? (Vector2)panel.contentsParent.InverseTransformPoint(map.contentsChild.TransformPoint(Project(position)))
                    : new Vector2(-9999, -9999);
                pair.Value.SetAsLastSibling();
            }
        }

        private void HideUnavailableTravelGuide()
        {
            // Native Update re-enables the input-specific guide every frame.
            panel.clickGuideMessage.gameObject.SetActive(false);
            panel.defaultGuide.SetActive(false);
            panel.consoleGuide.SetActive(false);
        }

        private void DrawOverview()
        {
            var floor = geometry.Designed;
            int width = Mathf.CeilToInt(floor.Size.x * 2);
            int height = Mathf.CeilToInt(floor.Size.y * 2);
            bool[] blocked = new bool[width * height];
            for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                {
                    Vector2 position = (Vector2)floor.transform.position + floor.bottomLeft +
                        new Vector2((x + 0.5f) * floor.Size.x / width,
                            (y + 0.5f) * floor.Size.y / height);
                    blocked[y * width + x] = Physics2D.OverlapPoint(position,
                        CombatManager.PathfindingObstacleLayerMask);
                }
            Color[] pixels = new Color[blocked.Length];
            for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                {
                    if (!blocked[y * width + x]) continue;
                    bool edge = FixedFloorMapOutline.IsEdge(blocked, width, height, x, y);
                    Color ink = textTemplate.color;
                    ink.a *= edge ? 0.45f : 0.06f;
                    pixels[y * width + x] = ink;
                }
            terrain = new Texture2D(width, height, TextureFormat.RGBA32, false);
            terrain.filterMode = FilterMode.Point;
            terrain.SetPixels(pixels);
            terrain.Apply();
            RawImage ground = new GameObject("Map Outline", typeof(RectTransform),
                typeof(RawImage)).GetComponent<RawImage>();
            ground.rectTransform.SetParent(root, false);
            ground.rectTransform.anchoredPosition = floor.Center * floor.mapScale + floor.mapOffset;
            ground.rectTransform.sizeDelta = floor.Size * floor.mapScale;
            ground.texture = terrain;
            ground.raycastTarget = false;

            // Use native map symbols at their actual target positions, without
            // copying the authored layout or its teleport button behaviours.
            foreach (var connection in floor.mapTeleportConnections)
            {
                if (connection.icon == null || connection.point == null ||
                    !connection.point.gameObject.activeInHierarchy ||
                    !Contains(connection.point.transform.position)) continue;
                Image source = connection.icon.GetComponent<Image>();
                if (source == null || source.sprite == null) continue;
                Image symbol = new GameObject("Map Landmark", typeof(RectTransform),
                    typeof(Image)).GetComponent<Image>();
                symbol.rectTransform.SetParent(root, false);
                symbol.rectTransform.anchoredPosition = Project(connection.point.transform.position);
                Vector2 size = source.sprite.rect.size;
                symbol.rectTransform.sizeDelta = size * (16f / Mathf.Max(size.x, size.y));
                symbol.sprite = source.sprite;
                symbol.color = source.color;
                symbol.raycastTarget = false;
                landmarkSymbols.Add(symbol.rectTransform);
            }
        }

        private Vector2 Project(Vector3 position) => geometry.Project(position);

        private bool Contains(Vector3 position) => geometry.Contains(position);

        internal void Clear()
        {
            roomTerrain?.Clear();
            roomTerrain = null;
            navigator?.Clear();
            navigator = null;
            landmarkSymbols.Clear();
            foreach (var pair in iconPositions)
                if (pair.Key != null)
                {
                    pair.Key.rectTransform.anchoredPosition = pair.Value.Position;
                    pair.Key.rectTransform.localScale = pair.Value.Scale;
                }
            iconPositions.Clear();
            foreach (var pair in playerPositions)
                if (pair.Key != null) pair.Key.anchoredPosition = pair.Value;
            playerPositions.Clear();
            labels.Clear();
            visible.Clear();
            removed.Clear();
            if (root != null)
            {
                root.gameObject.SetActive(false);
                root.SetParent(null);
                Object.Destroy(root.gameObject);
            }
            if (map != null)
            {
                map.showPlayerCursor = originalCursor;
                map.defaultSelectable = originalSelectable;
                map.rectTransform.sizeDelta = originalSize;
                map.contentsChild.anchoredPosition = originalContentsPosition;
                if (createdMap)
                {
                    if (panel != null &&
                        panel.maps.TryGetValue(mapFloorGuid, out UI_Map registered) && registered == map)
                        panel.maps.Remove(mapFloorGuid);
                    Object.Destroy(map.gameObject);
                }
            }
            if (panel != null)
            {
                panel.defaultSelectable = panel.IsOpened ? originalSelectable : null;
                panel.clickGuideMessage.gameObject.SetActive(originalGuide);
                panel.defaultGuide.SetActive(originalDefaultGuide);
                panel.consoleGuide.SetActive(originalConsoleGuide);
            }
            if (details != null) Object.Destroy(details.gameObject);
            details = null;
            textTemplate = null;
            root = null;
            if (terrain != null) Object.Destroy(terrain);
            terrain = null;
            map = null;
            panel = null;
            floor = null;
            geometry = null;
            mapFloorGuid = null;
            createdMap = false;
            generatedContents = false;
            interactions = null;
            nextRefreshAt = 0f;
            nextInteractionScanAt = 0f;
        }
    }
}
