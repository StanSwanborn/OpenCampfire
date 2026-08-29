using OpenCampfire.Abstractions.Models;
using System.Net;
using System.Text;

namespace OpenCampfire.Helpers;

internal static class TicketHelper
{
    public static string CreateBase64TicketString(IPEndPoint ipEndpoint) =>
        Convert.ToBase64String(
            Encoding.UTF8.GetBytes($"OCF{AssemblyVersionHelper.Major}|{ipEndpoint.Address}|{ipEndpoint.Port}")
        );

    public static OpenCampfireTicket UnpackBase64TicketString(string base64Ticket)
    {
        var ticket = Encoding.UTF8.GetString(
            Convert.FromBase64String(base64Ticket));

        var parts = ticket.Split('|');

        if (parts.Length != 3)
            throw new FormatException("Invalid OpenCampfire ticket.");

        if (parts[0] != $"OCF{AssemblyVersionHelper.Major}")
            throw new FormatException(
                $"Unsupported OpenCampfire ticket version: {parts[0]}.");

        if (!IPAddress.TryParse(parts[1], out var ipAddress))
            throw new FormatException("Invalid IP address.");

        if (!int.TryParse(parts[2], out var port) ||
            port is < 1 or > 65535)
            throw new FormatException("Invalid port.");

        return new OpenCampfireTicket
        {
            IpEndpoint = new IPEndPoint(ipAddress, port)
        };
    }
}