using System;
using System.IO;
using System.Reflection;
using TypeGameMod;

internal static class CompatibilityTests
{
    const BindingFlags Members = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
    static int checks;
    static void Check(bool condition, string reason)
    {
        checks++;
        if (!condition)
            throw new Exception(reason);
    }

    // This adapter stays in tests: production has no reflection or legacy fallback.
    sealed class Peer
    {
        readonly object instance;
        internal Peer(Assembly assembly, string prefix, bool host, int role)
        {
            Type type = assembly.GetType(prefix + "DuelSession", true);
            instance = Activator.CreateInstance(type, Members, null, new object[] { host, role, 450 }, null);
        }
        internal object Get(string field)
        {
            return GetField(instance, field);
        }
        internal object Call(string method, params object[] args)
        {
            return instance.GetType().GetMethod(method, Members).Invoke(instance, args);
        }
        static object GetField(object target, string name)
        {
            return target.GetType().GetField(name, Members).GetValue(target);
        }
    }

    internal static void Run(string baseline)
    {
        if (!File.Exists(baseline))
            throw new FileNotFoundException("Baseline MOD executable is required", baseline);
        Assembly old = Assembly.LoadFile(Path.GetFullPath(baseline));
        Check(old.GetName().Version == new Version(1, 11, 1, 0),
              "comparison uses the delivered 1.11.1 baseline");
        Assembly current = typeof(DuelSession).Assembly;
        Type legacyPayload = old.GetType("Payload", true);
        foreach (uint[] addresses in new[] { new uint[] { 0x570000, 0x400000 },
                                             new uint[] { 0x1f300000, 0x500000 } })
        {
            byte[] expected =
                (byte[])legacyPayload.GetMethod("Build", BindingFlags.Static | BindingFlags.NonPublic)
                    .Invoke(null, new object[] { addresses[0], addresses[1] });
            byte[] actual = NativePayload.Build(addresses[0], addresses[1]);
            // Keep byte-level checks for unchanged bridge blocks. Animation,
            // result and input blocks intentionally differ from this baseline.
            foreach (int[] range in new[] { new[] { 0x600, 0xc00 }, new[] { 0x1800, 0x1c00 },
                                            new[] { 0x1e00, 0x2000 }, new[] { 0x4400, 0x4800 },
                                            new[] { 0x4a00, 0x4c00 }, new[] { 0x5000, 0xcc00 },
                                            new[] { 0xd400, 0xe800 }, new[] { 0xf000, 0xf900 } })
            {
                bool same = true;
                for (int at = range[0]; at < range[1]; at++)
                    same &= expected[at] == actual[at];
                Check(same, "untouched native blocks match accepted baseline at both relocation bases");
            }
        }
        foreach (string resource in old.GetManifestResourceNames())
        {
            using (Stream expected = old.GetManifestResourceStream(resource)) using (
                Stream actual = current.GetManifestResourceStream(resource))
            {
                Check(actual != null, "accepted sprite resource remains embedded: " + resource);
                using (var left = new BinaryReader(expected)) using (var right = new BinaryReader(actual))
                    Check(
                        SameBytes(left.ReadBytes((int)expected.Length), right.ReadBytes((int)actual.Length)),
                        "accepted sprite resource remains unchanged: " + resource);
            }
        }
        foreach (bool newHost in new[] { false, true })
            foreach (int hostRole in new[] { 0, 1 })
            {
                var host = new Peer(newHost ? current : old, newHost ? "TypeGameMod." : "", true, hostRole);
                var client =
                    new Peer(newHost ? old : current, newHost ? "" : "TypeGameMod.", false, 1 - hostRole);
                Check(!(bool)host.Call("Receive", (string)client.Call("Packet"), 2u, 0.0),
                      "old and new ammunition rules cannot handshake");
                Check(!(bool)client.Call("Receive", (string)host.Call("Packet"), 1u, 0.0),
                      "both protocol versions reject incompatible peer");
                Check(!(bool)host.Get("acknowledged") && !(bool)client.Get("acknowledged"),
                      "version mismatch cannot start a combat session");
            }
        Console.WriteLine("PASS " + checks +
                          " baseline art/native preservation and explicit wire-v4/v6 separation checks");
    }

    static bool SameBytes(byte[] left, byte[] right)
    {
        if (left.Length != right.Length)
            return false;
        for (int i = 0; i < left.Length; i++)
            if (left[i] != right[i])
                return false;
        return true;
    }
}
