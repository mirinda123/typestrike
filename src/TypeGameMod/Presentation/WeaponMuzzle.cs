namespace TypeGameMod
{
    // Heading is always 0..7; frame is always the current rendered pose.
    // Native four-phase movement indexes never enter this interface.
    internal static class WeaponMuzzle
    {
        internal static void Offset(int role, int weapon, int heading, bool moving, int frame, out double x,
                                    out double y)
        {
            if (role == 0)
            {
                if (moving && weapon == GameRules.Shotgun)
                    PoliceShotgunRun.Muzzle(heading, frame, out x, out y);
                else if (moving)
                    PoliceAkRun.Muzzle(heading, frame, out x, out y);
                else if (weapon == GameRules.Shotgun)
                    PoliceShotgunIdle.Muzzle(heading, frame, out x, out y);
                else
                    PoliceAkIdle.Muzzle(heading, frame, out x, out y);
            }
            else
            {
                if (moving && weapon == GameRules.Shotgun)
                    ThiefShotgunRun.Muzzle(heading, frame, out x, out y);
                else if (moving)
                    ThiefAkRun.Muzzle(heading, frame, out x, out y);
                else if (weapon == GameRules.Shotgun)
                    ThiefShotgunPose.Muzzle(heading, frame, out x, out y);
                else
                    ThiefAkPose.Muzzle(heading, frame, out x, out y);
            }
        }
    }
}
