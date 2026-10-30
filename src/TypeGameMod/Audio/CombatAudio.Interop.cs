using System;
using System.Runtime.InteropServices;

namespace TypeGameMod
{
    internal sealed partial class CombatAudio
    {
        [StructLayout(LayoutKind.Sequential, Pack = 2)]
        struct Format
        {
            internal ushort Tag, Channels;
            internal uint Rate, Bytes;
            internal ushort Align, Bits, Extra;
        }
        [StructLayout(LayoutKind.Sequential)]
        struct Header
        {
            internal IntPtr Data;
            internal uint Length, Recorded;
            internal UIntPtr User;
            internal uint Flags, Loops;
            internal IntPtr Next;
            internal UIntPtr Reserved;
        }
        [DllImport("winmm.dll")]
        static extern uint waveOutOpen(out IntPtr handle, uint device, ref Format format, IntPtr callback,
                                       IntPtr instance, uint flags);
        [DllImport("winmm.dll")]
        static extern uint waveOutPrepareHeader(IntPtr handle, IntPtr header, uint size);
        [DllImport("winmm.dll")]
        static extern uint waveOutUnprepareHeader(IntPtr handle, IntPtr header, uint size);
        [DllImport("winmm.dll")]
        static extern uint waveOutWrite(IntPtr handle, IntPtr header, uint size);
        [DllImport("winmm.dll")]
        static extern uint waveOutReset(IntPtr handle);
        [DllImport("winmm.dll")]
        static extern uint waveOutClose(IntPtr handle);
    }
}
