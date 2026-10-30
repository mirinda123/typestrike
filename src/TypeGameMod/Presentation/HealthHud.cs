using System;
using System.Collections.Generic;

namespace TypeGameMod
{
    internal static class HealthHud
    {
        // Original 9x9 pixel glyphs, rendered at 2x using opaque spans only.
        // Dot/zero cells are transparent: the world remains visible between pixels.
        static readonly string[] HeartGlyph = { ".111.111.", "123313331", "143333331",
                                                "133333331", ".1333331.", "..13331..",
                                                "...131...", "....1....", "........." };
        static readonly string[][] RoleGlyph = {
            new[] { "..11111..", ".1222221.", ".1234321.", "111111111", ".1222221.", "..15551..", "..15651..",
                    "...111...", "........." },
            new[] { "..11111..", ".1222221.", "122222221", "135656531", "133333331", ".1333331.", "..13331..",
                    "...111...", "........." }
        };
        static uint PixelColour(int role, bool heart, bool full, bool flash, char value)
        {
            if (value == '1')
                return 0x001a1115u;
            if (heart)
            {
                if (!full)
                    return value == '4' ? 0x00605560u : 0x00352d35u;
                if (flash)
                    return value == '2' ? 0x008a72f0u : 0x00d9d9ffu;
                return value == '2' ? 0x002219a0u : value == '4' ? 0x00b4b4ffu : 0x003c30e8u;
            }
            if (value == '5')
                return 0x007cb8dbu;
            if (value == '6')
                return 0x00261c22u;
            if (role == 0)
                return value == '2' ? 0x00934726u : value == '3' ? 0x00df9250u : 0x0047d9f4u;
            return value == '2' ? 0x003a704bu : value == '3' ? 0x007a315fu : 0x0070a376u;
        }

        static void GlyphSpans(List<int[]> spans, string[] glyph, int x, int y, int role, bool heart,
                               bool full, bool flash)
        {
            for (int row = 0; row < glyph.Length; row++)
                for (int col = 0; col < glyph[row].Length;)
                {
                    char value = glyph[row][col];
                    if (value == '.')
                    {
                        col++;
                        continue;
                    }
                    uint colour = PixelColour(role, heart, full, flash, value);
                    int end = col + 1;
                    while (end < glyph[row].Length && glyph[row][end] != '.' &&
                           PixelColour(role, heart, full, flash, glyph[row][end]) == colour)
                        end++;
                    spans.Add(new[] { x + col * 2, y + row * 2, x + end * 2, y + row * 2 + 2,
                                      unchecked((int)colour) });
                    col = end;
                }
        }

        internal static byte[] Build()
        {
            byte[] bank = new byte[16 * 4096];
            for (int role = 0; role < 2; role++)
                for (int hearts = 0; hearts <= 3; hearts++)
                    for (int flash = 0; flash < 2; flash++)
                    {
                        var spans = new List<int[]>();
                        int y = 12 + role * 26;
                        GlyphSpans(spans, RoleGlyph[role], 12, y, role, false, false, false);
                        for (int i = 0; i < 3; i++)
                            GlyphSpans(spans, HeartGlyph, 36 + i * 22, y, role, true, i < hearts, flash != 0);
                        if (spans.Count > 200)
                            throw new InvalidOperationException("Too many health HUD spans");
                        int at = (role * 8 + hearts * 2 + flash) * 4096;
                        BinaryFields.Write(bank, at, (uint)spans.Count);
                        at += 4;
                        foreach (int[] span in spans)
                            foreach (int n in span)
                            {
                                BinaryFields.Write(bank, at, unchecked((uint)n));
                                at += 4;
                            }
                    }
            return bank;
        }
    }
}
