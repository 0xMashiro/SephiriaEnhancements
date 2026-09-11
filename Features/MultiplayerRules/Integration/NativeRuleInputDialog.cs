using System;
using UnityEngine.EventSystems;

namespace SephiriaEnhancements.MultiplayerRules.Integration
{
    internal static class NativeRuleInputDialog
    {
        internal static UI_MessageBox_InputYesNo Open(string prompt, string initial, MultiplayerRuleDefinition definition,
            Func<bool> canEdit, Action<MultiplayerRuleValue<float>> save,
            UnityEngine.GameObject returnSelection)
        {
            var holder = UIManager.Instance?.GetElement<UI_MessageBoxHolder>();
            if (holder == null) return null;
            var dialog = (UI_MessageBox_InputYesNo)holder.OpenInputYesNoPrefab(prompt,
                text =>
                {
                    if (canEdit() && MultiplayerRuleInput.TryParse(text, definition, out var value)) save(value);
                }, null, initial, "", true);
            dialog.input.characterLimit = 16;
            dialog.input.onValueChanged.AddListener(text =>
                dialog.yesButton.interactable = canEdit() && MultiplayerRuleInput.TryParse(text, definition, out _));
            dialog.yesButton.interactable = canEdit() && MultiplayerRuleInput.TryParse(initial, definition, out _);
            dialog.defaultSelectable = dialog.input.gameObject;
            if (EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(dialog.input.gameObject);
            RestoreSelection(dialog, returnSelection);
            return dialog;
        }

        private static void RestoreSelection(UI_MessageBox dialog, UnityEngine.GameObject returnSelection)
        {
            dialog.onCloseMessageBox += _ =>
            {
                if (returnSelection != null && returnSelection.activeInHierarchy && EventSystem.current != null)
                    EventSystem.current.SetSelectedGameObject(returnSelection);
            };
        }
    }
}
