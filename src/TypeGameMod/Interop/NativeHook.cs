using System;

namespace TypeGameMod
{
    // Keep each detour's location, expected bytes and destination together.
    internal sealed class NativeHook
    {
        internal readonly string Name;
        internal readonly uint GameOffset, CaveOffset;
        internal readonly byte[] Original;
        NativeHook(string name, uint site, uint entry, params byte[] original)
        {
            Name = name;
            GameOffset = site;
            CaveOffset = entry;
            Original = original;
        }

        internal byte[] Jump(uint game, uint cave)
        {
            byte[] bytes = new byte[Original.Length];
            for (int i = 0; i < bytes.Length; i++)
                bytes[i] = 0x90;
            bytes[0] = 0xe9;
            BinaryFields.Write(bytes, 1, unchecked(cave + CaveOffset - (game + GameOffset + 5)));
            return bytes;
        }

        internal static readonly NativeHook[] All = {
            new NativeHook("LAN discovery address", 0x2b35a, CaveLayout.LanAddress, 0x68, 0x28, 0xc4, 0x40,
                           0x00),
            new NativeHook("local movement", 0x223e0, CaveLayout.Movement, 0x8b, 0x54, 0x24, 0x04, 0x53),
            new NativeHook("keyboard", 0x23e70, CaveLayout.Keyboard, 0x55, 0x8b, 0xec, 0x83, 0xec, 0x0c),
            new NativeHook("physical collision", 0x272b5, CaveLayout.Collision, 0x8b, 0x45, 0xcc, 0x85, 0xc0),
            new NativeHook("render", 0x20dd0, CaveLayout.Render, 0x55, 0x8b, 0xec, 0x83, 0xec, 0x08),
            new NativeHook("sprite origin", 0x20fcb, CaveLayout.SpriteOrigin, 0xe8, 0x50, 0x9b, 0x00, 0x00),
            new NativeHook("result controls", 0x274e0, CaveLayout.ResultRender, 0x55, 0x8b, 0xec, 0x83, 0xec,
                           0x0c),
            new NativeHook("graphics device", 0x2ae40, CaveLayout.Device, 0x51, 0x56, 0x8b, 0x74, 0x24, 0x0c),
            new NativeHook("DirectPlay poll", 0x2cfb0, CaveLayout.NetworkPoll, 0x55, 0x8b, 0xec, 0x83, 0xec,
                           0x18),
            new NativeHook("DirectPlay receive", 0x2d1fb, CaveLayout.NetworkReceive, 0x83, 0xbd, 0xec, 0xfe,
                           0xff, 0xff, 0x06),
            new NativeHook("remote movement", 0x22ba0, CaveLayout.RemoteMovement, 0x53, 0x56, 0x8b, 0x74,
                           0x24, 0x0c),
            new NativeHook("caps lock", 0x1baf2, CaveLayout.CapsLock, 0x8d, 0x55, 0xbc, 0x8d, 0x45, 0xc4),
            new NativeHook("native article", 0x24352, CaveLayout.ArticleLoop, 0x66, 0x39, 0x9e, 0x00, 0x01,
                           0x00, 0x00)
        };
    }
}
