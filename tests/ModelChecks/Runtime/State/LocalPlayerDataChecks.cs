using SephiriaEnhancements.AutoCasting;
using SephiriaEnhancements.Core;
using SephiriaEnhancements.Inventory;
using SephiriaEnhancements.Runtime;
using SephiriaEnhancements.Runtime.Inventory;

namespace SephiriaEnhancements.ModelChecks.Runtime.State;

internal static class LocalPlayerDataChecks
{
    internal static void Run()
    {
        var store = new LocalPlayerDataStore();
        using var preferences = store.Preferences(() => new AutoCastingPreferences());
        using var progress = store.Progress(() => new FloorCombatStatistics(), value => value.Copy());
        progress.Value.ObserveFloor("floor");
        progress.Value.RecordDamage(1, "Local", true, 600, EncounterDamageType.Fire);
        progress.Value.RecordDamage(2, "Guest", false, 400, EncounterDamageType.Ice);
        progress.Value.RecordDefeat(100, EncounterEnemyTier.Normal);
        progress.Value.RecordLocalFinalBlow();
        progress.Value.UpdateClock(10, true);
        store.Saving += () => progress.Value.UpdateClock(20, false);
        var checkpoint = store.Capture();
        preferences.Value.Selection.Toggle(17);
        preferences.Value.IsPaused = true;
        for (int attempt = 0; attempt < 3; attempt++)
        {
            progress.Value.RecordDamage(1, "Local", true, 3000, EncounterDamageType.Physical);
            progress.Value.RecordDefeat(200, EncounterEnemyTier.Boss);
            progress.Value.RecordLocalFinalBlow();
            progress.Value.UpdateClock(30, true);
            progress.Value.UpdateClock(40, false);
            store.Load(checkpoint);
            var restored = progress.Value.Capture();
            Require(restored.TotalDamage == 1000 && restored.Duration == 10 &&
                restored.NormalDefeated == 1 && restored.BossDefeated == 0 && restored.LocalFinalBlows == 1 &&
                restored.Players.Count == 2 && restored.DamageTypes.Sum(type => type.Damage) == 1000,
                "Loading rolls back all statistics without mutating the reusable checkpoint");
            progress.Value.RecordDefeat(100, EncounterEnemyTier.Normal);
            Require(progress.Value.Capture().NormalDefeated == 1, "Restored identities still deduplicate");
            progress.Value.UpdateClock(1000, false);
            Require(progress.Value.Capture().Duration == 10, "Loading time is not combat time");
            Require(preferences.Value.IsPaused && preferences.Value.Selection.Contains(17),
                "Preferences edited after capture survive repeated loads");
        }
        store.Load(null);
        Require(progress.Value.Capture().TotalDamage == 0 && preferences.Value.IsPaused,
            "Floor restart loads fresh progress while keeping preferences");
        progress.Reset();
        store.Load(checkpoint);
        Require(progress.Value.Capture().TotalDamage == 0, "Disabling a feature invalidates its earlier data");
        using var laterFeature = store.Progress(() => new List<int>(), value => new List<int>(value));
        laterFeature.Value.Add(42);
        store.Load(checkpoint);
        Require(laterFeature.Value.Count == 0, "Features created after a save load their defaults");
        laterFeature.Value.Add(7);
        var extended = store.Capture();
        laterFeature.Value.Add(8);
        store.Load(extended);
        Require(laterFeature.Value.SequenceEqual(new[] { 7 }), "New data participates without changing the loader");
        store.Reset();
        Require(!preferences.Value.IsPaused && !preferences.Value.Selection.Contains(17),
            "A different player or ordinary world reload starts new local data");
        VerifyInventory();
        Console.WriteLine("Local player data: repeated load, preference retention, progress rollback, ownership reset and inventory rules passed.");
    }

    private static void VerifyInventory()
    {
        // Use the production preference owner, including its persistent-category composition.
        var persistent = new InventoryOptimizationPreferences(InventorySearchEffort.Thorough, true,
            Array.Empty<ArtifactOptimizationPreference>(), Array.Empty<ComboOptimizationPreference>(), false, false);
        PersistentInventoryOptimizationPolicyStore.Replace(persistent);
        LocalPlayerDataStore.Shared.Reset();
        var preferences = new InventoryOptimizationPreferences(InventorySearchEffort.Thorough, true,
            new[] { new ArtifactOptimizationPreference(17, 100, InventoryPreferenceLevel.Priority, 5, 0,
                ArtifactLevelTargetMode.SpecifiedLevel, InventoryConstraintStrength.Hard),
                new ArtifactOptimizationPreference(17, 200, InventoryPreferenceLevel.Avoid) },
            Array.Empty<ComboOptimizationPreference>(), false, false);
        LocalPlayerInventoryIntentStore.Replace(preferences);
        var saved = LocalPlayerDataStore.Shared.Capture();
        LocalPlayerDataStore.Shared.Load(saved);
        Require(ReferenceEquals(LocalPlayerInventoryIntentStore.Capture(), preferences),
            "Inventory preferences are retained, not replaced by checkpoint-time values");
        var pruned = InventoryArtifactIntentEditor.Prune(LocalPlayerInventoryIntentStore.Capture(),
            new[] { new InventoryItemKey(100, 17) });
        Require(pruned.ArtifactPreferences.Count == 1 && pruned.ArtifactPreferences[0].MinimumEffectiveLevel == 5 &&
            pruned.ArtifactPreferences[0].Strength == InventoryConstraintStrength.Hard,
            "Rebuilt matching item keeps its complete rule; same instance ID with another entity does not match");
        LocalPlayerDataStore.Shared.Reset();
        Require(LocalPlayerInventoryIntentStore.Capture().ArtifactPreferences.Count == 0 &&
            !LocalPlayerInventoryIntentStore.Capture().AllowAdditionalMagicCost,
            "New local data clears item rules but retains persistent policy");
        PersistentInventoryOptimizationPolicyStore.Replace(InventoryOptimizationPreferences.Default);
        LocalPlayerDataStore.Shared.Reset();
    }

    private static void Require(bool value, string message)
    {
        if (!value) throw new InvalidOperationException(message);
    }
}
