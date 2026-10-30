using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using TypeGameMod;

internal static class IdleAnimationTests
{
    static int checks;
    static void Check(bool yes, string reason)
    {
        checks++;
        if (!yes)
            throw new Exception(reason);
    }
    internal static byte[] Source(string name)
    {
        using (Stream stream =
                   typeof(IdleAnimation).Assembly.GetManifestResourceStream("TypeGameWASD." + name))
        {
            using (var reader = new BinaryReader(stream)) return reader.ReadBytes((int)stream.Length);
        }
    }
    // An independent coloured RT_BITMAP fixture exercises native palette order,
    // zero-biClrUsed, padded scan lines, and both native animation dimensions.
    static byte[] NativeFixture(string name)
    {
        bool police = name.StartsWith("POLICE_0_", StringComparison.Ordinal);
        int width = police ? 183 : 285, height = police ? 255 : 404, stride = (width + 3) & ~3;
        var data = new byte[1064 + stride * height];
        BinaryFields.Write(data, 0, 40u);
        BinaryFields.Write(data, 4, (uint)width);
        BinaryFields.Write(data, 8, (uint)height);
        data[12] = 1;
        data[14] = 8;
        // Index zero is white, not transparent: transparency is RGB black.
        BinaryFields.Write(data, 40, 0xffffffu);
        BinaryFields.Write(data, 44, 0x13397bu);
        BinaryFields.Write(data, 48, 0x4c792du);
        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
                data[1064 + (height - 1 - y) * stride + x] = (byte)((x + y) % 3);
        return data;
    }
    static uint Color(byte[] dib, int frame, int x, int y)
    {
        return BitConverter.ToUInt32(dib, 40 + dib[IdleAnimation.At(frame, x, y)] * 4) & 0xffffff;
    }
    static void CheckEastAkSeam(IdleAnimation.Sheet sheet)
    {
        // In 1.14.0 four east-facing AK frames had a transparent row across
        // the waist. Check actual decoded pixels, independent of the rig.
        const int width = 92, height = 80;
        for (int f = 0; f < IdleAnimation.Frames; f++)
        {
            var opaque = new bool[width * height];
            var seen = new bool[opaque.Length];
            var queue = new Queue<int>();
            int visible = 0, largest = 0;
            for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                    if (sheet.Pixels[IdleAnimation.At(f, x, y)] != 0)
                    {
                        opaque[y * width + x] = true;
                        visible++;
                    }
            for (int start = 0; start < opaque.Length; start++)
            {
                if (!opaque[start] || seen[start])
                    continue;
                queue.Enqueue(start);
                seen[start] = true;
                int count = 0;
                while (queue.Count != 0)
                {
                    int p = queue.Dequeue(), x = p % width, y = p / width;
                    count++;
                    int[] neighbours = { x > 0 ? p - 1 : -1, x + 1 < width ? p + 1 : -1,
                                         y > 0 ? p - width : -1, y + 1 < height ? p + width : -1 };
                    foreach (int next in neighbours)
                        if (next >= 0 && opaque[next] && !seen[next])
                        {
                            seen[next] = true;
                            queue.Enqueue(next);
                        }
                }
                largest = Math.Max(largest, count);
            }
            Check(largest > visible * 0.95, "east AK torso stays connected to head and legs");
        }
    }
    static void CheckCompleteNativeBodies()
    {
        // The accepted southeast samples remain unchanged. Other diagonals now
        // retain each complete native body instead of fitting a moving head to
        // an independently animated generated torso.
        using (var compressed = new MemoryStream(Source("IdleMaps"))) using (
            var gzip = new GZipStream(compressed,
                                      CompressionMode.Decompress)) using (var input = new BinaryReader(gzip))
        {
            input.ReadBytes(24);
            for (int item = 0; item < 48; item++)
            {
                int role = input.ReadByte(), equipment = input.ReadByte(), heading = input.ReadByte();
                input.ReadBytes(5);
                bool complete = equipment >= 1 && equipment <= 2 && (heading & 1) != 0 && heading != 3;
                int nativeWidth = role == 0 ? 61 : 95;
                for (int frame = 0; frame < 18; frame++)
                {
                    int lowerBody = 0, expected = role == 0 ? frame % 9 : frame * 11 / 18;
                    for (int pixel = 0; pixel < 92 * 99; pixel++)
                    {
                        uint code = input.ReadUInt32();
                        if (!complete || (code & 0x80000000u) == 0)
                            continue;
                        if (((code >> 16) & 16383) != expected)
                            throw new Exception("native body regions must use the same animation frame");
                        if ((code & 65535) / nativeWidth >= (role == 0 ? 60 : 70))
                            lowerBody++;
                    }
                    if (complete)
                        Check(lowerBody >= 180, "armed diagonals retain the complete native lower body");
                }
            }
        }
    }
    static void CheckSouthwestJoints()
    {
        // Review screenshots exposed a second jaw in the stock cutout and
        // transparent holes in the thief's bag strap. Inspect their actual
        // packed source pixels throughout both weapon loops.
        using (var compressed = new MemoryStream(Source("IdleMaps"))) using (
            var gzip = new GZipStream(compressed,
                                      CompressionMode.Decompress)) using (var input = new BinaryReader(gzip))
        {
            input.ReadBytes(24);
            for (int item = 0; item < 48; item++)
            {
                int role = input.ReadByte(), equipment = input.ReadByte(), heading = input.ReadByte();
                input.ReadBytes(5);
                bool reviewed = heading == 5 && equipment >= 1 && equipment <= 2;
                for (int frame = 0; frame < 18; frame++)
                {
                    bool secondFace = false;
                    int connected = 0;
                    var forearm = reviewed && role == 1 ? new byte[92 * 99] : null;
                    for (int pixel = 0; pixel < 92 * 99; pixel++)
                    {
                        uint code = input.ReadUInt32();
                        if (!reviewed)
                            continue;
                        if (role == 0 && code != 0 && code < 0x40000000u && (code - 1) / 92 < 42)
                            secondFace = true;
                        int x = pixel % 92, y = pixel / 92;
                        bool joint =
                            role == 0 ? (x == 48 && y == 43) || (x == 49 && y == 45)
                                      : (x == 50 && y == 52) || (x == 51 && y == 52) || (x == 52 && y == 53);
                        if (joint && (code & 0x80000000u) != 0)
                            connected++;
                        if (forearm != null && code != 0 && code < 0x40000000u)
                        {
                            int sx = (int)((code - 1) % 92), sy = (int)((code - 1) / 92);
                            if (sx >= 30 && sx <= 35 && sy >= 53 && sy <= 58)
                                forearm[pixel] = 1;
                            else if (sx >= 36 && sx <= 40 && sy >= 54 && sy <= 59)
                                forearm[pixel] = 2;
                        }
                    }
                    if (reviewed)
                    {
                        Check(!secondFace, "southwest stock cannot contain a second face or neck");
                        Check(connected == (role == 0 ? 2 : 3),
                              "southwest neck and strap retain their native connecting pixels");
                        if (forearm != null)
                        {
                            int edges = 0;
                            for (int y = 0; y < 99; y++)
                                for (int x = 0; x < 92; x++)
                                {
                                    int p = y * 92 + x;
                                    if (forearm[p] == 0)
                                        continue;
                                    if (x < 91 && forearm[p + 1] != 0 && forearm[p + 1] != forearm[p])
                                        edges++;
                                    if (y < 98 && forearm[p + 92] != 0 && forearm[p + 92] != forearm[p])
                                        edges++;
                                }
                            Check(edges >= 3, "thief palm directly joins its cuff through a broad seam");
                        }
                    }
                }
            }
        }
    }
    internal static void Run()
    {
        CheckCompleteNativeBodies();
        CheckSouthwestJoints();
        var before = new Dictionary<string, byte[]>();
        var snapshots = new Dictionary<string, string>();
        Func<string, byte[]> art = name =>
        {
            byte[] data = Source(name);
            if (!before.ContainsKey(name))
            {
                before.Add(name, data);
                snapshots.Add(name, Convert.ToBase64String(data));
            }
            return data;
        };
        List<IdleAnimation.Sheet> sheets = IdleAnimation.Build(art, NativeFixture);
        Check(sheets.Count == 48, "forty requested additions plus eight shield headings");
        var keys = new HashSet<string>();
        foreach (IdleAnimation.Sheet sheet in sheets)
        {
            string key = sheet.Role + ":" + sheet.Equipment + ":" + sheet.Heading;
            Check(keys.Add(key), "each role/equipment/heading has exactly one bank");
            Check(sheet.Pixels.Length == GameIdentity.SpriteBytes, "all frames fit the native borrowed DIB");
            Check(sheet.Equipment != 0 || (sheet.Heading & 1) == 0,
                  "native unarmed diagonals retain the original sequences");
            if (sheet.Role == 0 && sheet.Equipment >= 1 && sheet.Equipment <= 3)
            {
                string bank = sheet.Equipment == 3   ? "PoliceShieldIdle"
                              : sheet.Equipment == 1 ? "PoliceAkIdle"
                                                     : "PoliceShotgunIdle";
                Check(Convert.ToBase64String(sheet.Pixels) ==
                          Convert.ToBase64String(Source(bank + sheet.Heading)),
                      "approved police weapon pixels reach the runtime bank without reconstruction");
                if (sheet.Equipment == 1 && sheet.Heading == 2)
                    CheckEastAkSeam(sheet);
                continue;
            }
            if (sheet.Role == 1 && sheet.Equipment == 1)
            {
                Check(Convert.ToBase64String(sheet.Pixels) ==
                          Convert.ToBase64String(Source("ThiefAkIdle" + sheet.Heading)),
                      "reviewed thief AK sheet reaches its runtime bank without reconstruction");
                var idlePoses = new HashSet<string>();
                for (int f = 0; f < ThiefAkIdle.Frames; f++)
                {
                    var pose = new byte[92 * 99];
                    for (int y = 0; y < 99; y++)
                        for (int x = 0; x < 92; x++)
                            pose[y * 92 + x] = sheet.Pixels[IdleAnimation.At(f, x, y)];
                    idlePoses.Add(Convert.ToBase64String(pose));
                }
                Check(idlePoses.Count >= 5, "3D idle retains visible pose changes at native resolution");
                double restX, restY, raisedX, raisedY;
                WeaponMuzzle.Offset(1, GameRules.Ak47, sheet.Heading, false, 0, out restX, out restY);
                WeaponMuzzle.Offset(1, GameRules.Ak47, sheet.Heading, false, 3, out raisedX, out raisedY);
                Check(Math.Abs(restX - raisedX) + Math.Abs(restY - raisedY) > .5,
                      "muzzle follows the raised rifle, not the old fitted sprite");
            }
            var phases = new HashSet<string>();
            for (int f = 0; f < IdleAnimation.Frames; f++)
            {
                int visible = 0;
                var frame = new byte[92 * 99];
                for (int y = 0; y < 99; y++)
                    for (int x = 0; x < 92; x++)
                    {
                        byte pixel = sheet.Pixels[IdleAnimation.At(f, x, y)];
                        frame[y * 92 + x] = pixel;
                        if (pixel != 0)
                            visible++;
                    }
                Check(visible > 500, "every pose contains a complete character");
                phases.Add(Convert.ToBase64String(frame));
                // Newly authored cardinal feet and shadows stay exactly planted.
                if ((sheet.Heading & 1) == 0 && f > 0 && !(sheet.Role == 1 && sheet.Equipment == 1))
                    for (int y = sheet.Role == 0 ? 84 : 89; y < 99; y++)
                        for (int x = 0; x < 92; x++)
                            if (Color(sheet.Pixels, f, x, y) != Color(sheet.Pixels, 0, x, y))
                                throw new Exception("cardinal foot / ground shadow drifted");
            }
            Check(phases.Count >= 3, "each new idle plays a visible looping sequence");
        }
        for (int role = 0; role < 2; role++)
            for (int gear = 0; gear < 3; gear++)
                for (int h = 0; h < 8; h++)
                    Check(keys.Contains(role + ":" + gear + ":" + h) || (gear == 0 && (h & 1) != 0),
                          "all 48 requested combinations covered");
        foreach (var item in before)
            Check(Convert.ToBase64String(item.Value) == snapshots[item.Key],
                  "accepted source art remains byte-identical");
        // A nonzero original index-zero pixel survives as opaque white.
        bool white = false, nativeColor = false;
        foreach (var sheet in sheets)
            if ((sheet.Heading & 1) != 0)
                for (int y = 0; y < 42; y++)
                    for (int x = 0; x < 92; x++)
                    {
                        uint color = Color(sheet.Pixels, 0, x, y);
                        white |= color == 0xffffff;
                        nativeColor |= color == 0x13397b;
                    }
        Check(white && nativeColor, "original colours survive exact palette remapping, including index zero");
        Check(AssetLayout.IdleFrames + 48 * AssetLayout.IdleBankStride <= AssetLayout.AllocationBytes,
              "all CPU banks fit the reserved allocation");
        Console.WriteLine(
            "PASS " + checks +
            " idle coverage, southwest joints, native colour preservation, planted feet and DIB checks");
    }
    internal static void Export(string directory, string gameResources)
    {
        Directory.CreateDirectory(directory);
        using (var originals = new GameBitmapResources(
                   gameResources)) foreach (var sheet in IdleAnimation.Build(Source, originals.Read))
            File.WriteAllBytes(
                Path.Combine(directory, sheet.Role + "-" + sheet.Equipment + "-" + sheet.Heading + ".dib"),
                sheet.Pixels);
    }

    internal static void ExportPoliceShield(string directory)
    {
        Directory.CreateDirectory(directory);
        foreach (var sheet in IdleAnimation.Build(Source, NativeFixture))
            if (sheet.Role == 0 && sheet.Equipment == 3)
                File.WriteAllBytes(Path.Combine(directory, "PoliceShieldIdle" + sheet.Heading + ".dib"),
                                   sheet.Pixels);
    }
}
