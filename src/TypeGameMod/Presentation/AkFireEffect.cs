using System;

namespace TypeGameMod
{
    // The approved cream/orange/grey-green palette matches the
    // approved shotgun, with a smaller flash and shorter, thinner smoke.
    internal sealed class AkFireEffect
    {
        internal const int MaxRects = 8;
        internal const double Duration = .18, FlashSeconds = .045;
        static readonly int[] Radii = { 1, 2, 2, 1 };
        internal double Age = Double.PositiveInfinity;
        uint previousShots, heading;
        double originX, originY;
        internal void Update(bool active, uint shots, double x, double y, uint direction, double dt)
        {
            if (!active || shots < previousShots)
                Age = Double.PositiveInfinity;
            else if (shots > previousShots)
            {
                Age = 0;
                originX = x;
                originY = y;
                heading = direction;
            }
            else
                Age += dt;
            previousShots = shots;
        }

        internal void Append(FireEffectBuffer buffer, double muzzleX, double muzzleY, int cameraX,
                             int cameraY)
        {
            muzzleX -= cameraX;
            muzzleY -= cameraY;
            if (Age < Duration)
            {
                double dx, dy;
                Ballistics.AimVector(heading, out dx, out dy);
                if (Age < FlashSeconds)
                {
                    bool peak = Age < .025;
                    for (int i = 0; i < 4; i++)
                    {
                        double distance = 1 + i * (peak ? 1.5 : 1);
                        buffer.Rect(muzzleX + dx * distance, muzzleY + dy * distance, peak ? Radii[i] : 1,
                                    peak ? 0x1078e8u : 0x208de0u);
                    }
                    buffer.Rect(muzzleX + dx, muzzleY + dy, 1, 0xe4faffu);
                    buffer.Rect(muzzleX + dx * 3, muzzleY + dy * 3, 0, 0x60daffu);
                }
                if (Age >= .03)
                {
                    double fade = (Age - .03) / (Duration - .03);
                    for (int i = 0; i < 2; i++)
                    {
                        if (fade > .72 && i == 1)
                            continue;
                        double x = originX - cameraX + dx * (2 + i * 2 + fade * 2) + (i - .5) * fade;
                        double y = originY - cameraY + dy * (2 + i * 2) - fade * (4 + i * 2);
                        buffer.Rect(x, y, fade < .55 ? 1 : 0, fade < .5 ? 0x949f9cu : 0xb1bbb5u);
                    }
                }
            }
        }
    }
}
