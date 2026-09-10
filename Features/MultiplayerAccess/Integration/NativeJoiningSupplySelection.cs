using System;
using System.Collections.Generic;
using System.Linq;
using Mirror;
using UnityEngine;

namespace SephiriaEnhancements.MultiplayerAccess.Integration
{
    [Serializable]
    internal sealed class NativeJoiningSupplySelection
    {
        public int Seed, Rotation, FreeRerolls, Rerolls;
        public bool Generated;
        public List<int> Appeared = new List<int>();
        public List<int> ItemIds = new List<int>();
        public List<int> InstanceIds = new List<int>();
        public List<string> Miracles = new List<string>();
        public List<string> Requested = new List<string>();
        public int EnchantRemaining = -1;
        public JoiningSupplyOpportunity Child;

        internal static string Capture(GameObject obj, PlayerSpawner player, JoiningSupplyOpportunity child)
        {
            var state = new NativeJoiningSupplySelection { Child = child };
            if (obj.TryGetComponent<Sephirite>(out var reward))
            {
                state.Seed = reward.CurrentSeed;
                state.Rotation = reward.rotation;
                state.FreeRerolls = reward.freeRerolledCounter;
                state.Generated = reward.isGenerated;
                state.Appeared.AddRange(reward.appearedItems);
                foreach (var item in reward.rewards)
                { state.ItemIds.Add(item.entityID); state.InstanceIds.Add(item.instanceID); }
            }
            if (obj.TryGetComponent<MiracleSelector2>(out var selector))
            {
                var identity = player.netIdentity;
                if (MiracleLists(selector, "currentMiracles").TryGetValue(identity, out var current))
                    state.Miracles.AddRange(current.Select(miracle => miracle.id));
                if (MiracleLists(selector, "requestedMiracles").TryGetValue(identity, out var requested))
                    state.Requested.AddRange(requested.Select(miracle => miracle.id));
                var rerolls = (Dictionary<NetworkIdentity, int>)NativeJoiningSupplyRecipes.Field(
                    typeof(MiracleSelector2), "rerolledCount").GetValue(selector);
                rerolls.TryGetValue(identity, out state.Rerolls);
            }
            if (obj.TryGetComponent<AltarOfEnchant>(out var enchant))
            {
                var remaining = (Dictionary<string, int>)NativeJoiningSupplyRecipes.Field(
                    typeof(AltarOfEnchant), "remainingByGuid").GetValue(enchant);
                if (remaining.TryGetValue(player.playerGuid, out int amount)) state.EnchantRemaining = amount;
            }
            return JsonUtility.ToJson(state);
        }

        internal void Restore(GameObject obj, PlayerSpawner player)
        {
            if (obj.TryGetComponent<Sephirite>(out var reward))
            {
                reward.Initialize(Seed);
                reward.Networkrotation = Rotation;
                reward.NetworkfreeRerolledCounter = FreeRerolls;
                reward.appearedItems.Clear();
                foreach (int id in Appeared) reward.appearedItems.Enqueue(id);
                reward.rewards.Clear();
                for (int i = 0; i < ItemIds.Count; i++)
                    reward.rewards.Add(new SephiriteRewardMetadata(InstanceIds[i], ItemIds[i]));
                reward.NetworkisGenerated = Generated;
            }
            if (obj.TryGetComponent<MiracleSelector2>(out var selector))
            {
                var identity = player.netIdentity;
                if (Miracles.Count > 0)
                    MiracleLists(selector, "currentMiracles")[identity] = Miracles.Select(MiracleDatabase.FindMiracle).ToList();
                MiracleLists(selector, "requestedMiracles")[identity] = Requested.Select(MiracleDatabase.FindMiracle).ToList();
                ((Dictionary<NetworkIdentity, int>)NativeJoiningSupplyRecipes.Field(typeof(MiracleSelector2),
                    "rerolledCount").GetValue(selector))[identity] = Rerolls;
            }
            if (EnchantRemaining >= 0 && obj.TryGetComponent<AltarOfEnchant>(out var enchant))
                ((Dictionary<string, int>)NativeJoiningSupplyRecipes.Field(typeof(AltarOfEnchant),
                    "remainingByGuid").GetValue(enchant))[player.playerGuid] = EnchantRemaining;
        }

        private static Dictionary<NetworkIdentity, List<Miracle>> MiracleLists(MiracleSelector2 selector, string name) =>
            (Dictionary<NetworkIdentity, List<Miracle>>)NativeJoiningSupplyRecipes.Field(typeof(MiracleSelector2), name).GetValue(selector);
    }
}
