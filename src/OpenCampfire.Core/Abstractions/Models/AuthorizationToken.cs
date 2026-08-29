using Discord.Social.CSharp.Sdk.Authorization;

namespace OpenCampfire.Abstractions.Models;

public sealed record AuthorizationToken(
    string AccessToken,
    string RefreshToken,
    AuthorizationTokenType TokenType,
    DateTimeOffset ExpiresAt,
    string Scope)
{
    public bool IsValid(DateTimeOffset now, TimeSpan safetyMargin) =>
        !string.IsNullOrWhiteSpace(AccessToken) && ExpiresAt > now.Add(safetyMargin);

    public static AuthorizationToken FromExchange(
        string accessToken,
        string refreshToken,
        AuthorizationTokenType tokenType,
        int expiresIn,
        string scope,
        AuthorizationToken? previous = null) =>
        new(
            accessToken,
            string.IsNullOrWhiteSpace(refreshToken) ? previous?.RefreshToken ?? string.Empty : refreshToken,
            tokenType,
            DateTimeOffset.UtcNow.AddSeconds(Math.Max(0, expiresIn)),
            scope);
}