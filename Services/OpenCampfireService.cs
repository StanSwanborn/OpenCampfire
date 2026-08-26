using Microsoft.Extensions.Options;
using Discord.Social.CSharp.Sdk;

public sealed class OpenCampfireService : IOpenCampfireService
{
    private readonly DiscordClient _discordClient;
    private readonly DiscordOptions _discordOptions;
    private bool _disposed;
    
    public OpenCampfireService(IOptions<DiscordOptions> discordOptions)
    {
        _discordClient = new DiscordClient();
        _discordOptions = discordOptions.Value;

        // Initialize the Discord client with the application ID from the configuration
        _discordClient.SetApplicationId(_discordOptions.ApplicationId);
    }

    public async Task AuthorizeAsync()
    {
        var authorizeProcess = new AuthorizeProcess(_discordClient, _discordOptions);
        await authorizeProcess.Execute().ConfigureAwait(false);
    }

    public async Task ConnectToDiscordAsync()
    {
        _discordClient.TryStart();
    }

    public async Task RunCallbacksAsync()
    {
        _discordClient.RunCallBacks();
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _discordClient.Dispose();
        _disposed = true;
    }
}