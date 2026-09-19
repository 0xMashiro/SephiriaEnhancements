using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace SephiriaEnhancements.Integration
{
    internal sealed class NumberInputOptions
    {
        internal string Prompt, Initial, Placeholder, ConfirmKey, CancelKey, ClearKey;
        internal int CharacterLimit;
        internal bool Signed;
        internal Func<bool> CanEdit;
        internal Func<string, bool> IsValid;
        internal Func<string, string> Feedback;
        internal TMP_InputField.OnValidateInput ValidateCharacter;
        internal Func<string, int, string> Adjust;
        internal Action<string> Save;
    }

    internal static class NativeNumberInputDialog
    {
        private static UI_MessageBox_InputYesNo active;

        internal static UI_MessageBox_InputYesNo Open(NumberInputOptions options, GameObject returnSelection)
        {
            var dialog = UIManager.Instance?.GetElement<UI_MessageBox_InputYesNo>();
            if (dialog == null || dialog.IsOpened || !options.CanEdit()) return null;
            int characterLimit = dialog.input.characterLimit;
            bool interactable = dialog.yesButton.interactable;
            var defaultSelectable = dialog.defaultSelectable;
            dialog.Open(options.Prompt, text =>
            {
                if (options.CanEdit() && options.IsValid(text)) options.Save(text);
            }, null, options.Initial, options.Placeholder, true, 0);
            active = dialog;
            var navigation = dialog.gameObject.AddComponent<NativeNumberInputNavigation>();
            Action<UI_MessageBox> closed = null;
            UnityEngine.Events.UnityAction<string> submit = navigation.Submit;
            UnityEngine.Events.UnityAction<string> validate = _ =>
            {
                dialog.input.ForceLabelUpdate();
                navigation.RefreshFeedback();
            };
            closed = _ =>
            {
                active = null;
                dialog.input.onValueChanged.RemoveListener(validate);
                dialog.input.onSubmit.RemoveListener(submit);
                navigation.Release();
                dialog.onCloseMessageBox -= closed;
                dialog.yesButton.interactable = interactable;
                dialog.input.characterLimit = characterLimit;
                dialog.defaultSelectable = defaultSelectable;
                // Native Close invokes this event before restoring the parent's control.
                // Restore focus after that handoff, otherwise controller defaults replace it.
                var root = dialog.ParentRoot;
                if (dialog.hasControl && root != null)
                {
                    Action<UIBase> removed = null;
                    removed = control =>
                    {
                        if (control != dialog) return;
                        root.onControlRemoved -= removed;
                        RestoreFocus(returnSelection);
                    };
                    root.onControlRemoved += removed;
                }
                else RestoreFocus(returnSelection);
            };
            dialog.onCloseMessageBox += closed;
            try
            {
                navigation.Configure(dialog, options);
                dialog.input.onSubmit.AddListener(submit);
                dialog.input.onValueChanged.AddListener(validate);
                navigation.RefreshFeedback();
                dialog.defaultSelectable = navigation.InitialSelection;
                if (EventSystem.current != null)
                    EventSystem.current.SetSelectedGameObject(navigation.InitialSelection);
                return dialog;
            }
            catch
            {
                dialog.Close();
                throw;
            }
        }

        internal static void CloseActive()
        {
            if (active != null) active.Close();
            active = null;
        }

        private static void RestoreFocus(GameObject target)
        {
            if (target == null || !target.activeInHierarchy || EventSystem.current == null) return;
            var owner = target.GetComponentInParent<UIBase>();
            if (owner != null && !owner.IsControlEnabled) return;
            var selectable = target.GetComponent<UnityEngine.UI.Selectable>();
            if (selectable != null && !selectable.IsInteractable()) return;
            EventSystem.current.SetSelectedGameObject(target);
        }
    }
}
