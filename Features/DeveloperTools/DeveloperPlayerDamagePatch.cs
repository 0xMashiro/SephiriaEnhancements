using SephiriaEnhancements.Runtime;
#if SEPHIRIA_ENHANCEMENTS_DEVTOOLS
using HarmonyLib;
using SephiriaEnhancements.Configuration;
using SephiriaEnhancements.DeveloperTools.Core;
using SephiriaEnhancements.Integration;

namespace SephiriaEnhancements.DeveloperTools
{
    [HarmonyPatch(typeof(DamageInstance), nameof(DamageInstance.GetDamage))]
    internal static class DeveloperPlayerDamagePatch
    {
        private static void Postfix(DamageInstance __result)
        {
            if (!FeatureFailure.IsAvailable(FeatureId.DeveloperTools))
            {
                return;
            }

            try
            {
                PostfixCore(__result);
            }
            catch (System.Exception exception)
            {
                FeatureFailure.Disable(FeatureId.DeveloperTools, exception);
                return;
            }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static void PostfixCore(DamageInstance __result)
        {
            if (!EnhancementsSettings.Enabled || __result?.origin == null || DeveloperPlayerDamageSettings.MultiplierIndex == 0)
            {
                return;
            }

            PlayerAvatar player = ResolvePlayer(__result.origin as UnitAvatar);
            if (!LocalPlayerResolver.IsLocal(player))
            {
                return;
            }

            __result.damage = DeveloperPlayerDamagePolicy.Apply(__result.damage, DeveloperPlayerDamageSettings.MultiplierIndex);
        }

        private static PlayerAvatar ResolvePlayer(UnitAvatar source)
        {
            UnitAvatar current = source;
            for (int depth = 0; current != null && depth < 8; depth++)
            {
                if (current is PlayerAvatar player)
                {
                    return player;
                }

                UnitAvatar leader = current.NetworkLeader;
                if (leader == null || leader == current)
                {
                    break;
                }
                current = leader;
            }

            return null;
        }
    }
}
#endif
