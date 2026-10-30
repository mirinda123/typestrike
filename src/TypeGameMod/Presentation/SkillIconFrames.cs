using System;
using System.Drawing;
using System.IO;

namespace TypeGameMod
{
    internal static class SkillIconFrames
    {
        internal const int SkillIconSize = 48, SkillIconBytes = 48 * 48 * 4, SkillIconSteps = 64;
        // The game's owning UI thread blits one prebuilt top-down BI_RGB frame.
        // Frame0 is bright; frameN dims N/64 of the 48 rows;64 is empty.
        // These are CPU bytes, so there are no device-dependent surfaces to retain.
        internal static byte[] Build(string resource)
        {
            using (Stream stream = typeof(ModSession).Assembly.GetManifestResourceStream(resource))
            {
                if (stream == null)
                    throw new InvalidOperationException("技能图标缺失：" + resource);
                using (var bitmap = new Bitmap(stream))
                {
                    if (bitmap.Width != 64 || bitmap.Height != 64)
                        throw new InvalidOperationException("技能图标尺寸不匹配。");
                    byte[] bright = new byte[SkillIconBytes], dim = new byte[SkillIconBytes];
                    for (int y = 0; y < SkillIconSize; y++)
                        for (int x = 0; x < SkillIconSize; x++)
                        {
                            Color c = bitmap.GetPixel((2 * x + 1) * bitmap.Width / (2 * SkillIconSize),
                                                      (2 * y + 1) * bitmap.Height / (2 * SkillIconSize));
                            int at = (y * SkillIconSize + x) * 4;
                            bright[at] = c.B;
                            bright[at + 1] = c.G;
                            bright[at + 2] = c.R;
                            int grey = Math.Max(3, ((c.R * 54 + c.G * 183 + c.B * 19) / 256) * 28 / 100);
                            dim[at] = dim[at + 1] = dim[at + 2] = (byte)grey;
                        }
                    byte[] bank = new byte[(SkillIconSteps + 1) * SkillIconBytes];
                    for (int frame = 0; frame <= SkillIconSteps; frame++)
                        for (int y = 0; y < SkillIconSize; y++)
                            Buffer.BlockCopy(y < frame * SkillIconSize / SkillIconSteps ? dim : bright,
                                             y * SkillIconSize * 4, bank,
                                             frame * SkillIconBytes + y * SkillIconSize * 4,
                                             SkillIconSize * 4);
                    return bank;
                }
            }
        }

        internal static byte[] BuildResupply()
        {
            byte[] countdown = Build("TypeGameWASD.ResupplyIcon");
            byte[] charge = new byte[5 * SkillIconBytes];
            for (int words = 0; words <= 4; words++)
            {
                int start = words * SkillIconBytes;
                Buffer.BlockCopy(countdown, (4 - words) * 16 * SkillIconBytes, charge, start, SkillIconBytes);
                // The E keycap remains; energy now has one shared six-cell meter.
                for (int y = 34; y < 45; y++)
                    for (int x = 34; x < 45; x++)
                        Pixel(charge, start, x, y, 0x131824);
                const string glyph = "111100110100111";
                for (int y = 0; y < 5; y++)
                    for (int x = 0; x < 3; x++)
                        if (glyph[y * 3 + x] == '1')
                            Pixel(charge, start, 38 + x, 37 + y, words == 4 ? 0xffefadu : 0xb0b0a4u);
            }
            return charge;
        }

        static void Pixel(byte[] bank, int frame, int x, int y, uint rgb)
        {
            int at = frame + (y * SkillIconSize + x) * 4;
            bank[at] = (byte)rgb;
            bank[at + 1] = (byte)(rgb >> 8);
            bank[at + 2] = (byte)(rgb >> 16);
        }
    }
}
