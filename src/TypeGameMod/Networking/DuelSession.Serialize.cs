using System;
using System.IO;

namespace TypeGameMod
{
    internal sealed partial class DuelSession
    {
        internal string Packet()
        {
            using (var stream = new MemoryStream())
            {
                using (var writer = new BinaryWriter(stream))
                {
                    writer.Write(DuelProtocol.Magic);
                    writer.Write(DuelProtocol.Version);
                    writer.Write(Host);
                    writer.Write((byte)Role);
                    writer.Write(Token.ToByteArray());
                    writer.Write(peerToken.ToByteArray());
                    writer.Write(++sequence);
                    writer.Write(Round);
                    writer.Write(localScene);
                    writer.Write(localReady);
                    writer.Write((sbyte)Winner);
                    writer.Write((ushort)Speed);
                    writer.Write(speedSequence);
                    writer.Write(Host ? remoteFireAck : localFireAck);
                    DuelProtocol.WritePose(writer, Poses[Role]);
                    DuelProtocol.WritePose(writer, Poses[1 - Role]);
                    writer.Write((byte)(Host ? 0 : pendingFireActions.Count));
                    if (!Host)
                        foreach (DuelFire action in pendingFireActions)
                        {
                            writer.Write(action.Id);
                            writer.Write(action.Round);
                            DuelProtocol.WritePose(writer, action.Pose);
                            writer.Write(action.Kind);
                        }
                    writer.Write((byte)(Host ? Shots.Count : 0));
                    if (Host)
                        foreach (DuelShot shot in Shots)
                        {
                            writer.Write(shot.X);
                            writer.Write(shot.Y);
                            writer.Write(shot.DX);
                            writer.Write(shot.DY);
                            writer.Write(shot.Age);
                            writer.Write(shot.Distance);
                            writer.Write(shot.Owner);
                            writer.Write(shot.Weapon);
                        }
                    writer.Write(shiftHeld);
                    writer.Write(Host ? skillAck : 0u);
                    writer.Write(Blocks);
                    Skills[0].Write(writer, observationTime);
                    Skills[1].Write(writer, observationTime);
                    Health.Write(writer, observationTime);
                    writer.Write(Hits);
                    Armories[0].Write(writer, observationTime);
                    Armories[1].Write(writer, observationTime);
                    writer.Write((byte)(Host ? 0 : pendingSkillActions.Count));
                    if (!Host)
                        foreach (SkillAction action in pendingSkillActions)
                        {
                            writer.Write(action.Id);
                            writer.Write(action.Round);
                            writer.Write(action.Cycle);
                            writer.Write(action.Kind);
                            DuelProtocol.WritePose(writer, action.Origin);
                            byte[] word = System.Text.Encoding.ASCII.GetBytes(action.Word);
                            writer.Write((byte)word.Length);
                            writer.Write(word);
                        }
                    string text = DuelProtocol.Prefix + Convert.ToBase64String(stream.ToArray());
                    if (text.Length > DuelProtocol.MaximumTextLength)
                        throw new InvalidOperationException("Duel packet exceeds transport buffer");
                    return text;
                }
            }
        }
    }
}
