using System;
using System.IO;
using System.IO.Compression;

namespace TypeGameMod
{
    // Blender frames are immutable. The worker publishes only a frame number;
    // the native HUD composites mask/color onto the existing back buffer.
    internal static class TypingPrompt
    {
        internal const int Width = 180, Height = 70, X = 48, Y = 440;
        internal const int Frames = 41, IntroFrames = 5, FramesPerSecond = 12;
        internal const int HeaderBytes = 4096;
        internal const int PixelBytes = Width * Height * 4;
        internal const int FrameBytes = PixelBytes * 2;
        internal const int BankBytes = HeaderBytes + Frames * FrameBytes;
        internal static byte[] Build()
        {
            using (
                Stream resource =
                    typeof(TypingPrompt)
                        .Assembly.GetManifestResourceStream(
                            "TypeGameWASD.TypingPrompt")) using (var gzip =
                                                                     new GZipStream(
                                                                         resource,
                                                                         CompressionMode
                                                                             .Decompress)) using (var output =
                                                                                                      new MemoryStream(
                                                                                                          BankBytes))
            {
                gzip.CopyTo(output);
                byte[] data = output.ToArray();
                if (data.Length != BankBytes || BitConverter.ToInt32(data, 0) != 40 ||
                    BitConverter.ToInt32(data, 4) != Width || BitConverter.ToInt32(data, 8) != -Height ||
                    BitConverter.ToUInt16(data, 14) != 32)
                    throw new InvalidDataException("打字艺术字资源格式不匹配。");
                return data;
            }
        }
    }
    internal sealed class TypingPromptAnimation
    {
        bool enabled;
        double elapsed;
        internal int Update(bool typing, double dt)
        {
            if (!typing)
            {
                enabled = false;
                elapsed = 0;
                return 0;
            }
            if (!enabled)
            {
                enabled = true;
                elapsed = 0;
            }
            else
                elapsed += dt;
            double intro = TypingPrompt.IntroFrames / (double)TypingPrompt.FramesPerSecond;
            double loop =
                (TypingPrompt.Frames - TypingPrompt.IntroFrames) / (double)TypingPrompt.FramesPerSecond;
            if (elapsed >= intro + loop)
                elapsed = intro + (elapsed - intro) % loop;
            int frame = (int)(elapsed * TypingPrompt.FramesPerSecond + 1e-9);
            return frame < TypingPrompt.IntroFrames
                       ? frame
                       : TypingPrompt.IntroFrames + (frame - TypingPrompt.IntroFrames) %
                                                        (TypingPrompt.Frames - TypingPrompt.IntroFrames);
        }
    }
}
