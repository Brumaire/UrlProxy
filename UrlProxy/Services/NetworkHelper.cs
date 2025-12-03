using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace UrlProxy.Services;

public static class NetworkHelper
{
    public static string? GetWiFiIP()
    {
        try
        {
            var interfaces = NetworkInterface.GetAllNetworkInterfaces();

            foreach (var ni in interfaces)
            {
                // 找 Wi-Fi 介面
                if (ni.OperationalStatus == OperationalStatus.Up &&
                    (ni.Name.Contains("Wi-Fi", StringComparison.OrdinalIgnoreCase) ||
                     ni.Name.Contains("Wireless", StringComparison.OrdinalIgnoreCase) ||
                     ni.Description.Contains("Wireless", StringComparison.OrdinalIgnoreCase)))
                {
                    var ipProps = ni.GetIPProperties();
                    foreach (var addr in ipProps.UnicastAddresses)
                    {
                        if (addr.Address.AddressFamily == AddressFamily.InterNetwork)
                        {
                            return addr.Address.ToString();
                        }
                    }
                }
            }

            // 如果找不到 Wi-Fi，找任何可用的 IPv4
            foreach (var ni in interfaces)
            {
                if (ni.OperationalStatus == OperationalStatus.Up &&
                    ni.NetworkInterfaceType != NetworkInterfaceType.Loopback)
                {
                    var ipProps = ni.GetIPProperties();
                    foreach (var addr in ipProps.UnicastAddresses)
                    {
                        if (addr.Address.AddressFamily == AddressFamily.InterNetwork &&
                            !addr.Address.ToString().StartsWith("169.254")) // 排除 APIPA
                        {
                            return addr.Address.ToString();
                        }
                    }
                }
            }
        }
        catch
        {
            // 忽略錯誤
        }

        return null;
    }

    public static List<(string Name, string IP)> GetAllIPs()
    {
        var result = new List<(string Name, string IP)>();

        try
        {
            var interfaces = NetworkInterface.GetAllNetworkInterfaces();

            foreach (var ni in interfaces)
            {
                if (ni.OperationalStatus == OperationalStatus.Up &&
                    ni.NetworkInterfaceType != NetworkInterfaceType.Loopback)
                {
                    var ipProps = ni.GetIPProperties();
                    foreach (var addr in ipProps.UnicastAddresses)
                    {
                        if (addr.Address.AddressFamily == AddressFamily.InterNetwork &&
                            !addr.Address.ToString().StartsWith("169.254"))
                        {
                            result.Add((ni.Name, addr.Address.ToString()));
                        }
                    }
                }
            }
        }
        catch
        {
            // 忽略錯誤
        }

        return result;
    }
}
