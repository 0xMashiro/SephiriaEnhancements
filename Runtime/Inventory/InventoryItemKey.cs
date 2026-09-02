#nullable disable
using System;
using System.Globalization;

namespace SephiriaEnhancements.Runtime.Inventory
{
    // Native instance IDs can be shared by different artifact types.
    // This key identifies an item within the inventory being observed.
    internal readonly struct InventoryItemKey : IEquatable<InventoryItemKey>
    {
        internal InventoryItemKey(int entityId, int nativeInstanceId)
        {
            EntityId = entityId;
            NativeInstanceId = nativeInstanceId;
        }

        internal int EntityId { get; }
        internal int NativeInstanceId { get; }

        public bool Equals(InventoryItemKey other) =>
            EntityId == other.EntityId && NativeInstanceId == other.NativeInstanceId;

        public override bool Equals(object obj) =>
            obj is InventoryItemKey other && Equals(other);

        public override int GetHashCode() => HashCode.Combine(EntityId, NativeInstanceId);

        public override string ToString() =>
            EntityId.ToString(CultureInfo.InvariantCulture) + ":" +
            NativeInstanceId.ToString(CultureInfo.InvariantCulture);

        public static bool operator ==(InventoryItemKey left, InventoryItemKey right) =>
            left.Equals(right);

        public static bool operator !=(InventoryItemKey left, InventoryItemKey right) =>
            !left.Equals(right);
    }
}
