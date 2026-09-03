#nullable disable
using System;
using System.Collections.Concurrent;
using System.Threading;

namespace SephiriaEnhancements.Diagnostics
{
    // Owned by the inventory controller, retained across gameplay-context changes.
    // Immutable inputs are serialized and written on this thread, outside solver timing.
    internal sealed class InventoryReproductionLog : IDisposable
    {
        private readonly BlockingCollection<object> pending = new(16);
        private readonly Thread writer;
        private string error;
        private int dropped;

        internal InventoryReproductionLog(string path, string header)
        {
            writer = new Thread(() => Write(path, header))
            { IsBackground = true, Name = "Sephiria inventory reproduction writer" };
            writer.Start();
        }

        internal bool Record(object record)
        {
            try { if (pending.TryAdd(record)) return true; }
            catch (InvalidOperationException) { }
            Interlocked.Increment(ref dropped);
            return false;
        }

        internal string TakeError() => Interlocked.Exchange(ref error, null);
        internal int TakeDroppedCount() => Interlocked.Exchange(ref dropped, 0);

        private void Write(string path, string header)
        {
            try
            {
                using var log = new RollingLogFile(path, 32 * 1024 * 1024, 4, header);
                foreach (object record in pending.GetConsumingEnumerable())
                {
                    log.WriteLine(InventoryReproductionJson.Serialize(record));
                    log.Flush();
                }
            }
            catch (Exception exception)
            {
                Interlocked.Exchange(ref error, exception.GetType().Name);
                pending.CompleteAdding();
            }
        }

        public void Dispose()
        {
            pending.CompleteAdding();
            if (!writer.Join(2000)) Interlocked.Exchange(ref error, "WriterShutdownTimedOut");
        }
    }
}
