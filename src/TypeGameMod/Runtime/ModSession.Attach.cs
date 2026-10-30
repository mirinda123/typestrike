using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Threading;

namespace TypeGameMod
{
    internal sealed partial class ModSession : IDisposable
    {
        internal void Attach()
        {
            string path = process.MainModule.FileName;
            if (GameIdentity.Hash(path) != GameIdentity.ExecutableHash)
                throw new InvalidOperationException(
                    "这个程序仅支持已验证的经典版 TypeGame 8.00.0035（文件校验不匹配）。");
            processHandle = Win32.OpenProcess(0x0438, false, process.Id);
            if (processHandle == IntPtr.Zero)
                throw new Win32Exception(Marshal.GetLastWin32Error(),
                                         "无法连接游戏。请让游戏与启动器使用相同的权限。");
            gameBase = Win32.U32(process.MainModule.BaseAddress);
            // A TypeGame window is created before its DirectDraw initialization finishes.
            // Wait for both display and back-buffer surfaces before installing any hook.
            bool graphicsReady = false;
            for (int wait = 0; wait < 100 && IsAlive; wait++)
            {
                uint engine = BitConverter.ToUInt32(Read(gameBase + GameLayout.GraphicsEngine, 4), 0);
                if (engine != 0)
                {
                    byte[] surfaces = Read(engine + 0x34, 12);
                    if (BitConverter.ToUInt32(surfaces, 0) != 0 && BitConverter.ToUInt32(surfaces, 4) != 0 &&
                        BitConverter.ToUInt32(surfaces, 8) != 0)
                    {
                        graphicsReady = true;
                        break;
                    }
                }
                Thread.Sleep(50);
            }
            if (!graphicsReady)
                throw new InvalidOperationException("原游戏图形初始化未完成。请关闭游戏后重新启动。");
            if (!GameInstallation.Compatible(process))
                throw new InvalidOperationException(
                    "当前游戏未启用本版图形兼容修复。请先关闭游戏，再从本启动器启动。");
            foreach (NativeHook hook in NativeHook.All)
                if (!Same(Read(gameBase + hook.GameOffset, hook.Original.Length), hook.Original))
                    throw new InvalidOperationException("游戏内存与支持版本不一致：" + hook.Name);
            gameWindowHandle = Win32.U32(Win32.GameWindow(process.Id));
            uint owner;
            gameThreadId = Win32.GetWindowThreadProcessId(Win32.Address(gameWindowHandle), out owner);
            if (gameThreadId == 0)
                throw new InvalidOperationException("没有找到实际游戏窗口。");
            caveBase = Win32.U32(Win32.VirtualAllocEx(processHandle, IntPtr.Zero, 0x10000, 0x3000, 0x40));
            if (caveBase == 0)
                throw new Win32Exception(Marshal.GetLastWin32Error());
            Write(caveBase, NativePayload.Build(caveBase, gameBase));
            byte[] initial = new byte[1024];
            BinaryFields.Write(initial, Control.Enabled, 1u);
            BinaryFields.Write(initial, Control.LastMovementTick, unchecked((uint)Environment.TickCount));
            BinaryFields.Write(initial, Control.WorldMinX, 24f);
            BinaryFields.Write(initial, Control.WorldMaxX, 1576f);
            BinaryFields.Write(initial, Control.WorldMinY, 32f);
            BinaryFields.Write(initial, Control.WorldMaxY, 1168f);
            BinaryFields.Write(initial, Control.SecondsPerMillisecond, .001f);
            BinaryFields.Write(initial, Control.GetTickCount, RemoteFunction("kernel32.dll", "GetTickCount"));
            BinaryFields.Write(initial, Control.WorldWidth, 1600f);
            BinaryFields.Write(initial, Control.Heartbeat, unchecked((uint)Environment.TickCount));
            BinaryFields.Write(initial, Control.Focused,
                               Win32.GetForegroundWindow() == Win32.Address(gameWindowHandle) ? 1u : 0u);
            BinaryFields.Write(initial, Control.GetForegroundWindow,
                               RemoteFunction("user32.dll", "GetForegroundWindow"));
            BinaryFields.Write(initial, Control.GameWindow, gameWindowHandle);
            BinaryFields.Write(initial, Control.VirtualProtect,
                               RemoteFunction("kernel32.dll", "VirtualProtect"));
            BinaryFields.Write(initial, Control.TextOutW, RemoteFunction("gdi32.dll", "TextOutW"));
            BinaryFields.Write(initial, Control.SetBkColor, RemoteFunction("gdi32.dll", "SetBkColor"));
            BinaryFields.Write(initial, Control.SetTextColor, RemoteFunction("gdi32.dll", "SetTextColor"));
            BinaryFields.Write(initial, Control.SetBkMode, RemoteFunction("gdi32.dll", "SetBkMode"));
            BinaryFields.Write(initial, Control.SaveDC, RemoteFunction("gdi32.dll", "SaveDC"));
            BinaryFields.Write(initial, Control.RestoreDC, RemoteFunction("gdi32.dll", "RestoreDC"));
            BinaryFields.Write(initial, Control.CreatePen, RemoteFunction("gdi32.dll", "CreatePen"));
            BinaryFields.Write(initial, Control.SelectObject, RemoteFunction("gdi32.dll", "SelectObject"));
            BinaryFields.Write(initial, Control.MoveToEx, RemoteFunction("gdi32.dll", "MoveToEx"));
            BinaryFields.Write(initial, Control.LineTo, RemoteFunction("gdi32.dll", "LineTo"));
            BinaryFields.Write(initial, Control.DeleteObject, RemoteFunction("gdi32.dll", "DeleteObject"));
            BinaryFields.Write(initial, Control.SysFreeString,
                               RemoteFunction("oleaut32.dll", "SysFreeString"));
            Win32.LoadLibrary("imm32.dll");
            BinaryFields.Write(initial, Control.ImmGetContext, RemoteFunction("imm32.dll", "ImmGetContext"));
            BinaryFields.Write(initial, Control.ImmSetOpenStatus,
                               RemoteFunction("imm32.dll", "ImmSetOpenStatus"));
            BinaryFields.Write(initial, Control.ImmReleaseContext,
                               RemoteFunction("imm32.dll", "ImmReleaseContext"));
            BinaryFields.Write(initial, Control.ImmGetOpenStatus,
                               RemoteFunction("imm32.dll", "ImmGetOpenStatus"));
            PrepareSprites(initial);
            Write(ControlBase, initial);
            PrepareLanDiscovery();
            InitializeNativeBridges();
            Win32.FlushInstructionCache(processHandle, Win32.Address(caveBase), 0x10000);
            WithGameThreadPaused(delegate {
                foreach (NativeHook hook in NativeHook.All)
                {
                    WriteCode(gameBase + hook.GameOffset, hook.Jump(gameBase, caveBase));
                    installed.Add(hook);
                }
            }, false);
            StartMusic();
            worker = new SessionWorker(Poll);
            worker.Start();
            log("Installed PID=" + process.Id + " game=0x" + gameBase.ToString("x8") + " cave=0x" +
                caveBase.ToString("x8") + " data=0x" + ControlBase.ToString("x8"));
        }
    }
}
