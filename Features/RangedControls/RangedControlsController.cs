using SephiriaEnhancements.Diagnostics;
using System;
using System.Collections.Generic;
using SephiriaEnhancements.Configuration;
using SephiriaEnhancements.Integration;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SephiriaEnhancements.RangedControls
{
    internal sealed class RangedControlsController : MonoBehaviour
    {
        private const float AimDistance = PlayerInputController.MaxAimDistance;
        private const float DirectionalAssistDistance = 10f;
        private const float MouseAssistRadiusPixels = 96f;
        private const float TargetRefreshInterval = 0.1f;
        private const float MouseTargetRefreshInterval = 0.05f;
        private const float UnlockHoldSeconds = 0.45f;

        private static RangedControlsController current;

        private readonly List<RaycastHit2D> sightHits = new List<RaycastHit2D>(32);
        private readonly List<UnitAvatar> switchTargets = new List<UnitAvatar>(16);
        private readonly TargetLockFeedbackView targetFeedback =
            new TargetLockFeedbackView();
        private PlayerAvatar player;
        private WeaponControllerSimple weaponController;
        private UnitAvatar ownedTarget;
        private UnitAvatar switchedTarget;
        private UnitAvatar cachedAutomaticTarget;
        private UnitAvatar cachedMouseTarget;
        private Vector2 lastDirection = Vector2.right;
        private Vector2 lastScoredDirection = Vector2.right;
        private float nextTargetRefreshAt;
        private float nextMouseTargetRefreshAt;
        private float switchPressedAt;
        private bool keyboardAimActive;
        private bool lastScorePreferredDirection;
        private bool switchHoldHandled;
        private bool runtimeCompatible = true;

        private void Awake()
        {
            current = this;
        }

        private void LateUpdate()
        {
            if (!runtimeCompatible)
            {
                return;
            }

            try
            {
                RefreshAim();
            }
            catch (Exception ex)
            {
                ClearOwnedAim(force: true);
                runtimeCompatible = false;
                SupportLogger.Warning("ranged_controls_failed", "[SephiriaEnhancements] Ranged controls disabled " +
                    "for the current gameplay context: " + ex);
            }
        }

        internal void ResetGameplayContext()
        {
            ClearOwnedAim(force: true);
            targetFeedback.Hide();
            player = null;
            weaponController = null;
            switchedTarget = null;
            cachedAutomaticTarget = null;
            cachedMouseTarget = null;
            lastDirection = Vector2.right;
            lastScoredDirection = Vector2.right;
            nextTargetRefreshAt = 0f;
            nextMouseTargetRefreshAt = 0f;
            switchHoldHandled = false;
            runtimeCompatible = true;
        }

        internal static bool TryGetKeyboardAttackDirection(PlayerAvatar source,
            NativeActionId actionId, out Vector2 direction)
        {
            direction = Vector2.zero;
            if (current == null || source == null || current.player != source ||
                !LocalPlayerResolver.IsLocal(source))
            {
                return false;
            }

            PlayerInputController input = PlayerInputController.Instance;
            bool usesKeyboardAndMouse = input?.playerInput?.currentControlScheme ==
                PlayerInputController.KeyboardAndMouseScheme;
            if (usesKeyboardAndMouse)
            {
                InputActionAsset actions = input.playerInput.actions;
                if (OfficialCombatBindings.IsActionControlledByMouse(actions,
                    actionId))
                {
                    current.YieldToMouse(input);
                    return false;
                }

                if (!OfficialCombatBindings.IsActionControlledByKeyboard(actions,
                    actionId))
                {
                    return false;
                }

                current.keyboardAimActive = true;
            }

            if (!current.keyboardAimActive)
            {
                return false;
            }

            Vector2 liveDirection = source.Input;
            if (liveDirection.sqrMagnitude > 0.01f)
            {
                current.lastDirection = liveDirection.normalized;
            }

            UnitAvatar target = usesKeyboardAndMouse
                ? current.ResolveKeyboardTarget(source.Input)
                : current.switchedTarget;
            Vector2 aimPosition = target != null
                ? (Vector2)target.transform.position
                : (Vector2)source.transform.position + current.lastDirection * AimDistance;
            current.ownedTarget = target;
            direction = aimPosition - (Vector2)source.transform.position;
            return direction.sqrMagnitude > 0.0001f;
        }

        private void OnDisable()
        {
            ClearOwnedAim(force: true);
        }

        private void OnDestroy()
        {
            ClearOwnedAim(force: true);
            targetFeedback.Dispose();
            if (current == this)
            {
                current = null;
            }
        }

        private void RefreshAim()
        {
            PlayerInputController input = PlayerInputController.Instance;
            NativeControlCoordinator.PreparePlayerInput(input);
            PlayerAvatar localPlayer = CombatManager.Instance?.CurrentPlayer ??
                GameCamera.Instance?.Observer;
            if (!CanControl(input, localPlayer))
            {
                ClearOwnedAim(force: keyboardAimActive);
                return;
            }

            if (player != localPlayer)
            {
                ClearOwnedAim(force: true);
                player = localPlayer;
                weaponController = player.GetComponent<WeaponControllerSimple>();
                switchedTarget = null;
                cachedAutomaticTarget = null;
                cachedMouseTarget = null;
                lastDirection = Vector2.right;
                lastScoredDirection = Vector2.right;
                nextTargetRefreshAt = 0f;
                nextMouseTargetRefreshAt = 0f;
            }

            bool supportsTargetCursor = player.activeMagicCastModeClientside ||
                (weaponController?.currentWeapon != null &&
                 weaponController.currentWeapon.isRangedWeapon);

            TargetingMode mode = RangedControlsSettings.TargetingMode;
            InputActionAsset actions = input.playerInput.actions;
            if (mode != TargetingMode.Disabled)
            {
                if (input.playerInput.currentControlScheme ==
                    PlayerInputController.KeyboardAndMouseScheme)
                {
                    ApplyKeyboardAim(input, actions);
                }
                else
                {
                    ApplyGamepadTargetLock(input, actions);
                }
                return;
            }

            keyboardAimActive = false;
            if (input.playerInput.currentControlScheme ==
                    PlayerInputController.KeyboardAndMouseScheme &&
                supportsTargetCursor &&
                RangedControlsSettings.MouseAimAssistEnabled)
            {
                ApplyMouseAssist(input);
            }
            else
            {
                ClearOwnedAim(force: false);
            }
        }

        private bool CanControl(PlayerInputController input, PlayerAvatar localPlayer)
        {
            if (!EnhancementsSettings.Enabled || input == null || localPlayer == null ||
                localPlayer.NetworkaimObject == null || localPlayer.IsDead ||
                !localPlayer.gameObject.activeInHierarchy || input.BlockAvatarInput ||
                input.playerInput == null ||
                (input.playerInput.currentControlScheme !=
                    PlayerInputController.KeyboardAndMouseScheme &&
                 input.playerInput.currentControlScheme !=
                    PlayerInputController.GamepadScheme) ||
                UIManager.Instance == null || UIManager.Instance.CurrentControlStack != null)
            {
                return false;
            }

            return LocalPlayerResolver.IsLocal(localPlayer);
        }

        private void ApplyKeyboardAim(PlayerInputController input,
            InputActionAsset actions)
        {
            Vector2 direction = player.Input;
            if (direction.sqrMagnitude > 0.01f)
            {
                lastDirection = direction.normalized;
            }

            bool lockedTargetSwitchPressed = HandleLockedTargetSwitch(actions,
                input.autoAimedTarget);
            if (!lockedTargetSwitchPressed && InputDeviceState.HasPointerMoved())
            {
                YieldToMouse(input);
                return;
            }

            if (lockedTargetSwitchPressed ||
                OfficialCombatBindings.WasKeyboardCombatPressed(actions))
            {
                keyboardAimActive = true;
            }

            if (!keyboardAimActive)
            {
                ClearOwnedAim(force: false);
                return;
            }

            UnitAvatar target = ResolveKeyboardTarget(direction);
            Vector2 aimPosition = target != null
                ? (Vector2)target.transform.position
                : (Vector2)player.transform.position + lastDirection * AimDistance;

            keyboardAimActive = true;
            ApplyAim(input, target, aimPosition, forceTargetClear: true);
        }

        private void ApplyGamepadTargetLock(PlayerInputController input,
            InputActionAsset actions)
        {
            HandleLockedTargetSwitch(actions, input.autoAimedTarget);
            if (!IsSelectableTarget(switchedTarget))
            {
                switchedTarget = null;
                keyboardAimActive = false;
                ClearOwnedAim(force: false);
                return;
            }

            keyboardAimActive = true;
            ApplyAim(input, switchedTarget, switchedTarget.transform.position,
                forceTargetClear: false);
        }

        private bool HandleLockedTargetSwitch(InputActionAsset actions,
            UnitAvatar nativeTarget)
        {
            if (NativeInputActions.WasPressed(actions,
                    ModShortcuts.SwitchLockedTarget))
            {
                switchPressedAt = Time.unscaledTime;
                switchHoldHandled = false;
                SwitchTarget(ownedTarget != null ? ownedTarget : nativeTarget);
                return true;
            }

            if (NativeInputActions.IsPressed(actions,
                    ModShortcuts.SwitchLockedTarget) &&
                !switchHoldHandled &&
                Time.unscaledTime - switchPressedAt >= UnlockHoldSeconds)
            {
                switchedTarget = null;
                cachedAutomaticTarget = null;
                nextTargetRefreshAt = 0f;
                switchHoldHandled = true;
            }

            return false;
        }

        private void YieldToMouse(PlayerInputController input)
        {
            switchedTarget = null;
            cachedAutomaticTarget = null;
            nextTargetRefreshAt = 0f;
            ClearOwnedTarget(input, force: false);
            keyboardAimActive = false;
        }

        private UnitAvatar ResolveKeyboardTarget(Vector2 movementDirection)
        {
            float now = Time.unscaledTime;
            bool shouldRefresh = now >= nextTargetRefreshAt;
            if (IsTargetInViewAndRange(switchedTarget) &&
                (!shouldRefresh || HasClearShot(player.transform.position,
                    switchedTarget.transform.position)))
            {
                if (shouldRefresh) nextTargetRefreshAt = now + TargetRefreshInterval;
                return switchedTarget;
            }

            switchedTarget = null;
            bool preferDirection = movementDirection.sqrMagnitude > 0.16f;
            Vector2 scoredDirection = preferDirection
                ? movementDirection.normalized : lastDirection;
            bool directionChanged = preferDirection != lastScorePreferredDirection ||
                (preferDirection && Vector2.Dot(scoredDirection, lastScoredDirection) < 0.98f);
            if (!shouldRefresh && !directionChanged)
            {
                return IsTargetInViewAndRange(cachedAutomaticTarget)
                    ? cachedAutomaticTarget : null;
            }

            lastScorePreferredDirection = preferDirection;
            lastScoredDirection = scoredDirection;
            nextTargetRefreshAt = now + TargetRefreshInterval;
            cachedAutomaticTarget = FindAutomaticTarget(movementDirection);
            return cachedAutomaticTarget;
        }

        private void SwitchTarget(UnitAvatar currentTarget)
        {
            CollectSwitchTargets();
            if (switchTargets.Count == 0)
            {
                switchedTarget = null;
                return;
            }

            int currentIndex = switchTargets.IndexOf(currentTarget);
            switchedTarget = switchTargets[(currentIndex + 1) % switchTargets.Count];
            cachedAutomaticTarget = null;
            nextTargetRefreshAt = Time.unscaledTime + TargetRefreshInterval;
        }

        private void CollectSwitchTargets()
        {
            switchTargets.Clear();
            List<UnitAvatar> candidates = CombatManager.Instance?.AllCreatures;
            if (candidates == null)
            {
                return;
            }

            for (int index = 0; index < candidates.Count; index++)
            {
                UnitAvatar candidate = candidates[index];
                if (!IsSelectableTarget(candidate))
                {
                    continue;
                }

                int insertAt = switchTargets.Count;
                float distance = ((Vector2)candidate.transform.position -
                    (Vector2)player.transform.position).sqrMagnitude;
                while (insertAt > 0)
                {
                    UnitAvatar previous = switchTargets[insertAt - 1];
                    float previousDistance = ((Vector2)previous.transform.position -
                        (Vector2)player.transform.position).sqrMagnitude;
                    if (previousDistance <= distance)
                    {
                        break;
                    }
                    insertAt--;
                }
                switchTargets.Insert(insertAt, candidate);
            }
        }

        private bool IsSelectableTarget(UnitAvatar candidate)
        {
            return IsTargetInViewAndRange(candidate) &&
                HasClearShot(player.transform.position, candidate.transform.position);
        }

        private bool IsTargetInViewAndRange(UnitAvatar candidate)
        {
            Camera camera = GameCamera.Instance?.Camera;
            if (camera == null || !IsEligibleTarget(candidate) ||
                !IsOnScreen(camera, candidate.transform.position))
            {
                return false;
            }

            Vector2 offset = (Vector2)candidate.transform.position -
                (Vector2)player.transform.position;
            return offset.sqrMagnitude <= DirectionalAssistDistance *
                DirectionalAssistDistance;
        }

        private void ApplyMouseAssist(PlayerInputController input)
        {
            Camera camera = GameCamera.Instance?.Camera;
            if (!InputDeviceState.TryGetPointerPosition(out Vector2 pointer) ||
                camera == null)
            {
                ClearOwnedAim(force: false);
                return;
            }

            float now = Time.unscaledTime;
            if (now >= nextMouseTargetRefreshAt)
            {
                nextMouseTargetRefreshAt = now + MouseTargetRefreshInterval;
                cachedMouseTarget = FindMouseTarget(camera, pointer);
            }

            UnitAvatar target = cachedMouseTarget;
            if (target == null)
            {
                ClearOwnedAim(force: false);
                return;
            }

            ApplyAim(input, target, target.transform.position, forceTargetClear: false);
        }

        private void ApplyAim(PlayerInputController input, UnitAvatar target,
            Vector2 aimPosition, bool forceTargetClear)
        {
            if (target == null)
            {
                ClearOwnedTarget(input, forceTargetClear);
            }
            else
            {
                bool targetChanged = ownedTarget != target;
                ownedTarget = target;
                input.autoAimedTarget = target;
                player.autoAimedTarget = target;
                UI_TargetingCursor cursor = UIManager.Instance.GetElement<UI_TargetingCursor>();
                if (cursor != null)
                {
                    cursor.SetTarget(target);
                    bool showCursor = keyboardAimActive ||
                        player.activeMagicCastModeClientside ||
                        (weaponController?.currentWeapon != null &&
                         weaponController.currentWeapon.isRangedWeapon);
                    if (showCursor)
                    {
                        cursor.SetVisible(visible: true, autoAiming: true);
                    }
                }
                targetFeedback.Show(target, GameCamera.Instance?.Camera,
                    target == switchedTarget, targetChanged);
            }

            player.NetworkaimObject.transform.position = aimPosition;
        }

        private UnitAvatar FindAutomaticTarget(Vector2 movementDirection)
        {
            List<UnitAvatar> candidates = CombatManager.Instance?.AllCreatures;
            Camera camera = GameCamera.Instance?.Camera;
            if (candidates == null || camera == null)
            {
                return null;
            }

            bool preferDirection = movementDirection.sqrMagnitude > 0.16f;
            Vector2 direction = preferDirection ? movementDirection.normalized : lastDirection;
            Vector2 origin = player.transform.position;
            float maxDistanceSquared = DirectionalAssistDistance * DirectionalAssistDistance;
            UnitAvatar best = null;
            float bestScore = float.NegativeInfinity;

            for (int index = 0; index < candidates.Count; index++)
            {
                UnitAvatar candidate = candidates[index];
                if (!IsEligibleTarget(candidate) ||
                    !IsOnScreen(camera, candidate.transform.position))
                {
                    continue;
                }

                Vector2 offset = (Vector2)candidate.transform.position - origin;
                float distanceSquared = offset.sqrMagnitude;
                if (distanceSquared > maxDistanceSquared ||
                    !HasClearShot(origin, candidate.transform.position))
                {
                    continue;
                }

                float score = DirectionalAimMath.AutomaticTargetScore(
                    direction.x, direction.y, offset.x, offset.y, distanceSquared,
                    maxDistanceSquared, preferDirection, candidate == ownedTarget);
                if (score > bestScore)
                {
                    best = candidate;
                    bestScore = score;
                }
            }

            return best;
        }

        private UnitAvatar FindMouseTarget(Camera camera, Vector2 pointer)
        {
            List<UnitAvatar> candidates = CombatManager.Instance?.AllCreatures;
            if (candidates == null)
            {
                return null;
            }

            UnitAvatar best = null;
            float bestDistance = MouseAssistRadiusPixels * MouseAssistRadiusPixels;
            Vector2 origin = player.transform.position;
            for (int index = 0; index < candidates.Count; index++)
            {
                UnitAvatar candidate = candidates[index];
                if (!IsEligibleTarget(candidate))
                {
                    continue;
                }

                if (!IsOnScreen(camera, candidate.transform.position))
                {
                    continue;
                }

                Vector2 screen = camera.WorldToScreenPoint(candidate.transform.position);
                float distanceSquared = (screen - pointer).sqrMagnitude;
                if (distanceSquared >= bestDistance ||
                    !HasClearShot(origin, candidate.transform.position))
                {
                    continue;
                }

                best = candidate;
                bestDistance = distanceSquared;
            }

            return best;
        }

        private bool IsEligibleTarget(UnitAvatar candidate)
        {
            if (candidate == null || candidate == player || candidate.IsDead ||
                candidate.canBeTarget <= 0 ||
                !candidate.gameObject.activeInHierarchy || RuntimeFactionManager.Instance == null)
            {
                return false;
            }

            long hostileLayers = player.GetHostileFactionLayers(EDamageFromType.None);
            long candidateLayer = RuntimeFactionManager.Instance.FindFactionLayer(candidate.faction);
            return (hostileLayers & candidateLayer) != 0;
        }

        private bool HasClearShot(Vector2 from, Vector2 to)
        {
            int mask = CombatManager.TileLayerMask | CombatManager.BlockableLayerMask;
            if (mask == 0)
            {
                return false;
            }

            ContactFilter2D filter = new ContactFilter2D();
            filter.SetLayerMask(mask);
            filter.useTriggers = true;
            sightHits.Clear();
            Physics2D.Linecast(from, to, filter, sightHits);
            for (int index = 0; index < sightHits.Count; index++)
            {
                Collider2D collider = sightHits[index].collider;
                if (collider == null || collider.GetComponentInParent<UnitAvatar>() != null ||
                    collider.GetComponentInParent<ProjectileBase>() != null)
                {
                    continue;
                }

                return false;
            }

            return true;
        }

        private void ClearOwnedAim(bool force)
        {
            PlayerInputController input = PlayerInputController.Instance;
            ClearOwnedTarget(input, force);
            keyboardAimActive = false;
        }

        private void ClearOwnedTarget(PlayerInputController input, bool force)
        {
            bool ownsInputTarget = input != null && ownedTarget != null &&
                input.autoAimedTarget == ownedTarget;
            bool ownsPlayerTarget = player != null && ownedTarget != null &&
                player.autoAimedTarget == ownedTarget;
            if (input != null && (force || ownsInputTarget))
            {
                input.autoAimedTarget = null;
            }

            if (player != null && (force || ownsPlayerTarget))
            {
                player.autoAimedTarget = null;
            }

            if ((force || ownsInputTarget || ownsPlayerTarget) && UIManager.Instance != null)
            {
                UI_TargetingCursor cursor = UIManager.Instance.GetElement<UI_TargetingCursor>();
                cursor?.SetTarget(null);
            }

            ownedTarget = null;
            targetFeedback.Hide();
        }

        private static bool IsOnScreen(Camera camera, Vector3 position)
        {
            Vector3 viewport = camera.WorldToViewportPoint(position);
            return viewport.z > 0f && viewport.x >= 0f && viewport.x <= 1f &&
                viewport.y >= 0f && viewport.y <= 1f;
        }

    }
}
