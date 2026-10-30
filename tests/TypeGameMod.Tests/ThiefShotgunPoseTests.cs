using System;
using System.Collections.Generic;
using TypeGameMod;

internal static class ThiefShotgunPoseTests
{
    internal static void Run()
    {
        var idle = new ThiefIdleAnimation();
        var unarmed = new ThiefIdleAnimation();
        Check(idle.Update(true, .2) > 0 && unarmed.Update(false, .2) == 0,
              "shotgun breathing has an independent standing clock");
        Check(idle.Update(false, .01) == 0 && idle.Update(true, .001) == 0,
              "moving, switching or losing focus resets the shotgun standing pose");
        for (int heading = 0; heading < 8; heading++)
        {
            byte[][] banks = { IdleAnimationTests.Source("ThiefShotgunPose" + heading + "_0"),
                               IdleAnimationTests.Source("ThiefShotgunPose" + heading + "_1") };
            var poses = new HashSet<string>();
            var muzzles = new HashSet<string>();
            double runningX, runningY;
            WeaponMuzzle.Offset(1, GameRules.Shotgun, heading, true, 0, out runningX, out runningY);
            for (int frame = 0; frame < ThiefShotgunPose.IdleFrames; frame++)
            {
                byte[] data = banks[frame / 18];
                Check(data.Length == GameIdentity.SpriteBytes && BitConverter.ToInt32(data, 4) == 276 &&
                          BitConverter.ToInt32(data, 8) == 594 && BitConverter.ToInt16(data, 14) == 8,
                      "standing shotgun banks retain the native indexed DIB format");
                var decoded = new byte[92 * 99 * 4];
                for (int y = 0; y < 99; y++)
                    for (int x = 0; x < 92; x++)
                    {
                        int index = data[IdleAnimation.At(frame % 18, x, y)];
                        Buffer.BlockCopy(data, 40 + index * 4, decoded, (y * 92 + x) * 4, 4);
                    }
                poses.Add(Convert.ToBase64String(decoded));
                double mx, my;
                WeaponMuzzle.Offset(1, GameRules.Shotgun, heading, false, frame, out mx, out my);
                Check(
                    mx - ThiefShotgunPose.DrawOffsetX(heading) > 0 &&
                        mx - ThiefShotgunPose.DrawOffsetX(heading) < 92 && my > 0 && my < 99,
                    "every idle pose has a projected muzzle inside its cell after draw-origin compensation");
                muzzles.Add(mx + "/" + my);
                WeaponMuzzle.Offset(1, GameRules.Shotgun, heading, true, 0, out mx, out my);
                Check(mx == runningX && my == runningY,
                      "idle clock does not affect the independent running muzzle");
            }
            Check(poses.Count == 24 && muzzles.Count == 24,
                  "all eight headings retain 24 authored poses and corresponding gun tips");
        }
        Check(AssetLayout.ThiefShotgunPoseTable0 >=
                      AssetLayout.ThiefAkRunFrames + 16 * AssetLayout.IdleBankStride &&
                  AssetLayout.ThiefShotgunPoseTable1 + 32 <= AssetLayout.ThiefShotgunPoseFrames &&
                  AssetLayout.ThiefShotgunPoseFrames + 16 * AssetLayout.IdleBankStride <=
                      AssetLayout.AllocationBytes &&
                  Control.ThiefShotgunIdleFrame + 4 <= 1024,
              "shotgun tables and visual state fit beyond existing assets and inside the control block");
        for (int frame = 0; frame < ThiefShotgunPose.IdleFrames; frame++)
            foreach (int hostRole in new[] { 0, 1 })
                CheckNetworkMuzzle(frame, hostRole);
        Console.WriteLine("PASS thief shotgun idle: 192 native poses, independent running clock and 66 " +
                          "host/client six-pellet shots");
    }

    static void CheckNetworkMuzzle(int frame, int hostRole)
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

        var pose = new DuelPose { X = 1000, Y = 400, Heading = 2, Weapon = 2, Moving = 0 };
        if (hostRole == 1)
            host.Observe(3, pose, 450, true, false, t, 0);
        else
        {
            client.Observe(3, pose, 450, true, false, t, 0);
            host.Receive(client.Packet(), 20, t);
        }
        double mx, my;
        ThiefShotgunPose.Muzzle(2, frame, out mx, out my);
        Check(host.Shots.Count == 6, "one accepted shotgun fire creates six pellets");
        foreach (DuelShot shot in host.Shots)
            Check(shot.Owner == 1 && Math.Abs(shot.X - (1000 + mx)) < .001 &&
                      Math.Abs(shot.Y - (400 + my)) < .001,
                  "host and remote stationary/recoil shots use the current shotgun barrel tip");
    }

    static void Check(bool yes, string reason)
    {
        if (!yes)
            throw new Exception(reason);
    }
}
