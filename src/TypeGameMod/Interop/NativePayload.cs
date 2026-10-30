using System;
using System.IO;

namespace TypeGameMod
{
    // Assembly and the relocation table are generated from native/*.asm.
    // Relative calls inside the cave already work at every allocation address.
    internal static class NativePayload
    {
        internal static byte[] Build(uint cave, uint game)
        {
            byte[] code;
            using (Stream stream = Open("Code"))
            {
                using (var reader = new BinaryReader(stream))
                {
                    code = reader.ReadBytes((int)stream.Length);
                }
            }
            using (Stream stream = Open("Relocations"))
            {
                using (var reader = new BinaryReader(stream))
                {
                    while (stream.Position < stream.Length)
                    {
                        int offset = reader.ReadInt32();
                        int kind = reader.ReadInt32();
                        uint target = reader.ReadUInt32();
                        uint value;
                        switch (kind)
                        {
                        case 0:
                            value = cave + target;
                            break;
                        case 1:
                            value = unchecked(game + target - (cave + (uint)offset + 4));
                            break;
                        case 2:
                            value = game + target;
                            break;
                        default:
                            throw new InvalidDataException("Unknown native relocation kind.");
                        }
                        Buffer.BlockCopy(BitConverter.GetBytes(value), 0, code, offset, 4);
                    }
                }
            }
            return code;
        }

        static Stream Open(string name)
        {
            Stream stream =
                typeof(NativePayload).Assembly.GetManifestResourceStream("TypeGameMod.Native." + name);
            if (stream == null)
                throw new InvalidOperationException("Missing generated native payload: " + name);
            return stream;
        }
    }
}
