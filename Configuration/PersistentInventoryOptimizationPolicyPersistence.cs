#nullable disable

using SephiriaEnhancements.Inventory;

namespace SephiriaEnhancements.Configuration
{
    internal static class PersistentInventoryOptimizationPolicyPersistence
    {
        private static bool loaded;

        internal static bool EnsureLoaded()
        {
            // Native integration boundary: DeviceOptions is the game's
            // per-device, cross-launch settings store. Core policy models must
            // not depend on SaveData or OptionsBinding.
            SaveData deviceOptions = OptionsBinding.Instance?.DeviceOptions;
            if (loaded || deviceOptions == null)
            {
                return loaded;
            }

            string payload = deviceOptions.GetString(
                ModSettings.InventoryTargetPreferencesKey, string.Empty);
            InventoryOptimizationPreferences policy;
            if (!InventoryOptimizationPreferencesCodec.TryDecode(payload,
                    InventoryOptimizationPreferences.Default.SearchEffort,
                    InventoryOptimizationPreferences.Default.
                        AllowStoneTabletRotation, out policy))
            {
                policy = InventoryOptimizationPreferences.Default;
            }
            PersistentInventoryOptimizationPolicyStore.Replace(policy);
            ExplorationInventoryIntentStore.RestorePersistentCombos();
            loaded = true;
            return true;
        }

        internal static bool Save(InventoryOptimizationPreferences policy)
        {
            SaveData deviceOptions = OptionsBinding.Instance?.DeviceOptions;
            if (deviceOptions == null)
            {
                return false;
            }
            deviceOptions.SetString(ModSettings.InventoryTargetPreferencesKey,
                InventoryOptimizationPreferencesCodec.Encode(policy));
            deviceOptions.Save();
            return true;
        }
    }
}
