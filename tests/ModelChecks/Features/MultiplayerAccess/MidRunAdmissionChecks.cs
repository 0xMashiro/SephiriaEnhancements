using SephiriaEnhancements.MultiplayerAccess;
using SephiriaEnhancements.MultiplayerAccess.Presentation;

namespace SephiriaEnhancements.ModelChecks.Features.MultiplayerAccess;

internal static class MidRunAdmissionChecks
{
    internal static void Run()
    {
        if (!MidRunAdmissionPolicy.DefaultEnabled ||
            !MidRunAdmissionPolicy.CanOwnAdmission(true, true, true, true,
                true, true, false) ||
            MidRunAdmissionPolicy.CanOwnAdmission(false, true, true, true,
                true, true, false) ||
            MidRunAdmissionPolicy.CanOwnAdmission(true, false, true, true,
                true, true, false) ||
            MidRunAdmissionPolicy.CanOwnAdmission(true, true, false, true,
                true, true, false) ||
            MidRunAdmissionPolicy.CanOwnAdmission(true, true, true, false,
                true, true, false) ||
            MidRunAdmissionPolicy.CanOwnAdmission(true, true, true, true,
                false, true, false) ||
            MidRunAdmissionPolicy.CanOwnAdmission(true, true, true, true,
                true, false, false) ||
            MidRunAdmissionPolicy.CanOwnAdmission(true, true, true, true,
                true, true, true) ||
            !MidRunAdmissionPolicy.CanEnableNativeReconnect(true, true, true, false) ||
            MidRunAdmissionPolicy.CanEnableNativeReconnect(true, true, true, true))
        {
            throw new InvalidOperationException(
                "mid-run admission must default on, remain host-owned and " +
                "per-player-save only, and stay passive beside an extension");
        }

        var multiplayerAccessTexts =
            new Dictionary<(string Language, string Key), string>();
        MultiplayerAccessLocalization.Register(
            (language, key, value) =>
                multiplayerAccessTexts[(language, key)] = value,
            new[] { "en-US", "zh-CN", "fr-FR" });
        if (multiplayerAccessTexts[("zh-CN",
                MultiplayerAccessLocalization.AllowMidRunJoinSetting)] !=
                "新玩家中途加入" ||
            !multiplayerAccessTexts.ContainsKey(("fr-FR",
                MultiplayerAccessLocalization.AllowMidRunJoinHelp)))
        {
            throw new InvalidOperationException(
                "multiplayer-access localization must be complete with en-US fallback");
        }
    }
}
