using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace DestinyPOS2026.Wpf.Helpers;

public static class NetworkHelper
{
    public static string GetLocalIPAddress()
    {
        foreach (var nic in NetworkInterface.GetAllNetworkInterfaces())
        {
            // Only consider Ethernet or Wi-Fi adapters that are up
            if (nic.NetworkInterfaceType != NetworkInterfaceType.Ethernet &&
                nic.NetworkInterfaceType != NetworkInterfaceType.Wireless80211)
                continue;

            if (nic.OperationalStatus != OperationalStatus.Up)
                continue;

            var props = nic.GetIPProperties();
            foreach (var addr in props.UnicastAddresses)
            {
                if (addr.Address.AddressFamily == AddressFamily.InterNetwork &&
                    !IPAddress.IsLoopback(addr.Address))
                {
                    // Optional: only return IPs in 192.168.x.x or 10.x.x.x range
                    var ip = addr.Address.ToString();
                    if (ip.StartsWith("192.168.") || ip.StartsWith("10.") || ip.StartsWith("172."))
                        return ip;
                }
            }
        }

        // Fallback to loopback
        return "127.0.0.1";
    }
}
