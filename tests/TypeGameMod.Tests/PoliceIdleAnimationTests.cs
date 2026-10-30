using System;
using System.Collections.Generic;
using TypeGameMod;

internal static class PoliceIdleAnimationTests
{
    internal static void Run()
    {
        var idle = new PoliceIdleAnimation();
        var poses = new HashSet<int>();
        for (int i = 0; i < 140; i++)
        {
            int frame = idle.Update(true, .008);
            Check(frame >= 0 && frame < 9, "police idle stays in the nine authored cells");
            poses.Add(frame);
        }
        Check(poses.Count == 9, "all nine poses play during standing");
        Check(idle.Update(false, .02) == 0, "running, equipping or leaving the round resets idle");
        Check(idle.Update(true, .001) == 0, "returning to standing starts at the first pose");
        int paused = idle.Update(true, .2);
        Check(idle.Update(true, 0) == paused, "zero elapsed time does not advance the pose");
        idle.Update(false, 0);
        Check(idle.Update(true, PoliceIdleAnimation.CycleSeconds) == 0,
              "the measured original cycle wraps without a terminal pause");
        for (int heading = 0; heading < 8; heading++)
        {
            byte[] data = IdleAnimationTests.Source("PoliceIdle" + heading);
            Check(data.Length == GameIdentity.SpriteBytes && BitConverter.ToInt32(data, 4) == 276 &&
                      BitConverter.ToInt32(data, 8) == 594 && BitConverter.ToInt16(data, 14) == 8,
                  "police idle uses the native indexed DIB format");
            var distinct = new HashSet<string>();
            for (int frame = 0; frame < 9; frame++)
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
                              "spare cells repeat the nine accepted poses");
                    }
                Check(visible > 500, "each pose contains the full character and shadow");
                distinct.Add(Convert.ToBase64String(pixels));
            }
            Check(distinct.Count == 9, "each heading retains nine distinct authored poses");
        }
        Check(AssetLayout.PoliceIdleFrames >= AssetLayout.TypingArt &&
                  AssetLayout.PoliceIdleFrames + 8 * AssetLayout.IdleBankStride <=
                      AssetLayout.AllocationBytes,
              "new police sheets fit after the previous reserved assets");
        Check(Control.PoliceIdleFrame < 1024, "visual phase fits the existing control block");
        Console.WriteLine(
            "PASS police idle: 8 directions, 9 poses, original 517.97ms cycle, native DIBs and reset");
    }

    static void Check(bool yes, string reason)
    {
        if (!yes)
            throw new Exception(reason);
    }
}
