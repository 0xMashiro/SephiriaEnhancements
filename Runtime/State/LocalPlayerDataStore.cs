#nullable disable
using System;
using System.Collections.Generic;

namespace SephiriaEnhancements.Runtime
{
    // Local data only: no Unity objects, network authority or disk-save ownership.
    // Preferences keep their latest value on Load; progress comes from the snapshot.
    internal sealed class LocalPlayerDataStore
    {
        internal static LocalPlayerDataStore Shared { get; } = new LocalPlayerDataStore();
        private readonly List<IData> entries = new List<IData>();
        internal event Action Saving;

        internal LocalPlayerData<T> Preferences<T>(Func<T> create) where T : class =>
            Add(create, null);

        internal LocalPlayerData<T> Progress<T>(Func<T> create, Func<T, T> copy) where T : class =>
            Add(create, copy ?? throw new ArgumentNullException(nameof(copy)));

        private LocalPlayerData<T> Add<T>(Func<T> create, Func<T, T> copy) where T : class
        {
            var entry = new LocalPlayerData<T>(this, create, copy);
            entries.Add(entry);
            return entry;
        }

        internal Snapshot Capture()
        {
            Saving?.Invoke();
            var snapshot = new Snapshot();
            foreach (IData entry in entries)
                if (entry.IsProgress) snapshot.Values.Add(entry, (entry.Revision, entry.Copy()));
            return snapshot;
        }

        internal void Load(Snapshot snapshot)
        {
            foreach (IData entry in entries)
            {
                if (!entry.IsProgress) continue;
                if (snapshot != null && snapshot.Values.TryGetValue(entry, out var saved) &&
                    saved.Revision == entry.Revision) entry.Load(saved.Value);
                else entry.Load(null);
            }
        }

        internal void Reset()
        {
            foreach (IData entry in entries) entry.Reset();
        }

        internal sealed class Snapshot
        {
            internal readonly Dictionary<IData, (int Revision, object Value)> Values =
                new Dictionary<IData, (int, object)>();
        }

        internal interface IData
        {
            bool IsProgress { get; }
            int Revision { get; }
            object Copy();
            void Load(object value);
            void Reset();
        }

        internal sealed class LocalPlayerData<T> : IData, IDisposable where T : class
        {
            private readonly LocalPlayerDataStore store;
            private readonly Func<T> create;
            private readonly Func<T, T> copy;
            private int revision;
            internal T Value { get; set; }

            internal LocalPlayerData(LocalPlayerDataStore store, Func<T> create, Func<T, T> copy)
            {
                this.store = store;
                this.create = create;
                this.copy = copy;
                Value = create();
            }

            bool IData.IsProgress => copy != null;
            int IData.Revision => revision;
            object IData.Copy() => copy(Value);
            void IData.Load(object value) => Value = value == null ? create() : copy((T)value);
            public void Reset() { Value = create(); revision++; }
            public void Dispose() => store.entries.Remove(this);
        }
    }
}
