using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using TypeGameMod;

internal static class PoliceWeaponOriginsTests
{
    internal static void Run()
    {
        byte[] data = PoliceWeaponOrigins.Build();
        Check(data.Length == 128 &&
                  AssetLayout.PoliceWeaponOrigins + data.Length <= AssetLayout.PoliceRunFrames,
              "four offset blocks fit the existing CPU table padding");
        for (int weapon = 1; weapon <= 2; weapon++)
            for (int moving = 0; moving < 2; moving++)
                for (int heading = 0; heading < 8; heading++)
                {
                    int at = ((weapon - 1) * 2 + moving) * 32 + heading * 4;
                    int offset = BitConverter.ToInt32(data, at);
                    Check(offset == PoliceWeaponOrigins.Get(weapon, moving != 0, heading),
                          "local/LAN table stores each signed authored offset");
                    int frames = moving == 0 ? PoliceIdleAnimation.Frames : RunAnimation.Frames;
                    for (int frame = 0; frame < frames; frame++)
                    {
                        double x, y;
                        WeaponMuzzle.Offset(0, weapon, heading, moving != 0, frame, out x, out y);
                        Check(x - offset > 0 && x - offset < 92 && y > 0 && y < 99,
                              "all 528 world-local muzzles recover their packed native-cell point");
                    }
                }
        Console.WriteLine("PASS long police weapons: 32 signed drawing origins, 528 compensated muzzle " +
                          "points and unchanged allocation bounds");
    }

    internal static void Export(string folder)
    {
        Directory.CreateDirectory(folder);
        File.WriteAllBytes(Path.Combine(folder, "origins.bin"), PoliceWeaponOrigins.Build());
        var rows = new List<string>();
        for (int weapon = 1; weapon <= 2; weapon++)
            for (int moving = 0; moving < 2; moving++)
            {
                string name = "Police" + (weapon == 1 ? "Ak" : "Shotgun") + (moving == 0 ? "Idle" : "Run");
                for (int heading = 0; heading < 8; heading++)
                {
                    for (int bank = 0; bank < (moving == 0 ? 1 : 2); bank++)
                    {
                        string resource = name + heading + (moving == 0 ? "" : "_" + bank);
                        File.WriteAllBytes(Path.Combine(folder, resource + ".dib"),
                                           IdleAnimationTests.Source(resource));
                    }
                    for (int frame = 0; frame < (moving == 0 ? 9 : 24); frame++)
                    {
                        double x, y;
                        WeaponMuzzle.Offset(0, weapon, heading, moving != 0, frame, out x, out y);
                        rows.Add(String.Format(
                            CultureInfo.InvariantCulture, "{0},{1},{2},{3},{4:R},{5:R},{6}", weapon, moving,
                            heading, frame, x, y, PoliceWeaponOrigins.Get(weapon, moving != 0, heading)));
                    }
                }
            }
        File.WriteAllLines(Path.Combine(folder, "muzzles.csv"), rows.ToArray());
    }

    static void Check(bool value, string reason)
    {
        if (!value)
            throw new Exception(reason);
    }
}
