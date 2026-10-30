using System;
using System.Runtime.InteropServices;

namespace TypeGameMod
{
    internal static class Win32
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern IntPtr OpenProcess(uint access, bool inherit, int pid);
        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern bool ReadProcessMemory(IntPtr h, IntPtr address, byte[] buffer, int size,
                                                      out IntPtr read);
        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern bool WriteProcessMemory(IntPtr h, IntPtr address, byte[] buffer, int size,
                                                       out IntPtr written);
        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern IntPtr VirtualAllocEx(IntPtr h, IntPtr address, uint size, uint type,
                                                     uint protect);
        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern bool VirtualProtectEx(IntPtr h, IntPtr address, uint size, uint protect,
                                                     out uint oldProtect);
        [DllImport("kernel32.dll")]
        internal static extern bool FlushInstructionCache(IntPtr h, IntPtr address, uint size);
        [DllImport("kernel32.dll")]
        internal static extern bool CloseHandle(IntPtr h);
        [DllImport("kernel32.dll")]
        internal static extern uint GetCurrentThreadId();
        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern IntPtr OpenThread(uint access, bool inherit, uint tid);
        [DllImport("kernel32.dll")]
        internal static extern uint SuspendThread(IntPtr thread);
        [DllImport("kernel32.dll")]
        internal static extern uint ResumeThread(IntPtr thread);
        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern bool GetThreadContext(IntPtr thread, IntPtr context);
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        internal static extern IntPtr GetModuleHandle(string name);
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        internal static extern IntPtr LoadLibrary(string name);
        [DllImport("kernel32.dll", CharSet = CharSet.Ansi)]
        internal static extern IntPtr GetProcAddress(IntPtr module, string name);
        [DllImport("user32.dll")]
        internal static extern short GetAsyncKeyState(int key);
        [DllImport("user32.dll")]
        internal static extern IntPtr GetForegroundWindow();
        [DllImport("user32.dll")]
        internal static extern bool SetForegroundWindow(IntPtr hwnd);
        [DllImport("user32.dll")]
        internal static extern bool IsIconic(IntPtr hwnd);
        [DllImport("user32.dll")]
        internal static extern bool ShowWindow(IntPtr hwnd, int command);
        [DllImport("user32.dll")]
        internal static extern bool AttachThreadInput(uint from, uint to, bool attach);
        [DllImport("user32.dll")]
        internal static extern IntPtr SendMessage(IntPtr hwnd, uint message, IntPtr wparam, IntPtr lparam);
        [DllImport("user32.dll")]
        internal static extern uint GetWindowThreadProcessId(IntPtr hwnd, out uint pid);
        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        internal static extern int GetClassName(IntPtr hwnd, System.Text.StringBuilder name, int size);
        [DllImport("user32.dll")]
        internal static extern bool EnumWindows(EnumWindow callback, IntPtr extra);
        internal delegate bool EnumWindow(IntPtr hwnd, IntPtr extra);
        internal static IntPtr Address(uint address)
        {
            return new IntPtr(unchecked((int)address));
        }

        internal static uint U32(IntPtr value)
        {
            return unchecked((uint)value.ToInt32());
        }

        internal static IntPtr GameWindow(int pid)
        {
            IntPtr result = IntPtr.Zero;
            EnumWindows(delegate(IntPtr hwnd, IntPtr extra) {
                uint owner;
                GetWindowThreadProcessId(hwnd, out owner);
                if (owner == (uint)pid)
                {
                    var name = new System.Text.StringBuilder(128);
                    GetClassName(hwnd, name, 128);
                    if (name.ToString() == "TypeGame")
                    {
                        result = hwnd;
                        return false;
                    }
                }
                return true;
            }, IntPtr.Zero);
            return result;
        }

        internal static void FocusGame(IntPtr hwnd)
        {
            uint owner, target = GetWindowThreadProcessId(hwnd, out owner), current = GetCurrentThreadId();
            bool attached = target != current && AttachThreadInput(current, target, true);
            try
            {
                if (IsIconic(hwnd))
                    ShowWindow(hwnd, 9);
                SetForegroundWindow(hwnd);
            }
            finally
            {
                if (attached)
                    AttachThreadInput(current, target, false);
            }
        }
    }
}
