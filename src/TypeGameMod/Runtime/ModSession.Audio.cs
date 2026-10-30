using System;
using System.IO;

namespace TypeGameMod
{
    internal sealed partial class ModSession
    {
        CombatAudio combatAudio;
        readonly uint[,] heardShots = new uint[2, 3], heardReloads = new uint[2, 3];
        void ResetCombatAudio()
        {
            Array.Clear(heardShots, 0, heardShots.Length);
            Array.Clear(heardReloads, 0, heardReloads.Length);
        }

        void UpdateCombatAudio(bool active, bool focused, WeaponAmmo first, WeaponAmmo second,
                               byte[] snapshot)
        {
            if (combatAudio == null && active)
                combatAudio = new CombatAudio(
                    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "audio", "weapons"), log);
            if (combatAudio != null)
                combatAudio.Enabled = active && focused;
            WeaponAmmo[] states = { first, second };
            for (int role = 0; role < 2; role++)
                if (states[role] != null)
                    for (int weapon = 1; weapon <= 2; weapon++)
                    {
                        WeaponAmmo ammo = states[role];
                        uint fire = ammo.Fired[weapon], reload = ammo.Reloads[weapon];
                        if (combatAudio != null && active && focused)
                        {
                            if (fire > heardShots[role, weapon])
                                for (uint n = 0; n < Math.Min(4u, fire - heardShots[role, weapon]); n++)
                                {
                                    combatAudio.Play(weapon - 1);
                                    if (music != null)
                                        music.Punch();
                                }
                            if (reload > heardReloads[role, weapon])
                                combatAudio.Play(weapon + 1);
                        }
                        heardShots[role, weapon] = fire;
                        heardReloads[role, weapon] = reload;
                    }
            if (combatAudio != null)
            {
                BinaryFields.Write(snapshot, SnapshotLayout.AudioTelemetry, (uint)combatAudio.Queued);
                BinaryFields.Write(snapshot, 1444, (uint)combatAudio.Started);
                BinaryFields.Write(snapshot, 1448, (uint)combatAudio.LastError);
                BinaryFields.Write(snapshot, 1452, (uint)combatAudio.Ready);
                BinaryFields.Write(snapshot, 1456, (uint)combatAudio.Voices);
            }
        }

        void StopCombatAudio()
        {
            if (combatAudio != null)
            {
                combatAudio.Dispose();
                combatAudio = null;
            }
        }
    }
}
