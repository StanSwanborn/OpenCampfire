using Discord.Social.CSharp.Sdk.RichPresence;
using Discord.Social.CSharp.Sdk;
using OpenCampfire.Adapters;
using OpenCampfire.Options;
using OpenCampfire.Helpers;

namespace OpenCampfire.Processes;

public sealed class StreamingProcess(DiscordClient discordClient, NATOptions natOptions)
{
    private readonly NATAdapter natAdapter = new(natOptions);

    public async Task Execute()
    {
        Console.WriteLine("Streaming process started...");

        var publicIp = await natAdapter.GetPublicEndpointAsync().ConfigureAwait(false);
        var ticket = TicketHelper.CreateBase64TicketString(publicIp);

        var activity = new Activity();
        activity.SetType(ActivityType.Playing);
        activity.SetState("Currently testing OpenCampfire");
        activity.SetDetails("Testing OpenCampfire");

        var button = new ActivityButton();
        button.SetLabel("Start Watching");
        button.SetURL($"http://opencampfire.localhost:6000/stream/watch?ticket={ticket}");

        activity.AddButton(button);

        discordClient.UpdateRichPresence(activity, (_) => {});
    }
}