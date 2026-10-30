using System;
using System.Net;
using System.Net.NetworkInformation;
using TypeGameMod;

internal static class LanDiscoveryTests
{
    static int checks;
    static void Check(bool condition, string reason)
    {
        checks++;
        if (!condition)
            throw new Exception(reason);
    }

    static LanAdapter Adapter(string name, int index, string ip, string mask, bool gateway)
    {
        return new LanAdapter { Name = name,
                                Description = name,
                                Index = index,
                                Up = true,
                                HasGateway = gateway,
                                Type = NetworkInterfaceType.Ethernet,
                                Address = IPAddress.Parse(ip),
                                Mask = IPAddress.Parse(mask) };
    }

    internal static void Run()
    {
        var virtualBox = Adapter("VirtualBox Host-Only", 4, "192.168.56.1", "255.255.255.0", false);
        var vmware = Adapter("VMware VMnet8", 7, "172.16.2.1", "255.255.255.0", true);
        var ethernet = Adapter("Ethernet", 11, "192.168.5.26", "255.255.255.0", true);
        var wifi = Adapter("Wi-Fi", 20, "10.42.9.37", "255.255.254.0", true);
        wifi.Type = NetworkInterfaceType.Wireless80211;
        foreach (LanAdapter[] order in new[] { new[] { virtualBox, vmware, ethernet },
                                               new[] { ethernet, vmware, virtualBox } })
        {
            LanDiscovery result = LanDiscovery.Select(order, 4);
            Check(result.Adapter == ethernet && result.Broadcast == "192.168.5.255",
                  "virtual default and adapter enumeration order cannot divert LAN search");
        }
        Check(LanDiscovery.Select(new[] { ethernet, wifi }, 20).Broadcast == "10.42.9.255",
              "Wi-Fi default uses its actual /23 mask and DHCP address");
        Check(LanDiscovery.Select(new[] { ethernet, wifi }, 11).Adapter == ethernet,
              "physical default route resolves two active LAN adapters");
        ethernet.Up = false;
        Check(LanDiscovery.Select(new[] { ethernet, wifi }, 11).Adapter == wifi,
              "disconnected cable does not beat Wi-Fi");
        ethernet.Up = true;
        wifi.HasGateway = false;
        Check(LanDiscovery.Select(new[] { wifi, ethernet }, 7).Adapter == ethernet,
              "a virtual VPN default falls back to the physical gateway LAN");
        Check(LanDiscovery.Select(new[] { virtualBox, vmware }, 4) == null,
              "no physical LAN leaves native behavior available without a fabricated target");
        Check(LanDiscovery.Select(new[] { wifi }, 0).Adapter == wifi,
              "isolated LAN without an Internet gateway remains usable");
        foreach (NetworkInterfaceType type in new[] { NetworkInterfaceType.Loopback,
                                                      NetworkInterfaceType.Tunnel, NetworkInterfaceType.Ppp })
        {
            virtualBox.Name = virtualBox.Description = "Connection";
            virtualBox.Type = type;
            Check(LanDiscovery.Select(new[] { virtualBox, ethernet }, 4).Adapter == ethernet,
                  "loopback, tunnel and PPP adapters never receive LAN broadcasts");
        }
        string broadcast;
        foreach (string mask in new[] { "0.0.0.0", "255.0.255.0", "255.255.255.254", "255.255.255.255" })
            Check(
                !LanDiscovery.TryBroadcast(IPAddress.Parse("10.0.0.5"), IPAddress.Parse(mask), out broadcast),
                "invalid and point-to-point masks have no directed broadcast");
        foreach (string ip in new[] { "0.0.0.1", "127.0.0.1", "169.254.2.1", "224.0.0.1", "192.168.5.0",
                                      "192.168.5.255" })
            Check(!LanDiscovery.TryBroadcast(IPAddress.Parse(ip), IPAddress.Parse("255.255.255.0"),
                                             out broadcast),
                  "non-unicast and non-LAN source addresses are excluded");
        Check(!LanDiscovery.TryBroadcast(IPAddress.IPv6Loopback, IPAddress.Parse("255.255.255.0"),
                                         out broadcast),
              "IPv6 is not passed to legacy IPv4 DirectPlay");
        for (int network = 1; network < 200; network++)
        {
            var moved = Adapter("Ethernet", network + 30, "10." + network + ".12.17", "255.255.0.0", true);
            Check(LanDiscovery.Select(new[] { moved }, moved.Index).Broadcast == "10." + network + ".255.255",
                  "another machine, adapter index and subnet need no saved Windows route");
        }
        Check(CaveLayout.LanAddress + 128 <= CaveLayout.LanAddressPointer &&
                  CaveLayout.LanAddressPointer + 8 <= CaveLayout.LanAddressText &&
                  CaveLayout.LanAddressText + 36 < CaveLayout.Hud,
              "code, pointer, counter and immutable IPv4 BSTR do not overlap");
        Console.WriteLine(
            "PASS " + checks +
            " LAN discovery: physical Ethernet/Wi-Fi, VPN/VM exclusion, DHCP, masks and layout");
    }
}
