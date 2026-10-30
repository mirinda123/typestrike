using System;

namespace TypeGameMod
{
    internal sealed partial class ModSession
    {
        void AmmoSnapshot(byte[] snapshot, WeaponAmmo ammo, double now)
        {
            if (equippedWeapon == 0)
                return;
            BinaryFields.Write(snapshot, SnapshotLayout.Ammo, (uint)ammo.Rounds[equippedWeapon]);
            BinaryFields.Write(snapshot, SnapshotLayout.Capacity, (uint)WeaponAmmo.Capacity(equippedWeapon));
            BinaryFields.Write(snapshot, SnapshotLayout.Reserve, (uint)ammo.Reserve[equippedWeapon]);
            BinaryFields.Write(snapshot, SnapshotLayout.ReloadMilliseconds, (uint)ammo.Remaining(now));
            BinaryFields.Write(snapshot, SnapshotLayout.ReloadProgress, (uint)ammo.Progress(now));
        }
    }
}
