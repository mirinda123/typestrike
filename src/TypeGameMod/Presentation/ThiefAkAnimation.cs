using System;

namespace TypeGameMod
{
    // The 24-pose idle and accepted five-pose recoil share two native banks.
    // A real ammunition event starts recoil; holding fire with an empty gun does not.
    internal sealed class ThiefAkAnimation
    {
        internal const int ShotFirstFrame = 24, ShotFrames = 5;
        internal const double IdleSeconds = .6295542111102905, ShotFramesPerSecond = 30;
        double idleTime, shotTime = Double.PositiveInfinity;
        uint previousShots;
        internal int Update(bool active, uint shots, double dt)
        {
            if (shots < previousShots || !active)
                shotTime = Double.PositiveInfinity;
            if (active)
            {
                idleTime = (idleTime + dt) % IdleSeconds;
                shotTime = shots > previousShots ? 0 : shotTime + dt;
            }
            previousShots = shots;
            if (shotTime < ShotFrames / ShotFramesPerSecond)
                return ShotFirstFrame + (int)(shotTime * ShotFramesPerSecond);
            return (int)(idleTime * ThiefAkPose.IdleFrames / IdleSeconds);
        }
    }
}
