using System;
using TypeGameMod;

internal static class ShotgunBalanceTests
{
    static int checks;
    static void Check(bool value, string reason)
    {
        checks++;
        if (!value)
            throw new Exception(reason);
    }
    static void Spread()
    {
        double[] centres = { -90, -45, 0, 45, 90, 135, 180, -135 };
        double[] offsets = { -10, -6, -2, 2, 6, 10 };
        Check(GameRules.ShotgunPellets == 6 && GameRules.ShotgunRange == 260 &&
                  GameRules.ShotgunSpreadDegrees == 20 && WeaponAmmo.ShotgunInterval == .7,
              "approved six-pellet, twenty-degree, 260px profile retains 700ms cadence");
        for (uint heading = 0; heading < 8; heading++)
            for (int pellet = 0; pellet < 6; pellet++)
            {
                double dx, dy;
                Ballistics.PelletVector(heading, pellet, out dx, out dy);
                double angle = Math.Atan2(dy, dx / 2) - centres[heading] * Math.PI / 180;
                double degrees = Math.Atan2(Math.Sin(angle), Math.Cos(angle)) * 180 / Math.PI;
                Check(Math.Abs(degrees - offsets[pellet]) < .000001,
                      "six rays straddle the centre symmetrically after undoing isometric projection");
                Check(Math.Abs(dx * dx + dy * dy - 1) < .000001,
                      "all headings retain the selected pixel speed");
            }
    }
    static void Offline()
    {
        foreach (int speed in new[] { 100, 450, 2400 })
            for (uint heading = 0; heading < 8; heading++)
            {
                var match = new OfflineMatch(TestMap.Empty);
                match.Reset(1400, 1100);
                match.Select(2, 0);
                match.Ammo.Supply(2, 0);
                Check(match.Fire(heading, 600, 500, 0) && match.Projectiles.Count == 6 &&
                          match.PelletsFired == 6 && match.Ammo.Rounds[2] == 7,
                      "offline shotgun consumes one shell and spawns exactly six pellets");
                double now = 0;
                while (match.Projectiles.Count != 0)
                {
                    now += .01;
                    match.Advance(1, 1400, 1100, speed, now, .01);
                    foreach (var shot in match.Projectiles)
                        Check(shot.Range == 260 && shot.Distance < 260,
                              "offline pellets never survive beyond the new range");
                    Check(now < 3, "offline shotgun has no lingering old-range projectiles");
                }
                Check(Math.Abs(now - 260.0 / speed) <= .011,
                      "offline pellets expire at distance rather than an arbitrary lifetime");
            }
    }
    sealed class Pair
    {
        internal readonly DuelSession Host, Client;
        internal readonly DuelPose[] Poses = new DuelPose[2];
        internal readonly int Speed;
        internal double Now;
        internal Pair(int hostRole, int owner, int heading, int speed)
        {
            Speed = speed;
            Host = TestDuel.Create(true, hostRole, speed, TestMap.Empty);
            Client = TestDuel.Create(false, 1 - hostRole, speed, TestMap.Empty);
            Poses[owner] = new DuelPose { X = 600, Y = 500, Heading = (byte)heading, Weapon = 2 };
            Poses[1 - owner] = new DuelPose { X = 1400, Y = 1100, Heading = 0 };
            for (int i = 0; i < 20; i++)
                Step(.01);
            TestLoadout.Fill(Host, Now);
            Client.Receive(Host.Packet(), 1, Now);
        }
        internal void Step(double dt, int fireOwner = -1)
        {
            Now += dt;
            Client.Observe(3, Poses[Client.Role], Speed, fireOwner == Client.Role, false, Now, dt);
            Check(Host.Receive(Client.Packet(), 2, Now), "client balance packet accepted");
            Host.Observe(3, Poses[Host.Role], Speed, fireOwner == Host.Role, false, Now, dt);
            Check(Client.Receive(Host.Packet(), 1, Now), "host balance packet accepted");
        }
    }
    static void Network()
    {
        foreach (int hostRole in new[] { 0, 1 })
            foreach (int owner in new[] { 0, 1 })
                foreach (int speed in new[] { 100, 450, 2400 })
                    for (int heading = 0; heading < 8; heading++)
                    {
                        var pair = new Pair(hostRole, owner, heading, speed);
                        pair.Step(0, owner);
                        Check(pair.Host.Shots.Count == 6 && pair.Client.Shots.Count == 6 &&
                                  pair.Host.PelletsFired == 6 && pair.Host.Armories[owner].Rounds[2] == 7,
                              "both host roles and shooters spawn six authoritative pellets");
                        Check(pair.Client.PelletsFired == pair.Host.PelletsFired,
                              "client statistics reconstruct six pellets per shotgun shell");
                        double start = pair.Now;
                        while (pair.Host.Shots.Count != 0)
                        {
                            pair.Step(.01);
                            foreach (var shot in pair.Host.Shots)
                                Check(shot.Distance < 260.001,
                                      "LAN range does not retain the old 360px cutoff");
                            Check(pair.Now - start < 3, "LAN projectiles cannot linger at old range");
                        }
                        Check(pair.Client.Shots.Count == 0 &&
                                  Math.Abs(pair.Now - start - 260.0 / speed) <= .011,
                              "both peers agree on expiry at 260px");
                    }
        foreach (int existing in new[] { 58, 59 })
        {
            var pair = new Pair(0, 1, 0, 450);
            for (int i = 0; i < existing; i++)
                pair.Host.Shots.Add(new DuelShot { X = 20, Y = 20, DX = 1, DY = 0, Weapon = 1 });
            pair.Client.Receive(pair.Host.Packet(), 1, pair.Now);
            pair.Step(0, 1);
            Check(pair.Host.Shots.Count == (existing == 58 ? 64 : 59) &&
                      pair.Host.Armories[1].Rounds[2] == (existing == 58 ? 7 : 8),
                  "projectile capacity checks reserve room for six pellets, not seven");
        }
    }
    internal static void Run()
    {
        Spread();
        Offline();
        Network();
        Console.WriteLine(
            "PASS " + checks +
            " shotgun balance: six symmetric pellets, 20-degree fan, 260px offline/LAN range and capacity");
    }
}
