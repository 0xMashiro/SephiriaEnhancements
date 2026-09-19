using System.Globalization;

namespace SephiriaEnhancements.FixedExplorationSeed
{
    internal static class ExplorationSeedInput
    {
        internal static bool TryParse(string text, out int? seed)
        {
            seed = null;
            if (string.IsNullOrWhiteSpace(text)) return true;
            if (!int.TryParse(text.Trim(), NumberStyles.AllowLeadingSign,
                    CultureInfo.InvariantCulture, out int value)) return false;
            seed = value;
            return true;
        }

        internal static string Format(int? seed) => seed?.ToString(CultureInfo.InvariantCulture) ?? "";

        internal static char ValidateCharacter(string text, int index, char character) =>
            character >= '0' && character <= '9' ? character :
            character == '-' && index == 0 && !text.Contains("-") ? character : '\0';
    }
}
