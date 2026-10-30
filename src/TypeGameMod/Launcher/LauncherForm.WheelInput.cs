using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace TypeGameMod
{
    internal sealed partial class LauncherForm
    {
        [StructLayout(LayoutKind.Sequential)]
        struct WheelDevice
        {
            internal ushort Page, Usage;
            internal uint Flags;
            internal IntPtr Target;
        }
        [DllImport("user32.dll", SetLastError = true)]
        static extern bool RegisterRawInputDevices([In] WheelDevice[] devices, uint count, uint size);
        [DllImport("user32.dll", SetLastError = true)]
        static extern uint GetRawInputData(IntPtr input, uint command, [Out] byte[] data, ref uint size,
                                           uint headerSize);
        readonly byte[] rawMouse = new byte[64];
        bool wheelRegistered;
        void StartWheelInput()
        {
            var devices = new[] { new WheelDevice { Page = 1, Usage = 2, Flags = 0x100, Target = Handle } };
            if (!RegisterRawInputDevices(devices, 1, (uint)Marshal.SizeOf(typeof(WheelDevice))))
                throw new Win32Exception(Marshal.GetLastWin32Error(), "无法注册鼠标滚轮输入");
            wheelRegistered = true;
            Log("Raw Input wheel registered for launcher; game foreground/movement only.");
        }

        void StopWheelInput()
        {
            if (!wheelRegistered)
                return;
            var devices = new[] { new WheelDevice { Page = 1, Usage = 2, Flags = 1, Target = IntPtr.Zero } };
            bool removed = RegisterRawInputDevices(devices, 1, (uint)Marshal.SizeOf(typeof(WheelDevice)));
            wheelRegistered = false;
            Log("Raw Input wheel unregistered=" + removed);
        }
        protected override void WndProc(ref Message message)
        {
            if (message.Msg == 0xff && wheelRegistered && mod != null)
            {
                uint size = (uint)rawMouse.Length;
                int header = 8 + 2 * IntPtr.Size;
                uint copied = GetRawInputData(message.LParam, 0x10000003, rawMouse, ref size, (uint)header);
                int delta;
                if (copied <= rawMouse.Length &&
                    WeaponSelection.RawDelta(rawMouse, (int)copied, header, out delta))
                    mod.MouseWheel(delta);
            }
            base.WndProc(ref message);
        }
    }
}
