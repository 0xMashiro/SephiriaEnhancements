#nullable disable
using System.Linq;
using SephiriaEnhancements.Runtime.Inventory;

namespace SephiriaEnhancements.Inventory
{
    internal sealed class InventoryIntentInteractionState
    {
        internal bool Editable { get; private set; }
        internal ArtifactOptimizationPreference Pickup { get; private set; }
        internal bool HasPickup => Pickup != null;
        internal bool IsDragging { get; private set; }
        internal InventoryItemKey? ItemKey => Pickup?.ItemKey;

        internal void SetEditable(bool editable)
        {
            Editable = editable;
            if (!editable)
            {
                CancelPickup();
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
    }
}
