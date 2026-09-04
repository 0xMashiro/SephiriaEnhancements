using System;
using System.Collections.Generic;
using SephiriaEnhancements.Configuration;
using SephiriaEnhancements.Diagnostics;
using SephiriaEnhancements.Integration;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SephiriaEnhancements.CombatTargeting
{
    internal sealed class CombatTargetingController : MonoBehaviour
    {
        private const float SearchDistance = 10f;
        private const float MouseAssistRadiusPixels = 96f;
        private const float TargetRefreshInterval = 0.1f;
        private static CombatTargetingController current;
        private readonly List<RaycastHit2D> sightHits = new List<RaycastHit2D>(32);
        private readonly List<UnitAvatar> candidates = new List<UnitAvatar>(16);
        private readonly TargetSelection<UnitAvatar> selection = new TargetSelection<UnitAvatar>();
        private readonly TargetSwitchGesture switchGesture = new TargetSwitchGesture();
        private readonly TargetLockFeedbackView feedback = new TargetLockFeedbackView();
        private PlayerAvatar player;
        private WeaponControllerSimple weapon;
        private UnitAvatar ownedTarget;
        private InputAction abilityAction;
        private Vector2 lastDirection = Vector2.right;
        private string controlScheme;
        private float nextRefreshAt;
        private int inputFrame = -1;
        private int keyboardActionFrame = -1;
        private bool keyboardCombatActive;
        private bool aimApplied;
        private bool runtimeCompatible = true;

        internal static bool HidesPointer => current != null && current.aimApplied &&
            current.keyboardCombatActive && Application.isFocused;

        private void Awake() => current = this;

        private void LateUpdate()
        {
            if (ownedTarget != null) feedback.Show(ownedTarget, GameCamera.Instance?.Camera, selection.IsManual, false);
            else feedback.Hide();
        }

        internal static void UpdateInput(PlayerInputController input)
        {
            if (current == null) return;
            try { current.RefreshAim(input); }
            catch (Exception ex) { current.DisableAfterFailure(ex); }
        }

        internal static void PrepareCast(IntegratedActionController source, int slot,
            ref Vector3 aimPosition, ref UnitAvatar aimTarget)
        {
            if (current == null || source == null) return;
            try
            {
                PlayerInputController input = PlayerInputController.Instance;
                if (!current.TryBind(input) || source.gameObject != current.player.gameObject) return;
                bool mouseAction = false;
                if (current.controlScheme == PlayerInputController.KeyboardAndMouseScheme)
                {
                    InputAction action = NativeTargetingBridge.FindAction(input.playerInput.actions, slot);
                    if (action?.activeControl?.device is Mouse)
                    {
                        mouseAction = true;
                        current.YieldToMouse(input);
                    }
                    else if (action?.activeControl?.device is Keyboard && action.IsPressed() &&
                        CombatTargetingSettings.TargetingMode != TargetingMode.Disabled)
                    {
                        current.keyboardCombatActive = true;
                        current.keyboardActionFrame = Time.frameCount;
                        current.abilityAction = NativeTargetingBridge.IsAbility(source, slot) ? action : null;
                    }
                    else return;
                }
                current.RefreshAim(input);
                if (mouseAction || current.keyboardCombatActive || current.selection.IsManual)
                {
                    aimPosition = current.player.NetworkaimObject.transform.position;
                    aimTarget = input.autoAimedTarget;
                }
            }
            catch (Exception ex) { current.DisableAfterFailure(ex); }
        }

        internal static void PrepareRelease(IntegratedActionController source)
        {
            if (current?.player != null && source != null && source.gameObject == current.player.gameObject)
                UpdateInput(PlayerInputController.Instance);
        }

        internal void ResetGameplayContext()
        {
            ClearControl(PlayerInputController.Instance, preserveInputMode: true);
            runtimeCompatible = true;
        }

        private void OnDisable() => ClearControl(PlayerInputController.Instance);

        private void OnDestroy()
        {
            ClearControl(PlayerInputController.Instance);
            feedback.Dispose();
            if (current == this) current = null;
        }

        private void DisableAfterFailure(Exception ex)
        {
            ClearControl(PlayerInputController.Instance);
            runtimeCompatible = false;
            SupportLogger.Warning("combat_targeting_failed",
                "[SephiriaEnhancements] Combat targeting disabled for the current gameplay context: " + ex);
        }

        private bool TryBind(PlayerInputController input)
        {
            PlayerAvatar local = LocalPlayerResolver.Resolve();
            if (!runtimeCompatible || !isActiveAndEnabled || !EnhancementsSettings.Enabled ||
                input == null || !input.isActiveAndEnabled || input.playerInput == null ||
                local == null || local.IsDead || !local.gameObject.activeInHierarchy ||
                local.NetworkaimObject == null || UIManager.Instance == null)
            {
                ClearControl(input);
                return false;
            }
            string scheme = input.playerInput.currentControlScheme;
            if (scheme != PlayerInputController.KeyboardAndMouseScheme && scheme != PlayerInputController.GamepadScheme)
            {
                ClearControl(input);
                return false;
            }
            if (player != local || controlScheme != scheme)
            {
                ClearControl(input);
                player = local;
                weapon = player.GetComponent<WeaponControllerSimple>();
                controlScheme = scheme;
                Vector2 facing = player.CurrentLookingPosition - (Vector2)player.transform.position;
                lastDirection = facing.sqrMagnitude > 0.0001f ? facing.normalized : Vector2.right;
            }
            if (!Application.isFocused || local.loadingScreenType != -1 || input.BlockAvatarInput ||
                UIManager.Instance.CurrentControlStack != null || !NativeTargetingBridge.IsReady(input))
            {
                ClearControl(input, preserveInputMode: true);
                // UI pointer input also chooses how combat resumes after closing the panel.
                if (Application.isFocused && (InputDeviceState.HasPointerMoved() ||
                    InputDeviceState.HasPointerAction())) keyboardCombatActive = false;
                return false;
            }
            NativeControlCoordinator.PreparePlayerInput(input);
            return weapon != null;
        }

        private void RefreshAim(PlayerInputController input)
        {
            if (!TryBind(input)) return;
            bool keyboardScheme = controlScheme == PlayerInputController.KeyboardAndMouseScheme;
            if (CombatTargetingSettings.TargetingMode == TargetingMode.Disabled)
            {
                ClearControl(input);
            }
            else
            {
                Vector2 movement = NativeTargetingBridge.ReadMovement(input);

                if (inputFrame != Time.frameCount)
                {
                    inputFrame = Time.frameCount;
                    InputAction action = NativeInputActions.FindShortcut(input.playerInput.actions, ModShortcuts.SwitchLockedTarget);
                    bool pressed = action?.WasPressedThisFrame() ?? false;
                    if (keyboardScheme && pressed && action.activeControl?.device is Keyboard)
                    {
                        keyboardCombatActive = true;
                        keyboardActionFrame = Time.frameCount;
                    }
                    else if (keyboardScheme && pressed && action.activeControl?.device is Mouse)
                        keyboardCombatActive = false;
                    TargetSwitchCommand command = switchGesture.Update(pressed,
                        action?.IsPressed() ?? false, action?.WasReleasedThisFrame() ?? false, Time.unscaledTime);
                    if (command != TargetSwitchCommand.None)
                    {
                        RefreshCandidates(force: true, allowAutomatic: keyboardScheme && keyboardCombatActive &&
                            selection.Target != null && PrefersTarget());
                        if (command == TargetSwitchCommand.Switch)
                        {
                            selection.Switch(input.autoAimedTarget);
                        }
                        else selection.Unlock();
                    }
                    else if (keyboardScheme && keyboardActionFrame != Time.frameCount &&
                        !switchGesture.IsPending &&
                        (InputDeviceState.HasPointerMoved() || InputDeviceState.HasPointerAction())) YieldToMouse(input);
                }

                if (keyboardScheme && (keyboardCombatActive || selection.IsManual))
                {
                    RefreshCandidates(force: false, allowAutomatic: keyboardCombatActive &&
                        (!switchGesture.IsPending || selection.Target != null) && PrefersTarget());
                    if (selection.Target == null && movement.sqrMagnitude > 0.16f)
                        lastDirection = movement.normalized;
                    aimApplied = keyboardCombatActive;
                    if (keyboardCombatActive) NativeTargetingBridge.ClearPointerHover(input);
                    ApplyAim(input, selection.Target, selection.Target != null
                        ? (Vector2)selection.Target.transform.position
                        : (Vector2)player.transform.position + lastDirection * PlayerInputController.MaxAimDistance);
                    return;
                }
                if (!keyboardScheme && selection.IsManual)
                {
                    RefreshCandidates(force: false, allowAutomatic: false);
                    if (selection.IsManual)
                    {
                        ApplyAim(input, selection.Target, selection.Target.transform.position);
                        return;
                    }
                }
            }

            ClearOwnedTarget(input);
            if (keyboardScheme && CombatTargetingSettings.MouseAimAssistEnabled &&
                (player.activeMagicCastModeClientside || weapon.HasRangedWeapon())) ApplyMouseAssist(input);
        }

        private bool PrefersTarget() => player.activeMagicCastModeClientside || weapon.HasRangedWeapon() ||
            (abilityAction != null && (abilityAction.IsPressed() || abilityAction.WasReleasedThisFrame()));

        private void RefreshCandidates(bool force, bool allowAutomatic)
        {
            bool invalid = !ReferenceEquals(selection.Target, null) && !IsSelectableTarget(selection.Target);
            bool acquiring = allowAutomatic && ReferenceEquals(selection.Target, null) && candidates.Count > 0;
            if (force || invalid || acquiring || Time.unscaledTime >= nextRefreshAt)
            {
                nextRefreshAt = Time.unscaledTime + TargetRefreshInterval;
                candidates.Clear();
                List<UnitAvatar> creatures = CombatManager.Instance?.AllCreatures;
                if (creatures != null)
                {
                    foreach (UnitAvatar candidate in creatures)
                    {
                        if (IsSelectableTarget(candidate)) candidates.Add(candidate);
                    }
                    candidates.Sort(CompareTargets);
                }
            }
            selection.Refresh(candidates, allowAutomatic);
        }

        private int CompareTargets(UnitAvatar left, UnitAvatar right)
        {
            Vector2 origin = player.transform.position;
            int distance = ((Vector2)left.transform.position - origin).sqrMagnitude.CompareTo(
                ((Vector2)right.transform.position - origin).sqrMagnitude);
            return distance != 0 ? distance : left.GetInstanceID().CompareTo(right.GetInstanceID());
        }

        private bool IsSelectableTarget(UnitAvatar candidate) => IsEligibleTarget(candidate) &&
            IsOnScreen(GameCamera.Instance?.Camera, candidate.transform.position) &&
            ((Vector2)candidate.transform.position - (Vector2)player.transform.position).sqrMagnitude <= SearchDistance * SearchDistance &&
            HasClearShot(player.transform.position, candidate.transform.position);

        private bool IsEligibleTarget(UnitAvatar candidate)
        {
            if (candidate == null || candidate == player || candidate.IsDead || candidate.canBeTarget <= 0 ||
                !candidate.gameObject.activeInHierarchy || RuntimeFactionManager.Instance == null) return false;
            return (player.GetHostileFactionLayers(EDamageFromType.None) &
                RuntimeFactionManager.Instance.FindFactionLayer(candidate.faction)) != 0;
        }

        private void ApplyMouseAssist(PlayerInputController input)
        {
            Camera camera = GameCamera.Instance?.Camera;
            if (camera == null || !InputDeviceState.TryGetPointerPosition(out Vector2 pointer)) return;
            List<UnitAvatar> creatures = CombatManager.Instance?.AllCreatures;
            if (creatures == null) return;
            float bestDistance = MouseAssistRadiusPixels * MouseAssistRadiusPixels;
            UnitAvatar best = null;
            foreach (UnitAvatar candidate in creatures)
            {
                if (!IsEligibleTarget(candidate) || !IsOnScreen(camera, candidate.transform.position)) continue;
                float distance = ((Vector2)camera.WorldToScreenPoint(candidate.transform.position) - pointer).sqrMagnitude;
                if (distance >= bestDistance || !HasClearShot(player.transform.position, candidate.transform.position)) continue;
                best = candidate;
                bestDistance = distance;
            }
            if (best != null) ApplyAim(input, best, best.transform.position);
        }

        private void ApplyAim(PlayerInputController input, UnitAvatar target, Vector2 position)
        {
            ownedTarget = target;
            input.autoAimedTarget = target;
            player.autoAimedTarget = target;
            // Feedback must not change the native cursor's AutoAiming switch.
            UIManager.Instance.GetElement<UI_TargetingCursor>()?.SetTarget(null);
            NativeTargetingBridge.ApplyAim(input, player, weapon, position);
            Vector2 direction = position - (Vector2)player.transform.position;
            if (direction.sqrMagnitude > 0.0001f) lastDirection = direction.normalized;
            if (target == null) feedback.Hide();
        }

        private bool HasClearShot(Vector2 from, Vector2 to)
        {
            int mask = CombatManager.TileLayerMask | CombatManager.BlockableLayerMask;
            if (mask == 0) return false;
            ContactFilter2D filter = new ContactFilter2D();
            filter.SetLayerMask(mask);
            filter.useTriggers = true;
            sightHits.Clear();
            Physics2D.Linecast(from, to, filter, sightHits);
            foreach (RaycastHit2D hit in sightHits)
            {
                Collider2D collider = hit.collider;
                if (collider != null && collider.GetComponentInParent<UnitAvatar>() == null &&
                    collider.GetComponentInParent<ProjectileBase>() == null) return false;
            }
            return true;
        }

        private void YieldToMouse(PlayerInputController input)
        {
            ClearControl(input);
            NativeTargetingBridge.RestoreMouseAim(input, player, weapon);
            inputFrame = Time.frameCount;
        }

        private void ClearControl(PlayerInputController input, bool preserveInputMode = false)
        {
            ClearOwnedTarget(input);
            selection.Clear();
            candidates.Clear();
            abilityAction = null;
            if (!preserveInputMode) keyboardCombatActive = false;
            aimApplied = false;
            nextRefreshAt = 0f;
            switchGesture.Clear();
            inputFrame = -1;
            keyboardActionFrame = -1;
        }

        private void ClearOwnedTarget(PlayerInputController input)
        {
            if (ownedTarget != null)
            {
                if (input != null && input.autoAimedTarget == ownedTarget) input.autoAimedTarget = null;
                if (player != null && player.autoAimedTarget == ownedTarget) player.autoAimedTarget = null;
            }
            ownedTarget = null;
            feedback.Hide();
        }

        private static bool IsOnScreen(Camera camera, Vector3 position)
        {
            if (camera == null) return false;
            Vector3 point = camera.WorldToViewportPoint(position);
            return point.z > 0f && point.x >= 0f && point.x <= 1f && point.y >= 0f && point.y <= 1f;
        }
    }
}
