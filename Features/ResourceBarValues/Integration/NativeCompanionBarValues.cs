using SephiriaEnhancements.Runtime;
using HarmonyLib;

namespace SephiriaEnhancements.ResourceBarValues.Integration
{
    [HarmonyPatch(typeof(UI_CompanionHUD), nameof(UI_CompanionHUD.Connect))]
    internal static class NativeCompanionBarValuesPatch
    {
        private static void Postfix(UI_CompanionHUD __instance)
        {
            if (!FeatureFailure.IsAvailable(FeatureId.ResourceBarValues))
            {
                return;
            }

            try
            {
                PostfixCore(__instance);
            }
            catch (System.Exception exception)
            {
                FeatureFailure.Disable(FeatureId.ResourceBarValues, exception);
                return;
            }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static void PostfixCore(UI_CompanionHUD __instance)
        {
            // This HUD owns a native numeric HP label; do not change the player's
            // UI_HPBar or ordinary creatures' world-space bars.
            var owner = __instance.hpBar;
            var view = NativeResourceBarValueView.GetOrAdd(__instance);
            view.Clear();
            var health = owner.valueText;
            var spareHealth = owner.spareHpValue;
            bool healthEnabled = health != null && health.enabled;
            bool spareHealthEnabled = spareHealth != null && spareHealth.enabled;
            view.RefreshLayout = enabled =>
            {
                bool show = ResourceBarValueSettings.Get(ResourceBarValueSetting.CompanionHealthNumbers);
                if (health != null)
                    health.enabled = enabled ? show : healthEnabled;
                if (spareHealth != null)
                    spareHealth.enabled = enabled ? show : spareHealthEnabled;
            };
            view.RestoreLayout = () =>
            {
                if (health != null)
                    health.enabled = healthEnabled;
                if (spareHealth != null)
                    spareHealth.enabled = spareHealthEnabled;
            };
            view.RefreshLayout(Configuration.EnhancementsSettings.Enabled);
        }
    }
}
