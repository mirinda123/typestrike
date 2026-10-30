using System;
using System.Collections.Generic;
using TypeGameMod;

internal static class PoliceShotgunIdleTests
{
    internal static void Run()
    {
        for (int heading = 0; heading < 8; heading++)
        {
            byte[] data = IdleAnimationTests.Source("PoliceShotgunIdle" + heading);
            Check(data.Length == GameIdentity.SpriteBytes && BitConverter.ToInt32(data, 4) == 276 &&
                      BitConverter.ToInt32(data, 8) == 594 && BitConverter.ToInt16(data, 14) == 8,
                  "police shotgun idle uses the existing indexed DIB format");
            var poses = new HashSet<string>();
            var muzzles = new HashSet<string>();
            for (int frame = 0; frame < PoliceShotgunIdle.Frames; frame++)
            {
                var pixels = new byte[92 * 99];
                int visible = 0;
                for (int y = 0; y < 99; y++)
                    for (int x = 0; x < 92; x++)
                    {
                        byte pixel = data[IdleAnimation.At(frame, x, y)];
                        pixels[y * 92 + x] = pixel;
                        if (pixel != 0)
                            visible++;
                        Check(pixel == data[IdleAnimation.At(frame + 9, x, y)],
                              "spare cells repeat the approved nine poses");
                    }
                Check(visible > 500, "every pose contains a complete character");
                poses.Add(Convert.ToBase64String(pixels));
                double mx, my, bx, by;
                PoliceShotgunIdle.Muzzle(heading, frame, out mx, out my);
                WeaponMuzzle.Offset(0, GameRules.Shotgun, heading, false, frame, out bx, out by);
                int offset = PoliceShotgunIdle.DrawOffsetX(heading);
                Check(mx - offset > 0 && mx - offset < 92 && my > 0 && my < 99 && bx == mx && by == my,
                      "gun tip and destination use the same authored packing compensation");
                muzzles.Add(mx + "/" + my);
                for (int hostRole = 0; hostRole < 2; hostRole++)
                    CheckNetworkMuzzle(heading, frame, hostRole, mx, my);
            }
            Check(poses.Count == 9 && muzzles.Count >= 5,
                  "all nine poses and moving gun-tip positions survive");
        }
        Console.WriteLine("PASS police shotgun idle: 72 poses, west packing compensation and 144 " +
                          "host/client six-pellet shots");
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

        var pose = new DuelPose { X = 1000, Y = 400, Heading = (byte)heading, Weapon = 2, Moving = 0 };
        if (hostRole == 0)
            host.Observe(3, pose, 450, true, false, t, 0);
        else
        {
            client.Observe(3, pose, 450, true, false, t, 0);
            host.Receive(client.Packet(), 20, t);
        }
        Check(host.Shots.Count == 6, "a police shotgun shot still creates six pellets");
        foreach (DuelShot shot in host.Shots)
            Check(shot.Owner == 0 && Math.Abs(shot.X - (1000 + mx)) < .001 &&
                      Math.Abs(shot.Y - (400 + my)) < .001,
                  "every local/remote pellet starts at the current compensated gun tip");
        Check(host.Armories[0].Fired[2] == 1 && host.Armories[0].Fired[1] == 0,
              "only one shotgun round is consumed");
    }

    static void Check(bool yes, string reason)
    {
        if (!yes)
            throw new Exception(reason);
    }
}
