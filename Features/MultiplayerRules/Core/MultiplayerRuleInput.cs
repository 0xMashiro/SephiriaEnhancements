using System.Globalization;

namespace SephiriaEnhancements.MultiplayerRules
{
    internal static class MultiplayerRuleInput
    {
        internal static bool TryParse(string text, MultiplayerRuleDefinition definition,
            out MultiplayerRuleValue<float> value)
        {
            value = MultiplayerRuleValue<float>.UseGameBehavior();
            if (string.IsNullOrWhiteSpace(text)) return true;
            text = text.Trim();
            if (definition.Unit == MultiplayerRuleUnit.PercentagePoints && text.EndsWith("%") ||
                definition.Unit == MultiplayerRuleUnit.Multiplier && text.EndsWith("×"))
                text = text.Substring(0, text.Length - 1).Trim();
            if (!float.TryParse(text.Replace(',', '.'),
                NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint,
                CultureInfo.InvariantCulture, out float number) || !definition.IsValidOverride(number))
                return false;
            value = MultiplayerRuleValue<float>.Override(number);
            return true;
        }
    }
}
