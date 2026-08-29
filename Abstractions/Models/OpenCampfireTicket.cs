using System.Net;

namespace OpenCampfire.Abstractions.Models;

internal record OpenCampfireTicket
{
    public required IPEndPoint IpEndpoint { get; init; }
}