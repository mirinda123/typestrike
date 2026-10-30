using System;
using System.Globalization;
using System.IO;
using TypeGameMod;

internal static class WorldCollisionTests
{
    static int checks;
    static void Check(bool ok, string reason)
    {
        checks++;
        if (!ok)
            throw new Exception("World collision: " + reason);
    }

    internal static void Run()
    {
        Check(CaveLayout.MapMove >= CaveLayout.KeyboardQueue + 1024 &&
                  CaveLayout.MapMove + 1024 <= CaveLayout.Snapshot &&
                  AssetLayout.MovementMap + MapCover.Width * MapCover.Height / 8 <=
                      AssetLayout.AllocationBytes,
              "native helper and mask fit separate reserved memory without moving existing art");
        foreach (int role in new[] { 0, 1 })
        {
            DuelPose pose = DuelPose.Spawn(role);
            Check(MovementMap.City.CanStand(pose.X, pose.Y), "both original spawns are unobstructed");
        }
        Check(!MovementMap.City.CanStand(992 - MovementMap.FootX, 371 - MovementMap.FootY),
              "the entire palm canopy blocks feet too");
        Check(!MovementMap.City.CanStand(1160 - MovementMap.FootX, 380 - MovementMap.FootY),
              "building body blocks feet");
        Check(!MovementMap.City.CanStand(1310 - MovementMap.FootX, 130 - MovementMap.FootY),
              "vehicle blocks feet");

        var wall = new MovementMap(TestMap.Rectangle(800, 0, 800, 1199));
        float x, y;
        wall.Move(700, 500, 900, 600, out x, out y);
        Check(x < 760 && x > 759 && y == 600 && wall.CanStand(x, y),
              "a one-pixel wall stops the horizontal axis while allowing vertical sliding");
        wall.Move(900, 500, 700, 400, out x, out y);
        Check(x >= 761 && x < 762 && y == 400 && wall.CanStand(x, y),
              "reverse travel stops at the other side of a thin wall");
        TestMap.Movement.Move(500, 500, 512.75f, 493.25f, out x, out y);
        Check(x == 512.75f && y == 493.25f, "clear motion preserves exact native speed");

        var thief = new RoleSkill(1);
        thief.Energy = 2;
        DuelPose origin = new DuelPose { X = 700, Y = 500, Heading = 2 }, target;
        Check(thief.Use(origin, 0, wall, out target) && target.X < 760 && target.X > 759 && thief.Energy == 1,
              "blink clips the whole path and costs one point for actual displacement");
        origin.X = target.X;
        Check(!thief.Use(origin, 1, wall, out target) && thief.Energy == 1 && thief.BlinkSerial == 1,
              "a blink directly against a wall spends no energy or extra charge");
        origin = new DuelPose { X = 324, Y = 880, Heading = 0 };
        Check(thief.Use(origin, 2, MovementMap.City, out target) &&
                  MovementMap.City.CanStand(target.X, target.Y),
              "production blink ends on walkable ground");

        var random = new Random(1800);
        for (int i = 0; i < 6000; i++)
        {
            float fromX = 24 + (float)random.NextDouble() * 1500;
            float fromY = 32 + (float)random.NextDouble() * 1050;
            if (!MovementMap.City.CanStand(fromX, fromY))
                continue;
            float toX = fromX + (float)(random.NextDouble() * 120 - 60);
            float toY = fromY + (float)(random.NextDouble() * 60 - 30);
            MovementMap.City.Move(fromX, fromY, toX, toY, out x, out y);
            Check(MovementMap.City.CanStand(x, y),
                  "random swept moves never end inside the actual city mask");
        }

        foreach (int hostRole in new[] { 0, 1 })
        {
            var pair = new EnergyTestPair(hostRole, wall);
            DuelSession player = pair.Player(1);
            DuelPose at = player.Poses[1];
            at.X = 700;
            at.Y = 500;
            at.Heading = 2;
            pair.Host.Poses[1] = pair.Client.Poses[1] = at;
            pair.Step();
            pair.Charge(1, 2);
            player.SkillKeys(false, true);
            pair.Step();
            pair.Step();
            Check(pair.Host.Skills[1].BlinkSerial == 1 && pair.Client.Skills[1].BlinkSerial == 1 &&
                      pair.Host.Skills[1].BlinkX < 760 &&
                      Math.Abs(pair.Host.Skills[1].BlinkX - pair.Client.Skills[1].BlinkX) < .001 &&
                      pair.Host.Skills[1].Energy == 1 && pair.Client.Skills[1].Energy == 1,
                  "both host roles agree on clipped blink and spent energy");
        }
        Console.WriteLine(
            "PASS " + checks +
            " world collision: foot clearance, thin walls, sliding, clipped blink, canopy and LAN skills");
    }

    internal static void Export(string folder)
    {
        Directory.CreateDirectory(folder);
        var wall = TestMap.Rectangle(800, 0, 800, 1199);
        File.WriteAllBytes(Path.Combine(folder, "city.bin"), MovementMap.City.Cover.CopyPixels());
        File.WriteAllBytes(Path.Combine(folder, "wall.bin"), wall.CopyPixels());
        File.WriteAllBytes(Path.Combine(folder, "clear.bin"), TestMap.Empty.CopyPixels());
        using (var writer = new StreamWriter(Path.Combine(folder, "cases.csv")))
        {
            writer.WriteLine("mask,from_x,from_y,to_x,to_y,result_x,result_y");
            FlowerbedCollisionTests.ExportMovementCases(writer);
            WaterCollisionTests.ExportMovementCases(writer);
            var random = new Random(1818);
            foreach (string name in new[] { "city", "wall", "clear" })
            {
                MovementMap map = name == "city"   ? MovementMap.City
                                  : name == "wall" ? new MovementMap(wall)
                                                   : TestMap.Movement;
                for (int i = 0; i < 800; i++)
                {
                    float fromX = name == "wall" ? 720 + (float)random.NextDouble() * 80
                                                 : 24 + (float)random.NextDouble() * 1500;
                    float fromY = 32 + (float)random.NextDouble() * 1050;
                    if (!map.CanStand(fromX, fromY))
                        continue;
                    float toX = fromX + (float)(random.NextDouble() * 80 - 40);
                    float toY = fromY + (float)(random.NextDouble() * 40 - 20);
                    float x, y;
                    map.Move(fromX, fromY, toX, toY, out x, out y);
                    writer.WriteLine(String.Format(CultureInfo.InvariantCulture,
                                                   "{0},{1:R},{2:R},{3:R},{4:R},{5:R},{6:R}", name, fromX,
                                                   fromY, toX, toY, x, y));
                }
            }
        }
    }
}
