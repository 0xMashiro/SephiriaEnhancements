using SephiriaEnhancements.Integration;
using System.Reflection;

namespace SephiriaEnhancements.ModelChecks.Integration;

internal static class NativeUiActionCatalogChecks
{
    internal static string Run()
    {
        NativeActionId[] declaredActions = typeof(NativeUiActions)
            .GetFields(BindingFlags.Static | BindingFlags.NonPublic)
            .Where(field => field.FieldType == typeof(NativeActionId))
            .Select(field => (NativeActionId)field.GetValue(null)!)
            .ToArray();

        if (NativeUiActions.Navigate.MapName != "UI" ||
            NativeUiActions.All.Length != 22 ||
            declaredActions.Length != NativeUiActions.All.Length ||
            declaredActions.Any(action => !NativeUiActions.All.Contains(action)) ||
            NativeUiActions.All.Select(action => action.QualifiedName)
                .Distinct(StringComparer.Ordinal).Count() !=
                NativeUiActions.All.Length ||
            !new NativeActionId("UI", "RotateItem").Equals(
                NativeUiActions.RotateItem) ||
            new NativeActionId("Player", "RotateItem").Equals(
                NativeUiActions.RotateItem) ||
            NativeUiActions.RequiredByKeyboardNavigation.Length != 5 ||
            NativeUiActions.RequiredByKeyboardNavigation.Any(
                action => !NativeUiActions.All.Contains(action)) ||
            !NativeUiActions.RequiredByKeyboardNavigation.Contains(
                NativeUiActions.Navigate) ||
            !NativeUiActions.RequiredByKeyboardNavigation.Contains(
                NativeUiActions.Submit) ||
            !NativeUiActions.RequiredByKeyboardNavigation.Contains(
                NativeUiActions.ThrowItem) ||
            !NativeUiActions.RequiredByKeyboardNavigation.Contains(
                NativeUiActions.RotateItem) ||
            !NativeUiActions.RequiredByKeyboardNavigation.Contains(
                NativeUiActions.EngraveTablet))
        {
            throw new InvalidOperationException(
                "native UI action identifiers must remain map-qualified and unique");
        }

        return "canonical UI action names are map-qualified, unique and covered by the runtime dependency probe";
    }
}
