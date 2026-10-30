using System;
using System.Net.NetworkInformation;

namespace TypeGameMod
{
    internal sealed partial class ModSession
    {
        void PrepareLanDiscovery()
        {
            LanDiscovery discovery;
            try
            {
                discovery = LanInterfaces.Discover();
            }
            catch (NetworkInformationException error)
            {
                log("LAN adapter enumeration failed: " + error.Message);
                return;
            }
            if (discovery == null)
            {
                log("LAN discovery: no active Ethernet/Wi-Fi IPv4 subnet; native search retained.");
                return;
            }
            // Immutable BSTR for the game process lifetime. Only the connection's
            // search address changes; Windows routes and DirectPlay gameplay stay intact.
            uint text = WriteBstr(caveBase + CaveLayout.LanAddressText, discovery.Broadcast);
            Write(caveBase + CaveLayout.LanAddressPointer, BitConverter.GetBytes(text));
            log("LAN discovery adapter=" + discovery.Adapter.Name + " local=" + discovery.Adapter.Address +
                " broadcast=" + discovery.Broadcast + " index=" + discovery.Adapter.Index);
        }
    }
}
