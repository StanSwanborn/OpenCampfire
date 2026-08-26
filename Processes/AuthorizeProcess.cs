using Discord.Social.CSharp.Sdk.Extensions;
using Discord.Social.CSharp.Sdk.Logging;
using Discord.Social.CSharp.Sdk;

public sealed class AuthorizeProcess(
    DiscordClient discordClient,
    DiscordOptions discordOptions,
    IAuthorizationTokenStore tokenStore,
    AuthorizationCoordinator authorizationCoordinator)
{
    public void Execute()
    {
        discordClient.AddLogCallback((message, severity) =>
            Console.WriteLine($"[{severity}] {message}"), LoggingSeverity.Info
        );

        discordClient.SetStatusChangedCallback((status, error, errorDetail) =>
            Console.WriteLine($"Status changed: {status}, Error: {error}, Detail: {errorDetail}")
        );

        authorizationCoordinator.Begin(
            discordOptions.ApplicationId,
            DiscordClientExtensions.GetDefaultPresenceScopes(),
            tokenStore,
            successful =>
            {
                if (!successful)
                    Console.WriteLine("Discord authorization failed.");
            });
    }
}