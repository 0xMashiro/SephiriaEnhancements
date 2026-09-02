using SephiriaEnhancements.Runtime.Inventory;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SephiriaEnhancements.Runtime.GameBridge.Inventory
{
    internal static class InventoryCatalogReader
    {
        internal static bool TryCapture(UnitAvatar avatar,
            out InventoryCatalogSnapshot catalog)
        {
            try
            {
                int[] itemIds = ItemDatabase.GetAllItemID() ?? Array.Empty<int>();
                var items = new List<InventoryCatalogItemSnapshot>(itemIds.Length);
                foreach (int entityId in itemIds.OrderBy(value => value))
                {
                    ItemEntity entity = ItemDatabase.FindItemById(entityId);
                    if (entity == null)
                    {
                        continue;
                    }

                    string nativeItemTypeName = entity.type.ToString();
                    string[] possibleCategories = entity.type == EItemType.Charm
                        ? GetPossibleArtifactCategories(entity)
                        : Normalize(entity.categories);
                    items.Add(new InventoryCatalogItemSnapshot(entityId,
                        nativeItemTypeName, possibleCategories));
                }

                ItemCategoryEntity[] nativeCategories =
                    ItemDatabase.GetAllItemCategory() ??
                    Array.Empty<ItemCategoryEntity>();
                var categories = new List<InventoryCategoryCatalogSnapshot>(
                    nativeCategories.Length);
                foreach (ItemCategoryEntity category in nativeCategories
                    .Where(value => value != null)
                    .OrderBy(value => value.id, StringComparer.Ordinal))
                {
                    int[] setThresholds = category.setStatus?
                        .Select(value => value.itemCount)
                        .Distinct()
                        .OrderBy(value => value)
                        .ToArray() ?? Array.Empty<int>();
                    int[] comboThresholds = CaptureComboThresholds(category,
                        avatar, out int highestComboCount);
                    categories.Add(new InventoryCategoryCatalogSnapshot(category.id,
                        setThresholds, comboThresholds, highestComboCount));
                }

                catalog = new InventoryCatalogSnapshot(items.ToArray(),
                    categories.ToArray());
                return true;
            }
            catch (Exception)
            {
                catalog = null;
                return false;
            }
        }

        private static int[] CaptureComboThresholds(ItemCategoryEntity category,
            UnitAvatar avatar, out int highestComboCount)
        {
            highestComboCount = 0;
            if (category.comboEffectPrefab == null ||
                !category.comboEffectPrefab.TryGetComponent<ComboEffectBase>(
                    out var comboEffect))
            {
                return Array.Empty<int>();
            }

            var thresholds = new HashSet<int>();
            try
            {
                foreach (ComboEffectElement element in
                    comboEffect.RequestComboData(avatar))
                {
                    thresholds.Add(element.comboCount);
                }

                highestComboCount = comboEffect.GetHighestComboCount();
                if (highestComboCount > 0)
                {
                    thresholds.Add(highestComboCount);
                }
            }
            catch (Exception)
            {
                // Some native combo prefabs require a live avatar. A later
                // catalog refresh after player attachment can resolve them.
            }

            return thresholds.OrderBy(value => value).ToArray();
        }

        private static string[] GetPossibleArtifactCategories(ItemEntity entity)
        {
            // Charm_* is Sephiria's native API name. Runtime consumers receive
            // the player-facing Artifact terminology through snapshot types.
            if (entity.resourcePrefab != null &&
                entity.resourcePrefab.TryGetComponent<Charm_Basic>(out var charm))
            {
                try
                {
                    return Normalize(charm.GetPossibleCategory(entity));
                }
                catch (Exception)
                {
                    return Normalize(entity.categories);
                }
            }

            return Normalize(entity.categories);
        }

        private static string[] Normalize(IEnumerable<string> values)
        {
            return values?.Where(value => !string.IsNullOrEmpty(value))
                .Distinct(StringComparer.Ordinal)
                .OrderBy(value => value, StringComparer.Ordinal)
                .ToArray() ?? Array.Empty<string>();
        }
    }
}
