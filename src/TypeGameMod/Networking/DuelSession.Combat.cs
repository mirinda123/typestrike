using System;
using System.Collections.Generic;

namespace TypeGameMod
{
    internal sealed partial class DuelSession
    {
        const int MaximumPendingFireActions = 8;
        struct ShotContact
        {
            internal double Fraction;
            internal int Owner;
        }

        bool HasPendingSupply
        {
            get {
                return pendingSkillActions.Exists(action => action.Kind == SkillAction.SupplyUse);
            }
        }

        bool CanQueueSkillAction
        {
            get {
                return Host || pendingSkillActions.Count < DuelProtocol.MaximumSkillActions;
            }
        }

        void ProcessLocalActions(DuelPose pose, bool reload, bool fire, bool supply, double now)
        {
            bool waitingSupply = HasPendingSupply;
            bool blinkUsed = TryUseLocalBlink(pose, waitingSupply, now);
            if (Skills[Role].Shield)
                fire = false;
            // Blink can change the pose, so equipment and firing use its result.
            pose = Poses[Role];
            Armories[Role].Select(pose.Weapon, now);
            if (supply && !blinkUsed)
                RequestSupply(pose, waitingSupply, now);
            if (reload)
                RequestReload(pose, now);
            if (fire && !reload)
                RequestShot(pose, now);
        }

        bool TryUseLocalBlink(DuelPose pose, bool waitingSupply, double now)
        {
            if (Role != 1 || !shiftPressed || waitingSupply || !CanQueueSkillAction)
                return false;
            var action = new SkillAction { Id = skillSequence + 1, Round = Round, Cycle = Skills[Role].Cycle,
                                           Kind = SkillAction.RoleUse, Origin = pose };
            if (!ApplySkillAction(action, Role, now))
                return false;
            skillSequence++;
            if (!Host)
                pendingSkillActions.Add(action);
            return true;
        }

        void RequestSupply(DuelPose pose, bool waitingSupply, double now)
        {
            if (!Skills[Role].SupplyReady || waitingSupply || !CanQueueSkillAction ||
                !Armories[Role].CanSupply(pose.Weapon))
                return;
            var action = new SkillAction { Id = ++skillSequence, Round = Round, Cycle = Skills[Role].Cycle,
                                           Kind = SkillAction.SupplyUse, Origin = pose };
            if (Host)
                ApplySkillAction(action, Role, now);
            else
                pendingSkillActions.Add(action);
        }

        void RequestReload(DuelPose pose, double now)
        {
            if (pose.Weapon == GameRules.Unarmed || pendingFireActions.Count >= MaximumPendingFireActions)
                return;
            var action = new DuelFire { Id = ++fireSequence, Round = Round, Pose = pose, Kind = 1 };
            if (Host)
            {
                ApplyFireAction(action, Role, now);
                return;
            }
            WeaponAmmo ammo = Armories[Role];
            if (ammo.Rounds[pose.Weapon] < WeaponAmmo.Capacity(pose.Weapon) && ammo.Reloading == 0 &&
                ammo.Reserve[pose.Weapon] > 0)
                pendingFireActions.Add(action);
        }

        void RequestShot(DuelPose pose, double now)
        {
            bool waitingReload = pendingFireActions.Exists(pending => pending.Kind == 1);
            if (waitingReload || pose.Weapon == GameRules.Unarmed ||
                now - lastFire + .0000001 < WeaponAmmo.Interval(pose.Weapon) ||
                !Armories[Role].CanRequest(pose.Weapon, now) ||
                pendingFireActions.Count >= MaximumPendingFireActions ||
                Shots.Count + GameRules.PelletsPerShot(pose.Weapon) > GameRules.MaxProjectiles)
                return;
            var action = new DuelFire { Id = ++fireSequence, Round = Round, Pose = pose };
            lastFire = now;
            if (Host)
                ApplyFireAction(action, Role, now);
            else
                pendingFireActions.Add(action);
        }

        void AdvanceProjectiles(double now, double dt)
        {
            dt = Math.Max(0, Math.Min(GameRules.MaximumStepSeconds, dt));
            var contacts = new List<ShotContact>();
            for (int index = Shots.Count - 1; index >= 0; index--)
            {
                DuelShot shot = Shots[index];
                float fromX = shot.X, fromY = shot.Y;
                double remainingRange = Math.Max(0, GameRules.ProjectileRange(shot.Weapon) - shot.Distance);
                double travel = Math.Min(Speed * dt, remainingRange);
                shot.X += (float)(shot.DX * travel);
                shot.Y += (float)(shot.DY * travel);
                shot.Distance += (float)travel;
                shot.Age += (float)dt;
                int targetRole = 1 - shot.Owner;
                DuelPose target = Poses[targetRole], previous = previousPoses[targetRole];
                double contactFraction;
                ProjectileImpact impact =
                    Ballistics.FirstImpact(cover, fromX, fromY, shot.X, shot.Y, previous.X, previous.Y,
                                           target.X, target.Y, out contactFraction);
                if (impact == ProjectileImpact.Cover)
                {
                    Shots.RemoveAt(index);
                    continue;
                }
                if (impact == ProjectileImpact.Body)
                {
                    Shots.RemoveAt(index);
                    if (targetRole == 0 && Skills[0].Shield)
                        Blocks++;
                    else
                        contacts.Add(new ShotContact { Fraction = contactFraction, Owner = shot.Owner });
                    continue;
                }
                if (shot.Distance >= GameRules.ProjectileRange(shot.Weapon) ||
                    shot.Age > GameRules.ProjectileLifetime(shot.Weapon) || shot.X < 0 ||
                    shot.X > GameRules.WorldWidth || shot.Y < 0 || shot.Y > GameRules.WorldHeight)
                    Shots.RemoveAt(index);
            }
            previousPoses[0] = Poses[0];
            previousPoses[1] = Poses[1];
            ApplyProjectileContacts(contacts, now);
        }

        void ApplyProjectileContacts(List<ShotContact> contacts, double now)
        {
            // Resolve same-tick contacts chronologically, then by role for ties.
            contacts.Sort(delegate(ShotContact first, ShotContact second) {
                int order = first.Fraction.CompareTo(second.Fraction);
                return order != 0 ? order : first.Owner.CompareTo(second.Owner);
            });
            foreach (ShotContact contact in contacts)
            {
                int targetRole = 1 - contact.Owner;
                if (!Health.Hit(targetRole, now))
                    continue;
                Hits++;
                if (Health.Hearts[targetRole] != 0)
                    continue;
                Winner = contact.Owner;
                Shots.Clear();
                RequestedScene = Winner == Role ? GameScene.Victory : GameScene.Defeat;
                break;
            }
        }

        bool ApplyFireAction(DuelFire action, int owner, double now)
        {
            // Equipment comes from pose updates. Delayed fire cannot switch it.
            if (action.Pose.Weapon != Poses[owner].Weapon)
                return false;
            WeaponAmmo ammo = Armories[owner];
            if (action.Kind == 1)
                return ammo.Reload(action.Pose.Weapon, now);
            if (Skills[owner].Shield ||
                Shots.Count + GameRules.PelletsPerShot(action.Pose.Weapon) > GameRules.MaxProjectiles ||
                !ammo.Fire(action.Pose.Weapon, now, owner == Role ? 0 : .045))
                return false;
            SpawnProjectiles(action, owner);
            return true;
        }

        void SpawnProjectiles(DuelFire action, int owner)
        {
            DuelPose pose = action.Pose;
            int pelletCount = GameRules.PelletsPerShot(pose.Weapon);
            ShotOrigin origin = locateShot(owner, pose);
            for (int pellet = 0; pellet < pelletCount; pellet++)
            {
                double dx, dy;
                Ballistics.ProjectileDirection(pose.Weapon, pose.Heading, pellet, out dx, out dy);
                Shots.Add(new DuelShot { X = (float)origin.X, Y = (float)origin.Y, DX = (float)dx,
                                         DY = (float)dy, Owner = (byte)owner, Weapon = pose.Weapon });
            }
            ShotsFired++;
            PelletsFired += (uint)pelletCount;
        }
    }
}
