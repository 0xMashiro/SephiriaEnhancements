#if SEPHIRIA_ENHANCEMENTS_DEVTOOLS
using SephiriaEnhancements.Diagnostics;
using UnityEngine;

namespace SephiriaEnhancements.DeveloperTools
{
    internal sealed class InventoryReproductionOption : MonoBehaviour
    {
        private UI_HorizontalSelectionBox box;
        private UI_LocalizationStringText valueText;

        internal void Configure(UI_HorizontalSelectionBox selectionBox, UI_LocalizationStringText text)
        {
            box = selectionBox;
            valueText = text;
        }

        private void OnEnable()
        {
            if (box == null) return;
            box.numberOfElements = 2;
            box.overflowType = UI_HorizontalSelectionBox.OverflowType.Repeat;
            box.OnValueChanged += Changed;
            box.ChangeValueWithoutNotify(InventoryReproductionSettings.RecordAllResults ? 1 : 0);
            RefreshText();
        }

        private void OnDisable()
        {
            if (box != null) box.OnValueChanged -= Changed;
        }

        private void Changed(int value)
        {
            InventoryReproductionSettings.RecordAllResults = value == 1;
            RefreshText();
        }

        private void RefreshText() => valueText?.UpdateKey(InventoryReproductionSettings.RecordAllResults
            ? InventoryReproductionLocalization.On : InventoryReproductionLocalization.Off);
    }
}
#endif
