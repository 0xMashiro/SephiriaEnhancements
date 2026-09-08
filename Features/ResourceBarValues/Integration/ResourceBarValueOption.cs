using UnityEngine;

namespace SephiriaEnhancements.ResourceBarValues
{
    internal sealed class ResourceBarValueOption : MonoBehaviour
    {
        private UI_HorizontalSelectionBox box;
        private UI_LocalizationStringText valueText;
        private ResourceBarValueSetting setting;

        internal void Configure(UI_HorizontalSelectionBox selectionBox,
            UI_LocalizationStringText text, ResourceBarValueSetting valueSetting)
        {
            box = selectionBox;
            valueText = text;
            setting = valueSetting;
        }

        private void OnEnable()
        {
            if (box == null) return;
            box.numberOfElements = 2;
            box.overflowType = UI_HorizontalSelectionBox.OverflowType.Repeat;
            box.OnValueChanged += Changed;
            bool enabled = ResourceBarValueSettings.Get(setting);
            box.ChangeValueWithoutNotify(enabled ? 1 : 0);
            RefreshText(enabled);
        }

        private void OnDisable()
        {
            if (box != null) box.OnValueChanged -= Changed;
        }

        private void Changed(int value)
        {
            bool enabled = value == 1;
            ResourceBarValueSettings.Set(setting, enabled);
            ResourceBarValueSettings.Save();
            RefreshText(enabled);
        }

        private void RefreshText(bool enabled) =>
            valueText?.UpdateKey(enabled ? ResourceBarValueLocalization.On : ResourceBarValueLocalization.Off);
    }
}
