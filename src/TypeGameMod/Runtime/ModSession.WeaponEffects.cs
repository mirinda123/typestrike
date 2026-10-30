using System;

namespace TypeGameMod
{
    internal sealed partial class ModSession
    {
        readonly ShotgunFireEffect shotgunFireEffect = new ShotgunFireEffect();
        readonly AkFireEffect akFireEffect = new AkFireEffect();
        readonly AkFireEffect policeAkFireEffect = new AkFireEffect();
        readonly ShotgunFireEffect policeShotgunFireEffect = new ShotgunFireEffect();
        void PoliceWeaponEffects(GameFrame frame, FireEffectBuffer effects, bool ak, bool shotgun,
                                 bool moving, uint akShots, uint shotgunShots, double dt)
        {
            byte[] snapshot = effects.Snapshot;
            double x = BitConverter.ToSingle(snapshot, SnapshotLayout.PoliceX);
            double y = BitConverter.ToSingle(snapshot, SnapshotLayout.PoliceY);
            uint heading = frame.HasNetworkPlayers && networkMatch != null
                               ? networkMatch.Poses[0].Heading
                               : BitConverter.ToUInt32(snapshot, SnapshotLayout.Heading);
            double mx, my;
            LocateMuzzle(0, shotgun ? GameRules.Shotgun : GameRules.Ak47, heading, moving, out mx, out my);
            policeAkFireEffect.Update(ak, akShots, x + mx, y + my, heading, dt);
            policeShotgunFireEffect.Update(shotgun, shotgunShots, x + mx, y + my, heading, dt);
            int cameraX, cameraY;
            EffectCamera(snapshot, frame.Role, out cameraX, out cameraY);
            // In the accepted north view the muzzle is behind the body in all
            // idle/run poses (verified against the model). Keep fire off the cap.
            if (heading != 0)
            {
                if (ak)
                    policeAkFireEffect.Append(effects, x + mx, y + my, cameraX, cameraY);
                else if (shotgun)
                    policeShotgunFireEffect.Append(effects, x + mx, y + my, cameraX, cameraY);
            }
            if (frame.Role == 0 && equippedWeapon != 0)
                BinaryFields.Write(snapshot, SnapshotLayout.MuzzleFlash, 0u);
        }

        static void EffectCamera(byte[] snapshot, uint role, out int cameraX, out int cameraY)
        {
            int player = role == 1 ? SnapshotLayout.ThiefX : SnapshotLayout.PoliceX;
            double x = BitConverter.ToSingle(snapshot, player),
                   y = BitConverter.ToSingle(snapshot, player + 4);
            cameraX = (int)Math.Max(0, Math.Min(800, Math.Round(x - 400)));
            cameraY = (int)Math.Max(0, Math.Min(600, Math.Round(y - 300)));
        }

        void ThiefWeaponEffects(GameFrame frame, FireEffectBuffer effects, bool ak, bool shotgun, bool moving,
                                uint akShots, uint shotgunShots, double dt)
        {
            byte[] snapshot = effects.Snapshot;
            double x = BitConverter.ToSingle(snapshot, SnapshotLayout.ThiefX);
            double y = BitConverter.ToSingle(snapshot, SnapshotLayout.ThiefY);
            uint heading = frame.HasNetworkPlayers && networkMatch != null
                               ? networkMatch.Poses[1].Heading
                               : BitConverter.ToUInt32(snapshot, SnapshotLayout.Heading);
            double mx, my;
            LocateMuzzle(1, shotgun ? GameRules.Shotgun : GameRules.Ak47, heading, moving, out mx, out my);
            akFireEffect.Update(ak, akShots, x + mx, y + my, heading, dt);
            shotgunFireEffect.Update(shotgun, shotgunShots, x + mx, y + my, heading, dt);
            uint role = frame.Role;
            int cameraX, cameraY;
            EffectCamera(snapshot, role, out cameraX, out cameraY);
            // Both weapons reuse the existing pixel-effect snapshot channel.
            // Updating both counters prevents old effects replaying on a switch.
            if (ak)
                akFireEffect.Append(effects, x + mx, y + my, cameraX, cameraY);
            else if (shotgun)
                shotgunFireEffect.Append(effects, x + mx, y + my, cameraX, cameraY);
            if (role == 1 && equippedWeapon != 0)
                BinaryFields.Write(snapshot, SnapshotLayout.MuzzleFlash, 0u);
        }
    }
}
