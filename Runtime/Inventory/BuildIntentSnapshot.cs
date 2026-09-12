#nullable disable

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace SephiriaEnhancements.Runtime.Inventory
{
    internal sealed class BuildIntentSnapshot
    {
        internal BuildIntentSnapshot(int nativePresetSlot,
            bool nativePresetEnabled, int[] preferredArtifactEntityIds,
            string[] preferredCategories, FruitSkewerCategoryPreference[] fruitSkewerPreferences = null)
        {
            NativePresetSlot = nativePresetSlot;
            NativePresetEnabled = nativePresetEnabled;
            PreferredArtifactEntityIds = Array.AsReadOnly(
                preferredArtifactEntityIds == null
                    ? Array.Empty<int>()
                    : (int[])preferredArtifactEntityIds.Clone());
            PreferredCategories = Array.AsReadOnly(preferredCategories == null
                ? Array.Empty<string>()
                    : (string[])preferredCategories.Clone());
            var priorities = new Dictionary<string, int>(StringComparer.Ordinal);
            FruitSkewerPreferences = Array.AsReadOnly(fruitSkewerPreferences == null
                ? Array.Empty<FruitSkewerCategoryPreference>() : (FruitSkewerCategoryPreference[])fruitSkewerPreferences.Clone());
            foreach (var fruit in FruitSkewerPreferences)
                priorities[fruit.CategoryId] = priorities.TryGetValue(fruit.CategoryId, out int value)
                    ? value + fruit.Priority : fruit.Priority;
            FruitSkewerCategoryPriorities = new ReadOnlyDictionary<string, int>(priorities);
        }

        internal int NativePresetSlot { get; }
        internal bool NativePresetEnabled { get; }
        internal IReadOnlyList<int> PreferredArtifactEntityIds { get; }
        internal IReadOnlyList<string> PreferredCategories { get; }
        // Configuration expresses intent, not the drop bonus already consumed this exploration.
        internal IReadOnlyDictionary<string, int> FruitSkewerCategoryPriorities { get; }
        internal IReadOnlyList<FruitSkewerCategoryPreference> FruitSkewerPreferences { get; }
        internal static BuildIntentSnapshot FromNativePreset(
            NativePresetSnapshot preset)
        {
            return new BuildIntentSnapshot(preset?.SelectedSlot ?? -1,
                preset?.Enabled == true,
                Copy(preset?.FavoriteEntityIds),
                Copy(preset?.FavoriteCategories), preset?.Fruits.Select(fruit =>
                    new FruitSkewerCategoryPreference(fruit.CategoryId, fruit.Value)).ToArray());
        }

        private static T[] Copy<T>(IReadOnlyList<T> values)
        {
            if (values == null || values.Count == 0)
            {
                return Array.Empty<T>();
            }

            var result = new T[values.Count];
            for (int index = 0; index < result.Length; index++)
            {
                result[index] = values[index];
            }
            return result;
        }
    }

    internal sealed class FruitSkewerCategoryPreference
    {
        internal FruitSkewerCategoryPreference(string categoryId, int priority)
        {
            CategoryId = categoryId;
            Priority = priority;
        }

        internal string CategoryId { get; }
        internal int Priority { get; }
    }
}
