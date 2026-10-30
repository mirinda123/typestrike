using TypeGameMod;
using System;
using System.IO;
using System.Drawing;

internal static class WeaponTests
{
    static int checks;
    static void Check(bool yes, string message)
    {
        checks++;
        if (!yes)
            throw new Exception(message);
    }
    static void Raw(int header, int delta, int flags, int type, bool expected)
    {
        byte[] data = new byte[header + 24];
        Buffer.BlockCopy(BitConverter.GetBytes(type), 0, data, 0, 4);
        Buffer.BlockCopy(BitConverter.GetBytes(data.Length), 0, data, 4, 4);
        Buffer.BlockCopy(BitConverter.GetBytes((ushort)flags), 0, data, header + 4, 2);
        Buffer.BlockCopy(BitConverter.GetBytes((short)delta), 0, data, header + 6, 2);
        int actual;
        Check(WeaponSelection.RawDelta(data, data.Length, header, out actual) == expected,
              "raw wheel validation");
        if (expected)
            Check(actual == delta, "signed raw wheel amount");
        Check(!WeaponSelection.RawDelta(data, data.Length - 1, header, out actual),
              "truncated raw packet rejected");
    }
    internal static void RunSuite(string[] args)
    {
        try
        {
            var s = new WeaponSelection();
            int weapon = 0;
            for (int i = 0; i < 30; i++)
            {
                weapon = s.Choose(weapon, 0, 120, true);
                Check(weapon == (i + 1) % 3, "up cycles fist/AK/shotgun");
            }
            for (int i = 0; i < 30; i++)
            {
                weapon = s.Choose(weapon, 0, -120, true);
                Check(weapon == (2 - i % 3 + 3) % 3, "down reverses");
            }
            Check(s.Choose(0, 0, 90, true) == 0 && s.Remainder == 90, "partial detent retained");
            Check(s.Choose(0, 0, 30, true) == 1 && s.Remainder == 0, "partial detents accumulate");
            Check(s.Choose(0, 0, 360, true) == 0, "three detents wrap");
            Check(s.Choose(0, 0, -240, true) == 1, "multiple reverse detents");
            Check(s.Choose(1, 0, 90, true) == 1, "partial before typing");
            Check(s.Choose(1, 0, 120, false) == 1 && s.Remainder == 0,
                  "disabled ignores and clears partial input");
            Check(s.Choose(1, 0, 30, true) == 1 && s.Remainder == 30, "typing wheel not deferred");
            Check(s.Choose(1, 1, 120, true) == 0 && s.Remainder == 0,
                  "same digit holsters and wins over wheel");
            Check(s.Choose(0, 2, 0, true) == 2, "digit equips shotgun");
            Check(s.Choose(2, 1, 0, true) == 1, "digit switches to AK");
            foreach (int d in new[] { int.MinValue, int.MaxValue, -32768, 32767 })
            {
                s.Reset();
                int n = s.Choose(0, 0, d, true);
                Check(n >= 0 && n <= 2 && Math.Abs(s.Remainder) < 120, "bounded large deltas");
            }
            foreach (int h in new[] { 16, 24 })
            {
                Raw(h, 120, 0x400, 0, true);
                Raw(h, -120, 0x401, 0, true);
                Raw(h, 30, 0x400, 0, true);
                Raw(h, 120, 0x800, 0, false);
                Raw(h, 120, 0x400, 1, false);
                Raw(h, 0, 0x400, 0, false);
            }
            int ignored;
            Check(!WeaponSelection.RawDelta(null, 0, 16, out ignored), "null raw packet rejected");
            byte[] bank = WeaponHud.Build();
            Check(bank.Length == 3 * 0x20000, "three bounded transparent art states");
            for (int state = 0; state < 3; state++)
            {
                using (Stream stream = typeof(ModSession)
                                           .Assembly.GetManifestResourceStream(
                                               WeaponHud.Resources[state])) using (var bitmap =
                                                                                       new Bitmap(stream))
                {
                    Check(bitmap.Width == 64 && bitmap.Height == 48, "native art dimensions");
                    int at = state * 0x20000, count = BitConverter.ToInt32(bank, at);
                    Check(count > 0 && count <= 6000, "icon spans bounded");
                    int[,] coverage = new int[48, 64];
                    for (int i = 0; i < count; i++)
                    {
                        int a = at + 4 + i * 20, x0 = BitConverter.ToInt32(bank, a),
                            y0 = BitConverter.ToInt32(bank, a + 4), x1 = BitConverter.ToInt32(bank, a + 8),
                            y1 = BitConverter.ToInt32(bank, a + 12);
                        uint colour = BitConverter.ToUInt32(bank, a + 16);
                        Check(x0 >= 648 && x1 <= 712 && y0 >= 12 && y1 <= 60 && x1 > x0 && y1 > y0,
                              "icon inside upper-right64x48 viewport");
                        for (int y = y0; y < y1; y++)
                            for (int x = x0; x < x1; x++)
                            {
                                Color c = bitmap.GetPixel(x - 648, y - 12);
                                Check(c.A == 255 && (uint)(c.R | (c.G << 8) | (c.B << 16)) == colour,
                                      "opaque spans match accepted PNG exactly");
                                coverage[y - 12, x - 648]++;
                            }
                    }
                    int opaque = 0;
                    for (int y = 0; y < 48; y++)
                        for (int x = 0; x < 64; x++)
                        {
                            Color c = bitmap.GetPixel(x, y);
                            Check(c.A == 0 || c.A == 255, "binary alpha");
                            Check(coverage[y, x] == (c.A == 255 ? 1 : 0),
                                  "transparent gaps untouched and opaque pixels exactly once");
                            if (c.A == 255)
                                opaque++;
                        }
                    Check(opaque > 250 && opaque < 2000,
                          "freeform art has substantial transparent background");
                    Check(bitmap.GetPixel(0, 0).A == 0 && bitmap.GetPixel(63, 0).A == 0 &&
                              bitmap.GetPixel(0, 47).A == 0 && bitmap.GetPixel(63, 47).A == 0,
                          "no rectangular box at corners");
                }
            }
            if (args.Length == 2 && args[0] == "--bank")
                File.WriteAllBytes(args[1], bank);
            Console.WriteLine("PASS " + checks +
                              (" weapon checks; wheel direction/partial deltas/context/digits, x86/x64 Raw " +
                               "Input, three64x48 freeform PNG icons"));
        }
        catch (Exception e)
        {
            Console.WriteLine("FAIL after " + checks + " checks: " + e.Message);
            Environment.ExitCode = 1;
        }
    }
}
