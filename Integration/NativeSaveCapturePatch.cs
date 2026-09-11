using SephiriaEnhancements.Runtime;
using SephiriaEnhancements.Runtime.GameBridge.Inventory;
using HarmonyLib;
using SephiriaEnhancements.DefeatRetry;

namespace SephiriaEnhancements.Integration
{
    [HarmonyPatch(typeof(SaveManager), nameof(SaveManager.Save),
        new[] { typeof(bool), typeof(bool) })]
    internal static class NativeSaveCapturePatch
    {
        private static void Prefix(bool saveCurrentRun)
        {
            if (!FeatureFailure.IsAvailable(FeatureId.DefeatRetry))
            {
                return;
            }

            try
            {
                PrefixCore(saveCurrentRun);
            }
            catch (System.Exception exception)
            {
                FeatureFailure.Disable(FeatureId.DefeatRetry, exception);
                return;
            }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static void PrefixCore(bool saveCurrentRun)
        {
            if (saveCurrentRun)
            {
                DefeatRetryFeature.CaptureFloorEntryCheckpoint();
            }
        }

    }

    [HarmonyPatch(typeof(SaveManager), nameof(SaveManager.Save),
        new[] { typeof(bool), typeof(bool) })]
    internal static class NativePresetSavePatch
    {
        private static void Postfix(bool saveCurrent, bool saveCurrentRun)
        {
            if (!FeatureFailure.IsAvailable(FeatureId.Inventory))
            {
                return;
            }

            try
            {
                PostfixCore(saveCurrent, saveCurrentRun);
            }
            catch (System.Exception exception)
            {
                FeatureFailure.Disable(FeatureId.Inventory, exception);
                return;
            }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static void PostfixCore(bool saveCurrent, bool saveCurrentRun)
        {
            if (saveCurrent && !saveCurrentRun)
            {
                NativePresetChangeSignal.MarkChanged();
            }
        }
    }
}
