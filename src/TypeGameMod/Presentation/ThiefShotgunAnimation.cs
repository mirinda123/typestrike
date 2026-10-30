using System;

namespace TypeGameMod
{
    // The 24-pose idle and accepted nine-pose recoil share two native banks.
    // A real ammunition event starts recoil; holding fire with an empty gun does not.
    internal sealed class ThiefShotgunAnimation
    {
        internal const int ShotFirstFrame = 24, ShotFrames = 9;
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
            return (int)(idleTime * ThiefShotgunPose.IdleFrames / IdleSeconds);
        }
    }
}
