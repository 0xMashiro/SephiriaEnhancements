using SephiriaEnhancements.Runtime.Inventory;
using SephiriaEnhancements.Inventory;

namespace SephiriaEnhancements.ModelChecks.Features.Inventory;

internal static class InventoryArtifactIntentEditorChecks
{
    internal static string Run()
    {
        InventoryOptimizationPreferences original =
            InventoryOptimizationPreferences.Default;
        InventoryOptimizationPreferences marked =
            InventoryArtifactIntentEditor.Toggle(original, 501, 10);
        ArtifactOptimizationPreference rule = marked.ArtifactPreferences.
            Single();
        if (!rule.TargetsInstance || rule.InstanceId != 501 ||
            rule.EntityId != 10 ||
            rule.Level != InventoryPreferenceLevel.Priority ||
            rule.MinimumEffectiveLevel != 1 ||
            rule.PriorityOrder != 0 ||
            !InventoryArtifactIntentEditor.IsMarked(marked, new InventoryItemKey(10, 501)) ||
            InventoryArtifactIntentEditor.Count(marked) != 1)
        {
            throw new InvalidOperationException(
                "temporary priority marks must target one artifact instance");
        }

        InventoryOptimizationPreferences unmarked =
            InventoryArtifactIntentEditor.Toggle(marked, 501, 10);
        if (unmarked.ArtifactPreferences.Count != 0 ||
            InventoryArtifactIntentEditor.IsMarked(unmarked, new InventoryItemKey(10, 501)))
        {
            throw new InvalidOperationException(
                "toggling a marked artifact must restore automatic behavior");
        }

        InventoryOptimizationPreferences twoMarks =
            InventoryArtifactIntentEditor.Toggle(
                InventoryArtifactIntentEditor.Toggle(original, 501, 10),
                502, 10);
        InventoryOptimizationPreferences reordered =
            InventoryArtifactIntentEditor.PlacePriority(twoMarks, 502, 10, 0);
        ArtifactOptimizationPreference[] ordered =
            InventoryArtifactIntentEditor.OrderedPriorities(reordered);
        if (ordered.Length != 2 || ordered[0].InstanceId != 502 ||
            ordered[0].PriorityOrder != 0 ||
            ordered[1].InstanceId != 501 || ordered[1].PriorityOrder != 1)
        {
            throw new InvalidOperationException(
                "priority queue placement must define a stable order");
        }
        InventoryOptimizationPreferences avoided =
            InventoryArtifactIntentEditor.PlaceAvoid(reordered, 501, 10, 0);
        if (InventoryArtifactIntentEditor.OrderedPriorities(avoided).Length != 1 ||
            InventoryArtifactIntentEditor.AvoidedInstances(avoided).Single().
                InstanceId != 501)
        {
            throw new InvalidOperationException(
                "moving an artifact to exclusion must remove it from priority");
        }
        InventoryOptimizationPreferences pruned =
            InventoryArtifactIntentEditor.Prune(twoMarks, new[] { new InventoryItemKey(10, 502) });
        if (pruned.ArtifactPreferences.Count != 1 ||
            !InventoryArtifactIntentEditor.IsMarked(pruned, new InventoryItemKey(10, 502)) ||
            InventoryArtifactIntentEditor.IsMarked(pruned, new InventoryItemKey(10, 501)))
        {
            throw new InvalidOperationException(
                "marks for artifact instances outside the inventory must be removed");
        }

        return "ordered priority, exclusion and stale intent pruning passed";
    }
}
