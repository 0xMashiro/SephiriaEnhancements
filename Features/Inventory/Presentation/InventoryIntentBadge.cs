#nullable disable
using System.Linq;
using SephiriaEnhancements.Integration;
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
            new(0.58f, 0.76f, 0.78f, 1f);

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
                bool avoided = intent?.Level == InventoryPreferenceLevel.Avoid;
                label.text = intent == null ? "" : avoided
                    ? "×"
                    : (intent.PriorityOrder + 1).ToString();
                if (intent?.Strength == InventoryConstraintStrength.Hard) label.text += "!";
                label.color = avoided ? AvoidColor : PriorityColor;
                NativeLocalizedText.MatchFontSize(label, owner.powerText);
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
            // Mirror the native level label vertically, retaining its font,
            // material, scale, text area and inset in the same parent canvas.
            TextMeshProUGUI template = owner.powerText;
            label = Instantiate(template, template.transform.parent, false);
            badgeRoot = label.gameObject;
            badgeRoot.name = "TemporaryInventoryIntent";
            RectTransform source = template.rectTransform;
            RectTransform rect = label.rectTransform;
            rect.anchorMin = new Vector2(source.anchorMin.x, 1f - source.anchorMax.y);
            rect.anchorMax = new Vector2(source.anchorMax.x, 1f - source.anchorMin.y);
            rect.pivot = new Vector2(source.pivot.x, 1f - source.pivot.y);
            rect.anchoredPosition = new Vector2(source.anchoredPosition.x, -source.anchoredPosition.y);
            label.alignment = TextAlignmentOptions.BottomLeft;
            label.raycastTarget = false;
        }
    }
}
