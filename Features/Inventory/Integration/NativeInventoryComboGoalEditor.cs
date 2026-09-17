#nullable disable
using System;
using System.Collections.Generic;
using System.Linq;
using SephiriaEnhancements.Runtime.Inventory;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static SephiriaEnhancements.Inventory.NativeInventoryHudControls;

namespace SephiriaEnhancements.Inventory
{
    internal sealed class NativeInventoryComboGoalEditor
    {
        private const int RowsPerPage = InventoryOptimizationHudLayout.TargetRowsPerPage;
        private readonly List<TargetRow> rows = new();
        private readonly NativeInventoryHudControls controls;
        private readonly Action<string, InventoryComboGoalEdit> edit;
        private readonly Action changed;
        private readonly InventoryIntentInteractionState interaction;
        private string expandedCategoryId;

        internal int Page { get; private set; }
        internal int PageCount { get; private set; } = 1;
        internal int TargetCount { get; private set; }
        internal Button FirstChoice => rows.FirstOrDefault(row => row.Root.activeInHierarchy)?.Choice;
        internal Button LastChoice => rows.LastOrDefault(row => row.Root.activeInHierarchy)?.Choice;

        internal NativeInventoryComboGoalEditor(RectTransform parent, TextMeshProUGUI template,
            NativeInventoryHudControls controls, InventoryIntentInteractionState interaction,
            Action<string, InventoryComboGoalEdit> edit, Action changed)
        {
            this.controls = controls;
            this.interaction = interaction;
            this.edit = edit;
            this.changed = changed;
            for (int index = 0; index < RowsPerPage; index++)
                rows.Add(CreateTargetRow(parent, template, index));
        }

        internal void ResetPage()
        {
            Page = 0;
            expandedCategoryId = null;
        }

        internal void ChangePage(int delta)
        {
            Page = Math.Max(0, Page + delta);
            expandedCategoryId = null;
        }

        internal void Hide()
        {
            foreach (var row in rows)
            {
                row.Target = null;
                row.Root.SetActive(false);
            }
        }

        private void Toggle(TargetRow row)
        {
            if (!interaction.Editable || row.Target?.CanAdjustRequiredValue != true) return;
            expandedCategoryId = expandedCategoryId == row.Target.CategoryId ? null : row.Target.CategoryId;
            changed();
        }

        internal void ChoiceEdited(string categoryId, InventoryOptimizationPreferences preferences)
        {
            expandedCategoryId = preferences.ComboPreferences.Any(rule => rule.CategoryId == categoryId)
                ? categoryId : null;
        }

        internal void Render(InventorySnapshot snapshot, InventoryOptimizationPreferences preferences,
            InventoryIntentResultFeedback feedback)
        {
            bool editable = interaction.Editable;
            IReadOnlyList<InventoryComboTarget> targets =
                InventoryComboTargetEditor.BuildTargets(snapshot, preferences);
            PageCount = Math.Max(1,
                (targets.Count + RowsPerPage - 1) / RowsPerPage);
            Page = Mathf.Clamp(Page, 0, PageCount - 1);
            TargetCount = targets.Count;
            float rowTop = InventoryOptimizationHudLayout.TargetRowsTop;
            for (int rowIndex = 0; rowIndex < rows.Count; rowIndex++)
            {
                int targetIndex = Page * RowsPerPage + rowIndex;
                TargetRow row = rows[rowIndex];
                if (targetIndex >= targets.Count)
                {
                    row.Target = null;
                    row.Root.SetActive(false);
                    continue;
                }

                InventoryComboTarget target = targets[targetIndex];
                row.Target = target;
                row.Root.SetActive(true);
                bool expanded = target.CanAdjustRequiredValue && target.CategoryId == expandedCategoryId;
                float rowHeight = InventoryOptimizationHudLayout.TargetRowHeight(expanded);
                SetTopRect((RectTransform)row.Root.transform, new Vector2(8f, -rowTop),
                    new Vector2(344f, rowHeight));
                rowTop += rowHeight + InventoryOptimizationHudLayout.TargetRowGap;
                row.Name.text = target.FruitSkewerPriority > 0
                    ? $"{DisplayName(target)} · ↑{target.FruitSkewerPriority}" : DisplayName(target);
                row.Name.color = expanded ? TitleColor : PrimaryText;
                row.Select.interactable = editable && target.CanAdjustRequiredValue;
                string condition = InventoryOptimizationLocalization.FormatTargetCondition(target, key => Loc._(key));
                row.ChoiceText.text = !expanded && target.CanAdjustRequiredValue ? condition : Loc._(
                    InventoryOptimizationLocalization.PreferenceChoiceKeys[
                        (int)target.Choice]);
                row.Value.text = condition;
                row.Value.gameObject.SetActive(expanded);
                row.Value.color = NativeInventoryHudControls.SatisfactionColor(feedback?.FindCombo(target.CategoryId) ?? InventoryIntentSatisfaction.NotEvaluated);
                row.ChoiceText.color = target.CanAdjustRequiredValue ? row.Value.color : PrimaryText;
                row.Strength.gameObject.SetActive(expanded);
                row.Strength.interactable = editable;
                row.StrengthText.text = Loc._(target.Strength == InventoryConstraintStrength.Hard
                    ? InventoryOptimizationLocalization.HudHard : InventoryOptimizationLocalization.HudSoft);
                row.Decrease.gameObject.SetActive(expanded);
                row.Increase.gameObject.SetActive(expanded);
                row.Choice.interactable = editable;
                row.Decrease.interactable = editable &&
                    target.CanAdjustRequiredValue && target.RequiredValue > 0;
                row.Increase.interactable = editable &&
                    target.CanAdjustRequiredValue &&
                    target.RequiredValue < target.MaximumValue;
            }

        }

        private TargetRow CreateTargetRow(RectTransform parent,
            TextMeshProUGUI template, int index)
        {
            var row = new TargetRow
            {
                Root = new GameObject("TargetRow" + index,
                    typeof(RectTransform))
            };
            RectTransform rect = row.Root.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            SetTopRect(rect, new Vector2(8f, -InventoryOptimizationHudLayout.TargetRowsTop),
                new Vector2(344f, InventoryOptimizationHudLayout.TargetRowHeight(false)));
            row.Select = controls.CreateButton("Name", rect, template,
                new Vector2(16f, 0f), new Vector2(190f, 26f),
                () => Toggle(row), out row.Name);
            row.Name.alignment = TextAlignmentOptions.MidlineLeft;
            row.Name.color = PrimaryText;
            ColorBlock nameColors = row.Select.colors;
            nameColors.normalColor = nameColors.disabledColor = Color.clear;
            nameColors.highlightedColor = nameColors.selectedColor = new Color(1f, 1f, 1f, 0.12f);
            nameColors.pressedColor = new Color(1f, 1f, 1f, 0.2f);
            row.Select.colors = nameColors;
            row.Choice = controls.CreateButton("Choice", rect, template,
                new Vector2(208f, 0f), new Vector2(120f, 26f),
                () => edit(row.Target?.CategoryId, InventoryComboGoalEdit.CycleChoice), out row.ChoiceText);
            var tooltip = row.Choice.gameObject.AddComponent<UI_CommonTooltipOpener>();
            tooltip.tooltipContext = new LocalizedString(InventoryPresetIntentLocalization.Help);
            tooltip.UpdateTooltipData();
            row.Decrease = controls.CreateButton("Decrease", rect, template,
                new Vector2(260f, -28f), new Vector2(30f, 24f),
                () => edit(row.Target?.CategoryId, InventoryComboGoalEdit.DecreaseCount), out row.DecreaseText);
            row.Value = CreateText("Value", rect, template,
                new Vector2(128f, -28f), new Vector2(124f, 24f),
                TextAlignmentOptions.MidlineLeft);
            row.Strength = controls.CreateButton("ConstraintStrength", rect, template,
                new Vector2(16f, -28f), new Vector2(106f, 24f), () => edit(row.Target?.CategoryId, InventoryComboGoalEdit.ToggleStrength), out row.StrengthText);
            row.Value.color = SecondaryText;
            row.Value.fontSize *= 0.8f;
            row.Increase = controls.CreateButton("Increase", rect, template,
                new Vector2(298f, -28f), new Vector2(30f, 24f),
                () => edit(row.Target?.CategoryId, InventoryComboGoalEdit.IncreaseCount), out row.IncreaseText);
            row.DecreaseText.text = "−";
            row.IncreaseText.text = "+";
            return row;
        }

        private static string DisplayName(InventoryComboTarget target)
        {
            // Native integration boundary: categoryName is the game's
            // localized ItemCategoryEntity label, while CategoryId remains
            // the stable optimizer identifier.
            try
            {
                ItemCategoryEntity category =
                    ItemDatabase.FindItemCategory(target.CategoryId);
                string name = category?.categoryName?.ToString();
                return string.IsNullOrEmpty(name) ? target.CategoryId : name;
            }
            catch
            {
                return target.CategoryId;
            }
        }

        private sealed class TargetRow
        {
            internal GameObject Root;
            internal Button Select;
            internal TextMeshProUGUI Name;
            internal Button Choice;
            internal Button Strength;
            internal TextMeshProUGUI StrengthText;
            internal TextMeshProUGUI ChoiceText;
            internal Button Decrease;
            internal TextMeshProUGUI DecreaseText;
            internal TextMeshProUGUI Value;
            internal Button Increase;
            internal TextMeshProUGUI IncreaseText;
            internal InventoryComboTarget Target;
        }
    }
}
