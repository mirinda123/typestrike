using System;
using System.Collections.Generic;

namespace TypeGameMod
{
    // Pure single-player combat. Native memory and rendering belong to ModSession.
    internal sealed class OfflineMatch
    {
        internal sealed class Projectile
        {
            internal double X, Y, DX, DY, Age, Distance;
            internal readonly double Range, Lifetime;
            internal Projectile(double x, double y, double dx, double dy, bool shotgun)
            {
                X = x;
                Y = y;
                DX = dx;
                DY = dy;
                Range = shotgun ? GameRules.ShotgunRange : double.PositiveInfinity;
                Lifetime = shotgun ? GameRules.ShotgunProjectileLifetime : GameRules.AkProjectileLifetime;
            }
        }

        internal readonly WeaponAmmo Ammo = new WeaponAmmo();
        internal readonly DuelHealth Health = new DuelHealth();
        internal readonly List<Projectile> Projectiles = new List<Projectile>();
        internal int Weapon;
        internal uint ShotsFired, Hits, PelletsFired;
        internal double FlashUntil;
        double previousTargetX, previousTargetY;
        readonly MapCover cover;
        internal OfflineMatch() : this(MapCover.City)
        {
        }

        internal OfflineMatch(MapCover cover)
        {
            this.cover = cover;
        }

        internal void Reset(double targetX, double targetY)
        {
            Ammo.Reset();
            Health.Reset();
            Clear();
            ShotsFired = Hits = PelletsFired = 0;
            previousTargetX = targetX;
            previousTargetY = targetY;
        }

        internal void Clear()
        {
            Projectiles.Clear();
            Weapon = 0;
            FlashUntil = 0;
        }

        internal bool Select(int weapon, double now)
        {
            bool changed = weapon != Weapon;
            if (changed)
            {
                Projectiles.Clear();
                FlashUntil = 0;
                Weapon = weapon;
            }
            Ammo.Select(weapon, now);
            return changed;
        }

        internal bool Fire(uint heading, double muzzleX, double muzzleY, double now)
        {
            int pelletCount = GameRules.PelletsPerShot(Weapon);
            if (Weapon == GameRules.Unarmed || Projectiles.Count + pelletCount > GameRules.MaxProjectiles ||
                !Ammo.Fire(Weapon, now))
                return false;
            for (int pellet = 0; pellet < pelletCount; pellet++)
            {
                double dx, dy;
                Ballistics.ProjectileDirection(Weapon, heading, pellet, out dx, out dy);
                Projectiles.Add(new Projectile(muzzleX, muzzleY, dx, dy, Weapon == GameRules.Shotgun));
            }
            ShotsFired++;
            PelletsFired += (uint)pelletCount;
            FlashUntil = now + .045;
            return true;
        }

        internal bool Advance(int targetRole, double targetX, double targetY, int speed, double now,
                              double dt)
        {
            bool defeated = false;
            for (int i = Projectiles.Count - 1; i >= 0; i--)
            {
                Projectile shot = Projectiles[i];
                double oldX = shot.X, oldY = shot.Y;
                double travel = Math.Min(speed * dt, Math.Max(0, shot.Range - shot.Distance));
                shot.X += shot.DX * travel;
                shot.Y += shot.DY * travel;
                shot.Distance += travel;
                shot.Age += dt;
                double contact;
                ProjectileImpact impact =
                    Ballistics.FirstImpact(cover, oldX, oldY, shot.X, shot.Y, previousTargetX,
                                           previousTargetY, targetX, targetY, out contact);
                if (impact == ProjectileImpact.Cover)
                    Projectiles.RemoveAt(i);
                else if (impact == ProjectileImpact.Body)
                {
                    Projectiles.RemoveAt(i);
                    if (Health.Hit(targetRole, now))
                    {
                        Hits++;
                        defeated = Health.Hearts[targetRole] == 0;
                    }
                    if (defeated)
                        break;
                }
                else if (shot.Distance >= shot.Range || shot.Age > shot.Lifetime || shot.X < 0 ||
                         shot.X > GameRules.WorldWidth || shot.Y < 0 || shot.Y > GameRules.WorldHeight)
                    Projectiles.RemoveAt(i);
            }
            previousTargetX = targetX;
            previousTargetY = targetY;
            if (defeated)
                Projectiles.Clear();
            return defeated;
        }

        internal void TrackTarget(double x, double y)
        {
            previousTargetX = x;
            previousTargetY = y;
        }
    }
}
