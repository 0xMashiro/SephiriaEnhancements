#nullable disable
using System;
using System.Collections.Generic;

namespace SephiriaEnhancements.Runtime
{
    // Registered in teardown order. Failure and unload consume the same actions once.
    internal sealed class FeatureCleanup
    {
        private readonly List<(FeatureId Feature, Action Unload, Action Failure)> actions = new();
        private readonly Action<FeatureId, Exception> report;

        internal FeatureCleanup(Action<FeatureId, Exception> report) => this.report = report;

        internal void Add(FeatureId feature, Action unload, Action failure = null) =>
            actions.Add((feature, unload, failure ?? unload));

        internal void Stop(FeatureId feature)
        {
            var selected = actions.FindAll(entry => entry.Feature == feature);
            actions.RemoveAll(entry => entry.Feature == feature);
            foreach (var entry in selected) Run(entry.Feature, entry.Failure);
        }

        internal void Unload()
        {
            var selected = actions.ToArray();
            actions.Clear();
            foreach (var entry in selected) Run(entry.Feature, entry.Unload);
        }

        private void Run(FeatureId feature, Action action)
        {
            try { action?.Invoke(); }
            catch (Exception exception) { report(feature, exception); }
        }
    }
}
