public interface IOpenCampfireService : IDisposable
{
    Task AuthorizeAsync();
    Task ConnectToDiscordAsync();
    Task RunCallbacksAsync();
    Task StartStreamingAsync();
    Task DisposeAsync();
}