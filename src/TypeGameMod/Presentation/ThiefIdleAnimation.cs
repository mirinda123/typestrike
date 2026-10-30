namespace TypeGameMod
{
    // The idle shares the measured original thief cycle, with 24 authored poses.
    // Its visual phase stays local, just like the unarmed run animation.
    internal sealed class ThiefIdleAnimation
    {
        internal const int Frames = 24;
        internal const double CycleSeconds = .6295542111102905;
        double elapsed;
        internal int Update(bool standing, double dt)
        {
            elapsed = standing ? (elapsed + dt) % CycleSeconds : 0;
            return (int)(elapsed * Frames / CycleSeconds);
        }
    }
}
