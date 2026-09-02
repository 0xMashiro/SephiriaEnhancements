using SephiriaEnhancements.Diagnostics;
using System;
using System.Collections.Generic;
using SephiriaEnhancements.Integration;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SephiriaEnhancements.Configuration
{
    internal readonly struct NativeRebindDefinition
    {
        internal NativeRebindDefinition(string actionName, string labelKey)
            : this(ModShortcuts.MapName, actionName, labelKey)
        {
        }

        internal NativeRebindDefinition(NativeActionId actionId,
            string labelKey)
            : this(actionId.MapName, actionId.ActionName, labelKey)
        {
        }

        internal NativeRebindDefinition(string actionMapName, string actionName,
            string labelKey)
        {
            ActionMapName = actionMapName;
            ActionName = actionName;
            LabelKey = labelKey;
        }

        internal string ActionMapName { get; }
        internal string ActionName { get; }
        internal string LabelKey { get; }
    }

    internal static class NativeRebindSectionBuilder
    {
        internal static void Inject(UI_OptionsPanel panel,
            IReadOnlyList<NativeRebindDefinition> definitions, string group,
            string sectionKey, string ownedActionMapName)
        {
            NativeRebindSectionMarker[] existing =
                panel.GetComponentsInChildren<NativeRebindSectionMarker>(
                    includeInactive: true);
            for (int index = 0; index < existing.Length; index++)
            {
                if (existing[index].BindingGroup == group &&
                    existing[index].SectionKey == sectionKey)
                {
                    return;
                }
            }

            RebindActionUI[] templates =
                panel.GetComponentsInChildren<RebindActionUI>(includeInactive: true);
            RebindActionUI template = FindTemplate(
                templates, group, ownedActionMapName);
            Transform sourceRow = template == null
                ? null : FindRowRoot(template, panel.transform);
            Transform parent = sourceRow?.parent;
            Transform headerSource = parent == null
                ? null : FindPreviousSectionHeader(parent, sourceRow.GetSiblingIndex());
            if (sourceRow == null || parent == null || headerSource == null)
            {
                SupportLogger.Warning("rebind_templates_unavailable", "[SephiriaEnhancements] Native " + group +
                    " rebind section templates could not be found.");
                return;
            }

            List<GameObject> staged = new List<GameObject>();
            try
            {
                int insertionIndex = FindInsertionIndex(
                    parent, group, ownedActionMapName);
                GameObject header = CreateHeader(
                    headerSource, parent, group, sectionKey, insertionIndex);
                staged.Add(header);

                for (int index = 0; index < definitions.Count; index++)
                {
                    NativeRebindDefinition definition = definitions[index];
                    InputAction action = panel.actions?.FindAction(
                        definition.ActionMapName + "/" + definition.ActionName,
                        throwIfNotFound: false);
                    if (action == null)
                    {
                        throw new InvalidOperationException(
                            "Missing shortcut action " + definition.ActionMapName +
                            "/" + definition.ActionName + ".");
                    }

                    GameObject row = CreateBindingRow(
                        sourceRow, action, group, definition.LabelKey);
                    row.transform.SetSiblingIndex(insertionIndex + index + 1);
                    staged.Add(row);
                }

                for (int index = 0; index < staged.Count; index++)
                {
                    staged[index].SetActive(true);
                }

                SupportLogger.Info("rebind_section_attached", "[SephiriaEnhancements] Native " + group +
                    " rebind section attached.");
            }
            catch (Exception ex)
            {
                for (int index = 0; index < staged.Count; index++)
                {
                    staged[index].SetActive(false);
                    UnityEngine.Object.Destroy(staged[index]);
                }

                SupportLogger.Warning("rebind_section_failed", "[SephiriaEnhancements] Native " + group +
                    " rebind section was not attached: " + ex.Message);
            }
        }

        private static GameObject CreateHeader(Transform source, Transform parent,
            string group, string sectionKey, int insertionIndex)
        {
            GameObject header = UnityEngine.Object.Instantiate(source.gameObject, parent);
            header.name = "Section_SephiriaEnhancements_Shortcuts_" + group;
            header.SetActive(false);
            UI_LocalizationStringText[] labels =
                header.GetComponentsInChildren<UI_LocalizationStringText>(true);
            if (labels.Length == 0)
            {
                UnityEngine.Object.Destroy(header);
                throw new InvalidOperationException(
                    "Rebind section heading has no localized label.");
            }

            for (int index = 0; index < labels.Length; index++)
            {
                labels[index].UpdateKey(sectionKey);
            }

            NativeRebindSectionMarker marker =
                header.AddComponent<NativeRebindSectionMarker>();
            marker.Configure(group, sectionKey);
            header.transform.SetSiblingIndex(insertionIndex);
            return header;
        }

        private static GameObject CreateBindingRow(Transform sourceRow,
            InputAction action, string group, string labelKey)
        {
            List<int> targetBindingIndices =
                NativeInputActions.FindBindingIndices(action, group);
            if (targetBindingIndices.Count == 0)
            {
                throw new InvalidOperationException(action.name +
                    " has no " + group + " binding slots.");
            }

            GameObject row = UnityEngine.Object.Instantiate(
                sourceRow.gameObject, sourceRow.parent);
            row.name = "Control_SephiriaEnhancements_" + action.name + "_" + group;
            row.SetActive(false);

            RebindActionUI[] clonedBindings =
                row.GetComponentsInChildren<RebindActionUI>(includeInactive: true);
            List<RebindActionUI> bindingSlots =
                FindMatchingClones(clonedBindings, group);
            if (bindingSlots.Count < targetBindingIndices.Count)
            {
                UnityEngine.Object.Destroy(row);
                throw new InvalidOperationException(action.name + " needs " +
                    targetBindingIndices.Count + " binding slots, but the native row has " +
                    bindingSlots.Count + ".");
            }

            InputActionReference actionReference = InputActionReference.Create(action);
            for (int index = 0; index < clonedBindings.Length; index++)
            {
                int slotIndex = bindingSlots.IndexOf(clonedBindings[index]);
                if (slotIndex >= 0 && slotIndex < targetBindingIndices.Count)
                {
                    clonedBindings[index].actionReference = actionReference;
                    clonedBindings[index].bindingId = action.bindings[
                        targetBindingIndices[slotIndex]].id.ToString();
                    clonedBindings[index].gameObject.SetActive(true);
                }
                else
                {
                    clonedBindings[index].gameObject.SetActive(false);
                }
            }

            UI_LocalizationStringText[] labels =
                row.GetComponentsInChildren<UI_LocalizationStringText>(
                    includeInactive: true);
            if (labels.Length == 0)
            {
                UnityEngine.Object.Destroy(actionReference);
                UnityEngine.Object.Destroy(row);
                throw new InvalidOperationException(action.name +
                    " row has no localized label.");
            }

            labels[0].UpdateKey(labelKey);
            NativeRebindRowMarker marker =
                row.AddComponent<NativeRebindRowMarker>();
            marker.Configure(action.name, group, actionReference);
            return row;
        }

        private static RebindActionUI FindTemplate(RebindActionUI[] candidates,
            string group, string ownedActionMapName)
        {
            RebindActionUI best = null;
            int bestSlotCount = -1;
            for (int index = 0; index < candidates.Length; index++)
            {
                RebindActionUI candidate = candidates[index];
                if (!candidate.ResolveActionAndBinding(out InputAction action,
                    out int bindingIndex))
                {
                    continue;
                }

                InputBinding binding = action.bindings[bindingIndex];
                if (!NativeInputActions.HasGroup(binding, group) ||
                    action.actionMap == null || action.actionMap.name == "UI" ||
                    action.actionMap.name == "Magic_Joystick" ||
                    action.actionMap.name == ownedActionMapName)
                {
                    continue;
                }

                int slotCount = NativeInputActions.FindBindingIndices(
                    action, group).Count;
                if (slotCount > bestSlotCount)
                {
                    best = candidate;
                    bestSlotCount = slotCount;
                }
            }

            return best;
        }

        private static List<RebindActionUI> FindMatchingClones(
            RebindActionUI[] candidates, string group)
        {
            List<RebindActionUI> matches = new List<RebindActionUI>();
            for (int index = 0; index < candidates.Length; index++)
            {
                if (candidates[index].ResolveActionAndBinding(out InputAction action,
                    out int bindingIndex) &&
                    NativeInputActions.HasGroup(
                        action.bindings[bindingIndex], group))
                {
                    matches.Add(candidates[index]);
                }
            }

            if (matches.Count == 0)
            {
                matches.AddRange(candidates);
            }

            return matches;
        }

        private static Transform FindRowRoot(RebindActionUI binding, Transform panel)
        {
            Transform current = binding.transform;
            Transform fallback = current.parent;
            for (int depth = 0; depth < 6 && current != null && current != panel; depth++)
            {
                RebindActionUI[] bindings =
                    current.GetComponentsInChildren<RebindActionUI>(
                        includeInactive: true);
                UI_LocalizationStringText[] labels =
                    current.GetComponentsInChildren<UI_LocalizationStringText>(
                        includeInactive: true);
                if (bindings.Length > 0 && labels.Length > 0)
                {
                    return current;
                }

                current = current.parent;
            }

            return fallback;
        }

        private static Transform FindPreviousSectionHeader(Transform parent,
            int beforeIndex)
        {
            for (int index = beforeIndex - 1; index >= 0; index--)
            {
                Transform candidate = parent.GetChild(index);
                if (candidate.GetComponentInChildren<RebindActionUI>(true) == null &&
                    candidate.GetComponentInChildren<UI_LocalizationStringText>(true) != null)
                {
                    return candidate;
                }
            }

            return null;
        }

        private static int FindInsertionIndex(Transform parent, string group,
            string ownedActionMapName)
        {
            int lastRow = -1;
            for (int childIndex = 0; childIndex < parent.childCount; childIndex++)
            {
                RebindActionUI[] bindings = parent.GetChild(childIndex)
                    .GetComponentsInChildren<RebindActionUI>(true);
                for (int bindingIndex = 0; bindingIndex < bindings.Length; bindingIndex++)
                {
                    if (bindings[bindingIndex].ResolveActionAndBinding(
                            out InputAction action, out int index) &&
                        action.actionMap?.name != ownedActionMapName &&
                        NativeInputActions.HasGroup(action.bindings[index], group))
                    {
                        lastRow = childIndex;
                        break;
                    }
                }
            }

            return lastRow >= 0 ? lastRow + 1 : parent.childCount;
        }
    }

    internal sealed class NativeRebindSectionMarker : MonoBehaviour
    {
        internal string BindingGroup { get; private set; }
        internal string SectionKey { get; private set; }

        internal void Configure(string bindingGroup, string sectionKey)
        {
            BindingGroup = bindingGroup;
            SectionKey = sectionKey;
        }
    }

    internal sealed class NativeRebindRowMarker : MonoBehaviour
    {
        private InputActionReference actionReference;

        internal string ActionName { get; private set; }
        internal string BindingGroup { get; private set; }

        internal void Configure(string actionName, string bindingGroup,
            InputActionReference reference)
        {
            ActionName = actionName;
            BindingGroup = bindingGroup;
            actionReference = reference;
        }

        private void OnDestroy()
        {
            if (actionReference != null)
            {
                UnityEngine.Object.Destroy(actionReference);
                actionReference = null;
            }
        }
    }
}
