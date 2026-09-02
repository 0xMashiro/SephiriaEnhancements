using SephiriaEnhancements.MultiplayerRules;

namespace SephiriaEnhancements.ModelChecks.Features.MultiplayerRules;

internal static class ActiveExplorationRulesPayloadCodecChecks
{
    internal static void Run()
    {
        MultiplayerRuleSnapshot customPayloadSnapshot = MultiplayerRuleSnapshot.Create(
            (ruleId, participantCount) =>
                ruleId == MultiplayerRuleId.RestorativePotionQuantity &&
                    participantCount == 4
                    ? MultiplayerRuleValue<float>.Override(7f)
                    : ruleId ==
                        MultiplayerRuleId.QliphothFinalBattleEntryAttackTracksParticipant &&
                        participantCount == 2
                        ? MultiplayerRuleValue<float>.Override(1f)
                        : MultiplayerRuleValue<float>.UseGameBehavior());
        var customPayloadRules = ActiveExplorationMultiplayerRules.Custom(
            customPayloadSnapshot, EnemyHealthModifierCombination.Multiplicative);
        string customPayload = ActiveExplorationRulesPayloadCodec.Encode(customPayloadRules);
        string[] mismatchedPresetPayloadCells = customPayload.Split('|');
        mismatchedPresetPayloadCells[1] =
            ((int)MultiplayerRulesPreset.Original).ToString();
        string mismatchedPresetPayload = string.Join('|', mismatchedPresetPayloadCells);
        foreach (MultiplayerRulesPreset fixedPreset in new[]
            { MultiplayerRulesPreset.Original, MultiplayerRulesPreset.Optimized })
        {
            ActiveExplorationMultiplayerRules fixedPresetRules =
                ActiveExplorationMultiplayerRules.FromPreset(fixedPreset);
            if (!ActiveExplorationRulesPayloadCodec.TryDecode(
                    ActiveExplorationRulesPayloadCodec.Encode(fixedPresetRules),
                    out ActiveExplorationMultiplayerRules decodedFixedPresetRules) ||
                decodedFixedPresetRules.Preset != fixedPreset ||
                decodedFixedPresetRules.HealthModifierCombination !=
                    fixedPresetRules.HealthModifierCombination ||
                !decodedFixedPresetRules.Rules.IsEquivalentTo(fixedPresetRules.Rules))
            {
                throw new InvalidOperationException(
                    "fixed multiplayer-rule presets must round-trip exactly");
            }
        }
        if (!ActiveExplorationRulesPayloadCodec.TryDecode(customPayload,
                out ActiveExplorationMultiplayerRules decodedPayloadRules) ||
            decodedPayloadRules.Preset != MultiplayerRulesPreset.Custom ||
            decodedPayloadRules.HealthModifierCombination !=
                EnemyHealthModifierCombination.Multiplicative ||
            !decodedPayloadRules.Rules.Get(
                MultiplayerRuleId.RestorativePotionQuantity, 4)
                .TryGetOverride(out float decodedPotionQuantity) ||
            Math.Abs(decodedPotionQuantity - 7f) > 0.001f ||
            decodedPayloadRules.Rules.Get(
                MultiplayerRuleId.RestorativePotionQuantity, 3).Source !=
                MultiplayerRuleValueSource.UseGameBehavior ||
            ActiveExplorationRulesPayloadCodec.TryDecode(
                customPayload.Replace("|7", "|999"), out _) ||
            ActiveExplorationRulesPayloadCodec.TryDecode(
                mismatchedPresetPayload, out _))
        {
            throw new InvalidOperationException(
                "active multiplayer-rule payload must round-trip sparse values and reject invalid overrides");
        }
        Console.WriteLine("MultiplayerRulesPayload: sparse host snapshot round trip passed");
    }
}
