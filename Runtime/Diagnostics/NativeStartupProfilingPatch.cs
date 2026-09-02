using SephiriaEnhancements.Runtime.Inventory;
#if SEPHIRIA_ENHANCEMENTS_DEVTOOLS
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using HarmonyLib;

namespace SephiriaEnhancements.Diagnostics
{
    [HarmonyPatch]
    internal static class NativeStartupProfilingPatch
    {
        private static IEnumerable<MethodBase> TargetMethods()
        {
            yield return Initialize(typeof(RaceDatabase));
            yield return Initialize(typeof(QuestDatabase));
            yield return Initialize(typeof(FactionDatabase));
            yield return Initialize(typeof(UnitDatabase));
            yield return Initialize(typeof(FarmAbilityDatabase));
            yield return Initialize(typeof(MiracleDatabase));
            yield return Initialize(typeof(ActiveSkillDatabase));
            yield return Initialize(typeof(WeaponSkillDatabase));
            yield return Initialize(typeof(StatusDatabase));
            yield return Initialize(typeof(CostumeDatabase));
            yield return Initialize(typeof(WeaponDatabase));
            yield return Initialize(typeof(KeywordDatabase));
            yield return Initialize(typeof(ItemDatabase));
            yield return Initialize(typeof(PropDatabase));
            yield return Initialize(typeof(TileDatabase));
            yield return Initialize(typeof(StatusEffectDatabase));
            yield return Initialize(typeof(SocialIDDatabase));
            yield return Initialize(typeof(AvatarSpawnDatabase));
            yield return Initialize(typeof(TreeShopItemDatabase));
            yield return Initialize(typeof(UIMinimapElementImageDatabase));
            yield return Initialize(typeof(SwitchDatabase));
            yield return Initialize(typeof(KeyDatabase));
            yield return Initialize(typeof(EffectHUDDatabase));
            yield return Initialize(typeof(PassiveDatabase));
            // Native API spelling is preserved only at this integration boundary.
            yield return Initialize(typeof(HardModeDatebase));
            yield return Initialize(typeof(LoreDatabase));
            yield return Initialize(typeof(MiniscriptGlobal));
            yield return Initialize(typeof(Safe));
            yield return Initialize(typeof(DamageInstance));
            yield return Initialize(typeof(TopdownSpatialHash));
            yield return Initialize(typeof(TopdownRigidbody));
        }

        private static void Prefix(out long __state)
        {
            __state = Stopwatch.GetTimestamp();
        }

        private static Exception Finalizer(MethodBase __originalMethod,
            long __state, Exception __exception)
        {
            DeveloperLogger.RecordGameStartupOperation(
                __originalMethod.DeclaringType.Name + ".Initialize",
                ElapsedMilliseconds(__state), __exception == null);
            return __exception;
        }

        private static MethodBase Initialize(Type type)
        {
            MethodInfo method = AccessTools.DeclaredMethod(type, "Initialize",
                Type.EmptyTypes);
            if (method == null)
            {
                throw new MissingMethodException(type.FullName, "Initialize");
            }

            return method;
        }

        private static float ElapsedMilliseconds(long startedAt)
        {
            return (float)((Stopwatch.GetTimestamp() - startedAt) * 1000d /
                Stopwatch.Frequency);
        }
    }
}
#endif
