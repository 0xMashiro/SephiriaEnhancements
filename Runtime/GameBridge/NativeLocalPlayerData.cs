#nullable disable
using Mirror;
using SephiriaEnhancements.Integration;

namespace SephiriaEnhancements.Runtime.GameBridge
{
    // A peer owns its preferences and observations. They are never copied from the host.
    internal static class NativeLocalPlayerData
    {
        private static PlayerAvatar owner;
        private static NetworkConnectionToServer connection;
        private static LocalPlayerDataStore.Snapshot checkpoint, pending;
        private static long checkpointId;
        private static string checkpointFloor, destination;
        private static bool worldLoaded, progressLoaded;
        internal static bool IsRestoring { get; private set; }
        internal static bool ProgressPending => IsRestoring && !progressLoaded;

        internal static void ObserveOwner()
        {
            PlayerAvatar current = NetworkClient.active ? LocalPlayerResolver.Resolve() : null;
            if (ReferenceEquals(current, owner) && ReferenceEquals(connection, NetworkClient.connection)) return;
            Reset();
            owner = current;
            connection = NetworkClient.connection;
        }

        internal static void ObserveWorldSession(bool saved)
        {
            ObserveOwner();
            if (saved && IsRestoring && !worldLoaded && owner != null)
                worldLoaded = true;
            else Reset();
        }

        internal static void CaptureCheckpoint(long id, string floor)
        {
            ObserveOwner();
            if (ProgressPending || owner == null || owner.loadingScreenType != -1 ||
                owner.NetworkcurrentFloorGuid != floor || id == checkpointId) return;
            checkpoint = LocalPlayerDataStore.Shared.Capture();
            checkpointId = id;
            checkpointFloor = floor;
        }

        internal static void BeginRestore(long id, string floor)
        {
            ObserveOwner();
            if (owner == null) return;
            pending = id != 0 && id == checkpointId && floor == checkpointFloor ? checkpoint : null;
            if (id == 0) ClearCheckpoint();
            destination = floor;
            worldLoaded = progressLoaded = false;
            IsRestoring = true;
        }

        // Restore progress before the owner acknowledges arrival and combat resumes.
        // Preferences remain protected while delayed artifact/quick-slot objects arrive.
        internal static void LoadProgress()
        {
            ObserveOwner();
            if (!ProgressPending) return;
            if (!worldLoaded || owner == null || owner.loadingScreenType != -1 ||
                owner.NetworkcurrentFloorGuid != destination) { Reset(); return; }
            try { LocalPlayerDataStore.Shared.Load(pending); }
            finally
            {
                pending = null;
                progressLoaded = true;
            }
        }

        internal static void CompleteRestore()
        {
            try { LoadProgress(); }
            finally { IsRestoring = false; }
        }

        internal static void CancelRestore()
        {
            if (IsRestoring) Reset();
        }

        internal static void ClearCheckpoint()
        {
            checkpoint = null;
            checkpointId = 0;
            checkpointFloor = null;
        }

        internal static void Reset()
        {
            LocalPlayerDataStore.Shared.Reset();
            ClearCheckpoint();
            pending = null;
            destination = null;
            IsRestoring = worldLoaded = progressLoaded = false;
        }

        internal static void Shutdown()
        {
            Reset();
            owner = null;
            connection = null;
        }
    }
}
