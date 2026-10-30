using System;

namespace TypeGameMod
{
    internal sealed partial class ModSession
    {
        byte[] TickOffline(GameFrame frame, InputFrame input, double now, double dt)
        {
            if (networkMatch != null)
                EndLan();
            if (input.ConfirmResult)
                QueueScene(frame.Object, frame.Scene, GameScene.Playing);
            bool active = frame.OfflinePlaying;
            bool newRound =
                active && (frame.Object != previousFrame.Object || frame.Role != previousFrame.Role ||
                           previousFrame.Scene != GameScene.Playing ||
                           (frame.Started == 0 && previousFrame.Started != 0));
            if (!active || newRound)
            {
                ResetWeaponWheel();
                offlineMatch.Clear();
                equippedWeapon = 0;
                Write(ControlBase + Control.VictoryRequested, new byte[8]);
            }
            if (newRound)
            {
                int target = (1 - (int)frame.Role) * 56;
                offlineMatch.Reset(BitConverter.ToSingle(frame.Players, target + 4),
                                   BitConverter.ToSingle(frame.Players, target + 8));
                ResetOfflineSkill(frame.Role);
                ResetCombatAudio();
            }
            ReadSkillCharacters(active, frame.Object, frame.Role, input, now);
            if (!typing.ControlsAllowed)
            {
                input.X = input.Y = 0;
                input.Fire = false;
                Write(ControlBase + Control.VelocityX, new byte[16]);
            }
            byte[] snapshot = new byte[SnapshotBytes];
            BinaryFields.Write(snapshot, SnapshotLayout.BulletSpeed, (uint)SelectedBulletSpeed);
            BinaryFields.Write(snapshot, SnapshotLayout.GameObject, active ? frame.Object : 0u);
            BinaryFields.Write(snapshot, SnapshotLayout.Heading, input.Heading);
            if (active)
                SimulateOffline(frame, input, snapshot, now, dt);
            BinaryFields.Write(snapshot, SnapshotLayout.Armed, equippedWeapon != 0 ? 1u : 0u);
            BinaryFields.Write(snapshot, SnapshotLayout.Weapon, (uint)equippedWeapon);
            BinaryFields.Write(snapshot, SnapshotLayout.Focused, input.Focused ? 1u : 0u);
            BinaryFields.Write(snapshot, SnapshotLayout.ShotsFired, offlineMatch.ShotsFired);
            BinaryFields.Write(snapshot, SnapshotLayout.Hits, offlineMatch.Hits);
            BinaryFields.Write(snapshot, SnapshotLayout.PelletsFired, offlineMatch.PelletsFired);
            int local = frame.Role == 1 ? SnapshotLayout.ThiefX : SnapshotLayout.PoliceX;
            SkillSnapshot(snapshot, frame.Role, active, BitConverter.ToSingle(snapshot, local),
                          BitConverter.ToSingle(snapshot, local + 4), now);
            HealthSnapshot(snapshot, active, now);
            AmmoSnapshot(snapshot, offlineMatch.Ammo, now);
            UpdateCombatAudio(active, input.Focused, offlineMatch.Ammo, null, snapshot);
            UpdateOfflineStatus(frame);
            return snapshot;
        }

        void SimulateOffline(GameFrame frame, InputFrame input, byte[] snapshot, double now, double dt)
        {
            byte[] players = frame.Players;
            uint role = frame.Role;
            double policeX = BitConverter.ToSingle(players, 4), policeY = BitConverter.ToSingle(players, 8);
            double thiefX = BitConverter.ToSingle(players, 60), thiefY = BitConverter.ToSingle(players, 64);
            double localX = role == 0 ? policeX : thiefX, localY = role == 0 ? policeY : thiefY;
            double targetX = role == 0 ? thiefX : policeX, targetY = role == 0 ? thiefY : policeY;
            uint previousBlink = offlineSkill.BlinkSerial;
            OfflineSkillInput(frame.Object, role, ref localX, ref localY, input.Heading, input, now);
            bool blinkUsed = previousBlink != offlineSkill.BlinkSerial;
            if (role == 1)
            {
                thiefX = localX;
                thiefY = localY;
            }
            bool controls = input.Focused && typing.ControlsAllowed;
            int weapon = ChooseWeapon(controls ? input.Selection : 0, controls);
            if (offlineMatch.Select(weapon, now))
            {
                Write(ControlBase + Control.VictoryRequested, new byte[8]);
                log(weapon == 2 ? "Shotgun equipped" : weapon == 1 ? "AK47 equipped" : "Weapon holstered");
            }
            equippedWeapon = weapon;
            if (input.Focused && input.Slower)
                SelectedBulletSpeed -= GameRules.BulletSpeedStep;
            if (input.Focused && input.Faster)
                SelectedBulletSpeed += GameRules.BulletSpeedStep;
            SelectedBulletSpeed = GameRules.ClampBulletSpeed(SelectedBulletSpeed);
            BinaryFields.Write(snapshot, SnapshotLayout.BulletSpeed, (uint)SelectedBulletSpeed);
            bool idle = input.X == 0 && input.Y == 0;
            double muzzleX, muzzleY;
            LocateMuzzle((int)role, weapon, input.Heading, !idle, out muzzleX, out muzzleY);
            if (controls && input.Reload && offlineMatch.Ammo.Reload(weapon, now))
                log("Reload started weapon=" + weapon);
            if (controls && input.Supply && !blinkUsed &&
                offlineSkill.Resupply(offlineMatch.Ammo, weapon, now))
                log("Resupply used shared charge weapon=" + weapon + " cycle=" + offlineSkill.Cycle);
            if (input.Focused && !offlineSkill.Shield && input.Fire)
                offlineMatch.Fire(input.Heading, localX + muzzleX, localY + muzzleY, now);
            uint beforeHits = offlineMatch.Hits;
            bool defeated =
                input.Focused && weapon != 0 &&
                offlineMatch.Advance(1 - (int)role, targetX, targetY, SelectedBulletSpeed, now, dt);
            offlineMatch.TrackTarget(targetX, targetY);
            if (offlineMatch.Hits != beforeHits)
                log("Damage target=" + (1 - role) + " hearts=" + offlineMatch.Health.Hearts[1 - role]);
            if (defeated)
            {
                byte[] request = new byte[8];
                BinaryFields.Write(request, 0, 1u);
                BinaryFields.Write(request, 4, frame.Object);
                Write(ControlBase + Control.VictoryRequested, request);
                log("Target hearts depleted; requesting native " + (role == 0 ? "police" : "thief") +
                    " victory.");
            }
            WriteOfflineView(snapshot, input, localX, localY, muzzleX, muzzleY, now);
            BinaryFields.Write(snapshot, SnapshotLayout.PoliceX, (float)policeX);
            BinaryFields.Write(snapshot, SnapshotLayout.PoliceY, (float)policeY);
            BinaryFields.Write(snapshot, SnapshotLayout.ThiefX, (float)thiefX);
            BinaryFields.Write(snapshot, SnapshotLayout.ThiefY, (float)thiefY);
        }

        void UpdateOfflineStatus(GameFrame frame)
        {
            if (frame.Scene == GameScene.Playing)
                offlineStatus = (equippedWeapon == 2   ? "霰弹枪 · " + GameRules.ShotgunPellets +
                                                             " 颗散弹 / 射程 " + GameRules.ShotgunRange
                                 : equippedWeapon == 1 ? "AK47 已装备"
                                                       : "1 AK47 / 2 霰弹枪") +
                                " · " + SelectedBulletSpeed + " px/s · 回车射击";
            else if (frame.Scene == GameScene.Victory)
                offlineStatus = frame.Role == 1 ? "小偷胜利！回车再来一局 / Esc 返回。"
                                                : "抓捕成功！回车再来一局 / Esc 返回。";
            else if (frame.Scene == GameScene.Defeat)
                offlineStatus = "本局结束。回车再来一局 / Esc 返回。";
            else
                offlineStatus = "进入 生死时速 → 单人游戏 → 警察或小偷 → 开始";
        }
    }
}
