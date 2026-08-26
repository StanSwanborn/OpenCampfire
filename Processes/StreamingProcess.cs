using Discord.Social.CSharp.Sdk.RichPresence;
using Discord.Social.CSharp.Sdk;

public sealed class StreamingProcess(DiscordClient discordClient)
{
    public void Execute()
    {
        var activity = new Activity();
        activity.SetType(ActivityType.Streaming);
        activity.SetState("Currently testing OpenCampfire");
        activity.SetDetails("Testing OpenCampfire");

        var button = new ActivityButton();
        button.SetLabel("Start Streaming");
        button.SetURL("opencampfire://stream/start");

        activity.AddButton(button);

        discordClient.UpdateRichPresence(activity, null);
    }
}