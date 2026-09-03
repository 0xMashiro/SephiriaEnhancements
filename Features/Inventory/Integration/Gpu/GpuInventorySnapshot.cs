#nullable disable
using System;
using System.Collections.Generic;
using System.Linq;
using SephiriaEnhancements.Runtime.Inventory;

namespace SephiriaEnhancements.Inventory.Integration.Gpu;

internal sealed class GpuInventorySnapshot
{
    internal readonly InventorySnapshot Snapshot;
    internal readonly string[] Categories;
    internal readonly int[] Data;
    internal int ResultStride => 1 + Snapshot.Items.Count * 3 + Categories.Length * 2 + 7;

    internal GpuInventorySnapshot(InventorySnapshot snapshot)
    {
        Snapshot = snapshot;
        if (!snapshot.SettlementValidation.LayoutProjectionReady || snapshot.Storage > 64 || snapshot.Items.Count > 64)
            throw new NotSupportedException("GPU snapshot scope");
        if (snapshot.PositionEffects.Rules.Count != 0 || snapshot.PositionEffects.Observed.Count != 0)
            throw new NotSupportedException("GPU position effects");
        Categories = snapshot.ComboCategories.Select(c => c.CategoryId)
            .Concat(snapshot.Items.SelectMany(i => i.BaseCategories))
            .Concat(snapshot.Items.Where(i => i.Artifact != null).SelectMany(i => i.Artifact.CategoryRule.RowCategories))
            .Distinct(StringComparer.Ordinal).OrderBy(c => c, StringComparer.Ordinal).ToArray();
        if (Categories.Length > 64 || snapshot.Items.Any(i => i.BaseCategories.Distinct().Count() != i.BaseCategories.Count))
            throw new NotSupportedException("GPU category representation");
        var ids = Categories.Select((name, id) => (name, id)).ToDictionary(p => p.name, p => p.id, StringComparer.Ordinal);
        var data = new List<int>(new int[16]);
        int Reserve(int count) { int offset = data.Count; data.AddRange(new int[count]); return offset; }
        int Append(IEnumerable<int> values) { int offset = data.Count; data.AddRange(values); return offset; }
        data[0] = snapshot.Width; data[1] = snapshot.Storage; data[2] = snapshot.Items.Count; data[3] = Categories.Length;
        data[4] = Reserve(snapshot.Storage * 4);
        data[5] = Reserve(snapshot.Items.Count * 20);
        data[6] = Reserve(snapshot.Items.Count * snapshot.Storage * 4);
        data[7] = Reserve(snapshot.FixedTabletSources.Count * 2);
        data[8] = snapshot.FixedTabletSources.Count;
        data[9] = Reserve(Categories.Length * 2);
        data[10] = snapshot.ArtifactEffectsEnabled ? 1 : 0;
        data[11] = snapshot.GlobalActiveValue;
        data[12] = snapshot.SuppressDuplicateComboEntities ? 1 : 0;
        data[13] = ResultStride;
        data[14] = Append(snapshot.Items.SelectMany(i => new[] { i.CellIndex, i.StoneTablet?.Rotation ?? 0,
            i.Artifact?.EffectEnabled == true ? 1 : 0 }));
        data[15] = Reserve(Categories.Length * 2);
        foreach (var category in snapshot.ComboCategories)
        {
            int[] thresholds = category.SetThresholds.Union(category.ComboThresholds).Where(t => t > 0).ToArray();
            int p = data[15] + ids[category.CategoryId] * 2;
            data[p] = Append(thresholds); data[p + 1] = thresholds.Length;
        }
        for (int cell = 0; cell < snapshot.Storage; cell++)
        {
            var s = snapshot.Cells[cell].Settlement;
            int p = data[4] + cell * 4;
            data[p] = s.BaselineLevel + s.FixedLevel;
            data[p + 1] = s.BaselineLevelMultiplier + s.FixedLevelMultiplier;
            data[p + 2] = s.BaselineDisableCount + s.FixedDisableCount;
            data[p + 3] = s.BaselineCriteriaBypassCount + s.FixedCriteriaBypassCount;
        }
        foreach (var category in snapshot.ComboCategories)
        {
            int p = data[9] + ids[category.CategoryId] * 2;
            data[p] = category.BonusCount + category.InferredUniquePairCount;
            data[p + 1] = 1;
        }
        int Projection(TabletRotationProjectionSnapshot projection)
        {
            if (projection == null || !projection.ParseSucceeded) return -1;
            int p = Reserve(4);
            data[p] = projection.Criteria.Count;
            data[p + 1] = projection.Effects.Count;
            data[p + 2] = Append(projection.Criteria.SelectMany(c => new[] { (int)c.CriteriaKind, c.Y * snapshot.Width + c.X, c.ValidCell ? 1 : 0 }));
            data[p + 3] = Append(projection.Effects.SelectMany(e => new[] { (int)e.EffectKind, e.Y * snapshot.Width + e.X, e.LevelParameter, e.ValidCell ? 1 : 0 }));
            return p;
        }
        for (int item = 0; item < snapshot.Items.Count; item++)
        {
            var i = snapshot.Items[item];
            var a = i.Artifact;
            int p = data[5] + item * 20;
            data[p] = a != null ? 1 : 0;
            data[p + 1] = a?.MaxLevel ?? 0;
            data[p + 2] = a?.Enchant ?? 0;
            data[p + 3] = a?.WeaponCompatible == true ? 1 : 0;
            data[p + 4] = (int)(a?.Criteria?.Kind ?? ArtifactActivationConditionKind.None);
            data[p + 5] = (int)(a?.Criteria?.RuntimeState ?? CriteriaEvaluationState.NotApplicable);
            data[p + 6] = a?.Magic != null ? 1 : 0;
            data[p + 7] = Append(i.BaseCategories.Select(c => ids[c]));
            data[p + 8] = i.BaseCategories.Count;
            data[p + 9] = (int)(a?.CategoryRule.Kind ?? ArtifactCategoryRuleKind.Static);
            data[p + 10] = Append(a?.CategoryRule.RowCategories.Select(c => ids[c]) ?? Enumerable.Empty<int>());
            data[p + 11] = a?.CategoryRule.RowCategories.Count ?? 0;
            data[p + 12] = a?.CategoryRule.TargetX ?? 0;
            data[p + 13] = a?.CategoryRule.TargetY ?? 0;
            data[p + 14] = a?.Attackable == true ? 1 : 0;
            data[p + 15] = i.EntityId;
            data[p + 16] = Append(a?.CategoryRule.NeighborOffsets.SelectMany(o => new[] { o.X, o.Y }) ?? Enumerable.Empty<int>());
            data[p + 17] = a?.CategoryRule.NeighborOffsets.Count ?? 0;
            data[p + 18] = a?.CategoryRule.Match ?? 0;
            data[p + 19] = i.StoneTablet != null ? 1 : 0;
            for (int cell = 0; cell < snapshot.Storage; cell++)
                for (int rotation = 0; rotation < 4; rotation++)
                    data[data[6] + (item * snapshot.Storage + cell) * 4 + rotation] =
                        i.StoneTablet == null ? -1 : Projection(i.StoneTablet.FindProjection(cell, rotation));
        }
        for (int index = 0; index < snapshot.FixedTabletSources.Count; index++)
        {
            var source = snapshot.FixedTabletSources[index];
            data[data[7] + index * 2] = source.CellIndex;
            data[data[7] + index * 2 + 1] = Projection(source.Projection);
        }
        Data = data.ToArray();
    }

}
