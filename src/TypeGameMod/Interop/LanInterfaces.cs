using System.Collections.Generic;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.InteropServices;

namespace TypeGameMod
{
    internal static class LanInterfaces
    {
        [DllImport("iphlpapi.dll")]
        static extern uint GetBestInterface(uint destination, out uint index);

        internal static LanDiscovery Discover()
        {
            uint preferred;
            if (GetBestInterface(0, out preferred) != 0)
                preferred = 0;
            var adapters = new List<LanAdapter>();
            foreach (NetworkInterface network in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (network.OperationalStatus != OperationalStatus.Up ||
                    !network.Supports(NetworkInterfaceComponent.IPv4))
                    continue;
                IPInterfaceProperties properties = network.GetIPProperties();
                bool gateway = false;
                foreach (GatewayIPAddressInformation entry in properties.GatewayAddresses)
                    gateway |= entry.Address.AddressFamily == AddressFamily.InterNetwork &&
                               !entry.Address.Equals(IPAddress.Any);
                foreach (UnicastIPAddressInformation entry in properties.UnicastAddresses)
                    if (entry.Address.AddressFamily == AddressFamily.InterNetwork)
                        adapters.Add(new LanAdapter { Name = network.Name, Description = network.Description,
                                                      Index = properties.GetIPv4Properties().Index, Up = true,
                                                      HasGateway = gateway,
                                                      Type = network.NetworkInterfaceType,
                                                      Address = entry.Address, Mask = entry.IPv4Mask });
            }
            return LanDiscovery.Select(adapters, (int)preferred);
        }
    }
}
