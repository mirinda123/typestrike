using System;

namespace TypeGameMod
{
    internal sealed class WeaponSelection
    {
        int remainder;
        internal int Remainder
        {
            get {
                return remainder;
            }
        }

        internal void Reset()
        {
            remainder = 0;
        }

        internal int Choose(int current, int digit, int delta, bool allowed)
        {
            if (current < 0 || current > 2)
                current = 0;
            if (!allowed)
            {
                Reset();
                return current;
            }
            if (digit == 1 || digit == 2)
            {
                Reset();
                return current == digit ? 0 : digit;
            }
            long total = (long)remainder + delta;
            int steps = (int)(total / 120);
            remainder = (int)(total % 120);
            int result = (current + steps % 3) % 3;
            return result < 0 ? result + 3 : result;
        }

        internal static bool RawDelta(byte[] raw, int copied, int header, out int delta)
        {
            delta = 0;
            if (raw == null || (header != 16 && header != 24) || copied < header + 24 ||
                copied > raw.Length || BitConverter.ToUInt32(raw, 0) != 0 ||
                BitConverter.ToUInt32(raw, 4) != (uint)copied)
                return false;
            if ((BitConverter.ToUInt16(raw, header + 4) & 0x400) == 0)
                return false;
            delta = BitConverter.ToInt16(raw, header + 6);
            return delta != 0;
        }
    }
}
