using TypeGameMod;

// Combat fixtures choose a clear map explicitly; production always uses City.
internal static class TestMap
{
    internal static readonly MapCover Empty = new MapCover(new byte[MapCover.Width * MapCover.Height / 8]);
    internal static readonly MovementMap Movement = new MovementMap(Empty);

    internal static MapCover Rectangle(int left, int top, int right, int bottom)
    {
        var pixels = new byte[MapCover.Width * MapCover.Height / 8];
        for (int y = top; y <= bottom; y++)
            for (int x = left; x <= right; x++)
            {
                int bit = y * MapCover.Width + x;
                pixels[bit / 8] |= (byte)(0x80 >> (bit & 7));
            }
        return new MapCover(pixels);
    }
}
