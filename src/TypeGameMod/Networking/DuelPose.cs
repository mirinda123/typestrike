using System;

namespace TypeGameMod
{
    internal struct DuelPose
    {
        internal float X, Y;
        internal byte Heading, Weapon, Moving, Phase;
        internal bool Valid
        {
            get {
                return !float.IsNaN(X) && !float.IsInfinity(X) && !float.IsNaN(Y) && !float.IsInfinity(Y) &&
                       X >= 24 && X <= 1576 && Y >= 32 && Y <= 1168 && Heading < 8 && Weapon < 3 &&
                       Moving < 2 && Phase < 4;
            }
        }

        internal static DuelPose Spawn(int role)
        {
            return new DuelPose { X = role == 0 ? 324 : 48, Y = role == 0 ? 880 : 744,
                                  Heading = (byte)(role == 0 ? 7 : 1) };
        }
    }
}
