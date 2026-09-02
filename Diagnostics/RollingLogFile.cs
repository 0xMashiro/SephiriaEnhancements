#nullable disable
using System;
using System.IO;
using System.Text;

namespace SephiriaEnhancements.Diagnostics
{
    // One owner writes each file; support uses the main thread, diagnostics its writer thread.
    internal sealed class RollingLogFile : IDisposable
    {
        private static readonly Encoding Encoding = new UTF8Encoding(false);
        private readonly string path;
        private readonly int maximumBytes;
        private readonly int retainedFiles;
        private readonly string header;
        private StreamWriter writer;
        private int bytes;

        internal RollingLogFile(string path, int maximumBytes, int retainedFiles, string header)
        {
            if (maximumBytes < 256 || retainedFiles < 1)
                throw new ArgumentOutOfRangeException(nameof(maximumBytes));
            if (Encoding.GetByteCount(header + "\n") > maximumBytes / 2)
                throw new ArgumentException("Log header is too large.", nameof(header));
            this.path = Path.GetFullPath(path);
            this.maximumBytes = maximumBytes;
            this.retainedFiles = retainedFiles;
            this.header = header;
            Directory.CreateDirectory(Path.GetDirectoryName(this.path));
            Rotate();
        }

        internal void WriteLine(string line)
        {
            int length = Encoding.GetByteCount(line + "\n");
            if (length > maximumBytes - Encoding.GetByteCount(header + "\n"))
            {
                line = "{\"event\":\"log_record_omitted\",\"reason\":\"size_limit\",\"bytes\":" + length + "}";
                length = Encoding.GetByteCount(line + "\n");
            }
            if (bytes + length > maximumBytes) Rotate();
            writer.WriteLine(line);
            bytes += length;
        }

        internal void Flush() => writer.Flush();

        private void Rotate()
        {
            writer?.Dispose();
            writer = null;
            string oldest = ArchivePath(retainedFiles - 1);
            if (File.Exists(oldest)) File.Delete(oldest);
            for (int index = retainedFiles - 2; index >= 0; index--)
            {
                string source = ArchivePath(index);
                if (File.Exists(source)) File.Move(source, ArchivePath(index + 1));
            }
            writer = new StreamWriter(new FileStream(path, FileMode.CreateNew,
                FileAccess.Write, FileShare.Read), Encoding)
            { NewLine = "\n" };
            writer.WriteLine(header);
            bytes = Encoding.GetByteCount(header + "\n");
            writer.Flush();
        }

        private string ArchivePath(int index) => index == 0 ? path :
            Path.Combine(Path.GetDirectoryName(path),
                Path.GetFileNameWithoutExtension(path) + "." + index + Path.GetExtension(path));

        public void Dispose() => writer?.Dispose();
    }
}
