using Discord.Social.CSharp.Sdk.Authorization;
using Discord.Social.CSharp.Sdk.Extensions;
using OpenCampfire.Authorization;
using Discord.Social.CSharp.Sdk;

public sealed class AuthorizeProcess(DiscordClient discordClient, DiscordOptions discordOptions, IAuthorizationTokenStore tokenStore)
{
    public void Execute()
    {
        var cachedToken = tokenStore.Load();

        // When the token is still valid, we can immediately apply it and complete the authorization process.
        if (cachedToken?.IsValid(DateTimeOffset.UtcNow, TimeSpan.FromMinutes(5)) == true)
        {
            ApplyToken(cachedToken, () => {}, TryRefreshOrReAuthorizeOAuth);
            return;
        }

        // The token was no longer valid, we need to either refresh it or restart the OAuth process.
        TryRefreshOrReAuthorizeOAuth();
    }

    /// <summary>
    /// Attempts to refresh the cached token. If the refresh fails, the OAuth process is restarted.
    /// </summary>
    private void TryRefreshOrReAuthorizeOAuth()
    {
        var cachedToken = tokenStore.Load()!;

        void RefreshTokenCallBack(ClientResult result, string accessToken, string refreshToken, AuthorizationTokenType tokenType, int expiresIn, string scope)
        {
            using (result)
            {
                if (result.Successful && !string.IsNullOrWhiteSpace(accessToken))
                {
                    SaveAndApplyToken(
                        AuthorizationToken.FromExchange(
                            accessToken,
                            refreshToken,
                            tokenType,
                            expiresIn,
                            scope,
                            cachedToken
                        )
                    );

                    return;
                }
            }

            // RefreshToken failed, clear the token store and start the authorization process again.
            tokenStore.Clear();
            AuthorizeUsingOAuth();
        }

        discordClient.RefreshToken(discordOptions.ApplicationId, cachedToken.RefreshToken, RefreshTokenCallBack);
    }

    /// <summary>
    /// Saves the given token to the token store and applies it to the Discord client.
    /// On failure, the token store is cleared and the OAuth process is restarted.
    /// </summary>
    /// <param name="token"></param>
    private void SaveAndApplyToken(AuthorizationToken token)
    {
        void OnSuccess() => tokenStore.Save(token);
        void OnFailure() 
        {
            tokenStore.Clear();
            AuthorizeUsingOAuth();
        }

        ApplyToken(token, OnSuccess, OnFailure);
    }

    /// <summary>
    /// Applies the given token to the Discord client.
    /// </summary>
    /// <param name="token"></param>
    /// <param name="onSuccess"></param>
    /// <param name="onFailure"></param>
    private void ApplyToken(AuthorizationToken token, Action onSuccess, Action onFailure)
    {
        void UpdateTokenCallback(ClientResult result)
        {
            using (result)
            {
                if (result.Successful)
                {
                    onSuccess();
                    return;
                }
            }

            onFailure();
        }

        discordClient.UpdateToken(token.TokenType, token.AccessToken, UpdateTokenCallback);
    }

    private void AuthorizeUsingOAuth()
    {
        void GetTokenCallback(ClientResult result, string accessToken, string refreshToken, AuthorizationTokenType tokenType, int expiresIn, string scope)
        {
            using (result)
                if (!result.Successful || string.IsNullOrWhiteSpace(accessToken))
                    throw new OAuthFailedException();

            SaveAndApplyToken(
                AuthorizationToken.FromExchange(
                    accessToken,
                    refreshToken,
                    tokenType,
                    expiresIn,
                    scope
                )
            );
        }

        var codeVerifier    = discordClient.CreateAuthorizationCodeVerifier();
        var authArgs        = new AuthorizationArgs();

        authArgs.SetClientId(discordOptions.ApplicationId);
        authArgs.SetScopes(DiscordClientExtensions.GetDefaultPresenceScopes());
        authArgs.SetCodeChallenge(codeVerifier.Challenge());

        discordClient.Authorize(authArgs, (result, code, redirectUri) =>
        {
            using (result) if (!result.Successful) throw new OAuthFailedException();
            discordClient.GetToken(discordOptions.ApplicationId, code, codeVerifier.Verifier, redirectUri, GetTokenCallback);
        });
    }
}