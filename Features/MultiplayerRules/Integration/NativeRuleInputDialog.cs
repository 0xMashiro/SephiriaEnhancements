using System;
using SephiriaEnhancements.Configuration;
using SephiriaEnhancements.Integration;
using SephiriaEnhancements.MultiplayerRules.Presentation;
using UnityEngine.InputSystem;

namespace SephiriaEnhancements.MultiplayerRules.Integration
{
    internal static class NativeRuleInputDialog
    {
        internal static UI_MessageBox_InputYesNo Open(string prompt, string initial, MultiplayerRuleDefinition definition, float originalValue,
            Func<bool> canEdit, Action<MultiplayerRuleValue<float>> save, UnityEngine.GameObject returnSelection)
        {
            return NativeNumberInputDialog.Open(new NumberInputOptions
            {
                Prompt = prompt, Initial = initial, Placeholder = "", CharacterLimit = 16,
                ConfirmKey = MultiplayerRulesLocalization.ConfirmEdit,
                CancelKey = MultiplayerRulesLocalization.CancelEdit,
                ClearKey = MultiplayerRulesLocalization.UseOriginalAction,
                CanEdit = canEdit,
                ValidateCharacter = MultiplayerRuleInput.ValidateCharacter,
                IsValid = text => MultiplayerRuleInput.TryParse(text, definition, out _),
                Adjust = (text, direction) => MultiplayerRuleInput.Adjust(text, definition, originalValue, direction),
                Save = text => { if (MultiplayerRuleInput.TryParse(text, definition, out var value)) save(value); },
                Feedback = text => Feedback(text, definition)
            }, returnSelection);
        }

        private static string Feedback(string text, MultiplayerRuleDefinition definition)
        {
            var error = MultiplayerRuleInput.Validate(text, definition, out _);
            string message = error switch
            {
                MultiplayerRuleInputError.Number => T(MultiplayerRulesLocalization.InvalidNumber),
                MultiplayerRuleInputError.Range => string.Format(T(MultiplayerRulesLocalization.InvalidRange),
                    MultiplayerRulesLocalization.FormatValue(definition.Minimum, definition.Unit),
                    MultiplayerRulesLocalization.FormatValue(definition.Maximum, definition.Unit)),
                MultiplayerRuleInputError.Step => string.Format(T(MultiplayerRulesLocalization.InvalidStep),
                    MultiplayerRulesLocalization.FormatValue(definition.Step, definition.Unit),
                    MultiplayerRulesLocalization.FormatValue(definition.Minimum, definition.Unit)),
                _ => T(string.IsNullOrWhiteSpace(text) ? MultiplayerRulesLocalization.InputDefault : MultiplayerRulesLocalization.InputValid)
            };
            var keyboard = Keyboard.current;
            if (keyboard != null && ControlsChangeHandler.Current?.IsUsingKeyboardAndMouse != false)
                message += "\n" + string.Format(T(MultiplayerRulesLocalization.InputActions),
                    keyboard.enterKey.displayName, keyboard.escapeKey.displayName, keyboard.tabKey.displayName, keyboard.leftShiftKey.displayName);
            return message;
        }

        private static string T(string key) => ModLocalization.Get(key);
    }
}
