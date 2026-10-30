using System;
using System.IO;
using System.Security.Cryptography;

namespace TypeGameMod
{
    internal static class GameIdentity
    {
        internal const string ExecutableHash =
            "893bc2fd6e0d90fc7a8ee752dde20798d001b008fdce461b3f6ceb2a1880fc0c";
        internal const string ResourceHash =
            "fd505810e262a9941c93d0a9d3b82ff72b7a797e0496f9de7dcbb8298da9b2e6";
        internal const string CompatHash = "f75f0ac48d2782f225c483dc2f1142303a513e8dd8a60793891ade89f64755ea";
        internal const int SpriteBytes = 165008;
        internal const uint ResourceBitmapOffset = 0xbae8a0;

        internal static string Hash(string path)
        {
            using (var sha = SHA256.Create()) using (var stream = File.OpenRead(path)) return BitConverter
                .ToString(sha.ComputeHash(stream))
                .Replace("-", "")
                .ToLowerInvariant();
        }
    }
}
