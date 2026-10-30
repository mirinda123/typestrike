using System;

namespace TypeGameMod
{
    internal sealed partial class ModSession
    {
        void WriteNetworkSnapshot(GameFrame frame, InputFrame input, byte[] snapshot, bool compatible,
                                  bool playing, double now)
        {
            uint scene = frame.Scene, role = frame.Role, gameObject = frame.Object;
            BinaryFields.Write(snapshot, SnapshotLayout.BulletSpeed, (uint)networkMatch.Speed);
            BinaryFields.Write(snapshot, SnapshotLayout.GameObject,
                               scene == GameScene.Playing && compatible ? gameObject : 0u);
            BinaryFields.Write(snapshot, SnapshotLayout.Armed, equippedWeapon != 0 ? 1u : 0u);
            BinaryFields.Write(snapshot, SnapshotLayout.Weapon, (uint)equippedWeapon);
            BinaryFields.Write(snapshot, SnapshotLayout.ShotsFired, networkMatch.ShotsFired);
            BinaryFields.Write(snapshot, SnapshotLayout.Hits, networkMatch.Hits);
            BinaryFields.Write(snapshot, SnapshotLayout.PelletsFired, networkMatch.PelletsFired);
            BinaryFields.Write(snapshot, SnapshotLayout.PoliceX, networkMatch.Poses[0].X);
            BinaryFields.Write(snapshot, SnapshotLayout.PoliceY, networkMatch.Poses[0].Y);
            BinaryFields.Write(snapshot, SnapshotLayout.ThiefX, networkMatch.Poses[1].X);
            BinaryFields.Write(snapshot, SnapshotLayout.ThiefY, networkMatch.Poses[1].Y);
            if (scene == GameScene.Playing && compatible)
            {
                DuelPose own = networkMatch.Poses[role];
                int cameraX = (int)Math.Max(0, Math.Min(800, Math.Round(own.X - 400))),
                    cameraY = (int)Math.Max(0, Math.Min(600, Math.Round(own.Y - 300)));
                BinaryFields.Write(snapshot, SnapshotLayout.PlayerScreenX,
                                   (uint)(int)Math.Round(own.X - cameraX));
                BinaryFields.Write(snapshot, SnapshotLayout.PlayerScreenY,
                                   (uint)(int)Math.Round(own.Y - cameraY));
                int count = 0;
                foreach (DuelShot shot in networkMatch.Shots)
                    count = AppendTrace(snapshot, count, shot.X, shot.Y, shot.DX, shot.DY, cameraX, cameraY);
                BinaryFields.Write(snapshot, SnapshotLayout.LineCount, (uint)count);
                BinaryFields.Write(snapshot, SnapshotLayout.ProjectileCount, (uint)networkMatch.Shots.Count);
                if (networkMatch.Shots.Count != 0)
                {
                    BinaryFields.Write(snapshot, SnapshotLayout.LeadDistance, networkMatch.Shots[0].Distance);
                    BinaryFields.Write(snapshot, SnapshotLayout.LeadX, networkMatch.Shots[0].X);
                    BinaryFields.Write(snapshot, SnapshotLayout.LeadY, networkMatch.Shots[0].Y);
                }
            }
            SkillSnapshot(snapshot, role, playing, networkMatch.Poses[role].X, networkMatch.Poses[role].Y,
                          now);
            HealthSnapshot(snapshot, playing, now);
            AmmoSnapshot(snapshot, networkMatch.Armories[role], now);
            UpdateCombatAudio(playing, input.Focused, networkMatch.Armories[0], networkMatch.Armories[1],
                              snapshot);
        }
    }
}
