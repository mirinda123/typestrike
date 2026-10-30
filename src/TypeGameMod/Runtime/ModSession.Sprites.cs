using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

namespace TypeGameMod
{
    internal sealed partial class ModSession : IDisposable
    {
        byte[] Sprite(string name)
        {
            byte[] art = new byte[GameIdentity.SpriteBytes];
            using (Stream s = typeof(ModSession).Assembly.GetManifestResourceStream(name))
            {
                if (s == null || s.Length != GameIdentity.SpriteBytes)
                    throw new InvalidOperationException("启动器缺少八向动画。");
                int at = 0, n;
                while (at < art.Length && (n = s.Read(art, at, art.Length - at)) > 0)
                    at += n;
                if (at != art.Length)
                    throw new EndOfStreamException("八向动画数据不完整。");
            }
            if (BitConverter.ToUInt32(art, 0) != 40 || BitConverter.ToInt32(art, 4) != 276 ||
                BitConverter.ToInt32(art, 8) != 594 || BitConverter.ToUInt16(art, 14) != 8)
                throw new InvalidOperationException("八向动画格式不匹配。");
            return art;
        }

        void PrepareSprites(byte[] initial)
        {
            ProcessModule resources = null;
            foreach (ProcessModule module in process.Modules)
                if (String.Equals(module.ModuleName, "GameRes.dll", StringComparison.OrdinalIgnoreCase))
                {
                    resources = module;
                    break;
                }
            if (resources == null || GameIdentity.Hash(resources.FileName) != GameIdentity.ResourceHash)
                throw new InvalidOperationException("动画资源与支持版本不一致，无法启用新动画。");
            resourceAddress = Win32.U32(resources.BaseAddress) + GameIdentity.ResourceBitmapOffset;
            originalResource = Read(resourceAddress, GameIdentity.SpriteBytes);
            if (BitConverter.ToUInt32(originalResource, 0) != 40 ||
                BitConverter.ToInt32(originalResource, 4) != 276 ||
                BitConverter.ToInt32(originalResource, 8) != 594 ||
                BitConverter.ToUInt16(originalResource, 14) != 8)
                throw new InvalidOperationException("原游戏动画格式不匹配。");
            byte[] art = Sprite("TypeGameWASD.PoliceCardinal");
            assetBase = Win32.U32(
                Win32.VirtualAllocEx(processHandle, IntPtr.Zero, AssetLayout.AllocationBytes, 0x3000, 0x04));
            if (assetBase == 0)
                throw new Win32Exception(Marshal.GetLastWin32Error());
            uint movementMap = assetBase + AssetLayout.MovementMap;
            Write(movementMap, MovementMap.City.Cover.CopyPixels());
            BinaryFields.Write(initial, Control.MovementMap, movementMap);
            uint newArt = assetBase + AssetLayout.PoliceCardinal, name = assetBase + AssetLayout.SpriteName;
            Write(assetBase, originalResource);
            Write(newArt, art);
            Write(assetBase + AssetLayout.PoliceArmed0, Sprite("TypeGameWASD.PoliceArmed0"));
            Write(assetBase + AssetLayout.PoliceArmed1, Sprite("TypeGameWASD.PoliceArmed1"));
            Write(assetBase + AssetLayout.PoliceArmedIdle, Sprite("TypeGameWASD.PoliceArmedIdle"));
            Write(assetBase + AssetLayout.PoliceShotgun0, Sprite("TypeGameWASD.PoliceShotgun0"));
            Write(assetBase + AssetLayout.PoliceShotgun1, Sprite("TypeGameWASD.PoliceShotgun1"));
            Write(assetBase + AssetLayout.PoliceShotgunIdle, Sprite("TypeGameWASD.PoliceShotgunIdle"));
            Write(assetBase + AssetLayout.ThiefCardinal, Sprite("TypeGameWASD.ThiefCardinal"));
            Write(assetBase + AssetLayout.ThiefArmed0, Sprite("TypeGameWASD.ThiefArmed0"));
            Write(assetBase + AssetLayout.ThiefArmed1, Sprite("TypeGameWASD.ThiefArmed1"));
            Write(assetBase + AssetLayout.ThiefArmedIdle, Sprite("TypeGameWASD.ThiefArmedIdle"));
            Write(assetBase + AssetLayout.ThiefShotgun0, Sprite("TypeGameWASD.ThiefShotgun0"));
            Write(assetBase + AssetLayout.ThiefShotgun1, Sprite("TypeGameWASD.ThiefShotgun1"));
            Write(assetBase + AssetLayout.ThiefShotgunIdle, Sprite("TypeGameWASD.ThiefShotgunIdle"));
            Write(assetBase + AssetLayout.PoliceShield0, Sprite("TypeGameWASD.PoliceShield0"));
            Write(assetBase + AssetLayout.PoliceShield1, Sprite("TypeGameWASD.PoliceShield1"));
            Write(assetBase + AssetLayout.PoliceShieldIdle, Sprite("TypeGameWASD.PoliceShieldIdle"));
            BinaryFields.Write(initial, Control.ThiefAkRun0, assetBase + AssetLayout.ThiefArmed0);
            BinaryFields.Write(initial, Control.ThiefAkRun1, assetBase + AssetLayout.ThiefArmed1);
            BinaryFields.Write(initial, Control.ThiefAkIdle, assetBase + AssetLayout.ThiefArmedIdleTable);
            BinaryFields.Write(initial, Control.ThiefShotgunRun0, assetBase + AssetLayout.ThiefShotgun0);
            BinaryFields.Write(initial, Control.ThiefShotgunRun1, assetBase + AssetLayout.ThiefShotgun1);
            BinaryFields.Write(initial, Control.ThiefShotgunIdle,
                               assetBase + AssetLayout.ThiefShotgunIdleTable);
            BinaryFields.Write(initial, Control.ThiefCardinalPixels, assetBase + AssetLayout.ThiefCardinal);
            BinaryFields.Write(initial, Control.PoliceCardinalPixels, newArt);
            BinaryFields.Write(initial, Control.PoliceAkRun0, assetBase + AssetLayout.PoliceArmed0);
            BinaryFields.Write(initial, Control.PoliceAkRun1, assetBase + AssetLayout.PoliceArmed1);
            BinaryFields.Write(initial, Control.PoliceAkRunTable0, assetBase + AssetLayout.PoliceAkRunTable0);
            BinaryFields.Write(initial, Control.PoliceAkRunTable1, assetBase + AssetLayout.PoliceAkRunTable1);
            BinaryFields.Write(initial, Control.PoliceAkIdle, assetBase + AssetLayout.PoliceArmedIdleTable);
            BinaryFields.Write(initial, Control.PoliceShotgunRun0, assetBase + AssetLayout.PoliceShotgun0);
            BinaryFields.Write(initial, Control.PoliceShotgunRun1, assetBase + AssetLayout.PoliceShotgun1);
            BinaryFields.Write(initial, Control.PoliceShotgunIdle,
                               assetBase + AssetLayout.PoliceShotgunIdleTable);
            byte[] text = System.Text.Encoding.Unicode.GetBytes("POLICE_0_2_0\0");
            byte[] bstr = new byte[text.Length + 4];
            BinaryFields.Write(bstr, 0, (uint)(text.Length - 2));
            Buffer.BlockCopy(text, 0, bstr, 4, text.Length);
            Write(name, bstr);
            BinaryFields.Write(initial, Control.Heading, 0u);
            BinaryFields.Write(initial, Control.ResourceAddress, resourceAddress);
            BinaryFields.Write(initial, Control.ResourceBackup, assetBase);
            BinaryFields.Write(initial, Control.CardinalPixels, newArt);
            BinaryFields.Write(initial, Control.SpriteDwords, (uint)(GameIdentity.SpriteBytes / 4));
            BinaryFields.Write(initial, Control.SpriteName, name + 4);
            BinaryFields.Write(initial, Control.CardinalHeadingMask, 0x55u);
            PrepareThiefRun(initial);
            PrepareThiefIdle(initial);
            PrepareThiefAkPose(initial);
            PrepareThiefAkRun(initial);
            PreparePoliceRun(initial);
            PreparePoliceAkRun();
            PreparePoliceShotgunRun(initial);
            PreparePoliceShieldRun(initial);
            Write(assetBase + AssetLayout.PoliceWeaponOrigins, PoliceWeaponOrigins.Build());
            BinaryFields.Write(initial, Control.PoliceWeaponOrigins,
                               assetBase + AssetLayout.PoliceWeaponOrigins);
            PrepareThiefShotgunPose(initial);
            PrepareThiefShotgunRun(initial);
            PrepareLayeredIdle(initial, resources.FileName);
            PreparePoliceIdle();
            PrepareSkillIcons(initial);
        }
        // Every running/long-idle pose uses two banks per heading. Upload
        // pixels first, then publish their pointers in the corresponding table.
        void UploadDirectionalBanks(string prefix, uint frameOffset, uint firstTable, uint secondTable)
        {
            for (int heading = 0; heading < 8; heading++)
            {
                for (int bank = 0; bank < 2; bank++)
                {
                    uint pixels =
                        assetBase + frameOffset + (uint)(heading * 2 + bank) * AssetLayout.IdleBankStride;
                    Write(pixels, Sprite("TypeGameWASD." + prefix + heading + "_" + bank));
                    uint table = bank == 0 ? firstTable : secondTable;
                    Write(assetBase + table + (uint)heading * 4, BitConverter.GetBytes(pixels));
                }
            }
        }

        void PreparePoliceRun(byte[] initial)
        {
            UploadDirectionalBanks("PoliceRun", AssetLayout.PoliceRunFrames, AssetLayout.PoliceRunTable0,
                                   AssetLayout.PoliceRunTable1);
            BinaryFields.Write(initial, Control.PoliceRunTable0, assetBase + AssetLayout.PoliceRunTable0);
            BinaryFields.Write(initial, Control.PoliceRunTable1, assetBase + AssetLayout.PoliceRunTable1);
        }

        void PreparePoliceShieldRun(byte[] initial)
        {
            UploadDirectionalBanks("PoliceShieldRun", AssetLayout.PoliceShieldRunFrames,
                                   AssetLayout.PoliceShieldRunTable0, AssetLayout.PoliceShieldRunTable1);
            BinaryFields.Write(initial, Control.PoliceShieldRunTable0,
                               assetBase + AssetLayout.PoliceShieldRunTable0);
            BinaryFields.Write(initial, Control.PoliceShieldRunTable1,
                               assetBase + AssetLayout.PoliceShieldRunTable1);
        }

        void PreparePoliceShotgunRun(byte[] initial)
        {
            UploadDirectionalBanks("PoliceShotgunRun", AssetLayout.PoliceShotgunRunFrames,
                                   AssetLayout.PoliceShotgunRunTable0, AssetLayout.PoliceShotgunRunTable1);
            BinaryFields.Write(initial, Control.PoliceShotgunRunTable0,
                               assetBase + AssetLayout.PoliceShotgunRunTable0);
            BinaryFields.Write(initial, Control.PoliceShotgunRunTable1,
                               assetBase + AssetLayout.PoliceShotgunRunTable1);
        }

        void PreparePoliceAkRun()
        {
            UploadDirectionalBanks("PoliceAkRun", AssetLayout.PoliceAkRunFrames,
                                   AssetLayout.PoliceAkRunTable0, AssetLayout.PoliceAkRunTable1);
        }

        void PrepareThiefRun(byte[] initial)
        {
            UploadDirectionalBanks("ThiefRun", AssetLayout.ThiefRunFrames, AssetLayout.ThiefRunTable0,
                                   AssetLayout.ThiefRunTable1);
            BinaryFields.Write(initial, Control.ThiefRunTable0, assetBase + AssetLayout.ThiefRunTable0);
            BinaryFields.Write(initial, Control.ThiefRunTable1, assetBase + AssetLayout.ThiefRunTable1);
        }

        void PrepareThiefIdle(byte[] initial)
        {
            UploadDirectionalBanks("ThiefIdle", AssetLayout.ThiefIdleFrames, AssetLayout.ThiefIdleTable0,
                                   AssetLayout.ThiefIdleTable1);
            BinaryFields.Write(initial, Control.ThiefIdleTable0, assetBase + AssetLayout.ThiefIdleTable0);
            BinaryFields.Write(initial, Control.ThiefIdleTable1, assetBase + AssetLayout.ThiefIdleTable1);
        }

        void PrepareThiefAkPose(byte[] initial)
        {
            UploadDirectionalBanks("ThiefAkPose", AssetLayout.ThiefAkPoseFrames,
                                   AssetLayout.ThiefAkPoseTable0, AssetLayout.ThiefAkPoseTable1);
            BinaryFields.Write(initial, Control.ThiefAkPoseTable0, assetBase + AssetLayout.ThiefAkPoseTable0);
            BinaryFields.Write(initial, Control.ThiefAkPoseTable1, assetBase + AssetLayout.ThiefAkPoseTable1);
            for (int heading = 0; heading < 8; heading++)
                Write(assetBase + AssetLayout.ThiefAkPoseOrigins + (uint)heading * 4,
                      BitConverter.GetBytes(ThiefAkPose.DrawOffsetX(heading)));
            BinaryFields.Write(initial, Control.ThiefAkPoseOrigins,
                               assetBase + AssetLayout.ThiefAkPoseOrigins);
        }

        void PrepareThiefAkRun(byte[] initial)
        {
            UploadDirectionalBanks("ThiefAkRun", AssetLayout.ThiefAkRunFrames, AssetLayout.ThiefAkRunTable0,
                                   AssetLayout.ThiefAkRunTable1);
            BinaryFields.Write(initial, Control.ThiefAkRunTable0, assetBase + AssetLayout.ThiefAkRunTable0);
            BinaryFields.Write(initial, Control.ThiefAkRunTable1, assetBase + AssetLayout.ThiefAkRunTable1);
            for (int heading = 0; heading < 8; heading++)
                Write(assetBase + AssetLayout.ThiefAkRunOrigins + (uint)heading * 4,
                      BitConverter.GetBytes(ThiefAkRun.DrawOffsetX(heading)));
            BinaryFields.Write(initial, Control.ThiefAkRunOrigins, assetBase + AssetLayout.ThiefAkRunOrigins);
        }

        void PrepareThiefShotgunRun(byte[] initial)
        {
            UploadDirectionalBanks("ThiefShotgunRun", AssetLayout.ThiefShotgunRunFrames,
                                   AssetLayout.ThiefShotgunRunTable0, AssetLayout.ThiefShotgunRunTable1);
            BinaryFields.Write(initial, Control.ThiefShotgunRunTable0,
                               assetBase + AssetLayout.ThiefShotgunRunTable0);
            BinaryFields.Write(initial, Control.ThiefShotgunRunTable1,
                               assetBase + AssetLayout.ThiefShotgunRunTable1);
            for (int heading = 0; heading < 8; heading++)
                Write(assetBase + AssetLayout.ThiefShotgunRunOrigins + (uint)heading * 4,
                      BitConverter.GetBytes(ThiefShotgunRun.DrawOffsetX(heading)));
            BinaryFields.Write(initial, Control.ThiefShotgunRunOrigins,
                               assetBase + AssetLayout.ThiefShotgunRunOrigins);
        }

        void PrepareThiefShotgunPose(byte[] initial)
        {
            UploadDirectionalBanks("ThiefShotgunPose", AssetLayout.ThiefShotgunPoseFrames,
                                   AssetLayout.ThiefShotgunPoseTable0, AssetLayout.ThiefShotgunPoseTable1);
            BinaryFields.Write(initial, Control.ThiefShotgunPoseTable0,
                               assetBase + AssetLayout.ThiefShotgunPoseTable0);
            BinaryFields.Write(initial, Control.ThiefShotgunPoseTable1,
                               assetBase + AssetLayout.ThiefShotgunPoseTable1);
        }

        void PreparePoliceIdle()
        {
            for (int heading = 0; heading < 8; heading++)
            {
                uint pixels =
                    assetBase + AssetLayout.PoliceIdleFrames + (uint)heading * AssetLayout.IdleBankStride;
                Write(pixels, Sprite("TypeGameWASD.PoliceIdle" + heading));
                Write(assetBase + AssetLayout.PoliceCardinalIdle + (uint)heading * 4,
                      BitConverter.GetBytes(pixels));
            }
        }

        void PrepareLayeredIdle(byte[] initial, string gameResources)
        {
            uint[] policeTables = { AssetLayout.PoliceCardinalIdle, AssetLayout.PoliceArmedIdleTable,
                                    AssetLayout.PoliceShotgunIdleTable, AssetLayout.PoliceShieldIdleTable };
            uint[] thiefTables = { AssetLayout.ThiefCardinalIdle, AssetLayout.ThiefArmedIdleTable,
                                   AssetLayout.ThiefShotgunIdleTable };
            using (var originals = new GameBitmapResources(gameResources))
            {
                int bank = 0;
                foreach (IdleAnimation.Sheet sheet in IdleAnimation.Build(
                             name => Sprite("TypeGameWASD." + name), originals.Read))
                {
                    uint table =
                        sheet.Role == 0 ? policeTables[sheet.Equipment] : thiefTables[sheet.Equipment];
                    uint pixels =
                        assetBase + AssetLayout.IdleFrames + (uint)bank++ * AssetLayout.IdleBankStride;
                    Write(pixels, sheet.Pixels);
                    Write(assetBase + table + (uint)sheet.Heading * 4, BitConverter.GetBytes(pixels));
                }
            }
            BinaryFields.Write(initial, Control.PoliceCardinalIdle,
                               assetBase + AssetLayout.PoliceCardinalIdle);
            BinaryFields.Write(initial, Control.ThiefCardinalIdle, assetBase + AssetLayout.ThiefCardinalIdle);
        }
    }
}
