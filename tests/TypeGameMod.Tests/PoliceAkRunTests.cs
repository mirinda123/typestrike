using System;
using System.Collections.Generic;
using TypeGameMod;

internal static class PoliceAkRunTests
{
    internal static void Run()
    {
        for (int heading = 0; heading < 8; heading++)
        {
            byte[][] banks = { IdleAnimationTests.Source("PoliceAkRun" + heading + "_0"),
                               IdleAnimationTests.Source("PoliceAkRun" + heading + "_1") };
            var poses = new HashSet<string>();
            var muzzles = new HashSet<string>();
            for (int frame = 0; frame < PoliceAkRun.Frames; frame++)
            {
                int bank = frame / 18, cell = frame % 18;
                Check(banks[bank].Length == GameIdentity.SpriteBytes, "run uses the existing DIB bank size");
                var pixels = new byte[92 * 99];
                int visible = 0;
                for (int y = 0; y < 99; y++)
                    for (int x = 0; x < 92; x++)
                    {
                        byte value = banks[bank][IdleAnimation.At(cell, x, y)];
                        pixels[y * 92 + x] = value;
                        if (value != 0)
                            visible++;
                    }
                Check(visible > 500, "every running pose contains the full character");
                poses.Add(Convert.ToBase64String(pixels));
                double mx, my, bx, by;
                PoliceAkRun.Muzzle(heading, frame, out mx, out my);
                WeaponMuzzle.Offset(0, GameRules.Ak47, heading, true, frame, out bx, out by);
                Check(mx - PoliceAkRun.DrawOffsetX(heading) > 0 &&
                          mx - PoliceAkRun.DrawOffsetX(heading) < 92 && my > 0 && my < 99 && bx == mx &&
                          by == my,
                      "running bullets use the 24-pose gun tip rather than the network's four phases");
                muzzles.Add(mx + "/" + my);
                for (int hostRole = 0; hostRole < 2; hostRole++)
                    CheckNetworkMuzzle(heading, frame, hostRole, mx, my);
            }
            Check(poses.Count == 24 && muzzles.Count >= 12,
                  "all poses survive and the muzzle follows the moving upper body");
        }
        var clock = new RunAnimation();
        var phases = new HashSet<int>();
        for (int i = 0; i < 160; i++)
            phases.Add(clock.Update(true, .008));
        Check(phases.Count == 24 && clock.Update(false, .01) == 0,
              "running clock plays all poses and resets at stop");
        Check(AssetLayout.PoliceAkRunFrames + 16 * AssetLayout.IdleBankStride <=
                      AssetLayout.AllocationBytes &&
                  Control.PoliceAkRunFrame < 1024,
              "new CPU banks and visual phase fit their reserved areas");
        Console.WriteLine(
            "PASS police AK run: 192 native poses, 640ms cycle, projected muzzles and 384 host/client shots");
    }

    static void CheckNetworkMuzzle(int heading, int frame, int hostRole, double mx, double my)
    {
        var host = TestDuel.Create(true, hostRole, 450, TestMap.Empty, frame: frame);
        var client = TestDuel.Create(false, 1 - hostRole, 450, TestMap.Empty);
        double t = 0;
        for (int i = 0; i < 8; i++)
        {
            host.Observe(3, DuelPose.Spawn(host.Role), 450, false, false, t, .02);
            client.Observe(3, DuelPose.Spawn(client.Role), 450, false, false, t, .02);
            client.Receive(host.Packet(), 10, t);
            host.Receive(client.Packet(), 20, t);
            t += .02;
        }
        TestLoadout.Fill(host, t);
        client.Receive(host.Packet(), 10, t);

        var pose =
            new DuelPose { X = 1000, Y = 400, Heading = (byte)heading, Weapon = 1, Moving = 1, Phase = 3 };
        if (hostRole == 0)
            host.Observe(3, pose, 450, true, false, t, 0);
        else
        {
            client.Observe(3, pose, 450, true, false, t, 0);
            host.Receive(client.Packet(), 20, t);
        }
        Check(host.Shots.Count == 1 && host.Shots[0].Owner == 0 &&
                  Math.Abs(host.Shots[0].X - (1000 + mx)) < .001 &&
                  Math.Abs(host.Shots[0].Y - (400 + my)) < .001,
              "local and remote police shots follow the current running gun tip");
        Check(host.Armories[0].Fired[1] == 1, "only an accepted police AK shot consumes ammo");
    }

    static void Check(bool yes, string reason)
    {
        if (!yes)
            throw new Exception(reason);
    }
}
