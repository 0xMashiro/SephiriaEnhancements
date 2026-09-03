using System;
using System.Globalization;
using Febucci.UI;
using UnityEngine;

namespace SephiriaEnhancements.Integration
{
    internal sealed class NativeHitStreakText
    {
        private readonly TextAnimator_TMP animator;
        private string previousDigits = string.Empty;
        private int animatedFrom;
        private string appearance = "{size a=-0.05 d=0.28}{offset a=0.2 d=1.4}";

        internal NativeHitStreakText(GameObject root)
        {
            // Use the game's text-animation component and its configured effect databases.
            animator = root.AddComponent<TextAnimator_TMP>();
            animator.timeScale = TimeScale.Unscaled;
            animator.typewriterStartsAutomatically = false;
            animator.isResettingTimeOnNewText = false;
            animator.DefaultAppearancesTags = Array.Empty<string>();
            animator.DefaultDisappearancesTags = Array.Empty<string>();
        }

        internal void Show(int count, bool milestone, bool animate)
        {
            string digits = count.ToString(CultureInfo.InvariantCulture);
            if (animate)
            {
                animatedFrom = 0;
                if (!milestone && previousDigits.Length == digits.Length)
                    while (animatedFrom < digits.Length && previousDigits[animatedFrom] == digits[animatedFrom])
                        animatedFrom++;
                // These modifiers scale the game's configured size/offset appearances.
                appearance = milestone
                    ? count >= 100 ? "{size a=-0.12 d=0.44}{offset a=0.65 d=2.2}"
                        : count >= 25 ? "{size a=-0.1 d=0.4}{offset a=0.5 d=2}"
                        : "{size a=-0.08 d=0.36}{offset a=0.35 d=1.8}"
                    : "{size a=-0.05 d=0.28}{offset a=0.2 d=1.4}";
            }
            animatedFrom = Math.Min(animatedFrom, digits.Length);
            string text = digits.Substring(0, animatedFrom) + appearance +
                digits.Substring(animatedFrom) + "{/offset}{/size} <size=68%>HITS</size>";
            previousDigits = digits;
            if (animate)
            {
                animator.SetText(text, true);
                animator.SetVisibilityEntireText(true);
            }
            else
            {
                animator.SwapText(text);
            }
        }

        internal void Reset() => previousDigits = string.Empty;
    }
}
