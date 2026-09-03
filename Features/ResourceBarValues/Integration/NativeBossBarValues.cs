using System;
using System.Collections.Generic;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SephiriaEnhancements.ResourceBarValues.Integration
{
    internal static class NativeBossBarValues
    {
        private static readonly AccessTools.FieldRef<UI_BossHPBar, UnitAvatar> Boss =
            AccessTools.FieldRefAccess<UI_BossHPBar, UnitAvatar>("bossAvatar");
        private static readonly AccessTools.FieldRef<UI_BossHPBar, UnitAI_BossBasic> BossAI =
            AccessTools.FieldRefAccess<UI_BossHPBar, UnitAI_BossBasic>("bossAI");
        private static readonly AccessTools.FieldRef<UI_LibraryBossHPBar, UnitAvatar> Golem =
            AccessTools.FieldRefAccess<UI_LibraryBossHPBar, UnitAvatar>("golemAvatar");
        private static readonly AccessTools.FieldRef<UI_RootDemonBossHPBar, List<Unit_RootDemonPart>> Roots =
            AccessTools.FieldRefAccess<UI_RootDemonBossHPBar, List<Unit_RootDemonPart>>("rootAvatars");

        internal static void Configure(UI_BossHPBar owner)
        {
            var view = NativeResourceBarValueView.GetOrAdd(owner);
            view.Clear();
            if (owner is UI_RootDemonBossHPBar roots)
            {
                for (int index = 0; index < roots.rootBarImages.Length; index++)
                {
                    int part = index;
                    AddHealth(view, roots.rootBarImages[index], owner.nameText, () =>
                    {
                        var targets = Roots(roots);
                        return part < targets.Count ? targets[part] : null;
                    });
                }
                return;
            }
            if (owner is UI_LibraryBossHPBar library)
            {
                AddHealth(view, library.golemHPBarImage, owner.nameText, () => Golem(library));
                TextMeshProUGUI lifeValue = AddBar(view, owner.barImage, owner.nameText, () =>
                {
                    UnitAvatar target = Boss(owner);
                    UnitAI_BossBasic ai = BossAI(owner);
                    return target != null && ai != null
                        ? ResourceBarValueFormatter.RemainingLivesHealth(target.Networkhp,
                            target.MaxHp, ai.CurrentLife, ai.Life) : string.Empty;
                });
                // The native life bar is only two pixels high, between the name
                // and golem bar. Put its value beside it, not over either label.
                lifeValue.rectTransform.anchorMin = new Vector2(1f, 0f);
                lifeValue.rectTransform.anchorMax = new Vector2(1f, 0f);
                lifeValue.rectTransform.pivot = new Vector2(0f, 0.5f);
                lifeValue.rectTransform.anchoredPosition = new Vector2(3f, -2f);
                lifeValue.rectTransform.sizeDelta = new Vector2(70f, 8f);
                lifeValue.alignment = TextAlignmentOptions.Left;
                return;
            }
            AddHealth(view, owner.barImage, owner.nameText, () => Boss(owner));
        }

        private static void AddHealth(NativeResourceBarValueView view, Image bar,
            TextMeshProUGUI template, Func<UnitAvatar> read)
        {
            AddBar(view, bar, template, () =>
            {
                UnitAvatar target = read();
                return target != null ? ResourceBarValueFormatter.HealthWithShield(
                    target.Networkhp, target.MaxHp, target.Shield) : string.Empty;
            });
        }

        private static TextMeshProUGUI AddBar(NativeResourceBarValueView view, Image bar,
            TextMeshProUGUI template, Func<string> read)
        {
            // Six-unit tracks use a 6.5-unit value beside a ten-unit name. Refresh
            // the native font scale, and render above the shield/lost-HP siblings.
            var text = view.Add(bar.rectTransform.parent as RectTransform, template,
                () => bar.gameObject.activeInHierarchy ? read() : string.Empty,
                () => template.fontSize * 0.65f, "Resource Bar Values — Boss Health");
            RectTransform source = bar.rectTransform;
            RectTransform rect = text.rectTransform;
            rect.anchorMin = source.anchorMin;
            rect.anchorMax = source.anchorMax;
            rect.pivot = source.pivot;
            rect.anchoredPosition = source.anchoredPosition;
            rect.sizeDelta = source.sizeDelta;
            return text;
        }
    }

    // Every current large-bar subtype either inherits SetBoss or calls this base
    // method. Roots and the library golem are read after their later native binding.
    [HarmonyPatch(typeof(UI_BossHPBar), nameof(UI_BossHPBar.SetBoss))]
    internal static class NativeBossBarValuesPatch
    {
        private static void Postfix(UI_BossHPBar __instance) => NativeBossBarValues.Configure(__instance);
    }

}
