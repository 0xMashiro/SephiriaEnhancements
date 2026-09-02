#nullable disable
using System.Linq;
using SephiriaEnhancements.Runtime.Inventory;
using TMPro;
using UnityEngine;

namespace SephiriaEnhancements.Inventory
{
    internal sealed class InventoryIntentBadge : MonoBehaviour
    {
        private static readonly Color PriorityColor =
            new(1f, 0.78f, 0.16f, 1f);
        private static readonly Color AvoidColor =
            new(1f, 0.42f, 0.36f, 1f);

        private UI_NewInventoryIcon owner;
        private GameObject badgeRoot;
        private TextMeshProUGUI label;

        internal static void RefreshVisible(UI_CharacterStatusPanel panel,
            InventoryOptimizationPreferences preferences)
        {
            if (panel == null)
            {
                return;
            }
            foreach (UI_NewInventoryIcon icon in
                panel.GetComponentsInChildren<UI_NewInventoryIcon>(true))
            {
                NewItemOwnInstance item = icon?.Item;
                ArtifactOptimizationPreference intent = item?.Charm == null
                    ? null
                    : preferences?.ArtifactPreferences.FirstOrDefault(rule =>
                        rule.TargetsInstance &&
                        rule.ItemKey == new InventoryItemKey(item.EntityID, item.InstanceID));
                bool visible = intent?.Level ==
                        InventoryPreferenceLevel.Priority ||
                    intent?.Level == InventoryPreferenceLevel.Avoid;
                InventoryIntentBadge badge =
                    icon?.GetComponent<InventoryIntentBadge>();
                if (visible && badge == null)
                {
                    badge = icon.gameObject.AddComponent<InventoryIntentBadge>();
                }
                badge?.Refresh(icon, intent);
            }
        }

        private void Refresh(UI_NewInventoryIcon icon,
            ArtifactOptimizationPreference intent)
        {
            owner = icon;
            bool visible = intent?.Level ==
                    InventoryPreferenceLevel.Priority ||
                intent?.Level == InventoryPreferenceLevel.Avoid;
            if (visible)
            {
                EnsureVisual();
                bool avoided = intent.Level == InventoryPreferenceLevel.Avoid;
                label.text = avoided
                    ? "×"
                    : "↑" + (intent.PriorityOrder + 1);
                label.color = avoided ? AvoidColor : PriorityColor;
            }
            badgeRoot?.SetActive(visible);
            badgeRoot?.transform.SetAsLastSibling();
        }

        private void EnsureVisual()
        {
            if (badgeRoot != null || owner == null)
            {
                return;
            }
            badgeRoot = new GameObject("TemporaryInventoryIntent",
                typeof(RectTransform), typeof(TextMeshProUGUI));
            RectTransform rect = badgeRoot.GetComponent<RectTransform>();
            rect.SetParent(owner.transform, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.one;
            rect.anchoredPosition = new Vector2(-4f, -4f);
            rect.sizeDelta = new Vector2(24f, 18f);

            label = badgeRoot.GetComponent<TextMeshProUGUI>();
            label.font = owner.quantityText?.font;
            label.fontSharedMaterial = owner.quantityText?.fontSharedMaterial;
            label.fontStyle = FontStyles.Bold;
            label.alignment = TextAlignmentOptions.TopRight;
            label.enableAutoSizing = true;
            label.fontSizeMin = 8f;
            label.fontSizeMax = Mathf.Max(12f,
                owner.quantityText?.fontSize ?? 12f);
            label.raycastTarget = false;
        }
    }
}
