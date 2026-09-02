using System;

namespace SephiriaEnhancements.MultiplayerRules
{
    internal enum MultiplayerRuleValueSource
    {
        UseGameBehavior,
        Override
    }

    internal readonly struct MultiplayerRuleValue<T>
    {
        private readonly T overrideValue;

        private MultiplayerRuleValue(MultiplayerRuleValueSource source, T value)
        {
            Source = source;
            overrideValue = value;
        }

        internal MultiplayerRuleValueSource Source { get; }

        internal bool TryGetOverride(out T value)
        {
            value = overrideValue;
            return Source == MultiplayerRuleValueSource.Override;
        }

        internal static MultiplayerRuleValue<T> UseGameBehavior() =>
            new MultiplayerRuleValue<T>(MultiplayerRuleValueSource.UseGameBehavior,
                default!);

        internal static MultiplayerRuleValue<T> Override(T value) =>
            new MultiplayerRuleValue<T>(MultiplayerRuleValueSource.Override, value);
    }

    internal sealed class ParticipantCountRule<T>
    {
        private readonly MultiplayerRuleValue<T>[] values;

        internal ParticipantCountRule(MultiplayerRuleValue<T> oneParticipant,
            MultiplayerRuleValue<T> twoParticipants,
            MultiplayerRuleValue<T> threeParticipants,
            MultiplayerRuleValue<T> fourParticipants)
        {
            values = new[] { oneParticipant, twoParticipants,
                threeParticipants, fourParticipants };
        }

        internal MultiplayerRuleValue<T> ForParticipantCount(int participantCount)
        {
            if (participantCount < 1 || participantCount > 4)
            {
                throw new ArgumentOutOfRangeException(nameof(participantCount),
                    participantCount,
                    "Multiplayer rules support one to four participants.");
            }

            return values[participantCount - 1];
        }

        internal static ParticipantCountRule<T> UseGameBehavior() =>
            new ParticipantCountRule<T>(
                MultiplayerRuleValue<T>.UseGameBehavior(),
                MultiplayerRuleValue<T>.UseGameBehavior(),
                MultiplayerRuleValue<T>.UseGameBehavior(),
                MultiplayerRuleValue<T>.UseGameBehavior());
    }
}
