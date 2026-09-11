using System.Collections.Generic;
using System.Linq;
using SephiriaEnhancements.Configuration;
using SephiriaEnhancements.Integration;
using SephiriaEnhancements.Runtime;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SephiriaEnhancements.EffectStats.Integration
{
    internal sealed class NativeEffectStatsView : MonoBehaviour
    {
        private sealed class Row
        {
            internal string Id;
            internal StatusInstance_Custom Status;
            internal TextMeshProUGUI Text;
            internal LayoutElement Layout;
        }

        private sealed class Group
        {
            internal GameObject Root;
            internal TextMeshProUGUI Title;
            internal string TitleKey;
            internal readonly List<Row> Rows = new();
        }

        private readonly List<Group> groups = new();
        private UI_StatsPanel panel;
        private PlayerAvatar player;
        private TextMeshProUGUI rowTemplate;
        private TextMeshProUGUI titleTemplate;
        private RectTransform content;
        private Group introduction;
        private Row solarDamage;
        private float nextRefresh;

        internal void Show(UI_StatsPanel owner, PlayerAvatar current)
        {
            panel = owner;
            player = current;
            if (groups.Count == 0) Build();
            Refresh();
        }

        private void Build()
        {
            UI_StatsCategory template = panel.categories.First(category =>
                category.GetComponentInParent<ScrollRect>() != null &&
                category.transform.Find("Name") != null);
            content = template.GetComponentInParent<ScrollRect>().content;
            titleTemplate = template.transform.Find("Name").GetComponent<TextMeshProUGUI>();
            UI_StatusTooltipOpener nativeRow = panel.statElements.First(row =>
                row.GetComponent<TextMeshProUGUI>() != null);
            rowTemplate = nativeRow.GetComponent<TextMeshProUGUI>();
            introduction = MakeGroup(template, EffectStatsLocalization.Title);
            AddRow(introduction, nativeRow, null);
            foreach (var definition in NativeEffectStatsCatalog.Groups)
            {
                Group group = MakeGroup(template, definition.Title);
                if (definition.Title == "ItemCategory_FlameSword")
                    solarDamage = AddRow(group, nativeRow, null);
                foreach (string id in definition.Statuses)
                    AddRow(group, nativeRow, id);
            }
        }

        private Group MakeGroup(UI_StatsCategory template, string key)
        {
            UI_StatsCategory clone = Instantiate(template, content, false);
            clone.name = "Additional Effect Stats";
            clone.gameObject.SetActive(false);
            Transform heading = clone.transform.Find("Name");
            foreach (Transform child in clone.transform.Cast<Transform>().ToArray())
                if (child != heading) DestroyImmediate(child.gameObject);
            var group = new Group { Root = clone.gameObject,
                Title = heading.GetComponent<TextMeshProUGUI>(), TitleKey = key };
            var localization = heading.GetComponent<UI_LocalizationStringText>();
            if (localization != null) DestroyImmediate(localization);
            DestroyImmediate(clone);
            groups.Add(group);
            return group;
        }

        private Row AddRow(Group group, UI_StatusTooltipOpener template, string id)
        {
            UI_StatusTooltipOpener clone = Instantiate(template, group.Root.transform, false);
            TextMeshProUGUI text = clone.GetComponent<TextMeshProUGUI>();
            clone.statusValueText.gameObject.SetActive(false);
            var name = clone.GetComponent<UI_StatusName>();
            if (name != null) DestroyImmediate(name);
            DestroyImmediate(clone);
            text.margin = Vector4.zero;
            text.textWrappingMode = TextWrappingModes.Normal;
            text.overflowMode = TextOverflowModes.Overflow;
            var button = text.GetComponent<UI_HorayButton>();
            button.onClick = new Button.ButtonClickedEvent();
            button.SetForceNavUp(null);
            button.SetForceNavDown(null);
            button.SetForceNavLeft(null);
            button.SetForceNavRight(null);
            button.navigation = new Navigation { mode = Navigation.Mode.Automatic };
            var layout = text.GetComponent<LayoutElement>() ?? text.gameObject.AddComponent<LayoutElement>();
            var row = new Row { Id = id, Text = text, Layout = layout,
                Status = id == null ? null : NativeEffectStatsCatalog.CreateStatus(id) };
            group.Rows.Add(row);
            return row;
        }

        private void LateUpdate()
        {
            if (!FeatureFailure.IsAvailable(FeatureId.EffectStats)) return;
            try { LateUpdateCore(); }
            catch (System.Exception exception) { FeatureFailure.Disable(FeatureId.EffectStats, exception); }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private void LateUpdateCore()
        {
            if (panel == null || !panel.IsOpened || Time.unscaledTime < nextRefresh) return;
            nextRefresh = Time.unscaledTime + 0.2f;
            Refresh();
        }

        private void Refresh()
        {
            bool ready = EnhancementsSettings.Enabled && player != null && LocalPlayerResolver.IsLocal(player) &&
                player.loadingScreenType == -1;
            bool hasDamage = ready && NativeEffectStatsCatalog.TrySolarBladeDamage(player, out _);
            foreach (Group group in groups)
            {
                NativeLocalizedText.MatchFontSize(group.Title, titleTemplate);
                group.Title.text = group == introduction ? ModLocalization.Get(group.TitleKey)
                    : new LocalizedString(group.TitleKey).ToString();
                bool visible = group == introduction && ready;
                foreach (Row row in group.Rows)
                {
                    bool show;
                    string text;
                    if (group == introduction)
                    {
                        show = ready;
                        text = ModLocalization.Get(EffectStatsLocalization.Note);
                    }
                    else if (row == solarDamage)
                    {
                        show = hasDamage;
                        float damage = 0;
                        if (show) NativeEffectStatsCatalog.TrySolarBladeDamage(player, out damage);
                        text = string.Format(ModLocalization.Get(EffectStatsLocalization.SolarDamage), damage.ToString("0.#"));
                    }
                    else
                    {
                        int value = ready ? NativeEffectStatsCatalog.Read(player, row.Id) : 0;
                        show = value != 0;
                        text = show ? NativeEffectStatsCatalog.Describe(row.Status, value) : string.Empty;
                    }
                    SetVisible(row.Text.gameObject, show);
                    if (!show) continue;
                    visible = true;
                    NativeLocalizedText.MatchFontSize(row.Text, rowTemplate);
                    row.Text.text = text;
                    // Full sentences use the native body size and grow vertically inside its scroll area.
                    float width = Mathf.Max(1f, content.rect.width -
                        content.GetComponent<VerticalLayoutGroup>().padding.horizontal -
                        group.Root.GetComponent<VerticalLayoutGroup>().padding.horizontal);
                    row.Layout.preferredHeight = Mathf.Max(rowTemplate.rectTransform.rect.height,
                        row.Text.GetPreferredValues(text, width, float.PositiveInfinity).y + 4f);
                }
                SetVisible(group.Root, visible);
            }
            SetVisible(introduction.Root, ready && groups.Skip(1).Any(group => group.Root.activeSelf));
        }

        private void SetVisible(GameObject target, bool visible)
        {
            if (target.activeSelf == visible) return;
            GameObject selected = EventSystem.current?.currentSelectedGameObject;
            if (!visible && panel != null && panel.IsOpened && panel.tab.CurrentSelectedTab >= 0 &&
                selected != null && selected.transform.IsChildOf(target.transform))
                panel.DoControlSelection(panel.tab.tabButtons[panel.tab.CurrentSelectedTab].gameObject);
            target.SetActive(visible);
        }

        private void OnDestroy()
        {
            foreach (Group group in groups)
                if (group.Root != null) { SetVisible(group.Root, false); Destroy(group.Root); }
            groups.Clear();
        }

        internal static void DisposeAll()
        {
            foreach (NativeEffectStatsView view in Resources.FindObjectsOfTypeAll<NativeEffectStatsView>())
                DestroyImmediate(view);
        }
    }
}
