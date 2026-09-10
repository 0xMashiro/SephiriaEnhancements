using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace SephiriaEnhancements.MultiplayerAccess.Integration
{
    [Serializable]
    internal sealed class NativeJoiningSupplyAnvil
    {
        public bool Initialized;
        public int Rerolls;
        public List<int> Choices = new List<int>();
        public List<int> Candidates = new List<int>();
        private static readonly FieldInfo InitializedField = AccessTools.Field(typeof(Anvil), "localWeaponListInitialized");

        internal static string Capture(Anvil anvil) => JsonUtility.ToJson(new NativeJoiningSupplyAnvil
        {
            Initialized = (bool)InitializedField.GetValue(anvil), Rerolls = anvil.localRerollSeedOffset,
            Choices = anvil.localWeaponList.Select(item => item.enhanced.id).ToList(),
            Candidates = anvil.candidates.Select(item => item.enhanced.id).ToList()
        });

        internal void Restore(Anvil anvil)
        {
            InitializedField.SetValue(anvil, Initialized);
            anvil.localRerollSeedOffset = Rerolls;
            anvil.localWeaponList.Clear();
            anvil.localWeaponList.AddRange(Choices.Select(id => new EnhancementMetadata { enhanced = WeaponDatabase.FindWeaponById(id) }));
            anvil.candidates.Clear();
            anvil.candidates.AddRange(Candidates.Select(id => new EnhancementMetadata { enhanced = WeaponDatabase.FindWeaponById(id) }));
        }
    }

    [HarmonyPatch]
    internal static class JoiningSupplyAnvilSelectionPatch
    {
        private static IEnumerable<MethodBase> TargetMethods()
        {
            yield return AccessTools.Method(typeof(Anvil), "HandleInteraction");
            yield return AccessTools.Method(typeof(Anvil), nameof(Anvil.Reroll));
        }
        private static void Postfix(Anvil __instance)
        {
            if (JoiningSupplyBridge.IsCurrent(__instance.netId))
                JoiningSupplyBridge.SaveAnvil(__instance.netId, NativeJoiningSupplyAnvil.Capture(__instance));
        }
    }
}
