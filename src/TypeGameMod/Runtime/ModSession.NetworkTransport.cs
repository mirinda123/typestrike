using System;

namespace TypeGameMod
{
    internal sealed partial class ModSession
    {
        bool OwnedSurfacesRemain()
        {
            foreach (int offset in new[] { Control.CardinalSurface, Control.RunSurface0, Control.RunSurface1,
                                           Control.IdleSurface })
                if (BitConverter.ToUInt32(Read(ControlBase + (uint)offset, 4), 0) != 0)
                    return true;
            for (int role = 0; role < 2; role++)
                for (int bank = 0; bank < 4; bank++)
                    if (BitConverter.ToUInt32(
                            Read(caveBase + CaveLayout.RemoteSurfaces + (uint)(role * 128 + bank * 32), 4),
                            0) != 0)
                        return true;
            return BitConverter.ToUInt32(Read(ControlBase + Control.ImeCaptured, 4), 0) != 0 ||
                   BitConverter.ToUInt32(Read(ControlBase + Control.MusicBuffer, 4), 0) != 0;
        }

        void QueueScene(uint gameObject, uint scene, int desired)
        {
            if (desired < 0 || desired > 5 || scene == (uint)desired ||
                BitConverter.ToUInt32(Read(ControlBase + Control.SceneRequested, 4), 0) != 0)
                return;
            Write(ControlBase + Control.SceneObject, BitConverter.GetBytes(gameObject));
            Write(ControlBase + Control.SceneRequested, BitConverter.GetBytes((uint)desired + 1));
        }

        void ReceiveLan(double now)
        {
            byte[] header = Read(ControlBase + Control.ReceiveSequence, 8);
            uint sequence = BitConverter.ToUInt32(header, 0), length = BitConverter.ToUInt32(header, 4);
            if (sequence == receivedSequence || (sequence & 1) != 0 || length > 8190 || (length & 1) != 0)
                return;
            uint sender = BitConverter.ToUInt32(Read(ControlBase + Control.PacketSender, 4), 0);
            byte[] buffer = Read(caveBase + CaveLayout.ReceiveText, (int)length);
            if (BitConverter.ToUInt32(Read(ControlBase + Control.ReceiveSequence, 4), 0) != sequence)
                return;
            receivedSequence = sequence;
            if (networkMatch.Receive(System.Text.Encoding.Unicode.GetString(buffer), sender, now) &&
                !networkMatch.Host)
                SelectedBulletSpeed = networkMatch.Speed;
        }

        void SendLan(double now)
        {
            if (now - lastNetworkSend < .04 ||
                BitConverter.ToUInt32(Read(ControlBase + Control.SendAcknowledged, 4), 0) != sentSequence ||
                BitConverter.ToUInt32(Read(ControlBase + Control.Sending, 4), 0) != 0)
                return;
            WriteBstr(caveBase + CaveLayout.SendBstr, networkMatch.Packet());
            sentSequence++;
            Write(ControlBase + Control.SendSequence, BitConverter.GetBytes(sentSequence));
            lastNetworkSend = now;
        }

        void PublishPoses(uint gameObject, bool playing)
        {
            byte[] buffer = new byte[64];
            BinaryFields.Write(buffer, 4, gameObject);
            BinaryFields.Write(buffer, 8, playing ? 1u : 0u);
            BinaryFields.Write(buffer, 12, networkMatch.Round);
            for (int role = 0; role < 2; role++)
            {
                DuelPose pose = networkMatch.Poses[role];
                int offset = 16 + role * 24;
                BinaryFields.Write(buffer, offset, pose.X);
                BinaryFields.Write(buffer, offset + 4, pose.Y);
                BinaryFields.Write(buffer, offset + 8, (uint)pose.Heading);
                BinaryFields.Write(buffer, offset + 12,
                                   role == 0 && networkMatch.Skills[0].Shield ? 3u : (uint)pose.Weapon);
                BinaryFields.Write(buffer, offset + 16, (uint)pose.Moving);
                BinaryFields.Write(buffer, offset + 20, (uint)pose.Phase);
            }
            poseMailboxSequence += 2;
            Write(caveBase + CaveLayout.PoseMailbox, BitConverter.GetBytes(poseMailboxSequence - 1));
            byte[] body = new byte[60];
            Buffer.BlockCopy(buffer, 4, body, 0, 60);
            Write(caveBase + CaveLayout.PoseMailbox + 4, body);
            Write(caveBase + CaveLayout.PoseMailbox, BitConverter.GetBytes(poseMailboxSequence));
        }
    }
}
