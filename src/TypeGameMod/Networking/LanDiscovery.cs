using System;
using System.Collections.Generic;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace TypeGameMod
{
    internal sealed class LanAdapter
    {
        internal string Name, Description;
        internal int Index;
        internal bool Up, HasGateway;
        internal NetworkInterfaceType Type;
        internal IPAddress Address, Mask;
    }

    internal sealed class LanDiscovery
    {
        internal readonly LanAdapter Adapter;
        internal readonly string Broadcast;

        LanDiscovery(LanAdapter adapter, string broadcast)
        {
            Adapter = adapter;
            Broadcast = broadcast;
        }

        internal static LanDiscovery Select(IEnumerable<LanAdapter> adapters, int preferredIndex)
        {
            LanDiscovery selected = null;
            foreach (LanAdapter adapter in adapters)
            {
                string broadcast;
                if (!Eligible(adapter) || !TryBroadcast(adapter.Address, adapter.Mask, out broadcast))
                    continue;
                if (selected == null || Compare(adapter, selected.Adapter, preferredIndex) < 0)
                    selected = new LanDiscovery(adapter, broadcast);
            }
            return selected;
        }

        static bool Eligible(LanAdapter adapter)
        {
            if (!adapter.Up || (adapter.Type != NetworkInterfaceType.Ethernet &&
                                adapter.Type != NetworkInterfaceType.Wireless80211 &&
                                adapter.Type != NetworkInterfaceType.GigabitEthernet &&
                                adapter.Type != NetworkInterfaceType.FastEthernetT &&
                                adapter.Type != NetworkInterfaceType.FastEthernetFx))
                return false;
            string label = (adapter.Name + " " + adapter.Description).ToLowerInvariant();
            foreach (string marker in new[] { "virtual", "vmware", "vbox", "hyper-v", "vpn", "tap-windows",
                                              "wireguard", "tailscale", "zerotier", "hamachi" })
                if (label.Contains(marker))
                    return false;
            return true;
        }

        static int Compare(LanAdapter a, LanAdapter b, int preferredIndex)
        {
            // Prefer the normal default interface, then a LAN with a gateway.
            // A VPN default cannot win: virtual/tunnel adapters were excluded above.
            int result = (b.Index == preferredIndex).CompareTo(a.Index == preferredIndex);
            if (result == 0)
                result = b.HasGateway.CompareTo(a.HasGateway);
            if (result == 0)
                result = String.CompareOrdinal(a.Name, b.Name);
            if (result == 0)
                result = String.CompareOrdinal(a.Address.ToString(), b.Address.ToString());
            return result;
        }

        internal static bool TryBroadcast(IPAddress address, IPAddress mask, out string broadcast)
        {
            broadcast = null;
            if (address == null || mask == null || address.AddressFamily != AddressFamily.InterNetwork ||
                mask.AddressFamily != AddressFamily.InterNetwork)
                return false;
            byte[] ip = address.GetAddressBytes(), subnet = mask.GetAddressBytes();
            if (ip[0] == 0 || ip[0] == 127 || ip[0] >= 224 || (ip[0] == 169 && ip[1] == 254))
                return false;
            uint bits = 0;
            for (int i = 0; i < 4; i++)
                bits = (bits << 8) | subnet[i];
            uint hosts = ~bits;
            // A directed broadcast needs a contiguous mask and at least two host bits.
            if (bits == 0 || hosts < 3 || (hosts & (hosts + 1)) != 0)
                return false;
            uint host = 0;
            for (int i = 0; i < 4; i++)
                host = (host << 8) | ip[i];
            if ((host & hosts) == 0 || (host & hosts) == hosts)
                return false;
            byte[] target = new byte[4];
            for (int i = 0; i < 4; i++)
                target[i] = (byte)(ip[i] | (byte)~subnet[i]);
            broadcast = new IPAddress(target).ToString();
            return true;
        }
    }
}
