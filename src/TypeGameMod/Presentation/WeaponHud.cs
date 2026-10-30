using System;
using System.Drawing;
using System.IO;
using System.Collections.Generic;

namespace TypeGameMod
{
    internal static class WeaponHud
    {
        const int WeaponWidth = 64, WeaponHeight = 48, WeaponX = 648, WeaponY = 12;
        const int WeaponStateStride = 0x20000, WeaponMaxSpans = 6000;
        internal static readonly string[] Resources = { "TypeGameWASD.WeaponUnarmed",
                                                        "TypeGameWASD.WeaponAK47",
                                                        "TypeGameWASD.WeaponShotgun" };
        // Transparent pre-rendered PNGs. Merge equal-colour row runs vertically,
        // preserving the world in every transparent pixel without GDI allocations.
        internal static byte[] Build()
        {
            byte[] bank = new byte[3 * WeaponStateStride];
            for (int weapon = 0; weapon < 3; weapon++)
            {
                using (Stream stream =
                           typeof(WeaponHud).Assembly.GetManifestResourceStream(Resources[weapon]))
                {
                    if (stream == null)
                        throw new InvalidOperationException("武器图标缺失：" + Resources[weapon]);
                    using (var bitmap = new Bitmap(stream))
                    {
                        if (bitmap.Width != WeaponWidth || bitmap.Height != WeaponHeight)
                            throw new InvalidOperationException("武器图标尺寸不匹配。");
                        var spans = new List<int[]>();
                        var previous = new Dictionary<long, int>();
                        for (int y = 0; y < WeaponHeight; y++)
                        {
                            var next = new Dictionary<long, int>();
                            for (int x = 0; x < WeaponWidth;)
                            {
                                Color c = bitmap.GetPixel(x, y);
                                if (c.A < 128)
                                {
                                    x++;
                                    continue;
                                }
                                uint colour = (uint)(c.R | (c.G << 8) | (c.B << 16));
                                int end = x + 1;
                                while (end < WeaponWidth)
                                {
                                    Color n = bitmap.GetPixel(end, y);
                                    if (n.A < 128 || (uint)(n.R | (n.G << 8) | (n.B << 16)) != colour)
                                        break;
                                    end++;
                                }
                                long key = ((long)colour << 14) | ((long)x << 7) | (uint)end;
                                int index;
                                if (previous.TryGetValue(key, out index))
                                    spans[index][3] = WeaponY + y + 1;
                                else
                                {
                                    index = spans.Count;
                                    spans.Add(new[] { WeaponX + x, WeaponY + y, WeaponX + end,
                                                      WeaponY + y + 1, unchecked((int)colour) });
                                }
                                next.Add(key, index);
                                x = end;
                            }
                            previous = next;
                        }
                        if (spans.Count == 0 || spans.Count > WeaponMaxSpans)
                            throw new InvalidOperationException("武器图标绘制数据超出范围。");
                        int at = weapon * WeaponStateStride;
                        BinaryFields.Write(bank, at, (uint)spans.Count);
                        at += 4;
                        foreach (int[] span in spans)
                            foreach (int value in span)
                            {
                                BinaryFields.Write(bank, at, unchecked((uint)value));
                                at += 4;
                            }
                    }
                }
            }
            return bank;
        }
    }
}
