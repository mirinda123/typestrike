using TypeGameMod;

internal static class TestLoadout
{
    // Combat fixtures deliberately start armed. Spawn/resupply tests exercise the
    // real empty start, energy-priced reserve supply and timed reload instead.
    internal static void Fill(DuelSession host, double now)
    {
        foreach (WeaponAmmo ammo in host.Armories)
        {
            ammo.Rounds[1] = WeaponAmmo.Capacity(1);
            ammo.Rounds[2] = WeaponAmmo.Capacity(2);
            ammo.Reserve[1] = 60;
            ammo.Reserve[2] = 16;
        }
    }
}
