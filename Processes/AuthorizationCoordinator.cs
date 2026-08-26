using Discord.Social.CSharp.Sdk;
using Discord.Social.CSharp.Sdk.Authorization;

public sealed class AuthorizationCoordinator : IDisposable
{
    private static readonly TimeSpan ExpirationSafetyMargin = TimeSpan.FromMinutes(5);

    private readonly DiscordClient _discordClient;
    private IAuthorizationTokenStore? _tokenStore;
    private AuthorizationToken? _cachedToken;
    private AuthorizationCodeVerifier? _codeVerifier;
    private AuthorizationArgs? _authorizationArgs;
    private Action<bool>? _completion;
    private ulong _applicationId;
    private string _scopes = string.Empty;
    private bool _disposed;

    public AuthorizationCoordinator(DiscordClient discordClient)
    {
        _discordClient = discordClient;
    }

    public void Begin(
        ulong applicationId,
        string scopes,
        IAuthorizationTokenStore tokenStore,
        Action<bool> completion)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(tokenStore);
        ArgumentNullException.ThrowIfNull(completion);

        _applicationId = applicationId;
        _scopes = scopes;
        _tokenStore = tokenStore;
        _completion = completion;
        _cachedToken = tokenStore.Load();

        if (_cachedToken?.IsValid(DateTimeOffset.UtcNow, ExpirationSafetyMargin) == true)
        {
            ApplyToken(_cachedToken, () => Complete(true), TryRefreshOrAuthorize);
            return;
        }

        TryRefreshOrAuthorize();
    }

    private void TryRefreshOrAuthorize()
    {
        if (!string.IsNullOrWhiteSpace(_cachedToken?.RefreshToken))
        {
            _discordClient.RefreshToken(_applicationId, _cachedToken.RefreshToken, (result, accessToken, refreshToken, tokenType, expiresIn, scope) =>
            {
                using (result)
                {
                    if (result.Successful && !string.IsNullOrWhiteSpace(accessToken))
                    {
                        SaveAndApply(AuthorizationToken.FromExchange(
                            accessToken,
                            refreshToken,
                            tokenType,
                            expiresIn,
                            scope,
                            _cachedToken));
                        return;
                    }
                }

                _tokenStore?.Clear();
                BeginOAuthAuthorization();
            });
            return;
        }

        BeginOAuthAuthorization();
    }

    private void BeginOAuthAuthorization()
    {
        _codeVerifier = _discordClient.CreateAuthorizationCodeVerifier();
        _authorizationArgs = new AuthorizationArgs();
        _authorizationArgs.SetClientId(_applicationId);
        _authorizationArgs.SetScopes(_scopes);
        _authorizationArgs.SetCodeChallenge(_codeVerifier.Challenge());

        _discordClient.Authorize(_authorizationArgs, (result, code, redirectUri) =>
        {
            using (result)
            {
                if (!result.Successful)
                {
                    Complete(false);
                    return;
                }
            }

            _discordClient.GetToken(_applicationId, code, _codeVerifier!.Verifier, redirectUri,
                (tokenResult, accessToken, refreshToken, tokenType, expiresIn, scope) =>
                {
                    using (tokenResult)
                    {
                        if (!tokenResult.Successful || string.IsNullOrWhiteSpace(accessToken))
                        {
                            Complete(false);
                            return;
                        }
                    }

                    SaveAndApply(AuthorizationToken.FromExchange(
                        accessToken,
                        refreshToken,
                        tokenType,
                        expiresIn,
                        scope));
                });
        });
    }

    private void SaveAndApply(AuthorizationToken token)
    {
        _cachedToken = token;
        ApplyToken(token,
            () =>
            {
                try
                {
                    _tokenStore!.Save(token);
                    Complete(true);
                }
                catch
                {
                    Complete(false);
                }
            },
            () =>
            {
                _tokenStore?.Clear();
                BeginOAuthAuthorization();
            });
    }

    private void ApplyToken(AuthorizationToken token, Action onSuccess, Action onFailure)
    {
        _discordClient.UpdateToken(token.TokenType, token.AccessToken, result =>
        {
            using (result)
            {
                if (result.Successful)
                {
                    _discordClient.Connect();
                    onSuccess();
                    return;
                }
            }

            onFailure();
        });
    }

    private void Complete(bool successful)
    {
        var completion = _completion;
        _completion = null;
        _tokenStore = null;
        _cachedToken = null;

        _authorizationArgs?.Dispose();
        _authorizationArgs = null;
        _codeVerifier?.Dispose();
        _codeVerifier = null;

        completion?.Invoke(successful);
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _authorizationArgs?.Dispose();
        _codeVerifier?.Dispose();
        _disposed = true;
    }
}