using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace TypeGameMod
{
    internal static class DuelProtocol
    {
        internal const string Prefix = "TGDUEL19:";
        internal const uint Magic = 0x39444754;
        internal const ushort Version = 15;
        internal const int MaximumSkillActions = 32;
        internal const int MaximumTextLength = 4093;
        internal static void WritePose(BinaryWriter writer, DuelPose pose)
        {
            writer.Write(pose.X);
            writer.Write(pose.Y);
            writer.Write(pose.Heading);
            writer.Write(pose.Weapon);
            writer.Write(pose.Moving);
            writer.Write(pose.Phase);
        }

        static DuelPose ReadPose(BinaryReader reader)
        {
            return new DuelPose { X = reader.ReadSingle(),     Y = reader.ReadSingle(),
                                  Heading = reader.ReadByte(), Weapon = reader.ReadByte(),
                                  Moving = reader.ReadByte(),  Phase = reader.ReadByte() };
        }

        internal static DuelPacket Decode(string text, double now)
        {
            if (text == null || text.Length > MaximumTextLength ||
                !text.StartsWith(Prefix, StringComparison.Ordinal))
                return null;
            byte[] bytes = Convert.FromBase64String(text.Substring(Prefix.Length));
            using (var stream = new MemoryStream(bytes, false))
            {
                using (var reader = new BinaryReader(stream))
                {
                    if (reader.ReadUInt32() != Magic || reader.ReadUInt16() != Version)
                        return null;
                    var packet = new DuelPacket { Host = reader.ReadBoolean(),
                                                  Role = reader.ReadByte(),
                                                  Token = new Guid(reader.ReadBytes(16)),
                                                  AcknowledgedToken = new Guid(reader.ReadBytes(16)),
                                                  Sequence = reader.ReadUInt32(),
                                                  Round = reader.ReadUInt32(),
                                                  Scene = reader.ReadUInt32(),
                                                  Ready = reader.ReadBoolean(),
                                                  Winner = reader.ReadSByte(),
                                                  Speed = reader.ReadUInt16(),
                                                  SpeedSequence = reader.ReadUInt32(),
                                                  FireAck = reader.ReadUInt32(),
                                                  Pose = ReadPose(reader),
                                                  OtherPose = ReadPose(reader) };
                    if (packet.Role > 1 || packet.Token == Guid.Empty || packet.Scene > 5 ||
                        packet.Winner < -1 || packet.Winner > 1 || packet.Speed < 100 ||
                        packet.Speed > 2400 || packet.Speed % GameRules.BulletSpeedStep != 0 ||
                        !packet.Pose.Valid || !packet.OtherPose.Valid)
                        return null;
                    packet.Fires = ReadFires(reader, packet.Host);
                    if (packet.Fires == null)
                        return null;
                    packet.Shots = ReadShots(reader, packet.Host);
                    if (packet.Shots == null)
                        return null;
                    packet.ShiftHeld = reader.ReadBoolean();
                    packet.SkillAck = reader.ReadUInt32();
                    packet.Blocks = reader.ReadUInt32();
                    packet.Skills = new[] { RoleSkill.Read(reader, 0, now), RoleSkill.Read(reader, 1, now) };
                    packet.Health = DuelHealth.Read(reader, now);
                    packet.Hits = reader.ReadUInt32();
                    packet.Armories = new[] { WeaponAmmo.Read(reader, now), WeaponAmmo.Read(reader, now) };
                    if (packet.Host && !ValidWinner(packet))
                        return null;
                    packet.Actions = ReadActions(reader, packet.Host);
                    return packet.Actions != null && stream.Position == stream.Length ? packet : null;
                }
            }
        }

        static List<DuelFire> ReadFires(BinaryReader reader, bool host)
        {
            int count = reader.ReadByte();
            if (count > 8 || (host && count != 0))
                return null;
            var actions = new List<DuelFire>(count);
            for (int i = 0; i < count; i++)
            {
                var action = new DuelFire { Id = reader.ReadUInt32(), Round = reader.ReadUInt32(),
                                            Pose = ReadPose(reader), Kind = reader.ReadByte() };
                if (!action.Pose.Valid || action.Pose.Weapon == 0 || action.Id == 0 || action.Kind > 1)
                    return null;
                actions.Add(action);
            }
            return actions;
        }

        static List<DuelShot> ReadShots(BinaryReader reader, bool host)
        {
            int count = reader.ReadByte();
            if (count > GameRules.MaxProjectiles || (!host && count != 0))
                return null;
            var shots = new List<DuelShot>(count);
            for (int i = 0; i < count; i++)
            {
                var shot = new DuelShot { X = reader.ReadSingle(),   Y = reader.ReadSingle(),
                                          DX = reader.ReadSingle(),  DY = reader.ReadSingle(),
                                          Age = reader.ReadSingle(), Distance = reader.ReadSingle(),
                                          Owner = reader.ReadByte(), Weapon = reader.ReadByte() };
                if (!ValidShot(shot))
                    return null;
                shots.Add(shot);
            }
            return shots;
        }

        static bool InRange(double value, double minimum, double maximum)
        {
            // Bounded comparisons reject both NaN and infinities.
            return value >= minimum && value <= maximum;
        }

        static bool ValidShot(DuelShot shot)
        {
            double norm = shot.DX * shot.DX + shot.DY * shot.DY;
            return InRange(shot.X, -100, 1700) && InRange(shot.Y, -100, 1300) && InRange(norm, .99, 1.01) &&
                   InRange(shot.Age, 0, 4.1) && InRange(shot.Distance, 0, 10000) && shot.Owner <= 1 &&
                   shot.Weapon >= 1 && shot.Weapon <= 2;
        }

        static bool ValidWinner(DuelPacket packet)
        {
            int[] hearts = packet.Health.Hearts;
            return packet.Winner < 0 ? hearts[0] > 0 && hearts[1] > 0
                                     : hearts[1 - packet.Winner] == 0 && hearts[packet.Winner] > 0;
        }

        static List<SkillAction> ReadActions(BinaryReader reader, bool host)
        {
            int count = reader.ReadByte();
            if (count > MaximumSkillActions || (host && count != 0))
                return null;
            var actions = new List<SkillAction>(count);
            uint previousId = 0;
            for (int i = 0; i < count; i++)
            {
                var action = new SkillAction { Id = reader.ReadUInt32(), Round = reader.ReadUInt32(),
                                               Cycle = reader.ReadUInt32(), Kind = reader.ReadByte(),
                                               Origin = ReadPose(reader) };
                int length = reader.ReadByte();
                bool wordAction = action.Kind == SkillAction.RoleWord;
                if (length > 12 || action.Id == 0 || action.Id <= previousId ||
                    action.Kind > SkillAction.ShieldLower || !action.Origin.Valid ||
                    (wordAction ? length == 0 : length != 0))
                    return null;
                byte[] word = reader.ReadBytes(length);
                if (word.Length != length)
                    return null;
                foreach (byte letter in word)
                    if (letter < 97 || letter > 122)
                        return null;
                action.Word = Encoding.ASCII.GetString(word);
                actions.Add(action);
                previousId = action.Id;
            }
            return actions;
        }
    }
}
