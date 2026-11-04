using System.Net;
using OpenCampfire.Helpers;
using Xunit;

namespace OpenCampfire.Core.Tests.Helpers;

public sealed class TicketHelperTests
{
    [Theory]
    [InlineData("192.0.2.42", 3478)]
    [InlineData("2001:db8::42", 65535)]
    public void UnpackBase64TicketString_RestoresOriginalIpEndpoint(string address, int port)
    {
        var originalEndpoint = new IPEndPoint(IPAddress.Parse(address), port);

        var ticket = TicketHelper.CreateBase64TicketString(originalEndpoint);
        var unpackedTicket = TicketHelper.UnpackBase64TicketString(ticket);

        Assert.Equal(originalEndpoint.Address, unpackedTicket.IpEndpoint.Address);
        Assert.Equal(originalEndpoint.Port, unpackedTicket.IpEndpoint.Port);
    }
}
