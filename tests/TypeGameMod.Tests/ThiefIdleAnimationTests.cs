using System;
using System.Collections.Generic;
using TypeGameMod;

internal static class ThiefIdleAnimationTests
{
    internal static void Run()
    {
        var idle = new ThiefIdleAnimation();
        var run = new RunAnimation();
        var visited = new HashSet<int>();
        for (int i = 0; i < 160; i++)
        {
            int frame = idle.Update(true, .008);
            Check(frame >= 0 && frame < 24, "idle remains inside its authored 24 poses");
            visited.Add(frame);
            Check(run.Update(false, .008) == 0, "standing does not advance running");
        }
        Check(visited.Count == 24, "standing visits every pose across both sheets");
        Check(idle.Update(false, .03) == 0, "moving, equipping or leaving the round resets idle");
        Check(idle.Update(true, .001) == 0, "returning to idle starts at the first pose");
        idle.Update(false, 0);
        Check(idle.Update(true, ThiefIdleAnimation.CycleSeconds) == 0,
              "idle follows one measured original cycle without a terminal pause");
        Check(run.Update(true, .2) > 0 && idle.Update(false, .2) == 0,
              "movement advances run while clearing the previous standing phase");

        for (int heading = 0; heading < 8; heading++)
        {
            byte[][] banks = { IdleAnimationTests.Source("ThiefIdle" + heading + "_0"),
                               IdleAnimationTests.Source("ThiefIdle" + heading + "_1") };
            foreach (byte[] data in banks)
                Check(data.Length == GameIdentity.SpriteBytes && BitConverter.ToInt32(data, 4) == 276 &&
                          BitConverter.ToInt32(data, 8) == 594 && BitConverter.ToInt16(data, 14) == 8,
                      "idle banks retain the engine's native indexed format");
            var poses = new HashSet<string>();
            for (int frame = 0; frame < 24; frame++)
            {
                byte[] data = banks[frame / 18];
                var decoded = new byte[92 * 99 * 4];
                for (int y = 0; y < 99; y++)
                    for (int x = 0; x < 92; x++)
                    {
                        int index = data[IdleAnimation.At(frame % 18, x, y)];
                        Buffer.BlockCopy(data, 40 + index * 4, decoded, (y * 92 + x) * 4, 4);
                    }
                poses.Add(Convert.ToBase64String(decoded));
            }
            Check(poses.Count == 24, "all eight idle headings preserve 24 distinct authored poses");
        }
        Check(AssetLayout.ThiefIdleTable0 >= AssetLayout.ThiefRunFrames + 16 * AssetLayout.IdleBankStride &&
                  AssetLayout.ThiefIdleTable1 + 32 <= AssetLayout.ThiefIdleFrames &&
                  AssetLayout.ThiefIdleFrames + 16 * AssetLayout.IdleBankStride <=
                      AssetLayout.AllocationBytes,
              "idle tables and banks fit beyond the accepted run assets without overlap");
        Console.WriteLine("PASS thief unarmed idle: 24 poses, original 629.55ms cycle, run/equip reset and " +
                          "eight directions");
    }

    static void Check(bool yes, string reason)
    {
        if (!yes)
            throw new Exception(reason);
    }
}
