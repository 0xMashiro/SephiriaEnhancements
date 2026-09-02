using SephiriaEnhancements.Runtime.Inventory;

namespace SephiriaEnhancements.ModelChecks.Runtime.Inventory;

internal static class InventoryCatalogSnapshotChecks
{
    internal static void Run()
    {
        var catalogItems = new[]
        {
            new InventoryCatalogItemSnapshot(100, "Charm", new[] { "EMBER" })
        };
        var catalogCategories = new[]
        {
            new InventoryCategoryCatalogSnapshot("EMBER", new[] { 2, 4, 6 },
                new[] { 2, 4 }, 4)
        };
        var inventoryCatalog = new InventoryCatalogSnapshot(catalogItems,
            catalogCategories);
        catalogItems[0] = new InventoryCatalogItemSnapshot(999, "Other",
            Array.Empty<string>());
        catalogCategories[0] = new InventoryCategoryCatalogSnapshot("OTHER",
            Array.Empty<int>(), Array.Empty<int>());
        if (!inventoryCatalog.TryGetItem(100, out InventoryCatalogItemSnapshot catalogItem) ||
            catalogItem.NativeItemTypeName != "Charm" ||
            catalogItem.PossibleCategories.Count != 1 ||
            !inventoryCatalog.TryGetCategory("EMBER",
                out InventoryCategoryCatalogSnapshot catalogCategory) ||
            catalogCategory.SetThresholds.Count != 3 ||
            catalogCategory.ComboThresholds[1] != 4 ||
            catalogCategory.HighestComboCount != 4 ||
            inventoryCatalog.TryGetCategory("UNKNOWN", out _))
            throw new InvalidOperationException("inventory catalog lookup or immutability failed");
        Console.WriteLine("InventoryCatalogSnapshot: lookup and immutability checks passed");
    }
}
