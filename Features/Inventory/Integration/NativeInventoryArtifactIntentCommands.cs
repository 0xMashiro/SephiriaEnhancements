#nullable disable
using System;
using System.Linq;
using SephiriaEnhancements.Runtime.Inventory;

namespace SephiriaEnhancements.Inventory
{
    internal sealed class NativeInventoryArtifactIntentCommands
    {
        private readonly UI_CharacterStatusPanel panel;
        private readonly InventoryIntentInteractionState interaction;
        private readonly Action<InventoryOptimizationPreferences> replace;

        internal NativeInventoryArtifactIntentCommands(UI_CharacterStatusPanel panel,
            InventoryIntentInteractionState interaction, Action<InventoryOptimizationPreferences> replace)
        {
            this.panel = panel;
            this.interaction = interaction;
            this.replace = replace;
        }

        internal bool OwnsArtifact(UI_NewInventoryIcon icon) => icon?.Item?.Charm != null &&
            icon.Inventory == panel?.PlayerAvatar?.Inventory;

        internal bool HasArtifact(InventoryItemKey key)
        {
            GridInventory inventory = panel?.PlayerAvatar?.Inventory;
            if (inventory == null) return false;
            for (int index = 0; index < inventory.CurrentInventoryStorage; index++)
            {
                NewItemOwnInstance item = inventory.FindItem(inventory.IdxToPos(index));
                if (item?.InstanceID == key.NativeInstanceId && item.EntityID == key.EntityId && item.Charm != null)
                    return true;
            }
            return false;
        }

        internal bool TryPickup(ArtifactOptimizationPreference rule, bool dragging) =>
            !NativeInventoryIntentDrop.HasHeldItem && rule != null && HasArtifact(rule.ItemKey) &&
            interaction.TryPickup(rule, dragging);

        internal bool ValidatePickup() => !NativeInventoryIntentDrop.HasHeldItem &&
            interaction.Pickup != null && interaction.ValidatePickup(LocalPlayerInventoryIntentStore.Capture(),
                HasArtifact(interaction.Pickup.ItemKey));

        internal bool TryPlacePickup(InventoryPreferenceLevel level, int index)
        {
            var held = interaction.Pickup;
            if (held == null || !interaction.TryPlace(LocalPlayerInventoryIntentStore.Capture(),
                    level, index, HasArtifact(held.ItemKey), out var preferences)) return false;
            replace(preferences);
            return true;
        }

        internal bool TryPlaceInventoryArtifact(UI_NewInventoryIcon icon, InventoryPreferenceLevel level, int index)
        {
            if (!OwnsArtifact(icon) || !interaction.Editable) return false;
            var item = icon.Item;
            if (!HasArtifact(new InventoryItemKey(item.EntityID, item.InstanceID))) return false;
            var preferences = LocalPlayerInventoryIntentStore.Capture();
            var updated = level == InventoryPreferenceLevel.Priority
                ? InventoryArtifactIntentEditor.PlacePriority(preferences, item.InstanceID, item.EntityID, index)
                : InventoryArtifactIntentEditor.PlaceAvoid(preferences, item.InstanceID, item.EntityID, index);
            replace(updated);
            return true;
        }

        internal bool TryOpenGoal(InventoryItemKey key, out InventoryOptimizationPreferences preferences)
        {
            preferences = LocalPlayerInventoryIntentStore.Capture();
            // Resolve the current rule by item identity, never by a stale page index.
            var rule = preferences.ArtifactPreferences.FirstOrDefault(candidate => candidate.ItemKey == key);
            return !NativeInventoryIntentDrop.HasHeldItem && rule != null && HasArtifact(key) &&
                interaction.TryEditLevel(rule);
        }

        internal bool TryEditGoal(InventorySnapshot snapshot, InventoryArtifactGoalEdit edit,
            out InventoryOptimizationPreferences preferences)
        {
            preferences = null;
            if (NativeInventoryIntentDrop.HasHeldItem || !interaction.LevelTarget.HasValue ||
                !HasArtifact(interaction.LevelTarget.Value) ||
                !interaction.TryEditArtifactGoal(LocalPlayerInventoryIntentStore.Capture(), snapshot, edit, out preferences))
                return false;
            replace(preferences);
            return true;
        }

        internal bool TryRemove(ArtifactOptimizationPreference rule)
        {
            if (!interaction.Editable || interaction.HasPickup || NativeInventoryIntentDrop.HasHeldItem || rule == null)
                return false;
            replace(InventoryArtifactIntentEditor.Remove(LocalPlayerInventoryIntentStore.Capture(), rule.ItemKey));
            return true;
        }
    }
}
