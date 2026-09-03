using SephiriaEnhancements.Runtime.Inventory;
using SephiriaEnhancements.Inventory;

namespace SephiriaEnhancements.ModelChecks.Features.Inventory;

internal sealed record InventoryKnownSolution(string Id, int Seed, InventorySnapshot Snapshot,
    InventoryOptimizationPreferences Preferences, InventoryLayoutProjection Witness);

internal static class InventoryKnownSolutionFixture
{
    // Controlled model fixtures, not real items or observed saves. Initial settlement
    // and the planted layout are computed here without calling the production projector.
    internal static InventoryKnownSolution Create(int storage, int seed)
    {
        int rows = storage / 6;
        int artifacts = 6 + (rows - 1) * 4;
        int tablets = (rows - 1) * 2;
        int[] witnessCells = Enumerable.Range(0, 6).Concat(Enumerable.Range(1, rows - 1)
            .SelectMany(row => Enumerable.Range(0, 4).Select(x => row * 6 + x)))
            .Concat(Enumerable.Range(1, rows - 1).SelectMany(row => new[] { row * 6 + 4, row * 6 + 5 })).ToArray();
        int[] witnessRotations = Enumerable.Range(0, artifacts + tablets).Select(i => i < artifacts ? 0 : 2).ToArray();
        int[] positions = Enumerable.Range(0, storage).ToArray();
        var random = new Random(seed);
        random.Shuffle(positions);
        positions = positions.Take(artifacts + tablets).ToArray();
        int[] rotations = Enumerable.Range(0, positions.Length).Select(i => i < artifacts ? 0 : random.Next(4)).ToArray();
        int[] goals = { 0, 1, 6, 7, artifacts - 2, artifacts - 3 };
        ArtifactActivationConditionKind[] conditions = { ArtifactActivationConditionKind.TopRow,
            ArtifactActivationConditionKind.BothSidesArtifacts, ArtifactActivationConditionKind.SideEdge,
            ArtifactActivationConditionKind.Interior, ArtifactActivationConditionKind.BottomRow,
            ArtifactActivationConditionKind.BothSidesArtifacts };
        var artifactCells = positions.Take(artifacts).ToHashSet();
        var stoneTablets = Enumerable.Range(0, tablets).Select(t => Tablet(storage, t, positions[artifacts + t],
            rotations[artifacts + t], artifactCells)).ToArray();
        int[] levels = new int[storage], multipliers = new int[storage], disables = new int[storage], bypasses = new int[storage];
        for (int t = 0; t < tablets; t++)
        {
            if (!stoneTablets[t].Applied) continue;
            foreach (var effect in stoneTablets[t].FindProjection(positions[artifacts + t], rotations[artifacts + t]).Effects.Where(e => e.ValidCell))
            {
                int cell = effect.Y * 6 + effect.X;
                switch (effect.EffectKind)
                {
                    case TabletEffectKind.IncreaseLevel: levels[cell] += effect.LevelParameter; break;
                    case TabletEffectKind.MultiplyLevel: multipliers[cell] += effect.LevelParameter; break;
                    case TabletEffectKind.Disable: disables[cell]++; break;
                    case TabletEffectKind.IgnoreCriteria: bypasses[cell]++; break;
                }
            }
        }
        // The fixed engraving belongs to the board, not to the shuffled items.
        levels[6]++;
        var fixedSource = new FixedTabletSourceSnapshot(90001, 90001, 6, 0, true,
            new TabletRotationProjectionSnapshot(0, Array.Empty<TabletAdditionSnapshot>(),
                new[] { Addition(0, 1, storage, TabletEffectKind.IncreaseLevel, 1) }, true));
        int[] displayed = Enumerable.Range(0, storage).Select(cell =>
        {
            int index = Array.IndexOf(positions, cell);
            int additive = (cell < 6 ? 6 : 2) + levels[cell] + (index >= 0 && index < artifacts && index % 4 == 0 ? 1 : 0);
            return multipliers[cell] == 0 ? additive : additive * multipliers[cell];
        }).ToArray();
        int[] staticItems = Enumerable.Range(0, 20).Except(new[] { 0, 6 }).ToArray();
        var items = Enumerable.Range(0, artifacts).Select(i =>
        {
            int cell = positions[i];
            int goalIndex = Array.IndexOf(goals, i);
            var condition = goalIndex < 0 ? ArtifactActivationConditionKind.None : conditions[goalIndex];
            bool satisfied = Satisfied(condition, cell, storage, artifactCells);
            bool enabled = disables[cell] == 0 && (bypasses[cell] > 0 || satisfied);
            bool dynamic = i == 0 || i == 6;
            string category = dynamic ? (cell / 6 % 2 == 0 ? "FIRE" : "ICE") :
                i >= 20 ? "UTILITY" : Array.IndexOf(staticItems, i) < 9 ? "FIRE" : "ICE";
            var artifact = new ArtifactSnapshot(displayed[cell], 6, i % 4 == 0 ? 1 : 0, 0,
                enabled ? Math.Min(displayed[cell], 6) : 0, enabled, !enabled, false, "", true, false, false, "Pre",
                new CriteriaSnapshot(condition, condition == ArtifactActivationConditionKind.None ? CriteriaEvaluationState.NotApplicable :
                    satisfied ? CriteriaEvaluationState.Satisfied : CriteriaEvaluationState.Unsatisfied, CriteriaEvaluationState.NotApplicable),
                new[] { category }, dynamic ? new[] { "FIRE", "ICE" } : new[] { category }, true, null,
                dynamic ? new ArtifactCategoryRuleSnapshot(ArtifactCategoryRuleKind.RowModulo, new[] { "FIRE", "ICE" }) : null);
            return new InventoryItemSnapshot(i, 10000 + i, 1, cell, cell % 6, cell / 6, "Synthetic artifact", "", "Charm",
                "Normal", new[] { category }, condition == ArtifactActivationConditionKind.None ? InventoryItemKind.Artifact :
                    InventoryItemKind.RestrictedArtifact, artifact, null);
        }).Concat(Enumerable.Range(0, tablets).Select(t => new InventoryItemSnapshot(artifacts + t, 20000 + t, 1,
            positions[artifacts + t], positions[artifacts + t] % 6, positions[artifacts + t] / 6, "Synthetic tablet", "",
            "StoneTablet", "Normal", Array.Empty<string>(), InventoryItemKind.StoneTablet, null, stoneTablets[t]))).ToArray();
        var cells = Enumerable.Range(0, storage).Select(cell =>
        {
            int index = Array.IndexOf(positions, cell);
            bool artifact = index >= 0 && index < artifacts;
            return new InventoryCellSnapshot(cell, cell % 6, cell / 6, displayed[cell], artifact ? 6 : -1, 0,
                multipliers[cell], disables[cell], bypasses[cell], false,
                new InventoryCellSettlementSnapshot(true, cell < 6 ? 6 : 2, -1, 0, 0, 0, 0,
                    artifact && index % 4 == 0 ? 1 : 0, 0, 0, 0, 0, levels[cell], disables[cell], bypasses[cell], multipliers[cell]));
        }).ToArray();
        var combos = new[] { "FIRE", "ICE", "UTILITY" }.Select(category =>
        {
            int count = items.Count(item => item.Artifact?.EffectiveCategories.Contains(category) == true);
            return new ComboCategorySnapshot(category, count, count, count, 0, 0, new[] { 5, 10 }, new[] { 10 }, false);
        }).ToArray();
        string[] channels = { "FireDamage", "IceDamage", "LightningDamage" };
        var rule = new InventoryPositionEffectRule(items[0].ItemKey, InventoryPositionEffectKind.FirstSlotsElementDamage,
            Enumerable.Range(0, 7).Select(value => (double)value).ToArray(), boundary: 6, channels: channels);
        int firstSlotsCount = positions.Take(artifacts).Count(cell => cell < 6);
        double effectValue = items[0].Artifact.LimitedEffectEnabledLevel * firstSlotsCount;
        var observed = channels.Select(channel => new InventoryPositionEffectValue(new InventoryPositionEffectKey(items[0].ItemKey,
            rule.Kind, null, channel), effectValue, false)).ToArray();
        var snapshot = new InventorySnapshot(6, storage, cells, items, comboCategories: combos,
            fixedTabletSources: new[] { fixedSource }, positionEffects: new InventoryPositionEffectsSnapshot(new[] { rule },
                items.Take(artifacts).Select(item => new InventoryPositionTargetTraits(item.ItemKey, false, false, true, 1)).ToArray(), observed, null));
        var preferences = new InventoryOptimizationPreferences(InventorySearchEffort.Balanced, true,
            goals.Select((i, rank) => new ArtifactOptimizationPreference(i, 10000 + i, InventoryPreferenceLevel.Priority, 6, rank)).ToArray(),
            new[] { new ComboOptimizationPreference("FIRE", InventoryPreferenceLevel.Priority, 10),
                new ComboOptimizationPreference("ICE", InventoryPreferenceLevel.Priority, 10) });
        return new InventoryKnownSolution("Planted-" + storage, seed, snapshot, preferences,
            new InventoryLayoutProjection(witnessCells, witnessRotations));
    }

    private static bool Satisfied(ArtifactActivationConditionKind condition, int cell, int storage, HashSet<int> artifacts) => condition switch
    {
        ArtifactActivationConditionKind.None => true,
        ArtifactActivationConditionKind.TopRow => cell < 6,
        ArtifactActivationConditionKind.BottomRow => cell >= storage - 6,
        ArtifactActivationConditionKind.SideEdge => cell % 6 == 0 || cell % 6 == 5,
        ArtifactActivationConditionKind.Interior => cell % 6 > 0 && cell % 6 < 5 && cell >= 6 && cell + 7 < storage,
        ArtifactActivationConditionKind.BothSidesArtifacts => cell % 6 > 0 && cell % 6 < 5 &&
            cell + 1 < storage && artifacts.Contains(cell - 1) && artifacts.Contains(cell + 1),
        _ => throw new InvalidOperationException("Unsupported fixture condition")
    };

    private static TabletAdditionSnapshot Addition(int x, int y, int storage, TabletEffectKind kind, int value) =>
        new(x, y, "synthetic", x >= 0 && x < 6 && y >= 0 && y * 6 + x < storage,
            false, false, false, false, false, false, effectKind: kind, levelParameter: value);

    private static StoneTabletSnapshot Tablet(int storage, int index, int origin, int rotation, HashSet<int> artifacts)
    {
        bool primary = index % 2 == 0;
        var placements = Enumerable.Range(0, storage).Select(cell => new TabletPlacementProjectionSnapshot(cell, cell % 6, cell / 6,
            Enumerable.Range(0, 4).Select(r =>
            {
                var direction = new[] { (1, 0), (0, 1), (-1, 0), (0, -1) }[r];
                TabletEffectKind kind = primary ? TabletEffectKind.IncreaseLevel : (index / 2 % 4) switch
                { 0 => TabletEffectKind.MultiplyLevel, 1 => TabletEffectKind.Disable, 2 => TabletEffectKind.IgnoreCriteria, _ => TabletEffectKind.IncreaseLevel };
                var effects = Enumerable.Range(1, primary ? 4 : 1).Select(distance => Addition(cell % 6 + direction.Item1 * distance,
                    cell / 6 + direction.Item2 * distance, storage, kind, primary ? 4 : 2)).ToArray();
                var criteria = primary ? new[] { new TabletAdditionSnapshot(effects[0].X, effects[0].Y, "artifact", effects[0].ValidCell,
                    false, false, false, false, false, false, criteriaKind: TabletCriteriaKind.Artifact) } : Array.Empty<TabletAdditionSnapshot>();
                return new TabletRotationProjectionSnapshot(r, criteria, effects, true);
            }).ToArray())).ToArray();
        var current = placements[origin].FindRotation(rotation);
        bool applied = current.Criteria.All(criterion => criterion.ValidCell && artifacts.Contains(criterion.Y * 6 + criterion.X));
        return new StoneTabletSnapshot(rotation, true, false, applied, false, "synthetic", "synthetic",
            placements[origin].Rotations.ToArray(), placements);
    }
}
