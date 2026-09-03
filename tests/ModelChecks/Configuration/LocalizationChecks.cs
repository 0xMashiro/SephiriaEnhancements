using System.Collections;
using System.Globalization;
using System.Reflection;
using System.Text.RegularExpressions;
using SephiriaEnhancements.CombatVisuals;
using SephiriaEnhancements.Configuration;
using SephiriaEnhancements.Inventory;
using SephiriaEnhancements.MapEnhancements;
using SephiriaEnhancements.MultiplayerAccess.Presentation;
using SephiriaEnhancements.MultiplayerRules.Presentation;

namespace SephiriaEnhancements.ModelChecks.Configuration;

internal static class LocalizationChecks
{
    internal static void Run()
    {
        var languages = LocalizationLanguages.All.ToHashSet(StringComparer.Ordinal);
        if (languages.Count != 15 || languages.Count != LocalizationLanguages.All.Length)
            throw new InvalidOperationException("localization languages must be unique and cover the game");

        // Inspect source tables as well as registered output: English fallback must not
        // be mistaken for a completed translation just because a key was registered.
        Type[] owners =
        {
            typeof(ModLocalization), typeof(ControlLocalization),
            typeof(OptionsCategoryLocalization), typeof(CombatVisualLocalization),
            typeof(InventoryOptimizationLocalization), typeof(MapEnhancementsLocalization),
            typeof(MultiplayerAccessLocalization), typeof(MultiplayerRulesLocalization)
        };
        int tableCount = 0;
        foreach (Type owner in owners)
        {
            foreach (FieldInfo field in owner.GetFields(BindingFlags.Static | BindingFlags.NonPublic))
            {
                if (field.GetValue(null) is not IDictionary table ||
                    !table.Keys.Cast<object>().OfType<string>().Any(languages.Contains)) continue;
                string name = owner.Name + "." + field.Name;
                if (!languages.SetEquals(table.Keys.Cast<string>()))
                    throw new InvalidOperationException("incomplete translation table: " + name);
                foreach (string language in languages)
                {
                    CheckShape(table["en-US"]!, table[language]!, name + "/" + language);
                    if (language != "en-US" && Flatten(table[language]!).SequenceEqual(Flatten(table["en-US"]!)))
                        throw new InvalidOperationException("whole table is still English: " + name + "/" + language);
                }
                tableCount++;
            }
        }

        var texts = languages.ToDictionary(language => language,
            _ => new Dictionary<string, string>(StringComparer.Ordinal));
        ModLocalization.Register((language, key, value) => texts[language].Add(key, value));
        Dictionary<string, string> english = texts["en-US"];
        foreach (var (language, entries) in texts)
        {
            if (!entries.Keys.ToHashSet().SetEquals(english.Keys))
                throw new InvalidOperationException("registered key mismatch: " + language);
            foreach (var (key, value) in entries)
            {
                if (string.IsNullOrWhiteSpace(value) || value != value.Trim() ||
                    value.Contains('\uFFFD') || value.Contains("SephiriaEnhancements.", StringComparison.Ordinal) ||
                    Regex.IsMatch(value, "@(ROOT|BLOOD|WANDER|GUILD|POTION|SAMPLE|QLIPHOTH|TEMPLE)"))
                    throw new InvalidOperationException($"invalid UI text: {language}/{key}");
                if (language != "en-US" && value == english[key] &&
                    Regex.IsMatch(value, "[A-Za-z]") && !SharesEnglishSpelling(language, key))
                    throw new InvalidOperationException($"untranslated UI text: {language}/{key}");
                int[] placeholders = Placeholders(value);
                if (!placeholders.SequenceEqual(Placeholders(english[key])))
                    throw new InvalidOperationException($"placeholder mismatch: {language}/{key}");
                // CompositeFormat also catches unmatched braces and malformed format items.
                System.Text.CompositeFormat.Parse(value);
                if (placeholders.Length > 0)
                {
                    object[] values = Enumerable.Range(0, placeholders.Max() + 1)
                        .Select(index => (object)("BOUND-" + index)).ToArray();
                    string formatted = string.Format(CultureInfo.InvariantCulture, value, values);
                    if (placeholders.Any(index => !formatted.Contains("BOUND-" + index, StringComparison.Ordinal)))
                        throw new InvalidOperationException($"lost placeholder: {language}/{key}");
                }
            }
        }

        if (texts["zh-CN"][InventoryOptimizationLocalization.HudComboTargets] != "连招目标" ||
            texts["zh-TW"][InventoryOptimizationLocalization.HudComboTargets] != "連招目標" ||
            texts["zh-CN"][ModLocalization.SettingHitStreakFeedback] != "连续命中提示")
            throw new InvalidOperationException("artifact combos and consecutive hits must remain distinct concepts");

        foreach (var (language, entries) in texts)
        {
            if (entries[ModLocalization.Section] != "SEPHIRIA ENHANCEMENTS · by 0xMashiro")
                throw new InvalidOperationException("brand text must remain fixed: " + language);
            string[] inventoryFailures =
            {
                InventoryOptimizationLocalization.RuntimeNotReady,
                InventoryOptimizationLocalization.Unsupported,
                InventoryOptimizationLocalization.Changed,
                InventoryOptimizationLocalization.ApplyTimedOut,
                InventoryOptimizationLocalization.VerificationFailed,
                InventoryOptimizationLocalization.DisabledForGameplayContext
            };
            if (inventoryFailures.Select(key => entries[key]).Distinct().Count() != inventoryFailures.Length)
                throw new InvalidOperationException("inventory failure reasons must remain distinguishable: " + language);
        }
        Console.WriteLine($"Localization: {languages.Count} languages, {tableCount} complete source tables, " +
            $"{english.Count} keys each; placeholders, terminology and distinct failure messages passed");
    }

    private static int[] Placeholders(string text) => Regex.Matches(text, @"(?<!\{)\{(\d+)(?:[^{}]*)\}(?!\})")
        .Select(match => int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture)).Order().ToArray();

    // These are actual shared spellings, proper names or permitted HUD abbreviations.
    // Keep exceptions tied to both the key and language, rather than allowing English words globally.
    private static bool SharesEnglishSpelling(string language, string key) => key switch
    {
        ModLocalization.Section or ModLocalization.SettingMasterEnabled or
            ModLocalization.Dps or ModLocalization.ReportDamage => true,
        ModLocalization.NormalEnemy => language is "de-DE" or "es-ES" or "fr-FR" or "pt-BR" or "tr-TR",
        ModLocalization.MinibossEnemy => language is "it-IT" or "pl-PL" or "sv-SE",
        ModLocalization.DamageNormal => language is "de-DE" or "es-ES" or "fr-FR" or "pt-BR" or "sv-SE" or "tr-TR",
        MultiplayerRulesLocalization.OriginalPreset => language is "de-DE" or "es-ES" or "fr-FR" or "pt-BR" or "sv-SE",
        MultiplayerRulesLocalization.GroupQliphoth => language is "de-DE" or "es-ES" or "fr-FR" or "it-IT" or
            "pl-PL" or "sv-SE" or "th-TH" or "tr-TR",
        InventoryOptimizationLocalization.HudPage => language == "fr-FR",
        _ when key == OptionsCategoryLocalization.CategoryKeys[(int)OptionsCategory.General] => language == "es-ES",
        _ when key == CombatVisualLocalization.PresetKeys[2] => language is "de-DE" or "fr-FR",
        _ when key == CombatVisualLocalization.TransparencyKeys[0] => language is "de-DE" or "es-ES" or "fr-FR" or
            "pt-BR" or "sv-SE" or "tr-TR",
        _ when key == InventoryOptimizationLocalization.OptimizationTendencyKeys[1] => language == "fr-FR",
        _ => false
    };

    private static void CheckShape(object expected, object actual, string location)
    {
        if (expected is IDictionary expectedMap && actual is IDictionary actualMap)
        {
            if (!expectedMap.Keys.Cast<object>().ToHashSet().SetEquals(actualMap.Keys.Cast<object>()))
                throw new InvalidOperationException("translation key mismatch: " + location);
            foreach (object key in expectedMap.Keys)
                CheckShape(expectedMap[key]!, actualMap[key]!, location + "/" + key);
        }
        else if (expected is string[] expectedValues && actual is string[] actualValues &&
            expectedValues.Length != actualValues.Length)
            throw new InvalidOperationException("translation row length mismatch: " + location);
    }

    private static IEnumerable<string> Flatten(object value) => value switch
    {
        string text => new[] { text },
        string[] texts => texts,
        IDictionary table => table.Keys.Cast<object>().OrderBy(key => key.ToString(), StringComparer.Ordinal)
            .SelectMany(key => Flatten(table[key]!)),
        _ => throw new InvalidOperationException("unexpected localization table shape")
    };
}
