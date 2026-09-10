#nullable disable
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using SephiriaEnhancements.Runtime.Inventory;

namespace SephiriaEnhancements.Inventory
{
    internal static class InventoryGroupRelocation
    {
        internal static IEnumerable<InventoryLayoutProjection> Enumerate(InventorySnapshot snapshot,
            InventoryLayoutProjection layout, ProjectedInventorySettlement settlement,
            CancellationToken cancellationToken)
        {
            var indexes = snapshot.Items.Select((item, index) => (item.ItemKey, index))
                .ToDictionary(pair => pair.ItemKey, pair => pair.index);
            var groups = new List<HashSet<int>>();
            // Dependency effects report every contributing source against the
            // ultimate recipient, including sources reached through a chain.
            foreach (var target in settlement.PositionEffects.Where(effect => effect.Value > 0 &&
                         effect.Key.Target.HasValue && effect.Key.Kind == InventoryPositionEffectKind.DependencyDamage)
                         .GroupBy(effect => effect.Key.Target.Value))
                groups.Add(target.Select(effect => indexes[effect.Key.Source]).Append(indexes[target.Key]).ToHashSet());
            foreach (var source in settlement.PositionEffects.Where(effect => effect.Value > 0 &&
                         effect.Key.Target.HasValue && effect.Key.Kind != InventoryPositionEffectKind.DependencyDamage)
                         .GroupBy(effect => effect.Key.Source))
                groups.Add(source.Select(effect => indexes[effect.Key.Target.Value]).Append(indexes[source.Key]).ToHashSet());

            var occupants = Enumerable.Repeat(-1, snapshot.Storage).ToArray();
            for (int item = 0; item < layout.ItemCount; item++) occupants[layout.GetCell(item)] = item;
            foreach (var tablet in settlement.Tablets.Where(tablet => !tablet.FixedSource && tablet.Applied))
            {
                cancellationToken.ThrowIfCancellationRequested();
                int source = indexes[tablet.ItemKey];
                var projection = snapshot.Items[source].StoneTablet.FindProjection(tablet.CellIndex, tablet.Rotation);
                var group = new HashSet<int> { source };
                foreach (var effect in projection.Effects.Where(effect => effect.ValidCell &&
                             effect.EffectKind == TabletEffectKind.IncreaseLevel && effect.LevelParameter > 0))
                {
                    int target = occupants[effect.Y * snapshot.Width + effect.X];
                    if (target >= 0 && snapshot.Items[target].Artifact != null) group.Add(target);
                }
                groups.Add(group);
            }

            var visited = new List<HashSet<int>>();
            foreach (var group in groups)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (group.Count < 2 || visited.Any(previous => previous.SetEquals(group))) continue;
                visited.Add(group);
                int origin = layout.GetCell(group.Min());
                for (int destination = 0; destination < snapshot.Storage; destination++)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var candidate = Translate(snapshot, layout, group,
                        destination % snapshot.Width - origin % snapshot.Width,
                        destination / snapshot.Width - origin / snapshot.Width);
                    if (candidate != null) yield return candidate;
                }
            }
        }

        // Move the group without changing relative positions or rotations.
        // Outside occupants fill vacated cells, including overlapping regions.
        internal static InventoryLayoutProjection Translate(InventorySnapshot snapshot,
            InventoryLayoutProjection layout, HashSet<int> members, int dx, int dy)
        {
            if (dx == 0 && dy == 0) return null;
            int[] cells = layout.CopyCells();
            var oldCells = members.Select(item => cells[item]).ToHashSet();
            var destinations = new HashSet<int>();
            foreach (int item in members)
            {
                int x = cells[item] % snapshot.Width + dx, y = cells[item] / snapshot.Width + dy;
                if (x < 0 || x >= snapshot.Width || y < 0 || y * snapshot.Width + x >= snapshot.Storage) return null;
                destinations.Add(y * snapshot.Width + x);
            }
            int[] vacancies = oldCells.Except(destinations).OrderBy(cell => cell).ToArray();
            int vacancy = 0;
            for (int item = 0; item < cells.Length; item++)
                if (!members.Contains(item) && destinations.Contains(cells[item])) cells[item] = vacancies[vacancy++];
            foreach (int item in members) cells[item] = layout.GetCell(item) + dy * snapshot.Width + dx;
            return new InventoryLayoutProjection(cells, layout.CopyRotations());
        }
    }
}
