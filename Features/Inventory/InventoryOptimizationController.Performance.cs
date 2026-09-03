#if SEPHIRIA_ENHANCEMENTS_DEVTOOLS
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using SephiriaEnhancements.Diagnostics;
using UnityEngine;

namespace SephiriaEnhancements.Inventory;

internal sealed partial class InventoryOptimizationController
{
    private readonly Queue<float> idleFrameTimes = new();
    private readonly List<float> operationFrameTimes = new();
    private float[] precedingFrameTimes;

    private void ResetOptimizationFrameMetrics()
    {
        idleFrameTimes.Clear();
        operationFrameTimes.Clear();
        precedingFrameTimes = null;
    }

    private void SampleOptimizationFrame()
    {
        float milliseconds = Time.unscaledDeltaTime * 1000f;
        if (Busy)
        {
            precedingFrameTimes ??= idleFrameTimes.ToArray();
            operationFrameTimes.Add(milliseconds);
            return;
        }
        if (precedingFrameTimes != null)
        {
            // This final frame includes the last poll, inventory application, or cancellation.
            operationFrameTimes.Add(milliseconds);
            SupportLogger.Record("inventory_operation_frames", Describe("before", precedingFrameTimes) + " " +
                Describe("during", operationFrameTimes));
            precedingFrameTimes = null;
            operationFrameTimes.Clear();
            idleFrameTimes.Clear();
        }
        idleFrameTimes.Enqueue(milliseconds);
        if (idleFrameTimes.Count > 120) idleFrameTimes.Dequeue();
    }

    private static string Describe(string prefix, IEnumerable<float> samples)
    {
        float[] ordered = samples.OrderBy(value => value).ToArray();
        if (ordered.Length == 0) return prefix + "Count=0";
        string Number(float value) => value.ToString("F2", CultureInfo.InvariantCulture);
        return prefix + "Count=" + ordered.Length + " " + prefix + "MeanMs=" + Number(ordered.Average()) +
            " " + prefix + "P95Ms=" + Number(ordered[(int)Math.Ceiling(ordered.Length * 0.95) - 1]) +
            " " + prefix + "MaxMs=" + Number(ordered[ordered.Length - 1]);
    }
}
#endif
