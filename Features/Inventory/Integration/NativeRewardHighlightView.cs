using System.Collections.Generic;
using HarmonyLib;
using SephiriaEnhancements.Integration;
using SephiriaEnhancements.Runtime.Inventory;
using UnityEngine;
using UnityEngine.UI;

namespace SephiriaEnhancements.Inventory.Integration
{
    internal sealed class NativeRewardHighlightView
    {
        private static readonly AccessTools.FieldRef<UI_SephiriteRewardPanel,
            List<UI_SephiriteRewardElement>> RewardElements =
                AccessTools.FieldRefAccess<UI_SephiriteRewardPanel,
                    List<UI_SephiriteRewardElement>>("rewardElements");
        private static readonly AccessTools.FieldRef<UI_SephiriteRewardPanel,
            PlayerAvatar> OpenedAvatar =
                AccessTools.FieldRefAccess<UI_SephiriteRewardPanel,
                    PlayerAvatar>("openedAvatar");

        private sealed class RewardHighlight
        {
            internal GameObject Marker;
            internal CanvasGroup Group;
            internal Vector3 FavoriteScale;
        }

        private readonly Dictionary<UI_SephiriteRewardElement, RewardHighlight> markers = new();
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
                    Release(entry.Key, entry.Value);
                    removed.Add(entry.Key);
                }
            }
            foreach (UI_SephiriteRewardElement reward in removed)
                markers.Remove(reward);
            removed.Clear();

            foreach (UI_SephiriteRewardElement reward in rewards)
            {
                if (reward == null) continue;
                ItemEntity entity = ItemDatabase.FindItemById(reward.reward.entityID);
                // Use the same current local preference as native reward creation,
                // including edits while this panel is open, before inventory capture.
                bool favorite = entity != null && entity.type == EItemType.Charm &&
                    SaveManager.Current?.GetBool("Item_Favorite_" + entity.id, false) == true;
                if (reward.favoriteImage != null && reward.favoriteImage.activeSelf != favorite)
                    reward.SetFavorite(favorite);
                bool highlight = favorite || ShouldHighlight(reward, snapshot);
                if (!markers.TryGetValue(reward, out RewardHighlight visual))
                {
                    if (!highlight) continue;
                    GameObject marker = CreateMarker(reward.rectTransform);
                    visual = new RewardHighlight
                    {
                        Marker = marker,
                        Group = marker.GetComponent<CanvasGroup>(),
                        FavoriteScale = reward.favoriteImage != null
                            ? reward.favoriteImage.transform.localScale : Vector3.one
                    };
                    markers.Add(reward, visual);
                }
                if (visual.Marker.activeSelf != highlight) visual.Marker.SetActive(highlight);
                visual.Group.alpha = favorite ? 1f : 0.55f;
                if (reward.favoriteImage != null)
                    reward.favoriteImage.transform.localScale = visual.FavoriteScale *
                        (favorite ? 1.25f + 0.06f * Mathf.Sin(Time.unscaledTime * 3f) : 1f);
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
            GameObject marker = new("Sephiria Enhancements — Reward Highlight",
                typeof(RectTransform), typeof(CanvasGroup));
            RectTransform rect = marker.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            // Pixel corners sit outside the card, keeping its rarity frame,
            // category indicator and native selection cursor readable.
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(-6f, -6f);
            rect.offsetMax = new Vector2(6f, 6f);
            CanvasGroup group = marker.GetComponent<CanvasGroup>();
            group.blocksRaycasts = false;
            group.interactable = false;
            foreach (Vector2 corner in new[] { Vector2.zero, Vector2.right, Vector2.up, Vector2.one })
            {
                AddStroke(rect, corner, new Vector2(16f, 4f));
                AddStroke(rect, corner, new Vector2(4f, 16f));
            }
            return marker;
        }

        private static void AddStroke(RectTransform parent, Vector2 corner, Vector2 size)
        {
            GameObject stroke = new("Corner", typeof(RectTransform), typeof(Image));
            RectTransform rect = stroke.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = corner;
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = size;
            Image image = stroke.GetComponent<Image>();
            image.color = new Color(1f, 0.76f, 0.15f, 1f);
            image.raycastTarget = false;
        }

        private static void Release(UI_SephiriteRewardElement reward, RewardHighlight visual)
        {
            if (reward != null && reward.favoriteImage != null)
                reward.favoriteImage.transform.localScale = visual.FavoriteScale;
            if (visual.Marker != null) Object.Destroy(visual.Marker);
        }

        internal void Clear()
        {
            foreach (var entry in markers)
                Release(entry.Key, entry.Value);
            markers.Clear();
            removed.Clear();
            panel = null;
        }
    }
}
