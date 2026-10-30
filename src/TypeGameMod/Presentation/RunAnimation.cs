namespace TypeGameMod
{
    // Each character/weapon run owns a clock for its 24 authored poses. Native movement
    // and the four running phases on the existing network wire stay unchanged.
    internal sealed class RunAnimation
    {
        internal const int Frames = 24;
        internal const double CycleSeconds = .64;
        double elapsed;
        internal int Update(bool moving, double dt)
        {
            elapsed = moving ? (elapsed + dt) % CycleSeconds : 0;
            return (int)(elapsed * Frames / CycleSeconds);
        }
    }
}
