using SephiriaEnhancements.Runtime;
using System.Reflection;
using HarmonyLib;
using Mirror;
using System;
using SephiriaEnhancements.Diagnostics;
using SephiriaEnhancements.Integration;

namespace SephiriaEnhancements.DefeatRetry
{
    [HarmonyPatch(typeof(PlayerSpawner), nameof(PlayerSpawner.RestartNewGame))]
    internal static class DefeatRetryPlayerRestorePatch
    {
        private static readonly MethodInfo InitializePlayerMethod =
            AccessTools.Method(typeof(PlayerSpawner), "Initialize",
                new[] { typeof(int), typeof(string), typeof(string), typeof(int) });

        internal static PlayerAvatar RestoringPlayer { get; private set; }
        internal static bool IsAvailable => InitializePlayerMethod != null;

        private static bool Prefix(PlayerSpawner __instance, int loadingScreen)
        {
            if (!FeatureFailure.IsAvailable(FeatureId.DefeatRetry))
            {
                return true;
            }

            try
            {
                return PrefixCore(__instance, loadingScreen);
            }
            catch (System.Exception exception)
            {
                FeatureFailure.Disable(FeatureId.DefeatRetry, exception);
                return true;
            }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static bool PrefixCore(PlayerSpawner __instance, int loadingScreen)
        {
            // NewGame clears IsRetrying before players restart; ownership remains
            // until this player's Initialize call ends, including failure.
            if (!NetworkServer.active || !DefeatRetryFeature.HasPendingPlacement(__instance.PlayerAvatar))
            {
                return true;
            }

            PlayerAvatar avatar = __instance.PlayerAvatar;
            RestoringPlayer = avatar;
            try
            {
                PlayerLocalDataStorage data = __instance.LocalDataStorage;
                bool initialized = (bool)InitializePlayerMethod.Invoke(__instance, new object[] { data.defaultWeapon, data.defaultCostume, data.defaultCostumeSkin, loadingScreen });
                SupportLogger.Record("retry_player_initialized", "player=" + avatar.netId + " success=" + initialized, initialized ? "INFO" : "ERROR");
                if (!initialized)
                {
                    DefeatRetryBridge.CancelPlayer(avatar);
                    return false;
                }

                // Native RestartNewGame also grants starting items and a starting potion.
                // A checkpoint restore already contains the player's saved items.
                if (__instance.connectionToClient != null)
                    __instance.TargetRestartNewGame(__instance.connectionToClient);
            }
            catch (Exception exception)
            {
                DefeatRetryBridge.CancelPlayer(avatar);
                SupportLogger.Record("retry_player_restore_failed", "player=" + avatar.netId + " exception=" + (exception.InnerException ?? exception).GetType().Name, "ERROR");
                throw;
            }
            finally
            {
                RestoringPlayer = null;
                DefeatRetryFeature.FinishPlayerRestore(avatar);
            }

            return false;
        }
    }
}
