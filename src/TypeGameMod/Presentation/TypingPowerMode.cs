using System;
using System.Collections.Generic;

namespace TypeGameMod
{
    // Local visual feedback only. Native article acceptance remains the source of charge.
    internal sealed class TypingPowerMode
    {
        internal const int MaximumParticles = 32;
        sealed class Ember
        {
            internal double Born, Life, X, Y, VX, VY;
            internal int Size;
        }

        readonly List<Ember> embers = new List<Ember>();
        readonly Random random = new Random(0x1998);
        double lastStroke = -100;
        int combo;
        internal void Clear()
        {
            embers.Clear();
            lastStroke = -100;
            combo = 0;
        }

        internal void Stroke(bool accepted, double now)
        {
            combo = now - lastStroke > .65 ? 1 : Math.Min(24, combo + 1);
            lastStroke = now;
            int count = accepted ? 7 : 3;
            while (embers.Count + count > MaximumParticles)
                embers.RemoveAt(0);
            for (int i = 0; i < count; i++)
                embers.Add(new Ember { Born = now, Life = .24 + random.NextDouble() * .28,
                                       X = -18 + random.Next(23), Y = 531 + random.Next(7),
                                       VX = -14 + random.Next(29), VY = -65 - random.Next(85),
                                       Size = 2 + random.Next(3) });
        }

        internal void WriteSnapshot(byte[] snapshot, double now)
        {
            double age = now - lastStroke;
            int shake = age >= 0 && age < .13 ? Math.Max(1, (int)((2 + combo / 8.0) * (1 - age / .13))) : 0;
            int phase = (int)(now * 90);
            BinaryFields.Write(snapshot, SnapshotLayout.ShakeX,
                               unchecked((uint)(shake * (phase % 2 == 0 ? 1 : -1))));
            BinaryFields.Write(snapshot, SnapshotLayout.ShakeY,
                               unchecked((uint)(shake * (phase % 3 == 0 ? 1 : -1))));
            for (int i = embers.Count - 1; i >= 0; i--)
                if (now - embers[i].Born >= embers[i].Life)
                    embers.RemoveAt(i);
            int count = 0;
            foreach (Ember ember in embers)
            {
                double t = Math.Max(0, now - ember.Born), life = t / ember.Life;
                int x = (int)Math.Round(ember.X + ember.VX * t);
                int y = (int)Math.Round(ember.Y + ember.VY * t);
                int size = life > .65 ? 2 : ember.Size;
                // COLORREF (BGR): hot cream near the glyph, orange and red as it rises.
                uint colour = life < .20 ? 0x00b8f5ffu : life < .52 ? 0x0030b8ffu : 0x00243df0u;
                int at = SnapshotLayout.FireRects + count++ * 20;
                BinaryFields.Write(snapshot, at, unchecked((uint)x));
                BinaryFields.Write(snapshot, at + 4, (uint)y);
                BinaryFields.Write(snapshot, at + 8, unchecked((uint)(x + size)));
                BinaryFields.Write(snapshot, at + 12, (uint)(y + size * 2));
                BinaryFields.Write(snapshot, at + 16, colour);
            }
            BinaryFields.Write(snapshot, SnapshotLayout.FireCount, (uint)count);
        }
    }
}
