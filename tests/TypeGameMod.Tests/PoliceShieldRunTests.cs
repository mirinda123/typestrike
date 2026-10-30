using System;
using System.Collections.Generic;
using TypeGameMod;

internal static class PoliceShieldRunTests
{
    internal static void Run()
    {
        for (int heading = 0; heading < 8; heading++)
        {
            byte[][] banks = { IdleAnimationTests.Source("PoliceShieldRun" + heading + "_0"),
                               IdleAnimationTests.Source("PoliceShieldRun" + heading + "_1") };
            var poses = new HashSet<string>();
            for (int frame = 0; frame < RunAnimation.Frames; frame++)
            {
                byte[] bank = banks[frame / 18];
                Check(bank.Length == GameIdentity.SpriteBytes, "shield run fits the existing surface size");
                int visible = 0;
                var pixels = new byte[92 * 99 * 4];
                for (int y = 0; y < 99; y++)
                    for (int x = 0; x < 92; x++)
                    {
                        byte index = bank[IdleAnimation.At(frame % 18, x, y)];
                        uint color = BitConverter.ToUInt32(bank, 40 + index * 4) & 0xffffff;
                        BinaryFields.Write(pixels, (y * 92 + x) * 4, color);
                        if (color != 0)
                            visible++;
                    }
                Check(visible > 500, "each shield run pose contains a complete character");
                poses.Add(Convert.ToBase64String(pixels));
            }
            Check(poses.Count == 24, "all shield poses survive indexed palette packing");
        }
        var clock = new RunAnimation();
        var phases = new HashSet<int>();
        for (int i = 0; i < 160; i++)
            phases.Add(clock.Update(true, .008));
        Check(phases.Count == 24 && RunAnimation.CycleSeconds == .64,
              "shield movement uses the approved 640ms cycle");
        Check(clock.Update(false, .02) == 0 && clock.Update(true, .008) == 0,
              "stopping, releasing the shield or leaving play resets its clock");
        Check(AssetLayout.PoliceShieldRunTable1 + 32 <= AssetLayout.PoliceShieldRunFrames &&
                  AssetLayout.PoliceShieldRunFrames + 16 * AssetLayout.IdleBankStride ==
                      AssetLayout.EnergyHud &&
                  AssetLayout.EnergyHud + 122 * EnergyHud.StateBytes == AssetLayout.MovementMap,
              "shield banks fit their dedicated CPU allocation");
        Check(Control.PoliceShieldRunFrame + 4 <= 1024, "shield phase fits the unchanged control block");
        Console.WriteLine("PASS police shield run: 192 approved poses, eight headings, bank bounds and " +
                          "640ms stop/restart clock");
    }

    static void Check(bool value, string reason)
    {
        if (!value)
            throw new Exception(reason);
    }
}
