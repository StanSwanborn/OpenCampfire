public interface IOpenCampfireService : IDisposable
{
    Task AuthorizeAsync();
    Task ConnectToDiscordAsync();
    Task RunCallbacksAsync();
}