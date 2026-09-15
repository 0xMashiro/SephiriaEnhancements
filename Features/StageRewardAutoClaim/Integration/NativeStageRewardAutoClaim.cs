using System;
using System.Collections.Generic;
using Mirror;
using SephiriaEnhancements.Configuration;
using SephiriaEnhancements.Integration;
using SephiriaEnhancements.Runtime;
using UnityEngine;

namespace SephiriaEnhancements.StageRewardAutoClaim.Integration
{
    internal sealed class NativeStageRewardAutoClaim : MonoBehaviour
    {
        // Native persistent progression keys, shared with the game's reward panel and player initialization.
        internal const string ClaimedKey = "HardModeReceivedReward";
        internal const string HighestTierKey = "HardModeClearedHighestPoint";
        internal static NativeStageRewardAutoClaim Instance { get; private set; }
        private PlayerAvatar pendingPlayer;
        private SaveData pendingSave;
        private PlayerAvatar issuedPlayer;
        private SaveData issuedSave;
        private int issuedPointTarget;
        private int expectedPoints, rewardCount, rewardPoints;
        private float nextCheck, sentAt;

        internal static bool Enabled => EnhancementsSettings.Enabled && StageRewardAutoClaimSettings.Enabled &&
            FeatureFailure.IsAvailable(FeatureId.StageRewardAutoClaim);

        internal void Initialize() => Instance = this;

        internal void ObserveRewardCommand(PlayerAvatar player, string guid)
        {
            if (!LocalPlayerResolver.IsLocal(player)) return;
            var reward = HardModeDatebase.FindRewardByGuid(guid);
            if (reward == null) return;
            if (issuedPlayer != player || !ReferenceEquals(issuedSave, SaveManager.Current))
            {
                issuedPlayer = player;
                issuedSave = SaveManager.Current;
                issuedPointTarget = player.maxPassivePoint;
            }
            // Count the native one-tier award too: it may still be in flight when we send the rest.
            issuedPointTarget = Math.Max(issuedPointTarget, player.maxPassivePoint) + reward.rewardPassivePoint;
        }

        private void Update()
        {
            if (Time.unscaledTime < nextCheck) return;
            nextCheck = Time.unscaledTime + 0.25f;
            FeatureFailure.Run(FeatureId.StageRewardAutoClaim, Tick);
        }

        internal void Tick()
        {
            PlayerAvatar player = LocalPlayerResolver.Resolve();
            if (issuedPlayer != player || !ReferenceEquals(issuedSave, SaveManager.Current) ||
                (player != null && player.maxPassivePoint >= issuedPointTarget))
            {
                issuedPlayer = null;
                issuedSave = null;
                issuedPointTarget = 0;
            }
            if (pendingSave != null)
            {
                if (player == null || player != pendingPlayer || !ReferenceEquals(pendingSave, SaveManager.Current))
                {
                    ClearPending();
                    return;
                }
                if (player.loadingScreenType == -1 && player.maxPassivePoint >= expectedPoints)
                {
                    int count = rewardCount, points = rewardPoints;
                    NativeModNotifications.Important("stage_rewards:" + Guid.NewGuid(), () =>
                        string.Format(ModLocalization.Get(StageRewardAutoClaimLocalization.Claimed), count, points));
                    ClearPending();
                }
                else if (Time.unscaledTime - sentAt >= 10f)
                {
                    NativeModNotifications.Important("stage_rewards_unconfirmed:" + Guid.NewGuid(), () =>
                        ModLocalization.Get(StageRewardAutoClaimLocalization.Unconfirmed));
                    ClearPending();
                }
                return;
            }
            // Wait for the native initial reward list to reach the server before sending additions.
            // These checks also defer work during same-avatar restart and player travel.
            if (!Enabled || player == null || !NetworkClient.ready || !player.isOwned ||
                player.loadingScreenType != -1 || string.IsNullOrEmpty(player.currentCostume) ||
                string.IsNullOrEmpty(player.currentFloorGuid) || SaveManager.Current == null || SaveManager.CurrentRun == null ||
                SaveManager.IsSaving != SaveManager.ESaveState.None || HardModeDatebase.sortedRewardData == null ||
                (ScreenFader.Instance != null && ScreenFader.Instance.IsFading)) return;

            SaveData save = SaveManager.Current;
            int highestTier = save.GetInt(HighestTierKey, 0);
            string original = save.GetString(ClaimedKey, "");
            var claimed = StageRewardAutoClaimPolicy.ReadClaimed(original);
            var additions = new List<HardModeRewardEntity>();
            int pointsToAdd = 0;
            foreach (var reward in HardModeDatebase.FindAllReward())
            {
                // The global highest-tier record proves native clearTargetType 1 only.
                if (!StageRewardAutoClaimPolicy.CanClaim(highestTier, reward.stage, reward.clearTargetType == 1,
                    reward.rewardType == EHardModeRewardType.PassivePoint, reward.guid, claimed)) continue;
                additions.Add(reward);
                claimed.Add(reward.guid);
                pointsToAdd += reward.rewardPassivePoint;
            }
            if (additions.Count == 0) return;

            var ids = new List<string>();
            foreach (var reward in additions) ids.Add(reward.guid);
            // Record first: native initialization can restore all recorded awards after a disconnect.
            // Never resend an increment after a timeout; the native server endpoint is not idempotent.
            save.SetString(ClaimedKey, string.IsNullOrEmpty(original) ? string.Join(",", ids) : original + "," + string.Join(",", ids));
            SaveManager.Save(saveCurrent: true, saveCurrentRun: false);
            pendingPlayer = player;
            pendingSave = save;
            expectedPoints = player.maxPassivePoint + pointsToAdd;
            rewardCount = additions.Count;
            rewardPoints = pointsToAdd;
            sentAt = Time.unscaledTime;
            try
            {
                foreach (var reward in additions) player.CmdReceiveHardModeReward(reward.guid);
                expectedPoints = Math.Max(expectedPoints, issuedPointTarget);
            }
            catch
            {
                NativeModNotifications.Important("stage_rewards_interrupted:" + Guid.NewGuid(), () =>
                    ModLocalization.Get(StageRewardAutoClaimLocalization.Unconfirmed));
                throw;
            }

            var panel = UIManager.Instance?.GetElement<UI_HardModePanel>();
            if (panel != null && panel.IsOpened) panel.OnOpened();
        }

        private void ClearPending() { pendingPlayer = null; pendingSave = null; }
        private void OnDestroy()
        {
            ClearPending();
            if (Instance == this) Instance = null;
        }
    }
}
