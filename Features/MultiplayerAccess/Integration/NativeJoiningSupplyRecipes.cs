using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Mirror;
using UnityEngine;

namespace SephiriaEnhancements.MultiplayerAccess.Integration
{
    internal static class NativeJoiningSupplyRecipes
    {
        internal static FloorGenerator FloorAt(Vector3 position) => FloorGenerator.FloorGenerators
            .Find(floor => floor != null && Mathf.Abs(floor.transform.position.x - position.x) < 250f &&
                Mathf.Abs(floor.transform.position.y - position.y) < 250f);

        internal static JoiningSupplyOpportunity Read(GameObject obj, NetworkConnectionToClient owner, uint prefabOverride = 0)
        {
            if (obj == null) return null;
            obj.TryGetComponent<NetworkIdentity>(out var identity);
            if (identity == null && prefabOverride == 0) return null;
            var result = new JoiningSupplyOpportunity { Prefab = prefabOverride != 0 ? prefabOverride : identity.assetId };
            int seed = 0;
            if (obj.TryGetComponent<Sephirite>(out var reward))
            {
                if (owner?.identity == null || obj.name.StartsWith("Sephirite_LVUP", StringComparison.Ordinal)) return null;
                result.Kind = JoiningSupplyKind.ArtifactReward;
                result.Variant = (int)reward.type;
                seed = unchecked(reward.CurrentSeed - owner.identity.GetComponent<PlayerAvatar>().RandomID);
            }
            else if (obj.GetComponent<MiracleSelector2>() != null) result.Kind = JoiningSupplyKind.MiracleChoice;
            else if (obj.TryGetComponent<InventoryOrb>(out var storage))
            { result.Kind = JoiningSupplyKind.InventorySpace; result.Amount = storage.storage; }
            else if (obj.TryGetComponent<MaxHPDispenser>(out var health))
            { result.Kind = JoiningSupplyKind.MaximumHealth; result.Amount = health.increaseHp; }
            else if (obj.TryGetComponent<DiceSpawner>(out var dice))
            { result.Kind = JoiningSupplyKind.RerollDice; result.Amount = dice.diceAmount; }
            else if (obj.TryGetComponent<Dice>(out var pickup) && pickup.amount > 0 && owner?.identity != null)
            { result.Kind = JoiningSupplyKind.RerollDice; result.Amount = pickup.amount; result.Variant = 1; }
            else if (obj.GetComponent<AltarOfTablet>() != null) result.Kind = JoiningSupplyKind.TabletAltar;
            else if (obj.TryGetComponent<AltarOfEnchant>(out var enchant))
            { result.Kind = JoiningSupplyKind.EnchantAltar; result.Amount = enchant.localUseCount; }
            else if (obj.TryGetComponent<Anvil>(out var anvil))
            { result.Kind = JoiningSupplyKind.Anvil; result.Amount = anvil.enhanceSlotCount; }
            else return null;
            var floor = FloorAt(obj.transform.position);
            if (floor == null) return null;
            result.Floor = floor.guid;
            if (reward == null) seed = obj.GetComponents<MonoBehaviour>().OfType<IRandomID>().FirstOrDefault()?.RandomID ?? 0;
            result.Seed = seed;
            var position = obj.transform.position - floor.transform.position;
            result.Id = string.Join("/", floor.guid, (int)result.Kind, result.Variant,
                Mathf.RoundToInt(position.x * 100f), Mathf.RoundToInt(position.y * 100f), seed);
            return result;
        }

        internal static bool TryFindPosition(PlayerAvatar player, out Vector3 position)
        {
            int obstacles = CombatManager.TileLayerMask | CombatManager.CliffLayerMask | CombatManager.PitLayerMask |
                CombatManager.BlockCharacterLayerMask | CombatManager.PathfindingObstacleLayerMask;
            for (int direction = 0; direction < 8; direction++)
            {
                float angle = direction * Mathf.PI / 4f;
                var offset = new Vector2(Mathf.Sin(angle), -Mathf.Cos(angle));
                if (Physics2D.CircleCast(player.transform.position, 0.6f, offset, 2f, obstacles).collider != null) continue;
                position = player.transform.position + (Vector3)offset * 2f;
                return true;
            }
            position = default;
            return false;
        }

        internal static GameObject Create(JoiningSupplyOpportunity recipe, PlayerAvatar player, Vector3 position)
        {
            GameObject prefab;
            if (recipe.Kind == JoiningSupplyKind.LevelReward)
                prefab = Resources.Load<GameObject>("Sephirite/Sephirite_LVUP");
            else if (!NetworkClient.prefabs.TryGetValue(recipe.Prefab, out prefab))
                throw new InvalidOperationException("The native reward prefab is not registered.");
            if (prefab == null) throw new InvalidOperationException("The native reward prefab is missing.");
            GameObject obj = UnityEngine.Object.Instantiate(prefab, position, Quaternion.identity);
            foreach (var random in obj.GetComponents<MonoBehaviour>().OfType<IRandomID>())
                random.SetRandomID(recipe.Seed);
            if (obj.TryGetComponent<Sephirite>(out var reward))
            {
                reward.type = recipe.Kind == JoiningSupplyKind.LevelReward ? Sephirite.Type.NORMAL : (Sephirite.Type)recipe.Variant;
                reward.Initialize(recipe.Kind == JoiningSupplyKind.LevelReward ? recipe.Seed : unchecked(recipe.Seed + player.RandomID));
            }
            if (obj.TryGetComponent<InventoryOrb>(out var storage)) storage.storage = checked((short)recipe.Amount);
            if (obj.TryGetComponent<MaxHPDispenser>(out var health)) health.increaseHp = recipe.Amount;
            if (obj.TryGetComponent<DiceSpawner>(out var dice)) dice.diceAmount = recipe.Amount;
            if (obj.TryGetComponent<AltarOfEnchant>(out var enchant)) enchant.localUseCount = recipe.Amount;
            if (obj.TryGetComponent<Anvil>(out var anvil)) anvil.enhanceSlotCount = recipe.Amount;
            return obj;
        }

        internal static bool Acquired(GameObject obj, PlayerSpawner player)
        {
            string hash = Hash(player.playerGuid);
            if (obj.TryGetComponent<MiracleSelector2>(out var miracle)) return miracle.acquiredHashes.Contains(hash);
            if (obj.TryGetComponent<InventoryOrb>(out var storage)) return storage.acquiredHashes.Contains(hash);
            if (obj.TryGetComponent<DiceSpawner>(out var dice)) return dice.servedGuidHashes.Contains(hash);
            if (obj.TryGetComponent<Anvil>(out var anvil)) return anvil.enhancedGuidHashes.Contains(hash);
            if (obj.TryGetComponent<MaxHPDispenser>(out var health))
                return ((HashSet<string>)Field(typeof(MaxHPDispenser), "usedGuids").GetValue(health)).Contains(player.playerGuid);
            if (obj.TryGetComponent<AltarOfEnchant>(out var enchant))
                return ((Dictionary<string, int>)Field(typeof(AltarOfEnchant), "remainingByGuid").GetValue(enchant))
                    .TryGetValue(player.playerGuid, out int remaining) && remaining <= 0;
            return obj.TryGetComponent<Sephirite>(out var reward) && reward.isAcquired;
        }

        internal static FieldInfo Field(Type type, string name) => AccessTools.Field(type, name) ??
            throw new MissingFieldException(type.Name, name);

        private static string Hash(string guid)
        {
            using (var algorithm = System.Security.Cryptography.SHA256.Create())
                return Convert.ToBase64String(algorithm.ComputeHash(System.Text.Encoding.UTF8.GetBytes(guid)));
        }

        internal static bool Ready(PlayerAvatar player) => Loaded(player) && !player.IsDead && !player.IsInBattle;

        internal static bool Loaded(PlayerAvatar player) => player != null &&
            player.loadingScreenType == -1 && !string.IsNullOrEmpty(player.currentFloorGuid) &&
            FloorGenerator.FloorGenerators.Any(floor => floor != null && floor.guid == player.currentFloorGuid && floor.GenerateSuccess);

        internal static bool CanOpen(PlayerAvatar player) => Ready(player) && UIManager.Instance != null &&
            !UIManager.Instance.IsHidden && UIManager.Instance.IsPausePanelOpeningAllowed &&
            UIManager.Instance.CurrentControlStack.Count == 0;
    }
}
