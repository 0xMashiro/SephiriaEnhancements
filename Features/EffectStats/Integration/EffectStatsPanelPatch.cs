using HarmonyLib;
using SephiriaEnhancements.Integration;
using SephiriaEnhancements.Runtime;

namespace SephiriaEnhancements.EffectStats.Integration
{
    [HarmonyPatch(typeof(UI_StatsPanel), nameof(UI_StatsPanel.OnOpened))]
    internal static class EffectStatsPanelPatch
    {
        private static void Postfix(UI_StatsPanel __instance, UnitAvatar ___playerAvatar)
        {
            if (!FeatureFailure.IsAvailable(FeatureId.EffectStats)) return;
            try { PostfixCore(__instance, ___playerAvatar); }
            catch (System.Exception exception) { FeatureFailure.Disable(FeatureId.EffectStats, exception); }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static void PostfixCore(UI_StatsPanel panel, UnitAvatar avatar)
        {
            if (!(avatar is PlayerAvatar player) || !LocalPlayerResolver.IsLocal(player)) return;
            var view = panel.GetComponent<NativeEffectStatsView>() ?? panel.gameObject.AddComponent<NativeEffectStatsView>();
            view.Show(panel, player);
        }
    }
}
