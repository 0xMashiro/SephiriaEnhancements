using System;
using Mirror;
using SephiriaEnhancements.Configuration;
using SephiriaEnhancements.Integration;
using SephiriaEnhancements.Runtime;
using UnityEngine;

namespace SephiriaEnhancements.CostumeAppearance.Integration
{
    internal sealed class NativeCostumeAppearance : MonoBehaviour
    {
        internal const string SaveKey = "SephiriaEnhancements.CostumeAppearance";
        private const string CapabilityKey = "SephiriaEnhancements.CostumeAppearanceProtocol";
        internal static NativeCostumeAppearance Instance { get; private set; }
        private PlayerAvatar observed, requestedPlayer;
        private object observedSave, requestedSave;
        private string requestedCostume, requestedSkin, requestedPreference;
        private float requestedAt;
        private bool restored, explicitRequest, sending;
        internal bool Pending => requestedPlayer != null;
        internal static bool Available => EnhancementsSettings.Enabled && FeatureFailure.IsAvailable(FeatureId.CostumeAppearance);
        internal static bool HostSupports => Available && (NetworkServer.active ||
            (DungeonManager.Instance != null && DungeonManager.Instance.constValueDictionary.TryGetValue(CapabilityKey, out int v) && v == 1));

        internal static bool Owned(CostumeSkinEntity skin)
        {
            if (skin == null || SaveManager.Current == null) return false;
            CostumeEntity costume = CostumeDatabase.FindCostumeByID(skin.relatedCostumeID);
            if (costume == null) return false;
            bool costumeOwned = costume.costumeType == ECostumeUnlockType.Default ||
                (costume.costumeType == ECostumeUnlockType.Dungreed ? CostumeDatabase.IsDungreedDlcOwned() : CostumeDatabase.IsUnlocked(costume.id));
            return costumeOwned && (skin.unlockType == CostumeSkinEntity.ECostumeUnlockType.Default ||
                (skin.unlockType == CostumeSkinEntity.ECostumeUnlockType.Purchase && SaveManager.Current.GetBool("SkinPurchased_" + skin.skinID, false)));
        }

        internal static string Preference => SaveManager.Current?.GetString(SaveKey, "") ?? "";
        internal static string FollowingSkin(string costume)
        {
            var skin = CostumeDatabase.GetCostumeSkinByID(SaveManager.Current?.GetString("PlayerCostume_CurrentSkin_" + costume, "") ?? "");
            if (skin != null && skin.relatedCostumeID == costume && Owned(skin)) return skin.skinID;
            return CostumeDatabase.FindDefaultCostumeSkin(costume)?.skinID ?? "";
        }

        internal static string OverrideSkin(PlayerAvatar player, string original)
        {
            if (!HostSupports || !LocalPlayerResolver.IsLocal(player) || Instance == null || Instance.sending) return original;
            var preferred = CostumeDatabase.GetCostumeSkinByID(Preference);
            return Owned(preferred) ? preferred.skinID : original;
        }

        internal void Initialize() => Instance = this;

        private void Update()
        {
            if (!FeatureFailure.IsAvailable(FeatureId.CostumeAppearance)) return;
            try { UpdateCore(); }
            catch (Exception exception) { FeatureFailure.Disable(FeatureId.CostumeAppearance, exception); }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private void UpdateCore()
        {
            var world = DungeonManager.Instance;
            if (NetworkServer.active && world != null)
            {
                int value = Available ? 1 : 0;
                if (!world.constValueDictionary.TryGetValue(CapabilityKey, out int old) || old != value)
                    world.constValueDictionary[CapabilityKey] = value;
            }
            PlayerAvatar player = LocalPlayerResolver.Resolve();
            // Native restart can reinitialize the same avatar during travel.
            if (player != null && player.loadingScreenType != -1) restored = false;
            if (!ReferenceEquals(observed, player) || !ReferenceEquals(observedSave, SaveManager.Current))
            {
                observed = player; observedSave = SaveManager.Current; restored = false;
                if (Pending) Finish(false, CostumeAppearanceLocalization.Interrupted);
            }
            if (Pending)
            {
                if (!Available || !HostSupports || player != requestedPlayer ||
                    !ReferenceEquals(requestedSave, SaveManager.Current) || player.loadingScreenType != -1 ||
                    player.currentCostume != requestedCostume)
                    Finish(false, CostumeAppearanceLocalization.Interrupted);
                else if (player.currentCostumeSkin == requestedSkin)
                    Finish(true, null);
                else if (Time.unscaledTime - requestedAt > 5f)
                    Finish(false, CostumeAppearanceLocalization.Unconfirmed);
            }
            if (!Available || !HostSupports || restored || player == null || player.loadingScreenType != -1 ||
                string.IsNullOrEmpty(player.currentCostume) || Pending) return;
            restored = true;
            string skin = Preference;
            if (Owned(CostumeDatabase.GetCostumeSkinByID(skin)) && player.currentCostumeSkin != skin)
                Request(player, skin, false);
        }

        internal bool Request(PlayerAvatar player, string preference, bool userAction)
        {
            if (Pending || !HostSupports || !LocalPlayerResolver.IsLocal(player) || player.loadingScreenType != -1 || SaveManager.Current == null)
                return false;
            string skin = string.IsNullOrEmpty(preference) ? FollowingSkin(player.currentCostume) : preference;
            if (!Owned(CostumeDatabase.GetCostumeSkinByID(skin))) return false;
            requestedPlayer = player; requestedSave = SaveManager.Current;
            requestedCostume = player.currentCostume; requestedSkin = skin;
            requestedPreference = preference; requestedAt = Time.unscaledTime; explicitRequest = userAction;
            sending = true;
            try { player.EquipCostume(requestedCostume, skin); }
            finally { sending = false; }
            return true;
        }

        private void Finish(bool confirmed, string failure)
        {
            bool notify = explicitRequest;
            string preference = requestedPreference;
            requestedPlayer = null; requestedSave = null; explicitRequest = false;
            if (confirmed && notify)
            {
                SaveManager.Current.SetString(SaveKey, preference);
                SaveManager.Save(saveCurrent: true, saveCurrentRun: false);
                NativeModNotifications.ShortText(() => ModLocalization.Get(CostumeAppearanceLocalization.Applied));
            }
            else if (!confirmed && notify)
                NativeModNotifications.ShortText(() => ModLocalization.Get(failure));
        }

        internal void Shutdown()
        {
            if (NetworkServer.active && DungeonManager.Instance != null)
                DungeonManager.Instance.constValueDictionary[CapabilityKey] = 0;
            requestedPlayer = null;
            NativeCostumeAppearanceView.DisposeAll();
            if (Instance == this) Instance = null;
        }

        private void OnDestroy() => Shutdown();
    }
}
