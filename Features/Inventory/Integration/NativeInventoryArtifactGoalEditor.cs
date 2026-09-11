using System;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using SephiriaEnhancements.Integration;
using SephiriaEnhancements.Runtime.Inventory;
using static SephiriaEnhancements.Inventory.NativeInventoryHudControls;

namespace SephiriaEnhancements.Inventory
{
    internal sealed class NativeInventoryArtifactGoalEditor : IDisposable
    {
        private readonly NativeInventoryOptimizationViewTemplates nativeTemplates;
        private GameObject levelEditor;
        private TextMeshProUGUI levelTargetName;
        private TextMeshProUGUI levelTargetLabel;
        private TextMeshProUGUI levelRequirementLabel;
        private TextMeshProUGUI levelSummary;
        private TextMeshProUGUI levelBackText;
        private Button levelBack;
        private TextMeshProUGUI levelCondition;
        private Button levelMode;
        private Button constraintStrength;
        private TextMeshProUGUI constraintStrengthText;
        private Button decreaseLevel;
        private Button increaseLevel;

        internal bool Visible => levelEditor != null && levelEditor.activeSelf;
        internal bool ActiveInHierarchy => levelEditor != null && levelEditor.activeInHierarchy;
        internal void SetVisible(bool visible) { if (levelEditor != null) levelEditor.SetActive(visible); }
        internal bool Contains(GameObject selected) => selected != null && levelEditor != null && selected.transform.IsChildOf(levelEditor.transform);
        internal GameObject Entry => levelMode.IsInteractable() ? levelMode.gameObject : constraintStrength.gameObject;
        internal void SelectEntry() => EventSystem.current?.SetSelectedGameObject(Entry);
        public void Dispose()
        {
            if (levelEditor != null) UnityEngine.Object.Destroy(levelEditor);
            levelEditor = null;
        }

        internal NativeInventoryArtifactGoalEditor(RectTransform parent, TextMeshProUGUI template,
            NativeInventoryOptimizationViewTemplates nativeTemplates, NativeInventoryHudControls controls,
            Action<InventoryArtifactGoalEdit> edit, Action close)
        {
            this.nativeTemplates = nativeTemplates;
            levelEditor = new GameObject("ArtifactLevelEditor", typeof(RectTransform));
            var rect = levelEditor.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            SetTopRect(rect, new Vector2(24f, -104f), new Vector2(312f, 324f));
            // Use the native content-button text for controls and body text,
            // preserving the inventory canvas's two design units per native unit.
            template = (nativeTemplates.ContentButton as UI_HorayButton)?.text ?? template;
            levelTargetName = CreateText("ArtifactName", rect, template,
                Vector2.zero, new Vector2(312f, 32f), TextAlignmentOptions.MidlineLeft);
            NativeLocalizedText.SetShrinkOnlySize(levelTargetName, levelTargetName.fontSize, levelTargetName.fontSize * 0.75f);
            levelTargetLabel = CreateText("TargetLabel", rect, template,
                new Vector2(0f, -38f), new Vector2(312f, 24f), TextAlignmentOptions.MidlineLeft);
            levelMode = controls.CreateButton("TargetMode", rect, template,
                new Vector2(0f, -62f), new Vector2(228f, 36f),
                () => edit(InventoryArtifactGoalEdit.CycleTargetMode), out levelCondition);
            decreaseLevel = controls.CreateButton("DecreaseLevel", rect, template,
                new Vector2(236f, -62f), new Vector2(30f, 36f),
                () => edit(InventoryArtifactGoalEdit.DecreaseLevel), out var decreaseText);
            increaseLevel = controls.CreateButton("IncreaseLevel", rect, template,
                new Vector2(282f, -62f), new Vector2(30f, 36f),
                () => edit(InventoryArtifactGoalEdit.IncreaseLevel), out var increaseText);
            levelRequirementLabel = CreateText("RequirementLabel", rect, template,
                new Vector2(0f, -106f), new Vector2(312f, 24f), TextAlignmentOptions.MidlineLeft);
            constraintStrength = controls.CreateButton("ConstraintStrength", rect, template,
                new Vector2(0f, -134f), new Vector2(312f, 36f),
                () => edit(InventoryArtifactGoalEdit.ToggleStrength), out constraintStrengthText);
            levelSummary = CreateText("GoalSummary", rect, template,
                new Vector2(0f, -174f), new Vector2(312f, 104f), TextAlignmentOptions.TopLeft);
            levelSummary.textWrappingMode = TextWrappingModes.Normal;
            NativeLocalizedText.SetShrinkOnlySize(levelSummary, levelSummary.fontSize, levelSummary.fontSize * 0.75f);
            levelBack = controls.CreateButton("BackToArtifacts", rect, template,
                new Vector2(0f, -288f), new Vector2(312f, 32f), close, out levelBackText);
            decreaseText.text = "−";
            increaseText.text = "+";
        }

        internal void Render(ArtifactOptimizationPreference rule, InventoryItemSnapshot item,
            bool editable, bool allowAdditionalMagicCost, InventoryArtifactGoalFeedback feedback)
        {
            var nativeText = (nativeTemplates.ContentButton as UI_HorayButton)?.text;
            if (nativeText != null)
            {
                float designSize = nativeText.fontSize * InventoryOptimizationHudLayout.NativeUnitScale;
                foreach (var text in new[] { levelTargetName, levelTargetLabel, levelRequirementLabel,
                    levelCondition, constraintStrengthText, levelSummary, levelBackText })
                    NativeLocalizedText.SetShrinkOnlySize(text, designSize, designSize * 0.75f);
            }
            levelTargetName.text = string.Format(Loc._(InventoryOptimizationLocalization.HudGoalTitle), item.Name);
            levelTargetLabel.text = Loc._(InventoryOptimizationLocalization.HudGoalTarget);
            levelRequirementLabel.text = Loc._(InventoryOptimizationLocalization.HudGoalRequirement);
            levelBackText.text = Loc._(InventoryOptimizationLocalization.HudGoalBack);
            constraintStrengthText.text = Loc._(rule.Strength == InventoryConstraintStrength.Hard
                ? InventoryOptimizationLocalization.HudGoalHard : InventoryOptimizationLocalization.HudGoalSoft);
            constraintStrength.interactable = editable;
            levelCondition.text = InventoryOptimizationLocalization.FormatArtifactTarget(rule, item.Artifact, key => Loc._(key), allowAdditionalMagicCost: allowAdditionalMagicCost);
            levelSummary.text = InventoryOptimizationLocalization.FormatArtifactFeedback(rule, item.Artifact, feedback, key => Loc._(key), allowAdditionalMagicCost) + "\n" + Loc._(InventoryOptimizationLocalization.HudArtifactPersistence);
            levelMode.interactable = editable && rule.Level == InventoryPreferenceLevel.Priority;
            bool specified = rule.Level == InventoryPreferenceLevel.Priority && rule.TargetMode == ArtifactLevelTargetMode.SpecifiedLevel;
            // Move focus before disabling the selected control. Native selection
            // recovery must never mistake an exhausted adjustment for leaving the editor.
            GameObject selected = EventSystem.current?.currentSelectedGameObject;
            bool canDecrease = editable && specified && rule.MinimumEffectiveLevel > 1;
            bool canIncrease = editable && specified && rule.MinimumEffectiveLevel < item.Artifact.MaxLevel;
            if (selected == decreaseLevel.gameObject && !canDecrease ||
                selected == increaseLevel.gameObject && !canIncrease)
                EventSystem.current?.SetSelectedGameObject(levelMode.IsInteractable() ? levelMode.gameObject : constraintStrength.gameObject);
            decreaseLevel.interactable = canDecrease;
            increaseLevel.interactable = canIncrease;
            decreaseLevel.gameObject.SetActive(specified);
            increaseLevel.gameObject.SetActive(specified);
            SetTopRect((RectTransform)levelMode.transform, new Vector2(0f, -62f), new Vector2(specified ? 228f : 312f, 36f));
            RefreshNavigation();
        }

        internal void RefreshNavigation()
        {
            var targets = new[] { levelMode, decreaseLevel, increaseLevel }
                .Where(button => button.gameObject.activeInHierarchy && button.IsInteractable()).ToArray();
            for (int index = 0; index < targets.Length; index++)
                SetEditorNavigation(targets[index], targets[System.Math.Max(0, index - 1)],
                    targets[System.Math.Min(targets.Length - 1, index + 1)], targets[index], constraintStrength);
            SetEditorNavigation(constraintStrength, constraintStrength, constraintStrength,
                targets.FirstOrDefault() ?? constraintStrength, levelBack);
            SetEditorNavigation(levelBack, levelBack, levelBack, constraintStrength, levelBack);
        }

        private static void SetEditorNavigation(Button button, Selectable left, Selectable right,
            Selectable up, Selectable down)
        {
            // Explicit links cover Unity navigation; native forced links also
            // cover the game's AABB navigation. Self-links stop at editor edges.
            button.navigation = new Navigation
            {
                mode = Navigation.Mode.Explicit,
                selectOnLeft = left,
                selectOnRight = right,
                selectOnUp = up,
                selectOnDown = down
            };
            var native = (UI_HorayButton)button;
            native.SetForceNavLeft(left);
            native.SetForceNavRight(right);
            native.SetForceNavUp(up);
            native.SetForceNavDown(down);
        }
    }
}
