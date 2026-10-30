using System;
using System.Collections.Generic;
using TypeGameMod;

internal static class PoliceAkIdleTests
{
    internal static void Run()
    {
        for (int heading = 0; heading < 8; heading++)
        {
            byte[] data = IdleAnimationTests.Source("PoliceAkIdle" + heading);
            Check(data.Length == GameIdentity.SpriteBytes && BitConverter.ToInt32(data, 4) == 276 &&
                      BitConverter.ToInt32(data, 8) == 594 && BitConverter.ToInt16(data, 14) == 8,
                  "police AK idle fits the existing indexed DIB bank");
            var poses = new HashSet<string>();
            var muzzles = new HashSet<string>();
            for (int frame = 0; frame < PoliceAkIdle.Frames; frame++)
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
                              "spare cells repeat the approved poses");
                    }
                Check(visible > 500, "all frames contain a complete character");
                poses.Add(Convert.ToBase64String(pixels));
                double x0, y0, bx, by;
                PoliceAkIdle.Muzzle(heading, frame, out x0, out y0);
                WeaponMuzzle.Offset(0, GameRules.Ak47, heading, false, frame, out bx, out by);
                Check(x0 - PoliceAkIdle.DrawOffsetX(heading) > 0 &&
                          x0 - PoliceAkIdle.DrawOffsetX(heading) < 92 && y0 > 0 && y0 < 99 && bx == x0 &&
                          by == y0,
                      "standing bullets use the rendered gun tip for this pose");
                muzzles.Add(x0 + "/" + y0);
                for (int hostRole = 0; hostRole < 2; hostRole++)
                    CheckNetworkMuzzle(heading, frame, hostRole, x0, y0);
            }
            Check(poses.Count == 9 && muzzles.Count == 9, "all nine poses and muzzle projections survive");
        }
        Console.WriteLine(
            "PASS police AK idle: 72 poses, exact bank import, projected muzzles and 144 host/client shots");
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

        var pose = new DuelPose { X = 1000, Y = 400, Heading = (byte)heading, Weapon = 1, Moving = 0 };
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
              "local and remote police AK fire use the current native-cell gun tip");
        Check(host.Armories[0].Fired[1] == 1, "actual AK ammo is consumed");
    }

    static void Check(bool yes, string reason)
    {
        if (!yes)
            throw new Exception(reason);
    }
}
