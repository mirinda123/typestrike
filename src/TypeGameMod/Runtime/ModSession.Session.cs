using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace TypeGameMod
{
    internal sealed partial class ModSession : IDisposable
    {
        readonly Process process;
        readonly Action<string> log;
        readonly List<NativeHook> installed = new List<NativeHook>();
        IntPtr processHandle = IntPtr.Zero;
        uint gameBase, caveBase, gameThreadId, gameWindowHandle;
        uint resourceAddress, assetBase;
        byte[] originalResource;
        SessionWorker worker;
        internal Exception Failure
        {
            get {
                return worker == null ? null : worker.Failure;
            }
        }
        internal int Pid
        {
            get {
                return process.Id;
            }
        }

        internal bool IsAlive
        {
            get {
                return !process.HasExited;
            }
        }

        internal uint ControlBase
        {
            get {
                return caveBase + CaveLayout.Control;
            }
        }

        internal ModSession(Process target, Action<string> logger)
        {
            process = target;
            log = logger;
        }

        static bool Down(int key)
        {
            return (Win32.GetAsyncKeyState(key) & 0x10000) != 0;
        }
        // Only this input/physics thread writes the published snapshot. Native UI code
        // copies it with a sequence check before rendering. Native COM and victory
        // callbacks always execute on the game's original UI thread.
        internal const int SnapshotBytes = 3584;
        internal volatile int SelectedBulletSpeed = GameRules.DefaultBulletSpeed;
        volatile int equippedWeapon;
    }
}
