using System.Collections.Generic;

namespace SephiriaEnhancements.AutoCasting
{
    // Selection belongs to owned artifacts, not to the keys used to activate them.
    internal sealed class AutoCastingSelection
    {
        private readonly HashSet<int> artifacts = new HashSet<int>();
        internal bool Contains(int artifactId) => artifacts.Contains(artifactId);
        internal void Toggle(int artifactId)
        {
            if (!artifacts.Remove(artifactId)) artifacts.Add(artifactId);
        }
        internal void Retain(IEnumerable<int> ownedArtifacts) => artifacts.IntersectWith(ownedArtifacts);
        internal void Clear() => artifacts.Clear();
    }
}
