#nullable disable
using System;
using System.Globalization;
using System.Text.RegularExpressions;

namespace SephiriaEnhancements.ModInformation
{
    // Published versions follow the project's MAJOR.MINOR.PATCH[-alpha|beta|rc.N] convention.
    internal sealed class ModReleaseVersion : IComparable<ModReleaseVersion>
    {
        private static readonly Regex Pattern = new Regex(
            @"\A(0|[1-9][0-9]*)\.(0|[1-9][0-9]*)\.(0|[1-9][0-9]*)(?:-(alpha|beta|rc)\.(0|[1-9][0-9]*))?\z",
            RegexOptions.CultureInvariant);
        private readonly int major, minor, patch, channel, sequence;
        internal string Text { get; }
        internal bool IsPrerelease => channel < 3;

        private ModReleaseVersion(string text, int major, int minor, int patch,
            int channel, int sequence)
        {
            Text = text;
            this.major = major;
            this.minor = minor;
            this.patch = patch;
            this.channel = channel;
            this.sequence = sequence;
        }

        internal static bool TryParse(string text, out ModReleaseVersion version)
        {
            version = null;
            if (string.IsNullOrEmpty(text)) return false;
            Match match = Pattern.Match(text);
            if (!match.Success ||
                !int.TryParse(match.Groups[1].Value, NumberStyles.None, CultureInfo.InvariantCulture, out int major) ||
                !int.TryParse(match.Groups[2].Value, NumberStyles.None, CultureInfo.InvariantCulture, out int minor) ||
                !int.TryParse(match.Groups[3].Value, NumberStyles.None, CultureInfo.InvariantCulture, out int patch)) return false;
            int channel = match.Groups[4].Value == "alpha" ? 0 :
                match.Groups[4].Value == "beta" ? 1 : match.Groups[4].Value == "rc" ? 2 : 3;
            int sequence = 0;
            if (channel < 3 && !int.TryParse(match.Groups[5].Value, NumberStyles.None,
                CultureInfo.InvariantCulture, out sequence)) return false;
            version = new ModReleaseVersion(text, major, minor, patch, channel, sequence);
            return true;
        }

        public int CompareTo(ModReleaseVersion other)
        {
            if (other == null) return 1;
            int result = major.CompareTo(other.major);
            if (result == 0) result = minor.CompareTo(other.minor);
            if (result == 0) result = patch.CompareTo(other.patch);
            if (result == 0) result = channel.CompareTo(other.channel);
            return result == 0 ? sequence.CompareTo(other.sequence) : result;
        }
    }
}
