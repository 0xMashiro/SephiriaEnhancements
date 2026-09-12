using System.Collections.Generic;
using System.Linq;
using SephiriaEnhancements.Configuration;
using UnityEngine;

namespace SephiriaEnhancements.EffectStats.Integration
{
    internal static class NativeEffectStatsResults
    {
        internal static IEnumerable<(string Id, string Title, string Text)> Read(PlayerAvatar player, string group)
        {
            string title = new LocalizedString(group).ToString();
            switch (group)
            {
                case "ItemCategory_FlameSword":
                    yield return ("solar-damage", title, SolarDamage(player));
                    yield return ("solar-supply", title, SolarSupply(player));
                    break;
                case "ItemCategory_DarkCloud":
                    yield return ("cloud-damage", title, CloudDamage(player));
                    yield return ("cloud-supply", title, CloudSupply(player));
                    break;
                case "Status_Magic_Name":
                    var slots = player.GetComponent<SkillController>()?.magicCharmsOnEachClient;
                    if (slots == null) break;
                    for (int slot = 0; slot < slots.Length && slot < 8; slot++)
                        yield return ("magic-" + slot, slots[slot]?.ContainedMagic?.Name, Magic(player, slot));
                    break;
                case "Debuff_Burn":
                    yield return ("burn", title, Burn(player));
                    break;
                case "Debuff_Electric":
                    yield return ("electric", title, Electric(player));
                    break;
                case "ItemCategory_Frost":
                case "ItemCategory_Planet":
                    if (player.Inventory == null) break;
                    foreach (Charm_Basic charm in player.Inventory.charms.Values
                        .Where(charm => charm != null && charm.NetworkAvatar == player && charm.IsEffectEnabled &&
                            charm.netId != 0 && charm.Item?.Charm == charm).OrderBy(charm => charm.yIdx).ThenBy(charm => charm.xIdx))
                    {
                        string text = group == "ItemCategory_Frost" ? Frost(player, charm) : Planet(player, charm);
                        if (text != null) yield return ("artifact-" + charm.Item.InstanceID, charm.Item.Name, text);
                    }
                    break;
            }
        }

        private static string Format(string key, params object[] values) =>
            string.Format(ModLocalization.Get(key), values);

        private static ComboEffect_FlameSword Solar(PlayerAvatar player)
        {
            var effect = player.Inventory?.FindComboEffect("FLAMESWORD") as ComboEffect_FlameSword;
            return effect != null && effect.isEnabled && effect.isFlameSwordEnabled &&
                effect.Networkavatar == player ? effect : null;
        }

        internal static string SolarDamage(PlayerAvatar player)
        {
            var solar = Solar(player);
            return solar == null ? null : Format(EffectStatsLocalization.SolarDamage,
                solar.GetDamage(false).ToString("0.#"), solar.GetDamage(true).ToString("0.#"),
                Mathf.Clamp(player.GetCustomStatUnsafe("FLAMESWORDLUCK"), 0, 100));
        }

        internal static string SolarSupply(PlayerAvatar player)
        {
            var solar = Solar(player);
            // OnStartServer replaces maxSword with defaultMaxSword; maxSword itself is not synced.
            return solar == null ? null : Format(EffectStatsLocalization.SolarSupply,
                solar.currentSword, solar.defaultMaxSword + player.GetCustomStatUnsafe("FLAMESWORDMAX"));
        }

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
            // Sources also include artifact/projectile effects without a dedicated avatar stat.
            // Always label this as a reference instead of inferring whether Burn can be applied.
            var prefab = CombatManager.Instance?.burnDebuffPrefab as CharacterDebuff_Burn;
            if (prefab == null) return null;
            float damage = CharacterDebuff_Burn.CalculateTickDamage(player, out _);
            return Format(EffectStatsLocalization.Burn, damage.ToString("0.#"),
                Seconds(prefab.tickTimer.time, 1f + player.GetCustomStatUnsafe("BURNSPEED") / 100f),
                2 + player.GetCustomStatUnsafe("BURNSTACK"));
        }

        internal static string Electric(PlayerAvatar player)
        {
            var prefab = CombatManager.Instance?.electricDebuffPrefab as CharacterDebuff_Electric;
            if (prefab == null) return null;
            float damage = player.GetCustomStat(ECustomStat.LightningDamage) * prefab.statDamagePercent * 0.01f;
            damage *= 1f + player.GetCustomStatUnsafe("ELECTRICDAMAGE") / 100f;
            return Format(EffectStatsLocalization.Electric, damage.ToString("0.#"),
                2 + player.GetCustomStatUnsafe("ELECTRICSTACK"));
        }

        internal static string Frost(PlayerAvatar player, Charm_Basic charm)
        {
            if (charm is Charm_Guillotine guillotine)
                return Format(EffectStatsLocalization.Guillotine, charm.Item.Name,
                    Charm_Basic.CalculateDamage(charm).ToString("0.#"),
                    Seconds(guillotine.triggerCooldown, 1f + player.GetCustomStatUnsafe("CHARGINGCHARMBONUS") / 100f),
                    guillotine.bladeTargetCount, 1 + player.GetCustomStatUnsafe("CHARGINGCHARMAMPLIFY"),
                    guillotine.shockCooldownReduction.ToString("0.##"));
            ChargingCharm charging = charm is Charm_AirSlash slash ? slash.chargingCharm :
                charm is Charm_IceBow bow ? bow.chargingCharm : null;
            if (charging == null) return null;
            float damage = Charm_Basic.CalculateDamage(charm);
            int remainingMp = player.MP;
            int cost = 0;
            int mpDamage = player.GetCustomStatUnsafe("FROSTRELICMPDAMAGE");
            float skillMultiplier = 1f + player.GetCustomStatUnsafe("MPSKILLDAMAGE") / 100f;
            // Match the two sequential payments in ActivateChargingCharm without spending MP.
            if (mpDamage > 0 && remainingMp >= ChargingCharm.MPCost)
            {
                remainingMp -= ChargingCharm.MPCost;
                cost += ChargingCharm.MPCost;
                damage *= (1f + mpDamage / 100f) * skillMultiplier;
            }
            if (player.GetCustomStatUnsafe("FROSTRELICMPMAXMPDAMAGE") > 0 && remainingMp >= ChargingCharm.MPCost)
            {
                cost += ChargingCharm.MPCost;
                damage += Mathf.Max(0, player.MaxMp - KeywordDatabase.GetConstValue("PLAYERDEFAULTMP")) * skillMultiplier;
            }
            int projectiles = 1 + player.GetCustomStatUnsafe("CHARGINGCHARMAMPLIFY");
            if (charm is Charm_IceBow iceBow) projectiles *= iceBow.readyArrowCount;
            return Format(EffectStatsLocalization.Frost, charm.Item.Name, damage.ToString("0.#"), cost,
                Seconds(charging.GetChargeTimer(), 1f + player.GetCustomStatUnsafe("CHARGINGCHARMBONUS") / 100f),
                projectiles);
        }

        internal static string Planet(PlayerAvatar player, Charm_Basic charm)
        {
            if (!(charm is Charm_SummonGreenBat planet)) return null;
            float damage = planet.GetBatDamage() * (1f + player.GetCustomStatUnsafe("PLANETDAMAGE") / 100f);
            // Enhancement is held by the spawned projectile source, whose owner is not synced.
            // Present both damage references; do not read the server-only source as client state.
            return Format(EffectStatsLocalization.Planet, charm.Item.Name, damage.ToString("0.#"),
                (damage * 1.5f).ToString("0.#"));
        }
    }
}
