using System.Collections.Generic;
using SephiriaEnhancements.Runtime.Inventory;

namespace SephiriaEnhancements.Inventory
{
    internal static class RewardComboHighlightPolicy
    {
        internal static bool ShouldHighlight(InventorySnapshot snapshot,
            IEnumerable<string> possibleCategories)
        {
            if (snapshot?.NativePreset?.Enabled != true ||
                possibleCategories == null)
                return false;

            foreach (string categoryId in possibleCategories)
            {
                foreach (ComboCategorySnapshot category in snapshot.ComboCategories)
                {
                    if (category.CategoryId == categoryId &&
                        category.NativePresetFavorite &&
                        category.HighestComboCount > 0 &&
                        category.CurrentCount < category.HighestComboCount)
                        return true;
                }
            }
            return false;
        }
    }
}
