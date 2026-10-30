using System;

namespace TypeGameMod
{
    internal static class Ballistics
    {
        internal static void ProjectileDirection(int weapon, uint heading, int pellet, out double dx,
                                                 out double dy)
        {
            if (weapon == GameRules.Shotgun)
                PelletVector(heading, pellet, out dx, out dy);
            else
                AimVector(heading, out dx, out dy);
        }
        // Both contacts use the same 0..1 fraction of the current simulation
        // step. A moving target in front of a wall must be hit before the wall.
        internal static ProjectileImpact FirstImpact(MapCover cover, double fromX, double fromY, double toX,
                                                     double toY, double previousTargetX,
                                                     double previousTargetY, double targetX, double targetY,
                                                     out double fraction)
        {
            bool body = Contact(fromX - previousTargetX, fromY - previousTargetY, toX - targetX,
                                toY - targetY, 0, 0, out fraction);
            double coverFraction;
            if (cover.Contact(fromX, fromY, toX, toY, out coverFraction) &&
                (!body || coverFraction <= fraction))
            {
                fraction = coverFraction;
                return ProjectileImpact.Cover;
            }
            return body ? ProjectileImpact.Body : ProjectileImpact.Clear;
        }
        // The original road movement uses (+/-2,+/-1) in screen coordinates.
        // Normalize that projection, so SelectedBulletSpeed remains pixels/second.
        static readonly int[] AimX = { 0, 2, 1, 2, 0, -2, -1, -2 };
        static readonly int[] AimY = { -1, -1, 0, 1, 1, 1, 0, -1 };
        internal static void AimVector(uint heading, out double dx, out double dy)
        {
            dx = AimX[heading & 7];
            dy = AimY[heading & 7];
            double length = Math.Sqrt(dx * dx + dy * dy);
            dx /= length;
            dy /= length;
        }
        // Ground-plane spread is projected with the same 2:1 map as the centre ray.
        internal static void PelletVector(uint heading, int pellet, out double dx, out double dy)
        {
            // Even pellet counts straddle the centre: six pellets use
            // -10, -6, -2, +2, +6, +10 degrees before the isometric projection.
            double offset =
                (pellet / (double)(GameRules.ShotgunPellets - 1) - .5) * GameRules.ShotgunSpreadDegrees;
            double angle = Math.Atan2(AimY[heading & 7], AimX[heading & 7] / 2.0) + offset * Math.PI / 180;
            dx = 2 * Math.Cos(angle);
            dy = Math.Sin(angle);
            double length = Math.Sqrt(dx * dx + dy * dy);
            dx /= length;
            dy /= length;
        }

        static bool IntersectSlab(double origin, double delta, double low, double high, ref double begin,
                                  ref double end)
        {
            if (Math.Abs(delta) < 0.000001)
                return origin >= low && origin <= high;
            double a = (low - origin) / delta, b = (high - origin) / delta;
            if (a > b)
            {
                double swap = a;
                a = b;
                b = swap;
            }
            begin = Math.Max(begin, a);
            end = Math.Min(end, b);
            return begin <= end;
        }

        static int Outcode(double x, double y)
        {
            return (x < 0 ? 1 : x > 799 ? 2 : 0) | (y < 0 ? 4 : y > 499 ? 8 : 0);
        }

        internal static bool ClipToViewport(ref double x0, ref double y0, ref double x1, ref double y1)
        {
            for (int i = 0; i < 8; i++)
            {
                int a = Outcode(x0, y0), b = Outcode(x1, y1);
                if ((a | b) == 0)
                    return true;
                if ((a & b) != 0)
                    return false;
                int q = a != 0 ? a : b;
                double x = 0, y = 0;
                if ((q & 8) != 0)
                {
                    y = 499;
                    x = x0 + (x1 - x0) * (499 - y0) / (y1 - y0);
                }
                else if ((q & 4) != 0)
                {
                    y = 0;
                    x = x0 + (x1 - x0) * (-y0) / (y1 - y0);
                }
                else if ((q & 2) != 0)
                {
                    x = 799;
                    y = y0 + (y1 - y0) * (799 - x0) / (x1 - x0);
                }
                else
                {
                    x = 0;
                    y = y0 + (y1 - y0) * (-x0) / (x1 - x0);
                }
                if (a != 0)
                {
                    x0 = x;
                    y0 = y;
                }
                else
                {
                    x1 = x;
                    y1 = y;
                }
            }
            return false;
        }

        // Intersect a segment against the target's body box. Both offline and LAN
        // simulation subtract the target's movement before calling this method.
        internal static bool Contact(double x0, double y0, double x1, double y1, double targetX,
                                     double targetY, out double fraction)
        {
            double begin = 0, end = 1;
            bool hit = IntersectSlab(x0 - targetX, x1 - x0, 18, 54, ref begin, ref end) &&
                       IntersectSlab(y0 - targetY, y1 - y0, 14, 79, ref begin, ref end);
            fraction = begin;
            return hit;
        }

        internal static bool SweptHit(double x0, double y0, double x1, double y1, double beforeX,
                                      double beforeY, double targetX, double targetY)
        {
            double contact;
            return Contact(x0 - beforeX, y0 - beforeY, x1 - targetX, y1 - targetY, 0, 0, out contact);
        }
    }
}
