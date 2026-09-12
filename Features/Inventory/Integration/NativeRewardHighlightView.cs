using System.Collections.Generic;
using System.Linq;
using TMPro;
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
            internal string Reason;
        }

        private static readonly AccessTools.FieldRef<UI_CharmTooltip, TMP_Text> TypeText =
            AccessTools.FieldRefAccess<UI_CharmTooltip, TMP_Text>("typeText");
        private TMP_Text reasonText;
        private string originalText;
        private string renderedText;

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
            var opportunities = rewards.Where(reward => reward != null).ToDictionary(reward => reward,
                reward => FindOpportunity(reward, snapshot));
            long highestPriority = opportunities.Values.Where(value => value != null)
                .Select(value => value.Priority).DefaultIfEmpty(0).Max();
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
                var opportunity = opportunities[reward];
                bool supplement = opportunity != null && opportunity.Priority == highestPriority;
                bool highlight = favorite || supplement;
                if (!markers.TryGetValue(reward, out RewardHighlight visual))
                {
                    if (!highlight) continue;
                    GameObject marker = CreateMarker(reward.rectTransform);
                    visual = new RewardHighlight
                    {
                        Marker = marker
                    };
                    markers.Add(reward, visual);
                }
                if (visual.Marker.activeSelf != highlight) visual.Marker.SetActive(highlight);
                visual.Reason = favorite ? Loc._(RewardHighlightLocalization.Favorite) : null;
                if (!favorite && supplement)
                {
                    var category = ItemDatabase.FindItemCategory(opportunity.CategoryId);
                    visual.Reason = string.Format(Loc._(RewardHighlightLocalization.Fruit),
                        category.categoryName.ToString(), opportunity.CurrentCount, opportunity.TargetCount);
                }
            }
            UpdateReason();
        }

        private static RewardComboHighlightPolicy.Opportunity FindOpportunity(UI_SephiriteRewardElement reward,
            InventorySnapshot snapshot)
        {
            if (snapshot == null) return null;
            ItemEntity entity = ItemDatabase.FindItemById(reward.reward.entityID);
            // GetPossibleCategory includes conditional categories. Only the base
            // GetItemCategory contract guarantees the entity's fixed categories.
            if (entity == null || entity.type != EItemType.Charm || entity.resourcePrefab == null ||
                !entity.resourcePrefab.TryGetComponent<Charm_Basic>(out var artifact) ||
                artifact.GetType().GetMethod(nameof(Charm_Basic.GetItemCategory)).DeclaringType != typeof(Charm_Basic))
                return null;
            return RewardComboHighlightPolicy.FindOpportunity(snapshot, entity.id, entity.categories);
        }

        private void UpdateReason()
        {
            var tooltip = UIManager.Instance?.GetElement<UI_CharmTooltip>();
            if (tooltip == null || !tooltip.IsOpened ||
                !(tooltip.Target is UI_SephiriteRewardElement reward) || !reward.Showing ||
                !markers.TryGetValue(reward, out var visual) || !visual.Marker.activeSelf || visual.Reason == null)
            {
                ClearReason();
                return;
            }
            TMP_Text text = TypeText(tooltip);
            if (reasonText != text) ClearReason();
            // Keep the native font and layout. Native refreshes replace the base
            // text; only remove our own exact rendering when relinquishing it.
            if (reasonText == null || text.text != renderedText) originalText = text.text;
            reasonText = text;
            renderedText = originalText + "\n" + visual.Reason;
            if (text.text != renderedText) text.text = renderedText;
        }

        private void ClearReason()
        {
            if (reasonText != null && reasonText.text == renderedText) reasonText.text = originalText;
            reasonText = null;
            originalText = renderedText = null;
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
            if (visual.Marker != null) Object.Destroy(visual.Marker);
        }

        internal void Clear()
        {
            ClearReason();
            foreach (var entry in markers)
                Release(entry.Key, entry.Value);
            markers.Clear();
            removed.Clear();
            panel = null;
        }
    }
}
