using System;
using System.Collections.Generic;

namespace TypeGameMod
{
    // The native HUD blits one small rectangle list: six cells and the role's
    // count/time badge. Ten fill levels per cell keep shield use visibly gradual.
    internal static class EnergyHud
    {
        internal const int StateBytes = 4096, StatesPerRole = 61;
        internal const int ShineFrames = 20, ShineStateBytes = 128;
        internal const double ShineFrameSeconds = .03, ShineCycleSeconds = 1.8;
        static readonly string[] Digits = { "111101101101111", "010110010010111", "111001111100111",
                                            "111001111001111", "101101111001001", "111100111001111",
                                            "111100111101111", "111001001001001", "111101111101111",
                                            "111101111001111" };
        internal static int Step(double energy)
        {
            return energy <= 0 ? 0 : Math.Max(1, Math.Min(60, (int)Math.Floor(energy * 10 + 0.0000001)));
        }

        internal static void WriteSnapshot(byte[] snapshot, RoleSkill skill, WeaponAmmo ammo, int weapon,
                                           double now)
        {
            skill.Tick(now);
            bool supply = skill.SupplyReady && ammo.CanSupply(weapon);
            BinaryFields.Write(snapshot, SnapshotLayout.EnergySteps, (uint)Step(skill.Energy));
            BinaryFields.Write(snapshot, SnapshotLayout.EnergyMilliseconds, (uint)skill.Remaining(now));
            BinaryFields.Write(snapshot, SnapshotLayout.EnergyShineFrame, (uint)ShineFrame(skill, now));
            BinaryFields.Write(snapshot, SnapshotLayout.SupplyIconState, supply ? 4u : 0u);
            BinaryFields.Write(snapshot, SnapshotLayout.SupplyReady, supply ? 1u : 0u);
            BinaryFields.Write(snapshot, SnapshotLayout.SupplyCycle, skill.Cycle);
            BinaryFields.Write(snapshot, SnapshotLayout.SkillVisible, 1u);
            BinaryFields.Write(snapshot, SnapshotLayout.Shield, skill.Shield ? 1u : 0u);
            BinaryFields.Write(snapshot, SnapshotLayout.SkillReady, skill.Ready ? 1u : 0u);
            BinaryFields.Write(snapshot, SnapshotLayout.SkillCycle, skill.Cycle);
            BinaryFields.Write(snapshot, SnapshotLayout.SkillRole, (uint)skill.Role);
            BinaryFields.Write(snapshot, SnapshotLayout.SkillDarkRows, skill.Ready ? 0u : 64u);
        }
        // A short sweep followed by a pause signals usable stock without flashing
        // the empty cells. This clock is visual only and never goes over the network.
        internal static int ShineFrame(RoleSkill skill, double now)
        {
            if (!skill.Ready || skill.Shield || now < skill.NextBlink)
                return ShineFrames;
            return Math.Min(ShineFrames, (int)((now % ShineCycleSeconds) / ShineFrameSeconds));
        }

        internal static byte[] BuildShine()
        {
            var bank = new byte[StatesPerRole * ShineFrames * ShineStateBytes];
            uint[] colours = { 0x007deb98u, 0x00afffc4u, 0x00dcfff0u };
            int[] offsets = { -8, -4, 0 }, widths = { 4, 4, 2 };
            for (int step = 0; step < StatesPerRole; step++)
                for (int frame = 0; frame < ShineFrames; frame++)
                {
                    int state = (step * ShineFrames + frame) * ShineStateBytes, count = 0;
                    for (int cell = 0; cell < 6; cell++)
                    {
                        int rows = Math.Max(0, Math.Min(10, step - cell * 10));
                        if (rows == 0)
                            continue;
                        int left = 686 + cell * 18, beam = 678 + frame * 6;
                        for (int band = 0; band < colours.Length; band++)
                        {
                            int x0 = Math.Max(left, beam + offsets[band]);
                            int x1 = Math.Min(left + 11, beam + offsets[band] + widths[band]);
                            if (x0 >= x1)
                                continue;
                            int at = state + 4 + count++ * 20;
                            BinaryFields.Write(bank, at, (uint)x0);
                            BinaryFields.Write(bank, at + 4, (uint)(511 - rows));
                            BinaryFields.Write(bank, at + 8, (uint)x1);
                            BinaryFields.Write(bank, at + 12, 511u);
                            BinaryFields.Write(bank, at + 16, colours[band]);
                        }
                    }
                    BinaryFields.Write(bank, state, (uint)count);
                }
            return bank;
        }

        static void Rect(Dictionary<int, uint> pixels, int x, int y, int width, int height, uint colour)
        {
            for (int yy = y; yy < y + height; yy++)
                for (int xx = x; xx < x + width; xx++)
                    pixels[(yy << 10) | xx] = colour;
        }

        static void Draw(Dictionary<int, uint> pixels, int role, int step)
        {
            const uint outline = 0x001b1720u;
            for (int cell = 0; cell < 6; cell++)
            {
                int x = 684 + cell * 18, rows = Math.Max(0, Math.Min(10, step - cell * 10));
                Rect(pixels, x, 499, 15, 13, outline);
                Rect(pixels, x + 1, 500, 13, 11, 0x00334232u);
                Rect(pixels, x + 2, 501, 11, 10, 0x001a281bu);
                if (rows == 0)
                    continue;
                int top = 511 - rows;
                Rect(pixels, x + 2, top, 11, rows, 0x005de16eu);
                Rect(pixels, x + 2, top, 11, 1, 0x0095ffc0u);
                if (rows > 1)
                    Rect(pixels, x + 2, 510, 11, 1, 0x002b8a3bu);
            }
            string text = role == 0 ? (step / 10) + "." + (step % 10) + "s" : "×" + (step / 10);
            int left = 786 - text.Length * 8;
            Rect(pixels, left - 2, 480, text.Length * 8 + 2, 14, outline);
            uint colour = step > 0 ? 0x00d0f5deu : 0x007c877eu;
            foreach (char character in text)
            {
                string glyph = character >= '0' && character <= '9' ? Digits[character - '0']
                               : character == '.'                   ? "000000000000010"
                               : character == 's'                   ? "011100010001110"
                                                                    : "000101010101000";
                for (int y = 0; y < 5; y++)
                    for (int x = 0; x < 3; x++)
                        if (glyph[y * 3 + x] == '1')
                            Rect(pixels, left + x * 2, 482 + y * 2, 2, 2, colour);
                left += 8;
            }
        }

        static void WriteState(byte[] bank, int state, Dictionary<int, uint> pixels)
        {
            var spans = new List<int[]>();
            var previous = new Dictionary<long, int>();
            for (int y = 480; y < 512; y++)
            {
                var nextRow = new Dictionary<long, int>();
                for (int x = 684; x < 790;)
                {
                    uint colour;
                    if (!pixels.TryGetValue((y << 10) | x, out colour))
                    {
                        x++;
                        continue;
                    }
                    int end = x + 1;
                    uint next;
                    while (end < 790 && pixels.TryGetValue((y << 10) | end, out next) && next == colour)
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
                    nextRow.Add(key, index);
                    x = end;
                }
                previous = nextRow;
            }
            if (4 + spans.Count * 20 > StateBytes)
                throw new InvalidOperationException("Energy HUD spans exceed bank");
            int at = state * StateBytes;
            BinaryFields.Write(bank, at, (uint)spans.Count);
            at += 4;
            foreach (int[] span in spans)
                foreach (int value in span)
                {
                    BinaryFields.Write(bank, at, unchecked((uint)value));
                    at += 4;
                }
        }

        internal static byte[] Build()
        {
            var bank = new byte[2 * StatesPerRole * StateBytes];
            for (int role = 0; role < 2; role++)
                for (int step = 0; step < StatesPerRole; step++)
                {
                    var pixels = new Dictionary<int, uint>();
                    Draw(pixels, role, step);
                    WriteState(bank, role * StatesPerRole + step, pixels);
                }
            return bank;
        }
    }
}
