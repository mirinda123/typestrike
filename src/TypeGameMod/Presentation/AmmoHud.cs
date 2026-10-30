using System;
using System.Collections.Generic;

namespace TypeGameMod
{
    internal static class AmmoHud
    {
        const int AmmoX = 724, AmmoY = 17;
        static readonly string[] AmmoDigits = { "111101101101111", "010110010010111", "111001111100111",
                                                "111001111001111", "101101111001001", "111100111001111",
                                                "111100111101111", "111001001001001", "111101111101111",
                                                "111101111001111" };
        static void AmmoRect(Dictionary<int, uint> pixels, int x, int y, int w, int h, uint colour)
        {
            for (int yy = y; yy < y + h; yy++)
                for (int xx = x; xx < x + w; xx++)
                    pixels[(yy << 10) | xx] = colour;
        }

        static void AmmoText(Dictionary<int, uint> pixels, string text, int x, int y, int scale, uint colour)
        {
            var ink = new Dictionary<int, uint>();
            foreach (char c in text)
            {
                string glyph = c >= '0' && c <= '9' ? AmmoDigits[c - '0']
                               : c == 'R'           ? "110101110101101"
                               : c == '/'           ? "001001010100100"
                                                    : "000000111000000";
                for (int yy = 0; yy < 5; yy++)
                    for (int xx = 0; xx < 3; xx++)
                        if (glyph[yy * 3 + xx] == '1')
                            AmmoRect(ink, x + xx * scale, y + yy * scale, scale, scale, colour);
                x += 4 * scale;
            }
            foreach (int key in ink.Keys)
                for (int dy = -1; dy <= 1; dy++)
                    for (int dx = -1; dx <= 1; dx++)
                        pixels[key + dy * 1024 + dx] = 0x001b1720u;
            foreach (var pixel in ink)
                pixels[pixel.Key] = pixel.Value;
        }

        static void AmmoState(byte[] bank, int offset, int stride, Dictionary<int, uint> pixels)
        {
            var spans = new List<int[]>();
            var previous = new Dictionary<long, int>();
            for (int y = 12; y < 61; y++)
            {
                var next = new Dictionary<long, int>();
                for (int x = 720; x < 792;)
                {
                    uint colour;
                    if (!pixels.TryGetValue((y << 10) | x, out colour))
                    {
                        x++;
                        continue;
                    }
                    int end = x + 1;
                    uint value;
                    while (end < 792 && pixels.TryGetValue((y << 10) | end, out value) && value == colour)
                        end++;
                    long key = ((long)colour << 20) | ((long)x << 10) | (uint)end;
                    int index;
                    if (previous.TryGetValue(key, out index))
                        spans[index][3] = y + 1;
                    else
                    {
                        index = spans.Count;
                        spans.Add(new[] { x, y, end, y + 1, unchecked((int)colour) });
                    }
                    next.Add(key, index);
                    x = end;
                }
                previous = next;
            }
            if (4 + 20 * spans.Count > stride)
                throw new InvalidOperationException("Ammo HUD spans exceed bank");
            BinaryFields.Write(bank, offset, (uint)spans.Count);
            offset += 4;
            foreach (var span in spans)
                foreach (int v in span)
                {
                    BinaryFields.Write(bank, offset, unchecked((uint)v));
                    offset += 4;
                }
        }

        internal static byte[] Build()
        {
            var bank = new byte[62 * 4096];
            for (int weapon = 1; weapon <= 2; weapon++)
                for (int count = 0; count <= 30; count++)
                {
                    var px = new Dictionary<int, uint>();
                    int shown = Math.Min(count, WeaponAmmo.Capacity(weapon));
                    AmmoText(px, shown.ToString("00"), AmmoX, AmmoY, 3,
                             shown == 0 ? 0x005a6be5u : 0x00d0e9f5u);
                    AmmoState(bank, ((weapon - 1) * 31 + count) * 4096, 4096, px);
                }
            return bank;
        }

        internal static byte[] BuildReload()
        {
            var bank = new byte[65 * 1024];
            for (int step = 1; step <= 64; step++)
            {
                var px = new Dictionary<int, uint>();
                AmmoRect(px, AmmoX, AmmoY + 32, 62, 6, 0x001b1720u);
                AmmoRect(px, AmmoX + 1, AmmoY + 33, 60, 4, 0x00474d58u);
                AmmoRect(px, AmmoX + 1, AmmoY + 33, Math.Max(1, 60 * step / 64), 4, 0x007dc9e8u);
                AmmoState(bank, step * 1024, 1024, px);
            }
            return bank;
        }

        internal static byte[] BuildReserve()
        {
            var bank = new byte[91 * 4096];
            for (int count = 0; count <= 90; count++)
            {
                var pixels = new Dictionary<int, uint>();
                AmmoText(pixels, "/" + count.ToString("00"), AmmoX + 27, AmmoY + 4, 2, 0x00b8c9d2u);
                AmmoState(bank, count * 4096, 4096, pixels);
            }
            return bank;
        }
    }
}
