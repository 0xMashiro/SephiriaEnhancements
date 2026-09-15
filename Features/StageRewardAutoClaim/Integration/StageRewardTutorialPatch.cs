using HarmonyLib;
using SephiriaEnhancements.Runtime;

namespace SephiriaEnhancements.StageRewardAutoClaim.Integration
{
    [HarmonyPatch(typeof(LocalizationManager), nameof(LocalizationManager.GetText))]
    internal static class StageRewardTutorialPatch
    {
        private static void Postfix(LocalizationManager __instance, string __0, string __1, ref string __result)
        {
            if (!FeatureFailure.IsAvailable(FeatureId.StageRewardAutoClaim)) return;
            try { PostfixCore(__instance, __0, __1, ref __result); }
            catch (System.Exception exception) { FeatureFailure.Disable(FeatureId.StageRewardAutoClaim, exception); }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static void PostfixCore(LocalizationManager manager, string language, string key, ref string result)
        {
            if (key == "UI_HardModePanelPopup_Text" && NativeStageRewardAutoClaim.Enabled)
                result = manager.GetText(language, StageRewardAutoClaimLocalization.Tutorial);
        }
    }
}
