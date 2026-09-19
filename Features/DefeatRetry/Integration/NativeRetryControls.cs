using System;
using System.Collections.Generic;
using Mirror;
using UnityEngine;
using SephiriaEnhancements.Runtime.GameBridge;
using SephiriaEnhancements.Runtime.Inventory;
using SephiriaEnhancements.Diagnostics;
using SephiriaEnhancements.Integration;

namespace SephiriaEnhancements.DefeatRetry
{
    // Local preferences are best effort. They never gate the server's arrival receipt.
    internal static class NativeRetryControls
    {
        private static PlayerAvatar player;
        private static NetworkConnectionToServer connection;
        private static NativeRetrySkillLayout layout;
        private static int[] expected;
        private static InventoryItemKey[] artifacts;
        private static double deadline;
        private static bool arrived, incomplete;

        internal static void Begin(PlayerAvatar owner, int[] skillArtifacts, double expires,
            InventoryItemKey[] artifactKeys, long checkpointId = 0, string floor = null)
        {
            player = owner;
            connection = NetworkClient.connection;
            expected = skillArtifacts;
            artifacts = artifactKeys;
            deadline = expires;
            arrived = incomplete = false;
            NativeLocalPlayerData.BeginRestore(checkpointId, floor ?? owner.NetworkcurrentFloorGuid);
            try { layout = new NativeRetrySkillLayout(owner.GetComponent<IntegratedActionController>(), owner.Inventory); }
            catch (Exception exception) { RecordFailure(exception); }
        }

        internal static void Arrive()
        {
            NativeLocalPlayerData.LoadProgress();
            arrived = true;
        }

        internal static void Tick()
        {
            if (player == null) { Cancel(); return; }
            if (!NetworkClient.active || connection != NetworkClient.connection || !LocalPlayerResolver.IsLocal(player))
            {
                Cancel();
                return;
            }
            if (!arrived) return;
            try
            {
                var present = new HashSet<InventoryItemKey>();
                foreach (Charm_Basic artifact in player.Inventory.charms.Values)
                    if (artifact.Item != null) present.Add(new InventoryItemKey(artifact.Item.EntityID, artifact.Item.InstanceID));
                bool ready = present.IsSupersetOf(artifacts) && NativeRetrySkillLayout.IsReady(player.Inventory, expected);
                if (!ready && Time.realtimeSinceStartupAsDouble < deadline) return;
                incomplete |= !ready;
                if (layout != null)
                    incomplete |= layout.Restore(player.GetComponent<IntegratedActionController>(), player.Inventory, expected) != 0;
            }
            catch (Exception exception) { RecordFailure(exception); }
            // Data loading is independent of native quick-slot presentation.
            try { NativeLocalPlayerData.CompleteRestore(); }
            catch (Exception exception) { RecordFailure(exception); }
            bool notify = incomplete;
            Reset();
            if (notify)
            {
                SupportLogger.Record("retry_controls_incomplete", "Local controls could not be fully restored.", "WARN");
                NativeModNotifications.Short(RetryRecoveryLocalization.ControlsIncomplete);
            }
        }

        internal static void Cancel()
        {
            if (ReferenceEquals(player, null)) return;
            Reset();
            try { NativeLocalPlayerData.CancelRestore(); }
            catch (Exception exception) { SupportLogger.Failure("retry_controls_cancel_failed", exception); }
        }

        private static void RecordFailure(Exception exception)
        {
            incomplete = true;
            SupportLogger.Failure("retry_controls_restore_failed", exception);
        }

        private static void Reset()
        {
            player = null;
            connection = null;
            layout = null;
            expected = null;
            artifacts = null;
            arrived = incomplete = false;
        }
    }
}
