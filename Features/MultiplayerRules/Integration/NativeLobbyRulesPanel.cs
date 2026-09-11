using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Mirror;
using SephiriaEnhancements.Configuration;
using SephiriaEnhancements.Integration;
using SephiriaEnhancements.MultiplayerRules.Presentation;
using SephiriaEnhancements.Runtime;
using SephiriaEnhancements.Runtime.GameBridge;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SephiriaEnhancements.MultiplayerRules.Integration
{
    internal sealed class NativeLobbyRulesPanel : UIBase
    {
        private static NativeLobbyRulesPanel current;
        internal static void CloseCurrent() { if (current != null) current.Close(); }
        internal static bool IsEditing => current != null && current.CanEdit;
        private sealed class Row
        {
            internal GameObject Root;
            internal UI_HorizontalSelectionBox Box;
            internal TextMeshProUGUI Label;
            internal TextMeshProUGUI Value;
            internal Action Refresh;
        }

        private readonly List<Row> rows = new();
        private MultiplayerRulesDraft draft;
        private PlayerAvatar owner;
        private DungeonManager world;
        private bool editing;
        private int participants = 1;
        private int group;
        private bool teamChanged;
        private TextMeshProUGUI heading;
        private TextMeshProUGUI preview;
        private TextMeshProUGUI status;
        private ActiveExplorationMultiplayerRules applied;
        private TextMeshProUGUI fontTemplate;
        private UI_OptionBox_PartyMemberDamage rowTemplate;
        private RectTransform content;
        private ScrollRect scroll;
        private GameObject returnSelection;
        private MultiplayerRuleId selectedRule;
        private float nextRefresh;
        private GameObject lastSelection;
        private string selectedHelp;
        private ActiveExplorationMultiplayerRules displayed;
        private UI_MessageBox ownedDialog;
        private readonly List<(UnityEngine.UI.Button button, TextMeshProUGUI label, string key)> buttons = new();

        public override bool MarksPlayerAsPreparing => editing;

        internal static NativeLobbyRulesPanel Show()
        {
            try { return ShowCore(); }
            catch (System.Exception exception)
            {
                FeatureFailure.Disable(FeatureId.MultiplayerRules, exception);
                if (current != null) { Destroy(current.gameObject); current = null; }
                return null;
            }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static NativeLobbyRulesPanel ShowCore()
        {
            if (current != null) return current;
            if (!MultiplayerRulesLobbyContext.IsInLobby) return null;
            var options = UIManager.Instance?.GetElement<UI_OptionsPanel>();
            var template = options?.GetComponentInChildren<UI_OptionBox_PartyMemberDamage>(true);
            if (template == null || options.ParentRoot == null) return null;
            var root = new GameObject("Multiplayer Rules", typeof(RectTransform), typeof(CanvasGroup));
            root.SetActive(false);
            root.transform.SetParent(options.transform.parent, false);
            var rect = (RectTransform)root.transform;
            rect.anchorMin = new Vector2(.06f, .05f);
            rect.anchorMax = new Vector2(.94f, .95f);
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            var panel = root.AddComponent<NativeLobbyRulesPanel>();
            current = panel;
            panel.SetRoot(options.ParentRoot);
            panel.hasControl = true;
            panel.isPlayerUITHing = true;
            panel.owner = LocalPlayerResolver.Resolve();
            panel.world = DungeonManager.Instance;
            panel.editing = MultiplayerRulesLobbyContext.CanEdit;
            panel.participants = MultiplayerRulesLobbyContext.ParticipantCount;
            panel.selectedRule = MultiplayerRulePresentationGroups.All[0].RuleIds[0];
            panel.returnSelection = EventSystem.current?.currentSelectedGameObject;
            panel.draft = new MultiplayerRulesDraft(PreferredMultiplayerRulesStore.Read(),
                PreferredMultiplayerRulesStore.ReadAllowExternalRuleStacking());
            panel.rowTemplate = template;
            panel.fontTemplate = template.valueText.text;
            panel.applied = PreferredMultiplayerRulesStore.Read().Freeze();
            panel.Build();
            panel.Open();
            return panel;
        }

        private bool CanEdit => editing && IsOpened && owner == LocalPlayerResolver.Resolve() &&
            world == DungeonManager.Instance && MultiplayerRulesLobbyContext.CanEdit;
        private bool SupportedTeam => participants >= 1 && participants <= 4;
        private bool CanChangeRules => CanEdit && SupportedTeam && participants == MultiplayerRulesLobbyContext.ParticipantCount;

        private ActiveExplorationMultiplayerRules Displayed => displayed;

        private void Build()
        {
            gameObject.AddComponent<Image>().color = new Color(.18f, .18f, .27f, .99f);
            var outline = Rect(transform, "Outline", Vector2.zero, Vector2.one).gameObject.AddComponent<Image>();
            outline.color = new Color(.72f, .72f, .82f);
            var background = Rect(outline.transform, "Inset", Vector2.zero, Vector2.one);
            background.offsetMin = Vector2.one * 2;
            background.offsetMax = Vector2.one * -2;
            background.gameObject.AddComponent<Image>().color = new Color(.18f, .18f, .27f);
            var divider = Rect(transform, "Divider", new Vector2(.625f, .21f), new Vector2(.627f, .90f));
            divider.gameObject.AddComponent<Image>().color = new Color(.4f, .42f, .54f);
            heading = Text(transform, "Heading", new Vector2(.02f, .91f), new Vector2(.98f, .99f));
            var viewport = Rect(transform, "Rules", new Vector2(.02f, .21f), new Vector2(.61f, .90f));
            viewport.gameObject.AddComponent<RectMask2D>();
            scroll = viewport.gameObject.AddComponent<ScrollRect>();
            scroll.viewport = viewport;
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            content = Rect(viewport, "Content", new Vector2(0, 1), Vector2.one);
            content.pivot = new Vector2(.5f, 1);
            var layout = content.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = false;
            layout.spacing = 2;
            content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scroll.content = content;

            Choice(MultiplayerRulesLocalization.RuleGroupSetting, MultiplayerRulePresentationGroups.All.Count,
                () => group, n => { group = n; selectedRule = MultiplayerRulePresentationGroups.All[group].RuleIds[0]; Refresh(); },
                () => T(MultiplayerRulePresentationGroups.All[group].LocalizationKey), () => true);
            Choice(MultiplayerRulesLocalization.HealthCombinationSetting, 3,
                () => (int)(Displayed?.HealthModifierCombination ?? EnemyHealthModifierCombination.ParticipantRuleOnly),
                n => { if (CanChangeRules) { draft.BeginCustom(); draft.HealthCombination = (EnemyHealthModifierCombination)n; } Refresh(); },
                () => Displayed == null ? "—" : T(MultiplayerRulesLocalization.HealthCombinationKeys[(int)Displayed.HealthModifierCombination]),
                () => CanChangeRules,
                visible: () => Displayed != null &&
                    MultiplayerRulePresentationGroups.All[group].LocalizationKey == MultiplayerRulesLocalization.GroupEnemyHealth &&
                    MultiplayerRulePresentationGroups.All[group].RuleIds.Any(id => Displayed.Rules.Get(id, participants).Source == MultiplayerRuleValueSource.Override));
            Choice(MultiplayerRulesLocalization.ExternalRuleStackingSetting, 2,
                () => draft.AllowExternalStacking ? 1 : 0,
                n => { if (CanEdit) draft.AllowExternalStacking = n == 1; Refresh(); },
                () => T(draft.AllowExternalStacking ? MultiplayerRulesLocalization.ToggleEnabled : MultiplayerRulesLocalization.ToggleDisabled),
                () => CanChangeRules, visible: () => editing && MultiplayerExtensionDiscovery.HasDetectedExtension);
            foreach (var definition in MultiplayerRuleCatalog.All) AddRule(definition);
            preview = Text(transform, "Preview", new Vector2(.64f, .21f), new Vector2(.975f, .90f));
            preview.alignment = TextAlignmentOptions.TopLeft;
            status = Text(transform, "Review", new Vector2(.025f, .08f), new Vector2(.975f, .20f));
            Button(MultiplayerRulesLocalization.LobbyApply, .02f, .32f, Apply);
            Button(MultiplayerRulesLocalization.LobbyRestore, .35f, .65f, () =>
            {
                if (CanChangeRules) { draft.RestoreParticipant(participants); Refresh(); }
            });
            Button(MultiplayerRulesLocalization.LobbyCancel, .68f, .98f, Close);
            defaultSelectable = rows[0].Box.gameObject;
            Refresh();
        }


        private void Choice(string key, int count, Func<int> selected, Action<int> change,
            Func<string> value, Func<bool> enabled, Func<bool> visible = null)
        {
            Row row = MakeRow(key);
            row.Box.numberOfElements = count;
            row.Box.OnValueChanged += n => { if (enabled()) change(n); else Refresh(); };
            row.Refresh = () =>
            {
                row.Root.SetActive(visible == null || visible());
                row.Box.ChangeValueWithoutNotify(selected());
                row.Box.interactable = enabled();
                row.Label.text = T(key);
                row.Value.text = value();
                if (EventSystem.current?.currentSelectedGameObject == row.Box.gameObject)
                    selectedHelp = key == MultiplayerRulesLocalization.HealthCombinationSetting ? MultiplayerRulesLocalization.HealthCombinationHelp
                        : key == MultiplayerRulesLocalization.ExternalRuleStackingSetting ? MultiplayerRulesLocalization.ExternalRuleStackingHelp
                        : MultiplayerRulesLocalization.RuleGroupHelp;
                foreach (var arrow in row.Root.GetComponentsInChildren<UI_HorizontalSelectionBox_Arrow>(true))
                    arrow.gameObject.SetActive(enabled());
            };
        }

        private void AddRule(MultiplayerRuleDefinition definition)
        {
            Row row = MakeRow(MultiplayerRulesLocalization.RuleLabelKey(definition.Id));
            row.Box.overflowType = UI_HorizontalSelectionBox.OverflowType.Clamp;
            row.Box.numberOfElements = definition.Unit == MultiplayerRuleUnit.Toggle ? 3 : 1;
            row.Box.OnValueChanged += n =>
            {
                if (CanChangeRules && definition.Unit == MultiplayerRuleUnit.Toggle)
                {
                    draft.Set(definition.Id, participants, n == 0 ? MultiplayerRuleValue<float>.UseGameBehavior()
                        : MultiplayerRuleValue<float>.Override(definition.Minimum + (n - 1) * definition.Step));
                    selectedRule = definition.Id;
                }
                Refresh();
            };
            row.Box.gameObject.AddComponent<NativeOptionActivation>().Configure(() =>
            {
                selectedRule = definition.Id;
                selectedHelp = null;
                RefreshPreview();
                if (!CanChangeRules || definition.Unit == MultiplayerRuleUnit.Toggle) return;
                int count = participants;
                var current = Displayed.Rules.Get(definition.Id, count);
                string initial = current.TryGetOverride(out float number) ? number.ToString("0.##", CultureInfo.InvariantCulture) : "";
                ownedDialog = NativeRuleInputDialog.Open(string.Format(T(MultiplayerRulesLocalization.EditorPrompt),
                    T(MultiplayerRulesLocalization.RuleLabelKey(definition.Id)), count,
                    NativeRuleReference.Describe(definition, count), MultiplayerRulesLocalization.FormatValue(definition.Minimum, definition.Unit), MultiplayerRulesLocalization.FormatValue(definition.Maximum, definition.Unit), MultiplayerRulesLocalization.FormatValue(definition.Step, definition.Unit)),
                    initial, definition, () => CanChangeRules && count == MultiplayerRulesLobbyContext.ParticipantCount, value => { draft.Set(definition.Id, count, value); Refresh(); }, row.Box.gameObject);
                if (ownedDialog != null)
                {
                    var opened = ownedDialog;
                    Action<UI_MessageBox> release = null;
                    release = _ =>
                    {
                        opened.onCloseMessageBox -= release;
                        if (ownedDialog == opened) ownedDialog = null;
                    };
                    opened.onCloseMessageBox += release;
                }
            });
            row.Refresh = () =>
            {
                row.Root.SetActive(MultiplayerRulePresentationGroups.All[group].RuleIds.Contains(definition.Id));
                var displayed = Displayed;
                var configured = displayed?.Rules.Get(definition.Id, participants) ?? MultiplayerRuleValue<float>.UseGameBehavior();
                bool hasNumber = configured.TryGetOverride(out float number);
                if (!hasNumber && SupportedTeam) hasNumber = NativeRuleReference.TryRead(definition.Id, participants, out number);
                row.Box.ChangeValueWithoutNotify(definition.Unit == MultiplayerRuleUnit.Toggle && configured.TryGetOverride(out number) ? (number > 0 ? 2 : 1) : 0);
                // Read-only rows remain selectable to inspect their explanation.
                row.Box.interactable = true;
                row.Label.text = T(MultiplayerRulesLocalization.RuleLabelKey(definition.Id));
                row.Value.text = displayed == null ? "—" : configured.TryGetOverride(out number)
                    ? definition.Unit == MultiplayerRuleUnit.Toggle
                        ? T(number > 0 ? MultiplayerRulesLocalization.ToggleEnabled : MultiplayerRulesLocalization.ToggleDisabled)
                        : MultiplayerRulesLocalization.FormatValue(number, definition.Unit)
                    : T(MultiplayerRulesLocalization.UseGameBehavior);
                if (CanChangeRules && definition.Unit != MultiplayerRuleUnit.Toggle)
                    row.Value.text += " · " + T(MultiplayerRulesLocalization.EditAction);
                if (editing && applied != null && !configured.Equals(applied.Rules.Get(definition.Id, participants)))
                    row.Label.text = "* " + row.Label.text;
                if (EventSystem.current?.currentSelectedGameObject == row.Box.gameObject)
                { selectedRule = definition.Id; selectedHelp = null; }
                foreach (var arrow in row.Root.GetComponentsInChildren<UI_HorizontalSelectionBox_Arrow>(true))
                    arrow.gameObject.SetActive(CanChangeRules && definition.Unit == MultiplayerRuleUnit.Toggle);
            };
        }

        private Row MakeRow(string key)
        {
            var clone = Instantiate(rowTemplate, content, false);
            clone.gameObject.SetActive(false);
            var row = new Row { Root = clone.gameObject, Box = clone.box, Value = clone.valueText.text };
            row.Label = clone.GetComponentsInChildren<UI_LocalizationStringText>(true).First(t => t != clone.valueText).text;
            foreach (var text in clone.GetComponentsInChildren<UI_LocalizationStringText>(true)) DestroyImmediate(text);
            DestroyImmediate(clone);
            row.Box.ValueChangedCallback.RemoveAllListeners();
            row.Box.forceNavUp = row.Box.forceNavDown = null;
            row.Box.navigation = new Navigation { mode = Navigation.Mode.Automatic };
            var size = row.Root.GetComponent<LayoutElement>() ?? row.Root.AddComponent<LayoutElement>();
            size.preferredHeight = ((RectTransform)rowTemplate.transform).rect.height;
            rows.Add(row);
            return row;
        }

        private void Refresh()
        {
            if (heading == null) return;
            SynchronizeTeam();
            displayed = !SupportedTeam ? null : editing ? draft.ToPreferred().Freeze() : MultiplayerRulesLobbyContext.ReadDisplayed();
            if (!editing) applied = displayed;
            heading.text = string.Format(T(MultiplayerRulesLocalization.LobbyTeamHeading), participants) + " · " + T(editing
                ? MultiplayerRulesLocalization.LobbyDraft : MultiplayerRulesLocalization.LobbyReadOnly);
            if (teamChanged) heading.text += "\n" + T(MultiplayerRulesLocalization.LobbyTeamChanged);
            var position = content.anchoredPosition;
            foreach (var row in rows)
            {
                row.Refresh();
                NativeLocalizedText.MatchFontSize(row.Label, fontTemplate);
                NativeLocalizedText.MatchFontSize(row.Value, fontTemplate);
            }
            foreach (var item in buttons)
            {
                item.label.text = item.key == MultiplayerRulesLocalization.LobbyRestore
                    ? string.Format(T(item.key), participants) : T(item.key);
                NativeLocalizedText.MatchFontSize(item.label, fontTemplate);
                item.button.interactable = item.key == MultiplayerRulesLocalization.LobbyCancel || (CanChangeRules && (item.key != MultiplayerRulesLocalization.LobbyApply || Changes() > 0));
            }
            status.text = editing ? string.Format(T(MultiplayerRulesLocalization.ReviewChanges), Changes()) + "\n" + string.Format(T(MultiplayerRulesLocalization.ReviewScope), participants) : T(MultiplayerRulesLocalization.LobbyReadOnly);
            NativeLocalizedText.MatchFontSize(status, fontTemplate);
            NativeLocalizedText.MatchFontSize(heading, fontTemplate);
            LayoutRebuilder.ForceRebuildLayoutImmediate(content);
            content.anchoredPosition = position;
            RefreshNavigation();
            RefreshPreview();
        }

        private void RefreshPreview()
        {
            if (preview == null) return;
            var definition = MultiplayerRuleCatalog.Get(selectedRule);
            if (Displayed == null)
            {
                preview.text = T(SupportedTeam ? MultiplayerRulesLocalization.LobbyUnavailable : MultiplayerRulesLocalization.LobbyTeamUnsupported);
                NativeLocalizedText.MatchFontSize(preview, fontTemplate);
                return;
            }
            if (selectedHelp != null)
            {
                preview.text = T(selectedHelp);
                NativeLocalizedText.MatchFontSize(preview, fontTemplate);
                return;
            }
            preview.text = T(MultiplayerRulesLocalization.RuleHelpKey(selectedRule)) + "\n" +
                  string.Format(T(MultiplayerRulesLocalization.ReviewValues), NativeRuleReference.Describe(definition, participants),
                      Describe(applied?.Rules.Get(selectedRule, participants) ?? Displayed.Rules.Get(selectedRule, participants), definition),
                      Describe(Displayed.Rules.Get(selectedRule, participants), definition));
            if (definition.Unit != MultiplayerRuleUnit.Toggle)
                preview.text += "\n" + string.Format(T(MultiplayerRulesLocalization.InvalidRange),
                    MultiplayerRulesLocalization.FormatValue(definition.Minimum, definition.Unit), MultiplayerRulesLocalization.FormatValue(definition.Maximum, definition.Unit));
            if (MultiplayerRuleExample.TryCalculate(Displayed, selectedRule, participants, out float result, out bool health))
                preview.text += "\n" + string.Format(T(health ? MultiplayerRulesLocalization.LobbyHealthExample
                    : MultiplayerRulesLocalization.LobbyDamageExample), result.ToString("0.##", CultureInfo.CurrentCulture));
            if (CanChangeRules && definition.Unit != MultiplayerRuleUnit.Toggle)
                preview.text += "\n" + T(MultiplayerRulesLocalization.EditorHint);
            NativeLocalizedText.MatchFontSize(preview, fontTemplate);
        }

        private string Describe(MultiplayerRuleValue<float> value, MultiplayerRuleDefinition definition) =>
            value.TryGetOverride(out float number) ? definition.Unit == MultiplayerRuleUnit.Toggle
                ? T(number > 0 ? MultiplayerRulesLocalization.ToggleEnabled : MultiplayerRulesLocalization.ToggleDisabled)
                : MultiplayerRulesLocalization.FormatValue(number, definition.Unit) : T(MultiplayerRulesLocalization.UseGameBehavior);

        private int Changes()
        {
            if (!SupportedTeam || Displayed == null || applied == null) return 0;
            return MultiplayerRuleCatalog.All.Count(d => !Displayed.Rules.Get(d.Id, participants).Equals(applied.Rules.Get(d.Id, participants)))
                + (Displayed.HealthModifierCombination != applied.HealthModifierCombination ? 1 : 0)
                + (draft.AllowExternalStacking != PreferredMultiplayerRulesStore.ReadAllowExternalRuleStacking() ? 1 : 0);
        }

        private void Apply()
        {
            if (SynchronizeTeam()) { Refresh(); return; }
            if (!CanChangeRules) return;
            PreferredMultiplayerRulesStore.Apply(draft, participants);
            MultiplayerRulesLobbySnapshotCoordinator.PublishLobbyRules();
            NativeRulesBroadcast.Send(PreferredMultiplayerRulesStore.Read().Freeze(), participants, applied);
            Close();
        }

        private bool SynchronizeTeam()
        {
            int currentCount = MultiplayerRulesLobbyContext.ParticipantCount;
            if (participants == currentCount) return false;
            if (ownedDialog != null) { ownedDialog.ForceClose(); ownedDialog = null; }
            participants = currentCount;
            teamChanged = true;
            return true;
        }

        private void Update()
        {
            if (!FeatureFailure.IsAvailable(FeatureId.MultiplayerRules)) { Close(); return; }
            try { UpdateCore(); }
            catch (System.Exception exception) { FeatureFailure.Disable(FeatureId.MultiplayerRules, exception); Close(); }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private void UpdateCore()
        {
            if (owner != LocalPlayerResolver.Resolve() || world != DungeonManager.Instance ||
                !MultiplayerRulesLobbyContext.IsInLobby || (editing && !CanEdit)) { Close(); return; }
            NavigateWithTab();
            ScrollToNewSelection();
            if (Time.unscaledTime < nextRefresh) return;
            nextRefresh = Time.unscaledTime + .2f;
            Refresh();
        }

        public override void OnClosed()
        {
            if (current == this) current = null;
            Destroy(gameObject);
        }

        public override void Close()
        {
            if (ownedDialog != null) { ownedDialog.ForceClose(); ownedDialog = null; }
            base.Close();
            if (returnSelection != null && returnSelection.activeInHierarchy)
                EventSystem.current?.SetSelectedGameObject(returnSelection);
        }

        private void Button(string key, float left, float right, Action action)
        {
            var rect = Rect(transform, key, new Vector2(left, .015f), new Vector2(right, .075f));
            rect.gameObject.AddComponent<Image>().color = new Color(.27f, .29f, .40f);
            var button = rect.gameObject.AddComponent<UnityEngine.UI.Button>();
            var colors = button.colors;
            colors.normalColor = new Color(.72f, .72f, .78f);
            colors.selectedColor = colors.highlightedColor = Color.white;
            button.colors = colors;
            button.onClick.AddListener(() => action());
            var label = Text(rect, "Label", Vector2.zero, Vector2.one);
            label.text = T(key);
            label.alignment = TextAlignmentOptions.Center;
            label.rectTransform.offsetMin = new Vector2(4, 2);
            label.rectTransform.offsetMax = new Vector2(-4, -2);
            buttons.Add((button, label, key));
        }

        private List<Selectable> NavigationControls() => rows.Where(r => r.Root.activeSelf && r.Box.IsInteractable())
            .Select(r => (Selectable)r.Box).Concat(buttons.Where(b => b.button.IsInteractable()).Select(b => (Selectable)b.button)).ToList();

        private void RefreshNavigation()
        {
            var settings = rows.Where(r => r.Root.activeSelf && r.Box.IsInteractable()).Select(r => (Selectable)r.Box).ToList();
            var actions = buttons.Where(b => b.button.IsInteractable()).Select(b => (Selectable)b.button).ToList();
            for (int i = 0; i < settings.Count; i++)
                settings[i].navigation = new Navigation
                {
                    mode = Navigation.Mode.Explicit,
                    selectOnUp = i > 0 ? settings[i - 1] : null,
                    selectOnDown = i + 1 < settings.Count ? settings[i + 1] : actions.FirstOrDefault()
                };
            for (int i = 0; i < actions.Count; i++)
                actions[i].navigation = new Navigation
                {
                    mode = Navigation.Mode.Explicit,
                    selectOnLeft = i > 0 ? actions[i - 1] : null,
                    selectOnRight = i + 1 < actions.Count ? actions[i + 1] : null,
                    selectOnUp = settings.LastOrDefault()
                };
            var selected = EventSystem.current?.currentSelectedGameObject;
            if (ownedDialog == null && IsControlEnabled && selected != null && selected.transform.IsChildOf(transform) &&
                (!selected.activeInHierarchy || selected.GetComponent<Selectable>()?.IsInteractable() == false))
                EventSystem.current.SetSelectedGameObject((settings.FirstOrDefault() ?? actions.First()).gameObject);
        }

        private void NavigateWithTab()
        {
            var keyboard = UnityEngine.InputSystem.Keyboard.current;
            if (!IsControlEnabled || ownedDialog != null || keyboard == null || !keyboard.tabKey.wasPressedThisFrame || EventSystem.current == null) return;
            var controls = NavigationControls();
            int index = controls.FindIndex(c => c.gameObject == EventSystem.current.currentSelectedGameObject);
            int direction = keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed ? -1 : 1;
            EventSystem.current.SetSelectedGameObject(controls[(index + direction + controls.Count) % controls.Count].gameObject);
        }

        private void ScrollToNewSelection()
        {
            var selected = EventSystem.current?.currentSelectedGameObject;
            if (selected == lastSelection) return;
            lastSelection = selected;
            if (selected == null || ControlsChangeHandler.Current?.UseDefaultSelectable != true ||
                !selected.transform.IsChildOf(content)) return;
            var row = rows.FirstOrDefault(r => selected.transform == r.Box.transform || selected.transform.IsChildOf(r.Root.transform));
            if (row == null) return;
            var bounds = RectTransformUtility.CalculateRelativeRectTransformBounds(scroll.viewport, row.Root.transform);
            float delta = bounds.max.y > scroll.viewport.rect.yMax ? bounds.max.y - scroll.viewport.rect.yMax
                : bounds.min.y < scroll.viewport.rect.yMin ? bounds.min.y - scroll.viewport.rect.yMin : 0;
            content.anchoredPosition -= new Vector2(0, delta);
            scroll.StopMovement();
        }

        private TextMeshProUGUI Text(Transform parent, string name, Vector2 min, Vector2 max)
        {
            var rect = Rect(parent, name, min, max);
            var text = rect.gameObject.AddComponent<TextMeshProUGUI>();
            text.font = fontTemplate.font;
            text.fontSharedMaterial = fontTemplate.fontSharedMaterial;
            text.alignment = TextAlignmentOptions.MidlineLeft;
            text.raycastTarget = false;
            NativeLocalizedText.BindFont(text, fontTemplate);
            NativeLocalizedText.MatchFontSize(text, fontTemplate);
            return text;
        }

        private static RectTransform Rect(Transform parent, string name, Vector2 min, Vector2 max)
        {
            var rect = (RectTransform)new GameObject(name, typeof(RectTransform)).transform;
            rect.SetParent(parent, false);
            rect.anchorMin = min; rect.anchorMax = max;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            return rect;
        }

        private static string T(string key) => ModLocalization.Get(key);
    }
}
