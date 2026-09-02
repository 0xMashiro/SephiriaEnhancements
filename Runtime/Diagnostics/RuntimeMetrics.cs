#nullable disable
using SephiriaEnhancements.Runtime.Inventory;

using System;

namespace SephiriaEnhancements.Runtime
{
    internal enum RuntimeEventKind
    {
        Reconciliation,
        GridInventoryStartPermission,
        GridInventoryEndPermission,
        ItemUpdated,
        ItemAdded,
        ItemRemoved,
        InventoryStorageChanged,
        InventoryHeightChanged,
        UniquePairEnchanted,
        TabletRotated,
        ItemIdentified,
        CharmEffectRefreshed,
        InventoryCleared,
        InventoryCatalogRefreshed,
        InventoryCatalogRefreshFailed,
        NativePresetRefreshed,
        Count
    }

    internal sealed class RuntimeMetricSnapshot
    {
        internal RuntimeMetricSnapshot(int[] eventCounts, int captures,
            int failedCaptures, float averageCaptureMilliseconds,
            float p50CaptureMilliseconds, float p95CaptureMilliseconds,
            float maximumCaptureMilliseconds, int catalogCaptures,
            int failedCatalogCaptures, float averageCatalogCaptureMilliseconds,
            float maximumCatalogCaptureMilliseconds, int tabletQueryCacheHits,
            int tabletQueryCacheMisses, int failedTabletQueries,
            float averageTabletQueryMilliseconds, int presetCaptures,
            int failedPresetCaptures, float averagePresetCaptureMilliseconds,
            float maximumPresetCaptureMilliseconds)
        {
            EventCounts = eventCounts;
            Captures = captures;
            FailedCaptures = failedCaptures;
            AverageCaptureMilliseconds = averageCaptureMilliseconds;
            P50CaptureMilliseconds = p50CaptureMilliseconds;
            P95CaptureMilliseconds = p95CaptureMilliseconds;
            MaximumCaptureMilliseconds = maximumCaptureMilliseconds;
            CatalogCaptures = catalogCaptures;
            FailedCatalogCaptures = failedCatalogCaptures;
            AverageCatalogCaptureMilliseconds = averageCatalogCaptureMilliseconds;
            MaximumCatalogCaptureMilliseconds = maximumCatalogCaptureMilliseconds;
            TabletQueryCacheHits = tabletQueryCacheHits;
            TabletQueryCacheMisses = tabletQueryCacheMisses;
            FailedTabletQueries = failedTabletQueries;
            AverageTabletQueryMilliseconds = averageTabletQueryMilliseconds;
            PresetCaptures = presetCaptures;
            FailedPresetCaptures = failedPresetCaptures;
            AveragePresetCaptureMilliseconds = averagePresetCaptureMilliseconds;
            MaximumPresetCaptureMilliseconds = maximumPresetCaptureMilliseconds;
        }

        internal int[] EventCounts { get; }
        internal int Captures { get; }
        internal int FailedCaptures { get; }
        internal float AverageCaptureMilliseconds { get; }
        internal float P50CaptureMilliseconds { get; }
        internal float P95CaptureMilliseconds { get; }
        internal float MaximumCaptureMilliseconds { get; }
        internal int CatalogCaptures { get; }
        internal int FailedCatalogCaptures { get; }
        internal float AverageCatalogCaptureMilliseconds { get; }
        internal float MaximumCatalogCaptureMilliseconds { get; }
        internal int TabletQueryCacheHits { get; }
        internal int TabletQueryCacheMisses { get; }
        internal int FailedTabletQueries { get; }
        internal float AverageTabletQueryMilliseconds { get; }
        internal int PresetCaptures { get; }
        internal int FailedPresetCaptures { get; }
        internal float AveragePresetCaptureMilliseconds { get; }
        internal float MaximumPresetCaptureMilliseconds { get; }
    }

    internal sealed class RuntimeMetrics
    {
        private const int CaptureSampleCapacity = 128;
        private readonly int[] eventCounts =
            new int[(int)RuntimeEventKind.Count];
        private readonly float[] captureSamples = new float[CaptureSampleCapacity];
        private int captureSampleCount;
        private int captureSampleWriteIndex;
        private int captures;
        private int failedCaptures;
        private double captureMillisecondsTotal;
        private float maximumCaptureMilliseconds;
        private int catalogCaptures;
        private int failedCatalogCaptures;
        private double catalogCaptureMillisecondsTotal;
        private float maximumCatalogCaptureMilliseconds;
        private int tabletQueryCacheHits;
        private int tabletQueryCacheMisses;
        private int failedTabletQueries;
        private double tabletQueryMillisecondsTotal;
        private int presetCaptures;
        private int failedPresetCaptures;
        private double presetCaptureMillisecondsTotal;
        private float maximumPresetCaptureMilliseconds;

        internal void RecordEvent(RuntimeEventKind kind)
        {
            int index = (int)kind;
            if (index >= 0 && index < eventCounts.Length)
            {
                eventCounts[index]++;
            }
        }

        internal void RecordCapture(float elapsedMilliseconds, bool succeeded)
        {
            captures++;
            if (!succeeded)
            {
                failedCaptures++;
            }

            float nonNegativeElapsed = Math.Max(0f, elapsedMilliseconds);
            captureMillisecondsTotal += nonNegativeElapsed;
            maximumCaptureMilliseconds = Math.Max(maximumCaptureMilliseconds,
                nonNegativeElapsed);
            captureSamples[captureSampleWriteIndex] = nonNegativeElapsed;
            captureSampleWriteIndex = (captureSampleWriteIndex + 1) %
                captureSamples.Length;
            captureSampleCount = Math.Min(captureSampleCount + 1,
                captureSamples.Length);
        }

        internal RuntimeMetricSnapshot TakeSnapshotAndReset()
        {
            int[] counts = (int[])eventCounts.Clone();
            float[] samples = new float[captureSampleCount];
            Array.Copy(captureSamples, samples, captureSampleCount);
            Array.Sort(samples);
            float average = captures == 0
                ? 0f
                : (float)(captureMillisecondsTotal / captures);
            float catalogAverage = catalogCaptures == 0
                ? 0f
                : (float)(catalogCaptureMillisecondsTotal / catalogCaptures);
            RuntimeMetricSnapshot result = new RuntimeMetricSnapshot(counts,
                captures, failedCaptures, average, Percentile(samples, 0.5f),
                Percentile(samples, 0.95f), maximumCaptureMilliseconds,
                catalogCaptures, failedCatalogCaptures, catalogAverage,
                maximumCatalogCaptureMilliseconds, tabletQueryCacheHits,
                tabletQueryCacheMisses, failedTabletQueries,
                tabletQueryCacheMisses == 0 ? 0f :
                    (float)(tabletQueryMillisecondsTotal /
                        tabletQueryCacheMisses), presetCaptures,
                failedPresetCaptures, presetCaptures == 0 ? 0f :
                    (float)(presetCaptureMillisecondsTotal / presetCaptures),
                maximumPresetCaptureMilliseconds);

            Reset();
            return result;
        }

        internal void Reset()
        {
            Array.Clear(eventCounts, 0, eventCounts.Length);
            Array.Clear(captureSamples, 0, captureSamples.Length);
            captureSampleCount = 0;
            captureSampleWriteIndex = 0;
            captures = 0;
            failedCaptures = 0;
            captureMillisecondsTotal = 0d;
            maximumCaptureMilliseconds = 0f;
            catalogCaptures = 0;
            failedCatalogCaptures = 0;
            catalogCaptureMillisecondsTotal = 0d;
            maximumCatalogCaptureMilliseconds = 0f;
            tabletQueryCacheHits = 0;
            tabletQueryCacheMisses = 0;
            failedTabletQueries = 0;
            tabletQueryMillisecondsTotal = 0d;
            presetCaptures = 0;
            failedPresetCaptures = 0;
            presetCaptureMillisecondsTotal = 0d;
            maximumPresetCaptureMilliseconds = 0f;
        }

        internal void RecordCatalogCapture(float elapsedMilliseconds,
            bool succeeded)
        {
            catalogCaptures++;
            if (!succeeded)
            {
                failedCatalogCaptures++;
            }

            float nonNegativeElapsed = Math.Max(0f, elapsedMilliseconds);
            catalogCaptureMillisecondsTotal += nonNegativeElapsed;
            maximumCatalogCaptureMilliseconds = Math.Max(
                maximumCatalogCaptureMilliseconds, nonNegativeElapsed);
        }

        internal void RecordTabletQuery(bool cacheHit, float elapsedMilliseconds,
            bool succeeded)
        {
            if (cacheHit)
            {
                tabletQueryCacheHits++;
                return;
            }

            tabletQueryCacheMisses++;
            if (!succeeded)
            {
                failedTabletQueries++;
            }
            tabletQueryMillisecondsTotal += Math.Max(0f, elapsedMilliseconds);
        }

        internal void RecordPresetCapture(float elapsedMilliseconds,
            bool succeeded)
        {
            presetCaptures++;
            if (!succeeded)
            {
                failedPresetCaptures++;
            }

            float nonNegativeElapsed = Math.Max(0f, elapsedMilliseconds);
            presetCaptureMillisecondsTotal += nonNegativeElapsed;
            maximumPresetCaptureMilliseconds = Math.Max(
                maximumPresetCaptureMilliseconds, nonNegativeElapsed);
        }

        private static float Percentile(float[] sortedSamples, float percentile)
        {
            if (sortedSamples.Length == 0)
            {
                return 0f;
            }

            int index = (int)Math.Ceiling(percentile * sortedSamples.Length) - 1;
            return sortedSamples[Math.Max(0, Math.Min(index,
                sortedSamples.Length - 1))];
        }
    }
}
