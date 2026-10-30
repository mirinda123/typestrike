using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using TypeGameMod;

internal static class AkFireEffectsTests
{
    static int checks;

    internal static void Run()
    {
        var export = new List<string>();
        foreach (int mode in new[] { 0, 1 })
            for (int heading = 0; heading < 8; heading++)
            {
                var clock = new ThiefAkAnimation();
                var effect = new AkFireEffect();
                for (int f = 0; f < (mode == 0 ? 54 : 36); f++)
                {
                    uint shots = (uint)(mode == 0 ? Math.Min(3, 1 + f / 9) : 1);
                    double dt = f == 0 ? 0 : .02;
                    int pose = clock.Update(true, shots, dt);
                    double x, y;
                    ThiefAkPose.Muzzle(heading, pose, out x, out y);
                    x += 23;
                    y += 18;
                    effect.Update(true, shots, x, y, (uint)heading, dt);
                    var data = new byte[ModSession.SnapshotBytes];
                    TestFireEffects.Write(effect, data, x, y, 0, 0);
                    int count = BitConverter.ToInt32(data, SnapshotLayout.ShotgunFxCount);
                    Check(count >= 0 && count <= AkFireEffect.MaxRects,
                          "AK fire commands fit the shared pixel channel");
                    if (f == 0 || (mode == 0 && (f == 9 || f == 18)))
                        Check(pose == 24 && effect.Age == 0 && count == 6,
                              "180ms accepted fire events start recoil and flash together");
                    if ((mode == 1 && f >= 10) || (mode == 0 && f >= 28))
                        Check(count == 0, "a completed shot cannot leave persistent smoke");
                    var row = new List<string> { mode.ToString(), heading.ToString(), f.ToString(),
                                                 pose.ToString(), count.ToString() };
                    for (int i = 0; i < count * 5; i++)
                        row.Add(BitConverter.ToInt32(data, SnapshotLayout.ShotgunFxRects + i * 4)
                                    .ToString(CultureInfo.InvariantCulture));
                    export.Add(String.Join(",", row.ToArray()));
                }
                effect.Update(false, 7, 0, 0, (uint)heading, .01);
                effect.Update(true, 7, 0, 0, (uint)heading, .01);
                Check(Double.IsPositiveInfinity(effect.Age),
                      "switching back or restoring focus cannot replay old fire");
                effect.Update(true, 8, 0, 0, (uint)heading, .01);
                Check(effect.Age == 0, "a new accepted fire restarts the effect");
                effect.Update(true, 0, 0, 0, (uint)heading, .01);
                Check(Double.IsPositiveInfinity(effect.Age), "round reset clears smoke");
            }
        foreach (double x in new[] { -200.0, 0, 799, 1200 })
            foreach (double y in new[] { -100.0, 0, 499, 800 })
                for (uint heading = 0; heading < 8; heading++)
                {
                    var effect = new AkFireEffect();
                    effect.Update(true, 1, x, y, heading, 0);
                    effect.Update(true, 1, x, y, heading, .04);
                    var data = new byte[ModSession.SnapshotBytes];
                    TestFireEffects.Write(effect, data, x, y, 0, 0);
                    int count = BitConverter.ToInt32(data, SnapshotLayout.ShotgunFxCount);
                    Check(count <= 8, "edge clipping retains at most eight rectangles");
                    for (int i = 0; i < count; i++)
                    {
                        int at = SnapshotLayout.ShotgunFxRects + i * 20;
                        int left = BitConverter.ToInt32(data, at), top = BitConverter.ToInt32(data, at + 4);
                        int right = BitConverter.ToInt32(data, at + 8),
                            bottom = BitConverter.ToInt32(data, at + 12);
                        Check(left >= 0 && top >= 0 && right <= 800 && bottom <= 500 && left < right &&
                                  top < bottom,
                              "flash/smoke stays in the gameplay viewport above the article");
                    }
                }
        CheckSmokeAndCamera();
        for (int heading = 0; heading < 8; heading++)
            for (int pose = 24; pose < 29; pose++)
                foreach (int hostRole in new[] { 0, 1 })
                    CheckNetworkMuzzle(heading, pose, hostRole);
        string path = Environment.GetEnvironmentVariable("TYPEGAME_AK_FX_PREVIEW");
        if (!String.IsNullOrEmpty(path))
            File.WriteAllLines(path, export.ToArray());
        Console.WriteLine("PASS " + checks +
                          (" AK fire effects: 720 preview samples, empty/focus/switch/reset, clipping, " +
                           "camera and 80 host/client recoil shots"));
    }

    static void CheckSmokeAndCamera()
    {
        var effect = new AkFireEffect();
        effect.Update(true, 1, 100, 100, 2, 0);
        effect.Update(true, 1, 200, 200, 2, .04);
        var data = new byte[ModSession.SnapshotBytes];
        TestFireEffects.Write(effect, data, 200, 200, 0, 0);
        Check(BitConverter.ToInt32(data, SnapshotLayout.ShotgunFxCount) == 8,
              "late flash and early smoke overlap");
        Check(BitConverter.ToInt32(data, SnapshotLayout.ShotgunFxRects) > 190,
              "moving flash follows the current barrel");
        Check(BitConverter.ToInt32(data, SnapshotLayout.ShotgunFxRects + 6 * 20) < 110,
              "old smoke remains at the shot's world position");
        var shifted = new byte[ModSession.SnapshotBytes];
        TestFireEffects.Write(effect, shifted, 200, 200, 30, 40);
        for (int i = 0; i < 8; i++)
        {
            int at = SnapshotLayout.ShotgunFxRects + i * 20;
            for (int axis = 0; axis < 4; axis++)
                Check(BitConverter.ToInt32(data, at + axis * 4) -
                              BitConverter.ToInt32(shifted, at + axis * 4) ==
                          (axis % 2 == 0 ? 30 : 40),
                      "camera changes translate flash and smoke consistently");
        }
    }

    static void CheckNetworkMuzzle(int heading, int poseFrame, int hostRole)
    {
        var host = TestDuel.Create(true, hostRole, 450, TestMap.Empty, frame: poseFrame);
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
        if (hostRole == 1)
            host.Observe(3, pose, 450, true, false, t, 0);
        else
        {
            client.Observe(3, pose, 450, true, false, t, 0);
            host.Receive(client.Packet(), 20, t);
        }
        double x, y;
        ThiefAkPose.Muzzle(heading, poseFrame, out x, out y);
        Check(host.Shots.Count == 1 && host.Shots[0].Owner == 1 &&
                  Math.Abs(host.Shots[0].X - (1000 + x)) < .001 &&
                  Math.Abs(host.Shots[0].Y - (400 + y)) < .001,
              "local or remote thief fire uses the approved current recoil muzzle");
        Check(host.Armories[1].Fired[1] == 1 && host.Armories[1].Fired[2] == 0,
              "only the accepted AK counter drives the effect on either host role");
    }

    static void Check(bool value, string reason)
    {
        checks++;
        if (!value)
            throw new Exception(reason);
    }
}
