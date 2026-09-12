#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using SephiriaEnhancements.Runtime.Inventory;

namespace SephiriaEnhancements.Inventory
{
    internal static class RewardComboHighlightPolicy
    {
        internal sealed class Opportunity
        {
            internal string CategoryId = string.Empty;
            internal int CurrentCount;
            internal int TargetCount;
            internal int Priority;
        }

        internal static Opportunity? FindOpportunity(InventorySnapshot? snapshot,
            int rewardEntityId, IEnumerable<string>? fixedCategories)
        {
            if (snapshot == null || !snapshot.ArtifactEffectsEnabled ||
                !snapshot.BuildIntent.NativePresetEnabled || fixedCategories == null) return null;
            // Without placement simulation, only predict fixed additions into free space.
            // Duplicate conversion and dynamic categories can change the resulting counts.
            if (snapshot.Items.Any(item => item.EntityId == rewardEntityId ||
                item.Artifact != null && item.Artifact.CategoryRule.Kind != ArtifactCategoryRuleKind.Static) ||
                !snapshot.Cells.Any(cell => !cell.Disabled &&
                    !snapshot.Items.Any(item => item.CellIndex == cell.Index))) return null;
            Opportunity? best = null;
            foreach (string categoryId in fixedCategories.Distinct(StringComparer.Ordinal))
            {
                ComboCategorySnapshot? category = snapshot.ComboCategories.FirstOrDefault(value => value.CategoryId == categoryId);
                if (category == null || !category.AccountingConsistent || category.CurrentCount < 0 ||
                    !snapshot.BuildIntent.FruitSkewerCategoryPriorities.TryGetValue(categoryId, out int priority) ||
                    priority <= 0) continue;
                int target = category.SetThresholds.Concat(category.ComboThresholds)
                    .Where(value => value > category.CurrentCount).DefaultIfEmpty(0).Min();
                if (target <= category.CurrentCount) continue;
                if (best == null || priority > best.Priority || priority == best.Priority &&
                    string.CompareOrdinal(categoryId, best.CategoryId) < 0)
                    best = new Opportunity
                    {
                        CategoryId = categoryId,
                        CurrentCount = category.CurrentCount,
                        TargetCount = target,
                        Priority = priority
                    };
            }
            return best;
        }
    }
}
