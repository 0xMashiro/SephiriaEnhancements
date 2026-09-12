using SephiriaEnhancements.Runtime;
using System.Reflection;
using HarmonyLib;
using Mirror;
using SephiriaEnhancements.Integration;
using UnityEngine;

namespace SephiriaEnhancements.DefeatRetry
{
    internal static class NativeRetryBoss
    {
        private static readonly FieldInfo DefaultButtonColor = AccessTools.Field(typeof(UI_HorayButton), "defaultColor");
        private static readonly FieldInfo SkipWait = AccessTools.Field(typeof(UI_BossPanel), "isSkipWait");
        private static readonly FieldInfo Skippable = AccessTools.Field(typeof(UI_BossPanel), "isSkippable");
        private static readonly FieldInfo AutoSkipped = AccessTools.Field(typeof(UI_BossPanel), "isAutoSkipped");
        private static NetworkConnectionToServer connection;
        private static string floorGuid;
        private static bool worldLoaded;

        internal static bool IsAvailable => DefaultButtonColor != null && SkipWait != null && Skippable != null && AutoSkipped != null;

        internal static void Begin(string floor)
        {
            connection = NetworkClient.connection;
            floorGuid = floor;
            worldLoaded = false;
        }

        internal static void ObserveWorldSession(bool saved)
        {
            if (!saved || worldLoaded) Clear();
            else if (floorGuid != null) worldLoaded = true;
        }

        internal static void Clear()
        {
            connection = null;
            floorGuid = null;
            worldLoaded = false;
        }

        internal static bool CanRebuildBossFloor(string floor)
        {
            foreach (BossSpawner boss in UnityEngine.Object.FindObjectsByType<BossSpawner>(FindObjectsSortMode.None))
                if (boss.parent != null && boss.parent.guid == floor && !boss.IsCleared && RequiresFloorRebuild(boss))
                    return true;
            return false;
        }

        internal static bool RequiresFloorRebuild(BossSpawner boss) => boss is LibraryBossSpawner ||
            (boss != null && boss.GetType() == typeof(BossSpawner) &&
             boss.parent is FullyDesignedFloorGenerator floor && floor.props.Contains(boss.gameObject));

        internal static void RestoreButtonColor(UI_HorayButton button, UI_HorayButton source)
        {
            // Awake on a clone can capture the source's temporary disabled tint.
            Color color = (Color)DefaultButtonColor.GetValue(source);
            DefaultButtonColor.SetValue(button, color);
            if (button.text != null) button.text.color = button.IsInteractable() ? color : button.disabledColor;
        }

        internal static void SkipRetryCutscene(UI_BossPanel panel)
        {
            PlayerAvatar player = LocalPlayerResolver.Resolve();
            if (!DefeatRetrySettings.SkipCutscenes || !worldLoaded ||
                connection != NetworkClient.connection || player == null ||
                player.currentFloorGuid != floorGuid || !panel.IsOpened ||
                !(bool)Skippable.GetValue(panel) || (bool)SkipWait.GetValue(panel) ||
                (bool)AutoSkipped.GetValue(panel)) return;
            // Use the native vote/Command path, including its multiplayer rules.
            DungeonManager.Instance.SkipVote(player.GetComponent<PlayerSpawner>());
            AutoSkipped.SetValue(panel, true);
        }
    }

    [HarmonyPatch(typeof(UI_BossPanel), "Update")]
    internal static class DefeatRetryCutscenePatch
    {
        private static void Postfix(UI_BossPanel __instance)
        {
            if (!FeatureFailure.IsAvailable(FeatureId.DefeatRetry))
            {
                return;
            }

            try
            {
                PostfixCore(__instance);
            }
            catch (System.Exception exception)
            {
                FeatureFailure.Disable(FeatureId.DefeatRetry, exception);
                return;
            }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static void PostfixCore(UI_BossPanel __instance) => NativeRetryBoss.SkipRetryCutscene(__instance);
    }
}
