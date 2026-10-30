using System;

namespace TypeGameMod
{
    internal static class BinaryFields
    {
        internal static void Write(byte[] buffer, int offset, uint value)
        {
            Buffer.BlockCopy(BitConverter.GetBytes(value), 0, buffer, offset, 4);
        }

        internal static void Write(byte[] buffer, int offset, float value)
        {
            Buffer.BlockCopy(BitConverter.GetBytes(value), 0, buffer, offset, 4);
        }
    }
}
