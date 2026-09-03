using System.Collections.Generic;
using HarmonyLib;
using SephiriaEnhancements.Integration;
using SephiriaEnhancements.Runtime.Inventory;
using UnityEngine;
using UnityEngine.UI;

namespace SephiriaEnhancements.Inventory.Integration
{
    internal sealed class NativeRewardComboHighlightView
    {
        private static readonly AccessTools.FieldRef<UI_SephiriteRewardPanel,
            List<UI_SephiriteRewardElement>> RewardElements =
                AccessTools.FieldRefAccess<UI_SephiriteRewardPanel,
                    List<UI_SephiriteRewardElement>>("rewardElements");
        private static readonly AccessTools.FieldRef<UI_SephiriteRewardPanel,
            PlayerAvatar> OpenedAvatar =
                AccessTools.FieldRefAccess<UI_SephiriteRewardPanel,
                    PlayerAvatar>("openedAvatar");

        private readonly Dictionary<UI_SephiriteRewardElement, GameObject> markers = new();
        private readonly List<UI_SephiriteRewardElement> removed = new();
        private UI_SephiriteRewardPanel panel;

        internal void Update(bool enabled, InventorySnapshot snapshot)
        {
            UI_SephiriteRewardPanel current = enabled
                ? UIManager.Instance?.GetElement<UI_SephiriteRewardPanel>() : null;
            if (current == null || !current.IsOpened ||
                !LocalPlayerResolver.IsLocal(OpenedAvatar(current)))
            {
                Clear();
                return;
            }
            if (panel != current)
            {
                Clear();
                panel = current;
            }

            List<UI_SephiriteRewardElement> rewards = RewardElements(panel);
            foreach (var entry in markers)
            {
                if (entry.Key == null || !rewards.Contains(entry.Key))
                {
                    if (entry.Value != null) Object.Destroy(entry.Value);
                    removed.Add(entry.Key);
                }
            }
            foreach (UI_SephiriteRewardElement reward in removed)
                markers.Remove(reward);
            removed.Clear();

            foreach (UI_SephiriteRewardElement reward in rewards)
            {
                if (reward == null) continue;
                bool highlight = ShouldHighlight(reward, snapshot);
                if (!markers.TryGetValue(reward, out GameObject marker))
                {
                    if (!highlight) continue;
                    marker = CreateMarker(reward.rectTransform);
                    markers.Add(reward, marker);
                }
                if (marker.activeSelf != highlight) marker.SetActive(highlight);
            }
        }

        private static bool ShouldHighlight(UI_SephiriteRewardElement reward,
            InventorySnapshot snapshot)
        {
            if (snapshot?.NativePreset?.Enabled != true) return false;
            ItemEntity entity = ItemDatabase.FindItemById(reward.reward.entityID);
            // Charm is the native API name for an artifact. Possible categories
            // describe a reward before placement, not a guaranteed combo increase.
            return entity != null && entity.type == EItemType.Charm &&
                entity.resourcePrefab != null &&
                entity.resourcePrefab.TryGetComponent<Charm_Basic>(out var artifact) &&
                RewardComboHighlightPolicy.ShouldHighlight(snapshot,
                    artifact.GetPossibleCategory(entity));
        }

        private static GameObject CreateMarker(RectTransform parent)
        {
            GameObject marker = new("Sephiria Enhancements — Reward Combo",
                typeof(RectTransform), typeof(Image));
            RectTransform rect = marker.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            // A separate underline leaves rarity, category and selection frames
            // visible. Relative sizing follows the native reward card and Canvas.
            rect.anchorMin = new Vector2(0.12f, -0.13f);
            rect.anchorMax = new Vector2(0.88f, -0.02f);
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            Image backing = marker.GetComponent<Image>();
            backing.color = new Color(0.10f, 0.08f, 0.04f, 1f);
            backing.raycastTarget = false;

            GameObject bar = new("Bar", typeof(RectTransform), typeof(Image));
            RectTransform barRect = bar.GetComponent<RectTransform>();
            barRect.SetParent(rect, false);
            barRect.anchorMin = new Vector2(0.025f, 0.2f);
            barRect.anchorMax = new Vector2(0.975f, 0.8f);
            barRect.offsetMin = barRect.offsetMax = Vector2.zero;
            Image image = bar.GetComponent<Image>();
            image.color = new Color(1f, 0.76f, 0.15f, 1f);
            image.raycastTarget = false;
            return marker;
        }

        internal void Clear()
        {
            foreach (GameObject marker in markers.Values)
                if (marker != null) Object.Destroy(marker);
            markers.Clear();
            removed.Clear();
            panel = null;
        }
    }
}
