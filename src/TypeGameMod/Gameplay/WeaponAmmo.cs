using System;
using System.IO;

namespace TypeGameMod
{
    internal sealed class WeaponAmmo
    {
        internal const double AkInterval = .18, ShotgunInterval = .70;
        internal readonly int[] Rounds = new int[GameRules.WeaponSlots];
        internal readonly int[] Reserve = new int[GameRules.WeaponSlots];
        internal readonly uint[] Fired = new uint[GameRules.WeaponSlots];
        internal readonly uint[] Reloads = new uint[GameRules.WeaponSlots];
        internal int Equipped, Reloading;
        double reloadEndsAt;
        double nextShotAt = -100;
        internal static int Capacity(int weapon)
        {
            return weapon == GameRules.Ak47 ? 30 : weapon == GameRules.Shotgun ? 8 : 0;
        }

        internal static int ReserveCapacity(int weapon)
        {
            return 3 * Capacity(weapon);
        }

        internal bool CanSupply(int weapon)
        {
            return GameRules.IsGun(weapon) &&
                   (Rounds[weapon] < Capacity(weapon) || Reserve[weapon] < ReserveCapacity(weapon));
        }

        internal static double ReloadTime(int weapon)
        {
            return weapon == GameRules.Ak47 ? 1.8 : weapon == GameRules.Shotgun ? 2.4 : 0;
        }

        internal static double Interval(int weapon)
        {
            return weapon == GameRules.Shotgun ? ShotgunInterval : AkInterval;
        }

        internal void Reset()
        {
            Array.Clear(Rounds, 0, Rounds.Length);
            Array.Clear(Reserve, 0, Reserve.Length);
            Equipped = Reloading = 0;
            reloadEndsAt = 0;
            nextShotAt = -100;
            Array.Clear(Fired, 0, Fired.Length);
            Array.Clear(Reloads, 0, Reloads.Length);
        }

        internal void Tick(double now)
        {
            if (Reloading != 0 && now + 0.0000001 >= reloadEndsAt)
            {
                int count = Math.Min(Capacity(Reloading) - Rounds[Reloading], Reserve[Reloading]);
                Rounds[Reloading] += count;
                Reserve[Reloading] -= count;
                Reloading = 0;
                reloadEndsAt = 0;
            }
        }

        internal void Select(int weapon, double now)
        {
            if (weapon < 0 || weapon > 2)
                return;
            if (weapon != Equipped)
            {
                Reloading = 0;
                reloadEndsAt = 0;
                Equipped = weapon;
            }
            Tick(now);
        }

        internal bool Reload(int weapon, double now)
        {
            Select(weapon, now);
            if (weapon < 1 || weapon > 2 || Reloading != 0 || Rounds[weapon] >= Capacity(weapon) ||
                Reserve[weapon] == 0)
                return false;
            Reloading = weapon;
            reloadEndsAt = now + ReloadTime(weapon);
            Reloads[weapon]++;
            return true;
        }
        // E grants one magazine immediately. Fill the gun first, then store the
        // remainder in reserve. R remains the ordinary timed reload.
        internal bool Supply(int weapon, double now)
        {
            Tick(now);
            if (!CanSupply(weapon))
                return false;
            int grant = Capacity(weapon), loaded = Math.Min(grant, Capacity(weapon) - Rounds[weapon]);
            Rounds[weapon] += loaded;
            Reserve[weapon] = Math.Min(ReserveCapacity(weapon), Reserve[weapon] + grant - loaded);
            if (Reloading == weapon)
            {
                Reloading = 0;
                reloadEndsAt = 0;
            }
            return true;
        }

        internal bool CanFire(int weapon, double now)
        {
            Tick(now);
            return GameRules.IsGun(weapon) && Reloading == 0 && Rounds[weapon] > 0 &&
                   now + 0.0000001 >= nextShotAt;
        }

        internal bool CanRequest(int weapon, double now)
        {
            Tick(now);
            return GameRules.IsGun(weapon) && Reloading == 0 && Rounds[weapon] > 0;
        }

        internal bool Fire(int weapon, double now)
        {
            return Fire(weapon, now, 0);
        }

        internal bool Fire(int weapon, double now, double jitter)
        {
            Select(weapon, now);
            if (!CanRequest(weapon, now) || now + Math.Min(.045, Math.Max(0, jitter)) + .0000001 < nextShotAt)
                return false;
            // A single transport tick may arrive early, but its credit cannot
            // accumulate or raise the sustained rate. Never reset an early deadline.
            Rounds[weapon]--;
            nextShotAt = Math.Max(now, nextShotAt) + Interval(weapon);
            Fired[weapon]++;
            return true;
        }

        internal int Remaining(double now)
        {
            return Reloading == 0 ? 0 : (int)Math.Max(0, Math.Ceiling((reloadEndsAt - now) * 1000 - .000001));
        }

        internal int Progress(double now)
        {
            if (Reloading == 0)
                return 0;
            double fraction = 1 - Remaining(now) / (ReloadTime(Reloading) * 1000);
            return Math.Max(1, Math.Min(64, 1 + (int)(fraction * 63)));
        }

        internal void Write(BinaryWriter writer, double now)
        {
            Tick(now);
            writer.Write((byte)Equipped);
            writer.Write((byte)Rounds[1]);
            writer.Write((byte)Rounds[2]);
            writer.Write((byte)Reloading);
            writer.Write((byte)Reserve[1]);
            writer.Write((byte)Reserve[2]);
            writer.Write((ushort)Math.Min(2400, Remaining(now)));
            writer.Write(
                (ushort)Math.Max(0, Math.Min(700, Math.Ceiling((nextShotAt - now) * 1000 - .000001))));
            for (int i = 1; i <= 2; i++)
            {
                writer.Write(Fired[i]);
                writer.Write(Reloads[i]);
            }
        }

        internal static WeaponAmmo Read(BinaryReader reader, double now)
        {
            var ammo = new WeaponAmmo();
            ammo.Equipped = reader.ReadByte();
            ammo.Rounds[1] = reader.ReadByte();
            ammo.Rounds[2] = reader.ReadByte();
            ammo.Reloading = reader.ReadByte();
            ammo.Reserve[1] = reader.ReadByte();
            ammo.Reserve[2] = reader.ReadByte();
            int reloadMilliseconds = reader.ReadUInt16(), shotCooldownMilliseconds = reader.ReadUInt16();
            bool validMagazines =
                ammo.Equipped <= 2 && ammo.Rounds[1] <= Capacity(1) && ammo.Rounds[2] <= Capacity(2);
            bool validReload = ammo.Reloading == 0
                                   ? reloadMilliseconds == 0
                                   : ammo.Reloading <= 2 && ammo.Reloading == ammo.Equipped &&
                                         reloadMilliseconds > 0 &&
                                         reloadMilliseconds <= ReloadTime(ammo.Reloading) * 1000 &&
                                         ammo.Rounds[ammo.Reloading] < Capacity(ammo.Reloading);
            if (!validMagazines || !validReload || shotCooldownMilliseconds > 700 ||
                ammo.Reserve[1] > ReserveCapacity(1) || ammo.Reserve[2] > ReserveCapacity(2) ||
                (ammo.Reloading != 0 && ammo.Reserve[ammo.Reloading] == 0))
                throw new FormatException("Invalid magazine state");
            ammo.reloadEndsAt = reloadMilliseconds == 0 ? 0 : now + reloadMilliseconds / 1000.0;
            ammo.nextShotAt = now + shotCooldownMilliseconds / 1000.0;
            for (int i = 1; i <= 2; i++)
            {
                ammo.Fired[i] = reader.ReadUInt32();
                ammo.Reloads[i] = reader.ReadUInt32();
            }
            return ammo;
        }
    }
}
