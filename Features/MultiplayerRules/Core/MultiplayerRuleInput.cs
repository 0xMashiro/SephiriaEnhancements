using System.Globalization;

namespace SephiriaEnhancements.MultiplayerRules
{
    internal enum MultiplayerRuleInputError { None, Number, Range, Step }
    internal static class MultiplayerRuleInput
    {
        internal static char ValidateCharacter(string text, int index, char character) =>
            character >= '0' && character <= '9' ? character
                : (character == '.' || character == ',') && !text.Contains(".") && !text.Contains(",") ? '.' : '\0';

        internal static bool TryParse(string text, MultiplayerRuleDefinition definition,
            out MultiplayerRuleValue<float> value)
            => Validate(text, definition, out value) == MultiplayerRuleInputError.None;

        internal static MultiplayerRuleInputError Validate(string text, MultiplayerRuleDefinition definition,
            out MultiplayerRuleValue<float> value)
        {
            value = MultiplayerRuleValue<float>.UseGameBehavior();
            if (string.IsNullOrWhiteSpace(text)) return MultiplayerRuleInputError.None;
            text = text.Trim();
            if (definition.Unit == MultiplayerRuleUnit.PercentagePoints && text.EndsWith("%") ||
                definition.Unit == MultiplayerRuleUnit.Multiplier && text.EndsWith("×"))
                text = text.Substring(0, text.Length - 1).Trim();
            if (!float.TryParse(text.Replace(',', '.'),
                NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint,
                CultureInfo.InvariantCulture, out float number) || float.IsNaN(number) || float.IsInfinity(number))
                return MultiplayerRuleInputError.Number;
            if (number < definition.Minimum || number > definition.Maximum) return MultiplayerRuleInputError.Range;
            if (!definition.IsValidOverride(number)) return MultiplayerRuleInputError.Step;
            value = MultiplayerRuleValue<float>.Override(number);
            return MultiplayerRuleInputError.None;
        }
    }
}
