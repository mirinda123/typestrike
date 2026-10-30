using System;

namespace TypeGameMod
{
    internal sealed partial class ModSession
    {
        GameFrame ReadGameFrame()
        {
            var frame = new GameFrame();
            uint callback =
                BitConverter.ToUInt32(Read(gameBase + GameLayout.ActiveCallback, 4), 0) & 0xfffffffeu;
            // VB6 embeds the callback interface 0x160 bytes into PoliceGame.
            // Its vtable identifies the minigame; menu objects have a different vtable.
            if (callback < 0x160 ||
                BitConverter.ToUInt32(Read(callback - 0x160, 4), 0) != gameBase + GameLayout.PoliceGameVtable)
                return frame;
            frame.Object = callback - 0x160;
            Write(ControlBase + Control.GameObject, BitConverter.GetBytes(frame.Object));
            byte[] state = Read(frame.Object + 0x34, 0xd4);
            frame.Scene = BitConverter.ToUInt32(state, 0);
            frame.Online = BitConverter.ToUInt16(state, 0xcc) != 0;
            frame.Started = BitConverter.ToUInt16(state, 0xce);
            frame.Role = BitConverter.ToUInt32(state, 0xd0);
            uint players = BitConverter.ToUInt32(state, 0x94);
            if ((frame.Scene == GameScene.Playing || frame.Online) && frame.Role <= 1 && players != 0)
                frame.Players = Read(players, 112);
            return frame;
        }

        void PublishMovement(InputFrame input)
        {
            Write(ControlBase + Control.Focused, BitConverter.GetBytes(input.Focused ? 1u : 0u));
            Write(ControlBase + Control.Heading, BitConverter.GetBytes(input.Heading));
            double length = Math.Max(1, Math.Sqrt(input.X * input.X + input.Y * input.Y));
            byte[] movement = new byte[16];
            BinaryFields.Write(movement, 0, (float)(230 * input.X / length));
            BinaryFields.Write(movement, 4, (float)(140 * input.Y / length));
            BinaryFields.Write(movement, 8, (uint)((input.X > 0 ? 2 : 0) + (input.Y > 0 ? 1 : 0)));
            BinaryFields.Write(movement, 12, input.X != 0 || input.Y != 0 ? 1u : 0u);
            Write(ControlBase + Control.VelocityX, movement);
        }

        void Publish(byte[] snapshot, ref uint sequence)
        {
            // Odd sequence means a write is in progress. Readers keep their last
            // complete frame unless they see the same even sequence before/after.
            sequence += 2;
            Write(caveBase + CaveLayout.Snapshot, BitConverter.GetBytes(sequence - 1));
            byte[] body = new byte[SnapshotBytes - 4];
            Buffer.BlockCopy(snapshot, 4, body, 0, body.Length);
            Write(caveBase + CaveLayout.Snapshot + 4, body);
            Write(caveBase + CaveLayout.Snapshot, BitConverter.GetBytes(sequence));
        }
    }
}
