using TypeGameMod;

internal static class TestFireEffects
{
    internal static void Write(AkFireEffect effect, byte[] snapshot, double x, double y, int cx, int cy)
    {
        var buffer = new FireEffectBuffer(snapshot);
        effect.Append(buffer, x, y, cx, cy);
        buffer.Finish();
    }

    internal static void Write(ShotgunFireEffect effect, byte[] snapshot, double x, double y, int cx, int cy)
    {
        var buffer = new FireEffectBuffer(snapshot);
        effect.Append(buffer, x, y, cx, cy);
        buffer.Finish();
    }
}
