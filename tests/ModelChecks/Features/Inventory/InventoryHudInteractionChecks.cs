using SephiriaEnhancements.Inventory;
using SephiriaEnhancements.ModelChecks.Runtime.Inventory;
using SephiriaEnhancements.Runtime.Inventory;
using Layout = SephiriaEnhancements.Inventory.InventoryOptimizationHudLayout;

namespace SephiriaEnhancements.ModelChecks.Features.Inventory;

internal static class InventoryHudInteractionChecks
{
    internal static string Run()
    {
        VerifyDisclosureLayout();
        VerifySelectionLifecycle();
        VerifyPagesAndReordering();
        VerifySlotSwaps();
        VerifyLevelEditAndPickupAreExclusive();
        VerifyArtifactGoalEdits();
        VerifyComboGoalEdits();
        return "HUD bounds, click/drag pickup lifecycle, sparse slots, swaps and target preservation passed";
    }

    private static void VerifyLevelEditAndPickupAreExclusive()
    {
        var priority = new ArtifactOptimizationPreference(501, 10, InventoryPreferenceLevel.Priority, 4, 0);
        var avoided = new ArtifactOptimizationPreference(502, 10, InventoryPreferenceLevel.Avoid, 0, 0);
        var state = new InventoryIntentInteractionState();
        if (state.TryEditLevel(priority)) throw new InvalidOperationException("inactive HUD cannot edit levels");
        state.SetEditable(true);
        if (!state.TryEditLevel(avoided) || state.LevelTarget != avoided.ItemKey)
            throw new InvalidOperationException("avoid marks must expose their Hard/Soft setting through the same editor");
        if (state.TryEditLevel(null) || !state.TryEditLevel(priority) ||
            state.LevelTarget != priority.ItemKey || state.HasPickup)
            throw new InvalidOperationException("a marked artifact may open the target editor without picking up the mark");
        if (!state.TryEditLevel(priority) || state.LevelTarget != null)
            throw new InvalidOperationException("repeating the level action on the same mark must close its editor");
        foreach (bool dragging in new[] { false, true })
        {
            state.TryEditLevel(priority);
            if (!state.TryPickup(priority, dragging) || state.LevelTarget != null || state.TryEditLevel(priority))
                throw new InvalidOperationException("click pickup and drag must close level editing and block re-entry while holding a mark");
            state.CancelPickup();
        }
        state.TryEditLevel(priority);
        state.SetEditable(false);
        if (state.LevelTarget != null || state.HasPickup)
            throw new InvalidOperationException("suspending inventory interaction must clear transient editing state");
    }

    private static void VerifyArtifactGoalEdits()
    {
        var snapshot = InventorySnapshotFixture.ArtifactsAtLevels(new[] { 2, 0 }, new[] { 0, 1 }, maxLevel: 4);
        var preferences = InventoryArtifactIntentEditor.PlacePriority(InventoryOptimizationPreferences.Default, 100, 1000, 0);
        var state = new InventoryIntentInteractionState();
        state.SetEditable(true);
        state.TryEditLevel(preferences.ArtifactPreferences.Single());
        void Edit(InventoryArtifactGoalEdit edit)
        {
            if (!state.TryEditArtifactGoal(preferences, snapshot, edit, out var updated))
                throw new InvalidOperationException("active goal edit was rejected");
            preferences = updated;
        }
        void Expect(ArtifactLevelTargetMode mode, int level, InventoryConstraintStrength strength)
        {
            var rule = preferences.ArtifactPreferences.Single();
            if (rule.TargetMode != mode || rule.MinimumEffectiveLevel != level || rule.Strength != strength)
                throw new InvalidOperationException("goal edit changed the wrong condition");
        }
        Edit(InventoryArtifactGoalEdit.CycleTargetMode);
        Expect(ArtifactLevelTargetMode.ActiveOnly, 0, InventoryConstraintStrength.Soft);
        Edit(InventoryArtifactGoalEdit.CycleTargetMode);
        Expect(ArtifactLevelTargetMode.SpecifiedLevel, 2, InventoryConstraintStrength.Soft);
        Edit(InventoryArtifactGoalEdit.ToggleStrength);
        Edit(InventoryArtifactGoalEdit.IncreaseLevel);
        Edit(InventoryArtifactGoalEdit.IncreaseLevel);
        Edit(InventoryArtifactGoalEdit.IncreaseLevel);
        Expect(ArtifactLevelTargetMode.SpecifiedLevel, 4, InventoryConstraintStrength.Hard);
        Edit(InventoryArtifactGoalEdit.DecreaseLevel);
        Expect(ArtifactLevelTargetMode.SpecifiedLevel, 3, InventoryConstraintStrength.Hard);
        Edit(InventoryArtifactGoalEdit.CycleTargetMode);
        Expect(ArtifactLevelTargetMode.Automatic, 3, InventoryConstraintStrength.Hard);
        // The selected key stays stable when its rule is replaced or moved to another slot.
        preferences = InventoryArtifactIntentEditor.PlaceAvoid(preferences, 100, 1000, 2);
        Edit(InventoryArtifactGoalEdit.ToggleStrength);
        Expect(ArtifactLevelTargetMode.ActiveOnly, 0, InventoryConstraintStrength.Soft);
        foreach (var invalid in new[] { InventoryOptimizationPreferences.Default,
            InventoryArtifactIntentEditor.PlacePriority(InventoryOptimizationPreferences.Default, 101, 1001, 0) })
            if (state.TryEditArtifactGoal(invalid, snapshot, InventoryArtifactGoalEdit.ToggleStrength, out var updated) ||
                !ReferenceEquals(invalid, updated)) throw new InvalidOperationException("stale selection edited a replacement slot");
        if (state.TryEditArtifactGoal(preferences, InventorySnapshotFixture.ArtifactsAtLevels(new[] { 0 }, Array.Empty<int>()),
                InventoryArtifactGoalEdit.ToggleStrength, out _)) throw new InvalidOperationException("missing artifact was edited");
        state.SetEditable(false);
        if (state.TryEditArtifactGoal(preferences, snapshot, InventoryArtifactGoalEdit.ToggleStrength, out _))
            throw new InvalidOperationException("busy HUD changed an artifact goal");
    }

    private static void VerifyComboGoalEdits()
    {
        var snapshot = new InventorySnapshot(1, 0, Array.Empty<InventoryCellSnapshot>(), Array.Empty<InventoryItemSnapshot>(),
            comboCategories: new[] { new ComboCategorySnapshot("Scholar", 0, 0, 0, 0, 0, new[] { 3 }, Array.Empty<int>(), false) });
        var preferences = InventoryOptimizationPreferences.Default;
        var state = new InventoryIntentInteractionState();
        state.SetEditable(true);
        void Edit(InventoryComboGoalEdit edit)
        {
            if (!state.TryEditComboGoal(preferences, snapshot, "Scholar", edit, out var updated))
                throw new InvalidOperationException("combo edit rejected");
            preferences = updated;
        }
        Edit(InventoryComboGoalEdit.CycleChoice);
        Edit(InventoryComboGoalEdit.IncreaseCount);
        Edit(InventoryComboGoalEdit.IncreaseCount);
        Edit(InventoryComboGoalEdit.ToggleStrength);
        if (preferences.ComboPreferences.Single().TargetCount != 2 ||
            preferences.ComboPreferences.Single().Strength != InventoryConstraintStrength.Hard)
            throw new InvalidOperationException("successive edits reused a stale row projection");
        Edit(InventoryComboGoalEdit.CycleChoice);
        Edit(InventoryComboGoalEdit.DecreaseCount);
        if (preferences.ComboPreferences.Single().Level != InventoryPreferenceLevel.Avoid ||
            preferences.ComboPreferences.Single().TargetCount != 1 ||
            preferences.ComboPreferences.Single().Strength != InventoryConstraintStrength.Hard)
            throw new InvalidOperationException("cycling choice lost the current count or strength");
        Edit(InventoryComboGoalEdit.CycleChoice);
        if (preferences.ComboPreferences.Count != 0 ||
            state.TryEditComboGoal(preferences, snapshot, "Scholar", InventoryComboGoalEdit.IncreaseCount, out _))
            throw new InvalidOperationException("automatic mode retained an editable override");
        state.SetEditable(false);
        if (state.TryEditComboGoal(preferences, snapshot, "Scholar", InventoryComboGoalEdit.CycleChoice, out _))
            throw new InvalidOperationException("busy HUD edited a combo goal");
    }

    private static void VerifyDisclosureLayout()
    {
        // Check visible regions for both mutually exclusive content views.
        var board = new (float Top, float Height)[]
        {
            (Layout.PrioritySlotsTop, Layout.SlotSize),
            (Layout.AvoidSlotsTop, Layout.SlotSize),
            (Layout.BoardPagingTop, Layout.PagingHeight),
            (Layout.HintTop, Layout.HintHeight),
            (Layout.DetailsTop, Layout.DetailsHeight),
            (Layout.ActionsTop, Layout.ActionsHeight)
        };
        VerifyBounds(board, Layout.Height);
        if (Layout.TargetRowsPerPage < 8)
            throw new InvalidOperationException("24 combo targets must fit within three pages");
        // Every page keeps eight slots even when all targets have conditions.
        // Only the selected target expands; include each position and no selection.
        for (int count = 0; count <= Layout.TargetRowsPerPage; count++)
        {
            for (int mask = 0; mask < 1 << count; mask++)
            {
                for (int selected = -1; selected < count; selected++)
                {
                    var regions = new List<(float Top, float Height)>
                    {
                        (Layout.TargetPagingTop, Layout.PagingHeight)
                    };
                    float top = Layout.TargetRowsTop;
                    for (int index = 0; index < count; index++)
                    {
                        var choice = (mask & (1 << index)) == 0 ? InventoryPreferenceChoice.Automatic
                            : index % 2 == 0 ? InventoryPreferenceChoice.Priority : InventoryPreferenceChoice.Avoid;
                        var target = new InventoryComboTarget("target" + index, choice, 0, 10);
                        bool expanded = index == selected && target.CanAdjustRequiredValue;
                        float height = Layout.TargetRowHeight(expanded);
                        if (height < (expanded ? 52f : 26f))
                            throw new InvalidOperationException("target rows must fit their visible controls");
                        regions.Add((top, height));
                        top += height + Layout.TargetRowGap;
                    }
                    regions.Add((Layout.DetailsTop, Layout.DetailsHeight));
                    regions.Add((Layout.ActionsTop, Layout.ActionsHeight));
                    VerifyBounds(regions, Layout.Height);
                }
            }
        }
    }

    private static void VerifyBounds(IEnumerable<(float Top, float Height)> regions, float panelHeight)
    {
        float bottom = 0;
        foreach (var region in regions)
        {
            if (region.Top < bottom || region.Top + region.Height > panelHeight)
                throw new InvalidOperationException("visible HUD regions must not overlap or leave the panel");
            bottom = region.Top + region.Height;
        }
    }

    private static void VerifySelectionLifecycle()
    {
        var source = new ArtifactOptimizationPreference(501, 10,
            InventoryPreferenceLevel.Priority, 4, 0);
        var destination = new ArtifactOptimizationPreference(502, 10,
            InventoryPreferenceLevel.Priority, 1, 8);
        var original = new InventoryOptimizationPreferences(InventorySearchEffort.Balanced,
            true, new[] { source, destination }, Array.Empty<ComboOptimizationPreference>());
        foreach (bool dragging in new[] { false, true })
        {
            var state = new InventoryIntentInteractionState();
            if (state.TryPickup(source, dragging))
            {
                throw new InvalidOperationException("a closed HUD cannot pick up a mark");
            }
            state.SetEditable(true);
            if (!state.TryPickup(source, dragging) || state.IsDragging != dragging ||
                state.TryPickup(destination, dragging) || state.ItemKey != source.ItemKey)
            {
                throw new InvalidOperationException("pickup identity must not follow the hovered or rebound slot");
            }
            // The target is on another page; both input paths must use the
            // captured artifact and the destination's absolute slot index.
            if (!state.TryPlace(original, InventoryPreferenceLevel.Priority, 8, true, out var swapped) ||
                state.HasPickup || state.IsDragging)
            {
                throw new InvalidOperationException("placing or swapping must finish both click and drag pickups");
            }
            AssertSlot(swapped, 501, 10, InventoryPreferenceLevel.Priority, 8, 4);
            AssertSlot(swapped, 502, 10, InventoryPreferenceLevel.Priority, 0, 1);
            AssertSlot(original, 501, 10, InventoryPreferenceLevel.Priority, 0, 4);

            state.TryPickup(source, dragging);
            if (!state.TryPlace(original, InventoryPreferenceLevel.Priority, 0, true, out var unchanged) ||
                !ReferenceEquals(unchanged, original) || state.HasPickup)
            {
                throw new InvalidOperationException("placing on the original slot cancels without changing goals");
            }
            state.TryPickup(source, dragging);
            if (!state.TryPlace(original, InventoryPreferenceLevel.Avoid, 11, true, out var moved))
            {
                throw new InvalidOperationException("both pickup paths must place into empty cross-page exclusion slots");
            }
            AssertSlot(moved, 501, 10, InventoryPreferenceLevel.Avoid, 11, 0);

            // Right click and an outside drop cancel without removing the mark.
            state.TryPickup(source, dragging);
            state.CancelPickup();
            if (state.TryPlace(original, InventoryPreferenceLevel.Priority, 1, true, out var cancelled) ||
                !ReferenceEquals(cancelled, original) || !state.Editable)
            {
                throw new InvalidOperationException("cancelled pickups cannot be committed by a late release");
            }
            state.TryPickup(source, dragging);
            state.EndDrag();
            if (state.HasPickup == dragging)
            {
                throw new InvalidOperationException("an end-drag callback must only finish a drag pickup");
            }
            state.CancelPickup();

            foreach (string boundary in new[] { "close", "context", "search", "apply" })
            {
                state.TryPickup(source, dragging);
                state.SetEditable(false);
                state.SetEditable(true);
                if (state.HasPickup || state.IsDragging ||
                    state.TryPlace(original, InventoryPreferenceLevel.Priority, 1, true, out _))
                {
                    throw new InvalidOperationException(boundary + " must invalidate the pickup before re-entry");
                }
            }
            state.TryPickup(source, dragging);
            if (state.TryPlace(original, InventoryPreferenceLevel.Priority, 1, false, out var absent) ||
                !ReferenceEquals(absent, original) || state.HasPickup)
            {
                throw new InvalidOperationException("a removed inventory artifact cannot be placed from a stale pickup");
            }
            var removed = InventoryArtifactIntentEditor.Remove(original, source.ItemKey);
            state.TryPickup(source, dragging);
            if (state.TryPlace(removed, InventoryPreferenceLevel.Priority, 1, true, out var stale) ||
                !ReferenceEquals(stale, removed) || state.HasPickup)
            {
                throw new InvalidOperationException("removing a source mark must prevent its resurrection at drop");
            }
            state.TryPickup(source, dragging);
            if (state.ValidatePickup(swapped, true) || state.HasPickup)
            {
                throw new InvalidOperationException("an externally moved source invalidates the captured pickup");
            }
            var unrelated = InventoryArtifactIntentEditor.PlaceAvoid(original, 503, 11, 2);
            state.TryPickup(source, dragging);
            if (!state.ValidatePickup(unrelated, true) || state.ItemKey != source.ItemKey)
            {
                throw new InvalidOperationException("unrelated goal changes must preserve a valid pickup");
            }
        }
    }

    private static void VerifyPagesAndReordering()
    {
        foreach (var sample in new[] { (0, 0, 1), (5, 0, 1), (6, 1, 2), (0, 12, 3) })
        {
            if (Layout.IntentPageCount(sample.Item1, sample.Item2) != sample.Item3)
            {
                throw new InvalidOperationException("intent pages must expose all marks and an append slot");
            }
        }
        InventoryOptimizationPreferences preferences = InventoryOptimizationPreferences.Default;
        for (int index = 0; index < 14; index++)
        {
            preferences = InventoryArtifactIntentEditor.PlacePriority(preferences, 500 + index, 10, index);
        }
        preferences = InventoryArtifactIntentEditor.Remove(preferences, new InventoryItemKey(10, 507));
        preferences = InventoryArtifactIntentEditor.PlacePriority(preferences, 513, 10, 0);
        ArtifactOptimizationPreference[] ordered = InventoryArtifactIntentEditor.OrderedPriorities(preferences);
        if (ordered.Length != 13 || ordered[0].InstanceId != 513 ||
            ordered.Any(rule => rule.InstanceId == 507) ||
            !ordered.Select(rule => rule.PriorityOrder).SequenceEqual(
                Enumerable.Range(0, 14).Where(index => index != 7)))
        {
            throw new InvalidOperationException("marks beyond the first page must remain removable and reorderable");
        }

        foreach (int targetLevel in new[] { 0, 4 })
        {
            var original = new InventoryOptimizationPreferences(preferences.SearchEffort,
                preferences.AllowStoneTabletRotation,
                new[] { new ArtifactOptimizationPreference(501, 10,
                    InventoryPreferenceLevel.Priority, targetLevel, 0) },
                Array.Empty<ComboOptimizationPreference>());
            var reordered = InventoryArtifactIntentEditor.PlacePriority(original, 501, 10, 2);
            if (reordered.ArtifactPreferences.Single().IntentSlotIndex != 2 ||
                reordered.ArtifactPreferences.Single().MinimumEffectiveLevel != targetLevel ||
                original.ArtifactPreferences.Single().MinimumEffectiveLevel != targetLevel)
            {
                throw new InvalidOperationException("moving a priority must preserve its required level and original preferences");
            }
        }
    }

    private static void VerifySlotSwaps()
    {
        var original = new InventoryOptimizationPreferences(InventorySearchEffort.Balanced,
            true, new[]
            {
                new ArtifactOptimizationPreference(501, 10, InventoryPreferenceLevel.Priority, 4, 0),
                new ArtifactOptimizationPreference(502, 10, InventoryPreferenceLevel.Priority, 0, 3),
                new ArtifactOptimizationPreference(501, 11, InventoryPreferenceLevel.Avoid, 0, 8),
                new ArtifactOptimizationPreference(505, 12, InventoryPreferenceLevel.Priority, 3)
            }, Array.Empty<ComboOptimizationPreference>());
        var swapped = InventoryArtifactIntentEditor.PlacePriority(original, 501, 10, 3);
        AssertSlot(swapped, 501, 10, InventoryPreferenceLevel.Priority, 3, 4);
        AssertSlot(swapped, 502, 10, InventoryPreferenceLevel.Priority, 0, 0);
        AssertSlot(original, 501, 10, InventoryPreferenceLevel.Priority, 0, 4);
        if (!ReferenceEquals(swapped,
            InventoryArtifactIntentEditor.PlacePriority(swapped, 501, 10, 3)))
        {
            throw new InvalidOperationException("dropping back onto the source must be a no-op");
        }
        var crossRow = InventoryArtifactIntentEditor.PlaceAvoid(swapped, 501, 10, 8);
        AssertSlot(crossRow, 501, 10, InventoryPreferenceLevel.Avoid, 8, 0);
        AssertSlot(crossRow, 501, 11, InventoryPreferenceLevel.Priority, 3, 0);
        var moved = InventoryArtifactIntentEditor.PlaceAvoid(crossRow, 501, 10, 11);
        AssertSlot(moved, 501, 10, InventoryPreferenceLevel.Avoid, 11, 0);
        moved = InventoryArtifactIntentEditor.PlaceAvoid(moved, 503, 10, 8);
        moved = InventoryArtifactIntentEditor.PlaceAvoid(moved, 501, 10, 8);
        AssertSlot(moved, 503, 10, InventoryPreferenceLevel.Avoid, 11, 0);
        AssertSlot(moved, 501, 10, InventoryPreferenceLevel.Avoid, 8, 0);
        var replaced = InventoryArtifactIntentEditor.PlaceAvoid(moved, 504, 10, 8);
        AssertSlot(replaced, 504, 10, InventoryPreferenceLevel.Avoid, 8, 0);
        if (replaced.ArtifactPreferences.Any(rule => rule.ItemKey == new InventoryItemKey(10, 501)) ||
            !replaced.ArtifactPreferences.Contains(original.ArtifactPreferences.Last()))
        {
            throw new InvalidOperationException("new references replace only the destination mark and preserve entity goals");
        }
        var pruned = InventoryArtifactIntentEditor.Prune(moved,
            new[] { new InventoryItemKey(10, 503) });
        AssertSlot(pruned, 503, 10, InventoryPreferenceLevel.Avoid, 11, 0);
        if (InventoryArtifactIntentEditor.SlotCount(
                InventoryArtifactIntentEditor.AvoidedInstances(pruned)) != 12)
        {
            throw new InvalidOperationException("pruning must retain the occupied page and exact slot");
        }
    }

    private static void AssertSlot(InventoryOptimizationPreferences preferences,
        int instanceId, int entityId, InventoryPreferenceLevel level, int index, int minimum)
    {
        var rule = preferences.ArtifactPreferences.Single(candidate =>
            candidate.ItemKey == new InventoryItemKey(entityId, instanceId));
        if (rule.Level != level || rule.IntentSlotIndex != index ||
            rule.MinimumEffectiveLevel != minimum ||
            rule.PriorityOrder != (level == InventoryPreferenceLevel.Priority ? index : -1))
        {
            throw new InvalidOperationException("moving a mark must preserve identity, exact slot and priority semantics");
        }
    }
}
