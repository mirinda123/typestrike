using System;

namespace TypeGameMod
{
    // One buffer belongs to one rendered frame. Every actor appends to it;
    // neither actor clears commands produced by another actor.
    internal sealed class FireEffectBuffer
    {
        internal readonly byte[] Snapshot;
        int count;

        internal FireEffectBuffer(byte[] snapshot)
        {
            Snapshot = snapshot;
        }

        internal void Rect(double x, double y, int radius, uint color)
        {
            int left = Math.Max(0, (int)Math.Round(x) - radius);
            int top = Math.Max(0, (int)Math.Round(y) - radius);
            int right = Math.Min(800, (int)Math.Round(x) + radius + 1);
            int bottom = Math.Min(500, (int)Math.Round(y) + radius + 1);
            if (left >= right || top >= bottom)
                return;
            int at = SnapshotLayout.ShotgunFxRects + count++ * 20;
            BinaryFields.Write(Snapshot, at, (uint)left);
            BinaryFields.Write(Snapshot, at + 4, (uint)top);
            BinaryFields.Write(Snapshot, at + 8, (uint)right);
            BinaryFields.Write(Snapshot, at + 12, (uint)bottom);
            BinaryFields.Write(Snapshot, at + 16, color);
        }

        internal void Finish()
        {
            BinaryFields.Write(Snapshot, SnapshotLayout.ShotgunFxCount, (uint)count);
        }
    }
}
