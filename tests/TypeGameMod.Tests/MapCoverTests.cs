using System;
using TypeGameMod;

internal static class MapCoverTests
{
    static int checks;

    static void Check(bool value, string reason)
    {
        checks++;
        if (!value)
            throw new Exception("Map cover: " + reason);
    }

    static void Sweep()
    {
        var wall = TestMap.Rectangle(250, 100, 250, 900);
        double contact;
        Check(wall.Contact(200, 400, 320, 400, out contact) && Math.Abs(contact - 50.0 / 120) < 1e-10,
              "fast bullet crosses a one-pixel wall at the first contact");
        Check(wall.Contact(320, 400, 200, 400, out contact) && Math.Abs(contact - 69.0 / 120) < 1e-10,
              "reverse direction enters the far edge of the same pixel");
        Check(wall.Contact(250.5, 400, 250.5, 400, out contact) && contact == 0,
              "a muzzle inside solid cover stops immediately");
        Check(!wall.Contact(200, 99.5, 320, 99.5, out contact), "passing beside the wall stays clear");
        Check(!wall.Contact(200, 400, 200, 400, out contact), "stationary clear bullet terminates");
        Check(wall.Contact(249.5, 99.5, 250.5, 100.5, out contact) && contact == .5,
              "a diagonal cannot tunnel through a pixel corner");
        Check(!wall.Contact(-20, 400, -1, 400, out contact), "outside-map segments do not index the mask");

        var pixel = TestMap.Rectangle(800, 600, 800, 600);
        for (int heading = 0; heading < 8; heading++)
            foreach (int speed in new[] { 100, 450, 2400 })
            {
                double dx, dy;
                Ballistics.AimVector((uint)heading, out dx, out dy);
                double travel = speed * .05;
                Check(pixel.Contact(800.5 - dx * travel / 2, 600.5 - dy * travel / 2, 800.5 + dx * travel / 2,
                                    600.5 + dy * travel / 2, out contact),
                      "all headings and selectable speeds see a one-pixel obstacle");
            }

        // Independent rectangle oracle; random rays exercise grid traversal
        // without duplicating the DDA implementation.
        var box = TestMap.Rectangle(700, 400, 741, 437);
        var random = new Random(1700);
        for (int i = 0; i < 10000; i++)
        {
            double x0 = 650 + random.NextDouble() * 150, y0 = 350 + random.NextDouble() * 150;
            double x1 = 650 + random.NextDouble() * 150, y1 = 350 + random.NextDouble() * 150;
            double begin = 0, end = 1;
            bool expected = Slab(x0, x1 - x0, 700, 742, ref begin, ref end) &&
                            Slab(y0, y1 - y0, 400, 438, ref begin, ref end);
            bool actual = box.Contact(x0, y0, x1, y1, out contact);
            Check(actual == expected && (!actual || Math.Abs(begin - contact) < 1e-9),
                  "random ray agrees with independent rectangle intersection");
        }
    }

    static bool Slab(double origin, double delta, double low, double high, ref double begin, ref double end)
    {
        if (delta == 0)
            return origin >= low && origin <= high;
        double a = (low - origin) / delta, b = (high - origin) / delta;
        begin = Math.Max(begin, Math.Min(a, b));
        end = Math.Min(end, Math.Max(a, b));
        return begin <= end;
    }

    static void City()
    {
        Check(MapCover.City.Solid(1160, 380), "hospital blocks");
        Check(MapCover.City.Solid(1310, 130), "parked tow truck blocks");
        Check(MapCover.City.Solid(992, 420), "narrow palm trunk blocks");
        Check(MapCover.City.Solid(992, 371), "palm canopy blocks in the revised rules");
        Check(!MapCover.City.Solid(1150, 240), "road stays open");
        Check(!MapCover.City.Solid(713, 500), "park grass stays open");
        Check(!MapCover.City.Solid(1200, 700), "river stays open");
    }

    static void Offline()
    {
        foreach (int role in new[] { 0, 1 })
            foreach (bool shotgun in new[] { false, true })
                foreach (int targetX in new[] { 210, 260 })
                {
                    var match = new OfflineMatch(TestMap.Rectangle(250, 0, 251, 1199));
                    match.Reset(targetX, 360);
                    match.Projectiles.Add(new OfflineMatch.Projectile(200, 400, 1, 0, shotgun));
                    match.Advance(role, targetX, 360, 2400, .05, .05);
                    Check(match.Projectiles.Count == 0 &&
                              match.Health.Hearts[role] == (targetX == 210 ? 2 : 3),
                          "nearest target/cover contact wins for both weapons and roles");
                }
        var city = new OfflineMatch();
        city.Reset(1140, 360);
        city.Projectiles.Add(new OfflineMatch.Projectile(1000, 400, 1, 0, false));
        city.Advance(1, 1140, 360, 2400, .1, .1);
        Check(city.Projectiles.Count == 0 && city.Health.Hearts[1] == 3,
              "production offline constructor uses the embedded city cover");

        // A narrow trunk removes individual pellets, never the whole blast.
        var fan = new OfflineMatch(TestMap.Rectangle(250, 399, 250, 401));
        fan.Reset(1400, 1100);
        fan.Projectiles.Add(new OfflineMatch.Projectile(200, 400, 1, 0, true));
        fan.Projectiles.Add(new OfflineMatch.Projectile(200, 390, 1, 0, true));
        fan.Advance(1, 1400, 1100, 2400, .05, .05);
        Check(fan.Projectiles.Count == 1 && fan.Projectiles[0].Y == 390,
              "only the shotgun pellet that touches cover is removed");

        foreach (int startX in new[] { 220, 280 })
        {
            var moving = new OfflineMatch(TestMap.Rectangle(250, 0, 250, 1199));
            moving.Reset(startX, 360);
            moving.Projectiles.Add(new OfflineMatch.Projectile(200, 400, 1, 0, false));
            moving.Advance(1, startX == 220 ? 180 : 210, 360, 2400, .05, .05);
            Check(moving.Health.Hearts[1] == (startX == 220 ? 2 : 3),
                  "moving body and stationary cover compare fractions in the same tick");
        }
    }

    sealed class Pair
    {
        internal readonly DuelSession Host, Client;
        internal readonly DuelPose[] Poses = new DuelPose[2];
        internal double Now;
        internal Pair(int hostRole, int owner, int weapon, int speed, bool blocked)
        {
            // The default constructors exercise real embedded city geometry.
            Host = TestDuel.Create(true, hostRole, speed);
            Client = TestDuel.Create(false, 1 - hostRole, speed);
            double mx, my;
            WeaponMuzzle.Offset((int)((uint)owner), weapon, 2, false, 0, out mx, out my);
            int y = blocked ? 400 : 490;
            Poses[owner] = new DuelPose { X = (float)(1000 - mx), Y = (float)(y - my), Heading = 2,
                                          Weapon = (byte)weapon };
            Poses[1 - owner] = new DuelPose { X = 1140, Y = y - 40, Heading = 6 };
            for (int i = 0; i < 20; i++)
                Step(speed);
            TestLoadout.Fill(Host, Now);
            Check(Client.Receive(Host.Packet(), 1, Now), "loadout synchronized");
        }
        internal void Step(int speed, int fireOwner = -1)
        {
            Now += .01;
            Client.Observe(3, Poses[Client.Role], speed, fireOwner == Client.Role, false, Now, .01);
            Check(Host.Receive(Client.Packet(), 2, Now), "client event accepted");
            Host.Observe(3, Poses[Host.Role], speed, fireOwner == Host.Role, false, Now, .01);
            Check(Client.Receive(Host.Packet(), 1, Now), "authoritative snapshot accepted");
        }
    }

    static void Network()
    {
        var oldPeer = TestDuel.Create(false, 1, 450);
        byte[] oldPacket = Convert.FromBase64String(oldPeer.Packet().Substring(DuelProtocol.Prefix.Length));
        oldPacket[4] = 11;
        oldPacket[5] = 0;
        var currentHost = TestDuel.Create(true, 0, 450);
        Check(!currentHost.Receive(DuelProtocol.Prefix + Convert.ToBase64String(oldPacket), 2, 0),
              "v11 peers without canopy and movement rules are refused");
        foreach (int hostRole in new[] { 0, 1 })
            foreach (int owner in new[] { 0, 1 })
                foreach (int weapon in new[] { 1, 2 })
                    foreach (int speed in new[] { 100, 450, 2400 })
                        foreach (bool blocked in new[] { false, true })
                        {
                            var pair = new Pair(hostRole, owner, weapon, speed, blocked);
                            pair.Step(speed, owner);
                            for (int i = 0; i < 240; i++)
                                pair.Step(speed);
                            int expected = blocked ? 3 : 2;
                            Check(pair.Host.ShotsFired == 1 && pair.Host.Shots.Count == 0 &&
                                      pair.Client.Shots.Count == 0 &&
                                      pair.Host.Health.Hearts[1 - owner] == expected &&
                                      pair.Client.Health.Hearts[1 - owner] == expected,
                                  "both host roles/shooters/weapons/speeds agree on blocked and clear city " +
                                      "shots");
                        }
    }

    internal static void Run()
    {
        Sweep();
        City();
        Offline();
        Network();
        Console.WriteLine(
            "PASS " + checks +
            " map cover: swept grid, city silhouettes, contact ordering, offline/LAN authority");
    }
}
