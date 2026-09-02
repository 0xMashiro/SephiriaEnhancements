#if SEPHIRIA_ENHANCEMENTS_DEVTOOLS
using SephiriaEnhancements.DeveloperTools.Core;

namespace SephiriaEnhancements.DeveloperTools
{
    internal static class DeveloperPlayerDamageSettings
    {
        internal const string MultiplierIndexKey =
            "SephiriaEnhancements.DeveloperTools.PlayerDamageMultiplierIndex";

        internal static int MultiplierIndex
        {
            get => DeveloperPlayerDamagePolicy.NormalizeIndex(
                OptionsBinding.Instance?.DeviceOptions?.GetInt(
                    MultiplierIndexKey, 0) ?? 0);
            set => OptionsBinding.Instance?.DeviceOptions?.SetInt(
                MultiplierIndexKey,
                DeveloperPlayerDamagePolicy.NormalizeIndex(value));
        }

        internal static int MultiplierCount =>
            DeveloperPlayerDamagePolicy.MultiplierCount;

        internal static void Save()
        {
            OptionsBinding.Instance?.DeviceOptions?.Save();
        }
    }
}
#endif
