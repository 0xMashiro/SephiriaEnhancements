using System;
using System.Linq;
using SephiriaEnhancements.Configuration;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace SephiriaEnhancements.Integration
{
    internal sealed class NativeNumberInputNavigation : MonoBehaviour
    {
        private UI_MessageBox_InputYesNo dialog;
        private NumberInputOptions options;
        private TextMeshProUGUI feedback;
        private GameObject keypad;
        private GameObject adjustments;
        private Button[] keys;
        private Button decrease, clear, increase;
        private bool usingKeypad;
        private int openedFrame;
        private bool lastCanEdit;
        private TextMeshProUGUI buttonFont;
        internal GameObject InitialSelection { get; private set; }
        private readonly System.Collections.Generic.List<Action> restore = new();

        internal void Configure(UI_MessageBox_InputYesNo value, NumberInputOptions settings)
        {
            dialog = value;
            options = settings;
            openedFrame = Time.frameCount;
            foreach (var rect in new[] { dialog.rectTransform, dialog.text.rectTransform,
                (RectTransform)dialog.input.transform, (RectTransform)dialog.yesButton.transform,
                (RectTransform)dialog.noButton.transform }) CaptureLayout(rect);
            bool activateOnSelect = dialog.input.shouldActivateOnSelect;
            restore.Add(() => dialog.input.shouldActivateOnSelect = activateOnSelect);
            var steamKeyboard = dialog.input.GetComponent<SteamDeckKeyboardTrigger>();
            if (steamKeyboard != null)
            {
                bool wasEnabled = steamKeyboard.enabled;
                steamKeyboard.enabled = false;
                restore.Add(() => steamKeyboard.enabled = wasEnabled);
            }
            buttonFont = dialog.yesButton.GetComponentInChildren<TextMeshProUGUI>(true);
            SetActionLabel(dialog.yesButton, options.ConfirmKey);
            SetActionLabel(dialog.noButton, options.CancelKey);
            Place((RectTransform)dialog.yesButton.transform, new Vector2(.15f, .025f), new Vector2(.48f, .115f));
            Place((RectTransform)dialog.noButton.transform, new Vector2(.52f, .025f), new Vector2(.85f, .115f));
            var originalValidation = dialog.input.onValidateInput;
            var contentType = dialog.input.contentType;
            var inputType = dialog.input.inputType;
            var characterValidation = dialog.input.characterValidation;
            var lineType = dialog.input.lineType;
            dialog.input.contentType = TMP_InputField.ContentType.Standard;
            dialog.input.lineType = TMP_InputField.LineType.SingleLine;
            // Preserve invalid pasted text so validation cannot silently change the seed.
            dialog.input.onValidateInput = null;
            restore.Add(() => { dialog.input.contentType = contentType;
                dialog.input.inputType = inputType; dialog.input.characterValidation = characterValidation;
                dialog.input.lineType = lineType; });
            restore.Add(() => dialog.input.onValidateInput = originalValidation);
            bool originalCancel = dialog.canCloseControlWithESC;
            dialog.canCloseControlWithESC = true;
            restore.Add(() => dialog.canCloseControlWithESC = originalCancel);
            var parent = (RectTransform)dialog.transform.parent;
            dialog.rectTransform.sizeDelta = new Vector2(Mathf.Min(350, parent.rect.width * .9f), Mathf.Min(320, parent.rect.height * .9f));
            Place(dialog.text.rectTransform, new Vector2(.05f, .73f), new Vector2(.95f, .96f));
            Place((RectTransform)dialog.input.transform, new Vector2(.15f, .62f), new Vector2(.85f, .71f));
            var go = new GameObject("Input Feedback", typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(dialog.transform, false);
            feedback = go.GetComponent<TextMeshProUGUI>();
            Place(feedback.rectTransform, new Vector2(.05f, .43f), new Vector2(.95f, .60f));
            feedback.alignment = TextAlignmentOptions.Center;
            feedback.raycastTarget = false;
            NativeLocalizedText.BindFont(feedback, dialog.text);
            NativeLocalizedText.MatchFontSize(feedback, dialog.text);
            Selectable[] controls = { dialog.input, dialog.yesButton, dialog.noButton };
            for (int i = 0; i < controls.Length; i++)
            {
                var control = controls[i];
                var original = control.navigation;
                restore.Add(() => control.navigation = original);
                control.navigation = new Navigation { mode = Navigation.Mode.Explicit,
                    selectOnDown = controls[(i + 1) % 3], selectOnUp = controls[(i + 2) % 3],
                    selectOnLeft = i == 0 ? null : controls[i == 1 ? 2 : 1],
                    selectOnRight = i == 0 ? null : controls[i == 1 ? 2 : 1] };
            }
            BuildKeypad();
            BuildAdjustments();
            RefreshInputMode();
            if (ControlsChangeHandler.Current?.IsUsingKeyboardAndMouse != false)
                dialog.input.ActivateInputField();
        }

        private void BuildKeypad()
        {
            keypad = new GameObject("Number Pad", typeof(RectTransform));
            keypad.transform.SetParent(dialog.transform, false);
            Place((RectTransform)keypad.transform, new Vector2(.22f, .12f), new Vector2(.78f, .35f));
            string[] symbols = { "1", "2", "3", "4", "5", "6", "7", "8", "9", options.Signed ? "±" : ".", "0", "⌫" };
            keys = new Button[symbols.Length];
            for (int i = 0; i < symbols.Length; i++)
            {
                string symbol = symbols[i];
                var go = new GameObject("Digit " + i, typeof(RectTransform), typeof(Image), typeof(Button));
                go.transform.SetParent(keypad.transform, false);
                Place((RectTransform)go.transform, new Vector2((i % 3) / 3f, 1 - (i / 3 + 1) / 4f),
                    new Vector2((i % 3 + 1) / 3f, 1 - (i / 3) / 4f));
                ((RectTransform)go.transform).offsetMin = Vector2.one;
                ((RectTransform)go.transform).offsetMax = -Vector2.one;
                var button = keys[i] = go.GetComponent<Button>();
                NativeActionButtonStyle.Apply(button);
                button.onClick.AddListener(() =>
                {
                    SetInput(NumberInputEditing.Apply(dialog.input.text, symbol,
                        options.CharacterLimit, (text, index, character) =>
                            options.ValidateCharacter(text, index, character)));
                });
                var label = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
                label.transform.SetParent(go.transform, false);
                Place((RectTransform)label.transform, Vector2.zero, Vector2.one);
                var textLabel = label.GetComponent<TextMeshProUGUI>();
                textLabel.text = symbol;
                textLabel.alignment = TextAlignmentOptions.Center;
                textLabel.raycastTarget = false;
                NativeLocalizedText.BindFont(textLabel, dialog.text);
                NativeLocalizedText.MatchFontSize(textLabel, buttonFont);
            }
            for (int i = 0; i < keys.Length; i++)
                keys[i].navigation = new Navigation { mode = Navigation.Mode.Explicit,
                    selectOnUp = i < 3 ? dialog.noButton : keys[i - 3],
                    selectOnDown = i >= 9 ? (i == 11 ? dialog.noButton : dialog.yesButton) : keys[i + 3],
                    selectOnLeft = keys[i / 3 * 3 + (i + 2) % 3],
                    selectOnRight = keys[i / 3 * 3 + (i + 1) % 3] };
            InitialSelection = ControlsChangeHandler.Current?.IsUsingKeyboardAndMouse == false ? keys[0].gameObject : dialog.input.gameObject;
            var inputNavigation = dialog.input.navigation;
            inputNavigation.selectOnDown = keys[0];
            dialog.input.navigation = inputNavigation;
            foreach (var button in new[] { dialog.yesButton, dialog.noButton })
            {
                var navigation = button.navigation;
                navigation.selectOnUp = keys[10];
                button.navigation = navigation;
            }
        }

        private void BuildAdjustments()
        {
            adjustments = new GameObject("Adjust Value", typeof(RectTransform));
            adjustments.transform.SetParent(dialog.transform, false);
            if (options.Adjust != null) decrease = AdjustmentButton("−", 0, .18f, () => Adjust(-1));
            clear = AdjustmentButton(T(options.ClearKey), options.Adjust == null ? 0 : .20f, options.Adjust == null ? 1 : .80f, () =>
            {
                SetInput("");
            });
            if (options.Adjust != null) increase = AdjustmentButton("+", .82f, 1, () => Adjust(1));
        }

        private Button AdjustmentButton(string label, float left, float right, Action clicked)
        {
            var go = new GameObject("Value Action", typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(adjustments.transform, false);
            Place((RectTransform)go.transform, new Vector2(left, 0), new Vector2(right, 1));
            var button = go.GetComponent<Button>();
            NativeActionButtonStyle.Apply(button);
            button.onClick.AddListener(() => clicked());
            var textObject = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            textObject.transform.SetParent(go.transform, false);
            var text = textObject.GetComponent<TextMeshProUGUI>();
            Place(text.rectTransform, Vector2.zero, Vector2.one);
            text.text = label;
            text.alignment = TextAlignmentOptions.Center;
            text.raycastTarget = false;
            NativeLocalizedText.BindFont(text, dialog.text);
            NativeLocalizedText.MatchFontSize(text, buttonFont);
            return button;
        }

        private void SetActionLabel(Button button, string key)
        {
            var label = button.GetComponentInChildren<TextMeshProUGUI>(true);
            string original = label.text;
            var localization = label.GetComponent<UI_LocalizationStringText>();
            if (localization != null)
            {
                string originalKey = localization.valueString.key;
                restore.Add(() => localization.UpdateKey(originalKey));
                localization.UpdateKey(key);
            }
            else
            {
                restore.Add(() => label.text = original);
                label.text = T(key);
            }
        }

        private void Adjust(int direction) => SetInput(options.Adjust(dialog.input.text, direction));

        private void SetInput(string text)
        {
            dialog.input.text = text;
            dialog.input.ForceLabelUpdate();
            RefreshFeedback();
        }

        private void RefreshInputMode()
        {
            usingKeypad = ControlsChangeHandler.Current?.IsUsingKeyboardAndMouse == false;
            var parent = (RectTransform)dialog.transform.parent;
            dialog.rectTransform.sizeDelta = new Vector2(Mathf.Min(350, parent.rect.width * .9f),
                Mathf.Min(usingKeypad ? 380 : 300, parent.rect.height * .9f));
            dialog.input.shouldActivateOnSelect = !usingKeypad;
            if (usingKeypad) dialog.input.DeactivateInputField();
            keypad.SetActive(usingKeypad);
            Place(dialog.text.rectTransform, new Vector2(.05f, usingKeypad ? .77f : .72f), new Vector2(.95f, .97f));
            Place((RectTransform)dialog.input.transform, new Vector2(.15f, usingKeypad ? .66f : .60f), new Vector2(.85f, usingKeypad ? .75f : .69f));
            Place((RectTransform)adjustments.transform, new Vector2(.08f, usingKeypad ? .37f : .47f), new Vector2(.92f, usingKeypad ? .45f : .56f));
            Place(feedback.rectTransform, new Vector2(.05f, usingKeypad ? .47f : .14f), new Vector2(.95f, usingKeypad ? .64f : .44f));
            var actions = new[] { decrease, clear, increase }.Where(button => button != null).ToArray();
            for (int i = 0; i < actions.Length; i++)
                actions[i].navigation = new Navigation { mode = Navigation.Mode.Explicit,
                    selectOnLeft = actions[(i + actions.Length - 1) % actions.Length], selectOnRight = actions[(i + 1) % actions.Length],
                    selectOnUp = dialog.input, selectOnDown = usingKeypad ? keys[i] : (i == 2 ? dialog.noButton : dialog.yesButton) };
            var inputNavigation = dialog.input.navigation;
            inputNavigation.selectOnDown = actions[0];
            dialog.input.navigation = inputNavigation;
            for (int i = 0; i < 3; i++)
            {
                var nav = keys[i].navigation; nav.selectOnUp = actions[Math.Min(i, actions.Length - 1)]; keys[i].navigation = nav;
            }
            foreach (var button in new[] { dialog.yesButton, dialog.noButton })
            {
                var nav = button.navigation;
                nav.selectOnUp = usingKeypad ? keys[button == dialog.yesButton ? 9 : 11] : button == dialog.yesButton ? actions[0] : actions[actions.Length - 1];
                button.navigation = nav;
            }
            InitialSelection = usingKeypad ? keys[0].gameObject : dialog.input.gameObject;
            if (EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(InitialSelection);
            if (!usingKeypad) dialog.input.ActivateInputField();
        }

        private void CaptureLayout(RectTransform rect)
        {
            var min = rect.anchorMin; var max = rect.anchorMax;
            var position = rect.anchoredPosition; var size = rect.sizeDelta;
            restore.Add(() => { rect.anchorMin = min; rect.anchorMax = max;
                rect.sizeDelta = size; rect.anchoredPosition = position; });
        }

        private void Place(RectTransform rect, Vector2 min, Vector2 max)
        {
            rect.anchorMin = min; rect.anchorMax = max;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }

        internal void RefreshFeedback()
        {
            bool valid = options.IsValid(dialog.input.text);
            lastCanEdit = options.CanEdit();
            dialog.yesButton.interactable = lastCanEdit && valid;
            feedback.text = options.Feedback(dialog.input.text);
            NativeLocalizedText.MatchFontSize(feedback, dialog.text);
            foreach (var button in keys.Concat(new[] { decrease, clear, increase }).Where(button => button != null))
                NativeLocalizedText.MatchFontSize(button.GetComponentInChildren<TextMeshProUGUI>(), buttonFont);
            feedback.color = valid ? Color.white : new Color(1f, .65f, .35f);
        }

        private void Update()
        {
            if (dialog == null || !dialog.IsOpened || !dialog.IsControlEnabled) return;
            if (usingKeypad != (ControlsChangeHandler.Current?.IsUsingKeyboardAndMouse == false))
            {
                RefreshInputMode();
                RefreshFeedback();
            }
            if (lastCanEdit != options.CanEdit()) RefreshFeedback();
            var keyboard = Keyboard.current;
            if (keyboard == null || !keyboard.tabKey.wasPressedThisFrame || EventSystem.current == null) return;
            var controls = new Selectable[] { dialog.input, decrease, clear, increase, dialog.yesButton, dialog.noButton }
                .Where(c => c != null && c.gameObject.activeInHierarchy && c.IsInteractable()).ToArray();
            int index = Array.FindIndex(controls, c => c.gameObject == EventSystem.current.currentSelectedGameObject);
            int direction = keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed ? -1 : 1;
            var target = controls[(index + direction + controls.Length) % controls.Length];
            EventSystem.current.SetSelectedGameObject(target.gameObject);
            if (target == dialog.input) dialog.input.ActivateInputField();
        }

        private void LateUpdate()
        {
            if (dialog == null || !dialog.IsOpened || !dialog.IsControlEnabled ||
                EventSystem.current?.currentSelectedGameObject != dialog.input.gameObject) return;
            // TMP consumes move events while editing. Read the game's bound UI action after
            // its input module, then move focus once instead of turning navigation into text.
            var move = UIInputModule.currentModule?.move?.action;
            if (move == null || !move.WasPerformedThisFrame()) return;
            var direction = move.ReadValue<Vector2>();
            if (direction.y == 0) return; // Horizontal arrows retain caret editing.
            var target = direction.y < 0 ? dialog.input.navigation.selectOnDown : dialog.noButton;
            dialog.input.DeactivateInputField();
            EventSystem.current.SetSelectedGameObject(target.gameObject);
        }

        internal void Submit(string text)
        {
            if (Time.frameCount == openedFrame) return;
            RefreshFeedback();
            if (!dialog.input.wasCanceled && dialog.IsOpened && dialog.IsControlEnabled && dialog.yesButton.interactable)
            {
                dialog.input.ForceLabelUpdate();
                dialog.yesButton.onClick.Invoke();
            }
        }

        internal void Release()
        {
            enabled = false;
            dialog.input.DeactivateInputField();
            foreach (var action in restore) action();
            restore.Clear();
            if (feedback != null) Destroy(feedback.gameObject);
            if (keypad != null) Destroy(keypad);
            if (adjustments != null) Destroy(adjustments);
            Destroy(this);
        }

        private static string T(string key) => ModLocalization.Get(key);
    }
}
