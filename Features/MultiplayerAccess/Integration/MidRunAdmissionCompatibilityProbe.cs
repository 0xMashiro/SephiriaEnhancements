using SephiriaEnhancements.Diagnostics;
using System;
using System.Collections.Generic;
using HarmonyLib;
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
            foreach (string field in new[] { "currentMiracles", "requestedMiracles" })
                RequireFieldType(typeof(MiracleSelector2), field, typeof(Dictionary<NetworkIdentity, List<Miracle>>), missing);
            RequireFieldType(typeof(MiracleSelector2), "rerolledCount", typeof(Dictionary<NetworkIdentity, int>), missing);
            RequireFieldType(typeof(AltarOfEnchant), "remainingByGuid", typeof(Dictionary<string, int>), missing);
            RequireFieldType(typeof(MaxHPDispenser), "usedGuids", typeof(HashSet<string>), missing);
            RequireFieldType(typeof(Anvil), "localWeaponListInitialized", typeof(bool), missing);
            NativeContractProbe.RequireMethod(typeof(MiracleSelector2), "GenerateMiracles", missing,
                typeof(NetworkIdentity), typeof(int), typeof(UnitAvatar));
            NativeContractProbe.RequireMethod(typeof(MiracleSelector2), "UserCode_CmdHandleMiracleAcquired__MiracleController", missing,
                typeof(MiracleController));
            NativeContractProbe.RequireMethod(typeof(Anvil), "UserCode_CmdMarkEnhanced__NetworkConnectionToClient", missing,
                typeof(NetworkConnectionToClient));
            NativeContractProbe.RequireMethod(typeof(AltarOfEnchant), "UserCode_CmdUse__NetworkConnectionToClient", missing,
                typeof(NetworkConnectionToClient));
            foreach (var contract in new[]
            {
                (typeof(UI_MiracleElement), "actor"), (typeof(UI_MiracleElement), "entity"),
                (typeof(UI_MiracleElement), "miracleSelector"), (typeof(UI_WeaponEnhancementPanel), "anvil")
            }) NativeContractProbe.RequireField(contract.Item1, contract.Item2, missing);
            RequireClosure(typeof(UI_MiracleElement), "<>c__DisplayClass17_0", new[] { "<>4__this" }, missing);
            RequireClosure(typeof(UI_WeaponEnhancementPanel), "<>c__DisplayClass34_0", new[] { "<>4__this", "weapon" }, missing);
            RequireClosure(typeof(UI_CharacterStatusPanel), "<>c__DisplayClass175_0", new[] { "<>4__this", "icon", "savedEnchantBinding" }, missing);

            if (missing.Count == 0) return true;
            SupportLogger.Warning("mid_run_contracts_changed", "[SephiriaEnhancements] Mid-run admission native " +
                "contracts changed: " + string.Join(", ", missing));
            return false;
        }

        private static void RequireFieldType(Type owner, string name, Type expected, List<string> missing)
        {
            if (AccessTools.Field(owner, name)?.FieldType != expected) missing.Add(owner.Name + "." + name);
        }

        private static void RequireClosure(Type owner, string name, string[] fields, List<string> missing)
        {
            Type closure = AccessTools.Inner(owner, name);
            if (closure == null) { missing.Add(owner.Name + "." + name); return; }
            foreach (string field in fields) NativeContractProbe.RequireField(closure, field, missing);
        }
    }
}
