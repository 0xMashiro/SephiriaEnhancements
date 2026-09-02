using System;
using System.Globalization;
using System.Text;

namespace SephiriaEnhancements.MultiplayerRules
{
    internal static class ActiveExplorationRulesPayloadCodec
    {
        private const int SchemaVersion = 2;

        internal static string Encode(ActiveExplorationMultiplayerRules rules)
        {
            if (rules == null) throw new ArgumentNullException(nameof(rules));

            var payload = new StringBuilder();
            payload.Append(SchemaVersion).Append('|')
                .Append((int)rules.Preset).Append('|')
                .Append((int)rules.HealthModifierCombination);
            foreach (MultiplayerRuleDefinition definition in
                MultiplayerRuleCatalog.All)
            {
                for (int participantCount = 1; participantCount <= 4;
                    participantCount++)
                {
                    payload.Append('|');
                    MultiplayerRuleValue<float> value = rules.Rules.Get(
                        definition.Id, participantCount);
                    payload.Append(value.TryGetOverride(out float overrideValue)
                        ? overrideValue.ToString("R", CultureInfo.InvariantCulture)
                        : "n");
                }
            }
            return payload.ToString();
        }

        internal static bool TryDecode(string payload,
            out ActiveExplorationMultiplayerRules rules)
        {
            rules = null!;
            string[] cells = payload?.Split('|') ?? Array.Empty<string>();
            int expectedCells = 3 + MultiplayerRuleCatalog.All.Count * 4;
            if (cells.Length != expectedCells ||
                !int.TryParse(cells[0], out int schema) || schema != SchemaVersion ||
                !int.TryParse(cells[1], out int presetValue) ||
                presetValue < 0 || presetValue > 2 ||
                !int.TryParse(cells[2], out int combinationValue) ||
                combinationValue < 0 || combinationValue > 2)
                return false;

            int cellIndex = 3;
            try
            {
                MultiplayerRuleSnapshot snapshot = MultiplayerRuleSnapshot.Create(
                    (id, participantCount) =>
                    {
                        string cell = cells[cellIndex++];
                        if (cell == "n")
                            return MultiplayerRuleValue<float>.UseGameBehavior();
                        if (!float.TryParse(cell, NumberStyles.Float,
                                CultureInfo.InvariantCulture, out float value) ||
                            !MultiplayerRuleCatalog.Get(id).IsValidOverride(value))
                            throw new FormatException();
                        return MultiplayerRuleValue<float>.Override(value);
                    });
                MultiplayerRulesPreset preset =
                    (MultiplayerRulesPreset)presetValue;
                EnemyHealthModifierCombination combination =
                    (EnemyHealthModifierCombination)combinationValue;
                if (preset != MultiplayerRulesPreset.Custom)
                {
                    ActiveExplorationMultiplayerRules expected =
                        ActiveExplorationMultiplayerRules.FromPreset(preset);
                    if (!snapshot.IsEquivalentTo(expected.Rules) ||
                        combination != expected.HealthModifierCombination)
                        return false;
                }
                rules = new ActiveExplorationMultiplayerRules(
                    preset, snapshot, combination);
                return true;
            }
            catch (FormatException)
            {
                rules = null!;
                return false;
            }
            catch (ArgumentException)
            {
                rules = null!;
                return false;
            }
        }

    }
}
