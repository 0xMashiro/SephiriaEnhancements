#nullable enable
using System.Collections.Generic;

namespace SephiriaEnhancements.CombatTargeting
{
    // Candidate ordering is supplied by the game boundary; target identity stays
    // stable as candidates move. No engine objects or input devices are required.
    internal sealed class TargetSelection<T> where T : class
    {
        private readonly List<T> cycle = new List<T>();
        internal T? Target { get; private set; }
        internal bool IsManual { get; private set; }

        internal void Refresh(IReadOnlyList<T> nearestFirst, bool allowAutomatic)
        {
            for (int index = cycle.Count - 1; index >= 0; index--)
            {
                if (!Contains(nearestFirst, cycle[index])) cycle.RemoveAt(index);
            }
            for (int index = 0; index < nearestFirst.Count; index++)
            {
                if (!cycle.Contains(nearestFirst[index])) cycle.Add(nearestFirst[index]);
            }

            if (!Contains(nearestFirst, Target))
            {
                Target = null;
                IsManual = false;
            }
            if (IsManual) return;
            if (!allowAutomatic) Target = null;
            else if (Target == null && nearestFirst.Count > 0) Target = nearestFirst[0];
        }

        internal void Switch(T? initialTarget)
        {
            if (cycle.Count == 0) return;
            T? start = Target ?? initialTarget;
            int index = start == null ? -1 : cycle.IndexOf(start);
            Target = cycle[(index + 1) % cycle.Count];
            IsManual = true;
        }

        internal void Unlock()
        {
            Target = null;
            IsManual = false;
        }

        internal void Clear()
        {
            Unlock();
            cycle.Clear();
        }

        private static bool Contains(IReadOnlyList<T> values, T? value)
        {
            if (value == null) return false;
            for (int index = 0; index < values.Count; index++)
            {
                if (EqualityComparer<T>.Default.Equals(values[index], value)) return true;
            }
            return false;
        }
    }
}
