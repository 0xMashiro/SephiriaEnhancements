#nullable disable
using System;
using System.Globalization;

namespace SephiriaEnhancements.Diagnostics
{
    internal sealed class SupportLog : IDisposable
    {
        private readonly RollingLogFile file;
        private string previous;
        private DateTime previousWrittenAt;
        private int repeats;

        internal SupportLog(string path, string header)
        {
            file = new RollingLogFile(path, 1024 * 1024, 3, header);
        }

        internal void Record(string code, string details, string level, DateTime utc)
        {
            string entry = level + " " + code + (details.Length == 0 ? "" : " " + details);
            entry = entry.Replace('\r', ' ').Replace('\n', ' ');
            if (entry == previous)
            {
                repeats++;
                if ((utc - previousWrittenAt).TotalSeconds >= 30) FlushRepeats(utc);
                return;
            }
            FlushRepeats(utc);
            previous = entry;
            previousWrittenAt = utc;
            Write(utc, entry);
        }

        private void FlushRepeats(DateTime utc)
        {
            if (repeats == 0) return;
            Write(utc, previous + " repeated=" + repeats);
            repeats = 0;
            previousWrittenAt = utc;
        }

        private void Write(DateTime utc, string entry)
        {
            file.WriteLine(utc.ToString("O", CultureInfo.InvariantCulture) + " " + entry);
            file.Flush();
        }

        public void Dispose()
        {
            try { FlushRepeats(DateTime.UtcNow); }
            finally { file.Dispose(); }
        }
    }
}
