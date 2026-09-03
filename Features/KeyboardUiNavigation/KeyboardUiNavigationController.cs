using System.Reflection;
using HarmonyLib;
using SephiriaEnhancements.Configuration;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace SephiriaEnhancements.KeyboardUiNavigation
{
    internal sealed class KeyboardUiNavigationController : MonoBehaviour
    {
        private static readonly MethodInfo UseDefaultSelectableSetter =
            AccessTools.PropertySetter(typeof(ControlsChangeHandler),
                nameof(ControlsChangeHandler.UseDefaultSelectable));

        private static KeyboardUiNavigationController current;

        private UIBase pendingPanel;
        private GameObject pendingSelectable;
        private int requestedFrame;

        private void Awake()
        {
            current = this;
            ApplyNativeSelectionPolicy(ControlsChangeHandler.Current);
        }

        private void Update()
        {
            KeyboardUiPointer.RefreshInput();
            ApplyNativeSelectionPolicy(ControlsChangeHandler.Current);
            if (!EnhancementsSettings.Enabled)
            {
                ClearPendingSelection();
                return;
            }

            InitializePendingSelection();
            if (!OptionsKeyboardNavigation.SwitchTab())
                SwitchCombinedPanelWithTab();
            RestoreMissingKeyboardSelection();
        }

        private void LateUpdate()
        {
            // Navigation and scroll/layout updates can run after a picker's Update.
            // Resolve its final position again before rendering the same frame.
            if (KeyboardUiPointer.SelectedTarget() != null)
            {
                UI_NewItemPicker_Controller picker = UIManager.Instance
                    .GetElement<UI_NewItemPicker_Controller>();
                if (picker != null) KeyboardUiPointer.PositionCarriedItem(picker);
            }
            KeyboardUiPointer.UpdateCursor();
        }

        private void OnDestroy()
        {
            KeyboardUiPointer.Reset();
            SetKeyboardDefaultSelection(ControlsChangeHandler.Current, false);
            ClearPendingSelection();
            if (current == this)
            {
                current = null;
            }
        }

        internal void ResetGameplayContext()
        {
            KeyboardUiPointer.Reset();
            ClearPendingSelection();
        }

        internal static void RequestSelection(UIBase panel, GameObject selectable)
        {
            if (current == null || !EnhancementsSettings.Enabled || panel == null)
            {
                return;
            }

            current.pendingPanel = panel;
            current.pendingSelectable = selectable;
            current.requestedFrame = Time.frameCount;
        }

        internal static void ApplyNativeSelectionPolicy(
            ControlsChangeHandler controls)
        {
            // Keep global automatic reselection disabled. Menu entry focus is
            // supplied separately; other panels recover on navigation intent.
            SetKeyboardDefaultSelection(controls, false);
        }

        internal static bool IsKeyboardModeActive()
        {
            ControlsChangeHandler controls = ControlsChangeHandler.Current;
            return EnhancementsSettings.Enabled && controls?.PlayerInput != null &&
                controls.PlayerInput.currentControlScheme ==
                    PlayerInputController.KeyboardAndMouseScheme;
        }

        internal static bool WasNativeUiActionPressed(
            InputActionReference actionReference) =>
            IsKeyboardModeActive() && actionReference != null &&
            actionReference.action.WasPressedThisFrame();

        private void InitializePendingSelection()
        {
            UIBase panel = pendingPanel;
            if (panel == null || !panel.IsOpened ||
                (panel.hasControl && !panel.IsControlEnabled))
            {
                ClearPendingSelection();
                return;
            }
            if (Time.frameCount <= requestedFrame)
            {
                return;
            }

            EventSystem eventSystem = EventSystem.current;
            PlayerInputController input = PlayerInputController.Instance;
            if (eventSystem == null || input?.playerInput == null ||
                input.playerInput.currentControlScheme !=
                    PlayerInputController.KeyboardAndMouseScheme)
            {
                return;
            }

            if (panel.CanvasGroup != null && !panel.CanvasGroup.interactable)
            {
                return;
            }

            GameObject selectable = pendingSelectable;
            if (selectable == null || !selectable.activeInHierarchy)
            {
                return;
            }

            GameObject selected = eventSystem.currentSelectedGameObject;
            if (selected != null && selected != selectable &&
                selected.activeInHierarchy &&
                selected.transform.IsChildOf(panel.transform))
            {
                ClearPendingSelection();
                return;
            }

            Selectable nativeSelectable = selectable.GetComponent<Selectable>();
            if (nativeSelectable != null && !nativeSelectable.IsInteractable())
            {
                return;
            }

            // The native InputSystemUIInputModule continues to own navigation and
            // submit. This only supplies the focus that Keyboard&Mouse omits.
            eventSystem.SetSelectedGameObject(null);
            eventSystem.SetSelectedGameObject(selectable);
            if (eventSystem.currentSelectedGameObject == selectable)
            {
                ClearPendingSelection();
            }
        }

        private static void SetKeyboardDefaultSelection(
            ControlsChangeHandler controls, bool enabled)
        {
            if (controls?.PlayerInput == null ||
                controls.PlayerInput.currentControlScheme !=
                    PlayerInputController.KeyboardAndMouseScheme ||
                controls.UseDefaultSelectable == enabled)
            {
                return;
            }

            UseDefaultSelectableSetter.Invoke(controls, new object[] { enabled });
        }

        private static void RestoreMissingKeyboardSelection()
        {
            EventSystem eventSystem = EventSystem.current;
            ControlsChangeHandler controls = ControlsChangeHandler.Current;
            if (eventSystem == null || controls?.PlayerInput == null ||
                controls.PlayerInput.currentControlScheme !=
                    PlayerInputController.KeyboardAndMouseScheme)
            {
                return;
            }

            GameObject selected = eventSystem.currentSelectedGameObject;
            bool hasUsableSelection = selected != null &&
                selected.activeInHierarchy;
            if (!KeyboardSelectionRecoveryPolicy.ShouldRestore(
                    keyboardMode: true, hasUsableSelection,
                    WasKeyboardNavigationPressed()))
            {
                return;
            }

            UIManager manager = UIManager.Instance;
            if (manager == null || manager.CurrentControlStack == null ||
                manager.CurrentControlStack.Count == 0)
            {
                return;
            }

            GameObject selectable = manager.CurrentControlStack[0].defaultSelectable;
            if (selectable == null || !selectable.activeInHierarchy)
            {
                return;
            }

            Selectable nativeSelectable = selectable.GetComponent<Selectable>();
            if (nativeSelectable == null || nativeSelectable.IsInteractable())
            {
                eventSystem.SetSelectedGameObject(selectable);
            }
        }

        private static bool WasKeyboardNavigationPressed()
        {
            if (!IsKeyboardModeActive() || !KeyboardUiPointer.OwnsFocus)
            {
                return false;
            }
            InputActionReference move = UIInputModule.currentModule?.move;
            if (move?.action?.WasPressedThisFrame() == true)
            {
                return true;
            }
            Keyboard keyboard = Keyboard.current;
            return keyboard != null && keyboard.tabKey.wasPressedThisFrame;
        }

        private static void SwitchCombinedPanelWithTab()
        {
            Keyboard keyboard = Keyboard.current;
            EventSystem eventSystem = EventSystem.current;
            ControlsChangeHandler controls = ControlsChangeHandler.Current;
            UIManager manager = UIManager.Instance;
            if (keyboard == null || !keyboard.tabKey.wasPressedThisFrame ||
                eventSystem == null || controls?.PlayerInput == null ||
                controls.PlayerInput.currentControlScheme !=
                    PlayerInputController.KeyboardAndMouseScheme ||
                manager == null || manager.CurrentControlStack == null ||
                manager.CurrentControlStack.Count < 2)
            {
                return;
            }

            GameObject selected = eventSystem.currentSelectedGameObject;
            int currentPanelIndex = -1;
            for (int index = 0; index < manager.CurrentControlStack.Count; index++)
            {
                UIBase panel = manager.CurrentControlStack[index];
                if (panel != null && selected != null &&
                    selected.transform.IsChildOf(panel.transform))
                {
                    currentPanelIndex = index;
                    break;
                }
            }

            bool reverse = keyboard.leftShiftKey.isPressed ||
                keyboard.rightShiftKey.isPressed;
            int direction = reverse ? -1 : 1;
            int panelCount = manager.CurrentControlStack.Count;
            for (int offset = 1; offset <= panelCount; offset++)
            {
                int index = (currentPanelIndex + direction * offset) % panelCount;
                if (index < 0)
                {
                    index += panelCount;
                }

                GameObject entry = FindPanelEntry(manager.CurrentControlStack[index]);
                if (entry != null && entry != selected)
                {
                    eventSystem.SetSelectedGameObject(entry);
                    return;
                }
            }
        }

        private static GameObject FindPanelEntry(UIBase panel)
        {
            if (panel == null || !panel.IsControlEnabled)
            {
                return null;
            }

            GameObject entry = panel.defaultSelectable;
            if (IsSelectable(entry))
            {
                return entry;
            }

            // Reward entries are generated after the panel opens, so the native
            // panel has no serialized defaultSelectable for this partition.
            if (panel is UI_SephiriteRewardPanel rewardPanel &&
                rewardPanel.rewardZone != null)
            {
                for (int index = 0; index < rewardPanel.rewardZone.childCount;
                    index++)
                {
                    UI_SephiriteRewardElement reward = rewardPanel.rewardZone.
                        GetChild(index).GetComponent<UI_SephiriteRewardElement>();
                    if (reward != null && IsSelectable(reward.gameObject))
                    {
                        return reward.gameObject;
                    }
                }
            }

            return null;
        }

        private static bool IsSelectable(GameObject candidate)
        {
            if (candidate == null || !candidate.activeInHierarchy)
            {
                return false;
            }

            Selectable selectable = candidate.GetComponent<Selectable>();
            return selectable == null || selectable.IsInteractable();
        }

        private void ClearPendingSelection()
        {
            pendingPanel = null;
            pendingSelectable = null;
        }
    }
}
