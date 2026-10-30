using System;
using System.Collections.Generic;
using TypeGameMod;

internal static class ThiefAkAnimationTests
{
    internal static void Run()
    {
        var animation = new ThiefAkAnimation();
        var visited = new HashSet<int>();
        for (int i = 0; i < 1000; i++)
        {
            int frame = animation.Update(true, 0, .008);
            visited.Add(frame);
            Check(frame < ThiefAkPose.IdleFrames, "empty ammunition must never play a firing pose");
        }
        Check(visited.Count == 24, "new rifle idle plays all 24 approved poses");
        Check(animation.Update(true, 1, .008) == 24, "an actual shot starts the flash pose");
        Check(animation.Update(true, 1, .04) == 25, "recoil advances at 30 FPS");
        Check(animation.Update(true, 1, .04) == 26, "recoil returns without another bullet");
        Check(animation.Update(true, 2, .008) == 24, "a second accepted bullet restarts recoil");
        Check(animation.Update(true, 2, .15) == 28, "the final recoil pose stays in the second bank");
        Check(animation.Update(true, 2, .05) < 24, "one shot ends and returns to breathing");
        Check(animation.Update(true, 0, .008) < 24, "a new round resets the shot counter");
        animation.Update(false, 7, .01);
        Check(animation.Update(true, 7, .008) < 24, "focus restoration does not replay old shots");
        Check(animation.Update(true, 8, .008) == 24, "mirrored remote shot counts drive the same pose");
        for (int heading = 0; heading < 8; heading++)
        {
            byte[][] banks = { IdleAnimationTests.Source("ThiefAkPose" + heading + "_0"),
                               IdleAnimationTests.Source("ThiefAkPose" + heading + "_1") };
            foreach (byte[] data in banks)
                Check(data.Length == GameIdentity.SpriteBytes && BitConverter.ToInt32(data, 4) == 276 &&
                          BitConverter.ToInt32(data, 8) == 594 && BitConverter.ToInt16(data, 14) == 8,
                      "both rifle banks retain the engine's indexed DIB format");
            var poses = new HashSet<string>();
            for (int frame = 0; frame < ThiefAkPose.Frames; frame++)
            {
                byte[] data = banks[frame / 18];
                var decoded = new byte[92 * 99 * 4];
                for (int y = 0; y < 99; y++)
                    for (int x = 0; x < 92; x++)
                    {
                        int index = data[IdleAnimation.At(frame % 18, x, y)];
                        Buffer.BlockCopy(data, 40 + index * 4, decoded, (y * 92 + x) * 4, 4);
                    }
                if (frame < 24)
                    poses.Add(Convert.ToBase64String(decoded));
                double xMuzzle, yMuzzle;
                WeaponMuzzle.Offset(1, GameRules.Ak47, heading, false, frame, out xMuzzle, out yMuzzle);
                Check(xMuzzle - ThiefAkPose.DrawOffsetX(heading) > 0 &&
                          xMuzzle - ThiefAkPose.DrawOffsetX(heading) < 92 && yMuzzle > 0 && yMuzzle < 99,
                      "all idle and firing poses use their projected gun muzzle");
            }
            Check(poses.Count == 24, "every heading retains all 24 distinct approved idle frames");
        }
        Check(AssetLayout.ThiefAkPoseTable0 >=
                      AssetLayout.ThiefIdleFrames + 16 * AssetLayout.IdleBankStride &&
                  AssetLayout.ThiefAkPoseTable1 + 32 <= AssetLayout.ThiefAkPoseFrames &&
                  AssetLayout.ThiefAkPoseFrames + 16 * AssetLayout.IdleBankStride <=
                      AssetLayout.AllocationBytes,
              "new rifle banks fit beyond the existing idle/run assets");
        Console.WriteLine("PASS thief AK idle, actual-shot recoil, empty gun, round reset and focus checks");
    }

    static void Check(bool yes, string reason)
    {
        if (!yes)
            throw new Exception(reason);
    }
}
