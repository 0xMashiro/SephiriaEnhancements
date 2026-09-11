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
