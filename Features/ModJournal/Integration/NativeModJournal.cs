using SephiriaEnhancements.Runtime;
using System;
using System.Collections.Generic;
using HarmonyLib;
using SephiriaEnhancements.Configuration;
using SephiriaEnhancements.Integration;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SephiriaEnhancements.ModJournal.Integration
{
    // Journal lore is a native UI boundary; mod information is not a collectible lore entity.
    internal sealed class NativeModJournal : MonoBehaviour
    {
        private const string Category = "SephiriaEnhancements";
        private static readonly Action<UI_JournalContent_Lore, UI_JournalPanel_SearchOptionButton>
            SelectCategory = AccessTools.MethodDelegate<
                Action<UI_JournalContent_Lore, UI_JournalPanel_SearchOptionButton>>(
                    AccessTools.Method(typeof(UI_JournalContent_Lore), "OnLocationIconClick"));

        private static readonly string[] Addresses =
        {
            null,
            "https://www.nexusmods.com/sephiria/mods/24",
            "https://github.com/0xMashiro/SephiriaEnhancements",
            "https://live.bilibili.com/24299563",
            "https://space.bilibili.com/609288952"
        };

        private UI_JournalContent_Lore panel;
        private UI_JournalPanel_SearchOptionButton category;
        private readonly List<GameObject> entries = new();
        private readonly List<TextMeshProUGUI> labels = new();
        private string language;
        private bool selected;
        private int selectedEntry;

        internal void Build(UI_JournalContent_Lore owner)
        {
            panel = owner;
            // Several localized fonts lack hearts; only this symbol uses the native Korean font.
            MaterialReferenceManager.AddFontAsset(LocalizationManager.Instance.fontAssets["Galmuri"]);
            category = Instantiate(panel.locationIconOriginal, panel.locationIconContainer);
            category.gameObject.SetActive(true);
            category.Initialize(Category, Category, button => SelectCategory(panel, button));
            category.transform.Find("RedDot").gameObject.SetActive(false);

            for (int i = 0; i < Addresses.Length; i++)
            {
                int index = i;
                UI_JournalPanel_LoreIcon icon = Instantiate(panel.loreIconOriginal,
                    panel.loreIconContainer);
                GameObject entry = icon.gameObject;
                labels.Add(icon.text);
                // Only the native presentation is reused; selection never touches lore or saves.
                Destroy(icon);
                entry.transform.Find("RedDot").gameObject.SetActive(false);
                Button button = entry.GetComponent<Button>();
                button.onClick = new Button.ButtonClickedEvent();
                button.onClick.AddListener(() =>
                {
                    ShowEntry(index);
                    if (Addresses[index] != null) Application.OpenURL(Addresses[index]);
                });
                entry.AddComponent<UI_JournalPanel_TutorialIcon>().Initialize(() => ShowEntry(index));
                entry.SetActive(false);
                entries.Add(entry);
            }
            RefreshTexts();
        }

        internal void Select(string key)
        {
            selected = key == Category;
            foreach (GameObject entry in entries) entry.SetActive(selected);
            if (selected) ShowEntry(0);
        }

        private void ShowEntry(int index)
        {
            if (!selected) return;
            selectedEntry = index;
            panel.loreInfo.SetActive(true);
            panel.loreImageSlot.SetActive(false);
            panel.loreInfo_Title.text = index == 0
                ? "SEPHIRIA ENHANCEMENTS · by 0xMashiro" : EntryName(index);
            panel.loreInfo_Description.text = index == 0
                ? string.Format(ModLocalization.Get(ModJournalLocalization.Description),
                    "<font=\"Galmuri\"><color=#F49AC2>♥</color></font>")
                : Addresses[index];
            LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)panel.loreInfo.transform);
            ScrollRect scroll = panel.loreInfo_Description.GetComponentInParent<ScrollRect>();
            scroll.StopMovement();
            scroll.verticalNormalizedPosition = 1f;
        }

        private static string EntryName(int index)
        {
            return index == 0 ? ModLocalization.Get(ModJournalLocalization.About)
                : index == 1 ? "Nexus Mods" : index == 2 ? "GitHub"
                : ModLocalization.Get(index == 3
                    ? ModJournalLocalization.Livestream : ModJournalLocalization.Profile);
        }

        private void RefreshTexts()
        {
            language = LocalizationManager.Instance?.CurrentLanguage;
            for (int i = 0; i < labels.Count; i++)
                labels[i].text = i == 0 ? EntryName(i)
                    : string.Format(ModLocalization.Get(ModJournalLocalization.Visit), EntryName(i));
            if (selected) ShowEntry(selectedEntry);
        }

        private void LateUpdate()
        {
            if (!FeatureFailure.IsAvailable(FeatureId.ModJournal))
            {
                return;
            }

            try
            {
                LateUpdateCore();
            }
            catch (System.Exception exception)
            {
                FeatureFailure.Disable(FeatureId.ModJournal, exception);
                return;
            }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private void LateUpdateCore()
        {
            if (category == null)
                return;
            if (language != LocalizationManager.Instance?.CurrentLanguage)
                RefreshTexts();
            NativeLocalizedText.MatchFontSize(category.text, panel.locationIconOriginal.text);
            foreach (TextMeshProUGUI label in labels)
                NativeLocalizedText.MatchFontSize(label, panel.loreIconOriginal.text);
        }

        internal void Clear()
        {
            selected = false;
            if (category != null)
            {
                category.gameObject.SetActive(false);
                Destroy(category.gameObject);
                category = null;
            }
            foreach (GameObject entry in entries)
            {
                entry.SetActive(false);
                Destroy(entry);
            }
            entries.Clear();
            labels.Clear();
        }

        private void OnDestroy() => Clear();

        internal static void DisposeAll()
        {
            foreach (NativeModJournal view in Resources.FindObjectsOfTypeAll<NativeModJournal>())
            {
                view.Clear();
                if (view.panel != null && view.panel.isActiveAndEnabled)
                    view.panel.RefreshLore();
                Destroy(view);
            }
        }
    }

    [HarmonyPatch(typeof(UI_JournalContent_Lore), nameof(UI_JournalContent_Lore.RefreshLore))]
    internal static class ModJournalRefreshPatch
    {
        private static void Postfix(UI_JournalContent_Lore __instance)
        {
            if (!FeatureFailure.IsAvailable(FeatureId.ModJournal))
            {
                return;
            }

            try
            {
                PostfixCore(__instance);
            }
            catch (System.Exception exception)
            {
                FeatureFailure.Disable(FeatureId.ModJournal, exception);
                return;
            }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static void PostfixCore(UI_JournalContent_Lore __instance)
        {
            NativeModJournal view = __instance.GetComponent<NativeModJournal>() ?? __instance.gameObject.AddComponent<NativeModJournal>();
            view.Build(__instance);
        }
    }

    [HarmonyPatch(typeof(UI_JournalContent_Lore), nameof(UI_JournalContent_Lore.Clear))]
    internal static class ModJournalClearPatch
    {
        private static void Prefix(UI_JournalContent_Lore __instance)
        {
            if (!FeatureFailure.IsAvailable(FeatureId.ModJournal))
            {
                return;
            }

            try
            {
                PrefixCore(__instance);
            }
            catch (System.Exception exception)
            {
                FeatureFailure.Disable(FeatureId.ModJournal, exception);
                return;
            }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static void PrefixCore(UI_JournalContent_Lore __instance) => __instance.GetComponent<NativeModJournal>()?.Clear();
    }

    [HarmonyPatch(typeof(UI_JournalContent_Lore), "OnLocationIconClick")]
    internal static class ModJournalCategoryPatch
    {
        private static void Postfix(UI_JournalContent_Lore __instance, UI_JournalPanel_SearchOptionButton button)
        {
            if (!FeatureFailure.IsAvailable(FeatureId.ModJournal))
            {
                return;
            }

            try
            {
                PostfixCore(__instance, button);
            }
            catch (System.Exception exception)
            {
                FeatureFailure.Disable(FeatureId.ModJournal, exception);
                return;
            }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static void PostfixCore(UI_JournalContent_Lore __instance, UI_JournalPanel_SearchOptionButton button) => __instance.GetComponent<NativeModJournal>()?.Select(button.data);
    }

    [HarmonyPatch(typeof(UI_JournalContent_Lore), "OnTutorialCategoryClicked")]
    internal static class ModJournalTutorialPatch
    {
        private static void Prefix(UI_JournalContent_Lore __instance)
        {
            if (!FeatureFailure.IsAvailable(FeatureId.ModJournal))
            {
                return;
            }

            try
            {
                PrefixCore(__instance);
            }
            catch (System.Exception exception)
            {
                FeatureFailure.Disable(FeatureId.ModJournal, exception);
                return;
            }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static void PrefixCore(UI_JournalContent_Lore __instance) => __instance.GetComponent<NativeModJournal>()?.Select(null);
    }
}
