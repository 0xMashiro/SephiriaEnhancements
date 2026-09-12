using System.Text;
using System.Text.Json;
using SephiriaEnhancements.ModInformation;

namespace SephiriaEnhancements.ModelChecks.Features.ModInformation;

internal static class ModInformationChecks
{
    internal static void Run()
    {
        ModUpdateNetworkChecks.Run().GetAwaiter().GetResult();
        string[] ordered = { "0.5.0-alpha.9", "0.5.0-beta.2", "0.5.0-beta.10", "0.5.0-rc.1", "0.5.0", "0.5.1", "0.10.0", "1.0.0" };
        for (int i = 1; i < ordered.Length; i++)
        {
            Require(ModReleaseVersion.TryParse(ordered[i - 1], out var before), "valid version");
            Require(ModReleaseVersion.TryParse(ordered[i], out var after), "valid version");
            Require(after.CompareTo(before) > 0 && before.CompareTo(after) < 0, "SemVer ordering");
        }
        foreach (string invalid in new[] { "", "v1.0.0", "1.0", "1.00.0", "1.0.0-beta.01", "1.0.0-beta", "1.0.0+build", "1.0.0\n", "99999999999.0.0" })
            Require(!ModReleaseVersion.TryParse(invalid, out _), "invalid version accepted: " + invalid);

        var releases = new[]
        {
            Release("0.5.0-beta.10", true), Release("0.4.0"), Release("0.5.0-beta.2", true),
            Release("1.0.0", draft: true), Release("2.0.0", package: false), Release("0.3.0")
        };
        ModUpdateResult beta = Read(releases, "0.5.0-beta.8");
        Require(beta.Status == ModUpdateStatus.UpdateAvailable && beta.Version == "0.5.0-beta.10", "beta channel and numeric ordering");
        ModUpdateResult stable = Read(releases, "0.3.0");
        Require(stable.Status == ModUpdateStatus.UpdateAvailable && stable.Version == "0.4.0", "stable channel excludes prerelease/draft/announcement");
        Require(Read(releases, "0.5.0").Status == ModUpdateStatus.UpToDate, "local version ahead is not an update");
        Require(Read(new[] { Release("0.5.0") }, "0.5.0-beta.10").Status == ModUpdateStatus.UpdateAvailable, "beta upgrades to stable");
        Require(Read(new[] { Release("0.5.0", true) }, "0.4.0").Status == ModUpdateStatus.NoPublishedVersion, "GitHub prerelease flag also respected");
        Require(Read(Array.Empty<object>(), "0.5.0").Status == ModUpdateStatus.NoPublishedVersion, "empty feed is not up-to-date");
        Require(Read(releases, "development").Status == ModUpdateStatus.Failed, "uncomparable installed version");
        using (var broken = new MemoryStream(Encoding.UTF8.GetBytes("[{\"tag_name\":\"v9.0.0\"}]")))
        {
            bool failed = false;
            try { ModUpdateCheck.Read(broken, "0.5.0"); }
            catch (System.Runtime.Serialization.SerializationException) { failed = true; }
            Require(failed, "incomplete response must not become up-to-date");
        }

        var welcomeLanguages = new HashSet<string>();
        ModInformationLocalization.Register((language, key, text) =>
        {
            if (key != ModInformationLocalization.Welcome) return;
            string message = string.Format(text, "SEPHIRIA ENHANCEMENTS · by 0xMashiro", "0.5.0-beta.9", "1.0.31");
            Require(message.Contains("0.5.0-beta.9") && message.Contains("Sephiria 1.0.31"), "welcome identifies Mod and game versions");
            Require(message.Split('\n').Length == 3, "welcome leaves room for official links in native log pool");
            welcomeLanguages.Add(language);
        }, SephiriaEnhancements.Configuration.LocalizationLanguages.All);
        Require(welcomeLanguages.Count == SephiriaEnhancements.Configuration.LocalizationLanguages.All.Length, "all welcome languages verified");
        CheckReporting();

        var state = new ModInformationState();
        Require(!state.BeginAutomaticCheck(true), "no automatic network request before gameplay");
        Require(state.EnterGameplay(true), "first entry welcomes");
        Require(!state.EnterGameplay(true), "reconnect, floor, world and mod recreation share process state");
        Require(!state.BeginAutomaticCheck(false), "opt-out does not request");
        Require(state.BeginAutomaticCheck(true) && !state.BeginAutomaticCheck(true), "one automatic attempt per launch");
        state.Result = new ModUpdateResult(ModUpdateStatus.UpdateAvailable, "0.5.0");
        Require(!state.TakeUpdateNotice(false), "no notice when disabled");
        Require(state.TakeUpdateNotice(true) && !state.TakeUpdateNotice(true), "one notice per detected version");
        state.Result = new ModUpdateResult(ModUpdateStatus.Failed);
        Require(!state.TakeUpdateNotice(true), "automatic failure never masquerades as update");
        state.Result = new ModUpdateResult(ModUpdateStatus.UpdateAvailable, "0.5.1");
        Require(state.TakeUpdateNotice(true), "newer release may notify");
        var silent = new ModInformationState();
        Require(!silent.EnterGameplay(false) && !silent.EnterGameplay(true), "welcome preference cannot cause a mid-run first-entry message");
        Require(silent.BeginAutomaticCheck(true), "welcome and update preferences independent");
        var manuallyChecked = new ModInformationState { Result = new ModUpdateResult(ModUpdateStatus.UpToDate) };
        manuallyChecked.EnterGameplay(true);
        Require(!manuallyChecked.BeginAutomaticCheck(true), "manual check before loading avoids duplicate request");
        Console.WriteLine("Mod information: version/channel/package selection, response validity and process notification lifecycle passed");
    }

    private static object Release(string version, bool prerelease = false, bool draft = false, bool package = true) =>
        new { tag_name = "v" + version, prerelease, draft, assets = package ? new[] { new { name = "SephiriaEnhancements-" + version + ".zip" } } : Array.Empty<object>() };

    private static void CheckReporting()
    {
        const string version = "0.5.0-beta.9+test&template=other#fragment";
        foreach (string language in SephiriaEnhancements.Configuration.LocalizationLanguages.All)
        {
            var uri = new Uri(ModOfficialLinks.ReportIssue(language, "1.0.31", version, "Development"));
            Require(uri.Scheme == "https" && uri.Host == "github.com" &&
                uri.AbsolutePath == "/0xMashiro/SephiriaEnhancements/issues/new" && uri.Fragment.Length == 0,
                "report opens an unsubmitted form at the official repository");
            string[] query = uri.Query.TrimStart('?').Split('&');
            string template = language is "zh-CN" or "zh-TW" ? "bug-report.zh-CN.yml" : "bug-report.en.yml";
            Require(query.Length == 2 && query[0] == "template=" + template &&
                Uri.UnescapeDataString(query[1]) == "versions=Sephiria 1.0.31 / Sephiria Enhancements " + version + " / Development",
                "report safely prefills only versions and build flavor in the matching form");
        }

        var texts = new Dictionary<string, Dictionary<string, string>>();
        void Add(string language, string key, string value)
        {
            if (!texts.TryGetValue(language, out var entries)) texts[language] = entries = new();
            entries.Add(key, value);
        }
        var languages = SephiriaEnhancements.Configuration.LocalizationLanguages.All;
        ModInformationLocalization.Register(Add, languages);
        SephiriaEnhancements.Configuration.OptionsCategoryLocalization.Register(Add, languages);
        SephiriaEnhancements.Runtime.FeatureFailureLocalization.Register(Add, languages);
        SephiriaEnhancements.Inventory.InventoryOptimizationLocalization.Register(Add);
        foreach (var entries in texts.Values)
        {
            string details = string.Format(entries[ModInformationLocalization.ReportDetails], "1.0.31", "0.5.0-beta.9", "Development");
            Require(details.Contains("1.0.31") && details.Contains("0.5.0-beta.9") && details.Contains("Development") &&
                !details.Contains("{0}"), "copyable report identifies the running versions and leaves the problem for the player to describe");
            Require(entries[ModInformationLocalization.ReportIssue].Contains("GitHub"), "account-based destination is explicit before activation");
            string category = entries[SephiriaEnhancements.Configuration.OptionsCategoryLocalization.CategoryKeys[^1]];
            string button = entries[ModInformationLocalization.LogFolder];
            foreach (string key in new[] {
                SephiriaEnhancements.Runtime.FeatureFailureLocalization.SettingsHelp,
                SephiriaEnhancements.Inventory.InventoryOptimizationLocalization.StartUnavailable,
                SephiriaEnhancements.Inventory.InventoryOptimizationLocalization.DisabledAfterError })
                Require(entries[key].Contains(category) && entries[key].Contains(button), "failure guidance matches the exact localized settings route");
            Require(entries[ModInformationLocalization.LogFolderHelp].Contains("support*.log"), "log help identifies report attachments");
        }
        var english = texts["en-US"];
        SephiriaEnhancements.Runtime.FeatureFailure.Reset();
        Require(SephiriaEnhancements.Runtime.FeatureFailureLocalization.Describe(
            new[] { SephiriaEnhancements.Runtime.FeatureId.Inventory }, key => english[key]).Contains(english[ModInformationLocalization.LogFolder]),
            "failure points to settings when available");
        SephiriaEnhancements.Runtime.FeatureFailure.Disable(SephiriaEnhancements.Runtime.FeatureId.Settings, new Exception());
        Require(!SephiriaEnhancements.Runtime.FeatureFailureLocalization.Describe(
            new[] { SephiriaEnhancements.Runtime.FeatureId.Settings }, key => english[key]).Contains(english[ModInformationLocalization.LogFolder]),
            "settings failure must not direct players to an unavailable entry");
        SephiriaEnhancements.Runtime.FeatureFailure.Reset();
    }

    private static ModUpdateResult Read(object[] releases, string installed)
    {
        using var stream = new MemoryStream(JsonSerializer.SerializeToUtf8Bytes(releases));
        return ModUpdateCheck.Read(stream, installed);
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
