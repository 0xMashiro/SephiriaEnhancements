using System.Reflection;
using HarmonyLib;
using SephiriaEnhancements.Configuration;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

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
                RewardKeyboardNavigation.Reset();
                ClearPendingSelection();
                return;
            }

            // A new navigation gesture on an existing control supersedes an
            // entry request still waiting for another panel's animation.
            if (WasKeyboardNavigationPressed() &&
                KeyboardUiSelection.IsInControlStack(EventSystem.current?.currentSelectedGameObject))
                ClearPendingSelection();
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
            RewardKeyboardNavigation.Reset();
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
            RewardKeyboardNavigation.Reset();
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

        internal static void CancelSelection(UIBase panel)
        {
            if (current != null && current.pendingPanel == panel)
                current.ClearPendingSelection();
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

            if (!KeyboardUiSelection.IsPanelReady(panel))
            {
                return;
            }

            GameObject selectable = KeyboardUiSelection.FindPanelEntry(panel, pendingSelectable);
            if (selectable == null || !KeyboardUiSelection.IsInControlStack(selectable))
            {
                return;
            }

            GameObject selected = eventSystem.currentSelectedGameObject;
            if (KeyboardUiSelection.IsInPanel(panel, selected))
            {
                ClearPendingSelection();
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
            bool hasUsableSelection = KeyboardUiSelection.IsInControlStack(selected);
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

            foreach (UIBase panel in manager.CurrentControlStack)
            {
                GameObject selectable = KeyboardUiSelection.FindPanelEntry(panel);
                if (selectable == null) continue;
                current?.ClearPendingSelection();
                eventSystem.SetSelectedGameObject(selectable);
                return;
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
                if (KeyboardUiSelection.IsInPanel(panel, selected))
                {
                    currentPanelIndex = index;
                    break;
                }
            }

            bool reverse = keyboard.leftShiftKey.isPressed ||
                keyboard.rightShiftKey.isPressed;
            int startIndex = currentPanelIndex < 0 && reverse ? 0 : currentPanelIndex;
            int direction = reverse ? -1 : 1;
            int panelCount = manager.CurrentControlStack.Count;
            for (int offset = 1; offset <= panelCount; offset++)
            {
                int index = (startIndex + direction * offset) % panelCount;
                if (index < 0)
                {
                    index += panelCount;
                }
                if (index == currentPanelIndex) continue;

                UIBase targetPanel = manager.CurrentControlStack[index];
                GameObject entry = targetPanel is UI_SephiriteRewardPanel rewardPanel
                    ? RewardKeyboardNavigation.FindRememberedReward(rewardPanel) : null;
                if (targetPanel is UI_CharacterStatusPanel inventory &&
                    manager.GetElement<UI_NewItemPicker_Controller>()?.CurrentSephiriteReward != null)
                    entry = RewardKeyboardNavigation.FindFirstEmptyInventorySlot(inventory);
                if (entry == null) entry = KeyboardUiSelection.FindPanelEntry(targetPanel);
                if (entry != null && entry != selected)
                {
                    RewardKeyboardNavigation.RememberReward(selected);
                    current?.ClearPendingSelection();
                    eventSystem.SetSelectedGameObject(entry);
                    return;
                }
            }
        }

        private void ClearPendingSelection()
        {
            pendingPanel = null;
            pendingSelectable = null;
        }
    }
}
