using Mirror;
using SephiriaEnhancements.MultiplayerAccess;
using SephiriaEnhancements.MultiplayerRules;
using SephiriaEnhancements.Runtime.GameBridge;
using UnityEngine;
using TMPro;
using SephiriaEnhancements.Integration;
using SephiriaEnhancements.MultiplayerRules.Integration;

namespace SephiriaEnhancements.Configuration
{
    // Read the actual world and connection, not menu visibility or another player's floor.
    internal static class NativeSettingsInteraction
    {
        internal static SettingLockReason Reason(SettingInteractionKind kind) =>
            SettingsInteractionPolicy.Resolve(kind, NetworkClient.active, NetworkServer.active,
                DungeonManager.Instance != null,
                (DungeonManager.Instance != null && DungeonManager.Instance.isRunStarted) ||
                    MultiplayerRulesController.TryGetActivePreset(out _),
                SteamInvitation.IsRejoinInProgress || (!NetworkClient.active &&
                    (SteamInvitation.HasLastSession || PlayerLocalDataStorage.HasPendingDisconnectSapphire)),
                EnhancementsSettings.Enabled, MultiplayerExtensionDiscovery.HasDetectedExtension,
                MidRunAdmissionRuntime.IsAvailable);

        internal static bool CanEdit(SettingInteractionKind kind) => Reason(kind) == SettingLockReason.None;

        internal static void Bind(GameObject row, UI_HorizontalSelectionBox box,
            UI_LocalizationStringText value, SettingInteractionKind kind, TextMeshProUGUI fontReference)
        {
            var status = row.AddComponent<NativeSettingStatus>();
            status.Configure(box, value, kind, fontReference);
        }
    }

    internal sealed class NativeSettingStatus : MonoBehaviour
    {
        private UI_HorizontalSelectionBox box;
        private UI_LocalizationStringText value;
        private SettingInteractionKind kind;
        private float nextRefresh;
        private TextMeshProUGUI fontReference;

        internal void Configure(UI_HorizontalSelectionBox selection, UI_LocalizationStringText text,
            SettingInteractionKind interactionKind, TextMeshProUGUI reference)
        { box = selection; value = text; kind = interactionKind; fontReference = reference; }

        private void OnEnable() => nextRefresh = 0f;

        private void LateUpdate()
        {
            if (box == null || value == null || Time.unscaledTime < nextRefresh) return;
            nextRefresh = Time.unscaledTime + 0.1f;
            SettingLockReason reason = NativeSettingsInteraction.Reason(kind);
            var member = GetComponent<OptionsCategoryMember>();
            bool customAllowed = member == null || !member.RequiresCustomPreset ||
                MultiplayerRulesOptionsRefresh.DisplayedPreset() == MultiplayerRulesPreset.Custom;
            NativeHorizontalSelectionOptionState.Apply(gameObject, box,
                reason == SettingLockReason.None && customAllowed);
            // Keep the native translation key so language changes and value refreshes remain valid.
            string current = ModLocalization.Get(value.valueString.key);
            bool hostPreference = reason == SettingLockReason.HostOnly &&
                (kind == SettingInteractionKind.Admission || kind == SettingInteractionKind.Companion);
            value.text.text = hostPreference ? ModLocalization.Get(SettingsInteractionLocalization.HostOnly)
                : reason == SettingLockReason.None ? current : string.Format(
                ModLocalization.Get(SettingsInteractionLocalization.ValueWithStatus), current,
                ModLocalization.Get(SettingsInteractionLocalization.Reason(reason)));
            value.text.textWrappingMode = TextWrappingModes.NoWrap;
            if (fontReference != null)
                NativeLocalizedText.MatchFontSize(value.text, fontReference);
        }
    }
}
