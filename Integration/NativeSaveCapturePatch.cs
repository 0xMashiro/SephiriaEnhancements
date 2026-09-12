using SephiriaEnhancements.Runtime;
using SephiriaEnhancements.Runtime.GameBridge.Inventory;
using HarmonyLib;
using SephiriaEnhancements.DefeatRetry;
using System.Collections.Generic;
using System.Reflection;

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
            if (saveCurrent)
            {
                NativePresetChangeSignal.MarkChanged();
            }
        }
    }

    [HarmonyPatch]
    internal static class NativePresetEditPatch
    {
        private static IEnumerable<MethodBase> TargetMethods()
        {
            yield return AccessTools.Method(typeof(SaveData), nameof(SaveData.SetBool));
            yield return AccessTools.Method(typeof(SaveData), nameof(SaveData.SetInt));
            yield return AccessTools.Method(typeof(SaveData), nameof(SaveData.SetString));
        }

        private static void Postfix(SaveData __instance, string key)
        {
            if (!FeatureFailure.IsAvailable(FeatureId.Inventory)) return;
            try
            {
                PostfixCore(__instance, key);
            }
            catch (System.Exception exception)
            {
                FeatureFailure.Disable(FeatureId.Inventory, exception);
            }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static void PostfixCore(SaveData instance, string key)
        {
            if (ReferenceEquals(instance, SaveManager.Current))
                NativePresetChangeSignal.ObserveCurrentSetupWrite(key);
        }
    }
}
