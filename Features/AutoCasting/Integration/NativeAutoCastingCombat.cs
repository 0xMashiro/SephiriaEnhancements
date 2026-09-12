using UnityEngine;

namespace SephiriaEnhancements.AutoCasting.Integration
{
    internal static class NativeAutoCastingCombat
    {
        internal static bool IsActive(PlayerAvatar player)
        {
            if (player.IsInBattle) return true;

            // This native spawner synchronizes its arena and boss instead of the player's battle counter.
            foreach (SeedBossSpawner spawner in UnityEngine.Object.FindObjectsByType<SeedBossSpawner>(FindObjectsSortMode.None))
            {
                UnitAvatar boss = spawner.NetworkbossObject;
                if (!spawner.gameObject.activeInHierarchy || !spawner.NetworkplayerPrevent ||
                    boss == null || !boss.gameObject.activeInHierarchy || boss.IsDead || boss.canBeTarget <= 0) continue;

                Vector2 origin = spawner.transform.position;
                Vector2 lower = origin + spawner.playerPreventArea_lb;
                Vector2 upper = origin + spawner.playerPreventArea_rt;
                Vector2 position = player.transform.position;
                if (position.x >= lower.x && position.x <= upper.x &&
                    position.y >= lower.y && position.y <= upper.y) return true;
            }
            return false;
        }
    }
}
