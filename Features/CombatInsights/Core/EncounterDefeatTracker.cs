using System.Collections.Generic;

namespace SephiriaEnhancements.Core
{
    internal enum EncounterEnemyTier
    {
        Normal,
        // Keep the game's EMonsterType terminology. "Elite" is used by a few
        // concrete spawners, but it is not the native combat classification.
        Miniboss,
        Boss
    }

    internal sealed class EncounterDefeatTracker
    {
        private readonly HashSet<uint> observed = new HashSet<uint>();
        internal int DefeatedCount { get; private set; }
        internal int LocalFinalBlows { get; private set; }
        internal int NormalDefeated { get; private set; }
        internal int MinibossDefeated { get; private set; }
        internal int BossDefeated { get; private set; }
        internal bool RecordDefeat(uint identity, EncounterEnemyTier tier)
        {
            if (identity != 0 && !observed.Add(identity)) return false;
            DefeatedCount++;
            if (tier == EncounterEnemyTier.Boss) BossDefeated++;
            else if (tier == EncounterEnemyTier.Miniboss) MinibossDefeated++;
            else NormalDefeated++;
            return true;
        }

        internal void RecordLocalFinalBlow() => LocalFinalBlows++;

        internal void CopyFrom(EncounterDefeatTracker source)
        {
            Reset();
            observed.UnionWith(source.observed);
            DefeatedCount = source.DefeatedCount;
            LocalFinalBlows = source.LocalFinalBlows;
            NormalDefeated = source.NormalDefeated;
            MinibossDefeated = source.MinibossDefeated;
            BossDefeated = source.BossDefeated;
        }

        internal void Reset()
        {
            observed.Clear();
            DefeatedCount = 0;
            LocalFinalBlows = 0;
            NormalDefeated = 0;
            MinibossDefeated = 0;
            BossDefeated = 0;
        }
    }
}
