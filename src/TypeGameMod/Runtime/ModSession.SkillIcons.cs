using System;

namespace TypeGameMod
{
    internal sealed partial class ModSession
    {
        void PrepareSkillIcons(byte[] initial)
        {
            byte[] info = new byte[40];
            BinaryFields.Write(info, 0, 40u);
            BinaryFields.Write(info, 4, (uint)SkillIconFrames.SkillIconSize);
            BinaryFields.Write(info, 8, unchecked((uint)-SkillIconFrames.SkillIconSize));
            info[12] = 1;
            info[14] = 32;
            BinaryFields.Write(info, 20, (uint)SkillIconFrames.SkillIconBytes);
            Write(assetBase + AssetLayout.SkillBitmapInfo, info);
            Write(assetBase + AssetLayout.PoliceSkillFrames,
                  SkillIconFrames.Build("TypeGameWASD.PoliceShieldIcon"));
            Write(assetBase + AssetLayout.ThiefSkillFrames,
                  SkillIconFrames.Build("TypeGameWASD.ThiefBlinkIcon"));
            Write(assetBase + AssetLayout.SupplySkillFrames, SkillIconFrames.BuildResupply());
            BinaryFields.Write(initial, Control.SupplyIcon, assetBase + AssetLayout.SupplySkillFrames);
            BinaryFields.Write(initial, Control.GetTextExtentPoint32W,
                               RemoteFunction("gdi32.dll", "GetTextExtentPoint32W"));
            BinaryFields.Write(initial, Control.BitBlt, RemoteFunction("gdi32.dll", "BitBlt"));
            BinaryFields.Write(initial, Control.PoliceSkillIcon, assetBase + AssetLayout.PoliceSkillFrames);
            BinaryFields.Write(initial, Control.ThiefSkillIcon, assetBase + AssetLayout.ThiefSkillFrames);
            BinaryFields.Write(initial, Control.SkillIconBitmapInfo, assetBase + AssetLayout.SkillBitmapInfo);
            BinaryFields.Write(initial, Control.SetDIBitsToDevice,
                               RemoteFunction("gdi32.dll", "SetDIBitsToDevice"));
            PrepareHealthHud(initial);
        }
    }
}
