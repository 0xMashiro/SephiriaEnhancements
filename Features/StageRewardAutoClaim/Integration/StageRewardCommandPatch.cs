using HarmonyLib;
using SephiriaEnhancements.Runtime;

namespace SephiriaEnhancements.StageRewardAutoClaim.Integration
{
    [HarmonyPatch(typeof(PlayerAvatar), nameof(PlayerAvatar.CmdReceiveHardModeReward))]
    internal static class StageRewardCommandPatch
    {
        private static void Prefix(PlayerAvatar __instance, string guid)
        {
            if (!FeatureFailure.IsAvailable(FeatureId.StageRewardAutoClaim)) return;
            try { PrefixCore(__instance, guid); }
            catch (System.Exception exception) { FeatureFailure.Disable(FeatureId.StageRewardAutoClaim, exception); }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static void PrefixCore(PlayerAvatar player, string guid) =>
            NativeStageRewardAutoClaim.Instance?.ObserveRewardCommand(player, guid);
    }
}
