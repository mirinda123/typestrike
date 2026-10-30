using System;
using System.IO;

namespace TypeGameMod
{
    internal sealed class RoleSkill
    {
        internal const double Capacity = 6, SupplyCost = 3, BlinkCooldown = .35;
        internal readonly int Role;
        // Cycle counts accepted words, independently of shield use or resource spending.
        internal uint Cycle, BlinkSerial;
        internal double Energy, NextBlink, FlashUntil;
        internal bool Shield;
        double updatedAt;
        internal float BlinkX, BlinkY, GhostX, GhostY;
        internal byte BlinkHeading;
        static readonly string[] ChargeWords = { "shadow",  "river",  "silver", "forest", "rocket", "silent",
                                                 "bridge",  "planet", "window", "garden", "orange", "castle",
                                                 "thunder", "winter", "travel", "pocket", "hunter", "escape",
                                                 "bright",  "market", "danger", "spring", "little", "faster",
                                                 "signal",  "street", "purple", "yellow", "secret", "shield",
                                                 "copper",  "island" };
        static readonly int[] BlinkDirectionX = { 0, 1, 1, 1, 0, -1, -1, -1 };
        static readonly int[] BlinkDirectionY = { -1, -1, 0, 1, 1, 1, 0, -1 };
        internal const double BlinkDistanceX = 210, BlinkDistanceY = 105;
        internal RoleSkill(int role)
        {
            Role = role;
        }

        internal bool Full
        {
            get {
                return Energy >= Capacity;
            }
        }

        internal bool Ready
        {
            get {
                return Role == 0 ? Energy > 0 : Energy >= 1;
            }
        }

        internal bool CanCharge
        {
            get {
                return !Shield && !Full;
            }
        }

        internal bool SupplyReady
        {
            get {
                return !Shield && Energy >= SupplyCost;
            }
        }

        internal string Word
        {
            get {
                return ChargeWords[(int)((Cycle * 7u + (uint)(Role * 13)) % (uint)ChargeWords.Length)];
            }
        }

        internal void Reset()
        {
            Cycle = BlinkSerial = 0;
            Energy = updatedAt = NextBlink = FlashUntil = 0;
            Shield = false;
        }

        internal void Tick(double now)
        {
            if (Shield)
                Energy = Math.Max(0, Energy - Math.Max(0, now - updatedAt));
            updatedAt = now;
            if (Energy == 0)
                Shield = false;
        }

        internal bool Match(string word, double now)
        {
            Tick(now);
            if (!CanCharge || !String.Equals(Word, word, StringComparison.Ordinal))
                return false;
            Energy = Math.Min(Capacity, Energy + 1);
            Cycle++;
            return true;
        }

        internal bool Resupply(WeaponAmmo ammo, int weapon, double now)
        {
            Tick(now);
            if (!SupplyReady || !ammo.Supply(weapon, now))
                return false;
            Energy -= SupplyCost;
            return true;
        }

        internal bool Use(DuelPose origin, double now, MovementMap movement, out DuelPose target)
        {
            Tick(now);
            target = origin;
            if (Role != 1 || !origin.Valid || !Ready || now < NextBlink)
                return false;
            int heading = origin.Heading & 7;
            int directionX = BlinkDirectionX[heading], directionY = BlinkDirectionY[heading];
            double length = Math.Sqrt(directionX * directionX + directionY * directionY);
            target.X = (float)Math.Max(24, Math.Min(1576, origin.X + BlinkDistanceX * directionX / length));
            target.Y = (float)Math.Max(32, Math.Min(1168, origin.Y + BlinkDistanceY * directionY / length));
            if (!movement.Blink(origin, ref target))
                return false;
            Energy -= 1;
            GhostX = origin.X;
            GhostY = origin.Y;
            BlinkX = target.X;
            BlinkY = target.Y;
            BlinkHeading = (byte)heading;
            BlinkSerial++;
            NextBlink = now + BlinkCooldown;
            FlashUntil = now + .28;
            return true;
        }

        internal void Hold(bool held, double now)
        {
            Tick(now);
            Shield = Role == 0 && held && Ready;
        }

        internal int Remaining(double now)
        {
            Tick(now);
            return (int)Math.Ceiling(Energy * 1000);
        }

        internal void Write(BinaryWriter writer, double now)
        {
            Tick(now);
            writer.Write(Cycle);
            writer.Write(Energy);
            writer.Write(Shield);
            writer.Write(BlinkSerial);
            writer.Write(BlinkX);
            writer.Write(BlinkY);
            writer.Write(GhostX);
            writer.Write(GhostY);
            writer.Write(BlinkHeading);
            writer.Write((ushort)Math.Max(0, Math.Min(280, Math.Ceiling((FlashUntil - now) * 1000))));
            writer.Write((ushort)Math.Max(0, Math.Min(350, Math.Ceiling((NextBlink - now) * 1000))));
        }

        internal static RoleSkill Read(BinaryReader reader, int role, double now)
        {
            var skill = new RoleSkill(role);
            skill.Cycle = reader.ReadUInt32();
            skill.Energy = reader.ReadDouble();
            skill.Shield = reader.ReadBoolean();
            skill.BlinkSerial = reader.ReadUInt32();
            skill.BlinkX = reader.ReadSingle();
            skill.BlinkY = reader.ReadSingle();
            skill.GhostX = reader.ReadSingle();
            skill.GhostY = reader.ReadSingle();
            skill.BlinkHeading = reader.ReadByte();
            int flash = reader.ReadUInt16(), cooldown = reader.ReadUInt16();
            if (!(skill.Energy >= 0 && skill.Energy <= Capacity) ||
                (role == 1 && skill.Energy != Math.Floor(skill.Energy)) ||
                (skill.Shield && (role != 0 || skill.Energy == 0)) || flash > 280 || cooldown > 350 ||
                skill.BlinkHeading > 7 || !InWorld(skill.BlinkX, 1600) || !InWorld(skill.BlinkY, 1200) ||
                !InWorld(skill.GhostX, 1600) || !InWorld(skill.GhostY, 1200))
                throw new ArgumentException("Invalid skill energy state");
            skill.updatedAt = now;
            skill.FlashUntil = now + flash / 1000.0;
            skill.NextBlink = now + cooldown / 1000.0;
            return skill;
        }

        static bool InWorld(float value, int maximum)
        {
            return value >= 0 && value <= maximum;
        }
    }
}
