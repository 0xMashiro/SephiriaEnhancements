#nullable enable

using System;

namespace SephiriaEnhancements.Integration
{
    // Canonical identity for a game-owned input action. Integration code passes
    // this value instead of a physical key or an unqualified action name.
    internal readonly struct NativeActionId : IEquatable<NativeActionId>
    {
        internal NativeActionId(string mapName, string actionName)
        {
            MapName = mapName;
            ActionName = actionName;
        }

        internal string MapName { get; }
        internal string ActionName { get; }
        internal string QualifiedName => MapName + "/" + ActionName;

        public bool Equals(NativeActionId other) =>
            string.Equals(MapName, other.MapName, StringComparison.Ordinal) &&
            string.Equals(ActionName, other.ActionName,
                StringComparison.Ordinal);

        public override bool Equals(object? obj) =>
            obj is NativeActionId other && Equals(other);

        public override int GetHashCode() =>
            (MapName, ActionName).GetHashCode();

        public override string ToString() => QualifiedName;
    }
}
