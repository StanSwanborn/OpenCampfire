using Microsoft.Extensions.Options;
using Discord.Social.CSharp.Sdk;

public sealed class OpenCampfireService : IOpenCampfireService
{
    private readonly DiscordClient _discordClient;
    private readonly AuthorizeProcess _authorizeProcess;
    private bool _disposed;

    public OpenCampfireService(
        IOptions<DiscordOptions> discordOptions,
        DiscordClient discordClient,
        IAuthorizationTokenStore tokenStore,
        AuthorizationCoordinator authorizationCoordinator)
    {
        _discordClient = discordClient;
        var options = discordOptions.Value;
        _authorizeProcess = new AuthorizeProcess(
            discordClient,
            options,
            tokenStore,
            authorizationCoordinator);

        _discordClient.SetApplicationId(options.ApplicationId);
    }

    public Task AuthorizeAsync()
    {
        _authorizeProcess.Execute();
        return Task.CompletedTask;
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