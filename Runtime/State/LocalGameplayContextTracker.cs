#nullable disable
using System;

namespace SephiriaEnhancements.Runtime
{
    internal enum LocalGameplayContextChange
    {
        None,
        WorldSessionLoaded,
        PlayerChanged,
        TravelStarted,
        FloorChanged
    }

    internal sealed class LocalGameplayContextTracker
    {
        private object player;
        private string floorGuid = string.Empty;

        internal bool IsTraveling { get; private set; }
        internal string FloorGuid => floorGuid;

        internal LocalGameplayContextChange Observe(object currentPlayer,
            string currentFloorGuid, bool traveling)
        {
            currentFloorGuid = currentFloorGuid ?? string.Empty;
            LocalGameplayContextChange change;
            if (!ReferenceEquals(player, currentPlayer))
                change = LocalGameplayContextChange.PlayerChanged;
            else if (currentPlayer == null)
                change = LocalGameplayContextChange.None;
            else if (!string.Equals(floorGuid, currentFloorGuid,
                StringComparison.Ordinal))
                change = LocalGameplayContextChange.FloorChanged;
            else if (traveling && !IsTraveling)
                change = LocalGameplayContextChange.TravelStarted;
            else
                change = LocalGameplayContextChange.None;

            player = currentPlayer;
            floorGuid = currentFloorGuid;
            IsTraveling = currentPlayer != null && traveling;
            return change;
        }
    }
}
