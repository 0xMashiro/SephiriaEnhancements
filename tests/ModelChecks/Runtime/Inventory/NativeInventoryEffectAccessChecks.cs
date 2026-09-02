using SephiriaEnhancements.Runtime.GameBridge.Inventory;

namespace SephiriaEnhancements.ModelChecks.Runtime.Inventory;

internal static class NativeInventoryEffectAccessChecks
{
    internal static string Run()
    {
        var effect = new EffectFixture();
        double[] captured = NativeInventoryEffectAccess.Curve(effect, nameof(EffectFixture.Values));
        effect.Values[0] = 73;
        if (captured[0] != 19 || captured[1] != 41)
            throw new InvalidOperationException("runtime curves must be copied without default-value substitution");
        ExpectFailure<MissingFieldException>(() => NativeInventoryEffectAccess.Read(effect, "RemovedField"));
        ExpectFailure<MissingMethodException>(() => NativeInventoryEffectAccess.Method(effect.GetType(), "RemovedMethod"));
        ExpectFailure<InvalidOperationException>(() => NativeInventoryEffectAccess.Read<int>(effect, nameof(EffectFixture.Values)));
        ExpectFailure<InvalidOperationException>(() => NativeInventoryEffectAccess.Curve(effect, nameof(EffectFixture.ChangedType)));
        ExpectFailure<InvalidOperationException>(() => NativeInventoryEffectAccess.Curve(effect, nameof(EffectFixture.NonFinite)));
        ExpectFailure<InvalidOperationException>(() => NativeInventoryEffectAccess.CoordinateOffset(effect.GetType(),
            nameof(EffectFixture.ChangedOffset), nameof(EffectFixture.XIdx)));
        ExpectFailure<InvalidOperationException>(() => NativeInventoryEffectAccess.HalfBoardBoundary(effect.GetType(),
            nameof(EffectFixture.ChangedOffset)));
        ExpectFailure<InvalidOperationException>(() => NativeInventoryEffectAccess.SlotCount(effect.GetType(),
            nameof(EffectFixture.ChangedOffset)));
        return "missing members, changed field types, non-finite curves and changed method shape fail explicitly";
    }

    private static void ExpectFailure<T>(Action action) where T : Exception
    {
        try { action(); }
        catch (T) { return; }
        throw new InvalidOperationException("Expected " + typeof(T).Name);
    }

    private sealed class EffectFixture
    {
        public int[] Values = { 19, 41 };
        public string ChangedType = "19,41";
        public float[] NonFinite = { float.NaN };
        public int XIdx = 7;
        public int ChangedOffset() => XIdx * 3;
    }
}
