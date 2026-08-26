using Discord.Social.CSharp.Sdk.Authorization;
using Discord.Social.CSharp.Sdk.Extensions;
using Discord.Social.CSharp.Sdk.Logging;
using Discord.Social.CSharp.Sdk;

ulong ApplicationId = 1541884081984241784;
using var client = new DiscordClient();

client.SetApplicationId(ApplicationId);

client.AddLogCallback((message, severity) =>
{
    Console.WriteLine($"[{severity}] {message}");
}, LoggingSeverity.Info);

client.SetStatusChangedCallback((status, error, errorDetail) =>
{
    Console.WriteLine($"Status changed: {status}, Error: {error}, Detail: {errorDetail}");
});

var codeVerifier = client.CreateAuthorizationCodeVerifier();

var authArgs = new AuthorizationArgs();
authArgs.SetClientId(ApplicationId); 
authArgs.SetScopes(DiscordClientExtensions.GetDefaultPresenceScopes());
authArgs.SetCodeChallenge(codeVerifier.Challenge());

// Begin authorization process
client.Authorize(authArgs, (result, code, redirectUri) =>
{
    if (result.Successful)
    {
        Console.WriteLine($"Authorization successful! Code: {code}, Redirect URI: {redirectUri}");
    }
    else
    {
        Console.WriteLine($"Authorization failed: {result.Error}");
    }

    client.GetToken(ApplicationId, code, codeVerifier.Verifier, redirectUri, 
        (result, accessToken, refreshToken, tokenType, expiresIn, scope) => {
            if (result.Successful)
            {
                Console.WriteLine($"Token exchange successful! Access Token: {accessToken}, Refresh Token: {refreshToken}, Token Type: {tokenType}, Expires In: {expiresIn}, Scope: {scope}");
            }
            else
            {
                Console.WriteLine($"Token exchange failed: {result.Error}");
            }
        }
    );
});

while(true)
{
    client.RunCallBacks();
    Thread.Sleep(10); // Adjust the sleep duration as needed
}