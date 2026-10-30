using System;
using System.Collections.Generic;
using TypeGameMod;

internal static class ThiefShotgunRunTests
{
    internal static void Run()
    {
        for (int heading = 0; heading < 8; heading++)
        {
            byte[][] banks = { IdleAnimationTests.Source("ThiefShotgunRun" + heading + "_0"),
                               IdleAnimationTests.Source("ThiefShotgunRun" + heading + "_1") };
            var poses = new HashSet<string>();
            var muzzles = new HashSet<string>();
            for (int frame = 0; frame < ThiefShotgunRun.Frames; frame++)
            {
                byte[] data = banks[frame / 18];
                Check(data.Length == GameIdentity.SpriteBytes && BitConverter.ToInt32(data, 4) == 276 &&
                          BitConverter.ToInt32(data, 8) == 594 && BitConverter.ToInt16(data, 14) == 8,
                      "shotgun running banks retain the native indexed DIB format");
                var decoded = new byte[92 * 99 * 4];
                for (int y = 0; y < 99; y++)
                    for (int x = 0; x < 92; x++)
                    {
                        int index = data[IdleAnimation.At(frame % 18, x, y)];
                        Buffer.BlockCopy(data, 40 + index * 4, decoded, (y * 92 + x) * 4, 4);
                    }
                poses.Add(Convert.ToBase64String(decoded));
                double mx, my;
                WeaponMuzzle.Offset(1, GameRules.Shotgun, heading, true, frame, out mx, out my);
                Check(mx - ThiefShotgunRun.DrawOffsetX(heading) > 0 &&
                          mx - ThiefShotgunRun.DrawOffsetX(heading) < 92 && my > 0 && my < 99,
                      "running muzzle stays inside its frame after draw-origin compensation");
                muzzles.Add(mx + "/" + my);
                double px, py;
                WeaponMuzzle.Offset(1, GameRules.Shotgun, heading, true, frame, out px, out py);
                Check(px == mx && py == my, "native four-phase packets do not alter the 24-pose muzzle");
            }
            Check(poses.Count == 24 && muzzles.Count == 24,
                  "each heading retains every authored pose and its projected gun tip");
        }
        Check(AssetLayout.ThiefShotgunRunTable0 >=
                      AssetLayout.ThiefShotgunPoseFrames + 16 * AssetLayout.IdleBankStride &&
                  AssetLayout.ThiefShotgunRunTable1 + 32 <= AssetLayout.ThiefShotgunRunFrames &&
                  AssetLayout.ThiefShotgunRunFrames + 16 * AssetLayout.IdleBankStride <=
                      AssetLayout.AllocationBytes,
              "shotgun run tables and banks do not overlap any accepted assets");
        Check(Control.ThiefShotgunRunFrame + 4 <= 1024, "new visual state fits the existing control block");
        Check(AssetLayout.ThiefShotgunRunTable1 + 32 <= AssetLayout.ThiefShotgunRunOrigins &&
                  AssetLayout.ThiefShotgunRunOrigins + 32 <= AssetLayout.ThiefShotgunRunFrames &&
                  Control.ThiefShotgunRunOrigins + 4 <= 1024,
              "run origin tables and control pointers stay within their allocation");
        var clock = new RunAnimation();
        var visited = new HashSet<int>();
        for (int i = 0; i < 160; i++)
            visited.Add(clock.Update(true, .008));
        Check(visited.Count == 24 && clock.Update(false, .05) == 0 && clock.Update(true, .001) == 0,
              "shotgun run plays every pose and resets when stopping, switching or losing focus");
        // Pure protocol simulation: no DirectPlay process, socket or listener.
        for (int frame = 0; frame < ThiefShotgunRun.Frames; frame++)
            foreach (int hostRole in new[] { 0, 1 })
                for (int heading = 0; heading < 8; heading++)
                    CheckNetworkMuzzle(frame, hostRole, heading);
        Console.WriteLine("PASS thief shotgun run: 192 native poses, bank bounds, projected muzzles and " +
                          "384 host/client six-pellet shots");
    }

    static void CheckNetworkMuzzle(int frame, int hostRole, int heading)
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
            new DuelPose { X = 1000, Y = 400, Heading = (byte)heading, Weapon = 2, Moving = 1, Phase = 3 };
        if (hostRole == 1)
            host.Observe(3, pose, 450, true, false, t, 0);
        else
        {
            client.Observe(3, pose, 450, true, false, t, 0);
            host.Receive(client.Packet(), 20, t);
        }
        double mx, my;
        ThiefShotgunRun.Muzzle(heading, frame, out mx, out my);
        Check(host.Shots.Count == 6, "one running shotgun fire creates six pellets");
        foreach (DuelShot shot in host.Shots)
            Check(shot.Owner == 1 && Math.Abs(shot.X - (1000 + mx)) < .001 &&
                      Math.Abs(shot.Y - (400 + my)) < .001,
                  "host and remote thief pellets start at the current running barrel tip");
    }

    static void Check(bool yes, string reason)
    {
        if (!yes)
            throw new Exception(reason);
    }
}
