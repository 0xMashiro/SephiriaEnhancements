using System;

namespace SephiriaEnhancements.EffectStats.Integration
{
    // Status IDs and category localization keys belong to the native API boundary.
    internal static class NativeEffectStatsCatalog
    {
        internal static readonly (string Title, string[] Statuses)[] Groups =
        {
            ("ItemCategory_FlameSword", new[] { "FLAME_SWORD_DAMAGE", "FLAME_SWORD_CRITICAL",
                "FLAME_SWORD_MAX", "FLAME_SWORD_LUCK", "FLAME_SWORD_ADDITIONAL_ATTACK_FROM_WEAPON",
                "FLAME_SWORD_ADDITIONAL_ATTACK_FROM_MAGIC", "FLAME_SWORD_MAGIC_DAMAGE",
                "FLAME_SWORD_IGNORE_DEFENSE", "FLAME_SWORD_PICK_BONUS" }),
            ("ItemCategory_Frost", new[] { "FROST_RELIC_DAMAGE", "FROST_RELIC_MP_DAMAGE",
                "FROST_RELIC_MP_MAXMP_DAMAGE", "CHARGING_CHARM_AMPLIFY",
                "CHARGING_CHARM_RETRIGGER_BY_ATTACKSPEED" }),
            ("ItemCategory_DarkCloud", new[] { "DARK_CLOUD_DAMAGE", "DARK_CLOUD_SPEED",
                "DARK_CLOUD_KEEP", "DARK_CLOUD_MULTISHOT", "DARK_CLOUD_RESTORE_DURING_BATTLE" }),
            ("ItemCategory_Planet", new[] { "PLANET_DAMAGE", "PLANET_ATTACK_SPEED" }),
            ("Status_Magic_Name", new[] { "MAGIC_COST_REDUCE", "MAGIC_MP", "MAGIC_QUICK_CAST" }),
            ("Debuff_Burn", new[] { "BURN_DAMAGE", "BURN_DURATION", "BURN_SPEED", "BURN_STACK",
                "BURN_ADD", "BURN_EVO", "BLUE_BURN_CHANGE" }),
            ("Debuff_Electric", new[] { "ELECTRIC_DAMAGE", "ELECTRIC_QUICKNESS", "ELECTRIC_STACK", "ELECTRIC_LUCK" })
        };

        internal static StatusInstance_Custom CreateStatus(string id)
        {
            StatusEntity entity = StatusDatabase.GetStatusEntity(id);
            string[] parts = entity != null ? entity.className.Split('/') : Array.Empty<string>();
            if (parts.Length != 2 || parts[0] != "StatusInstance_Custom" ||
                entity.GetRelatedKeyword() == null || entity.divideForDisplay == 0)
                throw new InvalidOperationException("Effect statistic has no supported native definition: " + id);
            var status = new StatusInstance_Custom();
            status.SetCustomID(parts[1], id);
            status.SetKeyword(entity.statKeyword);
            return status;
        }

        internal static int Read(UnitAvatar player, string id)
        {
            StatusEntity entity = StatusDatabase.GetStatusEntity(id);
            string key = entity.className.Substring("StatusInstance_Custom/".Length).ToUpperInvariant();
            return player.GetCustomStatUnsafe(key);
        }

        internal static string Describe(StatusInstance_Custom status, int value)
        {
            // Unattached instances only format aggregate values; never apply them to an avatar.
            status.SetValue(value);
            return KeywordDatabase.Convert(status.ToString(false, false, false, false),
                useColor: false, useSprite: false);
        }

    }
}
