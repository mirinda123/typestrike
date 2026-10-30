using System;
using System.Collections.Concurrent;
using System.Runtime.InteropServices;

namespace TypeGameMod
{
    internal sealed partial class WeaponMusic
    {
        // Scalar PROPVARIANT layout in the x86 launcher, from Windows SDK mfplay.h.
        [StructLayout(LayoutKind.Explicit, Size = 16)]
        internal struct PropValue
        {
            [FieldOffset(0)]
            internal ushort Type;
            [FieldOffset(8)]
            internal long Value;
        }
        // Keep the SDK vtable order, including unused video methods before Shutdown.
        [ComImport, Guid("A714590A-58AF-430A-85BF-44F5EC838D85"),
         InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        internal interface MediaPlayer
        {
            [PreserveSig]
            int Play();
            [PreserveSig]
            int Pause();
            [PreserveSig]
            int Stop();
            [PreserveSig]
            int FrameStep();
            [PreserveSig]
            int SetPosition(ref Guid type, ref PropValue position);
            [PreserveSig]
            int GetPosition(ref Guid type, out PropValue position);
            [PreserveSig]
            int GetDuration(ref Guid type, out PropValue duration);
            [PreserveSig]
            int SetRate(float rate);
            [PreserveSig]
            int GetRate(out float rate);
            [PreserveSig]
            int GetSupportedRates([MarshalAs(UnmanagedType.Bool)] bool forward, out float slow,
                                  out float fast);
            [PreserveSig]
            int GetState(out int value);
            [PreserveSig]
            int CreateMediaItemFromURL([MarshalAs(UnmanagedType.LPWStr)] string url,
                                       [MarshalAs(UnmanagedType.Bool)] bool sync, UIntPtr user,
                                       out IntPtr item);
            [PreserveSig]
            int CreateMediaItemFromObject(IntPtr obj, [MarshalAs(UnmanagedType.Bool)] bool sync, UIntPtr user,
                                          out IntPtr item);
            [PreserveSig]
            int SetMediaItem(IntPtr item);
            [PreserveSig]
            int ClearMediaItem();
            [PreserveSig]
            int GetMediaItem(out IntPtr item);
            [PreserveSig]
            int GetVolume(out float volume);
            [PreserveSig]
            int SetVolume(float volume);
            [PreserveSig]
            int GetBalance(out float balance);
            [PreserveSig]
            int SetBalance(float balance);
            [PreserveSig]
            int GetMute([MarshalAs(UnmanagedType.Bool)] out bool mute);
            [PreserveSig]
            int SetMute([MarshalAs(UnmanagedType.Bool)] bool mute);
            [PreserveSig]
            int GetNativeVideoSize(IntPtr size, IntPtr aspect);
            [PreserveSig]
            int GetIdealVideoSize(IntPtr minimum, IntPtr maximum);
            [PreserveSig]
            int SetVideoSourceRect(IntPtr rectangle);
            [PreserveSig]
            int GetVideoSourceRect(IntPtr rectangle);
            [PreserveSig]
            int SetAspectRatioMode(uint mode);
            [PreserveSig]
            int GetAspectRatioMode(out uint mode);
            [PreserveSig]
            int GetVideoWindow(out IntPtr window);
            [PreserveSig]
            int UpdateVideo();
            [PreserveSig]
            int SetBorderColor(uint color);
            [PreserveSig]
            int GetBorderColor(out uint color);
            [PreserveSig]
            int InsertEffect(IntPtr effect, [MarshalAs(UnmanagedType.Bool)] bool optional);
            [PreserveSig]
            int RemoveEffect(IntPtr effect);
            [PreserveSig]
            int RemoveAllEffects();
            [PreserveSig]
            int Shutdown();
        }
        [ComVisible(true), Guid("766C8FFB-5FDB-4FEA-A28D-B912996F51BD"),
         InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        public interface MediaCallback
        {
            [PreserveSig]
            void OnMediaPlayerEvent(IntPtr header);
        }

        internal struct MediaEvent
        {
            internal int Type, Error;
        }
        [ComVisible(true), ClassInterface(ClassInterfaceType.None)]
        public sealed class Events : MediaCallback
        {
            internal readonly ConcurrentQueue<MediaEvent> Queue = new ConcurrentQueue<MediaEvent>();
            public void OnMediaPlayerEvent(IntPtr header)
            {
                // Free-threaded callback only copies scalars; it does no COM or UI work.
                if (header != IntPtr.Zero)
                    Queue.Enqueue(new MediaEvent { Type = Marshal.ReadInt32(header),
                                                   Error = Marshal.ReadInt32(header, 4) });
            }
        }
        [DllImport("ole32.dll")]
        static extern int CoInitializeEx(IntPtr reserved, uint model);
        [DllImport("ole32.dll")]
        static extern void CoUninitialize();
        [DllImport("mfplat.dll")]
        static extern int MFStartup(uint version, uint flags);
        [DllImport("mfplat.dll")]
        static extern int MFShutdown();
        [DllImport("mfplay.dll", CharSet = CharSet.Unicode)]
        static extern int MFPCreateMediaPlayer(string url, [MarshalAs(UnmanagedType.Bool)] bool autoPlay,
                                               uint options,
                                               [MarshalAs(UnmanagedType.Interface)] MediaCallback callback,
                                               IntPtr window, out MediaPlayer player);
    }
}
