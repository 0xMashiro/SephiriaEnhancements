using SephiriaEnhancements.MapEnhancements.Core;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SephiriaEnhancements.MapEnhancements
{
    internal sealed class TownNpcMapMarkerLayer
    {
        // Native FloorData.name at the game integration boundary.
        private const string RabbitTownFloorName = "TheRabbittown";
        private const float RefreshInterval = 0.2f;

        private readonly Dictionary<UnitAI_NewBasic, TownNpcMapMarkerView> markerViews =
            new Dictionary<UnitAI_NewBasic, TownNpcMapMarkerView>();
        private readonly List<UnitAI_NewBasic> visibleNpcs =
            new List<UnitAI_NewBasic>();

        private RectTransform root;
        private UI_Map map;
        private UI_MapPanel panel;
        private FullyDesignedFloorGenerator townFloor;
        private float nextRefreshAt;

        internal void Show(UI_MapPanel mapPanel, string shownFloorGuid)
        {
            Clear();
            if (!TryGetRabbitTown(shownFloorGuid,
                    out FullyDesignedFloorGenerator generator) ||
                !mapPanel.maps.TryGetValue(shownFloorGuid, out UI_Map shownMap) ||
                shownMap == null || shownMap.contentsChild == null)
            {
                return;
            }

            panel = mapPanel;
            map = shownMap;
            townFloor = generator;

            GameObject rootObject = new GameObject("Town NPC Map Markers",
                typeof(RectTransform), typeof(CanvasGroup));
            root = rootObject.GetComponent<RectTransform>();
            root.SetParent(map.contentsChild, false);
            root.anchorMin = Vector2.zero;
            root.anchorMax = Vector2.zero;
            root.pivot = Vector2.zero;
            root.anchoredPosition = Vector2.zero;
            root.sizeDelta = Vector2.zero;
            root.SetAsLastSibling();

            CanvasGroup canvasGroup = rootObject.GetComponent<CanvasGroup>();
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;

            Refresh();
        }

        internal void RefreshIfDue()
        {
            if (root == null || panel == null || !panel.IsOpened || map == null ||
                !map.gameObject.activeInHierarchy || Time.unscaledTime < nextRefreshAt)
            {
                return;
            }

            Refresh();
        }

        internal void Clear()
        {
            markerViews.Clear();
            visibleNpcs.Clear();
            if (root != null)
            {
                Object.Destroy(root.gameObject);
            }

            root = null;
            map = null;
            panel = null;
            townFloor = null;
            nextRefreshAt = 0f;
        }

        private void Refresh()
        {
            nextRefreshAt = Time.unscaledTime + RefreshInterval;
            visibleNpcs.Clear();

            List<UnitAI_NewBasic> allNpcs = UnitAI_NewBasic.AllInstances;
            if (allNpcs != null)
            {
                for (int index = 0; index < allNpcs.Count; index++)
                {
                    UnitAI_NewBasic npc = allNpcs[index];
                    if (IsVisibleTownNpc(npc))
                    {
                        visibleNpcs.Add(npc);
                    }
                }
            }

            List<UnitAI_NewBasic> removed = null;
            foreach (KeyValuePair<UnitAI_NewBasic, TownNpcMapMarkerView> pair in
                markerViews)
            {
                if (pair.Key == null || !visibleNpcs.Contains(pair.Key))
                {
                    removed ??= new List<UnitAI_NewBasic>();
                    removed.Add(pair.Key);
                    if (pair.Value != null)
                    {
                        Object.Destroy(pair.Value.gameObject);
                    }
                }
            }

            if (removed != null)
            {
                for (int index = 0; index < removed.Count; index++)
                {
                    markerViews.Remove(removed[index]);
                }
            }

            for (int index = 0; index < visibleNpcs.Count; index++)
            {
                UnitAI_NewBasic npc = visibleNpcs[index];
                if (!markerViews.TryGetValue(npc, out TownNpcMapMarkerView marker) ||
                    marker == null)
                {
                    marker = TownNpcMapMarkerView.Create(root,
                        panel.stageNameText);
                    markerViews[npc] = marker;
                }

                Vector3 worldPosition = npc.transform.position;
                Vector3 floorOrigin = townFloor.transform.position;
                TownMapPoint mapPoint = TownMapProjection.Project(
                    worldPosition.x, worldPosition.y,
                    floorOrigin.x, floorOrigin.y,
                    townFloor.mapScale, townFloor.mapOffset.x,
                    townFloor.mapOffset.y);
                marker.Set(npc.Avatar.Name, new Vector2(mapPoint.X, mapPoint.Y));
            }

            root.SetAsLastSibling();
        }

        private bool IsVisibleTownNpc(UnitAI_NewBasic npc) =>
            npc != null && npc.gameObject.activeInHierarchy &&
            !string.IsNullOrEmpty(npc.socialID) && npc.Avatar != null &&
            !npc.Avatar.IsDead && IsInsideTown(npc.transform.position) &&
            !string.IsNullOrEmpty(npc.Avatar.Name);

        private bool IsInsideTown(Vector3 worldPosition)
        {
            Vector3 floorOrigin = townFloor.transform.position;
            Vector2 localPosition = new Vector2(
                worldPosition.x - floorOrigin.x,
                worldPosition.y - floorOrigin.y);
            return localPosition.x >= townFloor.bottomLeft.x &&
                localPosition.x <= townFloor.topRight.x &&
                localPosition.y >= townFloor.bottomLeft.y &&
                localPosition.y <= townFloor.topRight.y;
        }

        private static bool TryGetRabbitTown(string shownFloorGuid,
            out FullyDesignedFloorGenerator generator)
        {
            generator = null;
            DungeonManager dungeon = DungeonManager.Instance;
            if (dungeon == null || string.IsNullOrEmpty(shownFloorGuid) ||
                !dungeon.generatedFloors.TryGetValue(shownFloorGuid,
                    out FloorData floor) || floor == null ||
                floor.name != RabbitTownFloorName)
            {
                return false;
            }

            foreach (FloorGenerator candidate in FloorGenerator.FloorGenerators)
            {
                if (candidate != null && candidate.guid == shownFloorGuid)
                {
                    generator = candidate as FullyDesignedFloorGenerator;
                    return generator != null;
                }
            }

            return false;
        }
    }

    internal sealed class TownNpcMapMarkerView : MonoBehaviour
    {
        private RectTransform rectTransform;
        private TextMeshProUGUI nameText;

        internal static TownNpcMapMarkerView Create(RectTransform parent,
            TextMeshProUGUI nativeTextTemplate)
        {
            GameObject markerObject = new GameObject("Town NPC Map Marker",
                typeof(RectTransform), typeof(TownNpcMapMarkerView));
            TownNpcMapMarkerView marker =
                markerObject.GetComponent<TownNpcMapMarkerView>();
            marker.rectTransform = markerObject.GetComponent<RectTransform>();
            marker.rectTransform.SetParent(parent, false);
            marker.rectTransform.anchorMin = Vector2.zero;
            marker.rectTransform.anchorMax = Vector2.zero;
            marker.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            marker.rectTransform.sizeDelta = Vector2.zero;

            GameObject dotObject = new GameObject("Position",
                typeof(RectTransform), typeof(Image));
            RectTransform dot = dotObject.GetComponent<RectTransform>();
            dot.SetParent(marker.rectTransform, false);
            dot.anchorMin = new Vector2(0.5f, 0.5f);
            dot.anchorMax = new Vector2(0.5f, 0.5f);
            dot.pivot = new Vector2(0.5f, 0.5f);
            dot.anchoredPosition = Vector2.zero;
            dot.sizeDelta = new Vector2(7f, 7f);
            dotObject.GetComponent<Image>().color = new Color(1f, 0.82f, 0.28f, 1f);

            GameObject textObject = new GameObject("Name",
                typeof(RectTransform), typeof(TextMeshProUGUI));
            RectTransform textRect = textObject.GetComponent<RectTransform>();
            textRect.SetParent(marker.rectTransform, false);
            textRect.anchorMin = new Vector2(0.5f, 0.5f);
            textRect.anchorMax = new Vector2(0.5f, 0.5f);
            textRect.pivot = new Vector2(0.5f, 0f);
            textRect.anchoredPosition = new Vector2(0f, 7f);
            textRect.sizeDelta = new Vector2(180f, 26f);

            marker.nameText = textObject.GetComponent<TextMeshProUGUI>();
            marker.nameText.alignment = TextAlignmentOptions.Bottom;
            marker.nameText.textWrappingMode = TextWrappingModes.NoWrap;
            marker.nameText.overflowMode = TextOverflowModes.Overflow;
            marker.nameText.raycastTarget = false;
            marker.nameText.color = Color.white;
            marker.nameText.fontSize = nativeTextTemplate != null
                ? Mathf.Max(12f, nativeTextTemplate.fontSize * 0.52f)
                : 16f;
            if (nativeTextTemplate != null)
            {
                marker.nameText.font = nativeTextTemplate.font;
                marker.nameText.fontSharedMaterial =
                    nativeTextTemplate.fontSharedMaterial;
                SephiriaEnhancements.Integration.NativeLocalizedText.BindFont(marker.nameText, nativeTextTemplate);
            }

            UnityEngine.UI.Shadow shadow =
                textObject.AddComponent<UnityEngine.UI.Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.92f);
            shadow.effectDistance = new Vector2(1.5f, -1.5f);
            shadow.useGraphicAlpha = true;
            return marker;
        }

        internal void Set(string npcName, Vector2 mapPosition)
        {
            rectTransform.anchoredPosition = mapPosition;
            if (nameText.text != npcName)
            {
                nameText.text = npcName;
            }
        }
    }
}
