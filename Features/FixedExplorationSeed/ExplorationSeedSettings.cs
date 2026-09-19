namespace SephiriaEnhancements.FixedExplorationSeed
{
    internal static class ExplorationSeedSettings
    {
        internal const string SeedKey = "SephiriaEnhancements.FixedExplorationSeed.Seed";
        internal static string Text => OptionsBinding.Instance?.DeviceOptions?.GetString(SeedKey, "") ?? "";

        internal static void Save(string text)
        {
            if (!ExplorationSeedInput.TryParse(text, out var seed)) return;
            var options = OptionsBinding.Instance?.DeviceOptions;
            options?.SetString(SeedKey, ExplorationSeedInput.Format(seed));
            options?.Save();
        }
    }
}
