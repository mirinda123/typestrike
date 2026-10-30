using System;

namespace TypeGameMod
{
    internal sealed partial class ModSession
    {
        void PrepareAmmoHud(byte[] initial)
        {
            Write(assetBase + AssetLayout.Ammo, AmmoHud.Build());
            Write(assetBase + AssetLayout.Reload, AmmoHud.BuildReload());
            Write(assetBase + AssetLayout.Reserve, AmmoHud.BuildReserve());
            BinaryFields.Write(initial, Control.AmmoSpans, assetBase + AssetLayout.Ammo);
            BinaryFields.Write(initial, Control.ReloadSpans, assetBase + AssetLayout.Reload);
            BinaryFields.Write(initial, Control.ReserveSpans, assetBase + AssetLayout.Reserve);
            Write(assetBase + AssetLayout.EnergyHud, EnergyHud.Build());
            BinaryFields.Write(initial, Control.EnergyHudSpans, assetBase + AssetLayout.EnergyHud);
            Write(assetBase + AssetLayout.EnergyShine, EnergyHud.BuildShine());
            BinaryFields.Write(initial, Control.EnergyShineSpans, assetBase + AssetLayout.EnergyShine);
            Write(assetBase + AssetLayout.TypingArt, TypingPrompt.Build());
            BinaryFields.Write(initial, Control.TypingArtInfo, assetBase + AssetLayout.TypingArt);
            BinaryFields.Write(initial, Control.TypingArtPixels,
                               assetBase + AssetLayout.TypingArt + TypingPrompt.HeaderBytes);
            BinaryFields.Write(initial, Control.StretchDIBits, RemoteFunction("gdi32.dll", "StretchDIBits"));
            Write(assetBase + AssetLayout.ResultHud, ResultHud.Build());
            BinaryFields.Write(initial, Control.ResultHintSpans, assetBase + AssetLayout.ResultHud);
        }
    }
}
