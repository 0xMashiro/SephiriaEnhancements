using SephiriaEnhancements.Configuration;
using SephiriaEnhancements.MultiplayerRules.Presentation;
using UnityEngine;

namespace SephiriaEnhancements.MultiplayerRules.Integration
{
    internal static class MultiplayerRulesOptions
    {
        internal static void Inject(UI_OptionsPanel panel, UI_OptionBox_PartyMemberDamage template,
            Transform section, OptionsCategoryController categoryController)
        {
            if (panel.GetComponentInChildren<MultiplayerRulesLobbyEntry>(true) != null) return;
            var row = NativeOptionsRows.CloneRow(template, section,
                "Option_SephiriaEnhancements_MultiplayerRulesLobby",
                MultiplayerRulesLocalization.LobbyTitle, MultiplayerRulesLocalization.LobbyEntryHelp, 2,
                out var box, out var value);
            row.AddComponent<MultiplayerRulesLobbyEntry>().Configure(box, value);
            NativeOptionsRows.MarkCategory(row, OptionsCategory.Multiplayer);
            row.SetActive(true);
        }
    }

    internal sealed class MultiplayerRulesLobbyEntry : MonoBehaviour
    {
        private UI_HorizontalSelectionBox box;
        private UI_LocalizationStringText value;
        internal void Configure(UI_HorizontalSelectionBox selection, UI_LocalizationStringText text)
        {
            box = selection;
            value = text;
            box.numberOfElements = 1;
            box.gameObject.AddComponent<NativeOptionActivation>().Configure(() => NativeLobbyRulesPanel.Show());
        }
        private void Update()
        {
            NativeHorizontalSelectionOptionState.Apply(gameObject, box, MultiplayerRulesLobbyContext.IsInLobby);
            value.UpdateKey(MultiplayerRulesLocalization.LobbyTitle);
        }
    }


}
