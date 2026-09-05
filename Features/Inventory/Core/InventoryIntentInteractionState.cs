#nullable disable
using System;
using System.Linq;
using SephiriaEnhancements.Runtime.Inventory;

namespace SephiriaEnhancements.Inventory
{
    internal enum InventoryArtifactGoalEdit { ToggleStrength, CycleTargetMode, DecreaseLevel, IncreaseLevel }

    internal enum InventoryComboGoalEdit { CycleChoice, DecreaseCount, IncreaseCount, ToggleStrength }

    internal sealed class InventoryIntentInteractionState
    {
        internal bool Editable { get; private set; }
        internal ArtifactOptimizationPreference Pickup { get; private set; }
        internal bool HasPickup => Pickup != null;
        internal bool IsDragging { get; private set; }
        internal InventoryItemKey? ItemKey => Pickup?.ItemKey;
        internal InventoryItemKey? LevelTarget { get; private set; }

        internal void SetEditable(bool editable)
        {
            Editable = editable;
            if (!editable)
            {
                CancelPickup();
                CancelLevelEdit();
            }
        }

        internal bool TryPickup(ArtifactOptimizationPreference source, bool dragging)
        {
            if (!Editable || HasPickup || source?.TargetsInstance != true ||
                source.IntentSlotIndex < 0)
            {
                return false;
            }
            Pickup = source;
            CancelLevelEdit();
            IsDragging = dragging;
            return true;
        }

        internal bool ValidatePickup(InventoryOptimizationPreferences preferences,
            bool artifactPresent)
        {
            if (!Editable || !artifactPresent || Pickup == null ||
                !preferences.ArtifactPreferences.Contains(Pickup))
            {
                CancelPickup();
                return false;
            }
            return true;
        }

        internal bool TryPlace(InventoryOptimizationPreferences preferences,
            InventoryPreferenceLevel level, int index, bool artifactPresent,
            out InventoryOptimizationPreferences updated)
        {
            updated = preferences;
            if (!ValidatePickup(preferences, artifactPresent) || index < 0 ||
                level != InventoryPreferenceLevel.Priority && level != InventoryPreferenceLevel.Avoid)
            {
                return false;
            }
            updated = level == InventoryPreferenceLevel.Priority
                ? InventoryArtifactIntentEditor.PlacePriority(preferences,
                    Pickup.InstanceId, Pickup.EntityId, index)
                : InventoryArtifactIntentEditor.PlaceAvoid(preferences,
                    Pickup.InstanceId, Pickup.EntityId, index);
            CancelPickup();
            return true;
        }

        internal void EndDrag()
        {
            if (IsDragging)
            {
                CancelPickup();
            }
        }

        internal void CancelPickup()
        {
            Pickup = null;
            IsDragging = false;
        }

        internal bool TryEditLevel(ArtifactOptimizationPreference source)
        {
            if (!Editable || HasPickup || source?.TargetsInstance != true ||
                source.IntentSlotIndex < 0)
            {
                return false;
            }
            LevelTarget = LevelTarget == source.ItemKey ? null : source.ItemKey;
            return true;
        }

        internal bool TryEditArtifactGoal(InventoryOptimizationPreferences preferences,
            InventorySnapshot snapshot, InventoryArtifactGoalEdit edit,
            out InventoryOptimizationPreferences updated)
        {
            updated = preferences;
            if (!Editable || HasPickup || !LevelTarget.HasValue) return false;
            var key = LevelTarget.Value;
            var rule = preferences.ArtifactPreferences.FirstOrDefault(candidate => candidate.ItemKey == key);
            var item = snapshot?.Items.FirstOrDefault(candidate => candidate.ItemKey == key);
            if (rule == null || item?.Artifact == null) return false;
            switch (edit)
            {
                case InventoryArtifactGoalEdit.ToggleStrength:
                    updated = InventoryArtifactIntentEditor.SetStrength(preferences, key,
                        rule.Strength == InventoryConstraintStrength.Hard ? InventoryConstraintStrength.Soft : InventoryConstraintStrength.Hard);
                    break;
                case InventoryArtifactGoalEdit.CycleTargetMode:
                    updated = rule.TargetMode == ArtifactLevelTargetMode.Automatic
                        ? InventoryArtifactIntentEditor.SetMinimumEffectiveLevel(preferences, snapshot, key, 0)
                        : rule.TargetMode == ArtifactLevelTargetMode.ActiveOnly && item.Artifact.MaxLevel > 0
                            ? InventoryArtifactIntentEditor.SetMinimumEffectiveLevel(preferences, snapshot, key,
                                Math.Max(1, item.Artifact.LimitedEffectEnabledLevel))
                            : InventoryArtifactIntentEditor.SetAutomatic(preferences, key);
                    break;
                case InventoryArtifactGoalEdit.DecreaseLevel:
                case InventoryArtifactGoalEdit.IncreaseLevel:
                    updated = InventoryArtifactIntentEditor.SetMinimumEffectiveLevel(preferences, snapshot, key,
                        rule.MinimumEffectiveLevel + (edit == InventoryArtifactGoalEdit.IncreaseLevel ? 1 : -1));
                    break;
            }
            return true;
        }

        internal bool TryEditComboGoal(InventoryOptimizationPreferences preferences,
            InventorySnapshot snapshot, string categoryId, InventoryComboGoalEdit edit,
            out InventoryOptimizationPreferences updated)
        {
            updated = preferences;
            if (!Editable || categoryId == null) return false;
            // A visible row is a projection, not the current editable rule.
            var target = InventoryComboTargetEditor.BuildTargets(snapshot, preferences)
                .FirstOrDefault(candidate => candidate.CategoryId == categoryId);
            if (target == null || edit != InventoryComboGoalEdit.CycleChoice && !target.CanAdjustRequiredValue) return false;
            switch (edit)
            {
                case InventoryComboGoalEdit.CycleChoice:
                    updated = InventoryComboTargetEditor.SetChoice(preferences, target,
                        InventoryComboTargetEditor.NextChoice(target.Choice));
                    break;
                case InventoryComboGoalEdit.ToggleStrength:
                    updated = InventoryComboTargetEditor.SetStrength(preferences, target,
                        target.Strength == InventoryConstraintStrength.Hard ? InventoryConstraintStrength.Soft : InventoryConstraintStrength.Hard);
                    break;
                case InventoryComboGoalEdit.DecreaseCount:
                case InventoryComboGoalEdit.IncreaseCount:
                    updated = InventoryComboTargetEditor.SetRequiredValue(preferences, target,
                        target.RequiredValue + (edit == InventoryComboGoalEdit.IncreaseCount ? 1 : -1));
                    break;
            }
            return true;
        }

        internal void CancelLevelEdit() => LevelTarget = null;
    }
}
