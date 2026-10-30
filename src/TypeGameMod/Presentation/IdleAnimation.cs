using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;

namespace TypeGameMod
{
    // Blender exports coordinates, not copied game artwork. Reconstruct once from
    // the owner's original bitmaps and the MOD's existing, accepted weapon art.
    internal static class IdleAnimation
    {
        internal const int Width = 92, Height = 99, Frames = 18;
        const int SheetWidth = 276, SheetHeight = 594, PixelOffset = 1064;
        static readonly string[] Roles = { "Police", "Thief" };
        static readonly string[] Equipment = { "Cardinal", "ArmedIdle", "ShotgunIdle", "ShieldIdle" };
        internal sealed class Sheet
        {
            internal int Role, Equipment, Heading;
            internal byte[] Pixels;
        }

        internal static List<Sheet> Build(Func<string, byte[]> modBitmap, Func<string, byte[]> gameBitmap)
        {
            var result = new List<Sheet>();
            var sources = new Dictionary<string, IndexedBitmap>();
            Func<string, bool, IndexedBitmap> source = (name, native) =>
            {
                IndexedBitmap image;
                if (!sources.TryGetValue(name, out image))
                {
                    image = new IndexedBitmap(native ? gameBitmap(name) : modBitmap(name));
                    sources.Add(name, image);
                }
                return image;
            };
            using (BinaryReader input = OpenMaps())
            {
                if (input.ReadUInt32() != 0x314c4449 || input.ReadInt32() != 1 || input.ReadInt32() != 48 ||
                    input.ReadInt32() != Frames || input.ReadInt32() != Width || input.ReadInt32() != Height)
                    throw new InvalidDataException("待机动画映射格式不匹配。");
                for (int item = 0; item < 48; item++)
                {
                    int role = input.ReadByte(), equipment = input.ReadByte(), heading = input.ReadByte(),
                        direction = input.ReadByte();
                    int ownCell = input.ReadUInt16(), cardinalCell = input.ReadUInt16();
                    if (role > 1 || equipment > (role == 0 ? 3 : 2) || heading > 7 ||
                        (direction != 255 && direction > 3) || ownCell > 17 || cardinalCell > 17)
                        throw new InvalidDataException("待机动画方向无效。");
                    // Authored AK views for both roles and police shotgun/shield views
                    // are complete sheets, not reconstructed image layers.
                    // Consume their old coordinate maps; use the authored native
                    // sheets directly so filtering and hand-painted seams survive.
                    if (equipment == 1 || (role == 0 && equipment >= 2))
                    {
                        int mapBytes = Frames * Width * Height * 4;
                        if (input.ReadBytes(mapBytes).Length != mapBytes)
                            throw new EndOfStreamException("待机动画映射不完整。");
                        string bank = role == 1        ? "ThiefAkIdle"
                                      : equipment == 3 ? "PoliceShieldIdle"
                                      : equipment == 2 ? "PoliceShotgunIdle"
                                                       : "PoliceAkIdle";
                        result.Add(new Sheet { Role = role, Equipment = equipment, Heading = heading,
                                               Pixels = modBitmap(bank + heading) });
                        continue;
                    }
                    string label = Roles[role];
                    IndexedBitmap own = source(label + Equipment[equipment], false);
                    IndexedBitmap cardinal = source(label + "Cardinal", false);
                    IndexedBitmap native =
                        direction == 255 ? null : source("POLICE_" + role + "_0_" + direction, true);
                    uint[] ownColors = own.Cell(ownCell, Width, Height),
                           baseColors = cardinal.Cell(cardinalCell, Width, Height);
                    int nativeWidth = role == 0 ? 61 : 95, nativeHeight = role == 0 ? 85 : 101;
                    uint[][] nativeFrames = new uint[role == 0 ? 9 : 11][];
                    if (native != null)
                        for (int f = 0; f < nativeFrames.Length; f++)
                            nativeFrames[f] = native.Cell(f, nativeWidth, nativeHeight);
                    int count = Frames * Width * Height;
                    var colors = new uint[count];
                    var originals = new HashSet<uint>();
                    var extras = new Dictionary<uint, int>();
                    for (int i = 0; i < count; i++)
                    {
                        uint code = input.ReadUInt32(), color = 0;
                        int pixel = (int)(code & 65535);
                        if ((code & 0x80000000u) != 0)
                        {
                            int frame = (int)((code >> 16) & 16383);
                            if (native == null || frame >= nativeFrames.Length ||
                                pixel >= nativeWidth * nativeHeight)
                                throw new InvalidDataException("待机动画原版像素索引无效。");
                            color = nativeFrames[frame][pixel];
                            originals.Add(color);
                        }
                        else if ((code & 0x40000000u) != 0)
                        {
                            if (pixel == 0 || pixel > baseColors.Length)
                                throw new InvalidDataException("待机动画身体像素索引无效。");
                            color = baseColors[pixel - 1];
                            originals.Add(color);
                        }
                        else if (code != 0)
                        {
                            if (pixel == 0 || pixel > ownColors.Length)
                                throw new InvalidDataException("待机动画武器像素索引无效。");
                            color = ownColors[pixel - 1];
                            int frequency;
                            extras.TryGetValue(color, out frequency);
                            extras[color] = frequency + 1;
                        }
                        colors[i] = color;
                    }
                    result.Add(new Sheet { Role = role, Equipment = equipment, Heading = heading,
                                           Pixels = Encode(colors, originals, extras) });
                }
                if (input.Read() != -1)
                    throw new InvalidDataException("待机动画映射包含多余数据。");
            }
            return result;
        }

        static BinaryReader OpenMaps()
        {
            Stream resource =
                typeof(IdleAnimation).Assembly.GetManifestResourceStream("TypeGameWASD.IdleMaps");
            // BinaryReader owns gzip, which also owns the embedded resource stream.
            return new BinaryReader(new GZipStream(resource, CompressionMode.Decompress));
        }

        static byte[] Encode(uint[] colors, HashSet<uint> originals, Dictionary<uint, int> extras)
        {
            originals.Remove(0);
            if (originals.Count > 255)
                throw new InvalidDataException("原版待机动画颜色超过索引位图容量。");
            var palette = new List<uint> { 0 };
            var preserved = new List<uint>(originals);
            preserved.Sort();
            palette.AddRange(preserved);
            var candidates = new List<uint>(extras.Keys);
            candidates.Sort((a, b) =>
                            {
                                int frequency = extras[b].CompareTo(extras[a]);
                                return frequency != 0 ? frequency : a.CompareTo(b);
                            });
            foreach (uint color in candidates)
                if (palette.Count < 256 && !palette.Contains(color))
                    palette.Add(color);
            var indices = new Dictionary<uint, byte>();
            for (int i = 0; i < palette.Count; i++)
                indices.Add(palette[i], (byte)i);
            var data = new byte[GameIdentity.SpriteBytes];
            BinaryFields.Write(data, 0, 40u);
            BinaryFields.Write(data, 4, (uint)SheetWidth);
            BinaryFields.Write(data, 8, (uint)SheetHeight);
            data[12] = 1;
            data[14] = 8;
            BinaryFields.Write(data, 20, (uint)(SheetWidth * SheetHeight));
            BinaryFields.Write(data, 32, 256u);
            for (int i = 0; i < palette.Count; i++)
                BinaryFields.Write(data, 40 + i * 4, palette[i]);
            for (int f = 0, i = 0; f < Frames; f++)
                for (int y = 0; y < Height; y++)
                    for (int x = 0; x < Width; x++, i++)
                    {
                        uint color = colors[i];
                        byte index;
                        if (!indices.TryGetValue(color, out index))
                        {
                            int best = Int32.MaxValue;
                            for (int p = 1; p < palette.Count; p++)
                            {
                                int dr = (int)((color >> 16) & 255) - (int)((palette[p] >> 16) & 255);
                                int dg = (int)((color >> 8) & 255) - (int)((palette[p] >> 8) & 255);
                                int db = (int)(color & 255) - (int)(palette[p] & 255);
                                int distance = dr * dr + dg * dg + db * db;
                                if (distance < best)
                                {
                                    best = distance;
                                    index = (byte)p;
                                }
                            }
                            indices.Add(color, index);
                        }
                        data[At(f, x, y)] = index;
                    }
            return data;
        }

        internal static int At(int cell, int x, int y)
        {
            return PixelOffset + (SheetHeight - 1 - (cell / 3 * Height + y)) * SheetWidth + cell % 3 * Width +
                   x;
        }
        sealed class IndexedBitmap
        {
            readonly byte[] data;
            readonly uint[] palette;
            readonly int width, height, stride, pixels;
            internal IndexedBitmap(byte[] bitmap)
            {
                data = bitmap;
                width = BitConverter.ToInt32(data, 4);
                height = BitConverter.ToInt32(data, 8);
                int count = (int)BitConverter.ToUInt32(data, 32);
                if (count == 0)
                    count = 256;
                stride = (width + 3) & ~3;
                pixels = 40 + count * 4;
                if (BitConverter.ToInt32(data, 0) != 40 || BitConverter.ToUInt16(data, 14) != 8 ||
                    BitConverter.ToInt32(data, 16) != 0 || width <= 0 || height <= 0 || count > 256 ||
                    data.Length < pixels + stride * height)
                    throw new InvalidDataException("待机动画需要未压缩的 8 位原版位图。");
                palette = new uint[count];
                for (int i = 0; i < count; i++)
                    palette[i] = BitConverter.ToUInt32(data, 40 + i * 4) & 0xffffff;
            }
            internal uint[] Cell(int cell, int cellWidth, int cellHeight)
            {
                int columns = width / cellWidth;
                if (columns == 0 || (cell / columns + 1) * cellHeight > height)
                    throw new InvalidDataException("待机动画位图缺少所需帧。");
                var colors = new uint[cellWidth * cellHeight];
                for (int y = 0; y < cellHeight; y++)
                    for (int x = 0; x < cellWidth; x++)
                        colors[y * cellWidth + x] =
                            palette[data[pixels + (height - 1 - (cell / columns * cellHeight + y)) * stride +
                                         cell % columns * cellWidth + x]];
                return colors;
            }
        }
    }
}
