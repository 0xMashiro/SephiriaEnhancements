using SephiriaEnhancements.Configuration;

namespace SephiriaEnhancements.ModelChecks.Configuration;

internal static class LocalizationGroupChecks
{
    internal static void Run()
    {
        string[] keys = { "Label", "Help" };
        string[] languages = { "en-US", "zh-CN", "partial", "blank", "unknown" };
        var arrays = new Dictionary<string, string[]>
        {
            ["en-US"] = new[] { "Label", "Help {0}" },
            ["zh-CN"] = new[] { "名称", "帮助 {0}" },
            ["partial"] = new[] { "Only one" },
            ["blank"] = new[] { "Translated label", " " }
        };
        var registered = new Dictionary<(string, string), string>();
        LocalizationGroup.Register((language, key, value) => registered.Add((language, key), value), languages, keys, arrays);
        foreach (string language in languages)
        {
            string[] expected = language == "zh-CN" ? arrays[language] : arrays["en-US"];
            for (int index = 0; index < keys.Length; index++)
                if (registered[(language, keys[index])] != expected[index])
                    throw new InvalidOperationException("Array groups must fall back together, including blank or incomplete translations");
        }

        var dictionaries = new Dictionary<string, Dictionary<string, string>>
        {
            ["en-US"] = new() { ["Label"] = "Label", ["Help"] = "Help {0}" },
            ["zh-CN"] = new() { ["Label"] = "名称", ["Help"] = "帮助 {0}" },
            ["partial"] = new() { ["Label"] = "Only one" },
            ["blank"] = new() { ["Label"] = "Translated label", ["Help"] = "" },
            ["wrong-key"] = new() { ["Label"] = "Translated label", ["Other"] = "Other help" }
        };
        var keyed = new Dictionary<(string, string), string>();
        LocalizationGroup.Register((language, key, value) => keyed.Add((language, key), value),
            languages.Append("wrong-key"), dictionaries);
        if (registered.Any(entry => keyed[entry.Key] != entry.Value) || keyed[("wrong-key", "Help")] != "Help {0}")
            throw new InvalidOperationException("Keyed and array groups must use identical whole-group fallback rules");

        arrays["en-US"] = new[] { "Incomplete" };
        bool rejected = false;
        try { LocalizationGroup.Register((_, _, _) => { }, languages, keys, arrays); }
        catch (InvalidOperationException) { rejected = true; }
        if (!rejected) throw new InvalidOperationException("An invalid English source must fail registration");

        var english = new Dictionary<string, string>();
        ModLocalization.Register((language, key, value) =>
        {
            if (language == "en-US") english.Add(key, value);
        });
        foreach (var text in english)
            if (ModLocalization.GetEnglish(text.Key) != text.Value)
                throw new InvalidOperationException("English fallback omitted a registered feature: " + text.Key);
        if (ModLocalization.GetEnglish("Unknown.Key") != "Unknown.Key")
            throw new InvalidOperationException("Unknown keys must remain visible for diagnosis");
        Console.WriteLine("Localization groups: complete/partial/blank/unknown fallback and full English lookup passed");
    }
}
