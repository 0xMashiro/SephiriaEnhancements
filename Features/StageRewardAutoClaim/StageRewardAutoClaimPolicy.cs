using System;
using System.Collections.Generic;

namespace SephiriaEnhancements.StageRewardAutoClaim
{
    internal static class StageRewardAutoClaimPolicy
    {
        internal static HashSet<string> ReadClaimed(string value) =>
            new((value ?? "").Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries), StringComparer.Ordinal);

        internal static bool CanClaim(int highestClearedTier, int rewardTier,
            bool clearTargetProven, bool talentPointReward, string rewardId, ISet<string> claimed) =>
            highestClearedTier > 0 && rewardTier > 0 && rewardTier <= highestClearedTier &&
            clearTargetProven && talentPointReward && !string.IsNullOrEmpty(rewardId) && !claimed.Contains(rewardId);
    }
}
