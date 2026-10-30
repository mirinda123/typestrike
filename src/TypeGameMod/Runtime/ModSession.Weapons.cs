using System;
using System.Threading;

namespace TypeGameMod
{
    internal sealed partial class ModSession
    {
        readonly WeaponSelection weaponSelection = new WeaponSelection();
        int wheelDelta;
        volatile bool acceptWeaponWheel;
        internal void MouseWheel(int delta)
        {
            if (acceptWeaponWheel && Win32.GetForegroundWindow() == Win32.Address(gameWindowHandle))
                Interlocked.Add(ref wheelDelta, delta);
        }

        void ResetWeaponWheel()
        {
            acceptWeaponWheel = false;
            Interlocked.Exchange(ref wheelDelta, 0);
            weaponSelection.Reset();
        }

        int ChooseWeapon(int digit, bool allowed)
        {
            acceptWeaponWheel = allowed;
            int delta = Interlocked.Exchange(ref wheelDelta, 0);
            int next = weaponSelection.Choose(equippedWeapon, digit, delta, allowed);
            if (allowed && delta != 0)
                log("Weapon wheel units=" + delta + " weapon=" + next);
            return next;
        }
    }
}
