using System;
using System.Globalization;
using System.IO;
using TypeGameMod;

internal static class FlowerbedCollisionTests
{
    static int checks;

    // Feet coordinates at four sections, on either side of the narrow planter.
    static readonly int[][] Sections = { new[] { 500, 701, 779 }, new[] { 650, 629, 709 },
                                         new[] { 800, 559, 638 }, new[] { 950, 488, 566 } };

    static void Check(bool condition, string reason)
    {
        checks++;
        if (!condition)
            throw new Exception("Flowerbed: " + reason);
    }

    static DuelPose OnPath(int[] section, bool north)
    {
        return new DuelPose { X = section[0] - MovementMap.FootX,
                              Y = section[north ? 1 : 2] - MovementMap.FootY,
                              Heading = north ? (byte)4 : (byte)0 };
    }

    static void Crossing(int[] section, bool north)
    {
        DuelPose origin = OnPath(section, north);
        float destinationY = section[north ? 2 : 1] - MovementMap.FootY;
        float x, y;
        Check(MovementMap.City.CanStand(origin.X, origin.Y), "pavement on both sides stays walkable");
        MovementMap.City.Move(origin.X, origin.Y, origin.X, destinationY, out x, out y);
        Check(Math.Abs(y - origin.Y) > 1 && Math.Abs(y - destinationY) > 1 && MovementMap.City.CanStand(x, y),
              "walking stops at each side of the planter");
        float stoppedY = y;
        MovementMap.City.Move(x, y, origin.X, destinationY, out x, out y);
        Check(Math.Abs(y - stoppedY) < .001, "holding the direction cannot push through");

        var skill = new RoleSkill(1);
        skill.Energy = 2;
        DuelPose target;
        Check(skill.Use(origin, 0, MovementMap.City, out target) &&
                  MovementMap.City.CanStand(target.X, target.Y) && skill.Energy == 1 &&
                  (north ? target.Y < destinationY : target.Y > destinationY),
              "blink stops at the narrow flowerbed instead of jumping across it");
        Check(!skill.Use(target, 1, MovementMap.City, out origin) && skill.Energy == 1,
              "blinking again into the edge does not spend another charge");

        double contact;
        Check(!MapCover.City.Contact(section[0], section[1], section[0], section[2], out contact),
              "the low planter adds no new bullet obstruction");
    }

    static void Network(int[] section, bool north)
    {
        foreach (int hostRole in new[] { 0, 1 })
        {
            var pair = new EnergyTestPair(hostRole, MovementMap.City);
            DuelPose origin = OnPath(section, north);
            pair.Host.Poses[1] = pair.Client.Poses[1] = origin;
            pair.Step();
            pair.Charge(1, 1);
            pair.Player(1).SkillKeys(false, true);
            pair.Step();
            pair.Step();
            RoleSkill host = pair.Host.Skills[1], client = pair.Client.Skills[1];
            Check(host.BlinkSerial == 1 && client.BlinkSerial == 1 && host.Energy == 0 &&
                      client.Energy == 0 && Math.Abs(host.BlinkY - client.BlinkY) < .001 &&
                      MovementMap.City.CanStand(host.BlinkX, host.BlinkY),
                  "both host roles agree on the flowerbed-limited blink");
        }
    }

    internal static void Run()
    {
        foreach (int[] feet in new[] { new[] { 360, 805 }, new[] { 500, 739 }, new[] { 650, 668 },
                                       new[] { 800, 596 }, new[] { 950, 525 }, new[] { 1025, 493 } })
        {
            Check(!MovementMap.City.CanStand(feet[0] - MovementMap.FootX, feet[1] - MovementMap.FootY),
                  "the full length and both end caps block feet");
            Check(!MapCover.City.Solid(feet[0], feet[1]), "grass and shrubs keep their existing bullet rule");
        }
        foreach (int[] section in Sections)
            foreach (bool north in new[] { true, false })
            {
                Crossing(section, north);
                Network(section, north);
            }

        var peer = TestDuel.Create(false, 1, 450);
        byte[] legacy = Convert.FromBase64String(peer.Packet().Substring(DuelProtocol.Prefix.Length));
        legacy[4] = 14;
        legacy[5] = 0;
        var hostSession = TestDuel.Create(true, 0, 450);
        Check(!hostSession.Receive(DuelProtocol.Prefix + Convert.ToBase64String(legacy), 2, 0),
              "peers without planter movement collision are refused");
        Console.WriteLine(
            "PASS " + checks +
            " narrow flowerbed: full length, both sides, movement/blink, pavement and LAN skills");
    }

    internal static void ExportMovementCases(TextWriter writer)
    {
        foreach (int[] section in Sections)
            foreach (bool north in new[] { true, false })
            {
                DuelPose origin = OnPath(section, north);
                foreach (int horizontal in new[] { -50, 0, 50 })
                {
                    float destinationX = origin.X + horizontal;
                    float destinationY = origin.Y + (north ? 130 : -130);
                    float x, y;
                    MovementMap.City.Move(origin.X, origin.Y, destinationX, destinationY, out x, out y);
                    writer.WriteLine(String.Format(CultureInfo.InvariantCulture,
                                                   "city,{0:R},{1:R},{2:R},{3:R},{4:R},{5:R}", origin.X,
                                                   origin.Y, destinationX, destinationY, x, y));
                }
            }
    }
}
