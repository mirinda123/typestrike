using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;

namespace TypeGameMod
{
    internal sealed partial class ModSession : IDisposable
    {
        byte[] Read(uint address, int size)
        {
            byte[] bytes = new byte[size];
            IntPtr count;
            if (!Win32.ReadProcessMemory(processHandle, Win32.Address(address), bytes, size, out count) ||
                count.ToInt32() != size)
                throw new Win32Exception(Marshal.GetLastWin32Error(), "读取游戏内存失败");
            return bytes;
        }

        void Write(uint address, byte[] bytes)
        {
            IntPtr count;
            if (!Win32.WriteProcessMemory(processHandle, Win32.Address(address), bytes, bytes.Length,
                                          out count) ||
                count.ToInt32() != bytes.Length)
                throw new Win32Exception(Marshal.GetLastWin32Error(), "写入游戏内存失败");
        }

        void WriteCode(uint address, byte[] bytes)
        {
            uint old;
            if (!Win32.VirtualProtectEx(processHandle, Win32.Address(address), (uint)bytes.Length, 0x40,
                                        out old))
                throw new Win32Exception(Marshal.GetLastWin32Error());
            try
            {
                Write(address, bytes);
                Win32.FlushInstructionCache(processHandle, Win32.Address(address), (uint)bytes.Length);
            }
            finally
            {
                uint ignored;
                Win32.VirtualProtectEx(processHandle, Win32.Address(address), (uint)bytes.Length, old,
                                       out ignored);
            }
        }

        bool Same(byte[] a, byte[] b)
        {
            if (a.Length != b.Length)
                return false;
            for (int i = 0; i < a.Length; i++)
                if (a[i] != b[i])
                    return false;
            return true;
        }

        uint RemoteFunction(string module, string symbol)
        {
            uint local = Win32.U32(Win32.GetProcAddress(Win32.GetModuleHandle(module), symbol));
            if (local == 0)
                throw new InvalidOperationException("无法定位系统函数：" + symbol);
            foreach (ProcessModule localModule in Process.GetCurrentProcess().Modules)
            {
                uint start = Win32.U32(localModule.BaseAddress);
                if (local >= start && local - start < (uint)localModule.ModuleMemorySize)
                {
                    foreach (ProcessModule remoteModule in process.Modules)
                    {
                        if (String.Equals(localModule.ModuleName, remoteModule.ModuleName,
                                          StringComparison.OrdinalIgnoreCase))
                            return Win32.U32(remoteModule.BaseAddress) + local - start;
                    }
                }
            }
            throw new InvalidOperationException("无法定位游戏进程的系统函数：" + symbol);
        }

        void WithGameThreadPaused(Action action, bool restoring)
        {
            IntPtr th = Win32.OpenThread(0x0002 | 0x0008 | 0x0040, false, gameThreadId);
            if (th == IntPtr.Zero)
                throw new Win32Exception(Marshal.GetLastWin32Error(), "无法短暂暂停游戏线程");
            IntPtr context = Marshal.AllocHGlobal(716);
            try
            {
                for (int attempt = 0; attempt < 80; attempt++)
                {
                    if (Win32.SuspendThread(th) == uint.MaxValue)
                        throw new Win32Exception(Marshal.GetLastWin32Error());
                    bool retry = false;
                    try
                    {
                        Marshal.WriteInt32(context, 0, 0x10001);
                        if (!Win32.GetThreadContext(th, context))
                            throw new Win32Exception(Marshal.GetLastWin32Error());
                        uint ip = unchecked((uint)Marshal.ReadInt32(context, 184));
                        foreach (NativeHook hook in NativeHook.All)
                            if (ip > gameBase + hook.GameOffset &&
                                ip < gameBase + hook.GameOffset + hook.Original.Length)
                                retry = true;
                        if (restoring && ip >= caveBase && ip < caveBase + 0x10000)
                            retry = true;
                        if (restoring && caveBase != 0 &&
                            BitConverter.ToUInt32(Read(ControlBase + Control.LoadingSprite, 4), 0) != 0)
                            retry = true;
                        if (restoring && caveBase != 0 &&
                            BitConverter.ToUInt32(Read(ControlBase + Control.ResourceSwapActive, 4), 0) != 0)
                            retry = true;
                        if (!retry)
                        {
                            action();
                            return;
                        }
                    }
                    finally
                    {
                        Win32.ResumeThread(th);
                    }
                    Thread.Sleep(2);
                }
                throw new InvalidOperationException("游戏线程正处于 MOD 函数内，请重试");
            }
            finally
            {
                Marshal.FreeHGlobal(context);
                Win32.CloseHandle(th);
            }
        }

        uint WriteBstr(uint address, string value)
        {
            byte[] text = System.Text.Encoding.Unicode.GetBytes(value + "\0"),
                   bstr = new byte[text.Length + 4];
            BinaryFields.Write(bstr, 0, (uint)(text.Length - 2));
            Buffer.BlockCopy(text, 0, bstr, 4, text.Length);
            Write(address, bstr);
            return address + 4;
        }
    }
}
