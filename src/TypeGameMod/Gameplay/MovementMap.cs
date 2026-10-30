using System;

namespace TypeGameMod
{
    // The map is expanded by a 10 x 5 pixel foot radius at asset-build time.
    // Both roles use the same planted-foot anchor in their 92 x 99 frames.
    internal sealed class MovementMap
    {
        internal const int FootX = 40, FootY = 85;
        internal static readonly MovementMap City = new MovementMap(MapCover.Load("MovementMap"));
        internal readonly MapCover Cover;
        internal MovementMap(MapCover expandedCover)
        {
            Cover = expandedCover;
        }

        internal bool CanStand(float x, float y)
        {
            int px = (int)Math.Floor(x) + FootX, py = (int)Math.Floor(y) + FootY;
            return px >= 24 + FootX && py >= 32 + FootY && px < MapCover.Width && py < MapCover.Height &&
                   !Cover.Solid(px, py);
        }
        // Subpixel steps prevent tunnelling; independent axes allow wall sliding.
        // The UI-thread x86 bridge uses this same algorithm and the same mask.
        internal void Move(float fromX, float fromY, float toX, float toY, out float x, out float y)
        {
            x = fromX;
            y = fromY;
            int steps = (int)Math.Max(Math.Abs(toX - fromX), Math.Abs(toY - fromY)) + 1;
            float dx = (toX - fromX) / steps, dy = (toY - fromY) / steps;
            bool blockedX = false, blockedY = false;
            for (int i = 0; i < steps; i++)
            {
                float candidate = x + dx;
                if (CanStand(candidate, y))
                    x = candidate;
                else
                    blockedX = true;
                candidate = y + dy;
                if (CanStand(x, candidate))
                    y = candidate;
                else
                    blockedY = true;
            }
            if (!blockedX && CanStand(toX, y))
                x = toX;
            if (!blockedY && CanStand(x, toY))
                y = toY;
        }

        internal bool Blink(DuelPose origin, ref DuelPose target)
        {
            target.X = Math.Min(target.X, MapCover.Width - FootX - 1);
            target.Y = Math.Min(target.Y, MapCover.Height - FootY - 1);
            double fraction;
            if (Cover.Contact((double)origin.X + FootX, (double)origin.Y + FootY, (double)target.X + FootX,
                              (double)target.Y + FootY, out fraction))
            {
                double dx = target.X - origin.X, dy = target.Y - origin.Y;
                double distance = Math.Sqrt(dx * dx + dy * dy);
                fraction = Math.Max(0, fraction - .25 / Math.Max(1, distance));
                target.X = (float)(origin.X + (target.X - origin.X) * fraction);
                target.Y = (float)(origin.Y + (target.Y - origin.Y) * fraction);
            }
            return CanStand(target.X, target.Y) &&
                   (Math.Abs(target.X - origin.X) >= 1 || Math.Abs(target.Y - origin.Y) >= 1);
        }
    }
}
