using SephiriaEnhancements.MapEnhancements.Core;
using UnityEngine;
using UnityEngine.UI;

namespace SephiriaEnhancements.MapEnhancements.Integration
{
    internal sealed class NativeMapDestination
    {
        private readonly FullyDesignedFloorGenerator.MapTeleportConnection connection;
        internal MapPlaceKind Kind => NativeMapPlaces.DestinationKind(connection.icon.name);
        internal Transform Target => connection.point.transform;
        internal Vector3 Position => connection.point.GetTeleportTargetPosition();
        internal bool IsVisible => connection.icon != null && connection.point != null &&
            connection.icon.gameObject.activeInHierarchy && connection.point.gameObject.activeInHierarchy;
        internal bool IsEnabled => IsVisible && connection.icon.enabled &&
            connection.icon.GetComponent<Selectable>() is Selectable button &&
            button.isActiveAndEnabled && button.interactable;
        internal bool CanTravel => IsEnabled && connection.icon.GetComponent<Selectable>().IsInteractable();

        internal NativeMapDestination(FullyDesignedFloorGenerator.MapTeleportConnection connection)
        {
            this.connection = connection;
        }

        internal Vector2 MapPosition(RectTransform contents, NativeMapGeometry geometry) =>
            connection.icon.transform.IsChildOf(contents)
                ? (Vector2)contents.InverseTransformPoint(connection.icon.transform.position)
                : geometry.Project(Position);

    }
}
