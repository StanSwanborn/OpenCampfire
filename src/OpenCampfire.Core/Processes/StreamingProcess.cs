using Discord.Social.CSharp.Sdk.RichPresence;
using Discord.Social.CSharp.Sdk;
using OpenCampfire.Adapters;
using OpenCampfire.Options;
using OpenCampfire.Helpers;

namespace OpenCampfire.Processes;

public sealed class StreamingProcess(
    DiscordClient discordClient,
    NATOptions natOptions,
    LocalhostOptions localhostOptions)
{
    private readonly NATAdapter natAdapter = new(natOptions);

    public async Task Execute()
    {
        Console.WriteLine("Streaming process started...");

        // Construct public IP with ticket (p2p connection)
        var publicIp = await natAdapter.GetPublicEndpointAsync().ConfigureAwait(false);
        var ticket = TicketHelper.CreateBase64TicketString(publicIp);

        // TODO: Start a stream (capture current screen)
        // TODO: Start a local web server to serve the stream (localhost)
        // TODO: Make the stream accessible via the public IP and ticket (p2p connection)

        // Discord update
        var activity = new Activity();
        activity.SetType(ActivityType.Playing);
        activity.SetState("Currently testing OpenCampfire");
        activity.SetDetails("Testing OpenCampfire");

        var button = new ActivityButton();
        button.SetLabel("Start Watching");
        button.SetURL($"http://opencampfire.localhost:{localhostOptions.Port}/stream/watch?ticket={ticket}");

        activity.AddButton(button);

        discordClient.UpdateRichPresence(activity, (_) => {});
        // /Discord update
    }
}