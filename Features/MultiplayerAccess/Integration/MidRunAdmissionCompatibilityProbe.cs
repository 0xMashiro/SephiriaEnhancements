using SephiriaEnhancements.Diagnostics;
using System.Collections.Generic;
using Mirror;
using SephiriaEnhancements.Runtime.GameBridge;
using UnityEngine;

namespace SephiriaEnhancements.MultiplayerAccess.Integration
{
    internal static class MidRunAdmissionCompatibilityProbe
    {
        internal static bool Validate()
        {
            var missing = new List<string>();
            NativeContractProbe.RequireMethod(typeof(HorayNetworkAuthenticator),
                "OnServerVersionMessage", missing,
                typeof(NetworkConnectionToClient),
                typeof(HorayNetworkAuthenticator.VersionMessage));
            NativeContractProbe.RequireMethod(typeof(PlayerSpawner),
                "ResolveCurrentPlayerIdxForSave", missing, typeof(string));
            NativeContractProbe.RequireMethod(typeof(DungeonManager),
                "LoadStageAndMove", missing,
                typeof(string));
            NativeContractProbe.RequireField(typeof(HorayNetworkManager),
                "versionApprovedConnIds", missing);

            if (missing.Count == 0) return true;
            SupportLogger.Warning("mid_run_contracts_changed", "[SephiriaEnhancements] Mid-run admission native " +
                "contracts changed: " + string.Join(", ", missing));
            return false;
        }
    }
}
