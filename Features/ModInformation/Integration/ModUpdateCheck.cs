#nullable disable
using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Threading;
using System.Threading.Tasks;

namespace SephiriaEnhancements.ModInformation
{
    internal enum ModUpdateStatus { NotChecked, Checking, UpdateAvailable, UpToDate, Failed, NoPublishedVersion, ConnectionFailed, TimedOut, RateLimited, ServiceError, InvalidResponse }

    internal sealed class ModUpdateResult
    {
        internal ModUpdateStatus Status { get; }
        internal string Version { get; }
        internal int HttpStatus { get; }
        internal ModUpdateResult(ModUpdateStatus status, string version = "", int httpStatus = 0)
        { Status = status; Version = version; HttpStatus = httpStatus; }
    }

    internal static class ModOfficialLinks
    {
        internal const string Nexus = "https://www.nexusmods.com/sephiria/mods/24";
        internal const string GitHub = "https://github.com/0xMashiro/SephiriaEnhancements/releases";
        internal const string ReleasesApi = "https://api.github.com/repos/0xMashiro/SephiriaEnhancements/releases?per_page=100";

        internal static string ReportIssue(string language, string gameVersion, string modVersion, string buildFlavor)
        {
            string template = language == "zh-CN" || language == "zh-TW" ? "bug-report.zh-CN.yml" : "bug-report.en.yml";
            return "https://github.com/0xMashiro/SephiriaEnhancements/issues/new?template=" + template +
                "&versions=" + Uri.EscapeDataString("Sephiria " + gameVersion + " / Sephiria Enhancements " + modVersion + " / " + buildFlavor);
        }
    }

    internal static class ModUpdateCheck
    {
        // GitHub's wire names stay confined to the release reader.
        [DataContract]
        private sealed class Release
        {
            [DataMember(Name = "tag_name", IsRequired = true)] public string Tag { get; set; }
            [DataMember(Name = "draft", IsRequired = true)] public bool Draft { get; set; }
            [DataMember(Name = "prerelease", IsRequired = true)] public bool Prerelease { get; set; }
            [DataMember(Name = "assets", IsRequired = true)] public Asset[] Assets { get; set; }
        }

        [DataContract]
        private sealed class Asset
        {
            [DataMember(Name = "name", IsRequired = true)] public string Name { get; set; }
        }

        internal static ModUpdateResult Read(Stream stream, string installedVersion)
        {
            if (!ModReleaseVersion.TryParse(installedVersion, out ModReleaseVersion installed))
                return new ModUpdateResult(ModUpdateStatus.Failed);
            var releases = (Release[])new DataContractJsonSerializer(typeof(Release[])).ReadObject(stream);
            if (releases == null) return new ModUpdateResult(ModUpdateStatus.InvalidResponse);
            ModReleaseVersion latest = null;
            foreach (Release release in releases)
            {
                if (release == null || release.Draft || release.Tag == null ||
                    !release.Tag.StartsWith("v", StringComparison.Ordinal) ||
                    !ModReleaseVersion.TryParse(release.Tag.Substring(1), out ModReleaseVersion candidate) ||
                    (!installed.IsPrerelease && (candidate.IsPrerelease || release.Prerelease))) continue;
                // An announcement without the installable package is not an available update.
                if (release.Assets == null || !release.Assets.Any(asset => asset != null &&
                    asset.Name == "SephiriaEnhancements-" + candidate.Text + ".zip")) continue;
                if (latest == null || candidate.CompareTo(latest) > 0) latest = candidate;
            }
            return latest == null ? new ModUpdateResult(ModUpdateStatus.NoPublishedVersion) :
                latest.CompareTo(installed) > 0 ? new ModUpdateResult(ModUpdateStatus.UpdateAvailable, latest.Text) :
                new ModUpdateResult(ModUpdateStatus.UpToDate);
        }

        internal static async Task<ModUpdateResult> FetchAsync(string version, CancellationToken cancellation)
        {
            using (var client = new HttpClient { Timeout = TimeSpan.FromSeconds(10) })
                return await FetchAsync(client, version, cancellation).ConfigureAwait(false);
        }

        internal static async Task<ModUpdateResult> FetchAsync(HttpClient client, string version, CancellationToken cancellation)
        {
            try
            {
                using (var request = new HttpRequestMessage(HttpMethod.Get, ModOfficialLinks.ReleasesApi))
                {
                    request.Headers.UserAgent.ParseAdd("SephiriaEnhancements/" + version);
                    request.Headers.Accept.ParseAdd("application/vnd.github+json");
                    using (HttpResponseMessage response = await client.SendAsync(request, cancellation).ConfigureAwait(false))
                    {
                        int status = (int)response.StatusCode;
                        if (!response.IsSuccessStatusCode)
                        {
                            bool limited = status == 429 || (status == 403 &&
                                (response.Headers.RetryAfter != null ||
                                 (response.Headers.TryGetValues("X-RateLimit-Remaining", out var remaining) && remaining.Contains("0"))));
                            return new ModUpdateResult(limited ? ModUpdateStatus.RateLimited : ModUpdateStatus.ServiceError,
                                httpStatus: status);
                        }
                        using (Stream stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false))
                            return Read(stream, version);
                    }
                }
            }
            catch (HttpRequestException) { return new ModUpdateResult(ModUpdateStatus.ConnectionFailed); }
            catch (IOException) { return new ModUpdateResult(ModUpdateStatus.ConnectionFailed); }
            catch (OperationCanceledException)
            {
                cancellation.ThrowIfCancellationRequested();
                return new ModUpdateResult(ModUpdateStatus.TimedOut);
            }
            catch (SerializationException) { return new ModUpdateResult(ModUpdateStatus.InvalidResponse); }
            catch (System.Xml.XmlException) { return new ModUpdateResult(ModUpdateStatus.InvalidResponse); }
        }
    }
}
