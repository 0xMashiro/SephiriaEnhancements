using SephiriaEnhancements.FixedExplorationSeed;
using SephiriaEnhancements.Integration;
using SephiriaEnhancements.MultiplayerRules;

namespace SephiriaEnhancements.ModelChecks.Features.FixedExplorationSeed;

internal static class ExplorationSeedChecks
{
    internal static void Run()
    {
        foreach (int seed in new[] { int.MinValue, -1, 0, 1, 16777217, int.MaxValue })
        {
            string text = ExplorationSeedInput.Format(seed);
            Check(ExplorationSeedInput.TryParse(text, out var parsed) && parsed == seed, "exact signed seed round trip");
        }
        Check(ExplorationSeedInput.TryParse("  ", out var empty) && empty == null, "empty means random");
        foreach (string text in new[] { "-", "2147483648", "-2147483649", "1.5", "1e3", "abc", "1 2" })
            Check(!ExplorationSeedInput.TryParse(text, out _), "invalid seed rejected: " + text);
        string value = "";
        foreach (char digit in "2147483648") value = Key(value, digit.ToString());
        Check(!ExplorationSeedInput.TryParse(value, out _), "overflow stays invalid until corrected");
        value = Key(value, "±");
        Check(ExplorationSeedInput.TryParse(value, out var minimum) && minimum == int.MinValue, "controller can enter minimum seed");
        Check(Key(value, "1") == value, "input length limit");
        Check(Key("-1", "±") == "1" && Key("", "±") == "-", "sign editing");
        Check(Key("1", ".") == "1", "controller rejects decimals");
        Check(Key("", "⌫") == "" && Key("0", "⌫") == "", "backspace can restore random");
        Check(NumberInputEditing.Apply("1.2", ".", 16, MultiplayerRuleInput.ValidateCharacter) == "1.2",
            "multiplayer keypad uses character validation");
        Check(NumberInputEditing.Apply("1", ".", 16, MultiplayerRuleInput.ValidateCharacter) == "1.",
            "multiplayer decimal editing retained");
        Console.WriteLine("Exploration seed and shared number editing checks passed.");
    }

    private static string Key(string value, string key) =>
        NumberInputEditing.Apply(value, key, 11, ExplorationSeedInput.ValidateCharacter);

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
