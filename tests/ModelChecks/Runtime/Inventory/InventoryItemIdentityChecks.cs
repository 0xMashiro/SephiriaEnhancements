using SephiriaEnhancements.Inventory;
using SephiriaEnhancements.Runtime.Inventory;

namespace SephiriaEnhancements.ModelChecks.Runtime.Inventory;

internal static class InventoryItemIdentityChecks
{
    private static readonly InventoryItemKey[] CompanionKeys =
    {
        new(5004, 0), new(5005, 0), new(5006, 0)
    };

    internal static string Run()
    {
        VerifyOptimizationPipeline();
        VerifySwapConfirmation();
        VerifyTabletIdentity();
        VerifyIdentityConflicts();
        VerifyIndependentIntentAndLifecycle();
        VerifyEvaluationOrder();
        return "shared native IDs, swaps, settlement, independent intent and identity conflicts passed";
    }

    private static void VerifyOptimizationPipeline()
    {
        int[] levels = { 0, -1, 3 };
        InventorySnapshot source = Snapshot(CompanionKeys, levels, new[] { 0, 1, 2 });
        Require(source.SettlementValidation.LayoutProjectionReady,
            "different entities sharing native ID zero must remain projectable");
        InventoryOptimizationPreferences preferences = InventoryArtifactIntentEditor.PlaceAvoid(
            InventoryArtifactIntentEditor.Toggle(InventoryOptimizationPreferences.Default, 0, 5004), 0, 5005, 0);
        ResolvedInventoryOptimizationPolicy policy = InventoryOptimizationPolicyResolver.Resolve(source, preferences);
        InventoryOptimizationProposal proposal = InventoryOptimizerSelector.Solve(source, policy,
            new InventorySearchBudget(4, 200, 1000));
        Require(proposal.Succeeded && proposal.Improved &&
            proposal.BestScore.PriorityTargetsSatisfied == 1 &&
            proposal.BestScore.AvoidedTargetsActive == 0,
            "priority and avoid must apply to different zero-ID artifacts");
        Require(proposal.TargetEvaluations.Select(target => target.Target).ToHashSet().SetEquals(
            new[] { "Artifact:5004:0", "Artifact:5005:0" }),
            "target evidence must retain both identity components");

        ProjectedInventorySettlement expected = InventorySettlementProjector.Evaluate(source, proposal.Layout);
        Require(expected.Artifacts.Select(item => item.ItemKey).ToHashSet().SetEquals(CompanionKeys),
            "projection must keep every artifact instead of overwriting repeated native IDs");
        InventorySnapshot actual = Snapshot(CompanionKeys, levels,
            Enumerable.Range(0, 3).Select(proposal.Layout.GetCell).ToArray());
        Require(InventoryApplicationConfirmation.MatchesTarget(actual, source, proposal.Layout) &&
            InventorySettlementDifferentialVerifier.Compare(source, proposal.Layout, expected, actual).Matched,
            "independently rebuilt observed settlement must match the selected layout");
        Require(proposal.Outcome.ArtifactChanges.Select(change => change.ItemKey).ToHashSet().SetEquals(
            new[] { CompanionKeys[0], CompanionKeys[2] }),
            "outcome must report both artifacts whose effective levels changed");
    }

    private static void VerifySwapConfirmation()
    {
        int[] levels = { 0, 0, 0 };
        InventorySnapshot source = Snapshot(CompanionKeys, levels, new[] { 0, 1, 2 });
        var target = new InventoryLayoutProjection(new[] { 1, 0, 2 }, new[] { 0, 0, 0 });
        Require(InventoryLayoutPlanner.TryCreate(source, target, out InventoryApplicationPlan plan, out _) &&
            plan.Swaps.Count == 1, "swapping different zero-ID artifacts requires one operation");
        InventorySnapshot swapped = Snapshot(CompanionKeys, levels, new[] { 1, 0, 2 });
        Require(!InventoryApplicationConfirmation.IsSwapObserved(source, plan.Swaps[0]) &&
            InventoryApplicationConfirmation.IsSwapObserved(swapped, plan.Swaps[0]) &&
            !InventoryApplicationConfirmation.MatchesTarget(source, source, target) &&
            InventoryApplicationConfirmation.MatchesTarget(swapped, source, target),
            "unchanged native IDs must not falsely confirm a pending swap");
        ProjectedInventorySettlement expected = InventorySettlementProjector.Evaluate(source, target);
        Require(!InventorySettlementDifferentialVerifier.Compare(source, target, expected, source).Matched,
            "differential verification must detect an unapplied zero-ID swap");

        InventorySnapshot wrongQuantity = Snapshot(CompanionKeys, levels, new[] { 1, 0, 2 }, firstQuantity: 2);
        Require(!InventoryApplicationConfirmation.MatchesTarget(wrongQuantity, source, target) &&
            !InventorySettlementDifferentialVerifier.Compare(source, target, expected, wrongQuantity).Matched,
            "identity matching must also preserve item quantities");
        InventorySnapshot fewerItems = Snapshot(CompanionKeys.Take(2).ToArray(), levels, new[] { 1, 0 });
        Require(!InventoryApplicationConfirmation.MatchesTarget(fewerItems, source, target) &&
            !InventorySettlementDifferentialVerifier.Compare(source, target, expected, fewerItems).Matched,
            "missing items must fail final confirmation");

        InventoryItemKey[] sameEntityKeys = { new(5004, 21), new(5004, 22), new(5004, 23) };
        InventorySnapshot sameEntitySource = Snapshot(sameEntityKeys, levels, new[] { 0, 1, 2 });
        Require(sameEntitySource.SettlementValidation.LayoutProjectionReady &&
            InventoryLayoutPlanner.TryCreate(sameEntitySource, target, out plan, out _) &&
            !InventoryApplicationConfirmation.IsSwapObserved(sameEntitySource, plan.Swaps[0]) &&
            InventoryApplicationConfirmation.IsSwapObserved(
                Snapshot(sameEntityKeys, levels, new[] { 1, 0, 2 }), plan.Swaps[0]),
            "same-entity copies must remain distinct through their native IDs");
    }

    private static void VerifyTabletIdentity()
    {
        InventorySnapshot source = Tablets(0, 0);
        var target = new InventoryLayoutProjection(new[] { 0, 1 }, new[] { 0, 1 });
        Require(source.SettlementValidation.LayoutProjectionReady &&
            InventoryLayoutPlanner.TryCreate(source, target, out InventoryApplicationPlan plan, out _) &&
            plan.Rotations.Count == 1 && plan.Rotations[0].ItemKey == new InventoryItemKey(6002, 0),
            "rotation planning must distinguish tablet entities sharing a native ID");
        var rotation = new InventoryRotationOperation(new InventoryItemKey(6002, 0), 1, 1);
        InventorySnapshot actual = Tablets(0, 1);
        Require(!InventoryApplicationConfirmation.IsRotationStepObserved(Tablets(1, 0), rotation, 0) &&
            InventoryApplicationConfirmation.IsRotationStepObserved(actual, rotation, 0),
            "rotating the other zero-ID tablet must not acknowledge this rotation");
        ProjectedInventorySettlement expected = InventorySettlementProjector.Evaluate(source, target);
        Require(expected.Tablets.Count == 3 &&
            InventorySettlementDifferentialVerifier.Compare(source, target, expected, actual).Matched,
            "movable tablets and a fixed source sharing an item key must retain separate settlements");
    }

    private static InventorySnapshot Tablets(int firstRotation, int secondRotation)
    {
        InventorySnapshot template = InventorySnapshotFixture.ArtifactsAtLevels(new[] { 0, 0 }, Array.Empty<int>());
        TabletRotationProjectionSnapshot[] rotations = Enumerable.Range(0, 4).Select(rotation =>
            new TabletRotationProjectionSnapshot(rotation, Array.Empty<TabletAdditionSnapshot>(),
                Array.Empty<TabletAdditionSnapshot>(), true)).ToArray();
        TabletPlacementProjectionSnapshot[] placements = Enumerable.Range(0, 2).Select(cell =>
            new TabletPlacementProjectionSnapshot(cell, cell, 0, rotations)).ToArray();
        InventoryItemSnapshot[] items = new[] { firstRotation, secondRotation }.Select((rotation, cell) =>
            new InventoryItemSnapshot(0, 6001 + cell, 1, cell, cell, 0, "Tablet", string.Empty,
                "StoneTablet", "Normal", Array.Empty<string>(), InventoryItemKind.StoneTablet, null,
                new StoneTabletSnapshot(rotation, true, false, true, false, string.Empty, string.Empty,
                    placementProjections: placements))).ToArray();
        return new InventorySnapshot(2, 2, template.Cells.ToArray(), items,
            fixedTabletSources: new[] { new FixedTabletSourceSnapshot(0, 6001, 0, 0, true, rotations[0]) });
    }

    private static void VerifyIdentityConflicts()
    {
        int[] levels = { 0, 0, 0 };
        InventorySnapshot duplicate = Snapshot(
            new[] { CompanionKeys[0], CompanionKeys[0], CompanionKeys[2] }, levels, new[] { 0, 1, 2 });
        Require(!duplicate.SettlementValidation.LayoutProjectionReady &&
            duplicate.SettlementValidation.HasItemIdentityConflict &&
            duplicate.SettlementValidation.Issues.Contains("SnapshotItemIdentityDuplicate:5004:0:Cells=0,1"),
            "a complete identity collision must fail with the conflicting cells");
        var current = InventoryLayoutProjection.Current(duplicate);
        Require(!InventoryLayoutPlanner.TryCreate(duplicate, current, out _, out _),
            "the planner must not collapse a genuine identity collision");
        InventorySnapshot source = Snapshot(CompanionKeys, levels, new[] { 0, 1, 2 });
        ProjectedInventorySettlement expected = InventorySettlementProjector.Evaluate(source, current);
        Require(!InventoryApplicationConfirmation.MatchesTarget(duplicate, source, current) &&
            !InventorySettlementDifferentialVerifier.Compare(source, current, expected, duplicate).Matched,
            "observation must reject duplicate complete identities");
    }

    private static void VerifyIndependentIntentAndLifecycle()
    {
        InventoryOptimizationPreferences preferences = InventoryOptimizationPreferences.Default;
        foreach (InventoryItemKey key in CompanionKeys)
        {
            preferences = InventoryArtifactIntentEditor.Toggle(preferences, key.NativeInstanceId, key.EntityId);
        }
        Require(InventoryArtifactIntentEditor.Count(preferences) == 3,
            "each companion must have its own priority mark");
        preferences = InventoryArtifactIntentEditor.PlacePriority(preferences, 0, 5006, 0);
        preferences = InventoryArtifactIntentEditor.PlaceAvoid(preferences, 0, 5005, 0);
        Require(InventoryArtifactIntentEditor.OrderedPriorities(preferences).Select(rule => rule.ItemKey)
                .SequenceEqual(new[] { CompanionKeys[2], CompanionKeys[0] }) &&
            InventoryArtifactIntentEditor.AvoidedInstances(preferences).Single().ItemKey == CompanionKeys[1],
            "reordering and exclusion must affect only the selected companion");
        InventoryOptimizationPreferences composed = InventoryOptimizationPreferenceComposer.Compose(
            InventoryOptimizationPreferences.Default, preferences, InventorySearchEffort.Fast, true);
        Require(composed.ArtifactPreferences.Count == 3,
            "composing rules must not merge different zero-ID artifacts");
        InventoryOptimizationPreferences removed = InventoryArtifactIntentEditor.Remove(preferences, CompanionKeys[0]);
        Require(removed.ArtifactPreferences.Count == 2 &&
            InventoryArtifactIntentEditor.IsMarked(removed, CompanionKeys[2]),
            "removing one zero-ID mark must preserve the other marks");
        InventoryOptimizationPreferences pruned = InventoryArtifactIntentEditor.Prune(preferences,
            new[] { CompanionKeys[0], CompanionKeys[2] });
        Require(pruned.ArtifactPreferences.Count == 2 &&
            InventoryArtifactIntentEditor.AvoidedInstances(pruned).Length == 0,
            "leaving the inventory must prune only that exact artifact's intent");
        preferences = InventoryArtifactIntentEditor.Toggle(preferences, 0, 5004);
        Require(preferences.ArtifactPreferences.Count == 2 &&
            !InventoryArtifactIntentEditor.IsMarked(preferences, CompanionKeys[0]),
            "toggling one zero-ID mark must not clear its siblings");

        var interaction = new InventoryIntentInteractionState();
        interaction.SetEditable(true);
        interaction.TryPickup(composed.ArtifactPreferences.Single(rule => rule.ItemKey == CompanionKeys[0]), false);
        Require(interaction.ItemKey == CompanionKeys[0], "pickup must retain the entity");
        interaction.CancelPickup();
        interaction.TryPickup(composed.ArtifactPreferences.Single(rule => rule.ItemKey == CompanionKeys[1]), false);
        Require(interaction.ItemKey == CompanionKeys[1], "zero-ID pickups must remain distinct");
        interaction.SetEditable(false);
        Require(interaction.ItemKey == null, "suspending editing must discard pickup identity");
        ExplorationInventoryIntentStore.Replace(composed);
        ExplorationInventoryIntentStore.Clear();
        Require(ExplorationInventoryIntentStore.Capture().ArtifactPreferences.Count == 0 &&
            InventoryOptimizationPreferencesCodec.Encode(composed) ==
                InventoryOptimizationPreferencesCodec.Encode(InventoryOptimizationPreferences.Default),
            "instance intent must reset with exploration and stay out of persistent settings");
    }

    private static void VerifyEvaluationOrder()
    {
        InventoryItemKey[] captured = (InventoryItemKey[])CompanionKeys.Clone();
        var order = new InventoryEvaluationOrderSnapshot(1, captured, captured,
            captured.Select(key => new UniqueEffectRegistrationSnapshot(
                key.NativeInstanceId, key.EntityId, true)).ToArray());
        captured[0] = captured[1];
        Require(order.CategoryRefreshItemKeys.SequenceEqual(CompanionKeys) &&
            order.ArtifactRefreshItemKeys.SequenceEqual(CompanionKeys) &&
            order.UniqueRegistrations.Select(item => item.ItemKey).SequenceEqual(CompanionKeys),
            "immutable evaluation traces must distinguish all zero-ID artifacts");
    }

    private static InventorySnapshot Snapshot(InventoryItemKey[] keys, int[] levels,
        int[] itemCells, int firstQuantity = 1)
    {
        InventorySnapshot template = InventorySnapshotFixture.ArtifactsAtLevels(levels, itemCells);
        InventoryItemSnapshot[] items = template.Items.Select((item, index) =>
            new InventoryItemSnapshot(keys[index].NativeInstanceId, keys[index].EntityId,
                index == 0 ? firstQuantity : 1, item.CellIndex, item.X, item.Y,
                item.Name, item.NameKey, item.NativeItemTypeName, item.Rarity,
                item.BaseCategories.ToArray(), item.Kind, item.Artifact, item.StoneTablet))
            .OrderBy(item => item.CellIndex).ToArray();
        return new InventorySnapshot(template.Width, template.Storage, template.Cells.ToArray(), items);
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
