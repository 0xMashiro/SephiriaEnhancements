namespace SephiriaEnhancements.DeveloperTools.Core
{
    internal static class DeveloperPlayerDamagePolicy
    {
        private static readonly float[] Multipliers = { 1f, 2f, 5f, 10f, 100f };

        internal static int MultiplierCount => Multipliers.Length;

        internal static int NormalizeIndex(int index)
        {
            if (index < 0) return 0;
            return index >= Multipliers.Length ? Multipliers.Length - 1 : index;
        }

        internal static float GetMultiplier(int index) =>
            Multipliers[NormalizeIndex(index)];

        internal static float Apply(float damage, int multiplierIndex) =>
            damage * GetMultiplier(multiplierIndex);
    }
}
