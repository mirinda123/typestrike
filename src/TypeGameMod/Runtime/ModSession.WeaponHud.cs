using System;

namespace TypeGameMod
{
    internal sealed partial class ModSession
    {
        void PrepareWeaponHud(byte[] initial)
        {
            PrepareAmmoHud(initial);
            // Three128KiB states end exactly at the existing asset allocation limit.
            Write(assetBase + AssetLayout.Weapons, WeaponHud.Build());
            BinaryFields.Write(initial, Control.WeaponSpans, assetBase + AssetLayout.Weapons);
        }
    }
}
