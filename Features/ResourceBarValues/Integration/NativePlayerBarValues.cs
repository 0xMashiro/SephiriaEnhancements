using HarmonyLib;
using TMPro;
using UnityEngine;

namespace SephiriaEnhancements.ResourceBarValues.Integration
{
    [HarmonyPatch(typeof(UI_MultiplayerHPBar), nameof(UI_MultiplayerHPBar.SetSpawner))]
    internal static class NativePlayerBarValuesPatch
    {
        private static void Postfix(UI_MultiplayerHPBar __instance, PlayerSpawner __0)
        {
            UI_MultiplayerHPBar owner = __instance;
            PlayerAvatar player = __0.GetComponent<PlayerAvatar>();
            var view = NativeResourceBarValueView.GetOrAdd(owner);
            view.Clear();
            RectTransform frame = owner.hpBar.rectTransform.parent as RectTransform;
            TextMeshProUGUI template = owner.pingText;
            var text = view.Add(frame, template, () =>
            {
                if (player == null || player.MaxHp <= 0f) return string.Empty;
                string health = ResourceBarValueFormatter.HealthWithShield(
                    player.IsDead ? 0f : player.Networkhp, player.MaxHp, player.Shield);
                string mana = ResourceBarValueFormatter.Mana(player.mp, player.MaxMp,
                    player.reservedMp, ReserveLabel());
                return "<color=#FFD4D4>HP " + health + "</color>   <color=#C5E2FF>MP " + mana + "</color>";
            }, () => template.fontSize, "Resource Bar Values — Player Resources");
            text.alignment = TextAlignmentOptions.Left;
            var rect = text.rectTransform;
            rect.anchorMin = new Vector2(1f, 0.5f);
            rect.anchorMax = new Vector2(1f, 0.5f);
            rect.pivot = new Vector2(0f, 0.5f);
            rect.anchoredPosition = new Vector2(4f, 0f);
            rect.sizeDelta = new Vector2(230f, frame.rect.height);

            RectTransform ping = owner.pingText.rectTransform;
            Vector2 originalPing = ping.anchoredPosition;
            view.RefreshLayout = enabled =>
            {
                if (ping == null) return;
                if (!enabled || !text.gameObject.activeSelf)
                {
                    ping.anchoredPosition = originalPing;
                    return;
                }
                float width = Mathf.Min(text.preferredWidth, rect.rect.width);
                Vector3 end = rect.TransformPoint(new Vector3(width, 0f, 0f));
                Vector3 local = ping.parent.InverseTransformPoint(end);
                Vector3 position = ping.localPosition;
                position.x = local.x + 6f + ping.rect.width * ping.pivot.x;
                ping.localPosition = position;
            };
            view.RestoreLayout = () =>
            {
                if (ping != null) ping.anchoredPosition = originalPing;
            };
        }

        private static string ReserveLabel()
        {
            // Reuse the native resource term; no unrelated feature's translations.
            return Loc._("Keyword_ReservedMP_Name");
        }
    }
}
