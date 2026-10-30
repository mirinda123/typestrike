namespace TypeGameMod
{
    // Match the measured original nine-pose cycle, independent of the legacy
    // 15 FPS legacy clock. Unarmed, AK, shotgun and shield share this local/LAN phase.
    internal sealed class PoliceIdleAnimation
    {
        internal const int Frames = 9;
        internal const double CycleSeconds = .5179694999969797;
        double elapsed;
        internal int Update(bool standing, double dt)
        {
            elapsed = standing ? (elapsed + dt) % CycleSeconds : 0;
            return (int)(elapsed * Frames / CycleSeconds);
        }
    }
}
