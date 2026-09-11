using SephiriaEnhancements.Configuration;
using UnityEngine;

namespace SephiriaEnhancements.EffectStats.Integration
{
    internal static class NativeEffectStatsResults
    {
        private static string Format(string key, params object[] values) =>
            string.Format(ModLocalization.Get(key), values);

        private static ComboEffect_DarkCloud Cloud(PlayerAvatar player)
        {
            var effect = player.Inventory?.FindComboEffect("DARKCLOUD") as ComboEffect_DarkCloud;
            return effect != null && effect.isEnabled && effect.isDarkCloudActive &&
                effect.Networkavatar == player ? effect : null;
        }

        internal static string CloudDamage(PlayerAvatar player)
        {
            if (Cloud(player) == null) return null;
            float lightning = player.GetCustomStat(ECustomStat.LightningDamage);
            float damage = lightning * KeywordDatabase.GetConstValue("darkCloudDamagePercent") / 100f;
            if (player.GetCustomStatUnsafe("DARKCLOUDICE") > 0)
            {
                float ice = player.GetCustomStat(ECustomStat.IceDamage);
                damage = Mathf.Max(lightning, ice) * KeywordDatabase.GetConstValue("darkCloudDamagePercent") / 100f +
                    Mathf.Min(lightning, ice) * KeywordDatabase.GetConstValue("darkCloudDamagePercentIce") / 100f;
            }
            damage *= 1f + player.GetCustomStatUnsafe("DARKCLOUDDAMAGE") / 100f;
            return Format(EffectStatsLocalization.CloudDamage, damage.ToString("0.#"));
        }

        internal static string CloudSupply(PlayerAvatar player)
        {
            var cloud = Cloud(player);
            if (cloud == null) return null;
            float speed = 1f + player.GetCustomStatUnsafe("DARKCLOUDSPEED") / 100f;
            int attackBonus = player.GetCustomStatUnsafe("DARKCLOUDATKSPEEDBONUS");
            if (attackBonus > 0)
                speed += player.GetCustomStat(ECustomStat.AttackSpeed) * attackBonus / 10000f;
            speed *= cloud.lightningIntervalSpeed;
            float restoreSpeed = 1f + player.GetCustomStatUnsafe("DARKCLOUDRESTOREDURINGBATTLE") / 100f;
            return Format(EffectStatsLocalization.CloudSupply, cloud.darkCloud, cloud.MaxDarkCloud,
                Seconds(cloud.cloudTimer.time, speed), Mathf.Max(1, player.GetCustomStatUnsafe("DARKCLOUDMULTISHOT")),
                Mathf.Clamp(player.GetCustomStatUnsafe("DARKCLOUDKEEP"), 0, 100),
                Mathf.Max(1, cloud.MaxDarkCloud * cloud.defaultRestorePercent / 100), Seconds(5f, restoreSpeed));
        }

        // Nonpositive timer speed cannot complete a future cycle.
        private static string Seconds(float duration, float speed) =>
            speed > 0f ? (duration / speed).ToString("0.##") : "∞";

        internal static string Magic(PlayerAvatar player, int slot)
        {
            var controller = player.GetComponent<SkillController>();
            var slots = controller?.magicCharmsOnEachClient;
            if (slots == null || slot >= slots.Length) return null;
            var magic = slots[slot];
            if (magic == null || magic.NetworkAvatar != player || !magic.IsEffectEnabled || magic.ContainedMagic == null)
                return null;
            int cost = magic.GetCost(player, magic.CurrentLevelToIdx());
            float recovery = 1f + (player.GetCustomStat(ECustomStat.CooldownRecoverySpeed) +
                magic.AdditionalcooldownRecoverySpeed) / 100f;
            return Format(EffectStatsLocalization.Magic, slot + 1, magic.ContainedMagic.Name, cost,
                cost > 0 ? (Mathf.Max(0, player.MP) / cost).ToString() : "∞",
                magic.currentAmmo, magic.maxAmmo, Seconds(magic.ContainedMagic.cooldownTime, recovery));
        }

        internal static string Burn(PlayerAvatar player)
        {
            // A reference, not a claim that this build can apply the debuff.
            if (player.GetCustomStatUnsafe("DIRECTATTACKBURN") <= 0 &&
                player.GetCustomStatUnsafe("BLUEBURNCHANGE") <= 0 && player.GetCustomStatUnsafe("BURNEVO") <= 0 &&
                player.GetCustomStatUnsafe("BURNDAMAGE") == 0 && player.GetCustomStatUnsafe("BURNSPEED") == 0 &&
                player.GetCustomStatUnsafe("BURNSTACK") == 0 && player.GetCustomStatUnsafe("BURNDURATION") == 0)
                return null;
            var prefab = CombatManager.Instance?.burnDebuffPrefab as CharacterDebuff_Burn;
            if (prefab == null) return null;
            float damage = CharacterDebuff_Burn.CalculateTickDamage(player, out _);
            return Format(EffectStatsLocalization.Burn, damage.ToString("0.#"),
                Seconds(prefab.tickTimer.time, 1f + player.GetCustomStatUnsafe("BURNSPEED") / 100f),
                2 + player.GetCustomStatUnsafe("BURNSTACK"));
        }
    }
}
