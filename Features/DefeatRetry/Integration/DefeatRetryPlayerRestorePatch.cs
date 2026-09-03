using System.Reflection;
using HarmonyLib;
using Mirror;

namespace SephiriaEnhancements.DefeatRetry
{
    [HarmonyPatch(typeof(PlayerSpawner), nameof(PlayerSpawner.RestartNewGame))]
    internal static class DefeatRetryPlayerRestorePatch
    {
        private static readonly MethodInfo InitializePlayerMethod =
            AccessTools.Method(typeof(PlayerSpawner), "Initialize",
                new[] { typeof(int), typeof(string), typeof(string), typeof(int) });

        private static bool Prefix(PlayerSpawner __instance, int loadingScreen)
        {
            // NewGame clears IsRetrying before players restart. Initialize consumes
            // this player's pending placement while restoring the saved inventory.
            if (!NetworkServer.active ||
                !DefeatRetryFeature.HasPendingPlacement(__instance.PlayerAvatar))
            {
                return true;
            }

            PlayerLocalDataStorage data = __instance.LocalDataStorage;
            InitializePlayerMethod.Invoke(__instance, new object[]
            {
                data.defaultWeapon, data.defaultCostume,
                data.defaultCostumeSkin, loadingScreen
            });
            // Native RestartNewGame also grants starting items and a starting potion.
            // A checkpoint restore already contains the player's saved items.
            if (__instance.connectionToClient != null)
            {
                __instance.TargetRestartNewGame(__instance.connectionToClient);
            }
            return false;
        }
    }
}
