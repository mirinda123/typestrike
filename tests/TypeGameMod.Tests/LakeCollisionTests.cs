using System;
using TypeGameMod;

internal static class LakeCollisionTests
{
    static int checks;
    static void Check(bool value, string reason)
    {
        checks++;
        if (!value)
            throw new Exception("Lake: " + reason);
    }

    internal static void Run()
    {
        foreach (int[] point in new[] { new[] { 1100, 780 }, new[] { 1200, 700 }, new[] { 1050, 850 } })
        {
            Check(!MovementMap.City.CanStand(point[0] - MovementMap.FootX, point[1] - MovementMap.FootY),
                  "water blocks character feet");
            Check(!MapCover.City.Solid(point[0], point[1]), "water leaves the bullet mask clear");
        }
        float x, y;
        Check(MovementMap.City.CanStand(980, 615), "north waterfront pavement remains walkable");
        MovementMap.City.Move(980, 615, 980, 750, out x, out y);
        Check(y > 615 && y < 660 && MovementMap.City.CanStand(x, y),
              "walking into water stops at the north shore");
        MovementMap.City.Move(x, y, x - 80, y + 80, out x, out y);
        Check(x < 960 && MovementMap.City.CanStand(x, y), "diagonal input can slide along the shore");
        Check(MovementMap.City.CanStand(1100 - MovementMap.FootX, 650 - MovementMap.FootY) &&
                  MovementMap.City.CanStand(950 - MovementMap.FootX, 1025 - MovementMap.FootY),
              "north and south waterfront paths remain open");

        var skill = new RoleSkill(1);
        skill.Energy = 2;
        DuelPose origin = new DuelPose { X = 980, Y = 615, Heading = 4 }, target;
        Check(skill.Use(origin, 0, MovementMap.City, out target) && target.Y < 660 &&
                  MovementMap.City.CanStand(target.X, target.Y) && skill.Energy == 1,
              "blink ends before the water and spends one point for actual movement");
        double contact;
        Check(!MapCover.City.Contact(1000, 800, 1250, 800, out contact),
              "a swept bullet crosses the open water without hitting the movement-only mask");

        foreach (int role in new[] { 0, 1 })
            foreach (bool shotgun in new[] { false, true })
            {
                var match = new OfflineMatch();
                match.Reset(1180, 760);
                match.Projectiles.Add(new OfflineMatch.Projectile(1000, 800, 1, 0, shotgun));
                match.Advance(role, 1180, 760, 2400, .1, .1);
                Check(match.Health.Hearts[role] == 2 && match.Projectiles.Count == 0,
                      "AK and shotgun pellets can still hit a target across water for both roles");
            }

        foreach (int hostRole in new[] { 0, 1 })
        {
            var pair = new EnergyTestPair(hostRole, MovementMap.City);
            DuelPose at = pair.Player(1).Poses[1];
            at.X = origin.X;
            at.Y = origin.Y;
            at.Heading = origin.Heading;
            pair.Host.Poses[1] = pair.Client.Poses[1] = at;
            pair.Step();
            pair.Charge(1, 1);
            pair.Player(1).SkillKeys(false, true);
            pair.Step();
            pair.Step();
            Check(pair.Host.Skills[1].BlinkY < 660 &&
                      Math.Abs(pair.Host.Skills[1].BlinkY - pair.Client.Skills[1].BlinkY) < .001 &&
                      pair.Host.Skills[1].Energy == 0 && pair.Client.Skills[1].Energy == 0,
                  "both LAN host roles agree on the shore-limited blink");
        }

        var peer = TestDuel.Create(false, 1, 450);
        byte[] oldPacket = Convert.FromBase64String(peer.Packet().Substring(DuelProtocol.Prefix.Length));
        oldPacket[4] = 12;
        oldPacket[5] = 0;
        var host = TestDuel.Create(true, 0, 450);
        Check(!host.Receive(DuelProtocol.Prefix + Convert.ToBase64String(oldPacket), 2, 0),
              "v12 peers without lake movement rules are refused");
        Console.WriteLine("PASS " + checks +
                          " lake collision: shore movement/blink, clear water ballistics and LAN skills");
    }
}
