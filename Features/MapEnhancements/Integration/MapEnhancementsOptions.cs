using SephiriaEnhancements.Configuration;
using UnityEngine;
using static SephiriaEnhancements.Configuration.NativeOptionsRows;

namespace SephiriaEnhancements.MapEnhancements.Integration
{
    internal static class MapEnhancementsOptions
    {
        internal static void Inject(UI_OptionsPanel panel, UI_OptionBox_PartyMemberDamage template, Transform section)
        {
            if (panel.GetComponentInChildren<ShowHiddenRoomsOption>(true) == null)
            {
                CreateShowHiddenRoomsRow(template, section);
            }
        }

        private static void CreateShowHiddenRoomsRow(UI_OptionBox_PartyMemberDamage template,
            Transform section)
        {
            GameObject row = CloneRow(template, section,
                "Option_SephiriaEnhancements_ShowHiddenRooms",
                MapEnhancementsLocalization.SettingShowHiddenRooms,
                MapEnhancementsLocalization.HelpShowHiddenRooms, 5,
                out UI_HorizontalSelectionBox box,
                out UI_LocalizationStringText valueText);
            row.AddComponent<ShowHiddenRoomsOption>().Configure(box, valueText);
            MarkCategory(row, OptionsCategory.General);
            row.SetActive(true);
        }
    }

    internal sealed class ShowHiddenRoomsOption : MonoBehaviour
    {
        private UI_HorizontalSelectionBox box;
        private UI_LocalizationStringText valueText;

        internal UI_HorizontalSelectionBox Box => box;

        internal void Configure(UI_HorizontalSelectionBox selectionBox,
            UI_LocalizationStringText text)
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
            int value = MapEnhancementsSettings.ShowHiddenRooms ? 1 : 0;
            box.ChangeValueWithoutNotify(value);
            valueText?.UpdateKey(value == 1
                ? MapEnhancementsLocalization.On
                : MapEnhancementsLocalization.Off);
        }

        private void OnDisable()
        {
            if (box != null) box.OnValueChanged -= Changed;
        }

        private void Changed(int value)
        {
            MapEnhancementsSettings.ShowHiddenRooms = value == 1;
            EnhancementsSettings.Save();
            valueText?.UpdateKey(value == 1
                ? MapEnhancementsLocalization.On
                : MapEnhancementsLocalization.Off);
        }
    }
}
