using Discord.Social.CSharp.Sdk.Logging;
using Microsoft.Extensions.Options;
using OpenCampfire.Authorization;
using Discord.Social.CSharp.Sdk;

public sealed class OpenCampfireService : IOpenCampfireService
{
    private readonly DiscordClient _discordClient;
    private readonly AuthorizeProcess _authorizeProcess;
    private readonly StreamingProcess _streamingProcess;
    private bool _disposed;

    public OpenCampfireService(IOptions<DiscordOptions> discordOptions, DiscordClient discordClient, IAuthorizationTokenStore tokenStore)
    {
        _discordClient  = discordClient;
        var options     = discordOptions.Value;

        _authorizeProcess = new AuthorizeProcess(discordClient, options, tokenStore);
        _streamingProcess = new StreamingProcess(discordClient);

        _discordClient.SetApplicationId(options.ApplicationId);

        _discordClient.AddLogCallback((message, severity) =>
            Console.WriteLine($"[{severity}] {message}"), LoggingSeverity.Info
        );

        _discordClient.SetStatusChangedCallback((status, error, errorDetail) =>
            Console.WriteLine($"Status changed: {status}, Error: {error}, Detail: {errorDetail}")
        );
    }

    public async Task AuthorizeAsync() => _authorizeProcess.Execute();

    public async Task ConnectToDiscordAsync() => _discordClient.Connect();

    public async Task RunCallbacksAsync() => _discordClient.RunCallBacks();

    public async Task StartStreamingAsync() => _streamingProcess.Execute();

    public void Dispose()
    {
        if (_disposed)
            return;

        _discordClient.Dispose();

        _disposed = true;
    }
}