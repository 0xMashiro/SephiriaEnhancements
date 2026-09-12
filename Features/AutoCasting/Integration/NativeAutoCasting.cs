using SephiriaEnhancements.Runtime;
using System;
using System.Collections.Generic;
using HarmonyLib;
using Mirror;
using SephiriaEnhancements.Configuration;
using SephiriaEnhancements.Integration;
using UnityEngine;

namespace SephiriaEnhancements.AutoCasting.Integration
{
    internal sealed class NativeAutoCasting : MonoBehaviour
    {
        private static readonly Func<PlayerInputController, Vector2> AimedPosition =
            AccessTools.MethodDelegate<Func<PlayerInputController, Vector2>>(
                AccessTools.Method(typeof(PlayerInputController), "GetAimedPosition"));
        private static readonly Func<PlayerInputController, bool> InputReady =
            AccessTools.MethodDelegate<Func<PlayerInputController, bool>>(
                AccessTools.Method(typeof(PlayerInputController), "ValidateScreenFader_PlayerMove"));
        internal static NativeAutoCasting Current { get; private set; }
        internal static bool IsRequestingCast(IntegratedActionController source) =>
            Current != null && Current.requesting && Current.actions == source;
        private readonly AutoCastingSelection selection = new AutoCastingSelection();
        private readonly AutoCastingRotation rotation = new AutoCastingRotation();
        private readonly List<int> ownedArtifacts = new List<int>();
        private PlayerAvatar player;
        private IntegratedActionController actions;
        private SkillController skills;
        private WeaponControllerSimple weapon;
        private bool requesting;
        private bool retryInProgress, retryWorldPending;
        private NetworkConnectionToServer retryConnection;
        internal bool IsPaused => rotation.IsPaused;
        internal string SelectionStateKey(Charm_Magic magic) => !IsSelected(magic) ? AutoCastingLocalization.Off :
            IsPaused ? AutoCastingLocalization.Paused : AutoCastingLocalization.On;

        private void Awake()
        {
            if (!FeatureFailure.IsAvailable(FeatureId.AutoCasting))
            {
                return;
            }

            try
            {
                AwakeCore();
            }
            catch (System.Exception exception)
            {
                FeatureFailure.Disable(FeatureId.AutoCasting, exception);
                return;
            }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private void AwakeCore() => Current = this;
        private void OnDestroy() { if (Current == this) Current = null; }

        internal void ResetWorld()
        {
            retryInProgress = retryWorldPending = false;
            retryConnection = null;
            selection.Clear();
            rotation.Reset();
        }

        internal void BeginRetry()
        {
            BindPlayer();
            if (player == null) return;
            retryConnection = NetworkClient.connection;
            retryInProgress = retryWorldPending = true;
        }

        internal void ObserveWorldSession(bool saved)
        {
            if (saved && retryInProgress && retryWorldPending &&
                retryConnection == NetworkClient.connection && player != null &&
                LocalPlayerResolver.Resolve() == player)
            {
                retryWorldPending = false;
                return;
            }
            ResetWorld();
        }

        internal void CompleteRetry()
        {
            retryInProgress = retryWorldPending = false;
            retryConnection = null;
            ResetGameplayContext();
        }

        internal void CancelRetry() { if (retryInProgress) ResetWorld(); }

        internal void ResetGameplayContext() => rotation.YieldToManualInput(Time.unscaledTimeAsDouble);

        private void BindPlayer()
        {
            PlayerAvatar local = LocalPlayerResolver.Resolve();
            if (player == local) return;
            ResetWorld();
            player = local;
            actions = player != null ? player.GetComponent<IntegratedActionController>() : null;
            skills = player != null ? player.GetComponent<SkillController>() : null;
            weapon = player != null ? player.GetComponent<WeaponControllerSimple>() : null;
        }

        // The native API numbers weapon slots 100-102, separate from slots 0-7.
        internal static int NativeSlot(int index) => index < 8 ? index : 100 + index - 8;
        internal Charm_Magic MagicAt(int index)
        {
            BindPlayer();
            if (actions == null || index < 0 || index >= 11) return null;
            QuickSlotData slot = index < 8 ? actions.quickSlots[index] : actions.quickSlotsWeapon[index - 8];
            return slot != null && slot.Type == QuickSlotType.Magic ? slot.magic : null;
        }

        internal bool CanSelect(Charm_Magic magic)
        {
            BindPlayer();
            return player != null && magic != null && magic.NetworkAvatar == player &&
                magic.Item != null && magic.ContainedMagic != null &&
                magic.ContainedMagic.castingType == ECastingType.SingleShot && HasBinding(magic);
        }

        private bool HasBinding(Charm_Magic magic)
        {
            if (actions == null) return false;
            foreach (QuickSlotData slot in actions.quickSlots)
                if (slot != null && slot.Type == QuickSlotType.Magic && slot.magic == magic) return true;
            foreach (QuickSlotData slot in actions.quickSlotsWeapon)
                if (slot != null && slot.Type == QuickSlotType.Magic && slot.magic == magic) return true;
            return false;
        }

        internal bool IsSelected(Charm_Magic magic) =>
            CanSelect(magic) && selection.Contains(magic.Item.InstanceID);

        internal bool Toggle(Charm_Magic magic)
        {
            if (!CanSelect(magic)) return false;
            selection.Toggle(magic.Item.InstanceID);
            rotation.YieldToManualInput(Time.unscaledTimeAsDouble);
            return true;
        }

        internal void ObserveManualCast(IntegratedActionController source)
        {
            if (!requesting && source == actions)
                rotation.YieldToManualInput(Time.unscaledTimeAsDouble);
        }

        private void LateUpdate()
        {
            if (!FeatureFailure.IsAvailable(FeatureId.AutoCasting))
            {
                return;
            }

            try
            {
                LateUpdateCore();
            }
            catch (System.Exception exception)
            {
                FeatureFailure.Disable(FeatureId.AutoCasting, exception);
                return;
            }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private void LateUpdateCore()
        {
            BindPlayer();
            if (player == null || actions == null || skills == null || weapon == null)
                return;
            // Rebuilt inventory objects arrive over multiple frames. Keep artifact IDs
            // and pause state until the local retry arrival has been confirmed.
            if (retryInProgress || player.loadingScreenType != -1) return;
            ownedArtifacts.Clear();
            foreach (Charm_Basic charm in player.Inventory.charms.Values)
                if (charm is Charm_Magic magic && magic.Item != null)
                    ownedArtifacts.Add(magic.Item.InstanceID);
            selection.Retain(ownedArtifacts);
            PlayerInputController input = PlayerInputController.Instance;
            if (!EnhancementsSettings.Enabled || input == null || !input.isActiveAndEnabled || input.CombatBehaviour != player || player.loadingScreenType != -1 || player.NetworkaimObject == null || input.BlockAvatarInput || !InputReady(input) || !Application.isFocused || UIManager.Instance == null || UIManager.Instance.CurrentControlStack != null || DungeonManager.Instance == null || !DungeonManager.Instance.isRunStarted || player.IsDead || Time.timeScale <= 0f)
                return;
            if (NativeInputActions.WasPressed(input.playerInput?.actions, ModShortcuts.ToggleAutoCastingPause))
            {
                bool hasSelection = false;
                for (int slot = 0; slot < 11; slot++)
                    if (IsSelected(MagicAt(slot)))
                    {
                        hasSelection = true;
                        break;
                    }

                string message = AutoCastingLocalization.SelectFirst;
                if (hasSelection)
                {
                    rotation.TogglePause();
                    message = IsPaused ? AutoCastingLocalization.PausedMessage : AutoCastingLocalization.ResumedMessage;
                }

                SephiriaEnhancements.Integration.NativeModNotifications.Short(message);
                // A toggle never sends a cast in the same frame.
                return;
            }

            if (IsPaused || !player.CanMove || !skills.CanCast || skills.IsInGlobalCooldown || weapon.IsCastAnimationRunning || !weapon.DoCastValidation() || player.GetCustomStatUnsafe("BLOCKCASTMAGIC") > 0)
                return;
            // Space requests by the native global interval and the measured round trip.
            // Failed native requests may be silent; no requests are queued for later execution.
            double interval = Math.Max(0.35, NetworkTime.rtt * 2);
            int index = rotation.Take(Time.unscaledTimeAsDouble, 11, CanRequest, interval);
            if (index < 0 || !NativeAutoCastingCombat.IsActive(player))
                return;
            Vector3 aimPosition = AimedPosition(input);
            UnitAvatar aimTarget = input.autoAimedTarget;
            if (!CombatTargeting.CombatTargetingController.TryPrepareAutomaticCast(actions, ref aimPosition, ref aimTarget))
                return;
            requesting = true;
            try
            {
                actions.Cast(NativeSlot(index), aimPosition, aimTarget);
            }
            finally
            {
                requesting = false;
            }
        }

        private bool CanRequest(int index)
        {
            Charm_Magic magic = MagicAt(index);
            return IsSelected(magic) && magic.CanCast(player, useCooldown: true, useMp: true) ==
                ECanUseSkillResult.Succeeded;
        }
    }

    [HarmonyPatch(typeof(IntegratedActionController), nameof(IntegratedActionController.Cast))]
    internal static class AutoCastingManualInputPatch
    {
        private static void Prefix(IntegratedActionController __instance)
        {
            if (!FeatureFailure.IsAvailable(FeatureId.AutoCasting))
            {
                return;
            }

            try
            {
                PrefixCore(__instance);
            }
            catch (System.Exception exception)
            {
                FeatureFailure.Disable(FeatureId.AutoCasting, exception);
                return;
            }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static void PrefixCore(IntegratedActionController __instance) => NativeAutoCasting.Current?.ObserveManualCast(__instance);
    }
}
