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
            internal System.Func<PlayerAvatar, string> ReadResult;
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
                if (definition.Title == "ItemCategory_DarkCloud")
                {
                    AddRow(group, nativeRow, null).ReadResult = NativeEffectStatsResults.CloudDamage;
                    AddRow(group, nativeRow, null).ReadResult = NativeEffectStatsResults.CloudSupply;
                }
                if (definition.Title == "Status_Magic_Name")
                    for (int slot = 0; slot < 8; slot++)
                    {
                        int index = slot;
                        AddRow(group, nativeRow, null).ReadResult = avatar => NativeEffectStatsResults.Magic(avatar, index);
                    }
                if (definition.Title == "Debuff_Burn")
                    AddRow(group, nativeRow, null).ReadResult = NativeEffectStatsResults.Burn;
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
            if (panel == null || !panel.IsOpened) { RestoreNativeScroll(); return; }
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
                    else if (row.ReadResult != null)
                    {
                        text = ready ? row.ReadResult(player) : null;
                        show = text != null;
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

        private void OnDisable() => RestoreNativeScroll();

        private void OnDestroy()
        {
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
