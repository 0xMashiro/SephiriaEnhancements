using System;
using System.Collections.Generic;
using System.Linq;
using Mirror;

namespace SephiriaEnhancements.DefeatRetry
{
    // Native save keys stay at this boundary. Each balance belongs to its player,
    // whereas run earnings are captured from the authoritative world.
    internal sealed class NativeRetrySapphire
    {
        internal readonly int Balance, Spent, Earned;
        private readonly int[] purchases;
        private readonly int[] charms, tablets;
        private readonly Dictionary<string, int> unlocks;

        internal NativeRetrySapphire(int balance, int spent, int earned, int[] purchases,
            Dictionary<string, int> unlocks, int[] charms = null, int[] tablets = null)
        {
            Balance = balance;
            Spent = spent;
            Earned = earned;
            this.purchases = (int[])purchases.Clone();
            this.unlocks = new Dictionary<string, int>(unlocks);
            this.charms = charms == null ? Array.Empty<int>() : (int[])charms.Clone();
            this.tablets = tablets == null ? Array.Empty<int>() : (int[])tablets.Clone();
        }

        internal static NativeRetrySapphire Capture(PlayerLocalDataStorage data) => new NativeRetrySapphire(
            data.sapphire, data.sapphireUseInRun, 0, data.purchasedPocketDimensionItem.ToArray(),
            SaveManager.Current.BakedData.Where(pair => IsUnlock(pair.Key))
                .ToDictionary(pair => pair.Key, pair => SaveManager.Current.GetInt(pair.Key, 0)));

        private static bool IsUnlock(string key) => key.StartsWith("PocketDimension_Item_", StringComparison.Ordinal) &&
            key.EndsWith("_Buy", StringComparison.Ordinal);

        internal NativeRetrySapphire WithWorld(PlayerSpawner player) => new NativeRetrySapphire(Balance, Spent,
            player.sapphireInRun, purchases, unlocks, player.unlockedCharms.ToArray(), player.unlockedStoneTablets.ToArray());

        internal void RestorePlayer(PlayerSpawner player)
        {
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
            // Death commits these purchases before the player chooses retry.
            // Only purchase flags are rolled back; other personal progress stays owned by its native lifecycle.
            foreach (string key in SaveManager.Current.BakedData.Select(pair => pair.Key).Where(IsUnlock).ToArray())
                SaveManager.Current.SetInt(key, unlocks.TryGetValue(key, out int value) ? value : 0);
            foreach (var pair in unlocks) SaveManager.Current.SetInt(pair.Key, pair.Value);
            WriteRun(SaveManager.CurrentRun, player.currentPlayerIdxForSave);
            SaveManager.Current.enableSave = SaveManager.CurrentRun.enableSave = true;
        }

        internal bool Matches(PlayerSpawner player) => player.LocalDataStorage.sapphire == Balance &&
            player.LocalDataStorage.sapphireUseInRun == Spent && player.sapphireInRun == Earned &&
            !player.LocalDataStorage.runSapphireSettled &&
            player.LocalDataStorage.purchasedPocketDimensionItem.SequenceEqual(purchases) &&
            player.unlockedCharms.SequenceEqual(charms) && player.unlockedStoneTablets.SequenceEqual(tablets);

        internal static void Write(NetworkWriter writer, NativeRetrySapphire state)
        {
            writer.WriteBool(state != null);
            if (state == null) return;
            writer.WriteInt(state.Balance); writer.WriteInt(state.Spent); writer.WriteInt(state.Earned);
            writer.WriteInt(state.purchases.Length);
            foreach (int item in state.purchases) writer.WriteInt(item);
            writer.WriteInt(state.unlocks.Count);
            foreach (var pair in state.unlocks) { writer.WriteString(pair.Key); writer.WriteInt(pair.Value); }
            writer.WriteInt(state.charms.Length);
            foreach (int item in state.charms) writer.WriteInt(item);
            writer.WriteInt(state.tablets.Length);
            foreach (int item in state.tablets) writer.WriteInt(item);
        }

        internal static NativeRetrySapphire Read(NetworkReader reader)
        {
            if (!reader.ReadBool()) return null;
            int balance = reader.ReadInt(), spent = reader.ReadInt(), earned = reader.ReadInt();
            int count = reader.ReadInt();
            if (count < 0 || count > reader.Remaining / 4) throw new InvalidOperationException("Invalid retry purchase count.");
            var purchases = new int[count];
            for (int i = 0; i < count; i++) purchases[i] = reader.ReadInt();
            count = reader.ReadInt();
            if (count < 0 || count > reader.Remaining / 6) throw new InvalidOperationException("Invalid retry unlock count.");
            var unlocks = new Dictionary<string, int>();
            for (int i = 0; i < count; i++)
            {
                string key = reader.ReadString();
                if (key == null || !IsUnlock(key)) throw new InvalidOperationException("Invalid retry purchase key.");
                unlocks.Add(key, reader.ReadInt());
            }
            return new NativeRetrySapphire(balance, spent, earned, purchases, unlocks, ReadItems(reader), ReadItems(reader));
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
