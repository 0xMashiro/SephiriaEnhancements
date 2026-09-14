using HarmonyLib;
using SephiriaEnhancements.Runtime;

namespace SephiriaEnhancements.Integration
{
    [HarmonyPatch(typeof(PlayerAvatar), "AddDealStat")]
    internal static class TrainingDirectDamagePatch
    {
        private static void Postfix(PlayerAvatar __instance, UnitAvatar victim, DamageInstance damage)
        {
            if (!FeatureFailure.IsAvailable(FeatureId.CombatInsights)) return;
            try { PostfixCore(__instance, victim, damage); }
            catch (System.Exception exception) { FeatureFailure.Disable(FeatureId.CombatInsights, exception); }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static void PostfixCore(PlayerAvatar __instance, UnitAvatar victim, DamageInstance damage)
        {
            TrainingDamageStatistics.Instance?.Record(__instance, victim, damage);
        }
    }

    [HarmonyPatch(typeof(PlayerAvatar), "AddDealStatAsLeader")]
    internal static class TrainingCompanionDamagePatch
    {
        private static void Postfix(PlayerAvatar __instance, UnitAvatar victim, UnitAvatar follower, DamageInstance damage)
        {
            if (!FeatureFailure.IsAvailable(FeatureId.CombatInsights)) return;
            try { PostfixCore(__instance, victim, follower, damage); }
            catch (System.Exception exception) { FeatureFailure.Disable(FeatureId.CombatInsights, exception); }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static void PostfixCore(PlayerAvatar __instance, UnitAvatar victim, UnitAvatar follower, DamageInstance damage)
        {
            if (follower != null) TrainingDamageStatistics.Instance?.Record(__instance, victim, damage, follower);
        }
    }

    [HarmonyPatch(typeof(PlayerAvatar), "set_NetworkloadingScreenType")]
    internal static class TrainingPlayerTravelPatch
    {
        private static void Prefix(PlayerAvatar __instance, int value)
        {
            if (!FeatureFailure.IsAvailable(FeatureId.CombatInsights)) return;
            try { PrefixCore(__instance, value); }
            catch (System.Exception exception) { FeatureFailure.Disable(FeatureId.CombatInsights, exception); }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static void PrefixCore(PlayerAvatar __instance, int value)
        {
            // The authoritative setter also covers travel to a rebuilt floor with the same GUID.
            if (value != -1) TrainingDamageStatistics.Instance?.Clear(__instance);
        }
    }

    [HarmonyPatch(typeof(UI_DamageStatsUI), nameof(UI_DamageStatsUI.OnOpened))]
    internal static class DamageSourcesOpenedPatch
    {
        private static void Postfix(UI_DamageStatsUI __instance)
        {
            if (!FeatureFailure.IsAvailable(FeatureId.CombatInsights)) return;
            try { PostfixCore(__instance); }
            catch (System.Exception exception) { FeatureFailure.Disable(FeatureId.CombatInsights, exception); }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static void PostfixCore(UI_DamageStatsUI __instance)
        {
            if (!TrainingDamageStatistics.Available) return;
            var panel = __instance.GetComponent<NativeDamageSourcesPanel>() ??
                __instance.gameObject.AddComponent<NativeDamageSourcesPanel>();
            panel.Show(__instance);
        }
    }

    [HarmonyPatch(typeof(UI_DamageStatsUI), nameof(UI_DamageStatsUI.CurrentLocationDamageButtonClick))]
    internal static class DamageSourcesCurrentAreaPatch
    {
        private static void Postfix(UI_DamageStatsUI __instance)
        {
            if (!FeatureFailure.IsAvailable(FeatureId.CombatInsights)) return;
            try { PostfixCore(__instance); }
            catch (System.Exception exception) { FeatureFailure.Disable(FeatureId.CombatInsights, exception); }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static void PostfixCore(UI_DamageStatsUI __instance)
        {
            __instance.GetComponent<NativeDamageSourcesPanel>()?.SelectNative(false);
        }
    }

    [HarmonyPatch(typeof(UI_DamageStatsUI), nameof(UI_DamageStatsUI.AllLocationDamageButtonClick))]
    internal static class DamageSourcesAllAreasPatch
    {
        private static void Postfix(UI_DamageStatsUI __instance)
        {
            if (!FeatureFailure.IsAvailable(FeatureId.CombatInsights)) return;
            try { PostfixCore(__instance); }
            catch (System.Exception exception) { FeatureFailure.Disable(FeatureId.CombatInsights, exception); }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static void PostfixCore(UI_DamageStatsUI __instance)
        {
            __instance.GetComponent<NativeDamageSourcesPanel>()?.SelectNative(true);
        }
    }

}
