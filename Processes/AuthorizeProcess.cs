using Discord.Social.CSharp.Sdk.Authorization;
using Discord.Social.CSharp.Sdk.Extensions;
using Discord.Social.CSharp.Sdk.Logging;
using Discord.Social.CSharp.Sdk;

public sealed class AuthorizeProcess(DiscordClient discordClient, DiscordOptions discordOptions) : IDisposable
{
    private readonly DiscordClient _discordClient = discordClient;
    private readonly DiscordOptions _discordOptions = discordOptions;
    private bool _disposed;

    public async Task Execute()
    {
        _discordClient.AddLogCallback((message, severity) => 
            Console.WriteLine($"[{severity}] {message}"), LoggingSeverity.Info
        );

        _discordClient.SetStatusChangedCallback((status, error, errorDetail) =>
            Console.WriteLine($"Status changed: {status}, Error: {error}, Detail: {errorDetail}")
        );

        var codeVerifier = _discordClient.CreateAuthorizationCodeVerifier();

        var authArgs = new AuthorizationArgs();
        authArgs.SetClientId(_discordOptions.ApplicationId);
        authArgs.SetScopes(DiscordClientExtensions.GetDefaultPresenceScopes());
        authArgs.SetCodeChallenge(codeVerifier.Challenge());

        _discordClient.Authorize(authArgs, (result, code, redirectUri) =>
        {
            if(!result.Successful)
            {
                Console.WriteLine($"Authorization failed: {result.Error}");
                return; // TODO: Throw AuthorizationFailedException
            }

            Console.WriteLine($"Authorization successful! Code: {code}, Redirect URI: {redirectUri}");

            _discordClient.GetToken(_discordOptions.ApplicationId, code, codeVerifier.Verifier, redirectUri,
                (result, accessToken, refreshToken, tokenType, expiresIn, scope) => {
                    if(!result.Successful)
                    {
                        Console.WriteLine($"Token exchange failed: {result.Error}");
                        return; // TODO: Throw TokenExchangeFailedException
                    }

                    Console.WriteLine($"Token exchange successful! Access Token: {accessToken}, Refresh Token: {refreshToken}, Token Type: {tokenType}, Expires In: {expiresIn}, Scope: {scope}");
                    UpdateClientTokenOnSuccessfulAuthorization(accessToken, refreshToken);
                }
            );
        });
    }

    private void UpdateClientTokenOnSuccessfulAuthorization(string accessToken, string refreshToken) 
    {
        _discordClient.UpdateToken(AuthorizationTokenType.Bearer, accessToken, (result) => {
            if(!result.Successful)
            {
                Console.WriteLine($"Failed to update client token: {result.Error}");
                return; // TODO: Throw TokenUpdateFailedException
            }

            Console.WriteLine("Client token updated successfully.");
            _discordClient.Connect();
        });
    }

    public void Dispose()
    {
        if(_disposed)
            return;

        _discordClient.Dispose();
        _disposed = true;
    }
}