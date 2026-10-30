using System;
using System.Collections.Generic;

namespace TypeGameMod
{
    internal struct SkillView
    {
        internal RoleSkill Own, Thief;
        internal WeaponAmmo Ammo;
        internal int Weapon;
        internal bool Active, Typing;
        internal ArticleProgress Article;
        internal TypingPowerMode Feedback;
        internal double PlayerX, PlayerY;
    }

    // Encodes a view only. Native skill requests are handled by NativeSkillBridge.
    internal static class SkillHud
    {
        internal static void Write(byte[] snapshot, SkillView view, double now)
        {
            if (!view.Active)
                return;
            EnergyHud.WriteSnapshot(snapshot, view.Own, view.Ammo, view.Weapon, now);
            view.Feedback.WriteSnapshot(snapshot, now);
            BinaryFields.Write(snapshot, SnapshotLayout.Typing, view.Typing ? 1u : 0u);
            if (view.Article != null)
            {
                BinaryFields.Write(snapshot, SnapshotLayout.ArticleLength, (uint)view.Article.Text.Length);
                BinaryFields.Write(snapshot, SnapshotLayout.ArticlePosition, (uint)view.Article.Position);
            }
            WriteBlink(snapshot, view.Thief, view.PlayerX, view.PlayerY, now);
        }

        static void WriteBlink(byte[] snapshot, RoleSkill thief, double x, double y, double now)
        {
            if (thief == null || now >= thief.FlashUntil)
                return;
            int cx = (int)Math.Max(0, Math.Min(800, Math.Round(x - 400))),
                cy = (int)Math.Max(0, Math.Min(600, Math.Round(y - 300)));
            var lines = new List<int[]>();
            foreach (int[] l in BlinkOutline.Lines[thief.BlinkHeading])
                lines.Add(new[] { (int)Math.Round(thief.GhostX) - cx + l[0],
                                  (int)Math.Round(thief.GhostY) - cy + l[1],
                                  (int)Math.Round(thief.GhostX) - cx + l[2],
                                  (int)Math.Round(thief.GhostY) - cy + l[3] });
            for (int i = 0; i < 12; i++)
            {
                double a = i * Math.PI / 6, b = (i + 1) * Math.PI / 6;
                lines.Add(new[] { (int)Math.Round(thief.BlinkX - cx + 36 + 27 * Math.Cos(a)),
                                  (int)Math.Round(thief.BlinkY - cy + 80 + 13 * Math.Sin(a)),
                                  (int)Math.Round(thief.BlinkX - cx + 36 + 27 * Math.Cos(b)),
                                  (int)Math.Round(thief.BlinkY - cy + 80 + 13 * Math.Sin(b)) });
            }
            int count = 0;
            foreach (int[] l in lines)
            {
                double x0 = l[0], y0 = l[1], x1 = l[2], y1 = l[3];
                if (!Ballistics.ClipToViewport(ref x0, ref y0, ref x1, ref y1))
                    continue;
                int at = SnapshotLayout.BlinkLines + count * 16;
                BinaryFields.Write(snapshot, at, (uint)(int)Math.Round(x0));
                BinaryFields.Write(snapshot, at + 4, (uint)(int)Math.Round(y0));
                BinaryFields.Write(snapshot, at + 8, (uint)(int)Math.Round(x1));
                BinaryFields.Write(snapshot, at + 12, (uint)(int)Math.Round(y1));
                if (++count >= 48)
                    break;
            }
            BinaryFields.Write(snapshot, SnapshotLayout.BlinkLineCount, (uint)count);
        }
    }
}
