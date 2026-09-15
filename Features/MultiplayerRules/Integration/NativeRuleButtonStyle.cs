using UnityEngine;
using UnityEngine.UI;

namespace SephiriaEnhancements.MultiplayerRules.Integration
{
    internal static class NativeRuleButtonStyle
    {
        internal static void Apply(Button button)
        {
            var image = button.GetComponent<Image>();
            image.color = Color.white;
            button.targetGraphic = image;
            button.transition = Selectable.Transition.ColorTint;
            var colors = button.colors;
            colors.normalColor = new Color(.30f, .32f, .44f);
            colors.highlightedColor = colors.selectedColor = new Color(.52f, .55f, .72f);
            colors.pressedColor = new Color(.23f, .25f, .36f);
            colors.disabledColor = new Color(.21f, .22f, .29f);
            colors.colorMultiplier = 1;
            colors.fadeDuration = .08f;
            button.colors = colors;
        }
    }
}
