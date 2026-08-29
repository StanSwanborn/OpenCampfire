using Discord.Social.CSharp.Sdk.RichPresence;
using Discord.Social.CSharp.Sdk;

namespace OpenCampfire.Processes;

public sealed class StreamingProcess(DiscordClient discordClient)
{
    public void Execute()
    {
        Console.WriteLine("Streaming process started...");

        var friendsCount = discordClient.GetRelationships().Size;
        Console.WriteLine($"Successfull connection, friend count: {friendsCount}");

        var activity = new Activity();
        activity.SetType(ActivityType.Playing);
        activity.SetState("Currently testing OpenCampfire");
        activity.SetDetails("Testing OpenCampfire");

        var button = new ActivityButton();
        button.SetLabel("Start Watching");
        button.SetURL("http://127.0.0.1:6000/stream/watch?id=uniquestreamid");

        activity.AddButton(button);

        discordClient.UpdateRichPresence(activity, (_) => {});
    }
}