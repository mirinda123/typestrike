using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using TypeGameMod;

internal static class ShotgunEffectsTests
{
    static int checks;
    internal static void Run()
    {
        var animation = new ThiefShotgunAnimation();
        for (int i = 0; i < 150; i++)
            Check(animation.Update(true, 0, .008) < 24, "empty fire cannot play recoil");
        Check(animation.Update(true, 1, .008) == 24, "actual shotgun fire starts recoil");
        Check(animation.Update(true, 1, .04) == 25, "recoil advances at 30 FPS");
        Check(animation.Update(true, 1, .27) < 24, "recoil ends after 300ms");
        animation.Update(false, 2, .01);
        Check(animation.Update(true, 2, .01) < 24, "focus or weapon restoration cannot replay old fire");
        Check(animation.Update(true, 3, .01) == 24, "a new round accepts subsequent shots");
        Check(animation.Update(true, 0, .01) < 24, "counter reset clears recoil");
        Check(SnapshotLayout.ShotgunFxRects >= SnapshotLayout.ThiefInvulnerable + 4 &&
                  SnapshotLayout.ShotgunFxRects + 2 * ShotgunFireEffect.MaxRects * 20 <=
                      ModSession.SnapshotBytes,
              "effects fit existing unused snapshot space");
        var export = new List<string>();
        for (int h = 0; h < 8; h++)
        {
            var effect = new ShotgunFireEffect();
            var poseClock = new ThiefShotgunAnimation();
            for (int f = 0; f < 21; f++)
            {
                int pose = poseClock.Update(true, 1, f == 0 ? 0 : 1.0 / 30);
                double mx, my;
                ThiefShotgunPose.Muzzle(h, pose, out mx, out my);
                mx += 23;
                my += 18;
                effect.Update(true, 1, mx, my, (uint)h, f == 0 ? 0 : 1.0 / 30);
                byte[] snapshot = new byte[ModSession.SnapshotBytes];
                TestFireEffects.Write(effect, snapshot, mx, my, 0, 0);
                int count = BitConverter.ToInt32(snapshot, SnapshotLayout.ShotgunFxCount);
                Check(count >= 0 && count <= 11, "flash and smoke stay in the allocated commands");
                if (f == 0)
                    Check(count == 8, "one shot has an orange silhouette and a bright core");
                if (f == 3)
                    Check(count == 3, "flash ends before the rising smoke");
                if (f >= 10)
                    Check(count == 0, "smoke expires without retriggering from held fire");
                var values =
                    new List<string> { h.ToString(), f.ToString(), pose.ToString(), count.ToString() };
                for (int n = 0; n < count; n++)
                {
                    int at = SnapshotLayout.ShotgunFxRects + n * 20;
                    int left = BitConverter.ToInt32(snapshot, at),
                        top = BitConverter.ToInt32(snapshot, at + 4);
                    int right = BitConverter.ToInt32(snapshot, at + 8),
                        bottom = BitConverter.ToInt32(snapshot, at + 12);
                    Check(left >= 0 && top >= 0 && right <= 800 && bottom <= 500 && right > left &&
                              bottom > top,
                          "effect rectangles are integer pixels above the article");
                    for (int k = 0; k < 5; k++)
                        values.Add(BitConverter.ToUInt32(snapshot, at + k * 4)
                                       .ToString(CultureInfo.InvariantCulture));
                }
                export.Add(String.Join(",", values.ToArray()));
            }
            effect.Update(false, 7, 0, 0, (uint)h, .01);
            effect.Update(true, 7, 0, 0, (uint)h, .01);
            Check(Double.IsPositiveInfinity(effect.Age),
                  "focus restoration does not replay remote or local shots");
            effect.Update(true, 8, 0, 0, (uint)h, .01);
            Check(effect.Age == 0, "the next accepted shot restarts the effect");
            effect.Update(true, 0, 0, 0, (uint)h, .01);
            Check(Double.IsPositiveInfinity(effect.Age), "new round clears old smoke");
        }
        // Include viewport edges, outside cameras and the 70ms overlap of flash/smoke.
        foreach (double x in new[] { -200.0, 0, 799, 1000 })
            foreach (double y in new[] { -100.0, 0, 499, 700 })
                for (uint h = 0; h < 8; h++)
                {
                    var fx = new ShotgunFireEffect();
                    fx.Update(true, 1, x, y, h, 0);
                    fx.Update(true, 1, x, y, h, .05);
                    byte[] data = new byte[ModSession.SnapshotBytes];
                    TestFireEffects.Write(fx, data, x, y, 0, 0);
                    Check(BitConverter.ToInt32(data, SnapshotLayout.ShotgunFxCount) <= 11,
                          "edge clipping stays within bounds");
                }
        for (int h = 0; h < 8; h++)
        {
            var poses = new HashSet<string>();
            byte[] bank = IdleAnimationTests.Source("ThiefShotgunPose" + h + "_1");
            for (int f = 24; f < 33; f++)
            {
                double x, y;
                ThiefShotgunPose.Muzzle(h, f, out x, out y);
                Check(x - ThiefShotgunPose.DrawOffsetX(h) > 0 && x - ThiefShotgunPose.DrawOffsetX(h) < 92 &&
                          y > 0 && y < 99,
                      "recoil muzzle matches the current body pose");
                var cell = new byte[92 * 99];
                for (int row = 0; row < 99; row++)
                    for (int col = 0; col < 92; col++)
                        cell[row * 92 + col] = bank[IdleAnimation.At(f % 18, col, row)];
                poses.Add(Convert.ToBase64String(cell));
            }
            Check(poses.Count >= 5, "each heading contains actual body recoil poses");
        }
        string output = Environment.GetEnvironmentVariable("TYPEGAME_FX_PREVIEW");
        if (!String.IsNullOrEmpty(output))
            File.WriteAllLines(output, export.ToArray());
        Console.WriteLine("PASS " + checks +
                          (" shotgun effects: real shots, empty/focus/reset, 72 recoil poses, eight " +
                           "headings and pixel clipping"));
    }
    static void Check(bool value, string reason)
    {
        checks++;
        if (!value)
            throw new Exception(reason);
    }
}
