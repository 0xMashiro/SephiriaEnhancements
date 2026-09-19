using System;
using System.Linq;
using Mirror;
using SephiriaEnhancements.Runtime.Inventory;

namespace SephiriaEnhancements.DefeatRetry
{
    // Native save keys stay at this boundary. This contains only state needed
    // by retained players; the complete personal save stays on its owner.
    internal sealed class NativeRetryPlayerState
    {
        internal readonly int Balance, Spent, Earned;
        internal readonly long AccountCheckpointId;
        internal readonly short DeathCount;
        private readonly int[] purchases;
        private readonly int[] charms, tablets;
        internal readonly int[] SkillArtifacts;
        internal readonly InventoryItemKey[] ArtifactKeys;

        internal NativeRetryPlayerState(long accountCheckpointId, short deathCount, int balance, int spent, int earned, int[] purchases,
            int[] charms, int[] tablets, int[] skillArtifacts, InventoryItemKey[] artifactKeys)
        {
            AccountCheckpointId = accountCheckpointId;
            DeathCount = deathCount;
            Balance = balance;
            Spent = spent;
            Earned = earned;
            this.purchases = (int[])purchases.Clone();
            this.charms = charms == null ? Array.Empty<int>() : (int[])charms.Clone();
            this.tablets = tablets == null ? Array.Empty<int>() : (int[])tablets.Clone();
            SkillArtifacts = (int[])skillArtifacts.Clone();
            ArtifactKeys = (InventoryItemKey[])artifactKeys.Clone();
        }

        internal static NativeRetryPlayerState Capture(PlayerLocalDataStorage data, long accountCheckpointId) => new NativeRetryPlayerState(
            accountCheckpointId, data.deathCount, data.sapphire, data.sapphireUseInRun, 0, data.purchasedPocketDimensionItem.ToArray(),
            Array.Empty<int>(), Array.Empty<int>(), Array.Empty<int>(), Array.Empty<InventoryItemKey>());

        internal NativeRetryPlayerState WithWorld(PlayerSpawner player) => new NativeRetryPlayerState(AccountCheckpointId, DeathCount, Balance, Spent,
            player.sapphireInRun, purchases, player.unlockedCharms.ToArray(), player.unlockedStoneTablets.ToArray(),
            player.PlayerAvatar.Inventory.charms.Values.Where(charm => charm is Charm_Magic || charm is Charm_Active)
                .Select(charm => charm.Item.InstanceID).ToArray(),
            player.PlayerAvatar.Inventory.charms.Values.Select(charm =>
                new InventoryItemKey(charm.Item.EntityID, charm.Item.InstanceID)).ToArray());

        internal void RestorePlayer(PlayerSpawner player)
        {
            player.LocalDataStorage.NetworkdeathCount = DeathCount;
            player.LocalDataStorage.Networksapphire = Balance;
            player.LocalDataStorage.sapphireUseInRun = Spent;
            player.LocalDataStorage.purchasedPocketDimensionItem.Clear();
            player.LocalDataStorage.purchasedPocketDimensionItem.AddRange(purchases);
            player.LocalDataStorage.runSapphireSettled = false;
            player.NetworksapphireInRun = Earned;
            if (NetworkServer.active)
            {
                player.unlockedCharms.Clear();
                foreach (int item in charms) player.unlockedCharms.Add(item);
                player.unlockedStoneTablets.Clear();
                foreach (int item in tablets) player.unlockedStoneTablets.Add(item);
            }
        }

        internal void WriteRun(SaveData run, int playerIndex)
        {
            string prefix = "Player" + playerIndex;
            run.SetInt(prefix + "SapphireUseInRun", Spent);
            run.SetInt(prefix + "SapphireInRun", Earned);
            run.SetInt(prefix + "PocketDimensionItemCount", purchases.Length);
            for (int i = 0; i < purchases.Length; i++) run.SetInt(prefix + "PocketDimensionItem" + i, purchases[i]);
        }

        internal void RestoreOwner(PlayerSpawner player)
        {
            RestorePlayer(player);
            SaveManager.Current.SetInt("Sapphire", Balance);
            SaveManager.Current.SetInt("DeathCount", DeathCount);
            WriteRun(SaveManager.CurrentRun, player.currentPlayerIdxForSave);
            SaveManager.Current.enableSave = SaveManager.CurrentRun.enableSave = true;
        }

        internal bool Matches(PlayerSpawner player) => player.LocalDataStorage.deathCount == DeathCount &&
            player.LocalDataStorage.sapphire == Balance &&
            player.LocalDataStorage.sapphireUseInRun == Spent && player.sapphireInRun == Earned &&
            !player.LocalDataStorage.runSapphireSettled &&
            player.LocalDataStorage.purchasedPocketDimensionItem.SequenceEqual(purchases) &&
            player.unlockedCharms.SequenceEqual(charms) && player.unlockedStoneTablets.SequenceEqual(tablets);

        internal static void Write(NetworkWriter writer, NativeRetryPlayerState state)
        {
            writer.WriteBool(state != null);
            if (state == null) return;
            writer.WriteLong(state.AccountCheckpointId); writer.WriteShort(state.DeathCount);
            writer.WriteInt(state.Balance); writer.WriteInt(state.Spent); writer.WriteInt(state.Earned);
            writer.WriteInt(state.purchases.Length);
            foreach (int item in state.purchases) writer.WriteInt(item);
            writer.WriteInt(state.charms.Length);
            foreach (int item in state.charms) writer.WriteInt(item);
            writer.WriteInt(state.tablets.Length);
            foreach (int item in state.tablets) writer.WriteInt(item);
            writer.WriteInt(state.SkillArtifacts.Length);
            foreach (int item in state.SkillArtifacts) writer.WriteInt(item);
            writer.WriteInt(state.ArtifactKeys.Length);
            foreach (InventoryItemKey item in state.ArtifactKeys)
            {
                writer.WriteInt(item.EntityId);
                writer.WriteInt(item.NativeInstanceId);
            }
        }

        internal static NativeRetryPlayerState Read(NetworkReader reader)
        {
            if (!reader.ReadBool()) return null;
            long accountCheckpointId = reader.ReadLong(); short deathCount = reader.ReadShort();
            int balance = reader.ReadInt(), spent = reader.ReadInt(), earned = reader.ReadInt();
            int count = reader.ReadInt();
            if (count < 0 || count > reader.Remaining / 4) throw new InvalidOperationException("Invalid retry purchase count.");
            var purchases = new int[count];
            for (int i = 0; i < count; i++) purchases[i] = reader.ReadInt();
            int[] charms = ReadItems(reader), tablets = ReadItems(reader), skills = ReadItems(reader);
            count = reader.ReadInt();
            if (count < 0 || count > reader.Remaining / 8) throw new InvalidOperationException("Invalid retry artifact count.");
            var artifacts = new InventoryItemKey[count];
            for (int i = 0; i < count; i++) artifacts[i] = new InventoryItemKey(reader.ReadInt(), reader.ReadInt());
            return new NativeRetryPlayerState(accountCheckpointId, deathCount, balance, spent, earned, purchases, charms, tablets, skills, artifacts);
        }

        private static int[] ReadItems(NetworkReader reader)
        {
            int count = reader.ReadInt();
            if (count < 0 || count > reader.Remaining / 4) throw new InvalidOperationException("Invalid retry item count.");
            var items = new int[count];
            for (int i = 0; i < count; i++) items[i] = reader.ReadInt();
            return items;
        }
    }
}
