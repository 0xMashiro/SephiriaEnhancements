using System;
using System.Collections.Generic;

namespace SephiriaEnhancements.MultiplayerRules
{
    internal sealed class MultiplayerRuleSnapshot
    {
        private readonly Dictionary<MultiplayerRuleId, ParticipantCountRule<float>> rules;

        internal MultiplayerRuleSnapshot(
            IEnumerable<KeyValuePair<MultiplayerRuleId, ParticipantCountRule<float>>> values)
        {
            rules = new Dictionary<MultiplayerRuleId, ParticipantCountRule<float>>();
            foreach (KeyValuePair<MultiplayerRuleId, ParticipantCountRule<float>> value in values)
            {
                if (value.Value == null)
                    throw new ArgumentException("A multiplayer rule cannot be null.",
                        nameof(values));
                rules.Add(value.Key, value.Value);
            }
        }

        internal MultiplayerRuleValue<float> Get(MultiplayerRuleId id,
            int participantCount)
        {
            return rules.ContainsKey(id)
                ? rules[id].ForParticipantCount(participantCount)
                : MultiplayerRuleValue<float>.UseGameBehavior();
        }

        internal bool HasAnyOverride(params MultiplayerRuleId[] ids)
        {
            foreach (MultiplayerRuleId id in ids)
            {
                for (int participantCount = 1; participantCount <= 4;
                    participantCount++)
                {
                    if (Get(id, participantCount).Source ==
                        MultiplayerRuleValueSource.Override)
                        return true;
                }
            }
            return false;
        }

        internal bool IsEquivalentTo(MultiplayerRuleSnapshot other)
        {
            if (other == null) return false;
            foreach (MultiplayerRuleDefinition definition in
                MultiplayerRuleCatalog.All)
            {
                for (int participantCount = 1; participantCount <= 4;
                    participantCount++)
                {
                    MultiplayerRuleValue<float> left = Get(definition.Id,
                        participantCount);
                    MultiplayerRuleValue<float> right = other.Get(definition.Id,
                        participantCount);
                    if (left.Source != right.Source) return false;
                    if (left.TryGetOverride(out float leftValue) &&
                        (!right.TryGetOverride(out float rightValue) ||
                         Math.Abs(leftValue - rightValue) > 0.0001f))
                        return false;
                }
            }
            return true;
        }

        internal static MultiplayerRuleSnapshot Original() =>
            new MultiplayerRuleSnapshot(Array.Empty<
                KeyValuePair<MultiplayerRuleId, ParticipantCountRule<float>>>());

        internal static MultiplayerRuleSnapshot Create(
            Func<MultiplayerRuleId, int, MultiplayerRuleValue<float>> readValue)
        {
            var values = new List<
                KeyValuePair<MultiplayerRuleId, ParticipantCountRule<float>>>();
            foreach (MultiplayerRuleDefinition definition in MultiplayerRuleCatalog.All)
            {
                MultiplayerRuleValue<float>[] byParticipantCount =
                    new MultiplayerRuleValue<float>[4];
                for (int participantCount = 1; participantCount <= 4;
                    participantCount++)
                {
                    MultiplayerRuleValue<float> value = readValue(definition.Id,
                        participantCount);
                    if (value.TryGetOverride(out float overrideValue) &&
                        !definition.IsValidOverride(overrideValue))
                    {
                        throw new ArgumentException(
                            "Invalid override for " + definition.Id + ".",
                            nameof(readValue));
                    }
                    byParticipantCount[participantCount - 1] = value;
                }

                values.Add(Rule(definition.Id, new ParticipantCountRule<float>(
                    byParticipantCount[0], byParticipantCount[1],
                    byParticipantCount[2], byParticipantCount[3])));
            }
            return new MultiplayerRuleSnapshot(values);
        }

        internal static MultiplayerRuleSnapshot Optimized() =>
            new MultiplayerRuleSnapshot(new[]
            {
                Rule(MultiplayerRuleId.RandomEncounterHealthMultiplier,
                    FourParticipantsOnly(1.3f)),
                Rule(MultiplayerRuleId.SeedEncounterBossHealthMultiplier,
                    new ParticipantCountRule<float>(
                        MultiplayerRuleValue<float>.UseGameBehavior(),
                        MultiplayerRuleValue<float>.Override(1.9f),
                        MultiplayerRuleValue<float>.Override(2.8f),
                        MultiplayerRuleValue<float>.Override(3.7f))),
                Rule(MultiplayerRuleId.MindEaterRootSummonHealthMultiplier,
                    FourParticipantsOnly(1.3f))
            });

        private static KeyValuePair<MultiplayerRuleId, ParticipantCountRule<float>> Rule(
            MultiplayerRuleId id, ParticipantCountRule<float> values) =>
            new KeyValuePair<MultiplayerRuleId, ParticipantCountRule<float>>(id, values);

        private static ParticipantCountRule<float> FourParticipantsOnly(float value) =>
            new ParticipantCountRule<float>(
                MultiplayerRuleValue<float>.UseGameBehavior(),
                MultiplayerRuleValue<float>.UseGameBehavior(),
                MultiplayerRuleValue<float>.UseGameBehavior(),
                MultiplayerRuleValue<float>.Override(value));
    }
}
