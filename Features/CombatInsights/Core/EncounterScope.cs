using System;

namespace SephiriaEnhancements.Core
{
    internal enum EncounterScopeKind
    {
        Ordinary,
        Boss
    }

    internal sealed class EncounterScope
    {
        private readonly float minX, minY, maxX, maxY;

        private EncounterScope(string floorGuid, int sourceInstanceId,
            EncounterScopeKind kind,
            float left, float bottom, float right, float top)
        {
            FloorGuid = floorGuid;
            SourceInstanceId = sourceInstanceId;
            Kind = kind;
            minX = left;
            minY = bottom;
            maxX = right;
            maxY = top;
        }

        internal string FloorGuid { get; }
        // Identifies the native spawner that supplied this encounter area. This
        // is not a game Room identifier.
        internal int SourceInstanceId { get; }
        internal EncounterScopeKind Kind { get; }

        internal static EncounterScope Create(string floorGuid,
            int sourceInstanceId,
            EncounterScopeKind kind, float left, float bottom, float right, float top)
        {
            if (string.IsNullOrEmpty(floorGuid) || sourceInstanceId == 0 ||
                !IsFinite(left) || !IsFinite(bottom) || !IsFinite(right) ||
                !IsFinite(top) || right <= left || top <= bottom)
            {
                return null!;
            }
            return new EncounterScope(floorGuid, sourceInstanceId, kind,
                left, bottom, right, top);
        }

        internal bool Contains(float x, float y) =>
            x >= minX && x <= maxX && y >= minY && y <= maxY;

        internal bool IsSame(EncounterScope other) => other != null &&
            SourceInstanceId == other.SourceInstanceId &&
            string.Equals(FloorGuid, other.FloorGuid, StringComparison.Ordinal);

        internal bool AllowsDamage(string ownerFloor, float ownerX, float ownerY,
            float targetX, float targetY) =>
            !string.IsNullOrEmpty(ownerFloor) &&
            string.Equals(FloorGuid, ownerFloor, StringComparison.Ordinal) &&
            Contains(ownerX, ownerY) && Contains(targetX, targetY);

        internal static EncounterScope SelectContaining(EncounterScope selected,
            EncounterScope candidate, float x, float y)
        {
            if (candidate == null || !candidate.Contains(x, y)) return selected;
            if (selected == null) return candidate;
            if (candidate.Kind != selected.Kind)
                return candidate.Kind == EncounterScopeKind.Boss ? candidate : selected;
            double selectedArea = ((double)selected.maxX - selected.minX) *
                ((double)selected.maxY - selected.minY);
            double candidateArea = ((double)candidate.maxX - candidate.minX) *
                ((double)candidate.maxY - candidate.minY);
            return candidateArea < selectedArea ||
                (candidateArea == selectedArea &&
                    candidate.SourceInstanceId < selected.SourceInstanceId)
                ? candidate : selected;
        }

        private static bool IsFinite(float value) =>
            !float.IsNaN(value) && !float.IsInfinity(value);
    }

    internal static class PlayerIdentityKey
    {
        internal static long Resolve(uint networkId, int instanceId) => networkId != 0
            ? networkId
            : -1L - unchecked((uint)instanceId);
    }
}
