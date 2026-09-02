using System;
using SephiriaEnhancements.Integration;

namespace SephiriaEnhancements.Runtime.GameBridge
{
    internal sealed class NativeLocalGameplayContext : IDisposable
    {
        private readonly LocalGameplayContextTracker tracker =
            new LocalGameplayContextTracker();
        private PlayerAvatar player;

        internal PlayerAvatar Player => player;
        internal bool IsTraveling => tracker.IsTraveling;
        internal string FloorGuid => tracker.FloorGuid;
        internal event Action<LocalGameplayContextChange> Changed;

        internal void Poll()
        {
            PlayerAvatar current = LocalPlayerResolver.Resolve();
            if (!ReferenceEquals(current, player))
            {
                Unsubscribe();
                player = current;
                if (player != null)
                {
                    player.OnEnteredFloorClientside += OnEnteredFloor;
                    player.OnChangedLoadingScreenClientside += OnLoadingChanged;
                }
            }
            Observe(player != null ? player.currentFloorGuid : null,
                player != null && player.loadingScreenType != -1);
        }

        private void OnEnteredFloor(string floorGuid)
        {
            ObserveEvent(floorGuid, player.loadingScreenType != -1);
        }

        private void OnLoadingChanged(int loadingScreenType)
        {
            // Native loading completion precedes the floor-GUID update. Arrival
            // is observed separately; this callback invalidates departing work.
            ObserveEvent(player.currentFloorGuid, loadingScreenType != -1);
        }

        private void ObserveEvent(string floorGuid, bool traveling)
        {
            if (!ReferenceEquals(LocalPlayerResolver.Resolve(), player))
            {
                Poll();
                return;
            }
            Observe(floorGuid, traveling);
        }

        private void Observe(string floorGuid, bool traveling)
        {
            LocalGameplayContextChange change = tracker.Observe(
                player != null ? player : null, floorGuid, traveling);
            if (change != LocalGameplayContextChange.None)
                Changed?.Invoke(change);
        }

        private void Unsubscribe()
        {
            // Managed event handlers must also be removed from destroyed Unity
            // objects, whose overloaded null comparison already returns true.
            if (ReferenceEquals(player, null)) return;
            player.OnEnteredFloorClientside -= OnEnteredFloor;
            player.OnChangedLoadingScreenClientside -= OnLoadingChanged;
        }

        public void Dispose()
        {
            Unsubscribe();
            player = null;
            tracker.Observe(null, null, false);
            Changed = null;
        }
    }
}
