namespace OpenCampfire.Abstractions.Interfaces;

public interface IOpenCampfireDiscordService : IDisposable
{
    Task AuthorizeAsync();
    Task ConnectToDiscordAsync();
    Task RunCallbacksAsync();
    Task StartStreamingAsync();
    Task DisposeAsync();
}