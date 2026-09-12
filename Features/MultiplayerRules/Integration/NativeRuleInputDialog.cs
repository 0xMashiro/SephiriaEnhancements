using System;
using UnityEngine.EventSystems;

namespace SephiriaEnhancements.MultiplayerRules.Integration
{
    internal static class NativeRuleInputDialog
    {
        internal static UI_MessageBox_InputYesNo Open(string prompt, string initial, MultiplayerRuleDefinition definition, float originalValue,
            Func<bool> canEdit, Action<MultiplayerRuleValue<float>> save,
            UnityEngine.GameObject returnSelection)
        {
            var dialog = UIManager.Instance?.GetElement<UI_MessageBox_InputYesNo>();
            if (dialog == null || dialog.IsOpened) return null;
            dialog.Open(prompt,
                text =>
                {
                    if (canEdit() && MultiplayerRuleInput.TryParse(text, definition, out var value)) save(value);
                }, null, initial, "", true);
            int characterLimit = dialog.input.characterLimit;
            var defaultSelectable = dialog.defaultSelectable;
            dialog.input.characterLimit = 16;
            var navigation = dialog.gameObject.AddComponent<NativeRuleInputNavigation>();
            navigation.Configure(dialog, definition, originalValue, () => dialog.yesButton.onClick.Invoke());
            UnityEngine.Events.UnityAction<string> submit = navigation.Submit;
            dialog.input.onSubmit.AddListener(submit);
            UnityEngine.Events.UnityAction<string> validate = text =>
            {
                dialog.yesButton.interactable = canEdit() && MultiplayerRuleInput.TryParse(text, definition, out _);
                navigation.RefreshFeedback(text);
            };
            dialog.input.onValueChanged.AddListener(validate);
            validate(initial);
            dialog.defaultSelectable = navigation.InitialSelection;
            if (EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(navigation.InitialSelection);
            Action<UI_MessageBox> closed = null;
            closed = _ =>
            {
                dialog.input.onValueChanged.RemoveListener(validate);
                dialog.input.onSubmit.RemoveListener(submit);
                navigation.Release();
                dialog.onCloseMessageBox -= closed;
                dialog.yesButton.interactable = true;
                dialog.input.characterLimit = characterLimit;
                dialog.defaultSelectable = defaultSelectable;
                if (returnSelection != null && returnSelection.activeInHierarchy && EventSystem.current != null)
                    EventSystem.current.SetSelectedGameObject(returnSelection);
            };
            dialog.onCloseMessageBox += closed;
            return dialog;
        }
    }
}
