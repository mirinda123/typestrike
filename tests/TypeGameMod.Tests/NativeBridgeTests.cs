using System;
using System.Collections.Generic;
using System.Text;
using TypeGameMod;

internal static class NativeBridgeTests
{
    sealed class Memory
    {
        internal readonly byte[] Bytes = new byte[0x30000];
        internal int Writes;
        internal byte[] Read(uint address, int count)
        {
            var result = new byte[count];
            Buffer.BlockCopy(Bytes, (int)address, result, 0, count);
            return result;
        }
        internal void Write(uint address, byte[] data)
        {
            Buffer.BlockCopy(data, 0, Bytes, (int)address, data.Length);
            Writes++;
        }
        internal void Number(uint address, uint value)
        {
            Write(address, BitConverter.GetBytes(value));
        }
        internal uint Number(uint address)
        {
            return BitConverter.ToUInt32(Bytes, (int)address);
        }
    }

    static void Check(bool condition, string reason)
    {
        if (!condition)
            throw new Exception(reason);
    }

    internal static void Run()
    {
        const uint control = 0x800;
        var memory = new Memory();
        var typing = new NativeTypingBridge(memory.Read, memory.Write, control, 0);
        const uint game = 0x10000, text = 0x11004, players = 0x12000;
        byte[] article = Encoding.Unicode.GetBytes("one two three");
        memory.Number(game + 0xf0, text);
        memory.Number(game + 0xc8, players);
        memory.Number(text - 4, (uint)article.Length);
        memory.Write(text, article);
        memory.Number(players + 56 + 0x34, 4);
        ArticleProgress progress = typing.ReadArticle(game, 1, 7);
        Check(progress.Text == "one two three" && progress.Position == 4,
              "bridge reads the native UTF-16 article and the selected role's cursor");
        memory.Number(text - 4, 65538);
        Check(typing.ReadArticle(game, 1, 7) == null,
              "malformed native article length is rejected at the boundary");

        Action<uint, int, int> key = delegate(uint serial, int value, int position)
        {
            uint at = CaveLayout.KeyboardQueue + (serial & 127) * 8;
            memory.Number(at, (uint)value);
            memory.Number(at + 4, (uint)position);
        };
        var received = new List<string>();
        Action<int, int> accept = delegate(int value, int position)
        {
            received.Add(value + ":" + position);
        };
        key(0, 9, 0);
        key(1, 'o', 1);
        memory.Number(control + Control.KeyboardWrite, 2);
        typing.ReadKeys(accept);
        typing.ReadKeys(accept);
        Check(received.Count == 2 && received[0] == "9:0" && received[1] == "111:1" &&
                  memory.Number(control + Control.KeyboardRead) == 2,
              "queue preserves key/cursor order and acknowledges each event once");
        memory.Number(control + Control.KeyboardWrite, 131);
        typing.ReadKeys(accept);
        Check(received.Count == 2 && memory.Number(control + Control.KeyboardRead) == 131,
              "overflow drops overwritten events instead of replaying old words");
        memory.Number(control + Control.KeyboardWrite, uint.MaxValue - 1);
        typing.ReadKeys(accept);
        key(uint.MaxValue - 1, 'a', 3);
        key(uint.MaxValue, 'b', 4);
        key(0, 'c', 5);
        memory.Number(control + Control.KeyboardWrite, 1);
        typing.ReadKeys(accept);
        Check(received.Count == 5 && received[4] == "99:5", "sequence and ring index wrap independently");
        typing.SetTyping(true);
        Check(memory.Number(control + Control.TypingMode) == 1, "typing mode is published to native code");

        const uint assets = 0x30000000;
        var skills = new NativeSkillBridge(memory.Read, memory.Write, control, assets, delegate(string s) {});
        var thief = new RoleSkill(1) { BlinkSerial = 1, BlinkX = 510, BlinkY = 420 };
        skills.QueueBlink(game, 1, thief, 1);
        Check(memory.Number(control + Control.BlinkSequence) == 1 &&
                  memory.Number(control + Control.BlinkObject) == game,
              "blink payload is written before its sequence becomes visible");
        int writes = memory.Writes;
        skills.QueueBlink(game, 1, thief, 1);
        Check(memory.Writes == writes, "already-published blinks are not duplicated");
        var pose = new DuelPose { X = 100, Y = 100, Heading = 2 };
        skills.ApplyPendingPose(ref pose, 1.1);
        Check(pose.X == 510 && pose.Y == 420, "local pose uses the pending UI-thread destination");
        thief.BlinkSerial = 2;
        skills.QueueBlink(game, 1, thief, 1.1);
        Check(memory.Number(control + Control.BlinkSequence) == 1, "a pending request cannot be overwritten");
        memory.Number(control + Control.BlinkAcknowledged, 1);
        pose.X = 110;
        skills.ApplyPendingPose(ref pose, 1.1);
        Check(pose.X == 110, "native acknowledgement ends local destination prediction");
        skills.SetPoliceShield(true);
        Check(memory.Number(control + Control.PoliceAkRun0) == assets + AssetLayout.PoliceShield0 &&
                  memory.Number(control + Control.PoliceAkIdle) == assets + AssetLayout.PoliceShieldIdleTable,
              "shield art swaps only the existing native tables");
        writes = memory.Writes;
        skills.SetPoliceShield(true);
        Check(memory.Writes == writes, "unchanged shield art does not republish pointers");
        skills.SetPoliceShield(false);
        skills.SetLocalShield(true);
        Check(memory.Number(control + Control.PoliceAkRun0) == assets + AssetLayout.PoliceArmed0 &&
                  memory.Number(control + Control.SkillActive) == 1,
              "lowering shield restores weapon tables independently of the local skill flag");
        Console.WriteLine(
            "PASS native bridges: article, keyboard wrap/ack, blink mailbox and shield pointers");
    }
}
