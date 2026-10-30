using System;

namespace TypeGameMod
{
    internal sealed partial class ModSession
    {
        // Native 800x600 view: gameplay traces stay above the original article.
        static int AppendTrace(byte[] snapshot, int count, double x, double y, double dx, double dy,
                               int cameraX, int cameraY)
        {
            double x1 = x - cameraX, y1 = y - cameraY, x0 = x1 - dx * 14, y0 = y1 - dy * 14;
            if (!Ballistics.ClipToViewport(ref x0, ref y0, ref x1, ref y1))
                return count;
            int at = SnapshotLayout.ProjectileLines + count * 16;
            BinaryFields.Write(snapshot, at, (uint)(int)Math.Round(x0));
            BinaryFields.Write(snapshot, at + 4, (uint)(int)Math.Round(y0));
            BinaryFields.Write(snapshot, at + 8, (uint)(int)Math.Round(x1));
            BinaryFields.Write(snapshot, at + 12, (uint)(int)Math.Round(y1));
            return count + 1;
        }

        void WriteOfflineView(byte[] snapshot, InputFrame input, double x, double y, double muzzleX,
                              double muzzleY, double now)
        {
            int cameraX = (int)Math.Max(0, Math.Min(800, Math.Round(x - 400)));
            int cameraY = (int)Math.Max(0, Math.Min(600, Math.Round(y - 300)));
            BinaryFields.Write(snapshot, SnapshotLayout.PlayerScreenX, (uint)(int)Math.Round(x - cameraX));
            BinaryFields.Write(snapshot, SnapshotLayout.PlayerScreenY, (uint)(int)Math.Round(y - cameraY));
            BinaryFields.Write(snapshot, SnapshotLayout.MuzzleScreenX,
                               (uint)(int)Math.Round(x - cameraX + muzzleX));
            BinaryFields.Write(snapshot, SnapshotLayout.MuzzleScreenY,
                               (uint)(int)Math.Round(y - cameraY + muzzleY));
            int count = 0;
            foreach (OfflineMatch.Projectile shot in offlineMatch.Projectiles)
                count = AppendTrace(snapshot, count, shot.X, shot.Y, shot.DX, shot.DY, cameraX, cameraY);
            BinaryFields.Write(snapshot, SnapshotLayout.LineCount, (uint)count);
            BinaryFields.Write(snapshot, SnapshotLayout.MuzzleFlash,
                               input.Focused && now < offlineMatch.FlashUntil ? 1u : 0u);
            BinaryFields.Write(snapshot, SnapshotLayout.ProjectileCount,
                               (uint)offlineMatch.Projectiles.Count);
            if (offlineMatch.Projectiles.Count == 0)
                return;
            OfflineMatch.Projectile lead = offlineMatch.Projectiles[0];
            BinaryFields.Write(snapshot, SnapshotLayout.LeadDistance, (float)lead.Distance);
            BinaryFields.Write(snapshot, SnapshotLayout.LeadX, (float)lead.X);
            BinaryFields.Write(snapshot, SnapshotLayout.LeadY, (float)lead.Y);
        }
    }
}
