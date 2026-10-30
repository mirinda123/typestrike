namespace TypeGameMod
{
    // Four eight-heading blocks: AK idle/run, then shotgun idle/run.
    // Fixed CPU data is shared by the local and LAN sprite-origin bridge.
    internal static class PoliceWeaponOrigins
    {
        internal const int Bytes = 4 * 8 * 4;
        internal static int Get(int weapon, bool moving, int heading)
        {
            if (weapon == 1)
                return moving ? PoliceAkRun.DrawOffsetX(heading) : PoliceAkIdle.DrawOffsetX(heading);
            return moving ? PoliceShotgunRun.DrawOffsetX(heading) : PoliceShotgunIdle.DrawOffsetX(heading);
        }

        internal static byte[] Build()
        {
            var data = new byte[Bytes];
            for (int weapon = 1; weapon <= 2; weapon++)
                for (int moving = 0; moving < 2; moving++)
                    for (int heading = 0; heading < 8; heading++)
                    {
                        int at = ((weapon - 1) * 2 + moving) * 32 + heading * 4;
                        BinaryFields.Write(data, at, unchecked((uint)Get(weapon, moving != 0, heading)));
                    }
            return data;
        }
    }
}
