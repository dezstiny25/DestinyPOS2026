using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace DestinyPOS2026.Wpf.Helpers;

public static class NetworkHelper
{
    public static string GetLocalIPAddress()
    {
        // First pass: prefer active adapters that have an IPv4 gateway (likely the real LAN)
        var interfaces = NetworkInterface.GetAllNetworkInterfaces()
            .Where(ni => ni.OperationalStatus == OperationalStatus.Up)
            .Where(ni => ni.NetworkInterfaceType == NetworkInterfaceType.Ethernet || ni.NetworkInterfaceType == NetworkInterfaceType.Wireless80211)
            .ToList();

        foreach (var ni in interfaces)
        {
            var name = ni.Name.ToLower();
            if (name.Contains("virtual") || name.Contains("veth") || name.Contains("hyper") || name.Contains("host-only") || name.Contains("virtualbox") || name.Contains("vbox") || name.Contains("vmnet"))
                continue;

            var ipProps = ni.GetIPProperties();
            bool hasIpv4Gateway = ipProps.GatewayAddresses.Any(g => g.Address != null && g.Address.AddressFamily == AddressFamily.InterNetwork);
            if (!hasIpv4Gateway) continue;

            var addr = ipProps.UnicastAddresses.FirstOrDefault(a => a.Address.AddressFamily == AddressFamily.InterNetwork && !IPAddress.IsLoopback(a.Address));
            if (addr != null) return addr.Address.ToString();
        }

        // Second pass: pick any non-virtual active IPv4 address
        foreach (var ni in interfaces)
        {
            var name = ni.Name.ToLower();
            if (name.Contains("virtual") || name.Contains("veth") || name.Contains("hyper") || name.Contains("host-only") || name.Contains("virtualbox") || name.Contains("vbox") || name.Contains("vmnet"))
                continue;

            var ipProps = ni.GetIPProperties();
            var addr = ipProps.UnicastAddresses.FirstOrDefault(a => a.Address.AddressFamily == AddressFamily.InterNetwork && !IPAddress.IsLoopback(a.Address));
            if (addr != null) return addr.Address.ToString();
        }

        return "127.0.0.1";
    }
}
