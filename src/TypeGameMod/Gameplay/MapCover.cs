using System;
using System.IO;
using System.IO.Compression;

namespace TypeGameMod
{
    // One bit per original background pixel.
    internal sealed class MapCover
    {
        internal const int Width = 1600, Height = 1200;
        internal static readonly MapCover City = Load("MapCover");
        readonly byte[] pixels;
        internal MapCover(byte[] pixels)
        {
            if (pixels.Length != Width * Height / 8)
                throw new ArgumentException("Cover must match the original 1600 x 1200 map.", "pixels");
            this.pixels = pixels;
        }

        internal static MapCover Load(string name)
        {
            var assembly = typeof(MapCover).Assembly;
            using (Stream resource = assembly.GetManifestResourceStream("TypeGameWASD." + name))
            {
                using (var compressed = new GZipStream(resource, CompressionMode.Decompress))
                {
                    using (var decoded = new MemoryStream())
                    {
                        compressed.CopyTo(decoded);
                        return new MapCover(decoded.ToArray());
                    }
                }
            }
        }

        internal byte[] CopyPixels()
        {
            return (byte[])pixels.Clone();
        }

        internal bool Solid(int x, int y)
        {
            if (x < 0 || y < 0 || x >= Width || y >= Height)
                return false;
            int bit = y * Width + x;
            return (pixels[bit / 8] & (0x80 >> (bit & 7))) != 0;
        }
        // Traverse every grid cell touched by the swept segment. The returned
        // fraction shares the same clock as Ballistics.Contact, so the nearer
        // of the obstacle and the moving target wins, even at maximum speed.
        internal bool Contact(double x0, double y0, double x1, double y1, out double fraction)
        {
            fraction = 0;
            int x = (int)Math.Floor(x0), y = (int)Math.Floor(y0);
            if (Solid(x, y))
                return true;
            double dx = x1 - x0, dy = y1 - y0;
            int stepX = Math.Sign(dx), stepY = Math.Sign(dy);
            double strideX = dx == 0 ? double.PositiveInfinity : 1 / Math.Abs(dx);
            double strideY = dy == 0 ? double.PositiveInfinity : 1 / Math.Abs(dy);
            double nextX = dx == 0 ? double.PositiveInfinity : (x + (stepX > 0 ? 1 : 0) - x0) / dx;
            double nextY = dy == 0 ? double.PositiveInfinity : (y + (stepY > 0 ? 1 : 0) - y0) / dy;
            while (Math.Min(nextX, nextY) <= 1)
            {
                fraction = Math.Min(nextX, nextY);
                if (nextX == nextY)
                {
                    // A corner touch must not slip between two adjacent walls.
                    if (Solid(x + stepX, y) || Solid(x, y + stepY))
                        return true;
                    x += stepX;
                    y += stepY;
                    nextX += strideX;
                    nextY += strideY;
                }
                else if (nextX < nextY)
                {
                    x += stepX;
                    nextX += strideX;
                }
                else
                {
                    y += stepY;
                    nextY += strideY;
                }
                if (Solid(x, y))
                    return true;
            }
            fraction = double.PositiveInfinity;
            return false;
        }
    }
}
