#if SEPHIRIA_ENHANCEMENTS_DEVTOOLS
namespace SephiriaEnhancements.DeveloperTools
{
    internal static class InventoryReproductionSettings
    {
        private const string RecordAllResultsKey =
            "SephiriaEnhancements.DeveloperTools.InventoryReproduction.RecordAllResults";

        internal static bool RecordAllResults
        {
            get => OptionsBinding.Instance?.DeviceOptions?.GetInt(RecordAllResultsKey, 0) == 1;
            set
            {
                OptionsBinding.Instance?.DeviceOptions?.SetInt(RecordAllResultsKey, value ? 1 : 0);
                OptionsBinding.Instance?.DeviceOptions?.Save();
            }
        }
    }
}
#endif
