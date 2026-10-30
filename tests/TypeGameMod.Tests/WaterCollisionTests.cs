using System;
using System.Globalization;
using System.IO;
using TypeGameMod;

internal static class WaterCollisionTests
{
    static int checks;

    static void Check(bool condition, string reason)
    {
        checks++;
        if (!condition)
            throw new Exception("Water collision: " + reason);
    }

    // Original background coordinates: approach feet, water sample, clear bullet segment.
    static readonly int[][] Pools = { new[] { 90, 1000, 100, 1040, 15, 1060, 220, 1060 },
                                      new[] { 390, 965, 380, 990, 335, 1007, 430, 1007 },
                                      new[] { 550, 880, 550, 925, 470, 941, 608, 941 },
                                      new[] { 780, 754, 780, 784, 735, 805, 840, 755 },
                                      new[] { 1180, 545, 1180, 585, 1088, 625, 1240, 557 } };

    static DuelPose Approach(int[] pool)
    {
        return new DuelPose { X = pool[0] - MovementMap.FootX, Y = pool[1] - MovementMap.FootY, Heading = 4 };
    }

    static void Movement(int[] pool)
    {
        var map = MovementMap.City;
        Check(!map.CanStand(pool[2] - MovementMap.FootX, pool[3] - MovementMap.FootY),
              "each fountain and ornamental pool blocks character feet");
        Check(!MapCover.City.Solid(pool[2], pool[3]), "pool water is absent from the bullet mask");

        DuelPose origin = Approach(pool);
        Check(map.CanStand(origin.X, origin.Y), "the approach pavement remains walkable");
        float x, y;
        float destinationY = pool[3] - MovementMap.FootY;
        map.Move(origin.X, origin.Y, origin.X, destinationY, out x, out y);
        Check(y > origin.Y && y < destinationY && map.CanStand(x, y),
              "continuous movement stops at each pool edge");
        float stoppedY = y;
        map.Move(x, y, origin.X, destinationY, out x, out y);
        Check(Math.Abs(y - stoppedY) < .001, "holding movement cannot advance into the water");

        var skill = new RoleSkill(1);
        skill.Energy = 2;
        DuelPose target;
        Check(skill.Use(origin, 0, map, out target) && target.Y > origin.Y && target.Y < destinationY &&
                  map.CanStand(target.X, target.Y) && skill.Energy == 1,
              "blink stops before each pool rather than crossing it");
        Check(!skill.Use(target, 1, map, out origin) && skill.Energy == 1,
              "another blink into the same edge spends no additional energy");
    }

    static void Bullets(int[] pool)
    {
        double contact;
        Check(!MapCover.City.Contact(pool[4], pool[5], pool[6], pool[7], out contact),
              "the entire swept bullet segment crosses each pool");
        double dx = pool[6] - pool[4], dy = pool[7] - pool[5];
        double length = Math.Sqrt(dx * dx + dy * dy);
        foreach (int role in new[] { 0, 1 })
            foreach (bool shotgun in new[] { false, true })
            {
                var match = new OfflineMatch();
                double targetX = pool[6] - 36, targetY = pool[7] - 40;
                match.Reset(targetX, targetY);
                match.Projectiles.Add(
                    new OfflineMatch.Projectile(pool[4], pool[5], dx / length, dy / length, shotgun));
                match.Advance(role, targetX, targetY, 2400, .1, .1);
                Check(match.Health.Hearts[role] == 2 && match.Projectiles.Count == 0,
                      "AK and shotgun projectiles still hit across the pool for either target role");
            }
    }

    static void Network(int[] pool)
    {
        foreach (int hostRole in new[] { 0, 1 })
        {
            var pair = new EnergyTestPair(hostRole, MovementMap.City);
            DuelPose origin = Approach(pool);
            pair.Host.Poses[1] = pair.Client.Poses[1] = origin;
            pair.Step();
            pair.Charge(1, 1);
            pair.Player(1).SkillKeys(false, true);
            pair.Step();
            pair.Step();
            RoleSkill host = pair.Host.Skills[1], client = pair.Client.Skills[1];
            Check(host.BlinkSerial == 1 && client.BlinkSerial == 1 && host.Energy == 0 &&
                      client.Energy == 0 && Math.Abs(host.BlinkY - client.BlinkY) < .001 &&
                      host.BlinkY < pool[3] - MovementMap.FootY &&
                      MovementMap.City.CanStand(host.BlinkX, host.BlinkY),
                  "both host roles agree on the clipped blink and its energy cost at every pool");
        }
    }

    internal static void ExportMovementCases(TextWriter writer)
    {
        foreach (int[] pool in Pools)
        {
            DuelPose origin = Approach(pool);
            foreach (int[] delta in new[] { new[] { 0, 50 }, new[] { 0, 150 }, new[] { -60, 70 },
                                            new[] { 60, 70 }, new[] { 120, 0 } })
            {
                float destinationX = origin.X + delta[0], destinationY = origin.Y + delta[1];
                float x, y;
                MovementMap.City.Move(origin.X, origin.Y, destinationX, destinationY, out x, out y);
                writer.WriteLine(String.Format(CultureInfo.InvariantCulture,
                                               "city,{0:R},{1:R},{2:R},{3:R},{4:R},{5:R}", origin.X, origin.Y,
                                               destinationX, destinationY, x, y));
            }
        }
    }

    internal static void Run()
    {
        foreach (int[] pool in Pools)
        {
            Movement(pool);
            Bullets(pool);
            Network(pool);
        }
        foreach (int[] feet in new[] { new[] { 160, 975 }, new[] { 260, 1065 }, new[] { 180, 1125 },
                                       new[] { 422, 1003 }, new[] { 457, 978 }, new[] { 480, 975 },
                                       new[] { 810, 735 }, new[] { 1180, 620 } })
            Check(MovementMap.City.CanStand(feet[0] - MovementMap.FootX, feet[1] - MovementMap.FootY),
                  "surrounding paths and stone passages remain open");

        var peer = TestDuel.Create(false, 1, 450);
        byte[] legacy = Convert.FromBase64String(peer.Packet().Substring(DuelProtocol.Prefix.Length));
        legacy[4] = 13;
        legacy[5] = 0;
        var hostSession = TestDuel.Create(true, 0, 450);
        Check(!hostSession.Receive(DuelProtocol.Prefix + Convert.ToBase64String(legacy), 2, 0),
              "v13 peers without the new pool collision rules are refused");
        Console.WriteLine(
            "PASS " + checks +
            " water collision: five pools, shore movement/blink, clear bullets, paths and LAN skills");
    }
}
