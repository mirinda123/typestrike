using System;
using System.Collections.Generic;
using TypeGameMod;

internal static class PoliceRunTests
{
    internal static void Run()
    {
        for (int heading = 0; heading < 8; heading++)
        {
            byte[][] banks = { IdleAnimationTests.Source("PoliceRun" + heading + "_0"),
                               IdleAnimationTests.Source("PoliceRun" + heading + "_1") };
            var poses = new HashSet<string>();
            for (int frame = 0; frame < RunAnimation.Frames; frame++)
            {
                byte[] bank = banks[frame / 18];
                Check(bank.Length == GameIdentity.SpriteBytes,
                      "unarmed run fits the existing DIB surface size");
                int visible = 0;
                var pixels = new byte[92 * 99];
                for (int y = 0; y < 99; y++)
                    for (int x = 0; x < 92; x++)
                    {
                        byte value = bank[IdleAnimation.At(frame % 18, x, y)];
                        pixels[y * 92 + x] = value;
                        if (value != 0)
                            visible++;
                    }
                Check(visible > 500, "every heading and phase contains the whole unarmed character");
                poses.Add(Convert.ToBase64String(pixels));
            }
            Check(poses.Count == 24, "all approved FK running poses survive packing");
        }
        var clock = new RunAnimation();
        var phases = new HashSet<int>();
        for (int i = 0; i < 160; i++)
            phases.Add(clock.Update(true, .008));
        Check(phases.Count == 24 && RunAnimation.CycleSeconds == .64,
              "unarmed run keeps the approved 640ms rhythm");
        Check(clock.Update(false, .02) == 0 && clock.Update(true, .008) == 0,
              "stop, equipment or focus interruption restarts from the first running pose");
        Check(AssetLayout.PoliceRunFrames + 16 * AssetLayout.IdleBankStride <=
                      AssetLayout.PoliceShieldRunTable0 &&
                  AssetLayout.PoliceRunTable1 + 32 <= AssetLayout.PoliceRunFrames &&
                  Control.PoliceRunFrame + 4 <= 1024,
              "new CPU banks and visual clock fit their reserved areas");
        Console.WriteLine("PASS police unarmed run: 192 FK poses, eight headings, bank bounds and 640ms " +
                          "stop/restart clock");
    }

    static void Check(bool value, string reason)
    {
        if (!value)
            throw new Exception(reason);
    }
}
