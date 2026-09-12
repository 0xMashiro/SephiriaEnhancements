using System;
using System.Linq;
using SephiriaEnhancements.Configuration;
using SephiriaEnhancements.Integration;
using SephiriaEnhancements.MultiplayerRules.Presentation;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace SephiriaEnhancements.MultiplayerRules.Integration
{
    internal sealed class NativeRuleInputNavigation : MonoBehaviour
    {
        private UI_MessageBox_InputYesNo dialog;
        private Action confirm;
        private TextMeshProUGUI feedback;
        private MultiplayerRuleDefinition definition;
        private GameObject keypad;
        private GameObject adjustments;
        private Button[] keys;
        private Button decrease, useOriginal, increase;
        private bool usingKeypad;
        private float originalValue;
        internal GameObject InitialSelection { get; private set; }
        private readonly System.Collections.Generic.List<Action> restore = new();

        internal void Configure(UI_MessageBox_InputYesNo value, MultiplayerRuleDefinition rule, float nativeValue, Action submit)
        {
            dialog = value;
            definition = rule;
            originalValue = nativeValue;
            confirm = submit;
            var originalValidation = dialog.input.onValidateInput;
            dialog.input.onValidateInput = MultiplayerRuleInput.ValidateCharacter;
            restore.Add(() => dialog.input.onValidateInput = originalValidation);
            bool originalCancel = dialog.canCloseControlWithESC;
            dialog.canCloseControlWithESC = true;
            restore.Add(() => dialog.canCloseControlWithESC = originalCancel);
            var size = dialog.rectTransform.sizeDelta;
            restore.Add(() => dialog.rectTransform.sizeDelta = size);
            var parent = (RectTransform)dialog.transform.parent;
            dialog.rectTransform.sizeDelta = new Vector2(Mathf.Min(350, parent.rect.width * .9f), Mathf.Min(320, parent.rect.height * .9f));
            Place(dialog.text.rectTransform, new Vector2(.05f, .73f), new Vector2(.95f, .96f));
            Place((RectTransform)dialog.input.transform, new Vector2(.15f, .62f), new Vector2(.85f, .71f));
            var go = new GameObject("Input Feedback", typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(dialog.transform, false);
            feedback = go.GetComponent<TextMeshProUGUI>();
            Place(feedback.rectTransform, new Vector2(.05f, .43f), new Vector2(.95f, .60f), false);
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
            Place((RectTransform)keypad.transform, new Vector2(.22f, .12f), new Vector2(.78f, .44f), false);
            string[] symbols = { "1", "2", "3", "4", "5", "6", "7", "8", "9", ".", "0", "⌫" };
            keys = new Button[symbols.Length];
            for (int i = 0; i < symbols.Length; i++)
            {
                string symbol = symbols[i];
                var go = new GameObject("Digit " + i, typeof(RectTransform), typeof(Image), typeof(Button));
                go.transform.SetParent(keypad.transform, false);
                Place((RectTransform)go.transform, new Vector2((i % 3) / 3f, 1 - (i / 3 + 1) / 4f),
                    new Vector2((i % 3 + 1) / 3f, 1 - (i / 3) / 4f), false);
                ((RectTransform)go.transform).offsetMin = Vector2.one;
                ((RectTransform)go.transform).offsetMax = -Vector2.one;
                go.GetComponent<Image>().color = new Color(.3f, .32f, .44f);
                var button = keys[i] = go.GetComponent<Button>();
                button.onClick.AddListener(() =>
                {
                    var text = dialog.input.text;
                    dialog.input.text = symbol == "⌫" ? (text.Length == 0 ? "" : text.Substring(0, text.Length - 1))
                        : text.Length < dialog.input.characterLimit ? text + symbol : text;
                });
                var label = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
                label.transform.SetParent(go.transform, false);
                Place((RectTransform)label.transform, Vector2.zero, Vector2.one, false);
                var textLabel = label.GetComponent<TextMeshProUGUI>();
                textLabel.text = symbol;
                textLabel.alignment = TextAlignmentOptions.Center;
                textLabel.raycastTarget = false;
                NativeLocalizedText.BindFont(textLabel, dialog.text);
                NativeLocalizedText.MatchFontSize(textLabel, dialog.text);
            }
            for (int i = 0; i < keys.Length; i++)
                keys[i].navigation = new Navigation { mode = Navigation.Mode.Explicit,
                    selectOnUp = i < 3 ? dialog.noButton : keys[i - 3],
                    selectOnDown = i >= 9 ? dialog.noButton : keys[i + 3],
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
            decrease = AdjustmentButton("−", 0, .18f, () => Adjust(-1));
            useOriginal = AdjustmentButton(T(MultiplayerRulesLocalization.UseOriginalAction), .20f, .80f, () =>
            {
                dialog.input.text = "";
                if (dialog.yesButton.interactable) confirm();
            });
            increase = AdjustmentButton("+", .82f, 1, () => Adjust(1));
        }

        private Button AdjustmentButton(string label, float left, float right, Action clicked)
        {
            var go = new GameObject("Value Action", typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(adjustments.transform, false);
            Place((RectTransform)go.transform, new Vector2(left, 0), new Vector2(right, 1), false);
            go.GetComponent<Image>().color = new Color(.3f, .32f, .44f);
            var button = go.GetComponent<Button>();
            button.onClick.AddListener(() => clicked());
            var textObject = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            textObject.transform.SetParent(go.transform, false);
            var text = textObject.GetComponent<TextMeshProUGUI>();
            Place(text.rectTransform, Vector2.zero, Vector2.one, false);
            text.text = label;
            text.alignment = TextAlignmentOptions.Center;
            text.raycastTarget = false;
            NativeLocalizedText.BindFont(text, dialog.text);
            NativeLocalizedText.MatchFontSize(text, dialog.text);
            return button;
        }

        private void Adjust(int direction) => dialog.input.text = MultiplayerRuleInput.Adjust(dialog.input.text, definition, originalValue, direction);

        private void RefreshInputMode()
        {
            usingKeypad = ControlsChangeHandler.Current?.IsUsingKeyboardAndMouse == false;
            keypad.SetActive(usingKeypad);
            Place(dialog.text.rectTransform, new Vector2(.05f, usingKeypad ? .74f : .65f), new Vector2(.95f, .97f), false);
            Place((RectTransform)dialog.input.transform, new Vector2(.15f, usingKeypad ? .65f : .56f), new Vector2(.85f, usingKeypad ? .72f : .63f), false);
            Place((RectTransform)adjustments.transform, new Vector2(.08f, usingKeypad ? .46f : .15f), new Vector2(.92f, usingKeypad ? .53f : .24f), false);
            Place(feedback.rectTransform, new Vector2(.05f, usingKeypad ? .55f : .26f), new Vector2(.95f, usingKeypad ? .63f : .54f), false);
            var actions = new[] { decrease, useOriginal, increase };
            for (int i = 0; i < actions.Length; i++)
                actions[i].navigation = new Navigation { mode = Navigation.Mode.Explicit,
                    selectOnLeft = actions[(i + 2) % 3], selectOnRight = actions[(i + 1) % 3],
                    selectOnUp = dialog.input, selectOnDown = usingKeypad ? keys[0] : dialog.yesButton };
            var inputNavigation = dialog.input.navigation;
            inputNavigation.selectOnDown = decrease;
            dialog.input.navigation = inputNavigation;
            for (int i = 0; i < 3; i++)
            {
                var nav = keys[i].navigation; nav.selectOnUp = actions[i]; keys[i].navigation = nav;
            }
            foreach (var button in new[] { dialog.yesButton, dialog.noButton })
            {
                var nav = button.navigation; nav.selectOnUp = usingKeypad ? keys[10] : useOriginal; button.navigation = nav;
            }
            InitialSelection = usingKeypad ? decrease.gameObject : dialog.input.gameObject;
            if (EventSystem.current?.currentSelectedGameObject is GameObject selected &&
                !selected.activeInHierarchy && selected.transform.IsChildOf(keypad.transform))
                EventSystem.current.SetSelectedGameObject(InitialSelection);
        }

        private void Place(RectTransform rect, Vector2 min, Vector2 max, bool save = true)
        {
            var oldMin = rect.anchorMin; var oldMax = rect.anchorMax;
            var oldPosition = rect.anchoredPosition; var oldSize = rect.sizeDelta;
            if (save) restore.Add(() => { rect.anchorMin = oldMin; rect.anchorMax = oldMax; rect.sizeDelta = oldSize; rect.anchoredPosition = oldPosition; });
            rect.anchorMin = min; rect.anchorMax = max;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }

        internal void RefreshFeedback(string text)
        {
            var error = MultiplayerRuleInput.Validate(text, definition, out _);
            string message = error switch
            {
                MultiplayerRuleInputError.Number => T(MultiplayerRulesLocalization.InvalidNumber),
                MultiplayerRuleInputError.Range => string.Format(T(MultiplayerRulesLocalization.InvalidRange), Format(definition.Minimum), Format(definition.Maximum)),
                MultiplayerRuleInputError.Step => string.Format(T(MultiplayerRulesLocalization.InvalidStep), Format(definition.Step), Format(definition.Minimum)),
                _ => T(string.IsNullOrWhiteSpace(text) ? MultiplayerRulesLocalization.InputDefault : MultiplayerRulesLocalization.InputValid)
            };
            var keyboard = Keyboard.current;
            if (keyboard != null && ControlsChangeHandler.Current?.IsUsingKeyboardAndMouse != false)
                message += "\n" + string.Format(T(MultiplayerRulesLocalization.InputActions),
                    keyboard.enterKey.displayName, keyboard.escapeKey.displayName, keyboard.tabKey.displayName, keyboard.leftShiftKey.displayName);
            feedback.text = message;
            feedback.color = error == MultiplayerRuleInputError.None ? Color.white : new Color(1f, .65f, .35f);
        }

        private void Update()
        {
            if (dialog == null || !dialog.IsOpened || !dialog.IsControlEnabled) return;
            if (usingKeypad != (ControlsChangeHandler.Current?.IsUsingKeyboardAndMouse == false))
            {
                RefreshInputMode();
                RefreshFeedback(dialog.input.text);
            }
            var keyboard = Keyboard.current;
            if (keyboard == null || !keyboard.tabKey.wasPressedThisFrame || EventSystem.current == null) return;
            var controls = new Selectable[] { dialog.input, decrease, useOriginal, increase, dialog.yesButton, dialog.noButton }
                .Where(c => c.IsInteractable()).ToArray();
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
            if (!dialog.input.wasCanceled && dialog.IsOpened && dialog.yesButton.interactable) confirm();
        }

        internal void Release()
        {
            enabled = false;
            foreach (var action in restore) action();
            restore.Clear();
            if (feedback != null) Destroy(feedback.gameObject);
            if (keypad != null) Destroy(keypad);
            if (adjustments != null) Destroy(adjustments);
            Destroy(this);
        }

        private string Format(float value) => MultiplayerRulesLocalization.FormatValue(value, definition.Unit);
        private static string T(string key) => ModLocalization.Get(key);
    }
}
