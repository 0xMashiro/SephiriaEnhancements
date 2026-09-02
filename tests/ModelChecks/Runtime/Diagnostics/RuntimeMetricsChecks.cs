using SephiriaEnhancements.Runtime;

namespace SephiriaEnhancements.ModelChecks.Runtime.Diagnostics;

internal static class RuntimeMetricsChecks
{
    internal static void Run()
    {
        var runtimeMetrics = new RuntimeMetrics();
        runtimeMetrics.RecordEvent(RuntimeEventKind.ItemUpdated);
        runtimeMetrics.RecordEvent(RuntimeEventKind.ItemUpdated);
        runtimeMetrics.RecordCapture(0.2f, succeeded: true);
        runtimeMetrics.RecordCapture(1.2f, succeeded: false);
        runtimeMetrics.RecordCatalogCapture(4f, succeeded: true);
        runtimeMetrics.RecordTabletQuery(cacheHit: false, 2f, succeeded: true);
        runtimeMetrics.RecordTabletQuery(cacheHit: true, 0f, succeeded: true);
        runtimeMetrics.RecordTabletQuery(cacheHit: false, 4f, succeeded: false);
        runtimeMetrics.RecordPresetCapture(3f, succeeded: true);
        runtimeMetrics.RecordPresetCapture(5f, succeeded: false);
        RuntimeMetricSnapshot metricSnapshot = runtimeMetrics.TakeSnapshotAndReset();
        if (metricSnapshot.EventCounts[(int)RuntimeEventKind.ItemUpdated] != 2 ||
            metricSnapshot.Captures != 2 || metricSnapshot.FailedCaptures != 1 ||
            Math.Abs(metricSnapshot.AverageCaptureMilliseconds - 0.7f) > 0.001f ||
            Math.Abs(metricSnapshot.P50CaptureMilliseconds - 0.2f) > 0.001f ||
            Math.Abs(metricSnapshot.P95CaptureMilliseconds - 1.2f) > 0.001f ||
            metricSnapshot.CatalogCaptures != 1 ||
            metricSnapshot.FailedCatalogCaptures != 0 ||
            Math.Abs(metricSnapshot.AverageCatalogCaptureMilliseconds - 4f) > 0.001f ||
            metricSnapshot.TabletQueryCacheHits != 1 ||
            metricSnapshot.TabletQueryCacheMisses != 2 ||
            metricSnapshot.FailedTabletQueries != 1 ||
            Math.Abs(metricSnapshot.AverageTabletQueryMilliseconds - 3f) > 0.001f ||
            metricSnapshot.PresetCaptures != 2 ||
            metricSnapshot.FailedPresetCaptures != 1 ||
            Math.Abs(metricSnapshot.AveragePresetCaptureMilliseconds - 4f) > 0.001f ||
            Math.Abs(metricSnapshot.MaximumPresetCaptureMilliseconds - 5f) > 0.001f ||
            runtimeMetrics.TakeSnapshotAndReset().Captures != 0)
            throw new InvalidOperationException("runtime metrics aggregation or reset failed");
        Console.WriteLine("RuntimeMetrics: events, latency percentiles and reset checks passed");
    }
}
