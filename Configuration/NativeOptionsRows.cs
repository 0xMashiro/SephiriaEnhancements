using UnityEngine;

namespace SephiriaEnhancements.Configuration
{
    internal static class NativeOptionsRows
    {
        internal static GameObject CloneRow(UI_OptionBox_PartyMemberDamage template,
            Transform section, string name, string labelKey, string helpKey, int siblingOffset,
            out UI_HorizontalSelectionBox box, out UI_LocalizationStringText valueText)
        {
            GameObject row = Object.Instantiate(template.gameObject, template.transform.parent);
            row.name = name;
            row.SetActive(false);

            UI_OptionBox_PartyMemberDamage old = row.GetComponent<UI_OptionBox_PartyMemberDamage>();
            box = old.box;
            valueText = old.valueText;
            Object.DestroyImmediate(old);

            UI_LocalizationStringText[] labels = row.GetComponentsInChildren<UI_LocalizationStringText>(true);
            foreach (UI_LocalizationStringText label in labels)
            {
                if (label != valueText)
                {
                    label.UpdateKey(labelKey);
                    break;
                }
            }

            GameObject helpTarget = box != null ? box.gameObject : row;
            UI_CommonTooltipOpener help = helpTarget.AddComponent<UI_CommonTooltipOpener>();
            help.tooltipName = new LocalizedString(labelKey);
            help.tooltipContext = new LocalizedString(helpKey);
            help.offset = new Vector2(12f, 0f);
            help.UpdateTooltipData();

            row.transform.SetSiblingIndex(section.GetSiblingIndex() + siblingOffset);
            row.AddComponent<ModOptionsNavigationEntry>().Configure(box);
            return row;
        }

        internal static void MarkCategory(GameObject target,
            OptionsCategory category, bool requiresCustomPreset = false,
            int multiplayerRuleGroup = -1)
        {
            target.AddComponent<OptionsCategoryMember>().Configure(category,
                requiresCustomPreset, multiplayerRuleGroup);
        }
    }
}
