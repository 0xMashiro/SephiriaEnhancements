using System.Collections.Generic;
using HarmonyLib;
using SephiriaEnhancements.Configuration;
using SephiriaEnhancements.Integration;
using SephiriaEnhancements.MapEnhancements.Core;
using UnityEngine;
using UnityEngine.UI;

namespace SephiriaEnhancements.MapEnhancements
{
    // Native API boundary: fixed-layout floors use world coordinates; the game's
    // ordinary map icon layout only places icons inside UI_Map_Room instances.
    internal sealed class FixedFloorMapLayer
    {
        private static readonly AccessTools.FieldRef<UI_MapPanel,
            Dictionary<Transform, UI_MinimapElement>> NativeIcons =
            AccessTools.FieldRefAccess<UI_MapPanel,
                Dictionary<Transform, UI_MinimapElement>>("icons");
        private static readonly AccessTools.FieldRef<UnitAI_NewBasic, GameObject> QuestMarker =
            AccessTools.FieldRefAccess<UnitAI_NewBasic, GameObject>("destinyQuestMarker");

        private readonly Dictionary<Transform, MapLocationMarkerView> labels = new();
        private readonly HashSet<Transform> visible = new();
        private readonly List<Transform> removed = new();
        private readonly Dictionary<UI_MinimapElement, Vector2> iconPositions = new();
        private UI_MapPanel panel;
        private UI_Map map;
        private FullyDesignedFloorGenerator floor;
        private string mapFloorGuid;
        private RectTransform root;
        private UI_MapPanelPlayerIcon playerIcon;
        private Interactable[] interactions;
        private Texture2D terrain;
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
                if (candidate is FullyDesignedFloorGenerator fixedFloor &&
                    candidate.guid == floorGuid && candidate.GenerateSuccess &&
                    candidate.isSafeFloor)
                {
                    floor = fixedFloor;
                    break;
                }
            }
            if (floor == null || floor.Size.x <= 0 || floor.Size.y <= 0 ||
                floor.mapScale <= 0) return;
            panel = mapPanel;
            mapFloorGuid = floorGuid;
            if (!panel.maps.TryGetValue(floorGuid, out map) || map == null)
            {
                map = new GameObject("Fixed Floor Map", typeof(RectTransform),
                    typeof(UI_Map)).GetComponent<UI_Map>();
                panel.AddMap(floorGuid, map);
                createdMap = true;
            }
            originalSize = map.rectTransform.sizeDelta;
            originalContentsPosition = map.contentsChild.anchoredPosition;
            originalCursor = map.showPlayerCursor;
            originalGuide = panel.clickGuideMessage.gameObject.activeSelf;
            generatedContents = map.rooms.Count == 0 &&
                map.contentsChild.GetComponentsInChildren<Graphic>(true).Length == 0;

            root = new GameObject("Fixed Floor Map Locations", typeof(RectTransform),
                typeof(CanvasGroup)).GetComponent<RectTransform>();
            root.SetParent(map.contentsChild, false);
            root.sizeDelta = Vector2.zero;
            CanvasGroup group = root.GetComponent<CanvasGroup>();
            group.interactable = false;
            group.blocksRaycasts = false;
            if (generatedContents)
            {
                map.rectTransform.sizeDelta = floor.Size * floor.mapScale;
                map.contentsChild.anchoredPosition =
                    -(floor.Center * floor.mapScale + floor.mapOffset);
                DrawLandmarks();
            }
            // Room-based native cursors otherwise remain at their default origin.
            map.showPlayerCursor = false;
            playerIcon = Object.Instantiate(panel.playerIconPrefab, root);
            playerIcon.gameObject.SetActive(true);
        }

        internal void Show(UI_MapPanel mapPanel, string floorGuid)
        {
            if (panel != mapPanel || floor == null || floor.guid != floorGuid) return;
            if (generatedContents) panel.clickGuideMessage.gameObject.SetActive(false);
            RefreshIfDue();
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
            playerIcon.rectTransform.anchoredPosition = Project(player.transform.position);
            playerIcon.rectTransform.SetAsLastSibling();
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
                Label(npc.transform, name);
            }
            if (interactions == null || Time.unscaledTime >= nextInteractionScanAt)
            {
                interactions = Object.FindObjectsByType<Interactable>(FindObjectsSortMode.None);
                nextInteractionScanAt = Time.unscaledTime + 1f;
            }
            Dictionary<Transform, UI_MinimapElement> nativeIcons = NativeIcons(panel);
            foreach (Interactable interaction in interactions)
            {
                if (interaction == null || !interaction.isActiveAndEnabled || interaction.hideGuide ||
                    !Contains(interaction.transform.position) ||
                    nativeIcons.ContainsKey(interaction.transform) ||
                    interaction.GetComponentInParent<UnitAI_NewBasic>() != null ||
                    !interaction.IsInteractable(player.gameObject)) continue;
                string text = interaction.GetInteractionString();
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
        }

        private void Label(Transform target, string text)
        {
            visible.Add(target);
            if (!labels.TryGetValue(target, out MapLocationMarkerView marker))
            {
                marker = MapLocationMarkerView.Create(root,
                    panel.clickGuideMessage.GetComponent<TMPro.TextMeshProUGUI>());
                labels.Add(target, marker);
            }
            marker.Set(text, Project(target.position));
        }

        private void PositionNativeIcons()
        {
            foreach (var pair in NativeIcons(panel))
            {
                if (pair.Key == null || pair.Value == null) continue;
                if (!iconPositions.ContainsKey(pair.Value))
                    iconPositions.Add(pair.Value, pair.Value.rectTransform.anchoredPosition);
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

        private void DrawLandmarks()
        {
            // Match the native fixed-floor HUD map's tile-collision sampling.
            int width = Mathf.CeilToInt(floor.Size.x);
            int height = Mathf.CeilToInt(floor.Size.y);
            terrain = new Texture2D(width, height, TextureFormat.RGBA32, false);
            terrain.filterMode = FilterMode.Point;
            Color[] pixels = new Color[width * height];
            for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                {
                    Vector2 position = (Vector2)floor.transform.position + floor.bottomLeft +
                        new Vector2((x + 0.5f) * floor.Size.x / width,
                            (y + 0.5f) * floor.Size.y / height);
                    pixels[y * width + x] = Physics2D.OverlapPoint(position, CombatManager.TileLayerMask)
                        ? new Color(0.25f, 0.22f, 0.18f, 0.32f)
                        : new Color(0.4f, 0.55f, 0.32f, 0.12f);
                }
            terrain.SetPixels(pixels);
            terrain.Apply();
            RawImage ground = new GameObject("Terrain", typeof(RectTransform),
                typeof(RawImage)).GetComponent<RawImage>();
            ground.rectTransform.SetParent(root, false);
            ground.rectTransform.anchoredPosition = floor.Center * floor.mapScale + floor.mapOffset;
            ground.rectTransform.sizeDelta = floor.Size * floor.mapScale;
            ground.texture = terrain;
            ground.raycastTarget = false;

            SpriteRenderer[] renderers = floor.GetComponentsInChildren<SpriteRenderer>();
            System.Array.Sort(renderers, (a, b) =>
            {
                int layer = SortingLayer.GetLayerValueFromID(a.sortingLayerID).
                    CompareTo(SortingLayer.GetLayerValueFromID(b.sortingLayerID));
                return layer != 0 ? layer : a.sortingOrder.CompareTo(b.sortingOrder);
            });
            foreach (SpriteRenderer renderer in renderers)
            {
                if (!renderer.enabled || renderer.sprite == null || renderer.color.a == 0 ||
                    !Contains(renderer.bounds.center) ||
                    renderer.bounds.size.x >= floor.Size.x ||
                    renderer.bounds.size.y >= floor.Size.y ||
                    renderer.GetComponentInParent<UnitAvatar>() != null) continue;
                Image image = new GameObject("Landmark", typeof(RectTransform),
                    typeof(Image)).GetComponent<Image>();
                image.rectTransform.SetParent(root, false);
                image.rectTransform.anchoredPosition = Project(renderer.bounds.center);
                image.rectTransform.sizeDelta = (Vector2)renderer.bounds.size * floor.mapScale;
                image.rectTransform.localScale = new Vector3(
                    renderer.flipX ^ (renderer.transform.lossyScale.x < 0) ? -1 : 1,
                    renderer.flipY ^ (renderer.transform.lossyScale.y < 0) ? -1 : 1, 1);
                image.sprite = renderer.sprite;
                image.color = renderer.color;
                image.raycastTarget = false;
            }
        }

        private Vector2 Project(Vector3 position)
        {
            Vector3 origin = floor.transform.position;
            FixedFloorMapPoint point = FixedFloorMapProjection.Project(position.x, position.y,
                origin.x, origin.y, floor.mapScale, floor.mapOffset.x, floor.mapOffset.y);
            return new Vector2(point.X, point.Y);
        }

        private bool Contains(Vector3 position)
        {
            Vector3 origin = floor.transform.position;
            return FixedFloorMapProjection.Contains(position.x, position.y, origin.x, origin.y,
                floor.bottomLeft.x, floor.bottomLeft.y, floor.topRight.x, floor.topRight.y);
        }

        internal void Clear()
        {
            foreach (var pair in iconPositions)
                if (pair.Key != null) pair.Key.rectTransform.anchoredPosition = pair.Value;
            iconPositions.Clear();
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
            if (panel != null && generatedContents)
                panel.clickGuideMessage.gameObject.SetActive(originalGuide);
            root = null;
            if (terrain != null) Object.Destroy(terrain);
            terrain = null;
            playerIcon = null;
            map = null;
            panel = null;
            floor = null;
            mapFloorGuid = null;
            createdMap = false;
            generatedContents = false;
            interactions = null;
            nextRefreshAt = 0f;
            nextInteractionScanAt = 0f;
        }
    }
}
