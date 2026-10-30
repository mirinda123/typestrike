using System;
using System.Collections.Generic;
using TypeGameMod;

internal static class ThiefRunAnimationTests
{
    internal static void Run()
    {
        var clock = new RunAnimation();
        var visited = new HashSet<int>();
        for (int i = 0; i < 160; i++)
        {
            int frame = clock.Update(true, .008);
            Check(frame >= 0 && frame < 24, "run clock stays inside its 24 authored poses");
            visited.Add(frame);
        }
        Check(visited.Count == 24, "continuous movement plays every pose across both sheets");
        Check(clock.Update(false, .04) == 0, "stopping or equipping a weapon resets the run");
        Check(clock.Update(true, .001) == 0, "running starts in the first pose again");
        Check(clock.Update(true, .64) == 0, "one full cycle returns to the same phase");
        for (int heading = 0; heading < 8; heading++)
        {
            byte[][] banks = { IdleAnimationTests.Source("ThiefRun" + heading + "_0"),
                               IdleAnimationTests.Source("ThiefRun" + heading + "_1") };
            foreach (byte[] data in banks)
                Check(data.Length == GameIdentity.SpriteBytes && BitConverter.ToInt32(data, 4) == 276 &&
                          BitConverter.ToInt32(data, 8) == 594 && BitConverter.ToInt16(data, 14) == 8,
                      "every heading uses the native indexed sprite format");
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
            Check(poses.Count == 24, "all eight headings retain 24 distinct decoded poses");
        }
        Check(AssetLayout.ThiefRunFrames >= AssetLayout.IdleFrames + 48 * AssetLayout.IdleBankStride &&
                  AssetLayout.ThiefRunFrames + 16 * AssetLayout.IdleBankStride <= AssetLayout.AllocationBytes,
              "new run banks do not overlap existing art and fit the allocation");
        Console.WriteLine(
            "PASS thief unarmed run: 24 poses, 640ms loop, stop/equip reset and eight native banks");
    }

    static void Check(bool yes, string reason)
    {
        if (!yes)
            throw new Exception(reason);
    }
}
