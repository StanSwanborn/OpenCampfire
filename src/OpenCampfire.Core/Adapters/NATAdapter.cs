using OpenCampfire.Options;
using SIPSorcery.Net;
using System.Net;

namespace OpenCampfire.Adapters;

internal class NATAdapter(NATOptions natOptions)
{
    public async Task<IPEndPoint> GetPublicEndpointAsync()
    {
        Console.WriteLine("Retrieving public UDP endpoint...");

        foreach (var (name, server) in natOptions.NATServers)
        {
            try
            {
                Console.WriteLine($"Querying {name} ({server})...");
                var parts = server.Split(':');
                var host = parts[0];
                var port = int.Parse(parts[1]);

                var publicEndpoint = STUNClient.GetPublicIPEndPoint(host, port);

                if (publicEndpoint is not null)
                {
                    Console.WriteLine($"✓ Public endpoint: {publicEndpoint}");
                    return publicEndpoint;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ STUN server {name} failed: {ex.Message}");
            }
        }

        throw new Exception("Could not determine public UDP endpoint.");
    }
}