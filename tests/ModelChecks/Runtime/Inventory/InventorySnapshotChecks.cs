using SephiriaEnhancements.Runtime.Inventory;

namespace SephiriaEnhancements.ModelChecks.Runtime.Inventory;

internal static class InventorySnapshotChecks
{
    internal static void Run()
    {
        InventorySnapshot inventorySnapshot = InventorySnapshotFixture.WithRestrictedArtifact(
            out InventoryCellSnapshot[] inventoryCells, out InventoryItemSnapshot[] inventoryItems);
        if (inventorySnapshot.Width != 2 || inventorySnapshot.Height != 2 ||
            inventorySnapshot.Storage != 3 || inventorySnapshot.Items.Count != 2 ||
            !inventorySnapshot.TryGetCell(1, 0, out InventoryCellSnapshot ignoredCell) ||
            !ignoredCell.Disabled || !ignoredCell.IgnoresCriteria || !ignoredCell.Mystic ||
            ignoredCell.DisableCount != 2 || ignoredCell.IgnoreCriteriaCount != 1 ||
            inventorySnapshot.TryGetCell(1, 1, out _) ||
            inventorySnapshot.Items[0].Kind != InventoryItemKind.Artifact ||
            inventorySnapshot.Items[0].NativeItemTypeName != "Charm" ||
            inventorySnapshot.Items[0].NativeType != NativeInventoryItemType.Charm ||
            inventorySnapshot.Items[1].Kind != InventoryItemKind.RestrictedArtifact ||
            inventorySnapshot.Items[1].Artifact.Criteria.Kind !=
                ArtifactActivationConditionKind.Unknown ||
            inventorySnapshot.Items[1].Artifact.Criteria.RuntimeState !=
                CriteriaEvaluationState.Unsatisfied ||
            inventorySnapshot.Items[1].Artifact.Criteria.PositionProjectionState !=
                CriteriaEvaluationState.Satisfied ||
            inventorySnapshot.NativePreset.SelectedSlot != 2 ||
            inventorySnapshot.NativePreset.HasExplicitComboTargets ||
            inventorySnapshot.BuildIntent.NativePresetSlot != 2 ||
            inventorySnapshot.BuildIntent.PreferredArtifactEntityIds[0] != 101 ||
            inventorySnapshot.ComboCategories.Count != 1 ||
            inventorySnapshot.ComboCategories[0].CurrentCount != 3 ||
            inventorySnapshot.ComboCategories[0].ArtifactCategoryCount != 1 ||
            inventorySnapshot.ComboCategories[0].InferredUniquePairCount != 1 ||
            inventorySnapshot.ComboCategories[0].HighestComboCount != 4 ||
            inventorySnapshot.ComboCategories[0].HighestReachedThreshold != 2 ||
            inventorySnapshot.ComboCategories[0].UnlimitedComboExtraCount != 0 ||
            !inventorySnapshot.ComboCategories[0].NativePresetFavorite ||
            !inventorySnapshot.SuppressDuplicateComboEntities ||
            inventorySnapshot.UniquePairComboMode != 2 ||
            inventorySnapshot.UnlimitedComboStatValue != 1 ||
            inventorySnapshot.SettlementValidation.LayoutProjectionReady ||
            inventorySnapshot.SettlementValidation.CurrentLayoutVerified ||
            !inventorySnapshot.SettlementValidation.Issues.Contains(
                "BaselineStateUnavailable") ||
            !inventorySnapshot.SettlementValidation.Issues.Contains(
                "LayoutProjectionArtifactCriteriaUnavailable"))
            throw new InvalidOperationException("inventory snapshot dimensions, lookup or semantics failed");
        inventoryCells[0] = inventoryCells[1];
        inventoryItems[0] = inventoryItems[1];
        if (inventorySnapshot.Cells[0].Index != 0 || inventorySnapshot.Items[0].InstanceId != 10)
            throw new InvalidOperationException("inventory snapshot must isolate caller arrays");
        Console.WriteLine("InventorySnapshot: dimensions, classification and immutability checks passed");

        string[] nativeInventoryTypes = Enum.GetNames<NativeInventoryItemType>();
        string[] expectedNativeInventoryTypes =
        {
            "Unknown", "Misc", "ThrowingWeapon", "Potion", "Food", "Scroll",
            "Charm", "StoneTablet", "Identifiable"
        };
        if (!nativeInventoryTypes.SequenceEqual(expectedNativeInventoryTypes))
            throw new InvalidOperationException(
                "native inventory item type contract drifted from Sephiria EItemType");
        Console.WriteLine("InventorySnapshot: native EItemType contract passed");

        string[] activationConditionKinds =
            Enum.GetNames<ArtifactActivationConditionKind>();
        string[] expectedActivationConditionKinds =
        {
            "None", "TopRow", "BottomRow", "SideEdge", "Interior", "Border",
            "BothSidesEmpty", "BothSidesArtifacts", "AllNeighborsOccupied",
            "AdjacentMagicArtifact", "FullHealth", "Unknown"
        };
        if (!activationConditionKinds.SequenceEqual(expectedActivationConditionKinds) ||
            !new InventoryMechanicCoverageSnapshot(inventorySnapshot).
                ActivationConditions.SequenceEqual(new[] { "Unknown" }))
            throw new InvalidOperationException(
                "artifact activation conditions must remain domain concepts");
        Console.WriteLine("InventorySnapshot: artifact activation condition contract passed");
    }
}
