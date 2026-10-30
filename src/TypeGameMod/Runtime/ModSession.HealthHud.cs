using System;

namespace TypeGameMod
{
    internal sealed partial class ModSession
    {
        void PrepareHealthHud(byte[] initial)
        {
            Write(assetBase + AssetLayout.Hearts, HealthHud.Build());
            BinaryFields.Write(initial, Control.HeartSpans, assetBase + AssetLayout.Hearts);
            BinaryFields.Write(initial, Control.SetDCBrushColor,
                               RemoteFunction("gdi32.dll", "SetDCBrushColor"));
            BinaryFields.Write(initial, Control.FillRect, RemoteFunction("user32.dll", "FillRect"));
            BinaryFields.Write(initial, Control.GetStockObject,
                               RemoteFunction("gdi32.dll", "GetStockObject"));
            PrepareWeaponHud(initial);
        }

        void HealthSnapshot(byte[] snapshot, bool active, double now)
        {
            if (!active)
                return;
            DuelHealth health = networkMatch == null ? offlineMatch.Health : networkMatch.Health;
            for (int role = 0; role < 2; role++)
            {
                BinaryFields.Write(snapshot, SnapshotLayout.PoliceHearts + role * 4,
                                   (uint)health.Hearts[role]);
                int ms = health.Remaining(role, now);
                BinaryFields.Write(snapshot, SnapshotLayout.PoliceInvulnerable + role * 4,
                                   ms > 0 && (ms / 80) % 2 == 0 ? 1u : 0u);
            }
        }
    }
}
