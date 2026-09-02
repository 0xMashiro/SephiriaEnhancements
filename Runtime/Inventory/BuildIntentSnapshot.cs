#nullable disable

using System;
using System.Collections.Generic;

namespace SephiriaEnhancements.Runtime.Inventory
{
    internal sealed class BuildIntentSnapshot
    {
        internal BuildIntentSnapshot(int nativePresetSlot,
            bool nativePresetEnabled, int[] preferredArtifactEntityIds,
            string[] preferredCategories)
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
        }

        internal int NativePresetSlot { get; }
        internal bool NativePresetEnabled { get; }
        internal IReadOnlyList<int> PreferredArtifactEntityIds { get; }
        internal IReadOnlyList<string> PreferredCategories { get; }
        internal static BuildIntentSnapshot FromNativePreset(
            NativePresetSnapshot preset)
        {
            return new BuildIntentSnapshot(preset?.SelectedSlot ?? -1,
                preset?.Enabled == true,
                Copy(preset?.FavoriteEntityIds),
                Copy(preset?.FavoriteCategories));
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
}
