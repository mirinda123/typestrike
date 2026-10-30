using System;

namespace TypeGameMod
{
    // Each player owns one instance. Smoke stays at the shot's world position
    // while the flash follows the current barrel, including during movement.
    internal sealed class ShotgunFireEffect
    {
        internal const int MaxRects = 11;
        internal const double Duration = .30, FlashSeconds = .07;
        static readonly int[] FlashRadii = { 1, 2, 3, 2, 1 };
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
            if (Age < Duration)
            {
                double dx, dy;
                Ballistics.AimVector(heading, out dx, out dy);
                if (Age < FlashSeconds)
                {
                    bool peak = Age < .035;
                    for (int i = 0; i < 5; i++)
                    {
                        int radius = peak ? FlashRadii[i] : 1;
                        double distance = 1 + i * (peak ? 2 : 1.4);
                        buffer.Rect(muzzleX - cameraX + dx * distance, muzzleY - cameraY + dy * distance,
                                    radius, peak ? 0x1078e8u : 0x208de0u);
                    }
                    for (int i = 0; i < 3; i++)
                        buffer.Rect(muzzleX - cameraX + dx * (1 + i * 2),
                                    muzzleY - cameraY + dy * (1 + i * 2), 1, i == 0 ? 0xe4faffu : 0x60daffu);
                }
                if (Age >= .045)
                {
                    double fade = (Age - .045) / (Duration - .045);
                    for (int i = 0; i < 3; i++)
                    {
                        // Small staggered puffs rise and separate. Late smoke
                        // loses pixels instead of becoming an opaque grey disk.
                        if ((fade > .70 && i == 1) || (fade > .88 && i == 2))
                            continue;
                        double x = originX - cameraX + dx * (3 + i * 2 + fade * 3) + (i - 1) * fade * 2;
                        double y = originY - cameraY + dy * (3 + i * 2) - fade * (5 + i * 2);
                        buffer.Rect(x, y, fade < .48 && i == 1 ? 2 : 1, fade < .5 ? 0x949f9cu : 0xb1bbb5u);
                    }
                }
            }
        }
    }
}
