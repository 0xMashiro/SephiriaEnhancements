using System;
using System.Collections.Generic;
using SephiriaEnhancements.Runtime.Inventory;

namespace SephiriaEnhancements.DefeatRetry
{
    // One owner's required controls. The recovery coordinator owns timing and failure.
    internal sealed class NativeRetryControls
    {
        private readonly NativeRetrySkillLayout layout;
        private readonly int[] expected;
        private readonly InventoryItemKey[] artifacts;
        private bool restored;

        internal NativeRetryControls(PlayerAvatar player, int[] skillArtifacts, InventoryItemKey[] artifactKeys)
        {
            expected = skillArtifacts;
            artifacts = artifactKeys;
            layout = new NativeRetrySkillLayout(player.GetComponent<IntegratedActionController>(), player.Inventory);
        }

        internal bool TryRestore(PlayerAvatar player)
        {
            var present = new HashSet<InventoryItemKey>();
            foreach (Charm_Basic artifact in player.Inventory.charms.Values)
                if (artifact.Item != null) present.Add(new InventoryItemKey(artifact.Item.EntityID, artifact.Item.InstanceID));
            if (!present.IsSupersetOf(artifacts) || !NativeRetrySkillLayout.IsReady(player.Inventory, expected)) return false;
            if (!restored)
            {
                if (layout.Restore(player.GetComponent<IntegratedActionController>(), player.Inventory, expected) != 0)
                    throw new InvalidOperationException("Required retry skill bindings could not be restored.");
                restored = true;
            }
            return NativeRetrySkillLayout.HasCurrentBindings(player.GetComponent<IntegratedActionController>(), player.Inventory);
        }
    }
}
