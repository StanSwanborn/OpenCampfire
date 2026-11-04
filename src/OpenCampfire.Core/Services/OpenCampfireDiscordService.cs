using OpenCampfire.Abstractions.Interfaces;
using Discord.Social.CSharp.Sdk.Logging;
using Microsoft.Extensions.Options;
using Discord.Social.CSharp.Sdk;
using OpenCampfire.Processes;
using OpenCampfire.Options;

namespace OpenCampfire.Services;

public sealed class OpenCampfireDiscordService : IOpenCampfireDiscordService
{
    private readonly DiscordClient _discordClient;
    private readonly AuthorizeProcess _authorizeProcess;
    private readonly StreamingProcess _streamingProcess;
    private bool _disposed;

    public OpenCampfireDiscordService(
        IOptions<DiscordOptions> discordOptions,
        IOptions<NATOptions> natOptions,
        IOptions<LocalhostOptions> localhostOptions,
        DiscordClient discordClient,
        IAuthorizationTokenStore tokenStore)
    {
        _discordClient  = discordClient;

        _authorizeProcess = new AuthorizeProcess(discordClient, discordOptions.Value, tokenStore);
        _streamingProcess = new StreamingProcess(discordClient, natOptions.Value, localhostOptions.Value);

        _discordClient.SetApplicationId(discordOptions.Value.ApplicationId);

        _discordClient.AddLogCallback((message, severity) =>
            Console.WriteLine($"[{severity}] {message}"), LoggingSeverity.Info
        );

        _discordClient.SetStatusChangedCallback((status, error, errorDetail) =>
            Console.WriteLine($"Status changed: {status}, Error: {error}, Detail: {errorDetail}")
        );
    }

    public async Task AuthorizeAsync() => 
        await _authorizeProcess.Execute().ConfigureAwait(false);

    public async Task ConnectToDiscordAsync() => 
        _discordClient.Connect();

    public async Task RunCallbacksAsync() => 
        _discordClient.RunCallBacks();

    public async Task StartStreamingAsync() => 
        await _streamingProcess.Execute().ConfigureAwait(false);

    public async Task DisposeAsync() => 
        this.Dispose();

    public void Dispose()
    {
        if (_disposed)
            return;

        _discordClient.Dispose();
        _disposed = true;
    }
}