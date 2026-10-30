using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using TypeGameMod;

internal static class TypingPromptTests
{
    static int checks;
    static void Check(bool condition, string reason)
    {
        checks++;
        if (!condition)
            throw new Exception(reason);
    }

    [DllImport("gdi32.dll")]
    static extern int StretchDIBits(IntPtr dc, int x, int y, int width, int height, int sourceX, int sourceY,
                                    int sourceWidth, int sourceHeight, IntPtr pixels, IntPtr info, uint usage,
                                    uint operation);

    static Bitmap Draw(byte[] bank, int frame)
    {
        var bitmap = new Bitmap(800, 600, PixelFormat.Format32bppRgb);
        using (Graphics graphics = Graphics.FromImage(bitmap))
        {
            graphics.Clear(Color.FromArgb(42, 64, 87));
            IntPtr dc = graphics.GetHdc();
            GCHandle pin = GCHandle.Alloc(bank, GCHandleType.Pinned);
            try
            {
                IntPtr info = pin.AddrOfPinnedObject();
                int at = TypingPrompt.HeaderBytes + frame * TypingPrompt.FrameBytes;
                int masked = StretchDIBits(dc, TypingPrompt.X, TypingPrompt.Y, TypingPrompt.Width,
                                           TypingPrompt.Height, 0, 0, TypingPrompt.Width, TypingPrompt.Height,
                                           IntPtr.Add(info, at), info, 0, 0x008800c6);
                int painted =
                    StretchDIBits(dc, TypingPrompt.X, TypingPrompt.Y, TypingPrompt.Width, TypingPrompt.Height,
                                  0, 0, TypingPrompt.Width, TypingPrompt.Height,
                                  IntPtr.Add(info, at + TypingPrompt.PixelBytes), info, 0, 0x00ee0086);
                Check(Math.Abs(masked) == TypingPrompt.Height && Math.Abs(painted) == TypingPrompt.Height,
                      "both real GDI bitmap passes copied the complete frame: " + masked + "/" + painted);
            }
            finally
            {
                pin.Free();
                graphics.ReleaseHdc(dc);
            }
        }
        return bitmap;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct Rgb565Info
    {
        internal uint Size;
        internal int Width, Height;
        internal ushort Planes, BitCount;
        internal uint Compression, ImageBytes;
        internal int XResolution, YResolution;
        internal uint Colors, ImportantColors, RedMask, GreenMask, BlueMask;
    }

    [DllImport("gdi32.dll")]
    static extern IntPtr CreateCompatibleDC(IntPtr dc);
    [DllImport("gdi32.dll")]
    static extern IntPtr CreateDIBSection(IntPtr dc, ref Rgb565Info info, uint usage, out IntPtr pixels,
                                          IntPtr section, uint offset);
    [DllImport("gdi32.dll")]
    static extern IntPtr SelectObject(IntPtr dc, IntPtr item);
    [DllImport("gdi32.dll")]
    static extern bool DeleteObject(IntPtr item);
    [DllImport("gdi32.dll")]
    static extern bool DeleteDC(IntPtr dc);
    [DllImport("gdi32.dll")]
    static extern bool GdiFlush();

    static void CheckRgb565(byte[] bank)
    {
        var info = new Rgb565Info { Size = 40,
                                    Width = 800,
                                    Height = -600,
                                    Planes = 1,
                                    BitCount = 16,
                                    Compression = 3,
                                    ImageBytes = 800 * 600 * 2,
                                    RedMask = 0xf800,
                                    GreenMask = 0x07e0,
                                    BlueMask = 0x001f };
        IntPtr dc = CreateCompatibleDC(IntPtr.Zero), pixels;
        IntPtr bitmap = CreateDIBSection(dc, ref info, 0, out pixels, IntPtr.Zero, 0);
        Check(dc != IntPtr.Zero && bitmap != IntPtr.Zero, "RGB565 memory surface is available");
        IntPtr previous = SelectObject(dc, bitmap);
        GCHandle pin = GCHandle.Alloc(bank, GCHandleType.Pinned);
        try
        {
            byte[] background = new byte[800 * 600 * 2];
            int value = ((42 >> 3) << 11) | ((64 >> 2) << 5) | (87 >> 3);
            for (int i = 0; i < background.Length; i += 2)
            {
                background[i] = (byte)value;
                background[i + 1] = (byte)(value >> 8);
            }
            byte[] reference = new byte[background.Length], actual = new byte[background.Length];
            IntPtr header = pin.AddrOfPinnedObject();
            for (int frame = 0; frame < TypingPrompt.Frames; frame++)
            {
                int at = TypingPrompt.HeaderBytes + frame * TypingPrompt.FrameBytes;
                IntPtr mask = IntPtr.Add(header, at), color = IntPtr.Add(mask, TypingPrompt.PixelBytes);
                // Let Windows produce the reference RGB565 conversion. Then
                // check the two raster operations independently of that conversion.
                StretchDIBits(dc, TypingPrompt.X, TypingPrompt.Y, TypingPrompt.Width, TypingPrompt.Height, 0,
                              0, TypingPrompt.Width, TypingPrompt.Height, color, header, 0, 0x00cc0020);
                GdiFlush();
                Marshal.Copy(pixels, reference, 0, reference.Length);
                Marshal.Copy(background, 0, pixels, background.Length);
                StretchDIBits(dc, TypingPrompt.X, TypingPrompt.Y, TypingPrompt.Width, TypingPrompt.Height, 0,
                              0, TypingPrompt.Width, TypingPrompt.Height, mask, header, 0, 0x008800c6);
                StretchDIBits(dc, TypingPrompt.X, TypingPrompt.Y, TypingPrompt.Width, TypingPrompt.Height, 0,
                              0, TypingPrompt.Width, TypingPrompt.Height, color, header, 0, 0x00ee0086);
                GdiFlush();
                Marshal.Copy(pixels, actual, 0, actual.Length);
                bool same = true;
                for (int y = 0; y < TypingPrompt.Height; y++)
                    for (int x = 0; x < TypingPrompt.Width; x++)
                    {
                        int target = ((TypingPrompt.Y + y) * 800 + TypingPrompt.X + x) * 2;
                        bool transparent = bank[at + (y * TypingPrompt.Width + x) * 4] == 255;
                        byte[] expected = transparent ? background : reference;
                        same &=
                            actual[target] == expected[target] && actual[target + 1] == expected[target + 1];
                    }
                Check(same,
                      "actual RGB565 GDI preserves the background and Windows color conversion for frame " +
                          frame);
            }
        }
        finally
        {
            pin.Free();
            SelectObject(dc, previous);
            DeleteObject(bitmap);
            DeleteDC(dc);
        }
    }

    internal static void Run()
    {
        var clock = new TypingPromptAnimation();
        Check(clock.Update(false, .1) == 0, "movement resets the hidden prompt");
        Check(clock.Update(true, .05) == 0, "entering typing starts the intro at its first frame");
        for (int i = 1; i < TypingPrompt.Frames; i++)
            Check(clock.Update(true, 1.0 / 12) == i, "intro then every authored loop pose in order");
        Check(clock.Update(true, 1.0 / 12) == TypingPrompt.IntroFrames,
              "loop wraps to its first pose and never replays the entry");
        for (int i = 0; i < 12; i++)
            Check(clock.Update(true, 0) == TypingPrompt.IntroFrames, "unfocused time pauses the animation");
        clock.Update(false, 0);
        Check(clock.Update(true, .04) == 0, "Tab exit and reentry replay the intro");
        clock.Update(false, 0);
        double elapsed = 0;
        clock.Update(true, 0);
        for (int i = 0; i < 2000; i++)
        {
            double dt = (i % 7 + 1) * .003;
            elapsed += dt;
            int tick = (int)(elapsed * 12 + 1e-7);
            int expected = tick < 5 ? tick : 5 + (tick - 5) % 36;
            Check(clock.Update(true, dt) == expected, "poll cadence does not alter the animation speed");
        }
        clock.Update(false, 0);
        Check(clock.Update(true, 0) == 0, "leaving the round resets the prompt");
        clock.Update(true, 10);
        Check(clock.Update(true, 3600) >= 5 && clock.Update(true, 0) < 41,
              "long typing sessions remain in the finite loop");

        byte[] bank = TypingPrompt.Build();
        Check(bank.Length == TypingPrompt.BankBytes &&
                  AssetLayout.TypingArt + bank.Length <= AssetLayout.AllocationBytes,
              "the full authored bank fits the remote allocation");
        Check(Control.TypingArtDraws + 4 <= 1024 &&
                  SnapshotLayout.TypingArtFrame + 4 <= SnapshotLayout.AudioTelemetry,
              "new local fields fit the existing control and coherent snapshot");
        Check(TypingPrompt.Y + TypingPrompt.Height <= 512 && TypingPrompt.X + TypingPrompt.Width < 696,
              "the title stays above the article and clear of the charge cells");

        for (int frame = 0; frame < TypingPrompt.Frames; frame++)
            using (Bitmap bitmap = Draw(bank, frame))
            {
                var region =
                    new Rectangle(TypingPrompt.X, TypingPrompt.Y, TypingPrompt.Width, TypingPrompt.Height);
                BitmapData bits = bitmap.LockBits(region, ImageLockMode.ReadOnly, PixelFormat.Format32bppRgb);
                byte[] row = new byte[TypingPrompt.Width * 4];
                bool same = true;
                try
                {
                    for (int y = 0; y < TypingPrompt.Height; y++)
                    {
                        Marshal.Copy(IntPtr.Add(bits.Scan0, y * bits.Stride), row, 0, row.Length);
                        for (int x = 0; x < TypingPrompt.Width; x++)
                        {
                            int at = TypingPrompt.HeaderBytes + frame * TypingPrompt.FrameBytes +
                                     (y * TypingPrompt.Width + x) * 4;
                            bool transparent = bank[at] == 255;
                            same &=
                                row[x * 4] == (transparent ? 87 : bank[at + TypingPrompt.PixelBytes]) &&
                                row[x * 4 + 1] ==
                                    (transparent ? 64 : bank[at + TypingPrompt.PixelBytes + 1]) &&
                                row[x * 4 + 2] == (transparent ? 42 : bank[at + TypingPrompt.PixelBytes + 2]);
                        }
                    }
                }
                finally
                {
                    bitmap.UnlockBits(bits);
                }
                Check(same, "real memory-DC GDI preserves transparent background and exact " +
                                "color/orientation for frame " + frame);
                Check(bitmap.GetPixel(TypingPrompt.X, 512).ToArgb() == Color.FromArgb(42, 64, 87).ToArgb(),
                      "no title pixels overwrite the article");
            }

        CheckRgb565(bank);
        EnergyHudTests.Run();
        Console.WriteLine(
            "PASS " + checks +
            " gilded typing timing, memory-DC GDI transparency and article preservation checks");
    }

    internal static void Export(string folder)
    {
        Directory.CreateDirectory(folder);
        byte[] bank = TypingPrompt.Build();
        File.WriteAllBytes(Path.Combine(folder, "typing-bank.bin"), bank);
        File.WriteAllBytes(Path.Combine(folder, "energy-bank.bin"), EnergyHud.Build());
        var region = new Rectangle(TypingPrompt.X, TypingPrompt.Y, TypingPrompt.Width, TypingPrompt.Height);
        for (int frame = 0; frame < TypingPrompt.Frames; frame++)
            using (Bitmap bitmap = Draw(bank, frame)) using (
                Bitmap crop = bitmap.Clone(region, PixelFormat.Format32bppRgb))
                crop.Save(Path.Combine(folder, "frame-" + frame.ToString("D3") + ".png"), ImageFormat.Png);
    }
}
