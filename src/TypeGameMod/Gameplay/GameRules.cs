namespace TypeGameMod
{
    internal static class GameRules
    {
        // Weapon numbers index the original game's three inventory slots.
        internal const int Unarmed = 0;
        internal const int Ak47 = 1;
        internal const int Shotgun = 2;
        internal const int WeaponSlots = 3;
        internal const int WorldWidth = 1600;
        internal const int WorldHeight = 1200;
        internal const double MaximumStepSeconds = .05;
        internal const double AkProjectileLifetime = 3;
        internal const double ShotgunProjectileLifetime = 4;
        internal const int DefaultBulletSpeed = 450;
        internal const int BulletSpeedStep = 50;
        internal const int MinimumBulletSpeed = 100;
        internal const int MaximumBulletSpeed = 2400;
        internal const int MaxProjectiles = 64;
        internal const int ShotgunPellets = 6;
        internal const double ShotgunSpreadDegrees = 20;
        internal const double ShotgunRange = 260;
        internal static bool IsGun(int weapon)
        {
            return weapon == Ak47 || weapon == Shotgun;
        }

        internal static int PelletsPerShot(int weapon)
        {
            return weapon == Shotgun ? ShotgunPellets : 1;
        }

        internal static double ProjectileRange(int weapon)
        {
            return weapon == Shotgun ? ShotgunRange : double.PositiveInfinity;
        }

        internal static double ProjectileLifetime(int weapon)
        {
            return weapon == Shotgun ? ShotgunProjectileLifetime : AkProjectileLifetime;
        }

        internal static int ClampBulletSpeed(int speed)
        {
            return System.Math.Max(
                MinimumBulletSpeed,
                System.Math.Min(MaximumBulletSpeed, speed / BulletSpeedStep * BulletSpeedStep));
        }
    }
}
