using SephiriaEnhancements.Runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using Mirror;
using HarmonyLib;
using SephiriaEnhancements.Diagnostics;
using UnityEngine;

namespace SephiriaEnhancements.MultiplayerAccess.Integration
{
    internal sealed class NativeJoiningSupplies : MonoBehaviour
    {
        private const string SaveKey = "SephiriaEnhancements.JoiningSupplies";
        internal static NativeJoiningSupplies Instance { get; private set; }
        internal static LevelController AdvancingLevel { get; private set; }
        internal static AltarOfTablet SelectingTablet;
        internal static bool SpawningFacilityDice;
        private SaveData run;
        private JoiningSupplyLedger ledger = new JoiningSupplyLedger();
        private readonly Dictionary<int, PlayerSpawner> initialized = new Dictionary<int, PlayerSpawner>();
        private readonly Dictionary<string, GameObject> available = new Dictionary<string, GameObject>();
        private readonly Dictionary<int, Grant> grants = new Dictionary<int, Grant>();
        private bool creating, dirty, loadFailed;
        private float nextPoll;
        private static readonly System.Reflection.MethodInfo CloseReward = AccessTools.Method(typeof(PlayerAvatar), "RpcOpenSephiriteUI_Null");
        internal int Revision { get; private set; }

        private sealed class Grant
        {
            internal PlayerSpawner Owner;
            internal JoiningSupplyClaim Claim;
            internal GameObject Object;
            internal JoiningSupplyOpportunity Child;
        }

        private void Awake()
        {
            if (!FeatureFailure.IsAvailable(FeatureId.MultiplayerAccess))
            {
                return;
            }

            try
            {
                AwakeCore();
            }
            catch (System.Exception exception)
            {
                FeatureFailure.Disable(FeatureId.MultiplayerAccess, exception);
                return;
            }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private void AwakeCore()
        {
            Instance = this;
            JoiningSupplyBridge.Initialize();
        }
        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
            SephiriaEnhancementsMod.CleanupFeature(FeatureId.MultiplayerAccess, Flush);
            SephiriaEnhancementsMod.CleanupFeature(FeatureId.MultiplayerAccess, SuspendAll);
            SephiriaEnhancementsMod.CleanupFeature(FeatureId.MultiplayerAccess, JoiningSupplyBridge.Shutdown);
            SephiriaEnhancementsMod.CleanupFeature(FeatureId.MultiplayerAccess, JoiningSupplyPauseButton.DisposeAll);
        }

        internal void BindRun(bool newExploration = false)
        {
            if (!NetworkServer.active) return;
            if (!newExploration && ReferenceEquals(run, SaveManager.CurrentRun)) return;
            SuspendAll();
            run = SaveManager.CurrentRun;
            available.Clear();
            if (!newExploration) initialized.Clear();
            string saved = !newExploration && run != null ? run.GetString(SaveKey, "") : "";
            loadFailed = false;
            ledger = new JoiningSupplyLedger { RecordedFromExplorationStart = newExploration };
            try
            {
                if (!string.IsNullOrEmpty(saved)) ledger = JsonUtility.FromJson<JoiningSupplyLedger>(saved);
                if (ledger == null || ledger.Opportunities == null || ledger.Participants == null ||
                    ledger.Opportunities.Any(item => item == null || item.Recipients == null) ||
                    ledger.Participants.Any(item => item == null || item.Claims == null ||
                        item.Claims.Any(claim => claim == null || claim.Opportunity == null)))
                    throw new InvalidOperationException("Invalid joining supply save data.");
            }
            catch (Exception error)
            {
                loadFailed = true;
                ledger = new JoiningSupplyLedger();
                SupportLogger.Record("joining_supply_load_failed", error.GetType().Name + ": " + error.Message, "ERROR");
            }
            Revision++;
            dirty = newExploration;
        }

        internal void Initialized(PlayerSpawner player)
        {
            BindRun();
            if (player == null) return;
            initialized[player.currentPlayerIdxForSave] = player;
            if (run == null || !MidRunAdmissionRuntime.IsAvailable) return;
            if (loadFailed) { MidRunAdmissionRuntime.RemoveConnection(player.connectionToClient); return; }
            if (!MidRunAdmissionRuntime.IsFreshConnection(player.connectionToClient)) return;
            var peers = initialized.Values
                .Where(other => other != null && other != player && other.GetComponent<LevelController>().currentLevel > 0 &&
                    other.connectionToClient?.identity == other.netIdentity &&
                    !MidRunAdmissionRuntime.IsFreshConnection(other.connectionToClient) &&
                    (ledger.Find(other.currentPlayerIdxForSave)?.BasicsComplete ?? true))
                .OrderBy(other => other.currentPlayerIdxForSave).ToArray();
            if (peers.Length > 0)
            {
                var reference = peers[0];
                var route = new HashSet<string>(reference.GetComponent<PlayerAvatar>().floorTravelHistory);
                route.Add(reference.GetComponent<PlayerAvatar>().currentFloorGuid);
                var present = new HashSet<string>(ledger.Opportunities.Where(item => item.Recipients.Count == 0 &&
                    item.Floor == reference.GetComponent<PlayerAvatar>().currentFloorGuid &&
                    available.TryGetValue(item.Id, out var obj) && obj != null).Select(item => item.Id));
                ledger.Admit(player.currentPlayerIdxForSave,
                    JoiningSupplyLedger.Mean(peers.Select(other => other.GetComponent<LevelController>().currentLevel)),
                    JoiningSupplyLedger.Mean(peers.Select(other => other.GetComponent<PlayerAvatar>().Money)),
                    reference.currentPlayerIdxForSave, route, present, player.GetComponent<PlayerAvatar>().Money);
                dirty = true;
            }
            MidRunAdmissionRuntime.RemoveConnection(player.connectionToClient);
        }

        internal void Spawned(GameObject obj, NetworkConnectionToClient owner)
        {
            if (creating || !NetworkServer.active || !MidRunAdmissionRuntime.IsAvailable) return;
            if (SpawningFacilityDice && obj != null && obj.GetComponent<Dice>() != null) return;
            BindRun();
            if (loadFailed) return;
            var recipe = NativeJoiningSupplyRecipes.Read(obj, owner);
            if (recipe == null) return;
            if (SelectingTablet != null)
            {
                var grant = grants.Values.FirstOrDefault(item => item.Object == SelectingTablet.gameObject);
                if (grant != null)
                {
                    var altar = grant.Object;
                    grant.Child = recipe;
                    grant.Object = obj;
                    grant.Claim.Selection = NativeJoiningSupplySelection.Capture(obj, grant.Owner, recipe);
                    NetworkServer.Destroy(altar);
                    dirty = true;
                }
                return;
            }
            int? recipient = owner?.identity?.GetComponent<PlayerSpawner>()?.currentPlayerIdxForSave;
            ledger.Record(recipe, recipient);
            dirty = true;
            if (recipient.HasValue)
            {
                var claim = ledger.Find(recipient.Value)?.Claims.Find(item => item.Opportunity.Id == recipe.Id);
                if (claim != null)
                {
                    if (!claim.AcceptNativeDelivery(grants.TryGetValue(recipient.Value, out var active) && active.Claim == claim))
                    {
                        NetworkServer.Destroy(obj);
                        return;
                    }
                }
            }
            available[recipe.Id] = obj;
            dirty = true;
        }

        internal bool DeferLevelReward(LevelController controller, int seed)
        {
            if (controller != AdvancingLevel) return false;
            var participant = ledger.Find(controller.GetComponent<PlayerSpawner>().currentPlayerIdxForSave);
            int index = participant.Claims.FindLastIndex(claim => claim.Opportunity.Kind == JoiningSupplyKind.LevelReward) + 1;
            participant.Claims.Insert(index, new JoiningSupplyClaim { Opportunity = new JoiningSupplyOpportunity
            {
                Id = "level/" + controller.currentLevel, Kind = JoiningSupplyKind.LevelReward, Seed = seed
            } });
            dirty = true;
            return true;
        }

        internal void ObservedEmbeddedReward(NetworkBehaviour source, NetworkIdentity recipient)
        {
            if (!NetworkServer.active || !MidRunAdmissionRuntime.IsAvailable || source == null || recipient == null ||
                source.GetComponent<NetworkIdentity>() != null) return;
            // Some fixed-map facilities are behaviours on the floor's network identity.
            // Recreate only the registered standalone facility, never the containing floor.
            var prefabs = NetworkClient.prefabs.Where(pair => pair.Value != null && pair.Value.GetComponent(source.GetType()) != null).ToArray();
            if (prefabs.Length != 1) return;
            var recipe = NativeJoiningSupplyRecipes.Read(source.gameObject, null, prefabs[0].Key);
            if (recipe == null) return;
            BindRun();
            if (loadFailed) return;
            ledger.Record(recipe, recipient.GetComponent<PlayerSpawner>()?.currentPlayerIdxForSave);
            available[recipe.Id] = source.gameObject;
            dirty = true;
        }

        private void Update()
        {
            if (!FeatureFailure.IsAvailable(FeatureId.MultiplayerAccess))
            {
                return;
            }

            try
            {
                UpdateCore();
            }
            catch (System.Exception exception)
            {
                FeatureFailure.Disable(FeatureId.MultiplayerAccess, exception);
                return;
            }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private void UpdateCore()
        {
            JoiningSupplyBridge.Tick();
            if (!NetworkServer.active || !MidRunAdmissionRuntime.IsAvailable)
                return;
            BindRun();
            if (Time.unscaledTime < nextPoll)
                return;
            nextPoll = Time.unscaledTime + 0.2f;
            if (loadFailed)
            {
                JoiningSupplyBridge.Publish();
                return;
            }

            foreach (var participant in ledger.Participants)
            {
                if (!initialized.TryGetValue(participant.Slot, out var player) || player == null)
                    continue;
                var avatar = player.GetComponent<PlayerAvatar>();
                if (!participant.Failed && !participant.BasicsComplete && !avatar.IsDead && NativeJoiningSupplyRecipes.Loaded(avatar))
                {
                    try
                    {
                        if (!participant.MoneyGranted)
                        {
                            if (participant.MoneyGrant > 0)
                                avatar.AddMoney(participant.MoneyGrant);
                            participant.MoneyGranted = true;
                        }

                        var level = player.GetComponent<LevelController>();
                        if (level.currentLevel < participant.TargetLevel)
                        {
                            AdvancingLevel = level;
                            try
                            {
                                level.NetworkcurrentLevel = level.currentLevel + 1;
                                level.NetworkcurrentExp = Math.Max(level.currentExp, LevelController.ExpTableByLevel[level.currentLevel - 1]);
                                level.LevelUpOnServer();
                            }
                            finally
                            {
                                AdvancingLevel = null;
                            }
                        }
                        else
                            participant.BasicsComplete = true;
                    }
                    catch (Exception error)
                    {
                        participant.Failed = true;
                        SupportLogger.Record("joining_growth_failed", error.GetType().Name + ": " + error.Message, "ERROR");
                    }

                    dirty = true;
                }
            }

            ReconcileGrants();
            JoiningSupplyBridge.Publish();
        }

        private void ReconcileGrants()
        {
            foreach (var pair in grants.ToArray())
            {
                var grant = pair.Value;
                if (grant.Owner == null || grant.Object == null) { Suspend(pair.Key); continue; }
                if (NativeJoiningSupplyRecipes.Acquired(grant.Object, grant.Owner))
                { grant.Claim.Completed = true; Suspend(pair.Key); }
            }
        }

        internal int Remaining(PlayerSpawner player) => player == null ? 0 : ledger.Find(player.currentPlayerIdxForSave)?.Remaining ?? 0;
        internal bool Prepared(PlayerSpawner player) => player != null && (ledger.Find(player.currentPlayerIdxForSave)?.BasicsComplete ?? false);
        internal bool RecordedFromStart(PlayerSpawner player) => player != null && (ledger.Find(player.currentPlayerIdxForSave)?.RecordedFromExplorationStart ?? false);
        internal bool HasFailure(PlayerSpawner player) => loadFailed || player != null && ledger.Find(player.currentPlayerIdxForSave) is JoiningSupplyParticipant participant &&
            (participant.Failed || participant.Claims.Any(claim => claim.Failed));
        internal uint CurrentObject(PlayerSpawner player) => player != null && grants.TryGetValue(player.currentPlayerIdxForSave, out var grant) &&
            grant.Object != null ? grant.Object.GetComponent<NetworkIdentity>().netId : 0;
        internal string ClientSelection(PlayerSpawner player) => player != null && grants.TryGetValue(player.currentPlayerIdxForSave, out var grant) ?
            grant.Claim.ClientSelection : "";
        internal int SelectionRevision(PlayerSpawner player) => player != null && grants.TryGetValue(player.currentPlayerIdxForSave, out var grant) ?
            grant.Claim.SelectionRevision : 0;

        internal bool SelectFacility(PlayerSpawner player, int revision, int selectionRevision, uint objectId,
            int weapon, ItemPosition position, int instanceId)
        {
            if (revision != Revision || player == null || !grants.TryGetValue(player.currentPlayerIdxForSave, out var grant) ||
                grant.Claim.Failed ||
                grant.Object == null || CurrentObject(player) != objectId || grant.Claim.SelectionRevision != selectionRevision ||
                !Owns(grant.Object, player.connectionToClient)) return false;
            bool changed;
            try
            {
                if (grant.Object.TryGetComponent<Anvil>(out var anvil))
                    changed = NativeJoiningSupplyFacility.Enhance(anvil, player, weapon, grant.Claim.ClientSelection);
                else if (grant.Object.TryGetComponent<AltarOfEnchant>(out var altar))
                    changed = NativeJoiningSupplyFacility.Enchant(altar, player, position, instanceId);
                else return false;
            }
            catch (Exception error) { Fail(grant.Claim, error); return false; }
            if (!changed) return false;
            grant.Claim.SelectionRevision++;
            dirty = true;
            ReconcileGrants();
            return true;
        }

        internal void SaveAnvil(PlayerSpawner player, int revision, uint objectId, string selection)
        {
            if (revision != Revision || player == null || string.IsNullOrEmpty(selection) || selection.Length > 16384 ||
                !grants.TryGetValue(player.currentPlayerIdxForSave, out var grant) || grant.Object == null ||
                CurrentObject(player) != objectId || grant.Object.GetComponent<Anvil>() == null) return;
            NativeJoiningSupplyAnvil decoded;
            try { decoded = JsonUtility.FromJson<NativeJoiningSupplyAnvil>(selection); }
            catch (ArgumentException) { return; }
            if (decoded == null || decoded.Choices == null || decoded.Candidates == null || decoded.Rerolls < 0 ||
                decoded.Choices.Concat(decoded.Candidates).Any(id => WeaponDatabase.FindWeaponById(id) == null)) return;
            grant.Claim.ClientSelection = selection;
            dirty = true;
        }

        internal bool SelectMiracle(PlayerSpawner player, int revision, uint objectId, string id, int instanceId)
        {
            if (revision != Revision || player == null || !grants.TryGetValue(player.currentPlayerIdxForSave, out var grant) ||
                grant.Claim.Failed ||
                grant.Object == null || CurrentObject(player) != objectId ||
                !Owns(grant.Object, player.connectionToClient) || !grant.Object.TryGetComponent<MiracleSelector2>(out var selector)) return false;
            try { if (!NativeJoiningSupplyMiracle.Grant(selector, player, id, instanceId)) return false; }
            catch (Exception error) { Fail(grant.Claim, error); return false; }
            grant.Claim.Completed = true;
            Suspend(player.currentPlayerIdxForSave);
            return true;
        }

        internal bool Request(PlayerSpawner player, int revision)
        {
            if (!MidRunAdmissionRuntime.IsAvailable || revision != Revision || !Prepared(player) || HasFailure(player) ||
                !NativeJoiningSupplyRecipes.Ready(player.GetComponent<PlayerAvatar>())) return false;
            int slot = player.currentPlayerIdxForSave;
            if (grants.TryGetValue(slot, out var existing))
            {
                Suspend(slot);
            }
            var claim = ledger.Find(slot)?.Next;
            if (claim == null) return false;
            var avatar = player.GetComponent<PlayerAvatar>();
            try
            {
                switch (claim.Opportunity.Kind)
                {
                    case JoiningSupplyKind.InventorySpace:
                        avatar.AddOrphanedStatusInstance(StatusDatabase.CreateStatusEntity("INVENTORY_SLOT", claim.Opportunity.Amount));
                        claim.Completed = true; dirty = true; return true;
                    case JoiningSupplyKind.MaximumHealth:
                        avatar.AddOrphanedStatusInstance(StatusDatabase.CreateStatusEntity("MAX_HP_NORATIO", claim.Opportunity.Amount));
                        avatar.Heal(claim.Opportunity.Amount, false, true);
                        claim.Completed = true; dirty = true; return true;
                    case JoiningSupplyKind.RerollDice:
                        avatar.AddDice(claim.Opportunity.Amount);
                        claim.Completed = true; dirty = true; return true;
                }
            }
            catch (Exception error) { Fail(claim, error); return false; }
            if (!NativeJoiningSupplyRecipes.TryFindPosition(avatar, out var position)) return false;
            creating = true;
            GameObject obj = null;
            try
            {
                var state = string.IsNullOrEmpty(claim.Selection) ? null : JsonUtility.FromJson<NativeJoiningSupplySelection>(claim.Selection);
                obj = NativeJoiningSupplyRecipes.Create(state?.Child ?? claim.Opportunity, avatar, position);
                state?.Restore(obj, player);
                if (player.isLocalPlayer && obj.TryGetComponent<Anvil>(out var anvil) && !string.IsNullOrEmpty(claim.ClientSelection))
                    JsonUtility.FromJson<NativeJoiningSupplyAnvil>(claim.ClientSelection).Restore(anvil);
                NetworkServer.Spawn(obj, player.connectionToClient);
                grants.Add(slot, new Grant { Owner = player, Claim = claim, Object = obj, Child = state?.Child });
                dirty = true;
                return true;
            }
            catch (Exception error)
            {
                if (obj != null) NetworkServer.Destroy(obj);
                Fail(claim, error);
                return false;
            }
            finally { creating = false; }
        }

        internal bool Owns(GameObject obj, NetworkConnectionToClient connection)
        {
            var grant = grants.Values.FirstOrDefault(item => item.Object == obj);
            return grant == null || !grant.Claim.Failed && grant.Owner != null && grant.Owner.connectionToClient == connection &&
                NativeJoiningSupplyRecipes.Ready(grant.Owner.GetComponent<PlayerAvatar>());
        }

        internal bool Finish(PlayerAvatar player, Sephirite reward)
        {
            var grant = grants.Values.FirstOrDefault(item => item.Object == reward.gameObject);
            if (grant == null) return false;
            if (grant.Owner != player.GetComponent<PlayerSpawner>()) return true;
            grant.Claim.Completed = true;
            reward.AcquireAndDestroy();
            CloseReward.Invoke(player, null);
            grants.Remove(grant.Owner.currentPlayerIdxForSave);
            dirty = true;
            return true;
        }

        internal void Disconnect(NetworkConnectionToClient connection)
        {
            ReconcileGrants();
            foreach (var pair in grants.Where(pair => pair.Value.Owner?.connectionToClient == connection).ToArray()) Suspend(pair.Key);
            foreach (var pair in initialized.Where(pair => pair.Value?.connectionToClient == connection).ToArray()) initialized.Remove(pair.Key);
            Flush();
        }

        internal void Stopped()
        {
            run = null;
            ledger = new JoiningSupplyLedger();
            initialized.Clear(); available.Clear(); grants.Clear();
            AdvancingLevel = null; SelectingTablet = null; SpawningFacilityDice = false;
        }

        internal void Despawning(GameObject obj)
        {
            var grant = grants.Values.FirstOrDefault(item => item.Object == obj);
            if (grant == null || grant.Owner == null) return;
            if (NativeJoiningSupplyRecipes.Acquired(obj, grant.Owner)) grant.Claim.Completed = true;
            else grant.Claim.Selection = NativeJoiningSupplySelection.Capture(obj, grant.Owner, grant.Child);
            grants.Remove(grant.Owner.currentPlayerIdxForSave);
            dirty = true;
        }

        private void Suspend(int slot)
        {
            if (!grants.TryGetValue(slot, out var grant)) return;
            if (grant.Object != null && grant.Owner != null && !grant.Claim.Completed)
                grant.Claim.Selection = NativeJoiningSupplySelection.Capture(grant.Object, grant.Owner, grant.Child);
            grants.Remove(slot);
            if (grant.Object != null && NetworkServer.active) NetworkServer.Destroy(grant.Object);
            dirty = true;
        }

        private void SuspendAll() { foreach (int slot in grants.Keys.ToArray()) Suspend(slot); }

        private void Fail(JoiningSupplyClaim claim, Exception error)
        {
            claim.Failed = true;
            dirty = true;
            SupportLogger.Record("joining_supply_failed", error.GetType().Name + ": " + error.Message, "ERROR");
        }

        internal void Flush()
        {
            if (!NetworkServer.active || loadFailed || run == null || !ReferenceEquals(run, SaveManager.CurrentRun)) return;
            ReconcileGrants();
            foreach (var grant in grants.Values)
            {
                grant.Claim.Selection = NativeJoiningSupplySelection.Capture(grant.Object, grant.Owner, grant.Child);
                dirty = true;
            }
            if (!dirty) return;
            run.SetString(SaveKey, JsonUtility.ToJson(ledger));
            dirty = false;
        }
    }
}
