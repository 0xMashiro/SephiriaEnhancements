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
            if (saveCurrentRun)
            {
                DefeatRetryFeature.CaptureFloorEntryCheckpoint();
            }
        }

        private static void Postfix(bool saveCurrent, bool saveCurrentRun)
        {
            if (saveCurrent && !saveCurrentRun)
            {
                NativePresetChangeSignal.MarkChanged();
            }
        }
    }
}
