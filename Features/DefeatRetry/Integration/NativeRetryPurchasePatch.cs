using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using SephiriaEnhancements.Runtime;
using SephiriaEnhancements.Integration;

namespace SephiriaEnhancements.DefeatRetry
{
    [HarmonyPatch]
    internal static class NativeRetryPurchasePatch
    {
        // Two purchases execute in compiler-generated confirmation callbacks.
        // Find those callbacks by their native expenditure write, not generated names.
        private static IEnumerable<MethodBase> TargetMethods()
        {
            FieldInfo expenditure = AccessTools.Field(typeof(PlayerLocalDataStorage), "sapphireUseInRun");
            foreach (Type root in new[] { typeof(PocketDimensionShopArm), typeof(CostChoiceGate), typeof(UI_ShopPanel) })
            {
                var matches = new List<MethodBase>();
                foreach (Type type in new[] { root }.Concat(root.GetNestedTypes(BindingFlags.Public | BindingFlags.NonPublic)))
                    foreach (MethodInfo method in AccessTools.GetDeclaredMethods(type))
                        if (method.GetMethodBody() != null && PatchProcessor.GetOriginalInstructions(method)
                            .Any(instruction => instruction.opcode == OpCodes.Stfld && Equals(instruction.operand, expenditure)))
                            matches.Add(method);
                if (matches.Count != 1) throw new InvalidOperationException("Native sapphire purchase boundary changed.");
                yield return matches[0];
            }
        }

        private static bool Prefix()
        {
            if (!FeatureFailure.IsAvailable(FeatureId.DefeatRetry)) return true;
            try { return PrefixCore(); }
            catch (System.Exception exception)
            {
                FeatureFailure.Disable(FeatureId.DefeatRetry, exception);
                return true;
            }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static bool PrefixCore()
        {
            if (!NativeRetryCapture.BlocksPurchases) return true;
            NativeModNotifications.Short(RetryRecoveryLocalization.PurchaseWait);
            return false;
        }
    }
}
