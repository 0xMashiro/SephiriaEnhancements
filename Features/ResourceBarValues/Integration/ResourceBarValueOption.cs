using SephiriaEnhancements.Runtime;
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
            if (!FeatureFailure.IsAvailable(FeatureId.ResourceBarValues))
            {
                return;
            }

            try
            {
                OnEnableCore();
            }
            catch (System.Exception exception)
            {
                FeatureFailure.Disable(FeatureId.ResourceBarValues, exception);
                return;
            }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private void OnEnableCore()
        {
            if (box == null)
                return;
            box.numberOfElements = 2;
            box.overflowType = UI_HorizontalSelectionBox.OverflowType.Repeat;
            box.OnValueChanged += Changed;
            Refresh();
        }

        private void Refresh()
        {
            if (!FeatureFailure.IsAvailable(FeatureId.ResourceBarValues))
            {
                return;
            }

            try
            {
                RefreshCore();
            }
            catch (System.Exception exception)
            {
                FeatureFailure.Disable(FeatureId.ResourceBarValues, exception);
                return;
            }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private void RefreshCore()
        {
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
            if (!FeatureFailure.IsAvailable(FeatureId.ResourceBarValues))
            {
                return;
            }

            try
            {
                ChangedCore(value);
            }
            catch (System.Exception exception)
            {
                FeatureFailure.Disable(FeatureId.ResourceBarValues, exception);
                return;
            }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private void ChangedCore(int value)
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
