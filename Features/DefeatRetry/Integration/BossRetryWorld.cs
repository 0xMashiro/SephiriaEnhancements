using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using HarmonyLib;
using Mirror;
using UnityEngine;

namespace SephiriaEnhancements.DefeatRetry
{
    // Native boundary: retain the existing floor and recreate its original boss prop.
    // Surrounding network objects must still match the pre-fight state.
    internal sealed class BossRetryWorld
    {
        private sealed class PropRecipe
        {
            internal FloorGenerator Floor;
            internal PropEntity Prop;
            internal int Seed;
            internal Vector3 Position;
            internal Vector3 Scale;
            internal XElement Options;
        }

        private sealed class PreservedObject
        {
            internal NetworkIdentity Identity;
            internal Vector3 Position;
            internal Quaternion Rotation;
            internal byte[] State;
        }

        private static readonly Dictionary<BossSpawner, PropRecipe> Recipes =
            new Dictionary<BossSpawner, PropRecipe>();

        private readonly PropRecipe recipe;
        private readonly HashSet<uint> originalObjects;
        private readonly Dictionary<uint, PlayerAvatar> players;
        private readonly List<PreservedObject> surroundings;
        private readonly HashSet<uint> bossObjects;
        private readonly Dictionary<GoatGate, bool> gates;
        private BossSpawner boss;

        private BossRetryWorld(BossSpawner boss, PropRecipe recipe,
            HashSet<uint> originalObjects, Dictionary<uint, PlayerAvatar> players,
            List<PreservedObject> surroundings, HashSet<uint> bossObjects,
            Dictionary<GoatGate, bool> gates)
        {
            this.boss = boss;
            this.recipe = recipe;
            this.originalObjects = originalObjects;
            this.players = players;
            this.surroundings = surroundings;
            this.bossObjects = bossObjects;
            this.gates = gates;
        }

        internal static void Register(FloorGenerator floor, PropEntity prop,
            int randomID, Vector3 position, Vector3 scale, XElement options,
            GameObject created)
        {
            if (created == null || !created.TryGetComponent(out BossSpawner boss))
            {
                return;
            }
            Recipes[boss] = new PropRecipe
            {
                Floor = floor, Prop = prop, Seed = randomID,
                Position = position, Scale = scale,
                Options = options == null ? null : new XElement(options)
            };
        }

        internal static void ClearRecipes() => Recipes.Clear();

        internal static BossRetryWorld Capture(BossSpawner boss)
        {
            // Scripted/subclass spawners need their own restart contract.
            if (boss == null || boss.GetType() != typeof(BossSpawner) ||
                !Recipes.TryGetValue(boss, out PropRecipe recipe) ||
                recipe.Floor == null || boss.bossObject == null)
            {
                return null;
            }

            var players = new Dictionary<uint, PlayerAvatar>();
            foreach (PlayerSpawner player in PlayerSpawner.MultiplayerList)
            {
                PlayerAvatar avatar = player?.PlayerAvatar;
                if (avatar == null || avatar.currentFloorGuid != recipe.Floor.guid)
                {
                    return null;
                }
                players.Add(avatar.netId, avatar);
            }

            var bossObjects = new HashSet<uint> { boss.netId, boss.bossObject.netId };
            foreach (NetworkIdentity child in boss.GetComponentsInChildren<NetworkIdentity>())
            {
                bossObjects.Add(child.netId);
            }
            foreach (NetworkIdentity child in boss.bossObject.GetComponentsInChildren<NetworkIdentity>())
            {
                bossObjects.Add(child.netId);
            }
            // Closing the arena gates is part of starting this same encounter.
            var gates = new Dictionary<GoatGate, bool>();
            foreach (Transform point in boss.doorPowerPoints)
            {
                foreach (RaycastHit2D hit in Physics2D.CircleCastAll(point.position, 1f, Vector2.zero))
                {
                    GoatGate gate = hit.transform.GetComponent<GoatGate>();
                    if (gate != null)
                    {
                        gates[gate] = gate.isOpened;
                    }
                    else if (hit.transform.GetComponents<IPower>().Length != 0)
                    {
                        // IPower exposes a setter only; its original state is unknown.
                        return null;
                    }
                }
            }

            var surroundings = new List<PreservedObject>();
            foreach (NetworkIdentity identity in NetworkServer.spawned.Values)
            {
                if (bossObjects.Contains(identity.netId) || IsPlayerState(identity) ||
                    identity.GetComponent<DungeonManager>() != null)
                {
                    continue;
                }
                surroundings.Add(new PreservedObject
                {
                    Identity = identity,
                    Position = identity.transform.position,
                    Rotation = identity.transform.rotation,
                    State = gates.Keys.Any(gate => gate.netIdentity == identity)
                        ? null : Serialize(identity)
                });
            }
            return new BossRetryWorld(boss, recipe,
                new HashSet<uint>(NetworkServer.spawned.Keys), players,
                surroundings, bossObjects, gates);
        }

        internal bool CanRestore()
        {
            if (recipe.Floor == null || boss == null || boss.IsCleared ||
                players.Count != PlayerSpawner.MultiplayerList.Count)
            {
                return false;
            }
            foreach (PlayerSpawner player in PlayerSpawner.MultiplayerList)
            {
                PlayerAvatar avatar = player?.PlayerAvatar;
                if (avatar == null || !players.TryGetValue(avatar.netId, out PlayerAvatar saved) ||
                    saved != avatar || avatar.currentFloorGuid != recipe.Floor.guid)
                {
                    return false;
                }
            }
            foreach (PreservedObject item in surroundings)
            {
                NetworkIdentity identity = item.Identity;
                if (identity == null ||
                    !NetworkServer.spawned.TryGetValue(identity.netId, out NetworkIdentity registered) ||
                    registered != identity || identity.transform.position != item.Position ||
                    identity.transform.rotation != item.Rotation ||
                    (item.State != null && !Serialize(identity).SequenceEqual(item.State)))
                {
                    return false;
                }
            }
            return true;
        }

        internal void RemoveEncounterObjects()
        {
            Recipes.Remove(boss);
            // Called at native ClearNetworkObjects, after native player cleanup.
            foreach (NetworkIdentity identity in NetworkServer.spawned.Values.ToArray())
            {
                if (identity != null && (bossObjects.Contains(identity.netId) ||
                    (!originalObjects.Contains(identity.netId) && !IsPlayerState(identity))))
                {
                    NetworkServer.Destroy(identity.gameObject);
                }
            }
        }

        internal void RecreateBoss()
        {
            // LoadDungeon replaced FloorData, while these generated floors survived.
            foreach (FloorGenerator floor in FloorGenerator.FloorGenerators)
            {
                if (floor != null && DungeonManager.Instance.generatedFloors.TryGetValue(
                        floor.guid, out FloorData data))
                {
                    floor.Connect(data, floor.seed, floor.luckyType,
                        floor.pdType, floor.randomRoomCount);
                }
            }
            foreach (KeyValuePair<GoatGate, bool> gate in gates)
            {
                gate.Key.SetPowerByLever(gate.Value);
            }
            GameObject created = recipe.Floor.CreateProp(recipe.Seed, recipe.Prop,
                recipe.Position, recipe.Scale,
                recipe.Options == null ? null : new XElement(recipe.Options), null);
            if (created == null || !created.TryGetComponent(out boss) || boss.bossObject == null)
            {
                throw new InvalidOperationException("Boss prop recreation failed.");
            }
            // A fresh prop constructs a fresh boss, including its initial life/phase.
            originalObjects.ExceptWith(bossObjects);
            bossObjects.Clear();
            bossObjects.Add(boss.netId);
            bossObjects.Add(boss.bossObject.netId);
            foreach (NetworkIdentity child in boss.GetComponentsInChildren<NetworkIdentity>())
            {
                bossObjects.Add(child.netId);
            }
            foreach (NetworkIdentity child in boss.bossObject.GetComponentsInChildren<NetworkIdentity>())
            {
                bossObjects.Add(child.netId);
            }
            originalObjects.UnionWith(bossObjects);
        }

        private static byte[] Serialize(NetworkIdentity identity)
        {
            using (NetworkWriterPooled writer = NetworkWriterPool.Get())
            {
                foreach (NetworkBehaviour behaviour in identity.GetComponents<NetworkBehaviour>())
                {
                    behaviour.OnSerialize(writer, true);
                }
                return writer.ToArray();
            }
        }

        private static bool IsPlayerState(NetworkIdentity identity)
        {
            return identity.GetComponent<NetworkPlayer>() != null ||
                identity.GetComponent<PlayerAvatarAim>() != null ||
                identity.GetComponent<AvatarSpawn_Apperance>() != null ||
                identity.GetComponent<FarmAbility>() != null ||
                identity.GetComponent<PassiveObjectMetadata>() != null ||
                identity.GetComponent<Charm_Basic>() != null ||
                identity.GetComponent<StoneTablet>() != null ||
                identity.GetComponent<ComboEffectBase>() != null ||
                identity.GetComponent<UnitElemental_DarkCloud>() != null ||
                identity.GetComponent<UnitElemental_FlameGround>() != null ||
                identity.GetComponent<CharacterBuff>() != null ||
                identity.GetComponent<CostumeEquipEffect>() != null;
        }
    }

    [HarmonyPatch(typeof(FloorGenerator), nameof(FloorGenerator.CreateProp))]
    internal static class BossRetryPropRecipePatch
    {
        private static void Postfix(FloorGenerator __instance, int randomID,
            PropEntity prop, Vector3 position, Vector3 localScale, XElement options,
            GameObject __result)
        {
            if (NetworkServer.active)
            {
                BossRetryWorld.Register(__instance, prop, randomID,
                    position, localScale, options, __result);
            }
        }
    }

    [HarmonyPatch(typeof(HorayNetworkManager), nameof(HorayNetworkManager.ClearNetworkObjects))]
    internal static class BossRetryPreserveFloorPatch
    {
        private static bool Prefix() => !DefeatRetryFeature.PreserveBossRetryWorld();
    }
}
