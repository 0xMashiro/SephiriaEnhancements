using System;
using System.Collections.Generic;
using System.Linq;
using SephiriaEnhancements.Runtime.Inventory;

namespace SephiriaEnhancements.Inventory
{
    internal static class InventoryFruitSkewerPriorities
    {
        internal static (string CategoryId, int Priority, int MaximumCount)[] Resolve(
            InventorySnapshot snapshot, IEnumerable<string> manualCategories)
        {
            var targets = new List<(string CategoryId, int Priority, int MaximumCount)>();
            if (snapshot.BuildIntent.NativePresetEnabled != true) return targets.ToArray();
            var manual = new HashSet<string>(manualCategories, StringComparer.Ordinal);
            foreach (var category in snapshot.ComboCategories)
            {
                if (manual.Contains(category.CategoryId) ||
                    !snapshot.BuildIntent.FruitSkewerCategoryPriorities.TryGetValue(category.CategoryId, out int priority) || priority <= 0)
                    continue;
                int maximum = Math.Max(category.HighestComboCount,
                    category.SetThresholds.Concat(category.ComboThresholds).DefaultIfEmpty(0).Max());
                if (maximum <= 0) continue;
                // The native unlimited-combo effect rewards counts beyond the final tier.
                // Without it, extra counts above the last useful threshold have no priority value.
                if (snapshot.UnlimitedComboStatValue > 0 && category.HighestComboCount > 0)
                    maximum = int.MaxValue;
                targets.Add((category.CategoryId, priority, maximum));
            }
            return targets.ToArray();
        }
    }
}
