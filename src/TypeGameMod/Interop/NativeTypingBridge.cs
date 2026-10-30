using System;
using System.Text;

namespace TypeGameMod
{
    internal sealed class NativeTypingBridge
    {
        readonly Func<uint, int, byte[]> read;
        readonly Action<uint, byte[]> write;
        readonly uint control, cave;
        uint readSequence;

        internal NativeTypingBridge(Func<uint, int, byte[]> read, Action<uint, byte[]> write, uint control,
                                    uint cave)
        {
            this.read = read;
            this.write = write;
            this.control = control;
            this.cave = cave;
        }

        internal ArticleProgress ReadArticle(uint gameObject, uint role, uint cycle)
        {
            uint text = BitConverter.ToUInt32(read(gameObject + 0xf0, 4), 0);
            if (text < 4)
                return null;
            uint length = BitConverter.ToUInt32(read(text - 4, 4), 0);
            if (length == 0 || length > 65536 || (length & 1) != 0)
                return null;
            uint players = BitConverter.ToUInt32(read(gameObject + 0xc8, 4), 0);
            int position = (int)BitConverter.ToUInt32(read(players + role * 56 + 0x34, 4), 0);
            return new ArticleProgress(Encoding.Unicode.GetString(read(text, (int)length)), position, cycle);
        }

        internal void ReadKeys(Action<int, int> accept)
        {
            uint end = BitConverter.ToUInt32(read(control + Control.KeyboardWrite, 4), 0);
            uint count = unchecked(end - readSequence);
            if (count > 128)
            {
                readSequence = end;
                count = 0;
            }
            byte[] queue = count == 0 ? null : read(cave + CaveLayout.KeyboardQueue, 1024);
            while (readSequence != end)
            {
                int at = (int)(readSequence & 127) * 8;
                int key = BitConverter.ToInt32(queue, at), position = BitConverter.ToInt32(queue, at + 4);
                readSequence++;
                accept(key, position);
            }
            write(control + Control.KeyboardRead, BitConverter.GetBytes(readSequence));
        }

        internal void SetTyping(bool enabled)
        {
            write(control + Control.TypingMode, BitConverter.GetBytes(enabled ? 1u : 0u));
        }
    }
}
