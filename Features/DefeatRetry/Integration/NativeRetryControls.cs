using System;
using Mirror;
using UnityEngine;
using SephiriaEnhancements.AutoCasting.Integration;
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
        private static double deadline;
        private static bool arrived, incomplete;

        internal static void Begin(PlayerAvatar owner, int[] skillArtifacts, double expires)
        {
            player = owner;
            connection = NetworkClient.connection;
            expected = skillArtifacts;
            deadline = expires;
            arrived = incomplete = false;
            try { NativeAutoCasting.Current?.BeginRetry(); }
            catch (Exception exception) { RecordFailure(exception); }
            try { layout = new NativeRetrySkillLayout(owner.GetComponent<IntegratedActionController>(), owner.Inventory); }
            catch (Exception exception) { RecordFailure(exception); }
        }

        internal static void Arrive() => arrived = true;

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
                bool ready = NativeRetrySkillLayout.IsReady(player.Inventory, expected);
                if (!ready && Time.realtimeSinceStartupAsDouble < deadline) return;
                incomplete |= !ready;
                if (layout != null)
                    incomplete |= layout.Restore(player.GetComponent<IntegratedActionController>(), player.Inventory, expected) != 0;
            }
            catch (Exception exception) { RecordFailure(exception); }
            // Release auto casting even if the layout/UI refresh failed. Its selections
            // still belong to item IDs; absent/unbound skills cannot cast.
            try { NativeAutoCasting.Current?.CompleteRetry(); }
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
            try { NativeAutoCasting.Current?.CancelRetry(); }
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
            arrived = incomplete = false;
        }
    }
}
