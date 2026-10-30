using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

namespace TypeGameMod
{
    internal sealed partial class ModSession : IDisposable
    {
        public void Dispose()
        {
            if (worker != null)
            {
                worker.Stop();
                if (worker.Failure != null)
                    log("Session worker stopped: " + worker.Failure);
            }
            try
            {
                StopCombatAudio();
            }
            catch (Exception ex)
            {
                log("SFX cleanup: " + ex.Message);
            }
            try
            {
                StopMusic();
            }
            catch (Exception ex)
            {
                log("Music cleanup: " + ex.Message);
            }
            if (processHandle == IntPtr.Zero)
                return;
            if (IsAlive && caveBase != 0)
            {
                try
                {
                    Write(ControlBase + Control.ReleaseRequested, BitConverter.GetBytes(1u));
                    Write(ControlBase, BitConverter.GetBytes(0u));
                    for (int wait = 0; wait < 50 && OwnedSurfacesRemain(); wait++)
                        Thread.Sleep(10);
                    WithGameThreadPaused(delegate {
                        uint gameObject = BitConverter.ToUInt32(Read(ControlBase + Control.GameObject, 4), 0);
                        uint current =
                            BitConverter.ToUInt32(Read(gameBase + GameLayout.ActiveCallback, 4), 0) &
                            0xfffffffeu;
                        // Leaving this minigame frees its object. Restore a custom
                        // pose only while the original callback still owns that object.
                        if (gameObject != 0 && current == gameObject + 0x160 &&
                            BitConverter.ToUInt32(Read(gameObject, 4), 0) ==
                                gameBase + GameLayout.PoliceGameVtable)
                        {
                            uint player = BitConverter.ToUInt32(Read(gameObject + 0xc8, 4), 0);
                            uint role = BitConverter.ToUInt32(Read(gameObject + 0x104, 4), 0);
                            if (role <= 1 && player != 0)
                                player += role * 56;
                            if (player != 0 && BitConverter.ToUInt32(Read(player + 0x24, 4), 0) == 2)
                            {
                                Write(player + 0x24, BitConverter.GetBytes(0u));
                                Write(player + 0x1c, BitConverter.GetBytes(0u));
                                uint heading =
                                    BitConverter.ToUInt32(Read(ControlBase + Control.Heading, 4), 0);
                                uint[] stockDirections = { 0, 2, 2, 3, 1, 1, 0, 0 };
                                Write(player + 0x28, BitConverter.GetBytes(stockDirections[heading & 7]));
                            }
                        }
                        for (int i = installed.Count - 1; i >= 0; i--)
                        {
                            NativeHook hook = installed[i];
                            WriteCode(gameBase + hook.GameOffset, hook.Original);
                        }
                        if (!Same(Read(resourceAddress, GameIdentity.SpriteBytes), originalResource))
                        {
                            uint protect, ignored;
                            if (!Win32.VirtualProtectEx(processHandle, Win32.Address(resourceAddress),
                                                        GameIdentity.SpriteBytes, 0x04, out protect))
                                throw new Win32Exception(Marshal.GetLastWin32Error());
                            Write(resourceAddress, originalResource);
                            Win32.VirtualProtectEx(processHandle, Win32.Address(resourceAddress),
                                                   GameIdentity.SpriteBytes, protect, out ignored);
                        }
                    }, true);
                    log("Restored original memory hooks. Original game files were not changed.");
                }
                catch (Exception ex)
                {
                    log("Restore warning: " + ex.Message +
                        ". Restart the game to clear all memory modifications.");
                }
            }
            // Keep inactive code/data until game exit so pending native return addresses remain valid.
            // No original surface ownership or on-disk resource is changed.
            Win32.CloseHandle(processHandle);
            processHandle = IntPtr.Zero;
        }
    }
}
