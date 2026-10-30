using System;
using System.IO;

namespace TypeGameMod
{
    internal sealed class DuelHealth
    {
        internal const int Maximum = 3;
        internal const double Grace = .5;
        internal readonly int[] Hearts = { Maximum, Maximum };
        readonly double[] protectedUntil = { -100, -100 };
        internal void Reset()
        {
            for (int i = 0; i < 2; i++)
            {
                Hearts[i] = Maximum;
                protectedUntil[i] = -100;
            }
        }

        internal bool Hit(int role, double now)
        {
            if (role < 0 || role > 1 || Hearts[role] <= 0 || now < protectedUntil[role])
                return false;
            Hearts[role]--;
            protectedUntil[role] = Hearts[role] > 0 ? now + Grace : -100;
            return true;
        }

        internal int Remaining(int role, double now)
        {
            return Hearts[role] > 0
                       ? (int)Math.Max(
                             0, Math.Min(500, Math.Ceiling((protectedUntil[role] - now) * 1000 - .000001)))
                       : 0;
        }

        internal void CopyFrom(DuelHealth other)
        {
            for (int i = 0; i < 2; i++)
            {
                Hearts[i] = other.Hearts[i];
                protectedUntil[i] = other.protectedUntil[i];
            }
        }

        internal void Write(BinaryWriter writer, double now)
        {
            for (int i = 0; i < 2; i++)
            {
                writer.Write((byte)Hearts[i]);
                writer.Write((ushort)Remaining(i, now));
            }
        }

        internal static DuelHealth Read(BinaryReader reader, double now)
        {
            var health = new DuelHealth();
            for (int i = 0; i < 2; i++)
            {
                int hearts = reader.ReadByte(), graceMilliseconds = reader.ReadUInt16();
                if (hearts > Maximum || graceMilliseconds > 500 || (hearts == 0 && graceMilliseconds != 0))
                    throw new FormatException("Invalid health state");
                health.Hearts[i] = hearts;
                health.protectedUntil[i] = graceMilliseconds == 0 ? -100 : now + graceMilliseconds / 1000.0;
            }
            return health;
        }
    }
}
