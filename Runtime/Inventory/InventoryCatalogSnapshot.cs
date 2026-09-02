#nullable disable

using System;
using System.Collections.Generic;

namespace SephiriaEnhancements.Runtime.Inventory
{
    internal sealed class InventoryCatalogItemSnapshot
    {
        internal InventoryCatalogItemSnapshot(int entityId,
            string nativeItemTypeName, string[] possibleCategories)
        {
            EntityId = entityId;
            NativeItemTypeName = nativeItemTypeName ?? string.Empty;
            PossibleCategories = Array.AsReadOnly(possibleCategories == null
                ? Array.Empty<string>()
                : (string[])possibleCategories.Clone());
        }

        internal int EntityId { get; }
        internal string NativeItemTypeName { get; }
        internal IReadOnlyList<string> PossibleCategories { get; }
    }

    internal sealed class InventoryCategoryCatalogSnapshot
    {
        internal InventoryCategoryCatalogSnapshot(string categoryId,
            int[] setThresholds, int[] comboThresholds,
            int highestComboCount = 0)
        {
            CategoryId = categoryId ?? string.Empty;
            SetThresholds = Array.AsReadOnly(setThresholds == null
                ? Array.Empty<int>()
                : (int[])setThresholds.Clone());
            ComboThresholds = Array.AsReadOnly(comboThresholds == null
                ? Array.Empty<int>()
                : (int[])comboThresholds.Clone());
            HighestComboCount = highestComboCount;
        }

        internal string CategoryId { get; }
        internal IReadOnlyList<int> SetThresholds { get; }
        internal IReadOnlyList<int> ComboThresholds { get; }
        internal int HighestComboCount { get; }
    }

    internal sealed class InventoryCatalogSnapshot
    {
        private readonly InventoryCatalogItemSnapshot[] items;
        private readonly InventoryCategoryCatalogSnapshot[] categories;
        private readonly Dictionary<int, InventoryCatalogItemSnapshot> itemsById;
        private readonly Dictionary<string, InventoryCategoryCatalogSnapshot>
            categoriesById;

        internal InventoryCatalogSnapshot(InventoryCatalogItemSnapshot[] items,
            InventoryCategoryCatalogSnapshot[] categories)
        {
            this.items = items == null
                ? Array.Empty<InventoryCatalogItemSnapshot>()
                : (InventoryCatalogItemSnapshot[])items.Clone();
            this.categories = categories == null
                ? Array.Empty<InventoryCategoryCatalogSnapshot>()
                : (InventoryCategoryCatalogSnapshot[])categories.Clone();
            Items = Array.AsReadOnly(this.items);
            Categories = Array.AsReadOnly(this.categories);
            itemsById = new Dictionary<int, InventoryCatalogItemSnapshot>();
            categoriesById = new Dictionary<string,
                InventoryCategoryCatalogSnapshot>(StringComparer.Ordinal);

            foreach (InventoryCatalogItemSnapshot item in this.items)
            {
                if (item != null)
                {
                    itemsById[item.EntityId] = item;
                }
            }
            foreach (InventoryCategoryCatalogSnapshot category in this.categories)
            {
                if (category != null && !string.IsNullOrEmpty(category.CategoryId))
                {
                    categoriesById[category.CategoryId] = category;
                }
            }
        }

        internal IReadOnlyList<InventoryCatalogItemSnapshot> Items { get; }
        internal IReadOnlyList<InventoryCategoryCatalogSnapshot> Categories { get; }

        internal bool TryGetItem(int entityId,
            out InventoryCatalogItemSnapshot item) =>
            itemsById.TryGetValue(entityId, out item);

        internal bool TryGetCategory(string categoryId,
            out InventoryCategoryCatalogSnapshot category)
        {
            if (categoryId != null && categoriesById.TryGetValue(categoryId,
                out category))
            {
                return true;
            }

            category = null;
            return false;
        }
    }
}
