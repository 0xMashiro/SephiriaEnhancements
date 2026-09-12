using System.Collections.Generic;
using System.Linq;
using System.Text;
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
            internal TextMeshProUGUI Text;
            internal LayoutElement Layout;
            internal UI_TooltipOpener Tooltip;
            internal string Summary;
            internal string Title;
            internal string Details;
        }

        private sealed class Group
        {
            internal GameObject Root;
            internal TextMeshProUGUI Title;
            internal string TitleKey;
            internal (string Id, StatusInstance_Custom Status)[] Statuses;
            internal readonly List<Row> Rows = new();
        }

        private readonly List<Group> groups = new();
        private UI_StatsPanel panel;
        private PlayerAvatar player;
        private TextMeshProUGUI rowTemplate;
        private TextMeshProUGUI titleTemplate;
        private RectTransform content;
        private UI_StatusTooltipOpener nativeRow;
        private float nextRefresh;
        private int openedFrame;
        private ScrollRect scroll;
        private UI_ScrollToSelection nativeScroll;
        private bool suspendedNativeScroll;
        private GameObject lastScrollSelection;

        internal void Show(UI_StatsPanel owner, PlayerAvatar current)
        {
            panel = owner;
            player = current;
            openedFrame = Time.frameCount;
            if (groups.Count == 0) Build();
            Refresh();
        }

        private void Build()
        {
            // OnOpened selects the common tab before this view builds; the special tab is inactive.
            UI_StatsCategory template = panel.categories.First(category =>
                category.GetComponentInParent<ScrollRect>(includeInactive: true) != null &&
                category.transform.Find("Name") != null);
            scroll = template.GetComponentInParent<ScrollRect>(includeInactive: true);
            content = scroll.content;
            nativeScroll = scroll.GetComponent<UI_ScrollToSelection>();
            titleTemplate = template.transform.Find("Name").GetComponent<TextMeshProUGUI>();
            nativeRow = panel.statElements.First(row =>
                row.GetComponent<TextMeshProUGUI>() != null);
            rowTemplate = nativeRow.GetComponent<TextMeshProUGUI>();
            foreach (var definition in NativeEffectStatsCatalog.Groups)
            {
                Group group = MakeGroup(template, definition.Title);
                group.Statuses = definition.Statuses.Select(id =>
                    (id, NativeEffectStatsCatalog.CreateStatus(id))).ToArray();
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
                Tooltip = text.gameObject.AddComponent<UI_TooltipOpener>() };
            row.Tooltip.OnSelected += _ => FeatureFailure.Run(FeatureId.EffectStats, () => ShowTooltip(row));
            row.Tooltip.OnDeselected += _ => HideTooltip(row);
            group.Rows.Add(row);
            lastScrollSelection = null;
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
            if (panel == null || !panel.IsOpened)
            {
                HideTooltips();
                RestoreNativeScroll();
                return;
            }
            HandleTabInput();
            ScrollToSelectedRow();
            if (Time.unscaledTime < nextRefresh) return;
            nextRefresh = Time.unscaledTime + 0.2f;
            Refresh();
        }

        private void HandleTabInput()
        {
            if (!EnhancementsSettings.Enabled || !panel.IsControlEnabled || !panel.IsInteractable ||
                Time.frameCount == openedFrame || player == null || !LocalPlayerResolver.IsLocal(player) ||
                player.loadingScreenType != -1) return;
            var manager = UIManager.Instance;
            var stack = manager?.CurrentControlStack;
            if (stack == null || !stack.Contains(panel)) return;
            GameObject selected = EventSystem.current?.currentSelectedGameObject;
            // Combined menus share control; only the panel containing the selection handles tabs.
            if (stack.Count > 1 && (selected == null || !selected.transform.IsChildOf(panel.transform))) return;
            // These native tooltips use the same actions to preview levels or weapon details.
            if (manager.GetElement<UI_CharmTooltip>()?.IsOpened == true ||
                manager.GetElement<UI_WeaponTooltip>()?.IsOpened == true) return;
            var input = UIInputModule.current;
            if (input == null) return;
            bool previous = input.prevTabAction?.action?.WasPressedThisFrame() == true;
            bool next = input.nextTabAction?.action?.WasPressedThisFrame() == true;
            if (previous == next) return;
            panel.SelectTab(panel.tab.CurrentSelectedTab + (next ? 1 : -1));
            GameObject target = panel.defaultSelectable;
            if (target == null || !target.activeInHierarchy)
                target = panel.tab.tabButtons[panel.tab.CurrentSelectedTab].gameObject;
            panel.DoControlSelection(target);
        }

        private void Refresh()
        {
            bool ready = EnhancementsSettings.Enabled && player != null && LocalPlayerResolver.IsLocal(player) &&
                player.loadingScreenType == -1;
            foreach (Group group in groups)
            {
                NativeLocalizedText.MatchFontSize(group.Title, titleTemplate);
                group.Title.text = new LocalizedString(group.TitleKey).ToString();
                var results = ready ? NativeEffectStatsResults.Read(player, group.TitleKey)
                    .Where(result => result.Text != null).ToList() : new List<(string Id, string Title, string Text)>();
                var bonuses = new StringBuilder();
                if (ready)
                    foreach (var status in group.Statuses)
                    {
                        int value = NativeEffectStatsCatalog.Read(player, status.Id);
                        if (value != 0) bonuses.AppendLine(NativeEffectStatsCatalog.Describe(status.Status, value));
                    }
                if (results.Count == 0 && bonuses.Length > 0)
                    results.Add(("bonuses", group.Title.text, ModLocalization.Get(EffectStatsLocalization.Bonuses)));
                foreach (Row obsolete in group.Rows.Where(row => !results.Any(result => result.Id == row.Id)).ToArray())
                {
                    HideTooltip(obsolete);
                    SetVisible(obsolete.Text.gameObject, false);
                    Destroy(obsolete.Text.gameObject);
                    group.Rows.Remove(obsolete);
                }
                for (int index = 0; index < results.Count; index++)
                {
                    var result = results[index];
                    Row row = group.Rows.FirstOrDefault(row => row.Id == result.Id) ?? AddRow(group, nativeRow, result.Id);
                    int newline = result.Text.IndexOf('\n');
                    row.Summary = newline < 0 ? result.Text : result.Text.Substring(0, newline);
                    row.Title = result.Title;
                    // Magic details already repeat cost and charges; other summaries contain unique values.
                    row.Details = group.TitleKey == "Status_Magic_Name" && newline >= 0
                        ? result.Text.Substring(newline + 1) : result.Text;
                    // Use the paragraph breaks used by native detailed attribute descriptions.
                    row.Details = row.Details.Replace("\n", "\n\n");
                    if (bonuses.Length > 0) row.Details += "\n\n" + bonuses.ToString().TrimEnd();
                    NativeLocalizedText.MatchFontSize(row.Text, rowTemplate);
                    row.Text.text = row.Summary;
                    row.Text.transform.SetSiblingIndex(index + 1);
                    // Summary rows retain the native body size; longer translations may wrap.
                    float width = Mathf.Max(1f, content.rect.width -
                        content.GetComponent<VerticalLayoutGroup>().padding.horizontal -
                        group.Root.GetComponent<VerticalLayoutGroup>().padding.horizontal);
                    float height = Mathf.Max(rowTemplate.rectTransform.rect.height,
                        row.Text.GetPreferredValues(row.Summary, width, float.PositiveInfinity).y + 4f);
                    if (!Mathf.Approximately(row.Layout.preferredHeight, height)) lastScrollSelection = null;
                    row.Layout.preferredHeight = height;
                    RefreshTooltip(row);
                }
                SetVisible(group.Root, results.Count > 0);
            }
        }

        private void ShowTooltip(Row row)
        {
            if (panel == null || !panel.IsOpened || !EnhancementsSettings.Enabled ||
                player == null || !LocalPlayerResolver.IsLocal(player) || player.loadingScreenType != -1) return;
            RectTransform rect = row.Text.rectTransform;
            UIManager.Instance.GetElement<UI_CommonTooltip>().Open(row.Tooltip, rect, rect.rect.size * 0.5f,
                new SimpleTooltipObject(row.Title, row.Details));
            RefreshTooltip(row);
        }

        private static void RefreshTooltip(Row row)
        {
            if (!row.Tooltip.Showing || !(row.Tooltip.LastTooltip is UI_CommonTooltip tooltip) ||
                !ReferenceEquals(tooltip.Target, row.Tooltip)) return;
            // Updating the open native text avoids restarting its fade on each value refresh.
            tooltip.titleText.text = KeywordDatabase.Convert(row.Title, useColor: false, useSprite: false);
            tooltip.flavorText.text = KeywordDatabase.Convert(row.Details, useColor: false, useSprite: false);
            tooltip.flavorText.gameObject.SetActive(row.Details.Length > 0);
            // Resolve wrapped text heights before the native tooltip positions itself on screen.
            LayoutRebuilder.ForceRebuildLayoutImmediate(tooltip.rectTransform);
        }

        private static void HideTooltip(Row row)
        {
            row.Tooltip.Showing = false;
            if (row.Tooltip.LastTooltip != null && ReferenceEquals(row.Tooltip.LastTooltip.Target, row.Tooltip))
                row.Tooltip.LastTooltip.Close();
        }

        private void HideTooltips()
        {
            foreach (Group group in groups)
                foreach (Row row in group.Rows) HideTooltip(row);
        }

        private void SetVisible(GameObject target, bool visible)
        {
            if (target.activeSelf == visible) return;
            lastScrollSelection = null;
            GameObject selected = EventSystem.current?.currentSelectedGameObject;
            if (!visible && panel != null && panel.IsOpened && panel.tab.CurrentSelectedTab >= 0 &&
                selected != null && selected.transform.IsChildOf(target.transform))
                panel.DoControlSelection(panel.tab.tabButtons[panel.tab.CurrentSelectedTab].gameObject);
            target.SetActive(visible);
        }

        private void ScrollToSelectedRow()
        {
            GameObject selected = EventSystem.current?.currentSelectedGameObject;
            bool ownRow = selected != null && groups.Any(group =>
                group.Rows.Any(row => row.Text.gameObject == selected));
            if (!ownRow || !selected.activeInHierarchy)
            {
                RestoreNativeScroll();
                return;
            }
            // Native scrolling targets the entire parent category, which may exceed the viewport.
            if (nativeScroll != null && nativeScroll.enabled)
            {
                nativeScroll.enabled = false;
                suspendedNativeScroll = true;
            }
            if (selected == lastScrollSelection) return;
            lastScrollSelection = selected;
            Canvas.ForceUpdateCanvases();
            var bounds = RectTransformUtility.CalculateRelativeRectTransformBounds(scroll.viewport,
                selected.GetComponent<RectTransform>());
            Rect window = scroll.viewport.rect;
            float offset = bounds.max.y > window.yMax ? bounds.max.y - window.yMax :
                bounds.min.y < window.yMin ? bounds.min.y - window.yMin : 0f;
            float overflow = content.rect.height - window.height;
            if (overflow > 0f && offset != 0f)
            {
                scroll.StopMovement();
                scroll.verticalNormalizedPosition = Mathf.Clamp01(scroll.verticalNormalizedPosition + offset / overflow);
            }
        }

        private void RestoreNativeScroll()
        {
            if (suspendedNativeScroll && nativeScroll != null) nativeScroll.enabled = true;
            suspendedNativeScroll = false;
            lastScrollSelection = null;
        }

        private void OnDisable()
        {
            HideTooltips();
            RestoreNativeScroll();
        }

        private void OnDestroy()
        {
            HideTooltips();
            RestoreNativeScroll();
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
