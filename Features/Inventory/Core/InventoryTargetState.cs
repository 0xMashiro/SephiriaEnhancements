using System;

namespace SephiriaEnhancements.Inventory
{
    // Bounded level/activation goals; directional damage is compared separately.
    internal readonly struct InventoryTargetState
    {
        internal const int TargetCompletionScale = 10_000;

        internal InventoryTargetState(int value, bool reached, int completionPoints)
        {
            Value = value;
            Reached = reached;
            CompletionPoints = completionPoints;
        }

        internal int Value { get; }
        internal bool Reached { get; }
        internal int CompletionPoints { get; }

        internal static InventoryTargetState Artifact(InventoryPreferenceLevel preference,
            int minimumLevel, bool active, int effectiveLevel)
        {
            if (preference == InventoryPreferenceLevel.Avoid)
                return new InventoryTargetState(active ? 1 : 0, !active, active ? 0 : TargetCompletionScale);
            int value = active ? effectiveLevel : 0;
            // An active level-zero artifact outranks an inactive one even below its target.
            return new InventoryTargetState(value, active && value >= minimumLevel,
                Math.Max(active ? 1 : 0, CalculateTargetCompletionPoints(active, value, minimumLevel)));
        }

        internal static (bool Reached, int CompletionPoints) Combo(
            ResolvedComboOptimizationRule rule, int count)
        {
            if (rule.Level == InventoryPreferenceLevel.Avoid)
            {
                bool reached = count <= rule.TargetCount;
                return (reached, reached ? TargetCompletionScale : 0);
            }
            return (count >= rule.TargetCount,
                CalculateTargetCompletionPoints(true, count, rule.TargetCount));
        }

        internal static int CalculateTargetCompletionPoints(bool active, int currentValue, int minimumValue)
        {
            if (!active) return 0;
            if (minimumValue <= 0) return TargetCompletionScale;
            return (int)Math.Min(TargetCompletionScale,
                Math.Max(0, currentValue) * (long)TargetCompletionScale / minimumValue);
        }
    }
}
